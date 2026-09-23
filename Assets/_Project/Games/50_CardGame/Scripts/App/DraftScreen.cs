#nullable enable
using System.Linq;
using CardGame.Core.Definitions;
using CardGame.Core.Draft;
using CardGame.Unity.Battle;
using CardGame.Unity.Net;
using CardGame.Unity.UI;
using UnityEngine;
using UnityEngine.UI;

namespace CardGame.Unity.App
{
    /// <summary>
    /// ドラフト画面とデッキ確認画面(07-draft.md、04-screens.md「ドラフト」)。
    /// 3 枚 1 組 × 3 組から 1 組を取るのを 10 回(1 回 30 秒)。完成したら確認画面から対戦(AI)へ。
    /// オンライン(案 2)は部屋で接続してから始まり、2 人が同じシード = 同じ組でドラフトし、両者のデッキが揃ったら対戦。
    /// </summary>
    public sealed class DraftScreen : MonoBehaviour
    {
        private const float PickCardScale = 0.90f;     // ドラフト中のカード(手札サイズ基準)。カードの枠の外側は余白なので、間を詰めて(cardGap < 0)3 組を画面幅 1920 に収める
        private const float ConfirmCardScale = 0.56f;  // 確認画面のカード
        private static readonly string[] Letters = { "A", "B", "C" };
        private static readonly (CostBand band, string name, int goal)[] Bands =
        {
            (CostBand.Low, "低 1〜2", 9), (CostBand.Mid, "中 3〜4", 11), (CostBand.High, "高 5〜6", 6), (CostBand.Top, "大型 7〜", 4),
        };
        private static readonly Color TimerRed = new Color(0.9f, 0.25f, 0.2f);
        private static readonly Color Parchment = new Color(0.85f, 0.78f, 0.62f);

        private BattleMode _mode;
        private DraftSession _session = null!;
        private ulong _seed;
        private RectTransform _body = null!;
        private RectTransform _overlayLayer = null!;
        private GameObject? _overlay;

        // 制限時間
        private bool _picking;
        private float _remaining;
        private Image? _timerFill;
        private Text? _timerText;
        private float _timerW;
        private GameObject? _toast;

        // オンライン(案 2)
        private OnlineSession? _online;
        private int _remotePicks;
        private Text? _remoteText;
        private Text? _waitText;
        private bool _submitted;
        private bool _battleStarted;

        public void Begin(BattleMode mode)
        {
            _mode = mode;
            _body = Ui.Fill(transform, "Body");
            _overlayLayer = Ui.Fill(transform, "OverlayLayer");
            StartDraft();
        }

        /// <summary>オンライン: ロビーで接続済み。ホストが決めたシードで 2 人同時にドラフトする。</summary>
        public void BeginOnline(ulong seed)
        {
            _mode = BattleMode.Online;
            _online = OnlineSession.Instance;
            if (_online != null)
            {
                _online.RemoteDraftProgress += OnRemoteProgress;
                _online.MatchStarted += OnMatchStarted;
                _online.Disconnected += OnDisconnected;
            }
            _body = Ui.Fill(transform, "Body");
            _overlayLayer = Ui.Fill(transform, "OverlayLayer");
            _seed = seed;
            _session = new DraftSession(AppRoot.Instance.Db, _seed);
            ShowPick();
        }

        private bool IsOnline => _online != null;

        private void OnDestroy()
        {
            if (_online == null) return;
            _online.RemoteDraftProgress -= OnRemoteProgress;
            _online.MatchStarted -= OnMatchStarted;
            _online.Disconnected -= OnDisconnected;
        }

        private void StartDraft()
        {
            _seed = (ulong)System.DateTime.Now.Ticks;
            _session = new DraftSession(AppRoot.Instance.Db, _seed);
            ShowPick();
        }

        // ------------------------------------------------------------------
        // ドラフト中
        // ------------------------------------------------------------------

