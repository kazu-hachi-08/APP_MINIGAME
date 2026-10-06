#nullable enable
using System.Collections.Generic;
using System.Linq;
using CardGame.Core.Definitions;
using CardGame.Unity.Battle;
using CardGame.Unity.UI;
using UnityEngine;
using UnityEngine.UI;

namespace CardGame.Unity.App
{
    /// <summary>
    /// デッキ選択(04-screens.md)。モードに応じて自分(+ 相手)のデッキを羊皮紙カードから選び、対戦へ進む。
    /// </summary>
    public sealed class DeckSelectScreen : MonoBehaviour
    {
        private BattleMode _mode;
        private int _deck0;
        private int _deck1;
        private readonly List<Image> _frames0 = new();
        private readonly List<Image> _frames1 = new();
        private bool _draft;                 // 自分のデッキを「ドラフトで作る」にしている
        private Image? _draftFrame;
        private CanvasGroup? _row1Group;
        private Text? _row1Note;
        private Text? _goLabel;

        private static readonly Color Selected = FantasyUi.Gold;
        private static readonly Color Unselected = FantasyUi.FrameColor;

        public void Begin(BattleMode mode)
        {
            _mode = mode;
            var app = AppRoot.Instance;
            _deck1 = Mathf.Min(1, app.Decks.Count - 1);
            Build();
            RefreshSelection();
        }

        private bool NeedsOpponent => _mode != BattleMode.Online;
        /// <summary>ドラフトを選べるモード(07-draft.md: AI 対戦とオンラインのみ)。</summary>
        private bool CanDraft => (_mode == BattleMode.VersusAi || _mode == BattleMode.Online) && !InvitedToDeckRoom;
        /// <summary>通常のデッキの部屋に招待されて来た(ドラフトは選べない)。</summary>
        private bool InvitedToDeckRoom => _mode == BattleMode.Online && Net.Invite.Pending is { Draft: false };

        private void Build()
        {
            var app = AppRoot.Instance;
            string modeName = _mode switch
            {
                BattleMode.VersusAi => "AI 対戦",
                BattleMode.LocalTwoPlayer => "ローカル対戦",
                BattleMode.AiVersusAi => "AI 同士の観戦",
                _ => "オンライン対戦",
            };
            FantasyUi.GoldTitle(transform, "Title", 0, 960, Ui.RefWidth, 100, $"{modeName} — デッキを選ぶ", 56);
            if (InvitedToDeckRoom)
                Ui.Label(transform, "Invite", 0, 880, Ui.RefWidth, 50, $"招待された部屋(コード {Net.Invite.Pending!.Code})に入ります。使うデッキを選んでください", 30, TextAnchor.MiddleCenter, FantasyUi.Gold, FontStyle.Bold).raycastTarget = false;

            if (NeedsOpponent)
            {
                // 2 段: 上 = 自分(1P)、下 = 相手(AI / 2P)
                string label0 = _mode == BattleMode.LocalTwoPlayer ? "1P のデッキ" : _mode == BattleMode.AiVersusAi ? "先攻側(AI)のデッキ" : "あなたのデッキ";
                string label1 = _mode == BattleMode.LocalTwoPlayer ? "2P のデッキ" : _mode == BattleMode.AiVersusAi ? "後攻側(AI)のデッキ" : "相手(AI)のデッキ";
                BuildRow(transform, label0, 560, 300, _frames0, i => { _deck0 = i; _draft = false; RefreshSelection(); }, CanDraft);
                // 相手の段はまとめて薄くできるように 1 つの入れ物に入れる(ドラフト時は相手 AI も自動ドラフト)
                var row1 = Ui.Fill(transform, "Row1");
                _row1Group = row1.gameObject.AddComponent<CanvasGroup>();
                BuildRow(row1, label1, 170, 300, _frames1, i => { _deck1 = i; RefreshSelection(); }, false);
                _row1Note = Ui.Label(transform, "Row1Note", 0, 290, Ui.RefWidth, 60, "相手(AI)もドラフトでデッキを作ります", 34, TextAnchor.MiddleCenter, FantasyUi.Gold, FontStyle.Bold);
                _row1Note.raycastTarget = false;
            }
            else
            {
                BuildRow(transform, "あなたのデッキ", 330, 420, _frames0, i => { _deck0 = i; _draft = false; RefreshSelection(); }, CanDraft);
            }

            FantasyUi.ParchmentButton(transform, "Back", 40, 40, 260, 76, "← 戻る", () => { Net.Invite.Clear(); app.ShowMainMenu(); }, 26);
            _goLabel = FantasyUi.ParchmentButton(transform, "Go", Ui.RefWidth - 40 - 420, 40, 420, 76, "", Go, 32, emphasized: true).GetComponentInChildren<Text>();
        }

