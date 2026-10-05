using MiniGame.Editor;
using MiniGame.PenguinWars.Battle;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.PenguinWars.Editor
{
    /// <summary>
    /// ステージモードの編成画面（ステージ計画 Phase 3）。上に10枠、下にずかんと同じマスの一覧。
    /// 一覧のマスはずかんのひな形をそのまま使い、中身は実行時にカタログから並べる
    /// </summary>
    public static partial class PenguinWarsSceneBuilder
    {
        // 枠は出撃ボタンと同じ 5×2。左寄りに置き、右側に人数と「けってい」を並べる
        private const int DeckSlotColumns = 5;
        private static readonly Vector2 DeckSlotSize = new Vector2(130f, 130f);
        private const float DeckSlotStep = 144f;
        private const float DeckSlotFirstX = -538f;
        private const float DeckSlotTopY = -170f;
        private static readonly Vector2 DeckSlotIconSize = new Vector2(120f, 120f);

        private static readonly Vector2 DeckCountPosition = new Vector2(330f, -200f);
        private static readonly Vector2 DeckCountSize = new Vector2(300f, 90f);
        private const int DeckCountFontSize = 64;
        private static readonly Vector2 DeckDecideSize = new Vector2(380f, 110f);
        private static readonly Vector2 DeckDecidePosition = new Vector2(330f, -320f);

        // 一覧は枠の下から。ずかん（8列）より大きく押しやすいよう6列にする
        private const float DeckListTopMargin = 470f;
        private const int DeckListColumns = 6;

        private static DeckEditPanel CreateDeckEditPanel(Transform parent, PenguinUnitCatalog catalog)
        {
            GameObject panelObj = UIDialogBuilder.CreateUIObject("DeckEditPanel", parent);
            UIDialogBuilder.SetStretchAll(panelObj.GetComponent<RectTransform>());
            // raycastTarget を残し、編成中に下のステージ選択が押せないようにする
            panelObj.AddComponent<Image>().color = ZukanBackColor;

            Text title = CreateText(panelObj.transform, "Title", ZukanTitleFontSize, TopCenterAnchor, ZukanTitlePosition, ZukanTitleSize, MessageColor);
            title.text = "へんせい";
            Button back = CreateAnchoredButton(panelObj.transform, "Btn_Back", "もどる", ZukanCloseSize, TopRightAnchor, ZukanClosePosition);

            (Button[] slotButtons, Image[] slotIcons) = CreateDeckSlots(panelObj.transform);
            Text count = CreateText(panelObj.transform, "Count", DeckCountFontSize, TopCenterAnchor, DeckCountPosition, DeckCountSize, Color.white);
            Button decide = CreateDeckDecideButton(panelObj.transform);

            (ScrollRect scroll, Transform content) = CreateZukanScroll(panelObj.transform, DeckListTopMargin, DeckListColumns);
            ZukanCell template = CreateZukanCellTemplate(content);
            // 開いた一覧がマスより手前に出るよう、スクロール領域より後に作る
            Dropdown column = CreateZukanDropdown(panelObj.transform, "Dropdown_Column", ZukanColumnDropdownSize, ZukanTopLeftAnchor, ZukanColumnDropdownPosition);

            var panel = panelObj.AddComponent<DeckEditPanel>();
            var so = new UnityEditor.SerializedObject(panel);
            SerializedArray(so, "_slotButtons", slotButtons);
            SerializedArray(so, "_slotIcons", slotIcons);
            so.ApplyModifiedPropertiesWithoutUndo();
            SetRefs(panel, ("_catalog", catalog), ("_cellTemplate", template), ("_scroll", scroll), ("_columnDropdown", column),
                ("_countLabel", count), ("_decideButton", decide), ("_backButton", back));
            panelObj.SetActive(false);
            return panel;
        }

        /// <summary>枠の並びは出撃ボタンと同じ（左上から右へ、6体目から2段目）</summary>
        private static (Button[] buttons, Image[] icons) CreateDeckSlots(Transform panel)
        {
            var buttons = new Button[DeckRules.DeckSize];
            var icons = new Image[DeckRules.DeckSize];
            for (int i = 0; i < DeckRules.DeckSize; i++)
            {
                int row = i / DeckSlotColumns;
                int col = i % DeckSlotColumns;
                var position = new Vector2(DeckSlotFirstX + col * DeckSlotStep, DeckSlotTopY - row * DeckSlotStep);

                Image back = CreateImage(panel, $"Slot{i + 1}", TopCenterAnchor, position, DeckSlotSize, ZukanCellColor);
                // CreateImage は押せない絵として作るので、枠だけ押せるように戻す
                back.raycastTarget = true;
                buttons[i] = back.gameObject.AddComponent<Button>();
                icons[i] = CreateImage(back.transform, "Icon", CenterAnchor, Vector2.zero, DeckSlotIconSize, Color.white);
                icons[i].preserveAspect = true;
            }
            return (buttons, icons);
        }

        private static Button CreateDeckDecideButton(Transform panel)
        {
            GameObject buttonObj = UIDialogBuilder.CreateButton(panel, "Btn_Decide", "けってい", DeckDecideSize.x, DeckDecideSize.y, StartButtonColor);
            SetAnchor(buttonObj.GetComponent<RectTransform>(), TopCenterAnchor, DeckDecidePosition);
            buttonObj.GetComponentInChildren<Text>().fontSize = TitleButtonFontSize;
            return buttonObj.GetComponent<Button>();
        }
    }
}
