using MiniGame.Editor;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.PenguinWars.Editor
{
    /// <summary>
    /// 一人用のステージのリザルトと、その演出（★を1つずつ・NEW RECORD!・なかまになった！のカード）（仕様書 §2.1・§9）
    /// </summary>
    public static partial class PenguinWarsSceneBuilder
    {
        // 右側に「なかまになった！」の列を置ける幅
        private static readonly Vector2 StageResultBoxSize = new Vector2(1800f, 940f);
        private static readonly Vector2 StageResultTitlePosition = new Vector2(0f, 360f);
        private static readonly Vector2 StageResultTitleSize = new Vector2(1200f, 140f);
        private const int StageResultTitleFontSize = 110;
        private static readonly Vector2 StageResultStarsPosition = new Vector2(0f, 240f);
        private static readonly Vector2 StageResultStarSize = new Vector2(120f, 110f);
        private const float StageResultStarStepX = 120f;
        private const int StageResultStarsFontSize = 96;
        // ★の右に斜めに貼る
        private static readonly Vector2 StageResultNewRecordPosition = new Vector2(330f, 250f);
        private static readonly Vector2 StageResultNewRecordSize = new Vector2(360f, 80f);
        private const int StageResultNewRecordFontSize = 52;
        private static readonly Color StageResultNewRecordColor = new Color(1f, 0.4f, 0.35f);
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

        // 「なかまになった！」のカード
        private static readonly Vector2 UnlockCardSize = new Vector2(760f, 760f);
        private static readonly Color UnlockCardColor = new Color(0.16f, 0.24f, 0.38f);
        private static readonly Vector2 UnlockCardTitlePosition = new Vector2(0f, 290f);
        private static readonly Vector2 UnlockCardTitleSize = new Vector2(700f, 110f);
        private const int UnlockCardTitleFontSize = 80;
        private static readonly Vector2 UnlockCardIconPosition = new Vector2(0f, 20f);
        private static readonly Vector2 UnlockCardIconSize = new Vector2(360f, 360f);
        private static readonly Vector2 UnlockCardNamePosition = new Vector2(0f, -230f);
        private static readonly Vector2 UnlockCardNameSize = new Vector2(700f, 90f);
        private const int UnlockCardNameFontSize = 60;
        private static readonly Vector2 UnlockCardCountPosition = new Vector2(-30f, 30f);
        private static readonly Vector2 UnlockCardCountSize = new Vector2(200f, 60f);
        private const int UnlockCardCountFontSize = 40;
        private static readonly Vector2 UnlockCardHintPosition = new Vector2(0f, -330f);
        private static readonly Vector2 UnlockCardHintSize = new Vector2(700f, 60f);
        private const int UnlockCardHintFontSize = 36;

        /// <summary>操作UI・HUDより手前、ポーズ・共通ダイアログより奥に作る</summary>
        private static StageResultPanel CreateStageResultPanel(Transform canvas, PenguinUnitCatalog catalog)
        {
            GameObject dimObj = CreateFullScreenPanel(canvas, "StageResultPanel", DialogDimColor);
            Image box = CreateImage(dimObj.transform, "Box", CenterAnchor, Vector2.zero, StageResultBoxSize, DialogBoxColor);

            Text title = CreateText(box.transform, "Title", StageResultTitleFontSize, CenterAnchor, StageResultTitlePosition, StageResultTitleSize, MessageColor);
            StarRevealAnimator stars = CreateResultStars(box.transform);
            GameObject newRecord = CreateNewRecordLabel(box.transform);
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
            // リザルトのボタンより手前に被せて、カードを見終わるまでボタンを押せないようにする
            UnlockRevealPanel unlockReveal = CreateUnlockRevealPanel(dimObj.transform, catalog);

            var panel = dimObj.AddComponent<StageResultPanel>();
            var so = new UnityEditor.SerializedObject(panel);
            SerializedArray(so, "_unlockIcons", unlockIcons);
            SerializedArray(so, "_unlockNames", unlockNames);
            so.FindProperty("_detailShiftWithUnlocks").floatValue = StageResultDetailShiftWithUnlocks;
            so.ApplyModifiedPropertiesWithoutUndo();
            SetRefs(panel, ("_titleLabel", title), ("_stars", stars), ("_newRecordLabel", newRecord), ("_detailLabel", detail),
                ("_nextButton", next), ("_retryButton", retry), ("_selectButton", select),
                ("_catalog", catalog), ("_unlockGroup", unlockGroup), ("_unlockReveal", unlockReveal));
            dimObj.SetActive(false);
            return panel;
        }

        /// <summary>★は1つずつ跳ねさせるので、3つを別々の文字にする</summary>
        private static StarRevealAnimator CreateResultStars(Transform box)
        {
            GameObject group = UIDialogBuilder.CreateUIObject("Stars", box);
            UIDialogBuilder.SetStretchAll(group.GetComponent<RectTransform>());
            int count = StageLabels.StarOrder.Length;
            var stars = new Text[count];
            for (int i = 0; i < count; i++)
            {
                float x = (i - (count - 1) * 0.5f) * StageResultStarStepX;
                stars[i] = CreateText(group.transform, $"Star{i + 1}", StageResultStarsFontSize, CenterAnchor,
                    StageResultStarsPosition + Vector2.right * x, StageResultStarSize, MessageColor);
            }

            var animator = group.AddComponent<StarRevealAnimator>();
            var so = new UnityEditor.SerializedObject(animator);
            SerializedArray(so, "_stars", stars);
            so.ApplyModifiedPropertiesWithoutUndo();
            return animator;
        }

        private static GameObject CreateNewRecordLabel(Transform box)
        {
            Text label = CreateText(box, "NewRecord", StageResultNewRecordFontSize, CenterAnchor, StageResultNewRecordPosition,
                StageResultNewRecordSize, StageResultNewRecordColor);
            label.text = "NEW RECORD!";
            label.gameObject.AddComponent<UiWobble>();
            label.gameObject.SetActive(false);
            return label.gameObject;
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
                FitText(names[i], StageResultUnlockNameMinFontSize);
            }
            group.SetActive(false);
            return (group, icons, names);
        }

        /// <summary>画面全体を押せるボタンにして、どこを押しても次のカードへ進める</summary>
        private static UnlockRevealPanel CreateUnlockRevealPanel(Transform parent, PenguinUnitCatalog catalog)
        {
            GameObject dimObj = CreateFullScreenPanel(parent, "UnlockReveal", DialogDimColor);
            var button = dimObj.AddComponent<Button>();
            button.transition = Selectable.Transition.None;

            Image card = CreateImage(dimObj.transform, "Card", CenterAnchor, Vector2.zero, UnlockCardSize, UnlockCardColor);
            Text title = CreateText(card.transform, "Title", UnlockCardTitleFontSize, CenterAnchor, UnlockCardTitlePosition, UnlockCardTitleSize, MessageColor);
            title.text = "なかまになった！";
            UnitSpriteAnimator icon = CreateAnimatedIcon(card.transform, CenterAnchor, UnlockCardIconPosition, UnlockCardIconSize);
            Text name = CreateText(card.transform, "Name", UnlockCardNameFontSize, CenterAnchor, UnlockCardNamePosition, UnlockCardNameSize, Color.white);
            Text count = CreateText(card.transform, "Count", UnlockCardCountFontSize, BottomRightAnchor, UnlockCardCountPosition, UnlockCardCountSize, TitleSubColor);
            count.alignment = TextAnchor.LowerRight;
            Text hint = CreateText(card.transform, "Hint", UnlockCardHintFontSize, CenterAnchor, UnlockCardHintPosition, UnlockCardHintSize, TitleSubColor);
            hint.text = "タップでつぎへ";

            var panel = dimObj.AddComponent<UnlockRevealPanel>();
            SetRefs(panel, ("_catalog", catalog), ("_nextButton", button), ("_card", card.rectTransform), ("_icon", icon),
                ("_nameLabel", name), ("_countLabel", count));
            dimObj.SetActive(false);
            return panel;
        }

        /// <summary>音（PenguinWarsAudio）はリザルト・ステージ選択より後に作るので、出来てから参照を入れる</summary>
        private static void AssignStageAudio(StageSelectPanel stageSelectPanel, StageResultPanel stageResultPanel, PenguinWarsAudio audio)
        {
            SetRefs(stageResultPanel.GetComponentInChildren<StarRevealAnimator>(true), ("_audio", audio));
            SetRefs(stageResultPanel.GetComponentInChildren<UnlockRevealPanel>(true), ("_audio", audio));
            // マスのひな形に入れておけば、実行時に複製したマスにも引き継がれる
            SetRefs(stageSelectPanel.GetComponentInChildren<StageNode>(true), ("_audio", audio));
        }
    }
}
