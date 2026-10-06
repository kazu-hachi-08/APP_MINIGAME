#nullable enable
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CardGame.Core.Commands;
using CardGame.Core.Definitions;
using CardGame.Core.Events;
using CardGame.Core.State;
using CardGame.Unity.App;
using CardGame.Unity.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CardGame.Unity.Battle
{
    /// <summary>
    /// バトル画面(04-screens.md)。横 1920×1080 基準。
    /// コマンド適用 → イベント列を順に再生(簡易エフェクト)→ 状態から手札・場を作り直す、の流れ。
    /// </summary>
    public sealed class BattleScreen : MonoBehaviour
    {
        // ---- レイアウト定数 ----
        private const float HandY = 0, HandH = 320;
        private const float LeftX = 10, ColW = 280;
        private const float CenterX = 300, CenterW = 1320;
        private const float RightX = 1630;
        private const float MyBoardY = 330, BoardH = 360, EnemyBoardY = 700;
        private static readonly Color BoardHighlight = new Color(0.3f, 0.7f, 0.4f, 0.28f);

        private BattleSession _session = null!;
        private EventFormatter _fmt = null!;
        private CardDatabase _db = null!;
        private int _viewer;

        // ---- UI 要素 ----
        private RectTransform _handArea = null!, _myBoardArea = null!, _enemyBoardArea = null!, _dragLayer = null!, _overlayLayer = null!;
        private Image _myBoardBg = null!, _enemyBoardBg = null!, _myLeaderPanel = null!, _enemyLeaderPanel = null!;
        private RectTransform? _myPortrait, _enemyPortrait;     // ハースストーン風のリーダーの肖像(中身はクラスが変わったら作り直す)
        private Image[]? _myPpRow, _enemyPpRow;      // PP の結晶(10 個)
        private RectTransform _myPpArea = null!, _enemyPpArea = null!;   // PP の結晶の列(覚醒の刻の演出の基準)
        private Image? _endTurnGlow;                    // 何もできないときのターン終了の強調
        private bool _endTurnHint;
        private CardClass? _myPortraitClass, _enemyPortraitClass;
        private Text _turnText = null!, _myHp = null!, _myDeck = null!, _enemyHp = null!, _enemyDeck = null!, _enemyHand = null!, _logText = null!, _hintText = null!;
        private Button _endTurn = null!;
        private readonly List<CardView> _handViews = new();
        private readonly List<CardView> _myBoardViews = new();
        private readonly List<CardView> _enemyBoardViews = new();
        private readonly List<string> _log = new();

        // ---- 操作状態 ----
        private bool _inputEnabled;
        private bool _playing;
        private PendingPlay? _pending;
        private GameObject? _overlay;
        private Coroutine? _aiRoutine;
        private GameObject? _tooltip;
        private RectTransform _fxLayer = null!;      // エフェクトを描くレイヤー(最前面)
        private RectTransform _shakeRoot = null!;   // 画面の揺れをかける対象
        private float _lastImpactAt = -1f;          // 直前に命中エフェクトを出した時刻
        private Coroutine? _tooltipRoutine;

        private sealed class PendingPlay
        {
            public int HandIndex;
            public int BoardPosition;
            public List<TargetRef> ValidTargets = new();
        }

        public RectTransform DragLayer => _dragLayer;
        private int Me => _viewer;
        private int Enemy => 1 - _viewer;
        private GameState State => _session.State;

        // ---- 開発用(DevAutoplay の操作シミュレーションから参照) ----
        internal IReadOnlyList<CardView> HandViews => _handViews;
        internal IReadOnlyList<CardView> MyBoardViews => _myBoardViews;
        internal IReadOnlyList<CardView> EnemyBoardViews => _enemyBoardViews;
        internal bool InputEnabled => _inputEnabled && !_playing;
        internal bool HasPending => _pending != null;
        internal IReadOnlyList<TargetRef> PendingTargets => _pending?.ValidTargets ?? (IReadOnlyList<TargetRef>)System.Array.Empty<TargetRef>();
        internal GameObject? CurrentOverlay => _overlay;
        internal RectTransform EnemyLeaderRect => _enemyLeaderPanel.rectTransform;
        internal RectTransform MyBoardRect => _myBoardBg.rectTransform;
        internal Button EndTurnButton => _endTurn;
        internal BattleSession Session => _session;

        // ==================================================================
        // 開始
        // ==================================================================

        public void Begin(BattleConfig config)
        {
            Time.timeScale = 1f;   // 念のため(演出の一時停止が残っていても対戦開始時は等速に)
            _db = AppRoot.Instance.Db;
            _session = new BattleSession(_db, config);
            _fmt = new EventFormatter(_db);
            _viewer = config.Mode == BattleMode.Online ? config.LocalPlayer : 0;
            _session.Applied += OnApplied;
            BuildLayout();
            if (config.Mode == BattleMode.Online) AttachOnline();
            AddLog($"対戦開始: {(State.CurrentPlayer == Me ? "あなた" : "相手")}が先攻");
            Render();
            Advance();
        }

        private void OnDestroy()
        {
            if (_session != null) _session.Applied -= OnApplied;
            DetachOnline();
        }

        // ==================================================================
        // オンライン(05-online.md): ホストが順序を決め、両端末で同じコマンド列を適用する
        // ==================================================================

        private Net.OnlineSession? _online;
        private readonly Queue<GameCommand> _incoming = new();
        private bool _processingIncoming;
        private bool _awaitingHostEcho;
        private float _turnDeadline = -1;
        private const float TurnSeconds = 90f;
        private bool IsOnline => _session.Config.Mode == BattleMode.Online;
        private bool IsOnlineHost => IsOnline && _session.Config.LocalPlayer == 0;

        private void AttachOnline()
        {
            _online = Net.OnlineSession.Instance;
            if (_online == null) return;
            _online.RequestFromGuest += OnGuestRequest;
            _online.CommandFromHost += OnHostCommand;
            _online.Rejected += OnRejected;
            _online.Disconnected += OnRemoteDisconnected;
        }

        private void DetachOnline()
        {
            if (_online == null) return;
            _online.RequestFromGuest -= OnGuestRequest;
            _online.CommandFromHost -= OnHostCommand;
            _online.Rejected -= OnRejected;
            _online.Disconnected -= OnRemoteDisconnected;
            _online = null;
        }

        /// <summary>ホスト: ゲストの要求を検証し、通れば適用して配信する。</summary>
        private void OnGuestRequest(GameCommand cmd)
        {
            if (!IsOnlineHost) return;
            if (cmd.Player != 1) { _online?.Reject("プレイヤー番号が不正"); return; }
            EnqueueIncoming(cmd);
        }

        /// <summary>ゲスト: ホストが確定したコマンド(自分の操作の echo を含む)。</summary>
        private void OnHostCommand(GameCommand cmd)
        {
            if (IsOnlineHost) return;
            if (cmd.Player == Me) _awaitingHostEcho = false;
            EnqueueIncoming(cmd);
        }

        private void OnRejected(string reason)
        {
            _awaitingHostEcho = false;
            AddLog($"<color=#E05050>ホストに拒否: {reason}</color>");
            Advance();
        }

        private void OnRemoteDisconnected()
        {
            if (State.IsFinished) return;
            AddLog("相手が切断しました");
            StopAllCoroutines();
            _aiRoutine = null;
            _playing = false;
            _processingIncoming = false;
            _incoming.Clear();
            // 切断 = 切断側の敗北(相手の降参として扱う。相手には届かないのでローカル適用のみ)
            _session.Submit(new SurrenderCommand(Enemy));
            Advance();
        }

        private void EnqueueIncoming(GameCommand cmd)
        {
            _incoming.Enqueue(cmd);
            if (!_processingIncoming) StartCoroutine(ProcessIncoming());
        }

        private IEnumerator ProcessIncoming()
        {
            _processingIncoming = true;
            while (_incoming.Count > 0)
            {
                // 自分の操作の再生中なら終わるまで待つ
                while (_playing) yield return null;
                var cmd = _incoming.Dequeue();
                var error = _session.Engine.Validate(cmd);
                if (error != null)
                {
                    if (IsOnlineHost && cmd.Player != Me) _online?.Reject(error);
                    else AddLog($"<color=#E05050>同期エラー: {error}</color>");
                    Advance();
                    continue;
                }
                var events = _session.Submit(cmd);
                if (IsOnlineHost) _online?.Broadcast(cmd);
                yield return Playback(events);
                Advance();
            }
            _processingIncoming = false;
        }

        private void Update()
        {
            if (_endTurnGlow != null && _endTurnHint)
            {
                float p = 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * 4.2f);
                _endTurnGlow.color = new Color(1f, 0.78f, 0.25f, p);
            }
            if (_session == null || !IsOnline || State.IsFinished || State.Phase != GamePhase.Main || _turnDeadline < 0) return;
            float remain = _turnDeadline - Time.unscaledTime;
            string whose = State.CurrentPlayer == Me ? "あなたの手番" : "相手の手番";
            _turnText.text = $"ターン {State.TurnNumber}\n{whose}\n<size=22>残り {Mathf.Max(0, Mathf.CeilToInt(remain))} 秒</size>";
            if (remain <= 0 && IsOnlineHost && !_playing && !_processingIncoming)
            {
                _turnDeadline = -1;
                // 制限時間切れ: ホストが手番プレイヤーのターン終了を発行する
                EnqueueIncoming(new EndTurnCommand(State.CurrentPlayer));
            }
        }

        // ==================================================================
        // レイアウト構築
        // ==================================================================

        // 配色と羊皮紙パネルは FantasyUi(共通部品)を使う
        private static readonly Color Ink = FantasyUi.Ink, InkDim = FantasyUi.InkDim, InkRed = FantasyUi.InkRed;
        private static readonly Color LeaderFrameColor = FantasyUi.FrameColor, MatColor = FantasyUi.MatColor, GoldTrim = FantasyUi.GoldTrim;
        private static Image ParchmentPanel(Transform parent, string name, float x, float y, float w, float h) => FantasyUi.ParchmentPanel(parent, name, x, y, w, h);

        private void BuildLayout()
        {
            // 背景(木の机 + 周辺減光)は AppRoot がキャンバス全体に敷く。ここでは透明にして机を見せる

            // 革のプレイマット: 場 2 列をまとめて覆う。金の縁取りと中央の紋章
            float matX = CenterX - 16, matY = MyBoardY - 12, matW = CenterW + 32, matH = (EnemyBoardY + BoardH - 20) - MyBoardY + 24;
            var matShadow = Ui.Panel(transform, "MatShadow", matX - 14, matY - 16, matW + 28, matH + 28, Color.white);
            matShadow.sprite = Materials.SoftShadow(); matShadow.type = Image.Type.Simple; matShadow.raycastTarget = false;
            var mat = UiTex.Panel(transform, "Mat", matX, matY, matW, matH, Materials.Leather(MatColor), Color.white, tiled: true, tilePx: 320);
            mat.raycastTarget = false;
            // 金の刺繍(二重線)
            void Trim(string n, float x, float y, float w, float h) { var l = Ui.Panel(transform, n, x, y, w, h, GoldTrim); l.raycastTarget = false; }
            Trim("MatT", matX + 8, matY + matH - 10, matW - 16, 2); Trim("MatB", matX + 8, matY + 8, matW - 16, 2);
            Trim("MatL", matX + 8, matY + 8, 2, matH - 16); Trim("MatR", matX + matW - 10, matY + 8, 2, matH - 16);
            var emblem = Ui.Rect(transform, "Emblem", CenterX + CenterW / 2 - 110, MyBoardY + (matH - 24) / 2 - 110, 220, 220).gameObject.AddComponent<Image>();
            emblem.sprite = Icons.Glyph(Icons.Kind.Shield); emblem.color = new Color(1f, 0.9f, 0.6f, 0.08f); emblem.raycastTarget = false;
            // 中央の仕切り線
            Trim("MatMid", matX + 60, EnemyBoardY - 6, matW - 120, 2);

            _enemyBoardBg = Ui.Panel(transform, "EnemyBoardBg", CenterX, EnemyBoardY, CenterW, BoardH - 20, new Color(0, 0, 0, 0.18f));
            AddDropTarget(_enemyBoardBg.gameObject, DropKind.EnemyBoard);
            _enemyBoardArea = Ui.Rect(transform, "EnemyBoard", CenterX, EnemyBoardY, CenterW, BoardH - 20);

            _myBoardBg = Ui.Panel(transform, "MyBoardBg", CenterX, MyBoardY, CenterW, BoardH, new Color(0, 0, 0, 0.18f));
            AddDropTarget(_myBoardBg.gameObject, DropKind.MyBoard);
            _myBoardArea = Ui.Rect(transform, "MyBoard", CenterX, MyBoardY, CenterW, BoardH);

            // 左: リーダー(羊皮紙)
            _enemyLeaderPanel = ParchmentPanel(transform, "EnemyLeader", LeftX, 800, ColW, 260);
            AddDropTarget(_enemyLeaderPanel.gameObject, DropKind.EnemyLeader);
            Ui.Label(_enemyLeaderPanel.transform, "Title", 0, 200, ColW, 50, "相手", 26, TextAnchor.MiddleCenter, InkDim, FontStyle.Bold);
            // クラスの肖像 + 体力の宝石 + PP の結晶(数値は出さない。オーナー判断 2026-09-23)
            MoveTitleToCorner(_enemyLeaderPanel.transform);
            _enemyPortrait = Ui.Rect(_enemyLeaderPanel.transform, "PortraitHolder", 0, 0, ColW, 260);
            _enemyHp = CardView.HsGem(_enemyLeaderPanel.transform, "Hp", ColW / 2 + 40, 86, 70, "gem_health", 30);
            (_enemyPpArea, _enemyPpRow) = BuildPpRow(_enemyLeaderPanel.transform, 40);
            _enemyHand = Ui.Label(_enemyLeaderPanel.transform, "Hand", 0, 6, ColW, 36, "", 20, TextAnchor.MiddleCenter, InkDim);

            var turnPanel = ParchmentPanel(transform, "TurnPanel", LeftX, 610, ColW, 170);
            turnPanel.raycastTarget = false;
            _turnText = Ui.FillLabel(turnPanel.transform, "Turn", "", 30, TextAnchor.MiddleCenter, Ink, FontStyle.Bold, 8);

            _myLeaderPanel = ParchmentPanel(transform, "MyLeader", LeftX, 330, ColW, 260);
            AddDropTarget(_myLeaderPanel.gameObject, DropKind.MyLeader);
            Ui.Label(_myLeaderPanel.transform, "Title", 0, 205, ColW, 50, "あなた", 26, TextAnchor.MiddleCenter, InkDim, FontStyle.Bold);
            MoveTitleToCorner(_myLeaderPanel.transform);
            _myPortrait = Ui.Rect(_myLeaderPanel.transform, "PortraitHolder", 0, 0, ColW, 260);
            _myHp = CardView.HsGem(_myLeaderPanel.transform, "Hp", ColW / 2 + 40, 86, 70, "gem_health", 30);
            (_myPpArea, _myPpRow) = BuildPpRow(_myLeaderPanel.transform, 40);
            _myDeck = Ui.Label(_myLeaderPanel.transform, "Deck", 0, 6, ColW, 36, "", 20, TextAnchor.MiddleCenter, InkDim);

            // 右: メニュー・ログ(羊皮紙の巻物)・ターン終了(蝋封)
            var menuBtn = Ui.Button(transform, "Menu", RightX, 1000, ColW, 60, "降参 / メニュー", ShowMenu, LeaderFrameColor, 24);
            UiTex.Fill(menuBtn.transform, "Paper", Materials.Parchment(), Color.white, tiled: true, tilePx: 256, margin: 3).raycastTarget = false;
            menuBtn.transform.Find("Label")!.SetAsLastSibling();
            menuBtn.GetComponentInChildren<Text>().color = Ink;
            _enemyDeck = Ui.Label(transform, "EnemyDeck", RightX, 940, ColW, 50, "", 22, TextAnchor.MiddleCenter, new Color(0.9f, 0.85f, 0.7f));
            var logPanel = ParchmentPanel(transform, "LogPanel", RightX, 450, ColW, 480);
            logPanel.raycastTarget = false;
            _logText = Ui.FillLabel(logPanel.transform, "Log", "", 21, TextAnchor.LowerLeft, InkDim, FontStyle.Normal, 12);

            _endTurn = Ui.Button(transform, "EndTurn", RightX, 330, ColW, 100, "", OnEndTurnClicked, LeaderFrameColor, 30);
            UiTex.Fill(_endTurn.transform, "Paper", Materials.Parchment(), Color.white, tiled: true, tilePx: 256, margin: 3).raycastTarget = false;
            var seal = Ui.Rect(_endTurn.transform, "Seal", 14, 14, 72, 72).gameObject.AddComponent<Image>();
            seal.sprite = Icons.Ring(new Color(0.70f, 0.14f, 0.12f), Icons.Kind.Shield); seal.raycastTarget = false;
            Ui.Label(_endTurn.transform, "Label", 90, 0, ColW - 100, 100, "ターン終了", 30, TextAnchor.MiddleCenter, Ink, FontStyle.Bold).raycastTarget = false;
            Destroy(_endTurn.transform.GetChild(0).gameObject); // Ui.Button が作った空ラベル

            // 下: 手札(机の手前を暗く落とす)
            var handBg = Ui.Panel(transform, "HandBg", 0, HandY, Ui.RefWidth, HandH, new Color(0, 0, 0, 0.55f));
            handBg.sprite = Icons.VerticalGradient(); handBg.type = Image.Type.Simple;
            handBg.raycastTarget = false;
            _handArea = Ui.Rect(transform, "Hand", 0, HandY, Ui.RefWidth, HandH);
            _hintText = Ui.Label(transform, "Hint", CenterX, 692, CenterW, 26, "", 24, TextAnchor.MiddleCenter, new Color(1f, 0.92f, 0.7f), FontStyle.Bold);

            _dragLayer = Ui.Fill(transform, "DragLayer");
            _fxLayer = Ui.Fill(transform, "FxLayer");          // エフェクトは手札より前、オーバーレイより後ろ
            _overlayLayer = Ui.Fill(transform, "OverlayLayer");
            _shakeRoot = (RectTransform)transform;             // 画面全体を揺らす
        }

        private DropTarget AddDropTarget(GameObject go, DropKind kind, int instanceId = -1)
        {
            var dt = go.AddComponent<DropTarget>();
            dt.Kind = kind;
            dt.InstanceId = instanceId;
            dt.Screen = this;
            return dt;
        }

        // ==================================================================
        // 描画
        // ==================================================================

        private void Render()
        {
            var me = State.PlayerOf(Me);
            var enemy = State.PlayerOf(Enemy);
            _fmt.Viewer = Me;
            HideTooltip();

            RenderLeaderTexts();
            _endTurn.interactable = _inputEnabled && !_playing && _pending == null && State.Phase == GamePhase.Main && State.CurrentPlayer == Me;
            // ターン終了以外にできることが無ければ、ボタンを金色に脈打たせて知らせる(04-screens.md)
            _endTurnHint = _endTurn.interactable && !_session.Engine.LegalCommands(Me).Any(c => c is not EndTurnCommand);
            if (_endTurnGlow == null)
            {
                _endTurnGlow = Ui.Rect(_endTurn.transform.parent, "EndTurnGlow", 0, 0, 10, 10).gameObject.AddComponent<Image>();
                var ert = (RectTransform)_endTurn.transform;
                var grt = _endTurnGlow.rectTransform;
                grt.anchoredPosition = ert.anchoredPosition - new Vector2(ert.sizeDelta.x * 0.10f, ert.sizeDelta.y * 0.35f);
                grt.sizeDelta = new Vector2(ert.sizeDelta.x * 1.20f, ert.sizeDelta.y * 1.70f);
                _endTurnGlow.sprite = Icons.GlowOutline(false);
                if (Fx.Additive != null) _endTurnGlow.material = Fx.Additive;
                _endTurnGlow.raycastTarget = false;
                _endTurnGlow.transform.SetSiblingIndex(_endTurn.transform.GetSiblingIndex() + 1);
            }
            if (!_endTurnHint) _endTurnGlow.color = Color.clear;

            ClearViews(_handViews);
            var hand = me.Hand;
            float spacing = Mathf.Min(CardView.HandSize.x + 12, (Ui.RefWidth - 40) / Mathf.Max(1, hand.Count));
            float startX = Ui.RefWidth / 2 - spacing * (hand.Count - 1) / 2;
            for (int i = 0; i < hand.Count; i++)
            {
                var view = CardView.Create(_handArea, hand[i].Definition, CardViewMode.Hand, null, i, hand[i].InstanceId);
                view.Screen = this;
                view.Rect.anchoredPosition = new Vector2(startX + i * spacing, HandH / 2);
                bool playable = _inputEnabled && !_playing && _pending == null && _session.Engine.CanPlay(Me, i) == null;
                view.Draggable = playable;
                view.SetFrame(playable ? Ui.Good : Ui.PanelDarkColor);
                _handViews.Add(view);
            }

            RenderBoard(_myBoardViews, _myBoardArea, me, isMine: true);
            RenderBoard(_enemyBoardViews, _enemyBoardArea, enemy, isMine: false);

            _hintText.text = "";
            if (_pending != null)
            {
                _hintText.text = "対象を選んでください(空いている場所をタップでキャンセル)";
                foreach (var t in _pending.ValidTargets) HighlightTarget(t, Ui.Bad);
            }

            _logText.text = string.Join("\n", _log.Skip(Mathf.Max(0, _log.Count - 9)));
        }

        /// <summary>PP の結晶を 10 個横に並べる(ハースストーンのマナ結晶)。戻り値の area は列全体(演出の基準)。</summary>
        private static (RectTransform area, Image[] row) BuildPpRow(Transform panel, float y)
        {
            var row = new Image[GameRules.PpMax];
            const float size = 24.5f, gap = 0.5f;   // 10 個で欄の幅いっぱい
            var area = Ui.Rect(panel, "PpRow", 10, y, row.Length * (size + gap), size);
            for (int i = 0; i < row.Length; i++)
            {
                var img = Ui.Rect(area, "Pp" + i, i * (size + gap), 0, size, size).gameObject.AddComponent<Image>();
                img.sprite = CardArt.Hs("gem_cost");
                img.raycastTarget = false;
                row[i] = img;
            }
            return (area, row);
        }

        /// <summary>使える = 明るい / 使った = 暗い / まだ無い = ほぼ透明 / 覚醒の刻で上限を超えた分 = 金色。</summary>
        private static void RefreshPpRow(Image[]? row, int pp, int maxPp)
        {
            if (row == null) return;
            for (int i = 0; i < row.Length; i++)
            {
                if (i < pp) row[i].color = i >= maxPp ? new Color(1f, 0.85f, 0.35f) : Color.white;
                else if (i < maxPp) row[i].color = new Color(0.35f, 0.35f, 0.42f, 0.85f);
                else row[i].color = new Color(1f, 1f, 1f, 0.12f);
            }
        }

        /// <summary>肖像を大きく置くため、欄の見出し(相手 / あなた)を左上の隅に寄せる。</summary>
        private static void MoveTitleToCorner(Transform panel)
        {
            var title = panel.Find("Title") as RectTransform;
            if (title == null) return;
            title.anchoredPosition = new Vector2(14, 214);
            title.sizeDelta = new Vector2(90, 40);
            var text = title.GetComponent<Text>();
            if (text != null) { text.alignment = TextAnchor.MiddleLeft; text.fontSize = 22; }
        }

        /// <summary>リーダーの肖像(プレイヤー player のデッキのクラス)。ローカル対戦で視点が替わったら作り直す。</summary>
        private void RefreshPortrait(RectTransform? holder, ref CardClass? built, int player)
        {
            if (holder == null) return;
            var cls = (player == 0 ? _session.Config.Deck0 : _session.Config.Deck1).Class;
            if (built == cls) return;
            built = cls;
            Ui.Clear(holder);
            CardView.LeaderPortrait(holder, cls, (ColW - 104) / 2f, 104, 104, 118);
        }

        private void RenderLeaderTexts()
        {
            var me = State.PlayerOf(Me);
            var enemy = State.PlayerOf(Enemy);
            _myHp.text = Mathf.Max(0, me.Hp).ToString();
            RefreshPpRow(_myPpRow, me.Pp, me.MaxPp);
            _myDeck.text = $"デッキ {me.Deck.Count} / 手札 {me.Hand.Count}";
            _enemyHp.text = Mathf.Max(0, enemy.Hp).ToString();
            RefreshPpRow(_enemyPpRow, enemy.Pp, enemy.MaxPp);
            _enemyHand.text = $"手札 {enemy.Hand.Count}";
            _enemyDeck.text = $"相手デッキ {enemy.Deck.Count}";
            var low = new Color(1f, 0.55f, 0.5f);
            _myHp.color = me.Hp <= 5 ? low : Color.white;
            _enemyHp.color = enemy.Hp <= 5 ? low : Color.white;
            RefreshPortrait(_myPortrait, ref _myPortraitClass, Me);
            RefreshPortrait(_enemyPortrait, ref _enemyPortraitClass, 1 - Me);

            string whose = State.IsFinished ? "決着" : State.Phase == GamePhase.Mulligan ? "マリガン" : State.CurrentPlayer == Me ? "あなたの手番" : "相手の手番";
            _turnText.text = $"ターン {State.TurnNumber}\n{whose}";
        }

        private void RenderBoard(List<CardView> views, RectTransform area, PlayerState owner, bool isMine)
        {
            ClearViews(views);
            var board = owner.Board;
            float spacing = CardView.BoardSize.x + 24;
            float startX = CenterW / 2 - spacing * (board.Count - 1) / 2;
            for (int i = 0; i < board.Count; i++)
            {
                var entity = board[i];
                var view = CardView.Create(area, entity.Definition, CardViewMode.Board, entity);
                view.Screen = this;
                view.Rect.anchoredPosition = new Vector2(startX + i * spacing, area.sizeDelta.y / 2);
                AddDropTarget(view.gameObject, isMine ? DropKind.MyEntity : DropKind.EnemyEntity, instanceId: entity.InstanceId);
                if (isMine)
                {
                    bool canAttack = _inputEnabled && !_playing && _pending == null && _session.Engine.CanAttack(entity) == null && _session.Engine.ValidAttackTargets(entity).Count > 0;
                    view.Draggable = canAttack;
                    view.SetFrame(canAttack ? Ui.Accent : Ui.PanelDarkColor);
                }
                else
                {
                    view.SetFrame(entity.HasKeyword(Keyword.Ward) ? new Color(0.6f, 0.6f, 0.7f) : Ui.PanelDarkColor);
                }
                views.Add(view);
            }
        }

        private static void ClearViews(List<CardView> views)
        {
            foreach (var v in views) if (v != null) Destroy(v.gameObject);
            views.Clear();
        }

        private CardView? FindView(int instanceId)
            => _myBoardViews.Concat(_enemyBoardViews).FirstOrDefault(v => v != null && v.Entity != null && v.Entity.InstanceId == instanceId);

        private CardView? FindHandView(int instanceId)
        {
            // 適用前に描画した手札ビューを、直前の手札の並びから逆引きする(CardView は InstanceId を持たないため名前で照合)
            foreach (var v in _handViews)
                if (v != null && v.HandInstanceId == instanceId) return v;
            return null;
        }

        private void HighlightTarget(TargetRef t, Color color)
        {
            if (t.IsLeader) Ui.SetBorder(t.PlayerIndex == Me ? _myLeaderPanel : _enemyLeaderPanel, color);
            else FindView(t.InstanceId)?.SetFrame(color);
        }

        private void ResetLeaderFrames()
        {
            Ui.SetBorder(_myLeaderPanel, LeaderFrameColor);
            Ui.SetBorder(_enemyLeaderPanel, LeaderFrameColor);
        }

        /// <summary>ドラッグ中のハイライトを、作り直さずに元の枠色へ戻す。</summary>
        private void ResetEntityFrames()
        {
            foreach (var v in _myBoardViews) if (v != null) v.SetFrame(v.Draggable ? Ui.Accent : Ui.PanelDarkColor);
            foreach (var v in _enemyBoardViews) if (v != null) v.SetFrame(v.Definition.HasKeyword(Keyword.Ward) ? new Color(0.6f, 0.6f, 0.7f) : Ui.PanelDarkColor);
        }

        private void AddLog(string line)
        {
            _log.Add(line);
            if (_log.Count > 200) _log.RemoveAt(0);
        }

        // ==================================================================
        // 進行
        // ==================================================================

        private void OnApplied(GameCommand cmd, IReadOnlyList<GameEvent> events)
        {
            foreach (var e in events)
            {
                var line = _fmt.Format(e);
                if (line != null) AddLog(line);
            }
        }

        /// <summary>人間の操作でコマンドを送る。</summary>
        private void Do(GameCommand cmd)
        {
            var error = _session.Engine.Validate(cmd);
            if (error != null)
            {
                AddLog($"<color=#E05050>{error}</color>");
                Render();
                return;
            }
            _inputEnabled = false;
            _pending = null;
            ResetLeaderFrames();

            if (IsOnline)
            {
                if (IsOnlineHost)
                {
                    // ホストは自分の操作をそのまま確定して配信する(ゲストの要求と同じ列に並べる)
                    EnqueueIncoming(cmd);
                }
                else
                {
                    // ゲストはホストの確定(echo)を待ってから適用する
                    _awaitingHostEcho = true;
                    Render();
                    _online?.SendRequest(cmd);
                }
                return;
            }

            var events = _session.Submit(cmd);
            StartCoroutine(AfterCommand(events));
        }

        private IEnumerator AfterCommand(IReadOnlyList<GameEvent> events)
        {
            yield return Playback(events);
            Advance();
        }

        /// <summary>状態を見て、次に誰が動くべきかに応じて UI を切り替える。</summary>
        private void Advance()
        {
            if (State.IsFinished)
            {
                _inputEnabled = false;
                Render();
                ShowResult();
                return;
            }
            int p = _session.PlayerToAct!.Value;
            if (IsOnline)
            {
                AdvanceOnline();
                return;
            }
            if (_session.IsAi(p))
            {
                _inputEnabled = false;
                Render();
                if (_aiRoutine == null) _aiRoutine = StartCoroutine(AiLoop());
                return;
            }
            if (_session.Config.Mode == BattleMode.LocalTwoPlayer && p != _viewer)
            {
                _inputEnabled = false;
                Render();
                ShowHandoff(p);
                return;
            }
            if (State.Phase == GamePhase.Mulligan)
            {
                _inputEnabled = false;
                Render();
                ShowMulligan(p);
                return;
            }
            _inputEnabled = true;
            Render();
        }

        /// <summary>オンライン: マリガンは両者同時、メインは手番側のみ操作可。ターン開始で制限時間をセット。</summary>
        private void AdvanceOnline()
        {
            if (State.Phase == GamePhase.Mulligan)
            {
                _inputEnabled = false;
                Render();
                bool mulliganOpen = _overlay != null && _overlay.name == "Mulligan";
                if (!State.PlayerOf(Me).MulliganDone && !_awaitingHostEcho) { if (!mulliganOpen) ShowMulligan(Me); }
                else if (_overlay == null) ShowWaiting("相手のマリガンを待っています…");
                return;
            }
            if (_overlay != null && _overlay.name == "Waiting") CloseOverlay();
            if (_turnDeadline < 0 || _lastTurnForDeadline != State.TurnNumber)
            {
                _turnDeadline = Time.unscaledTime + TurnSeconds;
                _lastTurnForDeadline = State.TurnNumber;
            }
            _inputEnabled = State.CurrentPlayer == Me && !_awaitingHostEcho;
            Render();
        }

        private int _lastTurnForDeadline = -1;

        private void LeaveToMenu()
        {
            Net.OnlineSession.Instance?.Leave();
            AppRoot.Instance.ShowMainMenu();
        }

        private void ShowWaiting(string text)
        {
            var ov = OpenOverlay(0.5f);
            ov.gameObject.name = "Waiting";
            Ui.Label(ov, "Text", 0, 500, Ui.RefWidth, 80, text, 36, TextAnchor.MiddleCenter, Ui.TextMain, FontStyle.Bold);
        }

        private IEnumerator AiLoop()
        {
            while (!State.IsFinished && _session.AiShouldAct)
            {
                yield return new WaitForSeconds(State.Phase == GamePhase.Mulligan ? 0.3f : 0.5f);
                var cmd = _session.AiDecide();
                var events = _session.Submit(cmd);
                yield return Playback(events);
                if (cmd is EndTurnCommand) yield return new WaitForSeconds(0.3f);
            }
            _aiRoutine = null;
            Advance();
        }

        // ==================================================================
        // イベント再生(簡易エフェクト)
        // ==================================================================

        /// <summary>
        /// 適用済みコマンドのイベント列を、適用前の見た目(現在のビュー)の上で順に再生し、最後に再描画する。
        /// 再生中は操作不可。
        /// </summary>
        private IEnumerator Playback(IReadOnlyList<GameEvent> events)
        {
            _playing = true;
            HideTooltip();
            var entered = new List<int>();
            var buffed = new List<int>();

            int i = 0;
            while (i < events.Count)
            {
                var e = events[i];
                switch (e)
                {
                    case TurnStartedEvent t:
                        RenderLeaderTexts();
                        Audio.Play(Audio.TurnStart, t.Player == Me ? 1f : 0.7f);
                        yield return Banner(t.Player == Me ? "あなたのターン" : "相手のターン", t.Player == Me ? Ui.Accent : Ui.TextMain);
                        break;

                    case BonusPpUsedEvent bp:
                        RenderLeaderTexts();
                        Audio.Play(Audio.Awakening);
                        yield return AwakeningEffect(bp.Player);
                        break;

                    case CardPlayedEvent p:
                        {
                            var hv = FindHandView(p.InstanceId);
                            var origin = CenterWorld(hv != null ? hv.Rect : (p.Player == Me ? _myLeaderPanel : _enemyLeaderPanel).rectTransform);
                            var playedDef = _db.Get(p.CardId);
                            Audio.Play(playedDef.IsSpell ? Audio.Spell : Audio.CardPlay);
                            if (hv != null) yield return FadeOut(hv.Group, 0.12f);
                            else if (p.Player != Me) yield return Banner($"相手: {playedDef.Name}", Ui.TextMain, 0.45f);
                            // 対象を取るカードは、発生源から対象へ光弾を飛ばす
                            if (p.Target.HasValue)
                            {
                                var trt = TargetRect(p.Target.Value);
                                if (trt != null)
                                {
                                    yield return Fx.Projectile(_fxLayer, origin, CenterWorld(trt), ClassAura(_db.Get(p.CardId).Class));
                                    _lastImpactAt = 0f;   // 着弾のダメージには衝撃を出す
                                }
                            }
                            break;
                        }

                    case EntityEnteredBoardEvent en:
                        entered.Add(en.InstanceId);
                        break;

                    case BuffedEvent b:
                        buffed.Add(b.InstanceId);
                        break;

                    case AttackDeclaredEvent a:
                        {
                            var attacker = FindView(a.AttackerInstanceId);
                            var targetRt = TargetRect(a.Target);
                            if (attacker != null && targetRt != null)
                            {
                                yield return Lunge(attacker.Rect, targetRt);
                                // 命中: 斬撃 + 衝撃波。リーダーへの攻撃は画面を揺らす
                                bool heavy = a.Target.IsLeader;
                                Audio.Play(heavy ? Audio.AttackLeader : Audio.Attack);
                                yield return Fx.Impact(_fxLayer, CenterWorld(targetRt), new Color(1f, 0.55f, 0.25f), heavy ? 1.45f : 1.05f, heavy);
                                if (heavy)
                                {
                                    CoroutineHost.Run(Fx.Shake(_shakeRoot, 16f, 0.3f));
                                    Fx.ScreenFlash(_fxLayer, new Color(1f, 0.5f, 0.3f), 0.28f);
                                }
                                else CoroutineHost.Run(Fx.Shake(_shakeRoot, 5f, 0.14f));
                                _lastImpactAt = Time.time;
                            }
                            break;
                        }

                    case DamageDealtEvent:
                    case HealedEvent:
                        {
                            // 連続するダメージ・回復はまとめて同時に再生する
                            int j = i;
                            bool healPlayed = false;
                            while (j < events.Count && (events[j] is DamageDealtEvent || events[j] is HealedEvent))
                            {
                                if (events[j] is DamageDealtEvent d)
                                {
                                    Float(d.Target, $"-{d.Amount}", Ui.Bad);
                                    StartCoroutine(Flash(d.Target, Ui.Bad));
                                    UpdateLeaderHp(d.Target, d.RemainingHealth);
                                    // 攻撃の命中演出は直前に出しているので、そこから離れたダメージ(効果ダメージ)だけ小さな衝撃を出す
                                    var drt = TargetRect(d.Target);
                                    if (drt != null && Time.time - _lastImpactAt > 0.25f)
                                        CoroutineHost.Run(Fx.Impact(_fxLayer, CenterWorld(drt), new Color(1f, 0.45f, 0.3f), 0.95f, d.Amount >= 4));
                                    if (d.Target.IsLeader && d.Amount >= 3) { CoroutineHost.Run(Fx.Shake(_shakeRoot, 12f, 0.25f)); Fx.ScreenFlash(_fxLayer, new Color(1f, 0.35f, 0.3f), 0.22f); }
                                }
                                else if (events[j] is HealedEvent h)
                                {
                                    Float(h.Target, $"+{h.Amount}", Ui.Good);
                                    StartCoroutine(Flash(h.Target, Ui.Good));
                                    UpdateLeaderHp(h.Target, h.ResultHealth);
                                    var hrt = TargetRect(h.Target);
                                    if (hrt != null) Fx.Heal(_fxLayer, CenterWorld(hrt), new Color(0.5f, 1f, 0.6f));
                                    if (!healPlayed) { Audio.Play(Audio.Heal); healPlayed = true; }
                                }
                                j++;
                            }
                            i = j - 1;
                            yield return new WaitForSeconds(0.28f);
                            break;
                        }

                    case EntityDestroyedEvent x:
                        {
                            var v = FindView(x.InstanceId);
                            if (v != null)
                            {
                                Fx.Destroyed(_fxLayer, CenterWorld(v.Rect), new Color(0.75f, 0.72f, 0.68f));
                                Audio.Play(Audio.Destroy, 0.9f);
                                yield return FadeOut(v.Group, 0.22f);
                            }
                            break;
                        }

                    case GameEndedEvent:
                        yield return new WaitForSeconds(0.3f);
                        break;
                }
                i++;
            }

            Render();
            bool summonPlayed = false, buffPlayed = false;
            foreach (var id in entered)
            {
                var v = FindView(id);
                if (v == null) continue;
                StartCoroutine(PopIn(v.Rect));
                Fx.Summon(_fxLayer, v.Rect, ClassAura(v.Definition.Class));
                if (!summonPlayed) { Audio.Play(Audio.Summon, 0.85f); summonPlayed = true; }
            }
            foreach (var id in buffed.Distinct())
            {
                var v = FindView(id);
                if (v == null) continue;
                StartCoroutine(Pulse(v.Rect));
                Fx.Buff(_fxLayer, v.Rect);
                if (!buffPlayed) { Audio.Play(Audio.Buff, 0.8f); buffPlayed = true; }
            }
            if (entered.Count > 0) yield return new WaitForSeconds(0.18f);
            _playing = false;
        }

        /// <summary>エフェクトの光の色(クラスの雰囲気に合わせる)。</summary>
        private static Color ClassAura(CardClass c) => c switch
        {
            CardClass.Knight => new Color(0.65f, 0.85f, 1f),
            CardClass.Mage => new Color(0.85f, 0.55f, 1f),
            CardClass.Necromancer => new Color(0.55f, 1f, 0.6f),
            CardClass.Druid => new Color(0.7f, 1f, 0.5f),
            CardClass.Dragon => new Color(1f, 0.6f, 0.3f),
            CardClass.Chaos => new Color(0.8f, 0.5f, 1f),
            _ => new Color(1f, 0.95f, 0.8f),
        };

        private RectTransform? TargetRect(TargetRef t)

        {
            if (t.IsLeader) return (t.PlayerIndex == Me ? _myLeaderPanel : _enemyLeaderPanel).rectTransform;
            return FindView(t.InstanceId)?.Rect;
        }

        private void UpdateLeaderHp(TargetRef t, int hp)
        {
            if (!t.IsLeader) return;
            var label = t.PlayerIndex == Me ? _myHp : _enemyHp;
            label.text = Mathf.Max(0, hp).ToString();
            label.color = hp <= 5 ? InkRed : Ink;
        }

        private static Vector3 CenterWorld(RectTransform rt) => rt.TransformPoint(rt.rect.center);

        /// <summary>攻撃側が対象へ突進して戻る。</summary>
        private static IEnumerator Lunge(RectTransform attacker, RectTransform target)
        {
            var start = attacker.position;
            var dest = Vector3.Lerp(start, CenterWorld(target), 0.75f);
            attacker.SetAsLastSibling();
            const float outDur = 0.12f, backDur = 0.14f;
            float t = 0;
            while (t < outDur && attacker != null) { t += Time.deltaTime; attacker.position = Vector3.Lerp(start, dest, Mathf.SmoothStep(0, 1, t / outDur)); yield return null; }
            t = 0;
            while (t < backDur && attacker != null) { t += Time.deltaTime; attacker.position = Vector3.Lerp(dest, start, Mathf.SmoothStep(0, 1, t / backDur)); yield return null; }
            if (attacker != null) attacker.position = start;
        }

        private IEnumerator Flash(TargetRef target, Color color)
        {
            Image? img = target.IsLeader ? (target.PlayerIndex == Me ? _myLeaderPanel : _enemyLeaderPanel) : null;
            CardView? view = target.IsLeader ? null : FindView(target.InstanceId);
            if (img == null && view == null) yield break;
            for (int n = 0; n < 2; n++)
            {
                if (img != null) img.color = color; else if (view != null) view.SetFrame(color);
                yield return new WaitForSeconds(0.07f);
                if (img != null) img.color = LeaderFrameColor; else if (view != null) view.SetFrame(Ui.PanelDarkColor);
                yield return new WaitForSeconds(0.05f);
            }
        }

        private static IEnumerator FadeOut(CanvasGroup group, float dur)
        {
            float t = 0;
            while (t < dur && group != null) { t += Time.deltaTime; group.alpha = 1 - t / dur; yield return null; }
            if (group != null) group.alpha = 0;
        }

        private static IEnumerator PopIn(RectTransform rt)
        {
            float t = 0;
            const float dur = 0.16f;
            while (t < dur && rt != null) { t += Time.deltaTime; float s = Mathf.SmoothStep(0.55f, 1f, t / dur); rt.localScale = new Vector3(s, s, 1); yield return null; }
            if (rt != null) rt.localScale = Vector3.one;
        }

        private static IEnumerator Pulse(RectTransform rt)
        {
            float t = 0;
            const float dur = 0.3f;
            while (t < dur && rt != null) { t += Time.deltaTime; float s = 1 + 0.12f * Mathf.Sin(Mathf.PI * t / dur); rt.localScale = new Vector3(s, s, 1); yield return null; }
            if (rt != null) rt.localScale = Vector3.one;
        }

        /// <summary>
        /// 「覚醒の刻」(後攻の追加 PP)の演出。01-rules.md「後攻の追加 PP」。
        /// PP 表示から金の光が立ち上がり、中央に紋章つきの帯が出る。
        /// </summary>
        private IEnumerator AwakeningEffect(int player)
        {
            bool mine = player == Me;
            var ppRt = mine ? _myPpArea : _enemyPpArea;

            // PP 表示を光らせる
            Fx.ScreenFlash(_fxLayer, FantasyUi.Gold, 0.18f);
            Fx.Buff(_fxLayer, ppRt);
            CoroutineHost.Run(Fx.Impact(_fxLayer, CenterWorld(ppRt), FantasyUi.Gold, 0.85f));

            // 中央の帯
            var panel = Ui.Panel(_fxLayer, "Awakening", CenterX, 470, CenterW, 130, new Color(0.06f, 0.05f, 0.03f, 0.88f));
            panel.raycastTarget = false;
            var group = panel.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            FantasyUi.GoldLine(panel.transform, "LineT", 20, 126, CenterW - 40, 3);
            FantasyUi.GoldLine(panel.transform, "LineB", 20, 1, CenterW - 40, 3);
            var emblem = Ui.Rect(panel.transform, "Emblem", CenterW / 2 - 300, 15, 100, 100).gameObject.AddComponent<Image>();
            emblem.sprite = Icons.Glyph(Icons.Kind.Gem);
            emblem.color = FantasyUi.Gold;
            emblem.raycastTarget = false;
            FantasyUi.GoldTitle(panel.transform, "Title", 0, 62, CenterW, 64, "覚 醒 の 刻", 52);
            Ui.Label(panel.transform, "Sub", 0, 18, CenterW, 44,
                mine ? "このターンだけ PP +1" : "相手のこのターンだけ PP +1", 26, TextAnchor.MiddleCenter, new Color(0.88f, 0.82f, 0.66f));

            // 帯の光の粒
            for (int n = 0; n < 10; n++)
            {
                var world = panel.rectTransform.TransformPoint(new Vector3(Random.Range(-CenterW / 2, CenterW / 2), -50, 0));
                Fx.Heal(_fxLayer, world, FantasyUi.Gold);
            }

            float t = 0;
            while (t < 0.15f) { t += Time.deltaTime; group.alpha = t / 0.15f; panel.rectTransform.localScale = new Vector3(Mathf.Lerp(1.12f, 1f, t / 0.15f), 1, 1); yield return null; }
            panel.rectTransform.localScale = Vector3.one;
            yield return new WaitForSeconds(0.85f);
            t = 0;
            while (t < 0.25f) { t += Time.deltaTime; group.alpha = 1 - t / 0.25f; yield return null; }
            Destroy(panel.gameObject);
        }

        private IEnumerator Banner(string text, Color color, float hold = 0.7f)

        {
            var panel = Ui.Panel(_dragLayer, "Banner", CenterX, 500, CenterW, 90, new Color(0, 0, 0, 0.7f));
            panel.raycastTarget = false;
            Ui.FillLabel(panel.transform, "Text", text, 48, TextAnchor.MiddleCenter, color, FontStyle.Bold);
            var group = panel.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            float t = 0;
            while (t < 0.12f) { t += Time.deltaTime; group.alpha = t / 0.12f; yield return null; }
            yield return new WaitForSeconds(hold);
            t = 0;
            while (t < 0.2f) { t += Time.deltaTime; group.alpha = 1 - t / 0.2f; yield return null; }
            Destroy(panel.gameObject);
        }

        private void Float(TargetRef target, string text, Color color)
        {
            var rt = TargetRect(target);
            if (rt == null) return;
            var world = CenterWorld(rt);
            var label = Ui.Label(_dragLayer, "Float", 0, 0, 140, 70, text, 44, TextAnchor.MiddleCenter, color, FontStyle.Bold);
            label.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            label.rectTransform.position = world;
            StartCoroutine(FloatRoutine(label));
        }

        private static IEnumerator FloatRoutine(Text label)
        {
            var rt = label.rectTransform;
            float t = 0;
            var start = rt.anchoredPosition;
            while (t < 0.9f && rt != null)
            {
                t += Time.deltaTime;
                rt.anchoredPosition = start + new Vector2(0, 90 * t);
                var c = label.color; c.a = 1 - Mathf.Clamp01((t - 0.4f) / 0.5f); label.color = c;
                yield return null;
            }
            if (label != null) Destroy(label.gameObject);
        }

        // ==================================================================
        // 入力: ドラッグ
        // ==================================================================

        public void OnCardDragBegin(CardView view, PointerEventData e)
        {
            if (view.Mode == CardViewMode.Hand)
            {
                Ui.SetBorder(_myBoardBg, BoardHighlight);
                var kind = view.Definition.PlaySelectTarget;
                if (kind.HasValue)
                    foreach (var t in _session.Engine.ValidSelectTargets(Me, kind.Value, null)) HighlightTarget(t, Ui.Bad);
                if (view.Definition.IsSpell) Ui.SetBorder(_enemyBoardBg, BoardHighlight);
            }
            else if (view.Entity != null)
            {
                foreach (var t in _session.Engine.ValidAttackTargets(view.Entity)) HighlightTarget(t, Ui.Bad);
            }
        }

        public void OnCardDragging(CardView view, PointerEventData e) { }

        public void OnCardDragEnd(CardView view, PointerEventData e)
        {
            Ui.SetBorder(_myBoardBg, new Color(0, 0, 0, 0.18f));
            Ui.SetBorder(_enemyBoardBg, new Color(0, 0, 0, 0.18f));
            ResetLeaderFrames();
            ResetEntityFrames();
            if (!_inputEnabled || _playing) { Render(); return; }

            var drop = FindDropTarget(e);
            if (drop == null) { Render(); return; }

            if (view.Mode == CardViewMode.Hand) HandleHandDrop(view, drop, e);
            else if (view.Entity != null) HandleAttackDrop(view.Entity, drop);
            else Render();
        }

        private DropTarget? FindDropTarget(PointerEventData e)
        {
            var go = e.pointerCurrentRaycast.gameObject;
            var dt = go != null ? go.GetComponentInParent<DropTarget>() : null;
            if (dt != null) return dt;
            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(e, results);
            foreach (var r in results)
            {
                dt = r.gameObject.GetComponentInParent<DropTarget>();
                if (dt != null) return dt;
            }
            return null;
        }

        private void HandleHandDrop(CardView view, DropTarget drop, PointerEventData e)
        {
            int handIndex = view.HandIndex;
            var def = view.Definition;
            if (_session.Engine.CanPlay(Me, handIndex) != null) { Render(); return; }

            var selectKind = def.PlaySelectTarget;
            var valid = selectKind.HasValue ? _session.Engine.ValidSelectTargets(Me, selectKind.Value, null).ToList() : new List<TargetRef>();
            int boardPos = BoardPositionFromPointer(e.position);

            var dropped = ToTargetRef(drop);
            if (dropped.HasValue && valid.Contains(dropped.Value))
            {
                Do(new PlayCardCommand(Me, handIndex, boardPos, dropped.Value));
                return;
            }

            bool onBoard = drop.Kind == DropKind.MyBoard || drop.Kind == DropKind.MyEntity
                           || (def.IsSpell && (drop.Kind == DropKind.EnemyBoard || drop.Kind == DropKind.EnemyEntity || drop.Kind == DropKind.EnemyLeader));
            if (!onBoard) { Render(); return; }

            if (valid.Count == 0)
            {
                Do(new PlayCardCommand(Me, handIndex, boardPos));
            }
            else
            {
                _pending = new PendingPlay { HandIndex = handIndex, BoardPosition = boardPos, ValidTargets = valid };
                Render();
            }
        }

        private void HandleAttackDrop(BoardEntity attacker, DropTarget drop)
        {
            var target = ToTargetRef(drop);
            if (target.HasValue && _session.Engine.ValidAttackTargets(attacker).Contains(target.Value))
                Do(new AttackCommand(Me, attacker.InstanceId, target.Value));
            else
                Render();
        }

        private TargetRef? ToTargetRef(DropTarget drop) => drop.Kind switch
        {
            DropKind.MyLeader => TargetRef.Leader(Me),
            DropKind.EnemyLeader => TargetRef.Leader(Enemy),
            DropKind.MyEntity => TargetRef.Entity(drop.InstanceId),
            DropKind.EnemyEntity => TargetRef.Entity(drop.InstanceId),
            _ => null,
        };

        private int BoardPositionFromPointer(Vector2 screenPos)
        {
            int pos = 0;
            foreach (var v in _myBoardViews)
                if (v.Rect.position.x < screenPos.x) pos++;
            return pos;
        }

        // ==================================================================
        // 入力: クリック・ホバー
        // ==================================================================

        public void OnTargetClicked(DropTarget dt, PointerEventData e)
        {
            if (_pending == null || _playing) return;
            var target = ToTargetRef(dt);
            if (target.HasValue && _pending.ValidTargets.Contains(target.Value))
            {
                var p = _pending;
                Do(new PlayCardCommand(Me, p.HandIndex, p.BoardPosition, target.Value));
            }
            else if (dt.Kind == DropKind.MyBoard || dt.Kind == DropKind.EnemyBoard)
            {
                _pending = null;
                ResetLeaderFrames();
                Render();
            }
        }

        public void OnCardClicked(CardView view, PointerEventData e)
        {
            if (_pending != null || _playing) return;
            ShowDetail(view.Definition, view.Entity);
        }

        /// <summary>ホバー(PC)/長押し(スマホ)でキーワード説明のツールチップ。</summary>
        public void OnCardHover(CardView view, bool entered)
        {
            if (_tooltipRoutine != null) { StopCoroutine(_tooltipRoutine); _tooltipRoutine = null; }
            if (!entered || _playing || _overlay != null) { HideTooltip(); return; }
            var text = KeywordHelp.ToRichText(view.Definition);
            if (string.IsNullOrEmpty(text)) { HideTooltip(); return; }
            _tooltipRoutine = StartCoroutine(ShowTooltipDelayed(view, text));
        }

        private IEnumerator ShowTooltipDelayed(CardView view, string text)
        {
            yield return new WaitForSeconds(0.4f);
            if (view == null) yield break;
            HideTooltip();
            const float w = 380;
            float h = 30 + 62 * KeywordHelp.ForCard(view.Definition).Count;
            var panel = Ui.Panel(_dragLayer, "Tooltip", 0, 0, w, h, new Color(0.05f, 0.05f, 0.07f, 0.95f));
            panel.raycastTarget = false;
            Ui.FillLabel(panel.transform, "Text", text, 20, TextAnchor.UpperLeft, Ui.TextMain, FontStyle.Normal, 12);

            // カードの右横に出し、画面外に出ないように調整
            // InverseTransformPoint はレイヤーの pivot(中央)基準なので、左下原点に直す
            Vector2 local = _dragLayer.InverseTransformPoint(CenterWorld(view.Rect));
            local += _dragLayer.rect.size / 2;
            float half = view.Rect.rect.width / 2;
            float x = local.x + half + 12;
            if (x + w > Ui.RefWidth - 10) x = local.x - half - 12 - w;
            float y = Mathf.Clamp(local.y - h / 2, 10, Ui.RefHeight - h - 10);
            panel.rectTransform.anchoredPosition = new Vector2(x, y);
            _tooltip = panel.gameObject;
        }

        private void HideTooltip()
        {
            if (_tooltip != null) Destroy(_tooltip);
            _tooltip = null;
        }

        private void OnEndTurnClicked()
        {
            if (!_inputEnabled || _playing || _pending != null) return;
            Do(new EndTurnCommand(Me));
        }

        // ==================================================================
        // オーバーレイ
        // ==================================================================

        private RectTransform OpenOverlay(float alpha = 0.75f)
        {
            CloseOverlay();
            HideTooltip();
            _overlay = Ui.Overlay(_overlayLayer, "Overlay", alpha).gameObject;
            return _overlay.GetComponent<RectTransform>();
        }

        private void CloseOverlay()
        {
            if (_overlay != null) Destroy(_overlay);
            _overlay = null;
        }

        private void ShowDetail(CardDefinition def, BoardEntity? entity)
        {
            var ov = OpenOverlay(0.6f);
            ov.gameObject.AddComponent<Button>().onClick.AddListener(CloseOverlay);
            CardDetailPopup.Build(ov, def, entity, CloseOverlay);
        }

        /// <summary>開発用: 1 枚のカードを拡大 / 手札 / 場 の 3 サイズで並べて表示する(イラスト確認)。</summary>
        internal void ShowCardPreview(CardDefinition def)
        {
            var ov = OpenOverlay(0.85f);
            var big = CardView.Create(ov, def, CardViewMode.Detail);
            big.Rect.anchoredPosition = new Vector2(560, Ui.RefHeight / 2);
            var hand = CardView.Create(ov, def, CardViewMode.Hand);
            hand.Rect.anchoredPosition = new Vector2(1060, Ui.RefHeight / 2 + 60);
            var board = CardView.Create(ov, def, CardViewMode.Board);
            board.Rect.anchoredPosition = new Vector2(1360, Ui.RefHeight / 2 + 60);
            Ui.Label(ov, "Ids", 900, 200, 700, 60, $"{def.Id} {def.Name}  (拡大 / 手札 / 場)", 32, TextAnchor.MiddleCenter, Ui.TextDim);
        }

        /// <summary>開発用: 各エフェクトを並べて出す(-preview-fx)。</summary>
        internal void ShowFxPreview(int step)
        {
            var layer = _fxLayer;
            Vector3 At(float x, float y) => layer.TransformPoint(new Vector3(x - Ui.RefWidth / 2, y - Ui.RefHeight / 2, 0));
            switch (step)
            {
                case 0:
                    Ui.Label(layer, "L1", 260, 820, 300, 40, "攻撃の命中", 30, TextAnchor.MiddleCenter, Ui.Accent, FontStyle.Bold);
                    Ui.Label(layer, "L2", 660, 820, 300, 40, "スペルの着弾", 30, TextAnchor.MiddleCenter, Ui.Accent, FontStyle.Bold);
                    Ui.Label(layer, "L3", 1060, 820, 300, 40, "破壊", 30, TextAnchor.MiddleCenter, Ui.Accent, FontStyle.Bold);
                    Ui.Label(layer, "L4", 1460, 820, 300, 40, "回復", 30, TextAnchor.MiddleCenter, Ui.Accent, FontStyle.Bold);
                    CoroutineHost.Run(Fx.Impact(layer, At(410, 620), new Color(1f, 0.55f, 0.25f), 1.3f));
                    CoroutineHost.Run(Fx.Impact(layer, At(810, 620), new Color(0.85f, 0.55f, 1f), 0.9f));
                    Fx.Destroyed(layer, At(1210, 620), new Color(0.75f, 0.72f, 0.68f));
                    Fx.Heal(layer, At(1610, 620), new Color(0.5f, 1f, 0.6f));
                    break;
                case 1:
                    // 召喚と強化は「カードの矩形」が要るので、場の 1 体目を使う
                    if (_myBoardViews.Count > 0)
                    {
                        Fx.Summon(layer, _myBoardViews[0].Rect, ClassAura(_myBoardViews[0].Definition.Class));
                        Ui.Label(layer, "L5", 260, 820, 400, 40, "召喚(光柱と輪)", 30, TextAnchor.MiddleCenter, Ui.Accent, FontStyle.Bold);
                    }
                    if (_myBoardViews.Count > 1)
                    {
                        Fx.Buff(layer, _myBoardViews[1].Rect);
                        Ui.Label(layer, "L6", 700, 820, 400, 40, "強化(金の輪)", 30, TextAnchor.MiddleCenter, Ui.Accent, FontStyle.Bold);
                    }
                    break;
                case 2:
                    CoroutineHost.Run(Fx.Projectile(layer, At(300, 300), At(1600, 700), new Color(1f, 0.6f, 0.3f)));
                    Ui.Label(layer, "L7", 660, 820, 600, 40, "光弾(発生源 → 対象)", 30, TextAnchor.MiddleCenter, Ui.Accent, FontStyle.Bold);
                    break;
            }
        }

        private void ShowMulligan(int player)
        {
            var ov = OpenOverlay(0.85f);
            ov.gameObject.name = "Mulligan";
            var hand = State.PlayerOf(player).Hand;
            var marked = new HashSet<int>();
            Ui.Label(ov, "Title", 0, 900, Ui.RefWidth, 80, "マリガン: 交換するカードをタップ", 40, TextAnchor.MiddleCenter, Ui.Accent, FontStyle.Bold);
            float spacing = CardView.DetailSize.x * 0.5f + 30;
            float startX = Ui.RefWidth / 2 - spacing * (hand.Count - 1) / 2;
            for (int i = 0; i < hand.Count; i++)
            {
                int idx = i;
                var view = CardView.Create(ov, hand[i].Definition, CardViewMode.Detail);
                view.Rect.localScale = Vector3.one * 0.5f;
                view.Rect.anchoredPosition = new Vector2(startX + i * spacing, 560);
                view.Draggable = false;
                view.gameObject.AddComponent<Button>().onClick.AddListener(() =>
                {
                    if (!marked.Add(idx)) marked.Remove(idx);
                    view.SetFrame(marked.Contains(idx) ? Ui.Bad : Ui.PanelDarkColor);
                });
            }
            Ui.Button(ov, "Confirm", Ui.RefWidth / 2 - 200, 120, 400, 90, "決定", () =>
            {
                CloseOverlay();
                Do(new MulliganCommand(player, marked));
            }, Ui.Accent, 34).GetComponentInChildren<Text>().color = Color.black;
        }

        private void ShowHandoff(int player)
        {
            var ov = OpenOverlay(0.95f);
            Ui.Label(ov, "Title", 0, 560, Ui.RefWidth, 100, $"プレイヤー {player + 1} の番です", 48, TextAnchor.MiddleCenter, Ui.Accent, FontStyle.Bold);
            Ui.Label(ov, "Sub", 0, 480, Ui.RefWidth, 60, "端末を渡してから OK を押してください", 28, TextAnchor.MiddleCenter, Ui.TextDim);
            Ui.Button(ov, "Ok", Ui.RefWidth / 2 - 150, 340, 300, 90, "OK", () =>
            {
                CloseOverlay();
                _viewer = player;
                _log.Clear();
                Advance();
            }, Ui.PanelColor, 34);
        }

        private void ShowResult()
        {
            var ov = OpenOverlay(0.8f);
            string title = State.Winner == null ? "引き分け" : State.Winner == Me ? "勝利!" : "敗北…";
            if (State.Winner != null) Audio.Play(State.Winner == Me ? Audio.Win : Audio.Lose, 1f, 0f);
            if (_session.Config.Mode == BattleMode.AiVersusAi && State.Winner != null) title = $"P{State.Winner + 1} の勝利";
            Ui.Label(ov, "Title", 0, 600, Ui.RefWidth, 120, title, 72, TextAnchor.MiddleCenter, State.Winner == Me ? Ui.Accent : Ui.TextMain, FontStyle.Bold);
            Ui.Label(ov, "Sub", 0, 520, Ui.RefWidth, 60, $"ターン {State.TurnNumber} / あなた {Mathf.Max(0, State.PlayerOf(Me).Hp)} HP - 相手 {Mathf.Max(0, State.PlayerOf(Enemy).Hp)} HP", 28, TextAnchor.MiddleCenter, Ui.TextDim);
            if (!IsOnline) Ui.Button(ov, "Again", Ui.RefWidth / 2 - 420, 360, 400, 90, "もう一度", () =>
            {
                var cfg = _session.Config;
                cfg.Seed = (ulong)System.DateTime.Now.Ticks;
                AppRoot.Instance.StartBattle(cfg);
            }, Ui.PanelColor, 32);
            Ui.Button(ov, "Menu", Ui.RefWidth / 2 + 20, 360, 400, 90, "メニューへ", LeaveToMenu, Ui.PanelColor, 32);
        }

        /// <summary>対戦中に設定(音量など)を開く。閉じるとメニューに戻る。</summary>
        private void ShowSettingsOverlay()
        {
            CloseOverlay();
            var ov = OpenOverlay(0.85f);
            SettingsPanel.Build(ov, (Ui.RefWidth - 900) / 2, 280, 900, 520);
            Ui.Button(ov, "Close", Ui.RefWidth / 2 - 200, 120, 400, 90, "閉じる", () => { CloseOverlay(); ShowMenu(); }, Ui.PanelColor, 32);
        }

        private void ShowMenu()
        {
            if (_overlay != null) return;
            var ov = OpenOverlay(0.8f);
            Ui.Label(ov, "Title", 0, 620, Ui.RefWidth, 80, "メニュー", 44, TextAnchor.MiddleCenter, Ui.TextMain, FontStyle.Bold);
            Ui.Button(ov, "Resume", Ui.RefWidth / 2 - 200, 480, 400, 90, "対戦に戻る", CloseOverlay, Ui.PanelColor, 32);
            Ui.Button(ov, "Settings", Ui.RefWidth / 2 + 220, 480, 300, 90, "設定", ShowSettingsOverlay, Ui.PanelDarkColor, 28);
            if (!State.IsFinished && _session.IsHuman(Me))
            {
                Ui.Button(ov, "Surrender", Ui.RefWidth / 2 - 200, 360, 400, 90, "降参する", () =>
                {
                    CloseOverlay();
                    if (IsOnline) { _inputEnabled = true; Do(new SurrenderCommand(Me)); return; }
                    StopAllCoroutines();
                    _aiRoutine = null;
                    _playing = false;
                    _session.Submit(new SurrenderCommand(Me));
                    Advance();
                }, Ui.Bad, 32);
            }
            Ui.Button(ov, "Quit", Ui.RefWidth / 2 - 200, 240, 400, 90, "メニューに戻る(対戦を破棄)", LeaveToMenu, Ui.PanelDarkColor, 28);
        }
    }
}
