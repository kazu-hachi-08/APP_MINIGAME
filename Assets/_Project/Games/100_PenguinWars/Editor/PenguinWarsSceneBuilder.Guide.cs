using MiniGame.Editor;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.PenguinWars.Editor
{
    /// <summary>
    /// タイトルから開く「あそびかた」（仕様書 §2.0）。真ん中は何も置かず、後ろの戦場で流すデモが見えるようにする。
    /// ページの中身は GuideTopics（コード）にあるので、ここは枠だけ作る
    /// </summary>
    public static partial class PenguinWarsSceneBuilder
    {
        private static readonly Vector2 GuideTopLeftAnchor = new Vector2(0f, 1f);
        private static readonly Color GuideCardColor = new Color(0f, 0.08f, 0.2f, 0.85f);
        // 左の城と頭上の HP バー（城キラーのデモで見せる）に被らないよう、カードは右上に寄せる
        private static readonly Vector2 GuideCardPosition = new Vector2(-30f, -30f);
        private static readonly Vector2 GuideCardSize = new Vector2(1300f, 250f);
        private static readonly Vector2 GuideTitlePosition = new Vector2(40f, -20f);
        private static readonly Vector2 GuideTitleSize = new Vector2(1220f, 80f);
        private const int GuideTitleFontSize = 60;
        private static readonly Vector2 GuideBodyPosition = new Vector2(40f, -110f);
        private static readonly Vector2 GuideBodySize = new Vector2(1220f, 120f);
        private const int GuideBodyFontSize = 34;

        // 下の出撃ボタン（高さ 162 + 余白 20）を隠して、デモ中に押せそうに見えないようにする
        private const float GuideBarHeight = 200f;
        private static readonly Color GuideBarColor = new Color(0.04f, 0.1f, 0.2f, 0.95f);
        private static readonly Vector2 GuideBarButtonSize = new Vector2(300f, 120f);
        private const float GuideBarButtonY = 40f;
        private const float GuideBarSideMargin = 30f;
        private const float GuideTurnButtonX = 330f;
        private const int GuidePageFontSize = 48;

        private static PenguinGuidePanel CreateGuidePanel(Transform canvas, BattleRunner battleRunner, BattleCamera battleCamera)
        {
            GameObject panelObj = UIDialogBuilder.CreateUIObject("GuidePanel", canvas);
            UIDialogBuilder.SetStretchAll(panelObj.GetComponent<RectTransform>());
            // 透明でも raycastTarget を残し、デモ中に戦場のドラッグや下の出撃ボタンが効かないようにする
            panelObj.AddComponent<Image>().color = Color.clear;

            (Text title, Text body) = CreateGuideCard(panelObj.transform);
            Transform bar = CreateGuideBar(panelObj.transform);
            Button close = CreateAnchoredButton(bar, "Btn_Close", "もどる", GuideBarButtonSize, BottomLeftAnchor,
                new Vector2(GuideBarSideMargin, GuideBarButtonY));
            Button prev = CreateAnchoredButton(bar, "Btn_Prev", "＜ まえ", GuideBarButtonSize, BottomCenterAnchor,
                new Vector2(-GuideTurnButtonX, GuideBarButtonY));
            Button next = CreateAnchoredButton(bar, "Btn_Next", "つぎ ＞", GuideBarButtonSize, BottomCenterAnchor,
                new Vector2(GuideTurnButtonX, GuideBarButtonY));
            Text page = CreateText(bar, "Page", GuidePageFontSize, BottomCenterAnchor, new Vector2(0f, GuideBarButtonY),
                GuideBarButtonSize, Color.white);

            var director = panelObj.AddComponent<GuideDemoDirector>();
            SetRefs(director, ("_battleRunner", battleRunner));
            var panel = panelObj.AddComponent<PenguinGuidePanel>();
            SetRefs(panel, ("_director", director), ("_battleCamera", battleCamera), ("_titleLabel", title), ("_bodyLabel", body),
                ("_pageLabel", page), ("_prevButton", prev), ("_nextButton", next), ("_closeButton", close));
            panelObj.SetActive(false);
            return panel;
        }

        private static (Text title, Text body) CreateGuideCard(Transform panel)
        {
            Image card = CreateImage(panel, "Card", TopRightAnchor, GuideCardPosition, GuideCardSize, GuideCardColor);

            Text title = CreateText(card.transform, "Title", GuideTitleFontSize, GuideTopLeftAnchor, GuideTitlePosition, GuideTitleSize, MessageColor);
            title.alignment = TextAnchor.MiddleLeft;
            Text body = CreateText(card.transform, "Body", GuideBodyFontSize, GuideTopLeftAnchor, GuideBodyPosition, GuideBodySize, Color.white);
            body.alignment = TextAnchor.UpperLeft;
            return (title, body);
        }

        private static Transform CreateGuideBar(Transform panel)
        {
            Image bar = CreateImage(panel, "Bar", BottomLeftAnchor, Vector2.zero, Vector2.zero, GuideBarColor);
            RectTransform rect = bar.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.right;
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(0f, GuideBarHeight);
            return bar.transform;
        }
    }
}
