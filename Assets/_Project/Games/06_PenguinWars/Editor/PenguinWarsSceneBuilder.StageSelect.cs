using MiniGame.Editor;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.PenguinWars.Editor
{
    /// <summary>
    /// 一人用のステージ選択・詳細・リザルト（ステージ計画 Phase 2）。マスはひな形を1つだけ作り、中身は実行時に定義表から並べる。
    /// 対戦のステージ（PenguinStageData）は .Stages.cs で、こちらとは別物
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

        private static readonly Vector2 StageDetailBoxSize = new Vector2(1300f, 800f);
        private static readonly Vector2 StageDetailTitlePosition = new Vector2(0f, 310f);
        private static readonly Vector2 StageDetailTitleSize = new Vector2(1200f, 100f);
        private const int StageDetailTitleFontSize = 64;
        private static readonly Vector2 StageDetailDescriptionPosition = new Vector2(0f, 190f);
        private static readonly Vector2 StageDetailDescriptionSize = new Vector2(1150f, 120f);
        private const int StageDetailBodyFontSize = 38;
        private const float StageDetailConditionTopY = 70f;
        private const float StageDetailConditionStep = 70f;
        private static readonly Vector2 StageDetailConditionSize = new Vector2(1000f, 60f);
        private const int StageDetailConditionFontSize = 40;
        private static readonly Vector2 StageDetailBestPosition = new Vector2(0f, -170f);
        private static readonly Vector2 StageDetailBestSize = new Vector2(1000f, 60f);
        private static readonly Vector2 StageSortieButtonSize = new Vector2(480f, 120f);
        private static readonly Vector2 StageSortiePosition = new Vector2(230f, -300f);
        private static readonly Vector2 StageDetailClosePosition = new Vector2(-280f, -300f);

        private static readonly Vector2 StageResultBoxSize = new Vector2(1300f, 940f);
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

        private static StageSelectPanel CreateStageSelectPanel(Transform canvas)
        {
            GameObject panelObj = UIDialogBuilder.CreateUIObject("StageSelectPanel", canvas);
            UIDialogBuilder.SetStretchAll(panelObj.GetComponent<RectTransform>());
            // raycastTarget を残し、選択中に下の戦場・ボタンが押せないようにする
            panelObj.AddComponent<Image>().color = ZukanBackColor;

            Text title = CreateText(panelObj.transform, "Title", ZukanTitleFontSize, TopCenterAnchor, StageSelectTitlePosition, StageSelectTitleSize, MessageColor);
            title.text = "ステージ";
            Button back = CreateAnchoredButton(panelObj.transform, "Btn_Back", "もどる", ZukanCloseSize, TopRightAnchor, ZukanClosePosition);

            Text chapter = CreateText(panelObj.transform, "Chapter", StageChapterFontSize, CenterAnchor, StageChapterLabelPosition, StageChapterLabelSize, Color.white);
            Button prev = CreateAnchoredButton(panelObj.transform, "Btn_PrevChapter", "◀", StageChapterButtonSize, CenterAnchor,
                new Vector2(-StageChapterButtonX, StageChapterLabelPosition.y));
            Button next = CreateAnchoredButton(panelObj.transform, "Btn_NextChapter", "▶", StageChapterButtonSize, CenterAnchor,
                new Vector2(StageChapterButtonX, StageChapterLabelPosition.y));

            Transform row = CreateStageRow(panelObj.transform);
            StageNode template = CreateStageNodeTemplate(row);
            StageDetailPanel detail = CreateStageDetailPanel(panelObj.transform);

            var panel = panelObj.AddComponent<StageSelectPanel>();
            SetRefs(panel, ("_nodeTemplate", template), ("_chapterLabel", chapter), ("_prevChapterButton", prev),
                ("_nextChapterButton", next), ("_backButton", back), ("_detailPanel", detail));
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

        private static StageDetailPanel CreateStageDetailPanel(Transform parent)
        {
            GameObject dimObj = UIDialogBuilder.CreateUIObject("DetailPanel", parent);
            UIDialogBuilder.SetStretchAll(dimObj.GetComponent<RectTransform>());
            dimObj.AddComponent<Image>().color = ZukanDimColor;
            Image box = CreateImage(dimObj.transform, "Box", CenterAnchor, Vector2.zero, StageDetailBoxSize, ZukanDetailBoxColor);

            Text title = CreateText(box.transform, "Title", StageDetailTitleFontSize, CenterAnchor, StageDetailTitlePosition, StageDetailTitleSize, MessageColor);
            Text description = CreateText(box.transform, "Description", StageDetailBodyFontSize, CenterAnchor, StageDetailDescriptionPosition, StageDetailDescriptionSize, Color.white);
            var conditions = new Text[StageLabels.StarOrder.Length];
            for (int i = 0; i < conditions.Length; i++)
            {
                var position = new Vector2(0f, StageDetailConditionTopY - i * StageDetailConditionStep);
                conditions[i] = CreateDetailText(box.transform, $"Condition{i + 1}", StageDetailConditionFontSize, position, StageDetailConditionSize, Color.white);
            }
            Text best = CreateText(box.transform, "Best", StageDetailConditionFontSize, CenterAnchor, StageDetailBestPosition, StageDetailBestSize, TitleSubColor);
            Button sortie = CreateTitleButton(box.transform, "Btn_Sortie", "しゅつげき", StageSortieButtonSize, StageSortiePosition, StartButtonColor);
            Button close = CreateTitleButton(box.transform, "Btn_Close", "閉じる", TitleSubButtonSize, StageDetailClosePosition, TitleSubButtonColor);

            var panel = dimObj.AddComponent<StageDetailPanel>();
            var so = new UnityEditor.SerializedObject(panel);
            SerializedArray(so, "_conditionLabels", conditions);
            so.ApplyModifiedPropertiesWithoutUndo();
            SetRefs(panel, ("_titleLabel", title), ("_descriptionLabel", description), ("_bestLabel", best),
                ("_sortieButton", sortie), ("_closeButton", close));
            dimObj.SetActive(false);
            return panel;
        }

        /// <summary>操作UI・HUDより手前、ポーズ・共通ダイアログより奥に作る</summary>
        private static StageResultPanel CreateStageResultPanel(Transform canvas)
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

            var panel = dimObj.AddComponent<StageResultPanel>();
            SetRefs(panel, ("_titleLabel", title), ("_starsLabel", stars), ("_detailLabel", detail),
                ("_nextButton", next), ("_retryButton", retry), ("_selectButton", select));
            dimObj.SetActive(false);
            return panel;
        }
    }
}
