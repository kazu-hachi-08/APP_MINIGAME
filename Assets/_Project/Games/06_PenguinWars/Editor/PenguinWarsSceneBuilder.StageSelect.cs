using MiniGame.Editor;
using MiniGame.PenguinWars.Battle;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.PenguinWars.Editor
{
    /// <summary>
    /// 一人用のステージ選択・詳細・リザルト（ステージ計画 Phase 2）。マスはひな形を1つだけ作り、中身は実行時に定義表から並べる。
    /// 対戦のステージ（PenguinStageData）は .Stages.cs で、こちらとは別物。ステージ詳細は .StageDetail.cs
    /// </summary>
    public static partial class PenguinWarsSceneBuilder
    {
        private static readonly Vector2 StageSelectTitlePosition = new Vector2(0f, -30f);
        private static readonly Vector2 StageSelectTitleSize = new Vector2(800f, 110f);
        private static readonly Vector2 StageChapterLabelPosition = new Vector2(0f, 230f);
        private static readonly Vector2 StageChapterLabelSize = new Vector2(600f, 110f);
        private const int StageChapterFontSize = 72;
        private static readonly Vector2 StageChapterButtonSize = new Vector2(160f, 110f);
        private const float StageChapterButtonX = 380f;

        // 1章6マスが 1920 幅に収まる大きさ。ボスは StageNode が 1.2 倍にするので、その分の隙間をあける
        private static readonly Vector2 StageRowSize = new Vector2(1700f, 320f);
        private static readonly Vector2 StageRowPosition = new Vector2(0f, -40f);
        private const float StageNodeSpacing = 60f;
        private static readonly Vector2 StageNodeSize = new Vector2(210f, 210f);
        private static readonly Vector2 StageNodeIdPosition = new Vector2(0f, 25f);
        private static readonly Vector2 StageNodeIdSize = new Vector2(200f, 100f);
        private const int StageNodeIdFontSize = 64;
        private static readonly Vector2 StageNodeStarsPosition = new Vector2(0f, 20f);
        private static readonly Vector2 StageNodeStarsSize = new Vector2(200f, 60f);
        private const int StageNodeStarsFontSize = 44;
        private const int StageNodeLockFontSize = 36;
        private static readonly Vector2 StageNodeBossPosition = new Vector2(0f, 45f);
        private static readonly Vector2 StageNodeBossSize = new Vector2(200f, 50f);
        private const int StageNodeBossFontSize = 36;
        private static readonly Color StageBossLabelColor = new Color(1f, 0.4f, 0.35f);

        // 「へんせい」は左上（ずかんの並べ替えと同じ位置）
        private static readonly Vector2 StageDeckButtonSize = new Vector2(300f, 100f);
        private static readonly Vector2 StageDeckButtonPosition = new Vector2(50f, -35f);

        // 右側に「なかまになった！」の列を置ける幅
        private static readonly Vector2 StageResultBoxSize = new Vector2(1800f, 940f);
        private static readonly Vector2 StageResultTitlePosition = new Vector2(0f, 360f);
        private static readonly Vector2 StageResultTitleSize = new Vector2(1200f, 140f);
        private const int StageResultTitleFontSize = 110;
        private static readonly Vector2 StageResultStarsPosition = new Vector2(0f, 240f);
        private static readonly Vector2 StageResultStarsSize = new Vector2(600f, 110f);
        private const int StageResultStarsFontSize = 96;
        // 「ステージ名・タイム・撃破・新しい★（最大3行）」の8行ほどが入る高さ
        private static readonly Vector2 StageResultDetailPosition = new Vector2(0f, -30f);
        private static readonly Vector2 StageResultDetailSize = new Vector2(1200f, 400f);
        private const int StageResultDetailFontSize = 38;
        private static readonly Vector2 StageResultButtonSize = new Vector2(380f, 110f);
        private const float StageResultButtonY = -380f;
        private const float StageResultButtonStepX = 410f;

        private const float StageResultDetailShiftWithUnlocks = -330f;
        private const int StageResultUnlockSlots = 4;
        private static readonly Vector2 StageResultUnlockTitlePosition = new Vector2(560f, 170f);
        private static readonly Vector2 StageResultUnlockTitleSize = new Vector2(620f, 80f);
        private const int StageResultUnlockTitleFontSize = 52;
        private const float StageResultUnlockFirstX = 365f;
        private const float StageResultUnlockStepX = 130f;
        private const float StageResultUnlockIconY = 30f;
        private static readonly Vector2 StageResultUnlockIconSize = new Vector2(124f, 124f);
        private const float StageResultUnlockNameY = -70f;
        private static readonly Vector2 StageResultUnlockNameSize = new Vector2(128f, 70f);
        private const int StageResultUnlockNameFontSize = 24;
        private const int StageResultUnlockNameMinFontSize = 12;

        private static StageSelectPanel CreateStageSelectPanel(Transform canvas, PenguinUnitCatalog catalog)
        {
            GameObject panelObj = UIDialogBuilder.CreateUIObject("StageSelectPanel", canvas);
            UIDialogBuilder.SetStretchAll(panelObj.GetComponent<RectTransform>());
            // raycastTarget を残し、選択中に下の戦場・ボタンが押せないようにする
            panelObj.AddComponent<Image>().color = ZukanBackColor;

            Text title = CreateText(panelObj.transform, "Title", ZukanTitleFontSize, TopCenterAnchor, StageSelectTitlePosition, StageSelectTitleSize, MessageColor);
            title.text = "ステージ";
            Button back = CreateAnchoredButton(panelObj.transform, "Btn_Back", "もどる", ZukanCloseSize, TopRightAnchor, ZukanClosePosition);
            Button deck = CreateAnchoredButton(panelObj.transform, "Btn_Deck", "へんせい", StageDeckButtonSize, ZukanTopLeftAnchor, StageDeckButtonPosition);

            Text chapter = CreateText(panelObj.transform, "Chapter", StageChapterFontSize, CenterAnchor, StageChapterLabelPosition, StageChapterLabelSize, Color.white);
            Button prev = CreateAnchoredButton(panelObj.transform, "Btn_PrevChapter", "◀", StageChapterButtonSize, CenterAnchor,
                new Vector2(-StageChapterButtonX, StageChapterLabelPosition.y));
            Button next = CreateAnchoredButton(panelObj.transform, "Btn_NextChapter", "▶", StageChapterButtonSize, CenterAnchor,
                new Vector2(StageChapterButtonX, StageChapterLabelPosition.y));

            Transform row = CreateStageRow(panelObj.transform);
            StageNode template = CreateStageNodeTemplate(row);
            StageDetailPanel detail = CreateStageDetailPanel(panelObj.transform, catalog);
            // 詳細から開くこともあるので、詳細より手前に作る
            DeckEditPanel deckEdit = CreateDeckEditPanel(panelObj.transform, catalog);

            var panel = panelObj.AddComponent<StageSelectPanel>();
            SetRefs(panel, ("_nodeTemplate", template), ("_chapterLabel", chapter), ("_prevChapterButton", prev),
                ("_nextChapterButton", next), ("_backButton", back), ("_detailPanel", detail),
                ("_deckButton", deck), ("_deckEditPanel", deckEdit));
            panelObj.SetActive(false);
            return panel;
        }

        /// <summary>マスを横一列に中央寄せで並べる。非表示のひな形は並びに入らない</summary>
        private static Transform CreateStageRow(Transform panel)
        {
            GameObject rowObj = UIDialogBuilder.CreateUIObject("Row", panel);
            RectTransform rect = rowObj.GetComponent<RectTransform>();
            SetAnchor(rect, CenterAnchor, StageRowPosition);
            rect.sizeDelta = StageRowSize;

            var layout = rowObj.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = StageNodeSpacing;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            return rowObj.transform;
        }

        private static StageNode CreateStageNodeTemplate(Transform row)
        {
            GameObject nodeObj = UIDialogBuilder.CreateUIObject("NodeTemplate", row);
            nodeObj.GetComponent<RectTransform>().sizeDelta = StageNodeSize;
            Image background = nodeObj.AddComponent<Image>();
            var button = nodeObj.AddComponent<Button>();

            Text id = CreateText(nodeObj.transform, "Id", StageNodeIdFontSize, CenterAnchor, StageNodeIdPosition, StageNodeIdSize, Color.white);
            Text stars = CreateText(nodeObj.transform, "Stars", StageNodeStarsFontSize, BottomCenterAnchor, StageNodeStarsPosition, StageNodeStarsSize, MessageColor);
            Text lockMark = CreateText(nodeObj.transform, "Lock", StageNodeLockFontSize, BottomCenterAnchor, StageNodeStarsPosition, StageNodeStarsSize, TitleSubColor);
            lockMark.text = "ロック";
            // マスの上にはみ出して出し、ボスステージだとひと目で分かるようにする
            Text boss = CreateText(nodeObj.transform, "Boss", StageNodeBossFontSize, TopCenterAnchor, StageNodeBossPosition, StageNodeBossSize, StageBossLabelColor);
            boss.text = "BOSS";

            var node = nodeObj.AddComponent<StageNode>();
            SetRefs(node, ("_button", button), ("_background", background), ("_idLabel", id), ("_starsLabel", stars),
                ("_lockMark", lockMark.gameObject), ("_bossMark", boss.gameObject));
            nodeObj.SetActive(false);
            return node;
        }

        /// <summary>操作UI・HUDより手前、ポーズ・共通ダイアログより奥に作る</summary>
        private static StageResultPanel CreateStageResultPanel(Transform canvas, PenguinUnitCatalog catalog)
        {
            GameObject dimObj = UIDialogBuilder.CreateUIObject("StageResultPanel", canvas);
            UIDialogBuilder.SetStretchAll(dimObj.GetComponent<RectTransform>());
            dimObj.AddComponent<Image>().color = ZukanDimColor;
            Image box = CreateImage(dimObj.transform, "Box", CenterAnchor, Vector2.zero, StageResultBoxSize, ZukanDetailBoxColor);

            Text title = CreateText(box.transform, "Title", StageResultTitleFontSize, CenterAnchor, StageResultTitlePosition, StageResultTitleSize, MessageColor);
            Text stars = CreateText(box.transform, "Stars", StageResultStarsFontSize, CenterAnchor, StageResultStarsPosition, StageResultStarsSize, MessageColor);
            Text detail = CreateText(box.transform, "Detail", StageResultDetailFontSize, CenterAnchor, StageResultDetailPosition, StageResultDetailSize, Color.white);
            detail.alignment = TextAnchor.UpperCenter;

            // 左から「ステージ選択」「もういちど」「つぎのステージ」。いちばん押してほしい「つぎ」を右端に目立つ色で置く
            Button select = CreateTitleButton(box.transform, "Btn_StageSelect", "ステージ選択", StageResultButtonSize,
                new Vector2(-StageResultButtonStepX, StageResultButtonY), TitleSubButtonColor);
            Button retry = CreateTitleButton(box.transform, "Btn_Retry", "もういちど", StageResultButtonSize,
                new Vector2(0f, StageResultButtonY), TitleSubButtonColor);
            Button next = CreateTitleButton(box.transform, "Btn_Next", "つぎのステージ", StageResultButtonSize,
                new Vector2(StageResultButtonStepX, StageResultButtonY), StartButtonColor);

            (GameObject unlockGroup, Image[] unlockIcons, Text[] unlockNames) = CreateStageResultUnlocks(box.transform);

            var panel = dimObj.AddComponent<StageResultPanel>();
            var so = new UnityEditor.SerializedObject(panel);
            SerializedArray(so, "_unlockIcons", unlockIcons);
            SerializedArray(so, "_unlockNames", unlockNames);
            so.FindProperty("_detailShiftWithUnlocks").floatValue = StageResultDetailShiftWithUnlocks;
            so.ApplyModifiedPropertiesWithoutUndo();
            SetRefs(panel, ("_titleLabel", title), ("_starsLabel", stars), ("_detailLabel", detail),
                ("_nextButton", next), ("_retryButton", retry), ("_selectButton", select),
                ("_catalog", catalog), ("_unlockGroup", unlockGroup));
            dimObj.SetActive(false);
            return panel;
        }

        /// <summary>リザルトの右側。見出しの下に絵と名前を横に並べる</summary>
        private static (GameObject group, Image[] icons, Text[] names) CreateStageResultUnlocks(Transform box)
        {
            GameObject group = UIDialogBuilder.CreateUIObject("Unlocks", box);
            UIDialogBuilder.SetStretchAll(group.GetComponent<RectTransform>());
            Text title = CreateText(group.transform, "Title", StageResultUnlockTitleFontSize, CenterAnchor, StageResultUnlockTitlePosition, StageResultUnlockTitleSize, MessageColor);
            title.text = "なかまになった！";

            var icons = new Image[StageResultUnlockSlots];
            var names = new Text[StageResultUnlockSlots];
            for (int i = 0; i < StageResultUnlockSlots; i++)
            {
                float x = StageResultUnlockFirstX + i * StageResultUnlockStepX;
                icons[i] = CreateImage(group.transform, $"Icon{i + 1}", CenterAnchor, new Vector2(x, StageResultUnlockIconY), StageResultUnlockIconSize, Color.white);
                icons[i].preserveAspect = true;
                names[i] = CreateText(group.transform, $"Name{i + 1}", StageResultUnlockNameFontSize, CenterAnchor, new Vector2(x, StageResultUnlockNameY), StageResultUnlockNameSize, Color.white);
                // 「こおりのじょおうペンギン」も枠に収まるよう、縮めて折り返す（Overflow のままだと縮まない）
                names[i].verticalOverflow = VerticalWrapMode.Truncate;
                names[i].resizeTextForBestFit = true;
                names[i].resizeTextMinSize = StageResultUnlockNameMinFontSize;
                names[i].resizeTextMaxSize = StageResultUnlockNameFontSize;
            }
            group.SetActive(false);
            return (group, icons, names);
        }
    }
}
