using MiniGame.Editor;
using MiniGame.PenguinWars.Battle;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.PenguinWars.Editor
{
    /// <summary>
    /// 一人用のステージ選択（ステージ計画 Phase 2）。マスはひな形を1つだけ作り、中身は実行時に定義表から並べる。
    /// 対戦のステージ（PenguinStageData）は .Stages.cs で、こちらとは別物。ステージ詳細は .StageDetail.cs、リザルトは .StageResult.cs
    /// </summary>
    public static partial class PenguinWarsSceneBuilder
    {
        private static readonly Vector2 StageSelectTitlePosition = new Vector2(0f, -30f);
        private static readonly Vector2 StageSelectTitleSize = new Vector2(800f, 110f);
        private static readonly Vector2 StageChapterLabelPosition = new Vector2(0f, 230f);
        // 「第2章 ゆきやまの奥」が1行に収まる幅
        private static readonly Vector2 StageChapterLabelSize = new Vector2(900f, 110f);
        private const int StageChapterFontSize = 64;
        private static readonly Vector2 StageChapterButtonSize = new Vector2(160f, 110f);
        private const float StageChapterButtonX = 560f;

        // 章送りの題字: マスの列の上に大きく重ねる
        private static readonly Vector2 StageChapterBannerSize = new Vector2(1920f, 260f);
        private static readonly Color StageChapterBannerColor = new Color(0f, 0f, 0f, 0.75f);
        private const int StageChapterBannerFontSize = 110;

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
            // 章送りの題字は詳細・編成より手前（章送りの途中で開かれても題字が隠れないように）
            (CanvasGroup chapterBanner, Text chapterBannerLabel) = CreateChapterBanner(panelObj.transform);

            var panel = panelObj.AddComponent<StageSelectPanel>();
            SetRefs(panel, ("_nodeTemplate", template), ("_chapterLabel", chapter), ("_prevChapterButton", prev),
                ("_nextChapterButton", next), ("_backButton", back), ("_detailPanel", detail),
                ("_deckButton", deck), ("_deckEditPanel", deckEdit));
            SetRefs(panel, ("_chapterBanner", chapterBanner), ("_chapterBannerLabel", chapterBannerLabel));
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

        /// <summary>題字は押しても何も起きない（raycastTarget を切り、下のマスを押せるままにする）</summary>
        private static (CanvasGroup group, Text label) CreateChapterBanner(Transform panel)
        {
            Image back = CreateImage(panel, "ChapterBanner", CenterAnchor, StageRowPosition, StageChapterBannerSize, StageChapterBannerColor);
            var group = back.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            Text label = CreateText(back.transform, "Label", StageChapterBannerFontSize, CenterAnchor, Vector2.zero, StageChapterBannerSize, MessageColor);
            back.gameObject.SetActive(false);
            return (group, label);
        }

        /// <summary>
        /// 並び（HorizontalLayoutGroup）が位置を決めるのは外側の NodeTemplate だけ。見た目とボタンは子の Body に置き、
        /// StageNode が Body だけをふわふわ動かせるようにする
        /// </summary>
        private static StageNode CreateStageNodeTemplate(Transform row)
        {
            GameObject nodeObj = UIDialogBuilder.CreateUIObject("NodeTemplate", row);
            nodeObj.GetComponent<RectTransform>().sizeDelta = StageNodeSize;
            GameObject bodyObj = UIDialogBuilder.CreateUIObject("Body", nodeObj.transform);
            UIDialogBuilder.SetStretchAll(bodyObj.GetComponent<RectTransform>());
            Image background = bodyObj.AddComponent<Image>();
            var button = bodyObj.AddComponent<Button>();
            Transform body = bodyObj.transform;

            Text id = CreateText(body, "Id", StageNodeIdFontSize, CenterAnchor, StageNodeIdPosition, StageNodeIdSize, Color.white);
            Text stars = CreateText(body, "Stars", StageNodeStarsFontSize, BottomCenterAnchor, StageNodeStarsPosition, StageNodeStarsSize, MessageColor);
            Text lockMark = CreateText(body, "Lock", StageNodeLockFontSize, BottomCenterAnchor, StageNodeStarsPosition, StageNodeStarsSize, TitleSubColor);
            lockMark.text = "ロック";
            // マスの上にはみ出して出し、ボスステージだとひと目で分かるようにする
            Text boss = CreateText(body, "Boss", StageNodeBossFontSize, TopCenterAnchor, StageNodeBossPosition, StageNodeBossSize, StageBossLabelColor);
            boss.text = "BOSS";

            var node = nodeObj.AddComponent<StageNode>();
            SetRefs(node, ("_button", button), ("_background", background), ("_idLabel", id), ("_starsLabel", stars),
                ("_lockMark", lockMark.gameObject), ("_bossMark", boss.gameObject), ("_body", bodyObj.GetComponent<RectTransform>()));
            nodeObj.SetActive(false);
            return node;
        }
    }
}