        /// <summary>デッキを横一列に並べる。panelH は羊皮紙カードの高さ。</summary>
        /// <summary>デッキを横一列に並べる。panelH は羊皮紙カードの高さ。withDraft なら先頭に「ドラフトで作る」を置く。</summary>
        private void BuildRow(Transform parent, string label, float y, float panelH, List<Image> frames, System.Action<int> onSelect, bool withDraft)
        {
            var app = AppRoot.Instance;
            int slots = app.Decks.Count + (withDraft ? 1 : 0);
            const float gap = 28;
            float panelW = Mathf.Min(300, (Ui.RefWidth - 120 - gap * (slots - 1)) / slots);
            float startX = (Ui.RefWidth - (panelW * slots + gap * (slots - 1))) / 2;
            Ui.Label(parent, label + "Label", startX, y + panelH + 4, 800, 40, label, 26, TextAnchor.MiddleLeft, FantasyUi.Gold, FontStyle.Bold);
            if (withDraft)
            {
                BuildDraftTile(parent, startX, y, panelW, panelH);
                startX += panelW + gap;
            }

            for (int i = 0; i < app.Decks.Count; i++)
            {
                int idx = i;
                var deck = app.Decks[i];
                float x = startX + i * (panelW + gap);
                var outer = FantasyUi.ParchmentPanel(parent, "Deck" + i, x, y, panelW, panelH);
                frames.Add(outer);
                var btn = outer.gameObject.AddComponent<Button>();
                btn.onClick.AddListener(() => onSelect(idx));

                // 代表イラスト(そのデッキで一番コストの高いフォロワー)を上 60% に敷く
                var art = RepresentativeArt(deck);
                float artH = Mathf.Round(panelH * 0.62f);
                if (art != null)
                {
                    var mask = Ui.Rect(outer.transform, "ArtMask", 6, panelH - 6 - artH, panelW - 12, artH);
                    mask.gameObject.AddComponent<RectMask2D>();
                    float aspect = art.rect.width / art.rect.height;
                    float iw = panelW - 12, ih = iw / aspect;
                    if (ih < artH) { ih = artH; iw = ih * aspect; }
                    var img = Ui.Rect(mask, "Art", (panelW - 12 - iw) / 2, artH - ih, iw, ih).gameObject.AddComponent<Image>();
                    img.sprite = art; img.raycastTarget = false;
                    var fade = Ui.Panel(mask, "Fade", 0, 0, panelW - 12, 40, new Color(0.93f, 0.85f, 0.66f, 0.9f));
                    fade.sprite = Icons.VerticalGradient(); fade.type = Image.Type.Simple; fade.raycastTarget = false;
                }
                // クラス紋章(イラストの左下に重ねる)
                var emblem = CardArt.Emblem(deck.Class);
                if (emblem != null)
                {
                    const float es = 64;
                    var em = Ui.Rect(outer.transform, "Emblem", 10, panelH - 6 - artH + 6, es, es).gameObject.AddComponent<Image>();
                    em.sprite = emblem; em.raycastTarget = false;
                    var sh = Ui.Rect(outer.transform, "EmblemShadow", 6, panelH - 6 - artH + 2, es + 8, es + 8).gameObject.AddComponent<Image>();
                    sh.sprite = Materials.SoftShadow(); sh.color = new Color(0, 0, 0, 0.7f); sh.raycastTarget = false;
                    sh.transform.SetSiblingIndex(em.transform.GetSiblingIndex());
                }
                float textTop = panelH - 6 - artH;
                // 長い名前(「ネクロマンサー プリセット」)も 1 行に収める(折り返すと下の行に重なる)
                var nameLabel = Ui.Label(outer.transform, "Name", 8, textTop - 54, panelW - 16, 50, deck.Name, 26, TextAnchor.MiddleCenter, FantasyUi.Ink, FontStyle.Bold);
                nameLabel.raycastTarget = false;
                nameLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
                float maxW = panelW - 24;
                if (nameLabel.preferredWidth > maxW) nameLabel.fontSize = Mathf.Max(16, Mathf.FloorToInt(nameLabel.fontSize * maxW / nameLabel.preferredWidth));
                Ui.Label(outer.transform, "Class", 8, textTop - 90, panelW - 16, 34, $"{CardView.ClassLabel(deck.Class)}  /  {deck.CardIds.Count} 枚", 20, TextAnchor.MiddleCenter, FantasyUi.InkDim).raycastTarget = false;
            }
        }

