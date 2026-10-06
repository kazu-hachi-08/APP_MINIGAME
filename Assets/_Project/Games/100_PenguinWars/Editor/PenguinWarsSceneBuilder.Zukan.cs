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

        // 並べ替えの2つは左上、特性は「もどる」の左隣。中央のタイトル文字とは重ならない幅にする
        private static readonly Vector2 ZukanColumnDropdownSize = new Vector2(240f, 100f);
        private static readonly Vector2 ZukanColumnDropdownPosition = new Vector2(50f, -35f);
        private static readonly Vector2 ZukanOrderButtonSize = new Vector2(240f, 100f);
        private static readonly Vector2 ZukanOrderButtonPosition = new Vector2(310f, -35f);
        private static readonly Vector2 ZukanTraitDropdownSize = new Vector2(380f, 100f);
        private static readonly Vector2 ZukanTraitDropdownPosition = new Vector2(-370f, -35f);
        // 開いた一覧。指で押しやすい行の高さにし、特性14個のうち7個ほどが一度に見える長さにする
        private const float ZukanDropdownItemHeight = 84f;
        private const float ZukanDropdownListHeight = 600f;
        private const int ZukanDropdownItemFontSize = 40;
        private static readonly Color ZukanDropdownListColor = new Color(0.1f, 0.16f, 0.28f, 0.98f);
        // 「特性: 遠距離キラー」も1行に収めるため、縮めてよい下限
        private const int ZukanHeaderButtonMinFontSize = 24;

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
        // 左上の数値。歩く絵に重なっても読めるよう、半透明の黒い下地に乗せる
        private static readonly Vector2 ZukanCellValueSize = new Vector2(96f, 46f);
        private static readonly Vector2 ZukanCellValuePosition = new Vector2(6f, -6f);
        private static readonly Color ZukanCellValueBackColor = new Color(0f, 0f, 0f, 0.55f);
        private const int ZukanCellValueFontSize = 34;
        private const int ZukanCellValueMinFontSize = 18;
        private static readonly Color ZukanCellDimColor = new Color(0f, 0f, 0f, 0.6f);

        private static readonly Vector2 ZukanDetailBoxSize = new Vector2(1400f, 760f);
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
            // raycastTarget を残し、ずかん中に下のタイトルのボタンが押せないようにする
            GameObject panelObj = CreateFullScreenPanel(parent, "ZukanPanel", ZukanBackColor);

            Text title = CreateText(panelObj.transform, "Title", ZukanTitleFontSize, TopCenterAnchor, ZukanTitlePosition, ZukanTitleSize, MessageColor);
            title.text = "ずかん";
            Button close = CreateAnchoredButton(panelObj.transform, "Btn_Close", "もどる", ZukanCloseSize, TopRightAnchor, ZukanClosePosition);
            // 文字は PenguinZukanPanel が開くたびに書き換えるので、ここでは仮の文字
            Button order = CreateZukanHeaderButton(panelObj.transform, "Btn_Order", ZukanOrderButtonSize, TopLeftAnchor, ZukanOrderButtonPosition);

            (ScrollRect scroll, Transform content) = CreateZukanScroll(panelObj.transform, ZukanTopMargin, ZukanColumns);
            ZukanCell template = CreateZukanCellTemplate(content);
            // 開いた一覧がマスより手前に出るよう、スクロール領域より後に作る
            Dropdown column = CreateZukanDropdown(panelObj.transform, "Dropdown_Column", ZukanColumnDropdownSize, TopLeftAnchor, ZukanColumnDropdownPosition);
            Dropdown trait = CreateZukanDropdown(panelObj.transform, "Dropdown_Trait", ZukanTraitDropdownSize, TopRightAnchor, ZukanTraitDropdownPosition);
            ZukanDetailPanel detail = CreateZukanDetailPanel(panelObj.transform);

            var panel = panelObj.AddComponent<PenguinZukanPanel>();
            SetRefs(panel, ("_catalog", catalog), ("_cellTemplate", template), ("_scroll", scroll),
                ("_detailPanel", detail), ("_closeButton", close),
                ("_columnDropdown", column), ("_orderButton", order), ("_traitDropdown", trait));
            panelObj.SetActive(false);
            return panel;
        }

        /// <summary>縦スクロールだけの一覧（編成画面でも使う）。Content の高さはグリッドの行数から自動で決まる</summary>
        private static (ScrollRect scroll, Transform content) CreateZukanScroll(Transform panel, float topMargin, int columns)
        {
            GameObject viewportObj = UIDialogBuilder.CreateUIObject("Viewport", panel);
            RectTransform viewport = viewportObj.GetComponent<RectTransform>();
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = new Vector2(ZukanSideMargin, ZukanBottomMargin);
            viewport.offsetMax = new Vector2(-ZukanSideMargin, -topMargin);
            viewportObj.AddComponent<RectMask2D>();
            // マスの隙間でもドラッグでスクロールできるよう、透明な受け皿を置く
            viewportObj.AddComponent<Image>().color = Color.clear;

            GameObject contentObj = UIDialogBuilder.CreateUIObject("Content", viewport);
            RectTransform content = contentObj.GetComponent<RectTransform>();
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = Vector2.one;
            content.pivot = new Vector2(0.5f, 1f);
            content.sizeDelta = Vector2.zero;
            AddZukanGrid(contentObj, columns);

            var scroll = viewportObj.AddComponent<ScrollRect>();
            scroll.content = content;
            scroll.viewport = viewport;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            return (scroll, content);
        }

        private static void AddZukanGrid(GameObject content, int columns)
        {
            var grid = content.AddComponent<GridLayoutGroup>();
            grid.cellSize = ZukanCellSize;
            grid.spacing = ZukanCellSpacing;
            grid.padding = new RectOffset(0, 0, ZukanGridPadding, ZukanGridPadding);
            grid.childAlignment = TextAnchor.UpperCenter;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = columns;
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
            FitText(name, ZukanCellNameMinFontSize);
            Text value = CreateZukanCellValue(cellObj.transform);
            // 編成画面で「もう枠に入れた」キャラを暗くする幕。ずかんでは使わない
            Image dimmer = CreateImage(cellObj.transform, "Dimmer", CenterAnchor, Vector2.zero, ZukanCellSize, ZukanCellDimColor);
            dimmer.gameObject.SetActive(false);

            var cell = cellObj.AddComponent<ZukanCell>();
            SetRefs(cell, ("_button", button), ("_icon", icon), ("_nameLabel", name), ("_valueLabel", value), ("_dimmer", dimmer.gameObject));
            cellObj.SetActive(false);
            return cell;
        }

        /// <summary>絵より後に作って手前に描く。「12.5」のような小数も枠に収まるよう縮めてよくする</summary>
        private static Text CreateZukanCellValue(Transform cell)
        {
            Image back = CreateImage(cell, "ValueBack", TopLeftAnchor, ZukanCellValuePosition, ZukanCellValueSize, ZukanCellValueBackColor);
            Text value = CreateText(back.transform, "Value", ZukanCellValueFontSize, CenterAnchor, Vector2.zero, ZukanCellValueSize, Color.white);
            FitText(value, ZukanCellValueMinFontSize);
            return value;
        }

        private static Button CreateZukanHeaderButton(Transform parent, string name, Vector2 size, Vector2 anchor, Vector2 position)
        {
            Button button = CreateAnchoredButton(parent, name, name, size, anchor, position);
            // ボタンの文字は元から Truncate なので、FitText で縮めても見た目は変わらない
            FitText(button.GetComponentInChildren<Text>(), ZukanHeaderButtonMinFontSize);
            return button;
        }

        /// <summary>Unity 標準のドロップダウンを、ヘッダーのボタンと同じ色・文字の大きさに揃える</summary>
        private static Dropdown CreateZukanDropdown(Transform parent, string name, Vector2 size, Vector2 anchor, Vector2 position)
        {
            GameObject obj = DefaultControls.CreateDropdown(new DefaultControls.Resources());
            obj.name = name;
            obj.transform.SetParent(parent, false);
            RectTransform rect = obj.GetComponent<RectTransform>();
            SetAnchor(rect, anchor, position);
            rect.sizeDelta = size;
            obj.GetComponent<Image>().color = TitleSubButtonColor;
            // 絵の素材を渡していないので矢印が白い四角になる。ほかのボタンと見た目を揃えるため消す
            Object.DestroyImmediate(obj.transform.Find("Arrow").gameObject);

            var dropdown = obj.GetComponent<Dropdown>();
            StyleZukanDropdownCaption(dropdown.captionText);
            StyleZukanDropdownList(dropdown);
            return dropdown;
        }

        private static void StyleZukanDropdownCaption(Text caption)
        {
            UIDialogBuilder.SetStretchAll(caption.rectTransform);
            caption.fontSize = TitleButtonFontSize;
            caption.fontStyle = FontStyle.Bold;
            caption.alignment = TextAnchor.MiddleCenter;
            caption.color = Color.white;
            FitText(caption, ZukanHeaderButtonMinFontSize);
        }

        /// <summary>開いた一覧の行は実行時に Item を複製して作られるので、ひな形の Item の大きさ・色を変えておく</summary>
        private static void StyleZukanDropdownList(Dropdown dropdown)
        {
            RectTransform template = dropdown.template;
            template.sizeDelta = new Vector2(0f, ZukanDropdownListHeight);
            template.GetComponent<Image>().color = ZukanDropdownListColor;
            template.GetComponent<ScrollRect>().content.sizeDelta = new Vector2(0f, ZukanDropdownItemHeight);

            var item = (RectTransform)dropdown.itemText.transform.parent;
            item.sizeDelta = new Vector2(0f, ZukanDropdownItemHeight);
            item.Find("Item Background").GetComponent<Image>().color = ZukanCellColor;
            item.Find("Item Checkmark").GetComponent<Image>().color = MessageColor;

            dropdown.itemText.fontSize = ZukanDropdownItemFontSize;
            dropdown.itemText.fontStyle = FontStyle.Bold;
            dropdown.itemText.color = Color.white;
        }

        private static ZukanDetailPanel CreateZukanDetailPanel(Transform parent)
        {
            GameObject dimObj = CreateFullScreenPanel(parent, "DetailPanel", DialogDimColor);

            Image box = CreateImage(dimObj.transform, "Box", CenterAnchor, Vector2.zero, ZukanDetailBoxSize, DialogBoxColor);
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
    }
}
