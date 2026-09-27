using MiniGame.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.Golf.Editor
{
    /// <summary>
    /// 試合の進行に使う全画面UI（設定画面・「○○の番」・スコアカード）を作る（Phase 6）。
    /// GolfSceneBuilder が大きくなりすぎないよう、ショット操作のUIとは分けておく。
    /// </summary>
    internal static class GolfMatchUiBuilder
    {
        private static readonly Color DimColor = new Color(0f, 0f, 0f, 0.8f);
        private static readonly Color BoxColor = new Color(0.12f, 0.14f, 0.18f);
        private static readonly Color ChoiceColor = new Color(0.3f, 0.33f, 0.4f);
        private static readonly Color PrimaryColor = new Color(0.2f, 0.7f, 0.35f);

        private const float BoxWidth = 920f;
        private const float RowWidth = 820f;
        private const float RowHeight = 110f;
        private const int TitleFontSize = 60;
        private const int LabelFontSize = 44;
        private const int ButtonFontSize = 44;
        private const float PrimaryButtonWidth = 560f;
        private const float PrimaryButtonHeight = 140f;
        private const int PrimaryButtonFontSize = 56;

        private const float SetupBoxHeight = 1000f;
        private const float ModeButtonWidth = 360f;
        private const float CountButtonWidth = 240f;

        private const int BannerTitleFontSize = 110;
        private const int BannerDetailFontSize = 52;
        private const int BannerHintFontSize = 44;

        private const float ScoreCardBoxHeight = 1150f;
        private const float ScoreGridHeight = 560f;
        private const int ScoreCellFontSize = 40;

        /// <summary>モード（1ホール／3ホール）→ 人数 → 試合開始 を縦に並べる（§11.1）</summary>
        public static GolfSetupPanel CreateSetupPanel(Transform canvas)
        {
            GameObject panelObj = CreateDimmedPanel(canvas, "GolfSetupPanel");
            Transform box = CreateBox(panelObj.transform, SetupBoxHeight);

            CreateText(box, "Title", "ゴルフ 設定", TitleFontSize, RowWidth, RowHeight);

            CreateText(box, "ModeLabel", "モード", LabelFontSize, RowWidth, RowHeight * 0.6f);
            Transform modeRow = CreateRow(box, "ModeRow");
            Button oneHole = CreateChoiceButton(modeRow, "Btn_OneHole", "1ホール", ModeButtonWidth);
            Button threeHole = CreateChoiceButton(modeRow, "Btn_ThreeHole", $"{GolfRules.LongModeHoleCount}ホール", ModeButtonWidth);

            CreateText(box, "CountLabel", "人数", LabelFontSize, RowWidth, RowHeight * 0.6f);
            Transform countRow = CreateRow(box, "CountRow");
            var countButtons = new Button[GolfSetupPanel.MaxPlayers - GolfSetupPanel.MinPlayers + 1];
            for (int i = 0; i < countButtons.Length; i++)
            {
                int count = GolfSetupPanel.MinPlayers + i;
                countButtons[i] = CreateChoiceButton(countRow, $"Btn_{count}Players", $"{count}人", CountButtonWidth);
            }

            Button start = CreatePrimaryButton(box, "Btn_Start", "試合開始").GetComponent<Button>();

            var panel = panelObj.AddComponent<GolfSetupPanel>();
            GolfSceneBuilder.SetRefs(panel, ("_oneHoleButton", oneHole), ("_threeHoleButton", threeHole),
                ("_startButton", start));
            SetArray(panel, "_countButtons", countButtons);

            panelObj.SetActive(false);
            return panel;
        }

        /// <summary>画面全体をボタンにして、どこをタップしても開始できるようにする</summary>
        public static GolfTurnBannerView CreateTurnBanner(Transform canvas)
        {
            GameObject bannerObj = UIDialogBuilder.CreateUIObject("TurnBanner", canvas);
            UIDialogBuilder.SetStretchAll(bannerObj.GetComponent<RectTransform>());
            var background = bannerObj.AddComponent<Image>();
            var tapArea = bannerObj.AddComponent<Button>();
            tapArea.transition = Selectable.Transition.None;

            Text detail = CreateText(bannerObj.transform, "DetailText", string.Empty, BannerDetailFontSize, 1000f, 300f);
            SetCenter(detail.rectTransform, 260f);
            Text title = CreateText(bannerObj.transform, "TitleText", string.Empty, BannerTitleFontSize, 1000f, 200f);
            SetCenter(title.rectTransform, 0f);
            title.gameObject.AddComponent<Outline>().effectDistance = new Vector2(4f, -4f);
            Text hint = CreateText(bannerObj.transform, "HintText", "タップで開始", BannerHintFontSize, 1000f, 90f);
            SetCenter(hint.rectTransform, -160f);

            var banner = bannerObj.AddComponent<GolfTurnBannerView>();
            GolfSceneBuilder.SetRefs(banner, ("_titleText", title), ("_detailText", detail), ("_tapArea", tapArea),
                ("_background", background));

            bannerObj.SetActive(false);
            return banner;
        }

        /// <summary>タイトル → 表（セルは ScoreCardView が実行時に作る）→ ボタン2つ（§13.4）</summary>
        public static ScoreCardView CreateScoreCard(Transform canvas)
        {
            GameObject panelObj = CreateDimmedPanel(canvas, "ScoreCard");
            Transform box = CreateBox(panelObj.transform, ScoreCardBoxHeight);

            Text title = CreateText(box, "Title", string.Empty, TitleFontSize, RowWidth, RowHeight);

            GameObject gridObj = UIDialogBuilder.CreateUIObject("Grid", box);
            gridObj.GetComponent<RectTransform>().sizeDelta = new Vector2(RowWidth, ScoreGridHeight);
            var grid = gridObj.AddComponent<GridLayoutGroup>();
            grid.childAlignment = TextAnchor.UpperCenter;

            Text cellTemplate = CreateText(gridObj.transform, "CellTemplate", string.Empty, ScoreCellFontSize, 100f, 80f);

            GameObject primaryObj = CreatePrimaryButton(box, "Btn_Primary", string.Empty);
            GameObject secondaryObj = UIDialogBuilder.CreateButton(box, "Btn_Title", "タイトルへ",
                PrimaryButtonWidth, RowHeight, ChoiceColor);
            secondaryObj.GetComponentInChildren<Text>().fontSize = ButtonFontSize;

            var view = panelObj.AddComponent<ScoreCardView>();
            GolfSceneBuilder.SetRefs(view, ("_titleText", title), ("_grid", grid), ("_cellTemplate", cellTemplate),
                ("_primaryButton", primaryObj.GetComponent<Button>()),
                ("_primaryLabel", primaryObj.GetComponentInChildren<Text>()),
                ("_secondaryButton", secondaryObj.GetComponent<Button>()));

            panelObj.SetActive(false);
            return view;
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
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(BoxWidth, height);
            boxObj.AddComponent<Image>().color = BoxColor;

            var layout = boxObj.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(40, 40, 40, 40);
            layout.spacing = 28f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            return boxObj.transform;
        }

        private static Transform CreateRow(Transform parent, string name)
        {
            GameObject rowObj = UIDialogBuilder.CreateUIObject(name, parent);
            rowObj.GetComponent<RectTransform>().sizeDelta = new Vector2(RowWidth, RowHeight);
            var layout = rowObj.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 20f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            return rowObj.transform;
        }

        private static Button CreateChoiceButton(Transform parent, string name, string label, float width)
        {
            GameObject obj = UIDialogBuilder.CreateButton(parent, name, label, width, RowHeight, ChoiceColor);
            obj.GetComponentInChildren<Text>().fontSize = ButtonFontSize;
            return obj.GetComponent<Button>();
        }

        private static GameObject CreatePrimaryButton(Transform parent, string name, string label)
        {
            GameObject obj = UIDialogBuilder.CreateButton(parent, name, label, PrimaryButtonWidth, PrimaryButtonHeight,
                PrimaryColor);
            obj.GetComponentInChildren<Text>().fontSize = PrimaryButtonFontSize;
            return obj;
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

        private static void SetCenter(RectTransform rect, float y)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, y);
        }

        private static void SetArray(Object target, string property, Object[] values)
        {
            var so = new SerializedObject(target);
            SerializedProperty array = so.FindProperty(property);
            array.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
