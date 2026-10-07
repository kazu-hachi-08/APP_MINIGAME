using MiniGame.Editor;
using MiniGame.PenguinWars.Battle;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.PenguinWars.Editor
{
    /// <summary>オンライン対戦のドラフト画面（仕様書 §6）と編成確認（§2.2）。どちらも全画面で操作UIの上に被せる</summary>
    public static partial class PenguinWarsSceneBuilder
    {
        private const int DraftCardCount = 3;
        private const int DraftPickedCount = DeckRules.DeckSize;

        private const int DraftRoundFontSize = 64;
        private static readonly Vector2 DraftRoundSize = new Vector2(800f, 80f);
        private static readonly Vector2 DraftRoundPosition = new Vector2(0f, -30f);
        private const int DraftTimeFontSize = 48;
        private static readonly Vector2 DraftTimeSize = new Vector2(500f, 64f);
        private static readonly Vector2 DraftTimePosition = new Vector2(0f, -115f);
        private const int DraftStatusFontSize = 40;
        private static readonly Vector2 DraftStatusSize = new Vector2(1000f, 60f);
        private static readonly Vector2 DraftStatusPosition = new Vector2(0f, 150f);

        private static readonly Vector2 DraftCardSize = new Vector2(440f, 560f);
        private const float DraftCardSpacing = 500f;
        private const float DraftCardY = 30f;
        private static readonly Color DraftCardColor = new Color(0.16f, 0.24f, 0.38f, 0.95f);
        private static readonly Color DraftSelectedColor = new Color(1f, 0.85f, 0.25f, 0.45f);
        private static readonly Vector2 DraftCardIconSize = new Vector2(220f, 220f);
        private static readonly Vector2 DraftCardIconPosition = new Vector2(0f, -30f);
        private const int DraftCardNameFontSize = 40;
        private const int DraftCardNameMinFontSize = 20;
        private static readonly Vector2 DraftCardNameSize = new Vector2(420f, 56f);
        private static readonly Vector2 DraftCardNamePosition = new Vector2(0f, -265f);
        private const int DraftCardInfoFontSize = 32;
        private static readonly Vector2 DraftCardInfoSize = new Vector2(420f, 44f);
        private static readonly Vector2 DraftCardCostPosition = new Vector2(0f, -330f);
        private static readonly Vector2 DraftCardRolePosition = new Vector2(0f, -385f);
        private static readonly Vector2 DraftCardAbilityPosition = new Vector2(0f, -440f);
        private static readonly Color DraftAbilityColor = new Color(0.6f, 0.9f, 1f);

        private static readonly Vector2 DraftPickedIconSize = new Vector2(80f, 80f);
        private const float DraftPickedSpacing = 100f;
        private const float DraftPickedY = 40f;
        private static readonly Color DraftPickedSlotColor = new Color(1f, 1f, 1f, 0.12f);

        // じぶんペンギンの印（ドラフトの最後の枠・編成確認のマス）
        private const string CustomMarkLabel = "じぶん";
        private static readonly Color CustomMarkColor = new Color(1f, 0.85f, 0.3f);
        private const int DraftCustomSlotFontSize = 22;
        private const int RevealCustomMarkFontSize = 22;
        private static readonly Vector2 RevealCustomMarkSize = new Vector2(120f, 30f);
        private const float RevealIconToMarkY = 68f;

        private const int RevealColumns = 10;
        private const float RevealCellWidth = 180f;
        private static readonly Vector2 RevealIconSize = new Vector2(110f, 110f);
        private const int RevealNameFontSize = 24;
        private const int RevealNameMinFontSize = 12;
        private static readonly Vector2 RevealNameSize = new Vector2(172f, 40f);
        private const float RevealIconToNameY = -75f;
        private const int RevealSeatFontSize = 48;
        private static readonly Vector2 RevealSeatSize = new Vector2(900f, 64f);
        private const float RevealMyLabelY = 260f;
        private const float RevealMyRowY = 130f;
        private const float RevealOpponentLabelY = -60f;
        private const float RevealOpponentRowY = -190f;
        private static readonly Color RevealMyColor = new Color(0.55f, 0.8f, 1f);
        private static readonly Color RevealOpponentColor = new Color(1f, 0.55f, 0.5f);
        private const float RevealStageY = -360f;

        // ------------------------------------------------------------------
        // ドラフト
        // ------------------------------------------------------------------

        private static DraftPanel CreateDraftPanel(Transform canvas, PenguinUnitCatalog catalog)
        {
            GameObject panelObj = CreateFullScreenPanel(canvas, "DraftPanel", IntroBackColor);

            Text round = CreateText(panelObj.transform, "Round", DraftRoundFontSize, TopCenterAnchor, DraftRoundPosition, DraftRoundSize, MessageColor);
            Text time = CreateText(panelObj.transform, "Time", DraftTimeFontSize, TopCenterAnchor, DraftTimePosition, DraftTimeSize, Color.white);
            Text status = CreateText(panelObj.transform, "Status", DraftStatusFontSize, BottomCenterAnchor, DraftStatusPosition, DraftStatusSize, Color.white);

            var cards = new DraftCard[DraftCardCount];
            for (int i = 0; i < cards.Length; i++)
            {
                float x = (i - (DraftCardCount - 1) * 0.5f) * DraftCardSpacing;
                cards[i] = CreateDraftCard(panelObj.transform, i, x);
            }

            var pickedIcons = new Image[DraftPickedCount];
            for (int i = 0; i < pickedIcons.Length; i++)
            {
                float x = (i - (DraftPickedCount - 1) * 0.5f) * DraftPickedSpacing;
                Image slot = CreateImage(panelObj.transform, $"Picked{i + 1}", BottomCenterAnchor, new Vector2(x, DraftPickedY),
                    DraftPickedIconSize, DraftPickedSlotColor);
                // ドラフトしない最後の枠に何が入るか分かるように（ドラフトで埋まることはないので絵とは重ならない）
                if (i >= DraftPickedCount - CustomUnitRules.SlotsInDeck)
                {
                    CreateText(slot.transform, "CustomMark", DraftCustomSlotFontSize, CenterAnchor, Vector2.zero, DraftPickedIconSize,
                        CustomMarkColor).text = CustomMarkLabel;
                }
                pickedIcons[i] = CreateImage(slot.transform, "Icon", CenterAnchor, Vector2.zero, DraftPickedIconSize, Color.white);
                pickedIcons[i].preserveAspect = true;
            }

            var panel = panelObj.AddComponent<DraftPanel>();
            var so = new UnityEditor.SerializedObject(panel);
            SerializedArray(so, "_cards", cards);
            SerializedArray(so, "_pickedIcons", pickedIcons);
            so.ApplyModifiedPropertiesWithoutUndo();
            SetRefs(panel, ("_catalog", catalog), ("_roundLabel", round), ("_timeLabel", time), ("_statusLabel", status));
            panelObj.SetActive(false);
            return panel;
        }

        private static DraftCard CreateDraftCard(Transform panel, int index, float x)
        {
            GameObject cardObj = UIDialogBuilder.CreateButton(panel, $"Card{index + 1}", string.Empty,
                DraftCardSize.x, DraftCardSize.y, DraftCardColor);
            SetAnchor(cardObj.GetComponent<RectTransform>(), CenterAnchor, new Vector2(x, DraftCardY));
            // 共通ヘルパーが作る中央ラベルは使わない（名前・コスト・役割を別々に置くため）
            Object.DestroyImmediate(cardObj.GetComponentInChildren<Text>().gameObject);

            Image icon = CreateImage(cardObj.transform, "Icon", TopCenterAnchor, DraftCardIconPosition, DraftCardIconSize, Color.white);
            icon.preserveAspect = true;
            Text nameLabel = CreateText(cardObj.transform, "Name", DraftCardNameFontSize, TopCenterAnchor, DraftCardNamePosition, DraftCardNameSize, Color.white);
            // 長い名前（こおりのじょおうペンギン など）でもカードからはみ出さないよう縮めて収める
            FitText(nameLabel, DraftCardNameMinFontSize);
            Text cost = CreateText(cardObj.transform, "Cost", DraftCardInfoFontSize, TopCenterAnchor, DraftCardCostPosition, DraftCardInfoSize, CostColor);
            Text role = CreateText(cardObj.transform, "Role", DraftCardInfoFontSize, TopCenterAnchor, DraftCardRolePosition, DraftCardInfoSize, Color.white);
            Text ability = CreateText(cardObj.transform, "Ability", DraftCardInfoFontSize, TopCenterAnchor, DraftCardAbilityPosition, DraftCardInfoSize, DraftAbilityColor);

            Image selected = CreateImage(cardObj.transform, "Selected", CenterAnchor, Vector2.zero, Vector2.zero, DraftSelectedColor);
            UIDialogBuilder.SetStretchAll(selected.rectTransform);
            selected.gameObject.SetActive(false);
            GameObject dimmer = CreateDimmer(cardObj.transform);
            dimmer.SetActive(false);

            var card = cardObj.AddComponent<DraftCard>();
            SetRefs(card, ("_button", cardObj.GetComponent<Button>()), ("_icon", icon), ("_nameLabel", nameLabel), ("_costLabel", cost),
                ("_roleLabel", role), ("_abilityLabel", ability), ("_selectedFrame", selected.gameObject), ("_dimmer", dimmer));
            return card;
        }

        // ------------------------------------------------------------------
        // 編成確認
        // ------------------------------------------------------------------

        private static DeckRevealPanel CreateDeckRevealPanel(Transform canvas, PenguinUnitCatalog catalog)
        {
            GameObject panelObj = CreateFullScreenPanel(canvas, "DeckRevealPanel", IntroBackColor);
            CreateText(panelObj.transform, "Title", IntroTitleFontSize, TopCenterAnchor, IntroTitlePosition, IntroTitleSize, MessageColor).text = "編成確認";

            Text myName = CreateText(panelObj.transform, "MyName", RevealSeatFontSize, CenterAnchor, new Vector2(0f, RevealMyLabelY), RevealSeatSize, RevealMyColor);
            Text opponentName = CreateText(panelObj.transform, "OpponentName", RevealSeatFontSize, CenterAnchor, new Vector2(0f, RevealOpponentLabelY), RevealSeatSize, RevealOpponentColor);
            Text stage = CreateText(panelObj.transform, "Stage", RevealSeatFontSize, CenterAnchor, new Vector2(0f, RevealStageY), RevealSeatSize, MessageColor);
            (Image[] myIcons, Text[] myNames, GameObject[] myMarks) = CreateRevealRow(panelObj.transform, "My", RevealMyRowY);
            (Image[] opponentIcons, Text[] opponentNames, GameObject[] opponentMarks) =
                CreateRevealRow(panelObj.transform, "Opponent", RevealOpponentRowY);

            var panel = panelObj.AddComponent<DeckRevealPanel>();
            var so = new UnityEditor.SerializedObject(panel);
            SerializedArray(so, "_myIcons", myIcons);
            SerializedArray(so, "_myNames", myNames);
            SerializedArray(so, "_opponentIcons", opponentIcons);
            SerializedArray(so, "_opponentNames", opponentNames);
            SerializedArray(so, "_myCustomMarks", myMarks);
            SerializedArray(so, "_opponentCustomMarks", opponentMarks);
            so.ApplyModifiedPropertiesWithoutUndo();
            SetRefs(panel, ("_catalog", catalog), ("_myNameLabel", myName), ("_opponentNameLabel", opponentName),
                ("_stageLabel", stage));
            panelObj.SetActive(false);
            return panel;
        }

        private static (Image[] icons, Text[] names, GameObject[] customMarks) CreateRevealRow(Transform panel, string prefix, float y)
        {
            var icons = new Image[RevealColumns];
            var names = new Text[RevealColumns];
            var customMarks = new GameObject[RevealColumns];
            for (int i = 0; i < RevealColumns; i++)
            {
                float x = (i - (RevealColumns - 1) * 0.5f) * RevealCellWidth;
                icons[i] = CreateImage(panel, $"{prefix}Icon{i + 1}", CenterAnchor, new Vector2(x, y), RevealIconSize, Color.white);
                icons[i].preserveAspect = true;
                names[i] = CreateText(panel, $"{prefix}Name{i + 1}", RevealNameFontSize, CenterAnchor, new Vector2(x, y + RevealIconToNameY),
                    RevealNameSize, Color.white);
                // 10体並べると1枠が狭いので、長い名前は縮めて収める
                FitText(names[i], RevealNameMinFontSize);
                Text mark = CreateText(panel, $"{prefix}CustomMark{i + 1}", RevealCustomMarkFontSize, CenterAnchor,
                    new Vector2(x, y + RevealIconToMarkY), RevealCustomMarkSize, CustomMarkColor);
                mark.text = CustomMarkLabel;
                customMarks[i] = mark.gameObject;
            }
            return (icons, names, customMarks);
        }
    }
}