        private void ShowPick()
        {
            Ui.Clear(_body);
            CloseOverlay();
            FantasyUi.GoldTitle(_body, "Title", 0, 975, Ui.RefWidth, 90, $"ドラフト  {_session.PickIndex + 1} / {DraftSession.TotalPicks}", 52);
            Ui.Label(_body, "Hint", 0, 928, Ui.RefWidth, 40, "3 枚 1 組を 1 つ選んでください。カードを押すと詳しく見られます", 22, TextAnchor.MiddleCenter, Parchment).raycastTarget = false;

            // 制限時間のバー
            _timerW = 900;
            float tx = (Ui.RefWidth - _timerW) / 2, ty = 900;
            Ui.Panel(_body, "TimerBg", tx, ty, _timerW, 16, new Color(0f, 0f, 0f, 0.55f)).raycastTarget = false;
            _timerFill = Ui.Panel(_body, "TimerFill", tx, ty, _timerW, 16, FantasyUi.Gold);
            _timerFill.raycastTarget = false;
            _timerText = Ui.Label(_body, "TimerText", tx + _timerW + 16, ty - 12, 200, 40, "", 26, TextAnchor.MiddleLeft, FantasyUi.Gold, FontStyle.Bold);
            if (IsOnline)
                _remoteText = Ui.Label(_body, "Remote", Ui.RefWidth - 360, 975, 320, 60, "", 30, TextAnchor.MiddleRight, Parchment, FontStyle.Bold);
            RefreshRemote();

            // 3 組
            float cardW = CardView.HandSize.x * PickCardScale, cardH = CardView.HandSize.y * PickCardScale;
            const float pad = 14, cardGap = -20, panelGap = 20, panelY = 360, panelH = 480;
            float panelW = pad * 2 + cardW * 3 + cardGap * 2;
            float startX = (Ui.RefWidth - (panelW * 3 + panelGap * 2)) / 2;
            for (int o = 0; o < _session.CurrentOffer.Count; o++)
            {
                int idx = o;
                var bundle = _session.CurrentOffer[o];
                var panel = FantasyUi.ParchmentPanel(_body, "Bundle" + o, startX + o * (panelW + panelGap), panelY, panelW, panelH);
                string classes = string.Join(" ・ ", bundle.Select(c => CardView.ClassLabel(c.Class)));
                Ui.Label(panel.transform, "Head", pad, panelH - 62, panelW - pad * 2, 48, $"{Letters[o]}   <size=22>{classes}</size>", 34, TextAnchor.MiddleCenter, FantasyUi.Ink, FontStyle.Bold).raycastTarget = false;

                for (int i = 0; i < bundle.Count; i++)
                {
                    var def = bundle[i];
                    float cx = pad + i * (cardW + cardGap);
                    var cell = Ui.Panel(panel.transform, "Cell" + i, cx, 116, cardW, cardH, Color.clear);
                    cell.gameObject.AddComponent<Button>().onClick.AddListener(() => ShowDetail(def));
                    var view = CardView.Create(cell.transform, def, CardViewMode.Hand);
                    view.Rect.localScale = Vector3.one * PickCardScale;
                    view.Rect.anchoredPosition = new Vector2(cardW / 2, cardH / 2);
                    view.Draggable = false;
                    view.Group.blocksRaycasts = false;
                }
                FantasyUi.ParchmentButton(panel.transform, "Take", pad, 22, panelW - pad * 2, 76, "この組を取る", () => Take(idx), 30, emphasized: true);
            }

            BuildCurve(_body, 150);
            FantasyUi.ParchmentButton(_body, "Back", 40, 40, 260, 76, IsOnline ? "やめる(退出)" : "← やめる", Quit, 26);

            _remaining = DraftSession.SecondsPerPick;
            _picking = true;
            UpdateTimer();
        }

        /// <summary>取ったカードのコスト分布(帯ごとの棒)と枚数。</summary>
        private void BuildCurve(Transform parent, float y)
        {
            var db = AppRoot.Instance.Db;
            var counts = _session.Picked.GroupBy(id => DraftSession.BandOf(db.Get(id).Cost)).ToDictionary(g => g.Key, g => g.Count());
            const float bw = 250, gap = 36, barH = 18;
            float total = Bands.Length * bw + (Bands.Length - 1) * gap;
            float x0 = (Ui.RefWidth - total) / 2 - 140;
            for (int i = 0; i < Bands.Length; i++)
            {
                var (band, name, goal) = Bands[i];
                int n = counts.TryGetValue(band, out var c) ? c : 0;
                float x = x0 + i * (bw + gap);
                Ui.Label(parent, "Band" + i, x, y + 26, bw, 34, $"{name}   {n} / {goal}", 22, TextAnchor.MiddleLeft, Parchment).raycastTarget = false;
                Ui.Panel(parent, "BandBg" + i, x, y, bw, barH, new Color(0f, 0f, 0f, 0.5f)).raycastTarget = false;
                if (n > 0) Ui.Panel(parent, "BandFill" + i, x, y, bw * Mathf.Min(1f, (float)n / goal), barH, FantasyUi.GoldTrim).raycastTarget = false;
            }
            Ui.Label(parent, "Count", x0 + total + 40, y - 6, 320, 60, $"{_session.Picked.Count} / {DeckDefinition.DraftDeckSize} 枚", 34, TextAnchor.MiddleLeft, FantasyUi.Gold, FontStyle.Bold).raycastTarget = false;
        }

