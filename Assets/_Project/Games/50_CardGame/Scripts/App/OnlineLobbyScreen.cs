#nullable enable
using System;
using System.Collections;
using System.Linq;
using CardGame.Core.Definitions;
using CardGame.Unity.Battle;
using CardGame.Unity.Net;
using CardGame.Unity.UI;
using UnityEngine;
using UnityEngine.UI;

namespace CardGame.Unity.App
{
    /// <summary>
    /// オンライン対戦のロビー(04-screens.md「オンライン用画面」)。
    /// 部屋を作る(コード表示 → 待機)/ 部屋に参加(コード入力)。開始情報が届いたらバトルへ。
    /// </summary>
    public sealed class OnlineLobbyScreen : MonoBehaviour
    {
        private DeckDefinition? _deck;       // null = ドラフトの部屋(07-draft.md 案 2)
        private OnlineSession _online = null!;
        private Text _status = null!;
        private Text _code = null!;
        private RectTransform _panel = null!;
        private bool _started;
        private bool _mismatch;

        /// <summary>deck が null ならドラフトの部屋(接続後に 2 人同時にドラフト)。</summary>
        public void Begin(DeckDefinition? deck)
        {
            _deck = deck;
            _online = OnlineSession.GetOrCreate();
            _online.Status += OnStatus;
            _online.MatchStarted += OnMatchStarted;
            _online.Disconnected += OnDisconnected;
            _online.DraftStarted += OnDraftStarted;
            _online.FormatMismatch += OnFormatMismatch;
            Build();
            // 招待リンクで来たときは、デッキ(またはドラフト)を選んだらそのまま部屋に入る(05-online.md「招待リンク」)
            var invite = Invite.Pending;
            if (invite != null)
            {
                Invite.Clear();
                _ = JoinAsync(invite.Code);
            }
        }

        private void OnDestroy()
        {
            if (_online == null) return;
            _online.Status -= OnStatus;
            _online.MatchStarted -= OnMatchStarted;
            _online.Disconnected -= OnDisconnected;
            _online.DraftStarted -= OnDraftStarted;
            _online.FormatMismatch -= OnFormatMismatch;
        }

        private void Build()
        {
            Ui.FillPanel(transform, "Bg", new Color(0.05f, 0.04f, 0.06f, 0.55f));
            Ui.Label(transform, "Title", 0, 920, Ui.RefWidth, 100, "オンライン対戦", 60, TextAnchor.MiddleCenter, Ui.Accent, FontStyle.Bold);
            Ui.Label(transform, "Deck", 0, 860, Ui.RefWidth, 50, _deck == null ? "ドラフトで対戦(2 人そろったら同時にドラフトを始めます。相手も「ドラフトで作る」を選んでください)" : $"あなたのデッキ: {_deck.Name}", 26, TextAnchor.MiddleCenter, Ui.TextDim);
            _panel = Ui.Rect(transform, "Panel", 0, 0, Ui.RefWidth, Ui.RefHeight);
            _status = Ui.Label(transform, "Status", 0, 120, Ui.RefWidth, 50, "", 26, TextAnchor.MiddleCenter, Ui.TextDim);
            _code = Ui.Label(transform, "Code", 0, 560, Ui.RefWidth, 140, "", 110, TextAnchor.MiddleCenter, Ui.Accent, FontStyle.Bold);
            Ui.Button(transform, "Back", 40, 40, 300, 70, "メニューへ戻る", () => { Invite.Clear(); _online.Leave(); AppRoot.Instance.ShowMainMenu(); }, Ui.PanelDarkColor, 26);
            ShowChoice();
        }

        private void ShowChoice()
        {
            Ui.Clear(_panel);
            _code.text = "";
            Ui.Button(_panel, "Host", Ui.RefWidth / 2 - 520, 560, 480, 120, "部屋を作る", () => _ = HostAsync(), Ui.Accent, 40).GetComponentInChildren<Text>().color = Color.black;
            Ui.Label(_panel, "HostHint", Ui.RefWidth / 2 - 520, 500, 480, 50, "招待リンクを相手に送れます", 22, TextAnchor.MiddleCenter, Ui.TextDim);

            var field = UiInput.Field(_panel, "CodeField", Ui.RefWidth / 2 + 40, 620, 320, 90, "部屋コード", 40, 8);
            // 貼り付け: 招待文・リンク・コードのどれでも、コードを取り出してそのまま参加する
            Ui.Button(_panel, "Paste", Ui.RefWidth / 2 + 376, 620, 144, 90, "貼り付け", () => StartCoroutine(PasteAndJoin(field)), Ui.PanelColor, 28);
            Ui.Button(_panel, "Join", Ui.RefWidth / 2 + 40, 500, 480, 100, "部屋に参加", () =>
            {
                var code = field.text;
                // ブラウザ(特にスマホ Safari)では Unity の入力欄がキーボードを開けないので、標準ダイアログで聞く
                if (string.IsNullOrWhiteSpace(code) && Application.platform == RuntimePlatform.WebGLPlayer)
                    code = BrowserPrompt("部屋コード(6 文字)を入力してください", "");
                _ = JoinAsync(Invite.ParseCode(code) ?? code);
            }, Ui.PanelColor, 36);
            Ui.Label(_panel, "JoinHint", Ui.RefWidth / 2 + 40, 440, 480, 50, "届いた招待をコピーして「貼り付け」", 22, TextAnchor.MiddleCenter, Ui.TextDim);
        }

