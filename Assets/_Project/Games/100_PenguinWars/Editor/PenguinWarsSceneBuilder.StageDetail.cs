using MiniGame.Editor;
using MiniGame.PenguinWars.Battle;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.PenguinWars.Editor
{
    /// <summary>
    /// ステージ選択でマスを押すと出る詳細（ステージ計画 Phase 2〜4）。
    /// 上から「名前・説明・★の条件・ベスト・出てくる敵・特別ルール・編成・ボタン」
    /// </summary>
    public static partial class PenguinWarsSceneBuilder
    {
        // 1080 の高さに収まる範囲で、★の条件の下に敵の顔・特別ルール・編成の帯を入れる
        private static readonly Vector2 StageDetailBoxSize = new Vector2(1300f, 1040f);
        private static readonly Vector2 StageDetailTitlePosition = new Vector2(0f, 445f);
        private static readonly Vector2 StageDetailTitleSize = new Vector2(1200f, 90f);
        private const int StageDetailTitleFontSize = 60;
        private static readonly Vector2 StageDetailDescriptionPosition = new Vector2(0f, 355f);
        private static readonly Vector2 StageDetailDescriptionSize = new Vector2(1150f, 90f);
        private const int StageDetailBodyFontSize = 36;
        private const float StageDetailConditionTopY = 270f;
        private const float StageDetailConditionStep = 55f;
        private static readonly Vector2 StageDetailConditionSize = new Vector2(1000f, 55f);
        private const int StageDetailConditionFontSize = 36;
        private static readonly Vector2 StageDetailBestPosition = new Vector2(0f, 105f);
        private static readonly Vector2 StageDetailBestSize = new Vector2(1000f, 50f);
        private static readonly Vector2 StageSortieButtonSize = new Vector2(480f, 120f);
        private static readonly Vector2 StageSortiePosition = new Vector2(230f, -420f);
        private static readonly Vector2 StageDetailClosePosition = new Vector2(-280f, -420f);

        // 帯（敵・編成）共通: 左に見出し、右に小さい立ち姿を並べる
        private static readonly Vector2 StageDetailStripSize = new Vector2(1260f, 120f);
        private static readonly Color StageDetailStripColor = new Color(1f, 1f, 1f, 0.08f);
        private static readonly Vector2 StageDetailStripLabelPosition = new Vector2(-515f, 0f);
        private static readonly Vector2 StageDetailStripLabelSize = new Vector2(220f, 110f);
        private const int StageDetailStripLabelFontSize = 40;
        private const float StageDetailStripIconFirstX = -345f;
        private static readonly Vector2 StageDetailIconSize = new Vector2(96f, 96f);

        private static readonly Vector2 StageDetailEnemyPosition = new Vector2(0f, 0f);
        private const int StageDetailEnemySlots = 8;
        private const float StageDetailEnemyStepX = 110f;
        private static readonly Vector2 StageDetailEnemyFrameSize = new Vector2(104f, 104f);
        private static readonly Vector2 StageDetailRulesPosition = new Vector2(0f, -95f);
        private static readonly Vector2 StageDetailRulesSize = new Vector2(1250f, 60f);
        private const int StageDetailRulesFontSize = 38;
        private static readonly Color StageDetailRulesColor = new Color(1f, 0.6f, 0.45f);

        // 編成の帯は全体が1つのボタン（押すと編成画面）
        private static readonly Vector2 StageDetailDeckPosition = new Vector2(0f, -200f);
        private const float StageDetailDeckIconStepX = 100f;

        private static StageDetailPanel CreateStageDetailPanel(Transform parent, PenguinUnitCatalog catalog)
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
            (Image[] enemyFrames, Image[] enemyIcons) = CreateStageDetailEnemies(box.transform);
            Text rules = CreateText(box.transform, "Rules", StageDetailRulesFontSize, CenterAnchor, StageDetailRulesPosition, StageDetailRulesSize, StageDetailRulesColor);
            (Button deckButton, Image[] deckIcons) = CreateStageDetailDeck(box.transform);
            Button sortie = CreateTitleButton(box.transform, "Btn_Sortie", "しゅつげき", StageSortieButtonSize, StageSortiePosition, StartButtonColor);
            Button close = CreateTitleButton(box.transform, "Btn_Close", "閉じる", TitleSubButtonSize, StageDetailClosePosition, TitleSubButtonColor);

            var panel = dimObj.AddComponent<StageDetailPanel>();
            var so = new UnityEditor.SerializedObject(panel);
            SerializedArray(so, "_conditionLabels", conditions);
            SerializedArray(so, "_deckIcons", deckIcons);
            SerializedArray(so, "_enemyFrames", enemyFrames);
            SerializedArray(so, "_enemyIcons", enemyIcons);
            so.ApplyModifiedPropertiesWithoutUndo();
            SetRefs(panel, ("_titleLabel", title), ("_descriptionLabel", description), ("_bestLabel", best),
                ("_sortieButton", sortie), ("_closeButton", close), ("_catalog", catalog), ("_deckButton", deckButton),
                ("_rulesLabel", rules));
            dimObj.SetActive(false);
            return panel;
        }

        /// <summary>見出し付きの帯。中身の並べ方は呼ぶ側で決める</summary>
        private static Image CreateStageDetailStrip(Transform box, string name, string label, Vector2 position)
        {
            Image strip = CreateImage(box, name, CenterAnchor, position, StageDetailStripSize, StageDetailStripColor);
            Text text = CreateText(strip.transform, "Label", StageDetailStripLabelFontSize, CenterAnchor, StageDetailStripLabelPosition, StageDetailStripLabelSize, MessageColor);
            text.text = label;
            return strip;
        }

        /// <summary>敵の顔は枠の色でボスを見分けるので、枠（背景）の上に絵を重ねる</summary>
        private static (Image[] frames, Image[] icons) CreateStageDetailEnemies(Transform box)
        {
            Image strip = CreateStageDetailStrip(box, "Enemies", "でてくる敵", StageDetailEnemyPosition);
            var frames = new Image[StageDetailEnemySlots];
            var icons = new Image[StageDetailEnemySlots];
            for (int i = 0; i < StageDetailEnemySlots; i++)
            {
                var position = new Vector2(StageDetailStripIconFirstX + i * StageDetailEnemyStepX, 0f);
                frames[i] = CreateImage(strip.transform, $"Enemy{i + 1}", CenterAnchor, position, StageDetailEnemyFrameSize, Color.white);
                icons[i] = CreateImage(frames[i].transform, "Icon", CenterAnchor, Vector2.zero, StageDetailIconSize, Color.white);
                icons[i].preserveAspect = true;
            }
            return (frames, icons);
        }

        private static (Button button, Image[] icons) CreateStageDetailDeck(Transform box)
        {
            Image strip = CreateStageDetailStrip(box, "Deck", "へんせい", StageDetailDeckPosition);
            // CreateImage は押せない絵として作るので、帯だけ押せるように戻す
            strip.raycastTarget = true;
            var button = strip.gameObject.AddComponent<Button>();

            var icons = new Image[DeckRules.DeckSize];
            for (int i = 0; i < icons.Length; i++)
            {
                var position = new Vector2(StageDetailStripIconFirstX + i * StageDetailDeckIconStepX, 0f);
                icons[i] = CreateImage(strip.transform, $"Icon{i + 1}", CenterAnchor, position, StageDetailIconSize, Color.white);
                icons[i].preserveAspect = true;
            }
            return (button, icons);
        }
    }
}