        /// <summary>「ドラフトで作る」の羊皮紙カード(混沌の紋章を大きく)。</summary>
        private void BuildDraftTile(Transform parent, float x, float y, float panelW, float panelH)
        {
            var outer = FantasyUi.ParchmentPanel(parent, "Draft", x, y, panelW, panelH);
            _draftFrame = outer;
            outer.gameObject.AddComponent<Button>().onClick.AddListener(() => { _draft = true; RefreshSelection(); });
            float artH = Mathf.Round(panelH * 0.62f);
            var dark = Ui.Panel(outer.transform, "Dark", 6, panelH - 6 - artH, panelW - 12, artH, new Color(0.10f, 0.06f, 0.14f));
            dark.raycastTarget = false;
            var emblem = CardArt.Emblem(CardClass.Chaos);
            float es = Mathf.Min(artH - 20, panelW - 40);
            if (emblem != null)
            {
                var em = Ui.Rect(outer.transform, "Emblem", (panelW - es) / 2, panelH - 6 - artH + (artH - es) / 2, es, es).gameObject.AddComponent<Image>();
                em.sprite = emblem; em.raycastTarget = false;
            }
            float textTop = panelH - 6 - artH;
            Ui.Label(outer.transform, "Name", 8, textTop - 54, panelW - 16, 50, "ドラフトで作る", 26, TextAnchor.MiddleCenter, FantasyUi.InkRed, FontStyle.Bold).raycastTarget = false;
            Ui.Label(outer.transform, "Class", 8, textTop - 90, panelW - 16, 34, $"カオス  /  {DeckDefinition.DraftDeckSize} 枚", 20, TextAnchor.MiddleCenter, FantasyUi.InkDim).raycastTarget = false;
        }

        private static Sprite? RepresentativeArt(DeckDefinition deck)
        {
            var db = AppRoot.Instance.Db;
            var best = deck.CardIds.Distinct().Select(id => db.Get(id))
                .Where(c => c.IsFollower && c.Class != CardClass.Neutral)
                .OrderByDescending(c => c.Cost).ThenBy(c => c.Id, System.StringComparer.Ordinal).FirstOrDefault()
                ?? deck.CardIds.Select(id => db.Get(id)).OrderByDescending(c => c.Cost).FirstOrDefault();
            return best == null ? null : CardArt.Get(best.Id);
        }

        private void RefreshSelection()
        {
            for (int i = 0; i < _frames0.Count; i++) _frames0[i].color = !_draft && i == _deck0 ? Selected : Unselected;
            if (_draftFrame != null) _draftFrame.color = _draft ? Selected : Unselected;
            if (_row1Group != null) { _row1Group.alpha = _draft ? 0.25f : 1f; _row1Group.interactable = _row1Group.blocksRaycasts = !_draft; }
            if (_row1Note != null) _row1Note.gameObject.SetActive(_draft);
            if (_goLabel != null) _goLabel.text = _draft ? "ドラフトへ進む" : _mode == BattleMode.Online ? "部屋へ進む" : "対戦開始";
            for (int i = 0; i < _frames1.Count; i++) _frames1[i].color = i == _deck1 ? Selected : Unselected;
        }

        private void Go()
        {
            var app = AppRoot.Instance;
            if (_draft)
            {
                // オンラインは部屋で接続してから 2 人同時にドラフト(07-draft.md 案 2)
                if (_mode == BattleMode.Online) app.ShowOnlineLobby(null);
                else app.ShowDraft(_mode);
                return;
            }
            if (_mode == BattleMode.Online)
            {
                app.ShowOnlineLobby(app.Decks[_deck0]);
                return;
            }
            app.StartBattle(new BattleConfig
            {
                Mode = _mode,
                Deck0 = app.Decks[_deck0],
                Deck1 = app.Decks[_deck1],
                Seed = (ulong)System.DateTime.Now.Ticks,
            });
        }

        // 開発用(DevAutoplay から)
        internal void Select(int player, int idx) { if (player == 0) { _deck0 = idx; _draft = false; } else _deck1 = idx; RefreshSelection(); }
        internal void SelectDraft() { _draft = true; RefreshSelection(); }
        internal void Proceed() => Go();
    }
}
