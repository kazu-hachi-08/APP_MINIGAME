using MiniGame.Editor;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.PenguinWars.Editor
{
    /// <summary>タイトルから開くキャラずかん（仕様書 §2.0）。マスはひな形を1つだけ作り、中身は実行時にカタログから並べる</summary>
    public static partial class PenguinWarsSceneBuilder
    {
        private static readonly Color ZukanBackColor = new Color(0.04f, 0.1f, 0.2f, 0.97f);
        private const int ZukanTitleFontSize = 80;
        private static readonly Vector2 ZukanTitlePosition = new Vector2(0f, -30f);
        private static readonly Vector2 ZukanTitleSize = new Vector2(800f, 110f);
        private static readonly Vector2 ZukanCloseSize = new Vector2(300f, 100f);
        private static readonly Vector2 ZukanClosePosition = new Vector2(-50f, -35f);

        // ヘッダーの下をすべてスクロール領域にする
        private const float ZukanSideMargin = 80f;
        private const float ZukanTopMargin = 170f;
        private const float ZukanBottomMargin = 40f;

        // 1920 幅に 8 列が収まる大きさ（50体で7行）
        private const int ZukanColumns = 8;
        private static readonly Vector2 ZukanCellSize = new Vector2(200f, 250f);
        private static readonly Vector2 ZukanCellSpacing = new Vector2(16f, 16f);
        private const int ZukanGridPadding = 10;
        private static readonly Color ZukanCellColor = new Color(0.16f, 0.24f, 0.38f, 0.95f);
        private static readonly Vector2 ZukanCellIconSize = new Vector2(170f, 170f);
        private static readonly Vector2 ZukanCellIconPosition = new Vector2(0f, -15f);
        private static readonly Vector2 ZukanCellNameSize = new Vector2(190f, 50f);
        private static readonly Vector2 ZukanCellNamePosition = new Vector2(0f, 12f);
        private const int ZukanCellNameFontSize = 26;
        // 「こおりのじょおうペンギン」のような長い名前も1行に収めるため、縮めてよい下限
        private const int ZukanCellNameMinFontSize = 14;

        private static readonly Color ZukanDimColor = new Color(0f, 0f, 0f, 0.7f);
        private static readonly Vector2 ZukanDetailBoxSize = new Vector2(1400f, 760f);
        private static readonly Color ZukanDetailBoxColor = new Color(0.12f, 0.14f, 0.18f);
        private static readonly Vector2 ZukanStagePosition = new Vector2(-370f, 40f);
        private static readonly Vector2 ZukanStageSize = new Vector2(520f, 520f);
        private static readonly Color ZukanStageColor = new Color(1f, 1f, 1f, 0.08f);
        private static readonly Vector2 ZukanDetailIconSize = new Vector2(480f, 480f);
        private static readonly Vector2 ZukanDetailNoPosition = new Vector2(300f, 300f);
        private static readonly Vector2 ZukanDetailNoSize = new Vector2(700f, 60f);
        private const int ZukanDetailNoFontSize = 40;
        private static readonly Vector2 ZukanDetailNamePosition = new Vector2(300f, 220f);
        private static readonly Vector2 ZukanDetailNameSize = new Vector2(700f, 100f);
        private const int ZukanDetailNameFontSize = 64;
        private static readonly Vector2 ZukanDetailStatsPosition = new Vector2(300f, -40f);
        private static readonly Vector2 ZukanDetailStatsSize = new Vector2(700f, 380f);
        private const int ZukanDetailStatsFontSize = 40;
        private const float ZukanDetailStatsLineSpacing = 1.25f;
        private static readonly Vector2 ZukanDetailClosePosition = new Vector2(0f, -310f);

        private static PenguinZukanPanel CreateZukanPanel(Transform parent, PenguinUnitCatalog catalog)
        {
            GameObject panelObj = UIDialogBuilder.CreateUIObject("ZukanPanel", parent);
            UIDialogBuilder.SetStretchAll(panelObj.GetComponent<RectTransform>());
            // raycastTarget を残し、ずかん中に下のタイトルのボタンが押せないようにする
            panelObj.AddComponent<Image>().color = ZukanBackColor;

            Text title = CreateText(panelObj.transform, "Title", ZukanTitleFontSize, TopCenterAnchor, ZukanTitlePosition, ZukanTitleSize, MessageColor);
            title.text = "ずかん";
            Button close = CreateAnchoredButton(panelObj.transform, "Btn_Close", "もどる", ZukanCloseSize, TopRightAnchor, ZukanClosePosition);

            (ScrollRect scroll, Transform content) = CreateZukanScroll(panelObj.transform);
            ZukanCell template = CreateZukanCellTemplate(content);
            ZukanDetailPanel detail = CreateZukanDetailPanel(panelObj.transform);

            var panel = panelObj.AddComponent<PenguinZukanPanel>();
            SetRefs(panel, ("_catalog", catalog), ("_cellTemplate", template), ("_scroll", scroll),
                ("_detailPanel", detail), ("_closeButton", close));
            panelObj.SetActive(false);
            return panel;
        }

        /// <summary>縦スクロールだけの一覧。Content の高さはグリッドの行数から自動で決まる</summary>
        private static (ScrollRect scroll, Transform content) CreateZukanScroll(Transform panel)
        {
            GameObject viewportObj = UIDialogBuilder.CreateUIObject("Viewport", panel);
            RectTransform viewport = viewportObj.GetComponent<RectTransform>();
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = new Vector2(ZukanSideMargin, ZukanBottomMargin);
            viewport.offsetMax = new Vector2(-ZukanSideMargin, -ZukanTopMargin);
            viewportObj.AddComponent<RectMask2D>();
            // マスの隙間でもドラッグでスクロールできるよう、透明な受け皿を置く
            viewportObj.AddComponent<Image>().color = Color.clear;

            GameObject contentObj = UIDialogBuilder.CreateUIObject("Content", viewport);
            RectTransform content = contentObj.GetComponent<RectTransform>();
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = Vector2.one;
            content.pivot = new Vector2(0.5f, 1f);
            content.sizeDelta = Vector2.zero;
            AddZukanGrid(contentObj);

            var scroll = viewportObj.AddComponent<ScrollRect>();
            scroll.content = content;
            scroll.viewport = viewport;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            return (scroll, content);
        }

        private static void AddZukanGrid(GameObject content)
        {
            var grid = content.AddComponent<GridLayoutGroup>();
            grid.cellSize = ZukanCellSize;
            grid.spacing = ZukanCellSpacing;
            grid.padding = new RectOffset(0, 0, ZukanGridPadding, ZukanGridPadding);
            grid.childAlignment = TextAnchor.UpperCenter;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = ZukanColumns;
            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        /// <summary>非表示のひな形。非表示の子はグリッドの並びに入らないので、そのまま Content の中に置ける</summary>
        private static ZukanCell CreateZukanCellTemplate(Transform content)
        {
            GameObject cellObj = UIDialogBuilder.CreateUIObject("CellTemplate", content);
            cellObj.AddComponent<Image>().color = ZukanCellColor;
            var button = cellObj.AddComponent<Button>();

            UnitSpriteAnimator icon = CreateAnimatedIcon(cellObj.transform, TopCenterAnchor, ZukanCellIconPosition, ZukanCellIconSize);
            Text name = CreateText(cellObj.transform, "Name", ZukanCellNameFontSize, BottomCenterAnchor, ZukanCellNamePosition, ZukanCellNameSize, Color.white);
            name.verticalOverflow = VerticalWrapMode.Truncate;
            name.resizeTextForBestFit = true;
            name.resizeTextMinSize = ZukanCellNameMinFontSize;
            name.resizeTextMaxSize = ZukanCellNameFontSize;

            var cell = cellObj.AddComponent<ZukanCell>();
            SetRefs(cell, ("_button", button), ("_icon", icon), ("_nameLabel", name));
            cellObj.SetActive(false);
            return cell;
        }

        private static ZukanDetailPanel CreateZukanDetailPanel(Transform parent)
        {
            GameObject dimObj = UIDialogBuilder.CreateUIObject("DetailPanel", parent);
            UIDialogBuilder.SetStretchAll(dimObj.GetComponent<RectTransform>());
            dimObj.AddComponent<Image>().color = ZukanDimColor;

            Image box = CreateImage(dimObj.transform, "Box", CenterAnchor, Vector2.zero, ZukanDetailBoxSize, ZukanDetailBoxColor);
            Image stage = CreateImage(box.transform, "Stage", CenterAnchor, ZukanStagePosition, ZukanStageSize, ZukanStageColor);
            UnitSpriteAnimator icon = CreateAnimatedIcon(stage.transform, CenterAnchor, Vector2.zero, ZukanDetailIconSize);

            Text no = CreateDetailText(box.transform, "No", ZukanDetailNoFontSize, ZukanDetailNoPosition, ZukanDetailNoSize, TitleSubColor);
            Text name = CreateDetailText(box.transform, "Name", ZukanDetailNameFontSize, ZukanDetailNamePosition, ZukanDetailNameSize, MessageColor);
            Text stats = CreateDetailText(box.transform, "Stats", ZukanDetailStatsFontSize, ZukanDetailStatsPosition, ZukanDetailStatsSize, Color.white);
            stats.alignment = TextAnchor.UpperLeft;
            stats.lineSpacing = ZukanDetailStatsLineSpacing;
            Button close = CreateAnchoredButton(box.transform, "Btn_Close", "閉じる", TitleSubButtonSize, CenterAnchor, ZukanDetailClosePosition);

            var panel = dimObj.AddComponent<ZukanDetailPanel>();
            SetRefs(panel, ("_icon", icon), ("_noLabel", no), ("_nameLabel", name), ("_statsLabel", stats), ("_closeButton", close));
            dimObj.SetActive(false);
            return panel;
        }

        private static UnitSpriteAnimator CreateAnimatedIcon(Transform parent, Vector2 anchor, Vector2 position, Vector2 size)
        {
            Image image = CreateImage(parent, "Icon", anchor, position, size, Color.white);
            // 大型ほど絵が大きいので、枠に合わせて縮めて縦横比だけ保つ
            image.preserveAspect = true;
            var animator = image.gameObject.AddComponent<UnitSpriteAnimator>();
            SetRefs(animator, ("_image", image));
            return animator;
        }

        private static Text CreateDetailText(Transform parent, string name, int fontSize, Vector2 position, Vector2 size, Color color)
        {
            Text text = CreateText(parent, name, fontSize, CenterAnchor, position, size, color);
            text.alignment = TextAnchor.MiddleLeft;
            return text;
        }

        private static Button CreateAnchoredButton(Transform parent, string name, string label, Vector2 size, Vector2 anchor, Vector2 position)
        {
            GameObject buttonObj = UIDialogBuilder.CreateButton(parent, name, label, size.x, size.y, TitleSubButtonColor);
            SetAnchor(buttonObj.GetComponent<RectTransform>(), anchor, position);
            buttonObj.GetComponentInChildren<Text>().fontSize = TitleButtonFontSize;
            return buttonObj.GetComponent<Button>();
        }
    }
}
