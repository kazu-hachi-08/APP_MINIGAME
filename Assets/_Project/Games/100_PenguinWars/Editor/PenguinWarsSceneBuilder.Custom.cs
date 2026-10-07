using MiniGame.Common.UI;
using MiniGame.Editor;
using MiniGame.PenguinWars.Art;
using MiniGame.PenguinWars.Battle;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.PenguinWars.Editor
{
    /// <summary>
    /// タイトルから開く「じぶんペンギン」の作成画面。左の列に 題名・枠・プレビュー・数値、右の列に タブ、下の段に もどる・ほぞん。
    /// 中身はすべて SafeArea の中の固定サイズの台紙（CustomContentSize）に並べ、狭い画面では台紙ごと縮める（FitContentScaler）。
    /// 画面の端を基準に置くと、横長スマホで高さが足りずボタンが重なったり、内カメ（ノッチ）に掛かったりするため。
    /// タブの中身は親オブジェクトを分け、のうりょくタブは .CustomStats.cs に置く（みためタブと触る場所が重ならないように）
    /// </summary>
    public static partial class PenguinWarsSceneBuilder
    {
        // 台紙の大きさ。1920x1080 の画面では周りに余白が残り、縮めるのは SafeArea がこれより狭いときだけ
        private static readonly Vector2 CustomContentSize = new Vector2(1800f, 960f);

        // 左の列（x -900〜-180）
        private const int CustomTitleFontSize = 64;
        private static readonly Vector2 CustomTitleSize = new Vector2(720f, 90f);

        private static readonly Vector2 CustomSlotButtonSize = new Vector2(200f, 90f);
        private const float CustomSlotSpacing = 230f;
        private static readonly Vector2 CustomSlotCenter = new Vector2(-540f, 330f);
        private const int CustomSlotFontSize = 56;

        // プレビューの台と数値。大型（拡大率2）でも台に収まる大きさにする（190 x 2 = 380 < 420）
        private static readonly Vector2 CustomStagePosition = new Vector2(-540f, 60f);
        private static readonly Vector2 CustomStageSize = new Vector2(420f, 420f);
        private static readonly Vector2 CustomPreviewFootPosition = new Vector2(0f, 20f);
        private const float CustomPreviewBaseSize = 190f;
        private static readonly Vector2 CustomStatsPosition = new Vector2(-540f, -270f);
        private static readonly Vector2 CustomStatsSize = new Vector2(720f, 210f);
        private const int CustomStatsFontSize = 30;
        private const float CustomStatsLineSpacing = 1.1f;

        // 右の列（x -120〜880）: タブのボタンと中身
        private static readonly Vector2 CustomTabButtonSize = new Vector2(320f, 90f);
        private static readonly Vector2 CustomLookTabButtonPosition = new Vector2(215f, 435f);
        private static readonly Vector2 CustomStatsTabButtonPosition = new Vector2(545f, 435f);
        private static readonly Vector2 CustomTabRootPosition = new Vector2(380f, 50f);
        private static readonly Vector2 CustomTabRootSize = new Vector2(1000f, 640f);

        // タブの中の行（名前・5部位・おまかせ）。スマホで押しやすいよう ◀▶ は大きめ
        private const float CustomRowHeight = 84f;
        private const float CustomRowStep = 92f;
        private const int CustomRowFontSize = 44;
        private const int CustomRowMinFontSize = 24;
        private const float CustomRowLabelX = -390f;
        private const float CustomRowLabelWidth = 220f;
        private const float CustomRowPrevX = -200f;
        private const float CustomRowValueX = 60f;
        private const float CustomRowValueWidth = 360f;
        private const float CustomRowNextX = 320f;
        private const float CustomArrowWidth = 130f;
        private const float CustomNameInputWidth = 490f;
        private const float CustomInputTextPadding = 16f;
        private static readonly Color CustomInputPlaceholderColor = new Color(0.5f, 0.5f, 0.5f);

        // 下の段（y -390〜-480）: もどる・ひとこと・ほぞん。数値（下端 -375）・タブの中身（下端 -270）と重ならない高さ
        private static readonly Vector2 CustomFooterButtonSize = new Vector2(280f, 90f);
        private const int CustomHintFontSize = 32;
        private static readonly Vector2 CustomHintSize = new Vector2(1000f, 90f);

        private static readonly PenguinPartSlot[] CustomRowSlots =
        {
            PenguinPartSlot.Body, PenguinPartSlot.BodyColor, PenguinPartSlot.Head, PenguinPartSlot.Hand, PenguinPartSlot.Back,
        };

        private static CustomUnitPanel CreateCustomUnitPanel(Transform parent)
        {
            // raycastTarget を残し、作成画面の下のタイトルのボタンが押せないようにする
            GameObject panelObj = CreateFullScreenPanel(parent, "CustomUnitPanel", ZukanBackColor);
            Transform content = CreateCustomContent(panelObj.transform);
            CreateCustomHeader(content);
            Button[] slots = CreateCustomSlotButtons(content);

            Image stage = CreateImage(content, "Stage", CenterAnchor, CustomStagePosition, CustomStageSize, ZukanStageColor);
            CustomUnitPreview preview = CreateCustomPreview(stage.transform);
            Text stats = CreateDetailText(content, "Stats", CustomStatsFontSize, CustomStatsPosition, CustomStatsSize, Color.white);
            stats.alignment = TextAnchor.UpperLeft;
            stats.lineSpacing = CustomStatsLineSpacing;

            Button lookTabButton = CreateAnchoredButton(content, "Btn_TabLook", "みため", CustomTabButtonSize, CenterAnchor, CustomLookTabButtonPosition);
            Button statsTabButton = CreateAnchoredButton(content, "Btn_TabStats", "のうりょく", CustomTabButtonSize, CenterAnchor, CustomStatsTabButtonPosition);
            CustomLookTab lookTab = CreateCustomLookTab(content);
            CustomStatsTab statsTab = CreateCustomStatsTab(content);

            Button back = CreateAnchoredButton(content, "Btn_Back", "もどる", CustomFooterButtonSize, BottomLeftAnchor, Vector2.zero);
            Button save = CreateAnchoredButton(content, "Btn_Save", "ほぞん", CustomFooterButtonSize, BottomRightAnchor, Vector2.zero, SettingsCloseColor);

            var panel = panelObj.AddComponent<CustomUnitPanel>();
            var so = new SerializedObject(panel);
            SerializedArray(so, "_slotButtons", slots);
            so.ApplyModifiedPropertiesWithoutUndo();
            SetRefs(panel, ("_lookTabButton", lookTabButton), ("_statsTabButton", statsTabButton),
                ("_lookTabRoot", lookTab.gameObject), ("_statsTabRoot", statsTab.gameObject), ("_lookTab", lookTab),
                ("_statsTab", statsTab),
                ("_preview", preview), ("_statsLabel", stats), ("_saveButton", save), ("_backButton", back));
            panelObj.SetActive(false);
            return panel;
        }

        /// <summary>背景は画面全体に敷いたまま、操作するものはノッチ・ホームバーを避けた台紙の上に置く</summary>
        private static Transform CreateCustomContent(Transform panel)
        {
            GameObject safeArea = UIDialogBuilder.CreateUIObject("SafeArea", panel);
            UIDialogBuilder.SetStretchAll(safeArea.GetComponent<RectTransform>());
            safeArea.AddComponent<SafeAreaFitter>();

            GameObject contentObj = UIDialogBuilder.CreateUIObject("Content", safeArea.transform);
            RectTransform content = contentObj.GetComponent<RectTransform>();
            SetAnchor(content, CenterAnchor, Vector2.zero);
            content.sizeDelta = CustomContentSize;

            var scaler = safeArea.AddComponent<FitContentScaler>();
            SetRefs(scaler, ("_content", content));
            return content;
        }

        private static void CreateCustomHeader(Transform content)
        {
            Text title = CreateText(content, "Title", CustomTitleFontSize, TopLeftAnchor, Vector2.zero, CustomTitleSize, MessageColor);
            title.text = "じぶんペンギン";
            title.alignment = TextAnchor.MiddleLeft;

            Text hint = CreateText(content, "Hint", CustomHintFontSize, BottomCenterAnchor, Vector2.zero, CustomHintSize, TitleSubColor);
            hint.text = "★対戦の前に3つから選べます";
        }

        private static Button[] CreateCustomSlotButtons(Transform content)
        {
            var buttons = new Button[CustomUnitPresets.SlotCount];
            for (int i = 0; i < buttons.Length; i++)
            {
                float x = CustomSlotCenter.x + (i - (buttons.Length - 1) * 0.5f) * CustomSlotSpacing;
                buttons[i] = CreateAnchoredButton(content, $"Btn_Slot{i + 1}", (i + 1).ToString(), CustomSlotButtonSize,
                    CenterAnchor, new Vector2(x, CustomSlotCenter.y));
                buttons[i].GetComponentInChildren<Text>().fontSize = CustomSlotFontSize;
            }
            return buttons;
        }

        /// <summary>足元を台の下に揃え、大型で大きくしたときに上へ伸びるようにする</summary>
        private static CustomUnitPreview CreateCustomPreview(Transform stage)
        {
            UnitSpriteAnimator icon = CreateAnimatedIcon(stage, BottomCenterAnchor, CustomPreviewFootPosition,
                new Vector2(CustomPreviewBaseSize, CustomPreviewBaseSize));
            var preview = stage.gameObject.AddComponent<CustomUnitPreview>();
            var so = new SerializedObject(preview);
            so.FindProperty("_baseSize").floatValue = CustomPreviewBaseSize;
            so.ApplyModifiedPropertiesWithoutUndo();
            SetRefs(preview, ("_icon", icon));
            return preview;
        }

        private static RectTransform CreateCustomTabRoot(Transform panel, string name)
        {
            GameObject root = UIDialogBuilder.CreateUIObject(name, panel);
            RectTransform rect = root.GetComponent<RectTransform>();
            SetAnchor(rect, CenterAnchor, CustomTabRootPosition);
            rect.sizeDelta = CustomTabRootSize;
            return rect;
        }

        /// <summary>行は上から 名前 → 5部位 → おまかせ の順に置く</summary>
        private static CustomLookTab CreateCustomLookTab(Transform panel)
        {
            RectTransform root = CreateCustomTabRoot(panel, "LookTab");
            float topY = CustomTabRootSize.y * 0.5f - CustomRowHeight * 0.5f;

            CreateCustomRowLabel(root, "NameLabel", "なまえ", topY);
            (InputField nameInput, Button webCover) = CreateCustomNameInput(root, topY);

            var rows = new CustomPartRow[CustomRowSlots.Length];
            for (int i = 0; i < rows.Length; i++) rows[i] = CreateCustomPartRow(root, CustomRowSlots[i], topY - (i + 1) * CustomRowStep);

            float randomY = topY - (rows.Length + 1) * CustomRowStep;
            Button random = CreateAnchoredButton(root, "Btn_Random", "おまかせ", new Vector2(CustomRowValueWidth, CustomRowHeight),
                CenterAnchor, new Vector2(CustomRowValueX, randomY), StartButtonColor);

            var tab = root.gameObject.AddComponent<CustomLookTab>();
            var so = new SerializedObject(tab);
            SerializedArray(so, "_rows", rows);
            so.ApplyModifiedPropertiesWithoutUndo();
            SetRefs(tab, ("_nameInput", nameInput), ("_webNameCover", webCover), ("_randomButton", random));
            return tab;
        }

        private static CustomPartRow CreateCustomPartRow(Transform root, PenguinPartSlot slot, float y)
        {
            GameObject rowObj = UIDialogBuilder.CreateUIObject($"Row_{slot}", root);
            RectTransform rect = rowObj.GetComponent<RectTransform>();
            SetAnchor(rect, CenterAnchor, new Vector2(0f, y));
            rect.sizeDelta = new Vector2(CustomTabRootSize.x, CustomRowHeight);

            // 文字は CustomPartRow が実行時に入れるので、ここでは空
            Text slotLabel = CreateCustomRowLabel(rowObj.transform, "SlotLabel", string.Empty, 0f);
            Vector2 arrowSize = new Vector2(CustomArrowWidth, CustomRowHeight);
            Button prev = CreateAnchoredButton(rowObj.transform, "Btn_Prev", "◀", arrowSize, CenterAnchor, new Vector2(CustomRowPrevX, 0f));
            Text value = CreateText(rowObj.transform, "Value", CustomRowFontSize, CenterAnchor, new Vector2(CustomRowValueX, 0f),
                new Vector2(CustomRowValueWidth, CustomRowHeight), Color.white);
            FitText(value, CustomRowMinFontSize);
            Button next = CreateAnchoredButton(rowObj.transform, "Btn_Next", "▶", arrowSize, CenterAnchor, new Vector2(CustomRowNextX, 0f));

            var row = rowObj.AddComponent<CustomPartRow>();
            var so = new SerializedObject(row);
            FindPropertyOrLog(so, "_slot").enumValueIndex = (int)slot;
            so.ApplyModifiedPropertiesWithoutUndo();
            SetRefs(row, ("_slotLabel", slotLabel), ("_valueLabel", value), ("_prevButton", prev), ("_nextButton", next));
            return row;
        }

        /// <summary>行の左端に置く項目名（左寄せ）</summary>
        private static Text CreateCustomRowLabel(Transform parent, string name, string label, float y)
        {
            Text text = CreateText(parent, name, CustomRowFontSize, CenterAnchor, new Vector2(CustomRowLabelX, y),
                new Vector2(CustomRowLabelWidth, CustomRowHeight), Color.white);
            text.alignment = TextAnchor.MiddleLeft;
            text.text = label;
            return text;
        }

        /// <summary>共通の名前入力（NameEntryDialogBuilder）と同じ作り。ブラウザ版だけ上に透明ボタンを被せて window.prompt で入力させる</summary>
        private static (InputField input, Button webCover) CreateCustomNameInput(Transform root, float y)
        {
            // 入力欄の左端を ◀ の左端に揃える
            var position = new Vector2(CustomRowPrevX - CustomArrowWidth * 0.5f + CustomNameInputWidth * 0.5f, y);
            Image back = CreateImage(root, "NameInput", CenterAnchor, position, new Vector2(CustomNameInputWidth, CustomRowHeight), Color.white);
            back.raycastTarget = true;

            Text placeholder = CreateCustomInputText(back.transform, "Placeholder", CustomInputPlaceholderColor);
            placeholder.text = CustomUnitRules.DefaultName;
            Text text = CreateCustomInputText(back.transform, "Text", Color.black);
            // 入力中に < > を打たれてもタグとして解釈させない
            text.supportRichText = false;

            var input = back.gameObject.AddComponent<InputField>();
            input.textComponent = text;
            input.placeholder = placeholder;
            input.characterLimit = CustomUnitRules.NameMaxLength;

            GameObject coverObj = UIDialogBuilder.CreateUIObject("WebInputCover", back.transform);
            UIDialogBuilder.SetStretchAll(coverObj.GetComponent<RectTransform>());
            Image coverImage = coverObj.AddComponent<Image>();
            coverImage.color = Color.clear;
            var cover = coverObj.AddComponent<Button>();
            cover.targetGraphic = coverImage;
            coverObj.SetActive(false);
            return (input, cover);
        }

        private static Text CreateCustomInputText(Transform parent, string name, Color color)
        {
            GameObject obj = UIDialogBuilder.CreateUIObject(name, parent);
            RectTransform rect = obj.GetComponent<RectTransform>();
            UIDialogBuilder.SetStretchAll(rect);
            rect.offsetMin = new Vector2(CustomInputTextPadding, 0f);
            rect.offsetMax = new Vector2(-CustomInputTextPadding, 0f);
            var text = obj.AddComponent<Text>();
            text.fontSize = CustomRowFontSize;
            text.alignment = TextAnchor.MiddleLeft;
            text.color = color;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            return text;
        }
    }
}
