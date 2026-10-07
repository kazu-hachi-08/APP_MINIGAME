using MiniGame.PenguinWars.Battle;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.PenguinWars.Editor
{
    /// <summary>ドラフトの後の「じぶんペンギン選択」。ドラフト画面と同じ並び・カードで作り、見た目をそろえる</summary>
    public static partial class PenguinWarsSceneBuilder
    {
        private const string CustomPickTitle = "じぶんペンギンを えらぶ";
        private const int CustomPickStatsFontSize = 28;
        private static readonly Vector2 CustomPickStatsSize = new Vector2(420f, 40f);
        // ドラフトのカードの能力の行（-440）の下
        private static readonly Vector2 CustomPickStatsPosition = new Vector2(0f, -495f);

        private static CustomPickPanel CreateCustomPickPanel(Transform canvas)
        {
            GameObject panelObj = CreateFullScreenPanel(canvas, "CustomPickPanel", IntroBackColor);

            CreateText(panelObj.transform, "Title", DraftRoundFontSize, TopCenterAnchor, DraftRoundPosition, DraftRoundSize, MessageColor)
                .text = CustomPickTitle;
            Text time = CreateText(panelObj.transform, "Time", DraftTimeFontSize, TopCenterAnchor, DraftTimePosition, DraftTimeSize, Color.white);
            Text status = CreateText(panelObj.transform, "Status", DraftStatusFontSize, BottomCenterAnchor, DraftStatusPosition, DraftStatusSize, Color.white);

            var cards = new DraftCard[CustomUnitPresets.SlotCount];
            var walkers = new UnitSpriteAnimator[cards.Length];
            var stats = new Text[cards.Length];
            for (int i = 0; i < cards.Length; i++)
            {
                float x = (i - (cards.Length - 1) * 0.5f) * DraftCardSpacing;
                cards[i] = CreateDraftCard(panelObj.transform, i, x);
                walkers[i] = AddWalker(cards[i]);
                stats[i] = CreateText(cards[i].transform, "Stats", CustomPickStatsFontSize, TopCenterAnchor, CustomPickStatsPosition,
                    CustomPickStatsSize, Color.white);
            }

            var panel = panelObj.AddComponent<CustomPickPanel>();
            var so = new UnityEditor.SerializedObject(panel);
            SerializedArray(so, "_cards", cards);
            SerializedArray(so, "_walkers", walkers);
            SerializedArray(so, "_statsLabels", stats);
            so.ApplyModifiedPropertiesWithoutUndo();
            SetRefs(panel, ("_timeLabel", time), ("_statusLabel", status));
            panelObj.SetActive(false);
            return panel;
        }

        /// <summary>ドラフトのカードの絵（Icon）をそのまま歩かせる。絵を2枚重ねないため</summary>
        private static UnitSpriteAnimator AddWalker(DraftCard card)
        {
            Image icon = card.transform.Find("Icon").GetComponent<Image>();
            var walker = icon.gameObject.AddComponent<UnitSpriteAnimator>();
            SetRefs(walker, ("_image", icon));
            return walker;
        }
    }
}