        private void Take(int index)
        {
            if (!_picking) return;
            _picking = false;
            Audio.Play(Audio.CardDraw, 0.9f);
            _session.Pick(index);
            _online?.SendDraftProgress(_session.PickIndex);
            Next();
        }

        private void Next()
        {
            if (_session.IsComplete) ShowConfirm();
            else ShowPick();
        }

        private void Update()
        {
            if (!_picking) return;
            _remaining -= Time.unscaledDeltaTime;
            UpdateTimer();
            if (_remaining > 0) return;

            // 時間切れ: ランダムに 1 組を取る
            _picking = false;
            int taken = _session.PickRandom();
            _online?.SendDraftProgress(_session.PickIndex);
            Audio.Play(Audio.CardDraw, 0.9f);
            Toast($"時間切れ — {Letters[taken]} の組を取りました");
            Next();
        }

        private void UpdateTimer()
        {
            if (_timerFill == null || _timerText == null) return;
            float r = Mathf.Clamp01(_remaining / DraftSession.SecondsPerPick);
            _timerFill.rectTransform.sizeDelta = new Vector2(_timerW * r, _timerFill.rectTransform.sizeDelta.y);
            bool hurry = _remaining <= 5f;
            _timerFill.color = hurry ? TimerRed : FantasyUi.Gold;
            _timerText.color = hurry ? TimerRed : FantasyUi.Gold;
            _timerText.text = $"{Mathf.CeilToInt(Mathf.Max(0, _remaining))} 秒";
        }

        private void Toast(string text)
        {
            if (_toast != null) Destroy(_toast);
            // 組と分布の間の空き(ドラフト中のみ。確認画面に移ったら消す)
            var label = Ui.Label(transform, "Toast", 0, 270, Ui.RefWidth, 60, text, 32, TextAnchor.MiddleCenter, TimerRed, FontStyle.Bold);
            label.raycastTarget = false;
            _toast = label.gameObject;
            Destroy(_toast, 2.5f);
        }

        // ------------------------------------------------------------------
        // デッキ確認
        // ------------------------------------------------------------------

        private void ShowConfirm()
        {
            Ui.Clear(_body);
            if (_toast != null) Destroy(_toast);
            CloseOverlay();
            _picking = false;
            var app = AppRoot.Instance;
            var deck = _session.ToDeck();

            FantasyUi.GoldTitle(_body, "Title", 0, 975, Ui.RefWidth, 90, "デッキ完成", 56);
            var emblem = CardArt.Emblem(CardClass.Chaos);
            if (emblem != null)
            {
                var em = Ui.Rect(_body, "Emblem", Ui.RefWidth / 2 - 330, 968, 104, 104).gameObject.AddComponent<Image>();
                em.sprite = emblem; em.raycastTarget = false;
            }
            Ui.Label(_body, "Sub", 0, 928, Ui.RefWidth, 40, "カオスのデッキ(30 枚)。カードを押すと詳しく見られます", 22, TextAnchor.MiddleCenter, Parchment).raycastTarget = false;

            var cards = deck.CardIds.Select(id => app.Db.Get(id)).OrderBy(c => c.Cost).ThenBy(c => c.Id, System.StringComparer.Ordinal).ToList();
            const int cols = 10;
            float cw = CardView.HandSize.x * ConfirmCardScale, ch = CardView.HandSize.y * ConfirmCardScale;
            const float gx = 14, gy = 18;
            float startX = (Ui.RefWidth - (cols * cw + (cols - 1) * gx)) / 2;
            float topY = 900;
            for (int i = 0; i < cards.Count; i++)
            {
                var def = cards[i];
                int col = i % cols, row = i / cols;
                float x = startX + col * (cw + gx), y = topY - (row + 1) * ch - row * gy;
                var cell = Ui.Panel(_body, "Cell" + i, x, y, cw, ch, Color.clear);
                cell.gameObject.AddComponent<Button>().onClick.AddListener(() => ShowDetail(def));
                var view = CardView.Create(cell.transform, def, CardViewMode.Hand);
                view.Rect.localScale = Vector3.one * ConfirmCardScale;
                view.Rect.anchoredPosition = new Vector2(cw / 2, ch / 2);
                view.Draggable = false;
                view.Group.blocksRaycasts = false;
            }

            BuildCurve(_body, 190);
            if (IsOnline)
            {
                // オンライン: デッキを自動で送り、相手を待つ(作り直しは無し)
                FantasyUi.ParchmentButton(_body, "Back", 40, 40, 300, 76, "やめる(退出)", Quit, 26);
                _remoteText = Ui.Label(_body, "Remote", Ui.RefWidth - 360, 975, 320, 60, "", 30, TextAnchor.MiddleRight, Parchment, FontStyle.Bold);
                _waitText = Ui.Label(_body, "Wait", Ui.RefWidth - 40 - 900, 40, 900, 76, "", 30, TextAnchor.MiddleRight, FantasyUi.Gold, FontStyle.Bold);
                RefreshRemote();
                if (!_submitted)
                {
                    _submitted = true;
                    _online!.SubmitDraftDeck(DeckCodec.Encode(deck));
                }
                return;
            }
            FantasyUi.ParchmentButton(_body, "Back", 40, 40, 300, 76, "← デッキ選択へ", () => app.ShowDeckSelect(_mode), 26);
            FantasyUi.ParchmentButton(_body, "Redo", 370, 40, 260, 76, "作り直す", StartDraft, 26);
            FantasyUi.ParchmentButton(_body, "Go", Ui.RefWidth - 40 - 420, 40, 420, 76, "対戦開始", () => Go(deck), 32, emphasized: true);
        }

