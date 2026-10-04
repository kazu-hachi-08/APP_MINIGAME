using MiniGame.Editor;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.PenguinWars.Editor
{
    /// <summary>試合前の編成発表パネル（仕様書 §2.1）。出撃ボタンと同じ 5体×2段で並べる</summary>
    public static partial class PenguinWarsSceneBuilder
    {
        private const int IntroColumns = 5;
        private const int IntroRows = 2;
        private static readonly Color IntroBackColor = new Color(0f, 0.05f, 0.15f, 0.8f);

        private const int IntroTitleFontSize = 90;
        private static readonly Vector2 IntroTitleSize = new Vector2(1000f, 120f);
        private static readonly Vector2 IntroTitlePosition = new Vector2(0f, -110f);

        private static readonly Vector2 IntroCellSize = new Vector2(300f, 240f);
        // 2段の中心を画面中央より少し下にして、タイトルと重ならないようにする
        private const float IntroGridOffsetY = -60f;
        private static readonly Vector2 IntroIconSize = new Vector2(130f, 130f);
        private static readonly Vector2 IntroIconPosition = new Vector2(0f, 30f);
        private const int IntroNameFontSize = 34;
        private const int IntroNameMinFontSize = 16;
        private static readonly Vector2 IntroNameSize = new Vector2(290f, 50f);
        private static readonly Vector2 IntroNamePosition = new Vector2(0f, -75f);

        private static DeckIntroPanel CreateDeckIntroPanel(Transform canvas, PenguinUnitCatalog catalog)
        {
            GameObject panelObj = UIDialogBuilder.CreateUIObject("DeckIntroPanel", canvas);
            UIDialogBuilder.SetStretchAll(panelObj.GetComponent<RectTransform>());
            // 背景は raycastTarget を残し、発表中に下の出撃ボタンが押せないようにする
            panelObj.AddComponent<Image>().color = IntroBackColor;

            CreateText(panelObj.transform, "Title", IntroTitleFontSize, TopCenterAnchor, IntroTitlePosition, IntroTitleSize, MessageColor).text = "今回の編成";

            int cellCount = IntroColumns * IntroRows;
            var icons = new Image[cellCount];
            var names = new Text[cellCount];
            for (int i = 0; i < cellCount; i++)
            {
                (icons[i], names[i]) = CreateIntroCell(panelObj.transform, i);
            }

            var panel = panelObj.AddComponent<DeckIntroPanel>();
            var so = new UnityEditor.SerializedObject(panel);
            SerializedArray(so, "_icons", icons);
            SerializedArray(so, "_names", names);
            so.ApplyModifiedPropertiesWithoutUndo();
            SetRefs(panel, ("_catalog", catalog));
            panelObj.SetActive(false);
            return panel;
        }

        private static (Image icon, Text name) CreateIntroCell(Transform panel, int index)
        {
            int column = index % IntroColumns;
            int row = index / IntroColumns;
            float x = (column - (IntroColumns - 1) * 0.5f) * IntroCellSize.x;
            float y = ((IntroRows - 1) * 0.5f - row) * IntroCellSize.y + IntroGridOffsetY;

            GameObject cellObj = UIDialogBuilder.CreateUIObject($"Cell{index + 1}", panel);
            RectTransform cellRect = cellObj.GetComponent<RectTransform>();
            SetAnchor(cellRect, CenterAnchor, new Vector2(x, y));
            cellRect.sizeDelta = IntroCellSize;

            Image icon = CreateImage(cellObj.transform, "Icon", CenterAnchor, IntroIconPosition, IntroIconSize, Color.white);
            icon.preserveAspect = true;
            Text name = CreateText(cellObj.transform, "Name", IntroNameFontSize, CenterAnchor, IntroNamePosition, IntroNameSize, Color.white);
            // 長い名前でも枠に収める（出撃ボタンと同じ理由）
            name.verticalOverflow = VerticalWrapMode.Truncate;
            name.resizeTextForBestFit = true;
            name.resizeTextMinSize = IntroNameMinFontSize;
            name.resizeTextMaxSize = IntroNameFontSize;
            return (icon, name);
        }
    }
}
