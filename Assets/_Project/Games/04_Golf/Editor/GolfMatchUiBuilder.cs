using MiniGame.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.Golf.Editor
{
    /// <summary>
    /// 試合の進行に使う全画面UI（設定画面・「○○の番」・スコアカード）を作る。
    /// GolfSceneBuilder が大きくなりすぎないよう、ショット操作のUIとは分けておく。
    /// </summary>
    internal static class GolfMatchUiBuilder
    {
        private static readonly Color DimColor = new Color(0f, 0f, 0f, 0.8f);
        private static readonly Color BoxColor = new Color(0.12f, 0.14f, 0.18f);
        private static readonly Color ChoiceColor = new Color(0.3f, 0.33f, 0.4f);
        private static readonly Color PrimaryColor = new Color(0.2f, 0.7f, 0.35f);

        private static readonly Vector2 CenterAnchor = new Vector2(0.5f, 0.5f);

        // 文字の縁取りの太さ。大きい文字ほど太くして背景の上でも読めるようにする
        private static readonly Vector2 NameOutlineDistance = new Vector2(3f, -3f);
        private static readonly Vector2 BannerTitleOutlineDistance = new Vector2(4f, -4f);

        private const float BoxWidth = 920f;
        private const int BoxPadding = 40;
        private const float BoxSpacing = 28f;
        private const float RowWidth = 820f;
        private const float RowHeight = 110f;
        // 見出しの行は選択肢の行より低くして、パネルの縦幅に収める
        private const float LabelRowHeight = RowHeight * 0.6f;
        private const float RowSpacing = 20f;
        private const int TitleFontSize = 60;
        private const int LabelFontSize = 44;
        private const int ButtonFontSize = 44;
        private const float PrimaryButtonWidth = 560f;
        private const float PrimaryButtonHeight = 140f;
        private const int PrimaryButtonFontSize = 56;

        private const float SetupBoxHeight = 1260f;
        private const float ModeButtonWidth = 360f;
        private const float CountButtonWidth = 240f;
        private const float TypeButtonWidth = 190f;
        private const float TypeButtonHeight = 130f;
        private const int TypeButtonFontSize = 34;

        private const float CharacterBoxHeight = 1380f;
        private const float PortraitWidth = 360f;
        private const float PortraitHeight = 540f;
        private const float ArrowButtonSize = 150f;
        private const int CharacterNameFontSize = 64;
        private const float StatLabelWidth = 300f;
        private const float StatRowHeight = 70f;
        private const float StatCellSize = 60f;
        private const int StatCellCount = 5;
        private const float BackButtonWidth = 280f;
        private const float ConfirmButtonWidth = 440f;

        // 飛距離 0.9〜1.15、曲がりにくさ 0.75〜1.35 の差がマスの数で見えるよう、バーごとに幅を合わせる
        private const float DistanceBarMin = 0.8f;
        private const float DistanceBarMax = 1.25f;
        private const float StraightnessBarMin = 0.75f;
        private const float StraightnessBarMax = 1.35f;

        // 「○○の番」は 詳細 → 名前 → ヒント を画面中央から上下に並べる
        private const float BannerTextWidth = 1000f;
        private const float BannerDetailHeight = 300f;
        private const float BannerDetailY = 260f;
        private const float BannerTitleHeight = 200f;
        private const float BannerTitleY = 0f;
        private const float BannerHintHeight = 90f;
        private const float BannerHintY = -160f;
        private const int BannerTitleFontSize = 110;
        private const int BannerTitleMinFontSize = 60;
        private const int BannerDetailFontSize = 52;
        private const int BannerHintFontSize = 44;

        private const float ScoreCardBoxHeight = 1150f;
        private const float ScoreGridHeight = 560f;
        private const float ScoreCellWidth = 100f;
        private const float ScoreCellHeight = 80f;
        private const int ScoreCellFontSize = 40;
        private const int ScoreCellMinFontSize = 18;

        /// <summary>モード（1ホール／3ホール）→ 人数 → 人間/NPC → 試合開始 を縦に並べる</summary>
        public static GolfSetupPanel CreateSetupPanel(Transform canvas)
        {
            GameObject panelObj = CreateDimmedPanel(canvas, "GolfSetupPanel");
            Transform box = CreateBox(panelObj.transform, SetupBoxHeight);

            CreateText(box, "Title", "ゴルフ 設定", TitleFontSize, RowWidth, RowHeight);

            CreateSectionLabel(box, "ModeLabel", "モード");
            Transform modeRow = CreateRow(box, "ModeRow");
            Button oneHole = CreateChoiceButton(modeRow, "Btn_OneHole", "1ホール", ModeButtonWidth);
            Button threeHole = CreateChoiceButton(modeRow, "Btn_ThreeHole", $"{GolfRules.LongModeHoleCount}ホール", ModeButtonWidth);

            CreateSectionLabel(box, "CountLabel", "人数");
            Button[] countButtons = CreateCountButtons(CreateRow(box, "CountRow"));

            CreateSectionLabel(box, "TypeLabel", "人間 / NPC（タップで切り替え）");
            Button[] typeButtons = CreateTypeButtons(CreateRow(box, "TypeRow", TypeButtonHeight));

            Button start = CreatePrimaryButton(box, "Btn_Start", "試合開始").GetComponent<Button>();

            var panel = panelObj.AddComponent<GolfSetupPanel>();
            GolfSceneBuilder.SetRefs(panel, ("_oneHoleButton", oneHole), ("_threeHoleButton", threeHole),
                ("_startButton", start));
            GolfSceneBuilder.SetArray(panel, "_countButtons", countButtons);
            GolfSceneBuilder.SetArray(panel, "_typeButtons", typeButtons);

            panelObj.SetActive(false);
            return panel;
        }

        private static Button[] CreateCountButtons(Transform row)
        {
            var buttons = new Button[GolfSetupPanel.MaxPlayers - GolfSetupPanel.MinPlayers + 1];
            for (int i = 0; i < buttons.Length; i++)
            {
                int count = GolfSetupPanel.MinPlayers + i;
                buttons[i] = CreateChoiceButton(row, $"Btn_{count}Players", $"{count}人", CountButtonWidth);
            }

            return buttons;
        }

        private static Button[] CreateTypeButtons(Transform row)
        {
            var buttons = new Button[GolfSetupPanel.MaxPlayers];
            for (int i = 0; i < buttons.Length; i++)
            {
                // ラベルは GolfSetupPanel が「P1 / 人間」のように実行時に書き換える
                GameObject obj = GolfSceneBuilder.CreateButton(row, $"Btn_TypeP{i + 1}", string.Empty, TypeButtonWidth,
                    TypeButtonHeight, ChoiceColor, TypeButtonFontSize);
                buttons[i] = obj.GetComponent<Button>();
            }

            return buttons;
        }

        /// <summary>
        /// 人数設定の後に1人ずつキャラを選ぶパネル。
        /// 立ち絵を大きく中央に置き、左右の ◀ ▶ で切り替える。能力は2行のマス表示で見せる
        /// </summary>
        public static GolfCharacterSelectPanel CreateCharacterSelectPanel(Transform canvas, GolfCharacterCatalog catalog)
        {
            GameObject panelObj = CreateDimmedPanel(canvas, "GolfCharacterSelectPanel");
            Transform box = CreateBox(panelObj.transform, CharacterBoxHeight);

            Text title = CreateText(box, "Title", string.Empty, TitleFontSize, RowWidth, RowHeight);
            FitToRect(title, LabelFontSize);

            Image portrait = CreatePortraitRow(box, out Button prev, out Button next);

            Text nameText = CreateText(box, "NameText", string.Empty, CharacterNameFontSize, RowWidth, RowHeight);
            nameText.gameObject.AddComponent<Outline>().effectDistance = NameOutlineDistance;

            GolfStatBarView distanceBar = CreateStatRow(box, "Distance", "飛距離", DistanceBarMin, DistanceBarMax);
            GolfStatBarView straightnessBar = CreateStatRow(box, "Straightness", "曲がりにくさ", StraightnessBarMin,
                StraightnessBarMax);

            CreateBackConfirmRow(box, out Button back, out Button confirm);

            var panel = panelObj.AddComponent<GolfCharacterSelectPanel>();
            GolfSceneBuilder.SetRefs(panel, ("_catalog", catalog), ("_titleText", title), ("_portrait", portrait),
                ("_nameText", nameText), ("_distanceBar", distanceBar), ("_straightnessBar", straightnessBar),
                ("_prevButton", prev), ("_nextButton", next),
                ("_confirmButton", confirm), ("_backButton", back));

            panelObj.SetActive(false);
            return panel;
        }

        /// <summary>◀ 立ち絵 ▶ の行</summary>
        private static Image CreatePortraitRow(Transform box, out Button prev, out Button next)
        {
            Transform row = CreateRow(box, "PortraitRow", PortraitHeight);
            prev = CreateSquareButton(row, "Btn_Prev", "◀");
            GameObject portraitObj = UIDialogBuilder.CreateUIObject("Portrait", row);
            portraitObj.GetComponent<RectTransform>().sizeDelta = new Vector2(PortraitWidth, PortraitHeight);
            var portrait = portraitObj.AddComponent<Image>();
            portrait.preserveAspect = true;
            portrait.raycastTarget = false;
            next = CreateSquareButton(row, "Btn_Next", "▶");
            return portrait;
        }

        private static void CreateBackConfirmRow(Transform box, out Button back, out Button confirm)
        {
            Transform row = CreateRow(box, "ButtonRow", PrimaryButtonHeight);
            back = GolfSceneBuilder.CreateButton(row, "Btn_Back", "戻る", BackButtonWidth, PrimaryButtonHeight,
                ChoiceColor, ButtonFontSize).GetComponent<Button>();
            confirm = GolfSceneBuilder.CreateButton(row, "Btn_Confirm", "決定", ConfirmButtonWidth, PrimaryButtonHeight,
                PrimaryColor, PrimaryButtonFontSize).GetComponent<Button>();
        }

        /// <summary>能力1行（ラベル＋5マス）</summary>
        private static GolfStatBarView CreateStatRow(Transform parent, string name, string label, float min, float max)
        {
            Transform row = CreateRow(parent, $"Stat_{name}", StatRowHeight);
            Text labelText = CreateText(row, "Label", label, LabelFontSize, StatLabelWidth, StatRowHeight);
            labelText.alignment = TextAnchor.MiddleLeft;

            var cells = new Image[StatCellCount];
            for (int i = 0; i < cells.Length; i++)
            {
                GameObject cellObj = UIDialogBuilder.CreateUIObject($"Cell_{i + 1}", row);
                cellObj.GetComponent<RectTransform>().sizeDelta = new Vector2(StatCellSize, StatCellSize);
                cells[i] = cellObj.AddComponent<Image>();
                cells[i].raycastTarget = false;
            }

            var bar = row.gameObject.AddComponent<GolfStatBarView>();
            GolfSceneBuilder.SetArray(bar, "_cells", cells);
            var so = new SerializedObject(bar);
            so.FindProperty("_minMultiplier").floatValue = min;
            so.FindProperty("_maxMultiplier").floatValue = max;
            so.ApplyModifiedPropertiesWithoutUndo();
            return bar;
        }

        private static Button CreateSquareButton(Transform parent, string name, string label)
        {
            return GolfSceneBuilder.CreateButton(parent, name, label, ArrowButtonSize, ArrowButtonSize, ChoiceColor,
                PrimaryButtonFontSize).GetComponent<Button>();
        }

        /// <summary>画面全体をボタンにして、どこをタップしても開始できるようにする</summary>
        public static GolfTurnBannerView CreateTurnBanner(Transform canvas)
        {
            GameObject bannerObj = UIDialogBuilder.CreateUIObject("TurnBanner", canvas);
            UIDialogBuilder.SetStretchAll(bannerObj.GetComponent<RectTransform>());
            var background = bannerObj.AddComponent<Image>();
            var tapArea = bannerObj.AddComponent<Button>();
            tapArea.transition = Selectable.Transition.None;

            Text detail = CreateBannerText(bannerObj.transform, "DetailText", string.Empty, BannerDetailFontSize,
                BannerDetailHeight, BannerDetailY);
            Text title = CreateBannerText(bannerObj.transform, "TitleText", string.Empty, BannerTitleFontSize,
                BannerTitleHeight, BannerTitleY);
            title.gameObject.AddComponent<Outline>().effectDistance = BannerTitleOutlineDistance;
            // 「P1 テクニック型 の番」が2行に折り返して下のヒントと重ならないよう、1行に収まる大きさまで縮める
            FitToRect(title, BannerTitleMinFontSize);
            Text hint = CreateBannerText(bannerObj.transform, "HintText", "タップで開始", BannerHintFontSize,
                BannerHintHeight, BannerHintY);

            var banner = bannerObj.AddComponent<GolfTurnBannerView>();
            GolfSceneBuilder.SetRefs(banner, ("_titleText", title), ("_detailText", detail), ("_hintText", hint),
                ("_tapArea", tapArea),
                ("_background", background));

            bannerObj.SetActive(false);
            return banner;
        }

        private static Text CreateBannerText(Transform parent, string name, string content, int fontSize, float height,
            float y)
        {
            Text text = CreateText(parent, name, content, fontSize, BannerTextWidth, height);
            RectTransform rect = text.rectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = CenterAnchor;
            rect.anchoredPosition = new Vector2(0f, y);
            return text;
        }

        /// <summary>タイトル → 表（セルは ScoreCardView が実行時に作る）→ ボタン2つ</summary>
        public static ScoreCardView CreateScoreCard(Transform canvas)
        {
            GameObject panelObj = CreateDimmedPanel(canvas, "ScoreCard");
            Transform box = CreateBox(panelObj.transform, ScoreCardBoxHeight);

            Text title = CreateText(box, "Title", string.Empty, TitleFontSize, RowWidth, RowHeight);
            GridLayoutGroup grid = CreateScoreGrid(box, out Text cellTemplate);

            GameObject primaryObj = CreatePrimaryButton(box, "Btn_Primary", string.Empty);
            GameObject secondaryObj = GolfSceneBuilder.CreateButton(box, "Btn_Title", "タイトルへ",
                PrimaryButtonWidth, RowHeight, ChoiceColor, ButtonFontSize);

            var view = panelObj.AddComponent<ScoreCardView>();
            GolfSceneBuilder.SetRefs(view, ("_titleText", title), ("_grid", grid), ("_cellTemplate", cellTemplate),
                ("_primaryButton", primaryObj.GetComponent<Button>()),
                ("_primaryLabel", primaryObj.GetComponentInChildren<Text>()),
                ("_secondaryButton", secondaryObj.GetComponent<Button>()));

            panelObj.SetActive(false);
            return view;
        }

        private static GridLayoutGroup CreateScoreGrid(Transform box, out Text cellTemplate)
        {
            GameObject gridObj = UIDialogBuilder.CreateUIObject("Grid", box);
            gridObj.GetComponent<RectTransform>().sizeDelta = new Vector2(RowWidth, ScoreGridHeight);
            var grid = gridObj.AddComponent<GridLayoutGroup>();
            grid.childAlignment = TextAnchor.UpperCenter;

            cellTemplate = CreateText(gridObj.transform, "CellTemplate", string.Empty, ScoreCellFontSize, ScoreCellWidth,
                ScoreCellHeight);
            // 見出し列の「1位 P1」「テクニック型」の2行がセルに収まるよう縮める。数字だけのセルは元の大きさのまま
            FitToRect(cellTemplate, ScoreCellMinFontSize);
            return grid;
        }

        private static GameObject CreateDimmedPanel(Transform canvas, string name)
        {
            GameObject panelObj = UIDialogBuilder.CreateUIObject(name, canvas);
            UIDialogBuilder.SetStretchAll(panelObj.GetComponent<RectTransform>());
            panelObj.AddComponent<Image>().color = DimColor;
            return panelObj;
        }

        /// <summary>中身を上から順に積む箱。非表示にした子は VerticalLayoutGroup が詰める</summary>
        private static Transform CreateBox(Transform parent, float height)
        {
            GameObject boxObj = UIDialogBuilder.CreateUIObject("Box", parent);
            var rect = boxObj.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = CenterAnchor;
            rect.sizeDelta = new Vector2(BoxWidth, height);
            boxObj.AddComponent<Image>().color = BoxColor;

            var layout = boxObj.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(BoxPadding, BoxPadding, BoxPadding, BoxPadding);
            layout.spacing = BoxSpacing;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            return boxObj.transform;
        }

        private static Transform CreateRow(Transform parent, string name, float height = RowHeight)
        {
            GameObject rowObj = UIDialogBuilder.CreateUIObject(name, parent);
            rowObj.GetComponent<RectTransform>().sizeDelta = new Vector2(RowWidth, height);
            var layout = rowObj.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = RowSpacing;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            return rowObj.transform;
        }

        private static void CreateSectionLabel(Transform box, string name, string label)
        {
            CreateText(box, name, label, LabelFontSize, RowWidth, LabelRowHeight);
        }

        private static Button CreateChoiceButton(Transform parent, string name, string label, float width)
        {
            return GolfSceneBuilder.CreateButton(parent, name, label, width, RowHeight, ChoiceColor, ButtonFontSize)
                .GetComponent<Button>();
        }

        private static GameObject CreatePrimaryButton(Transform parent, string name, string label)
        {
            return GolfSceneBuilder.CreateButton(parent, name, label, PrimaryButtonWidth, PrimaryButtonHeight,
                PrimaryColor, PrimaryButtonFontSize);
        }

        private static Text CreateText(Transform parent, string name, string content, int fontSize, float width, float height)
        {
            GameObject obj = UIDialogBuilder.CreateUIObject(name, parent);
            obj.GetComponent<RectTransform>().sizeDelta = new Vector2(width, height);
            var text = obj.AddComponent<Text>();
            text.text = content;
            text.fontSize = fontSize;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        /// <summary>
        /// 枠に収まる大きさまで文字を縮める。CreateText は文字が消えないよう枠からはみ出せる設定にしているが、
        /// そのままだと縮小（Best Fit）が効かないので、ここでは枠内に収める設定に戻す
        /// </summary>
        private static void FitToRect(Text text, int minSize)
        {
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = minSize;
            text.resizeTextMaxSize = text.fontSize;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
        }
    }
}
