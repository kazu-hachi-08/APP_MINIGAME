#nullable enable
using System.Collections.Generic;
using System.Linq;
using CardGame.Core.Definitions;
using CardGame.Unity.Battle;
using CardGame.Unity.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CardGame.Unity.App
{
    /// <summary>
    /// アプリのエントリポイント。ブートシーンに 1 つだけ置く。
    /// カードデータの読み込み、Canvas/EventSystem の生成、画面遷移を担当する。
    /// </summary>
    public sealed class AppRoot : MonoBehaviour
    {
        public static AppRoot Instance { get; private set; } = null!;

        public CardDatabase Db { get; private set; } = null!;
        public IReadOnlyList<DeckDefinition> Decks { get; private set; } = null!;

        private Canvas _canvas = null!;
        private GameObject? _currentScreen;

        private void Awake()
        {
            Instance = this;
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            LoadData();
            Audio.PreloadAll();   // WebGL は音の展開が非同期なので、最初に鳴らす前に読み込みを始めておく

            _canvas = Ui.CreateCanvas("Canvas");
            BuildBackground();
            if (FindAnyObjectByType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            ShowMainMenu();
            // 招待リンクで開かれたら、その部屋に入る流れへ直行する(05-online.md「招待リンク」)
            Net.Invite.CaptureAtStartup();
            if (Net.Invite.Pending != null) RouteInvite();
            DevAutoplay.TryStart(this);
        }

        /// <summary>Core パッケージ内の Resources/cards, Resources/decks(JSON)を読む。</summary>
        private void LoadData()
        {
            Db = new CardDatabase();
            foreach (var asset in Resources.LoadAll<TextAsset>("cards").OrderBy(a => a.name))
                Db.LoadJson(asset.text);
            Db.Validate();

            Decks = Resources.LoadAll<TextAsset>("decks").OrderBy(a => a.name)
                .Select(a => DeckDefinition.FromJson(a.text)).OrderBy(d => d.Class).ToList();
            foreach (var d in Decks)
            {
                var errors = d.Validate(Db);
                if (errors.Count > 0) Debug.LogError($"デッキ '{d.Name}' が不正: {string.Join(" / ", errors)}");
            }
            // ドラフトの AI 用の評価表(07-draft.md)。Unity では埋め込みリソースが無いので Resources から渡す
            var ratings = Resources.Load<TextAsset>("draft/ratings");
            if (ratings != null) Core.Draft.DraftRating.LoadTable(ratings.text);
            else Debug.LogWarning("[AppRoot] draft/ratings が無い。ドラフトの AI は仮の式で選ぶ");
            Debug.Log($"[AppRoot] カード {Db.Count} 枚、デッキ {Decks.Count} 個を読み込んだ");
        }

        /// <summary>
        /// 全画面に木の机と周辺減光を敷く(フィールド案 A)。画面はこの上に載る。
        /// Resources/Field/table があればその画像を画面いっぱいに(縦横比を保って余白なく)敷き、無ければノイズ生成の木目。
        /// </summary>
        private void BuildBackground()
        {
            var table = Resources.Load<Sprite>("Field/table");
            if (table != null)
            {
                // 画像の縦横比(2:1)と画面(16:9 など)が違うので、中央基準で画面を覆う大きさに広げる(はみ出た分は切れる)
                var holder = Ui.Fill(_canvas.transform, "TableHolder");
                var img = new GameObject("Table", typeof(RectTransform)).AddComponent<Image>();
                var rt = img.rectTransform;
                rt.SetParent(holder, false);
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
                img.sprite = table; img.type = Image.Type.Simple; img.raycastTarget = false;
                var fit = img.gameObject.AddComponent<AspectRatioFitter>();
                fit.aspectRatio = table.rect.width / table.rect.height;
                fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            }
            else
            {
                var wood = UiTex.Fill(_canvas.transform, "TableWood", Materials.Wood(), new Color(0.95f, 0.9f, 0.85f), tiled: true, tilePx: 900);
                wood.raycastTarget = false;
            }
            var vignette = Ui.FillPanel(_canvas.transform, "Vignette", Color.white);
            vignette.sprite = Materials.Vignette(); vignette.type = Image.Type.Simple; vignette.raycastTarget = false;
        }

        // ---- 画面遷移 ----

        private T Switch<T>(string name) where T : MonoBehaviour
        {
            if (_currentScreen != null) Destroy(_currentScreen);
            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(_canvas.transform, false);
            // 画面は常に 1920×1080 の固定枠を中央に置く。横長端末(iPhone 19.5:9 など)では左右に余白、
            // 縦長寄り(タブレット 4:3)では上下に余白ができる。CanvasScaler 側で「短い方の辺」を基準に拡縮する
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(Ui.RefWidth, Ui.RefHeight);
            rt.anchoredPosition = Vector2.zero;
            _currentScreen = go;
            return go.AddComponent<T>();
        }

        public void ShowMainMenu()
        {
            Audio.PlayBgm("menu");
            Switch<MainMenuScreen>("MainMenu");
        }
        public void ShowCardList() => Switch<CardListScreen>("CardList");
        public void ShowSettings() => Switch<SettingsScreen>("Settings");
        public void ShowDeckSelect(BattleMode mode) => Switch<DeckSelectScreen>("DeckSelect").Begin(mode);
        public void ShowDraft(BattleMode mode) => Switch<DraftScreen>("Draft").Begin(mode);
        public void ShowOnlineDraft(ulong seed) => Switch<DraftScreen>("Draft").BeginOnline(seed);

        /// <summary>届いた招待へ進む。ドラフトの部屋ならすぐ部屋へ、通常の部屋ならデッキを選んでから部屋へ(自動で参加する)。</summary>
        public void RouteInvite()
        {
            var invite = Net.Invite.Pending;
            if (invite == null) return;
            if (invite.Draft) ShowOnlineLobby(null);
            else ShowDeckSelect(BattleMode.Online);
        }

        public void ShowOnlineLobby(Core.Definitions.DeckDefinition? deck)
        {
            var screen = Switch<OnlineLobbyScreen>("OnlineLobby");
            screen.Begin(deck);
        }

        public void StartBattle(BattleConfig config)
        {
            Audio.PlayBgm("battle");
            var screen = Switch<BattleScreen>("Battle");
            screen.Begin(config);
        }
    }
}