        private void Go(DeckDefinition deck)
        {
            var app = AppRoot.Instance;
            // 相手 AI も同じ方式で自動ドラフト(別のシード)
            var aiDeck = DraftRating.AutoDraft(app.Db, _seed ^ 0x9E3779B97F4A7C15UL, "AI のドラフトデッキ");
            app.StartBattle(new BattleConfig
            {
                Mode = _mode,
                Deck0 = deck,
                Deck1 = aiDeck,
                Seed = (ulong)System.DateTime.Now.Ticks,
            });
        }

        // ------------------------------------------------------------------
        // オンライン
        // ------------------------------------------------------------------

        private void Quit()
        {
            if (IsOnline) { _online!.Leave(); AppRoot.Instance.ShowMainMenu(); }
            else AppRoot.Instance.ShowDeckSelect(_mode);
        }

        private void OnRemoteProgress(int picks)
        {
            if (this == null) return;
            _remotePicks = picks;
            RefreshRemote();
        }

        private void RefreshRemote()
        {
            if (!IsOnline) return;
            bool remoteDone = _remotePicks >= DraftSession.TotalPicks;
            if (_remoteText != null) _remoteText.text = remoteDone ? "相手: 完成" : $"相手 {_remotePicks} / {DraftSession.TotalPicks}";
            if (_waitText != null)
                _waitText.text = remoteDone ? "まもなく対戦を始めます…" : $"相手のドラフトを待っています(相手 {_remotePicks} / {DraftSession.TotalPicks})";
        }

        private void OnMatchStarted(MatchStartInfo info)
        {
            if (this == null || _battleStarted) return;
            _battleStarted = true;
            OnlineLobbyScreen.StartOnlineBattle(info, _online!, ex => ShowNetworkError($"デッキを受け取れませんでした: {ex.Message}"));
        }

        private void OnDisconnected()
        {
            if (this == null || _battleStarted) return;
            ShowNetworkError("相手との接続が切れました。ドラフトは破棄されます");
        }

        private void ShowNetworkError(string message)
        {
            _picking = false;
            _online?.Leave();
            CloseOverlay();
            var ov = Ui.Overlay(_overlayLayer, "Error", 0.75f);
            _overlay = ov.gameObject;
            Ui.Label(ov.transform, "Msg", 0, 560, Ui.RefWidth, 80, message, 34, TextAnchor.MiddleCenter, FantasyUi.Gold, FontStyle.Bold);
            FantasyUi.ParchmentButton(ov.transform, "Menu", (Ui.RefWidth - 360) / 2, 420, 360, 80, "メニューへ戻る", () => AppRoot.Instance.ShowMainMenu(), 28);
        }

        // ------------------------------------------------------------------
        // 拡大表示
        // ------------------------------------------------------------------

        private void ShowDetail(CardDefinition def)
        {
            CloseOverlay();
            var ov = Ui.Overlay(_overlayLayer, "Overlay", 0.6f);
            _overlay = ov.gameObject;
            ov.gameObject.AddComponent<Button>().onClick.AddListener(CloseOverlay);
            CardDetailPopup.Build(ov.rectTransform, def, null, CloseOverlay);
        }

        private void CloseOverlay()
        {
            if (_overlay != null) Destroy(_overlay);
            _overlay = null;
        }

        // 開発用(DevAutoplay から)
        internal void DevTake(int index) => Take(index);
        internal bool DevIsComplete => _session.IsComplete;
        internal void DevGo() => Go(_session.ToDeck());
    }
}