        /// <summary>クリップボードから部屋コードを取り出して参加する。読めないブラウザでは入力ダイアログ(長押しで貼り付けられる)。</summary>
        private IEnumerator PasteAndJoin(InputField field)
        {
            string text = "";
            yield return Invite.ReadClipboard(t => text = t);
            if (this == null) yield break;
            var code = Invite.ParseCode(text);
            if (code == null && Application.platform == RuntimePlatform.WebGLPlayer)
                code = Invite.ParseCode(BrowserPrompt("届いた招待(またはコード)を貼り付けてください", ""));
            if (code == null) { _status.text = "コピーした文字に部屋コードが見つかりません。招待をコピーし直してください"; yield break; }
            field.text = code;
            _ = JoinAsync(code);
        }

        private IEnumerator ShareInvite(string code, bool draft)
        {
            string result = "";
            yield return Invite.Share(Invite.BuildMessage(code, draft), Invite.BuildLink(code, draft), r => result = r);
            if (this == null) yield break;
            _status.text = result switch
            {
                "shared" => "招待を送りました。相手がリンクを開くと、この部屋に入ります",
                "copied" => "招待リンクをコピーしました。LINE などに貼り付けて送ってください",
                _ => _status.text,
            };
        }

        private IEnumerator CopyCode(string code)
        {
            string result = "";
            yield return Invite.Share(code, "", r => result = r);
            if (this != null && result == "copied") _status.text = "コードをコピーしました";
        }

        private async System.Threading.Tasks.Task HostAsync()
        {
            Ui.Clear(_panel);
            try
            {
                var code = await _online.HostAsync(DeckWire(_deck));
                if (this == null) return;
                _code.text = code;
                Ui.Label(_panel, "Wait", 0, 460, Ui.RefWidth, 60, "招待リンクかコードを相手に送ってください。相手を待っています…", 30, TextAnchor.MiddleCenter, Ui.TextMain);
                bool draft = _deck == null;
                Ui.Button(_panel, "Share", Ui.RefWidth / 2 - 440, 300, 520, 110, "招待リンクを送る", () => StartCoroutine(ShareInvite(code, draft)), Ui.Accent, 38)
                    .GetComponentInChildren<Text>().color = Color.black;
                Ui.Button(_panel, "CopyCode", Ui.RefWidth / 2 + 110, 300, 330, 110, "コードをコピー", () => StartCoroutine(CopyCode(code)), Ui.PanelColor, 32);
            }
            catch (Exception ex)
            {
                Fail(ex);
            }
        }

        private async System.Threading.Tasks.Task JoinAsync(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) { _status.text = "部屋コードを入力してください"; return; }
            Ui.Clear(_panel);
            try
            {
                await _online.JoinAsync(code, DeckWire(_deck));
                if (this == null) return;
                Ui.Label(_panel, "Wait", 0, 560, Ui.RefWidth, 60, "接続中… ホストの開始を待っています", 30, TextAnchor.MiddleCenter, Ui.TextMain);
            }
            catch (Exception ex)
            {
                Fail(ex);
            }
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        [System.Runtime.InteropServices.DllImport("__Internal")]
        private static extern string CardGame_Prompt(string message, string defaultValue);
        private static string BrowserPrompt(string message, string def) => CardGame_Prompt(message, def);
#else
        private static string BrowserPrompt(string message, string def) => def;
#endif

        /// <summary>送るデッキの文字列。プリセットは名前だけ、それ以外(ドラフト)は中身ごと。</summary>
        private static string DeckWire(DeckDefinition? deck)
            => deck == null ? OnlineSession.DraftMarker : AppRoot.Instance.Decks.Contains(deck) ? deck.Name : DeckCodec.Encode(deck);

        private void Fail(Exception ex)
        {
            Debug.LogException(ex);
            if (this == null) return;
            _status.text = $"失敗: {ex.Message}";
            _online.Leave();
            ShowChoice();
        }

        private void OnStatus(string s)
        {
            if (this != null) _status.text = s;
        }

        private void OnDisconnected()
        {
            if (this == null || _started || _mismatch) return;
            _status.text = "接続が切れました";
            ShowChoice();
        }

        private void OnDraftStarted(ulong seed)
        {
            if (this == null || _started) return;
            _started = true;
            AppRoot.Instance.ShowOnlineDraft(seed);
        }

        private void OnFormatMismatch()
        {
            if (this == null) return;
            _mismatch = true;
            ShowChoice();
            _status.text = "部屋の形式(ドラフト / 通常のデッキ)が相手と違います。2 人とも同じものを選んでください";
        }

        private void OnMatchStarted(MatchStartInfo info)
        {
            if (this == null || _started) return;
            _started = true;
            StartOnlineBattle(info, _online, ex => { _started = false; Fail(ex); });
        }

        /// <summary>
        /// 開始情報から対戦を始める(ロビーとオンラインのドラフト画面で共用)。
        /// プリセットは名前、ドラフトのデッキは中身ごと届く(05-online.md「デッキの送り方」)。不正なら onError を呼んで false。
        /// </summary>
        internal static bool StartOnlineBattle(MatchStartInfo info, OnlineSession online, Action<Exception> onError)
        {
            var app = AppRoot.Instance;
            DeckDefinition deck0, deck1;
            try
            {
                deck0 = DeckCodec.Decode(info.hostDeck, app.Db, app.Decks);
                deck1 = DeckCodec.Decode(info.guestDeck, app.Db, app.Decks);
            }
            catch (Exception ex)
            {
                onError(ex);
                return false;
            }
            var config = new BattleConfig
            {
                Mode = BattleMode.Online,
                Deck0 = deck0,
                Deck1 = deck1,
                Seed = info.seed,
                FirstPlayer = info.firstPlayer,
                LocalPlayer = online.IsHost ? 0 : 1,
            };
            app.StartBattle(config);
            return true;
        }
    }
}
