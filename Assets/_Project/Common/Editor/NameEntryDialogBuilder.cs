using MiniGame.Common.Profile;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.Editor
{
    /// <summary>
    /// 初回のユーザー名入力ダイアログをシーンへ生成する。タイトル（縦画面 1080x1920 基準）で使う。
    /// </summary>
    public static class NameEntryDialogBuilder
    {
        private const float PanelWidth = 900f;
        private const float PanelHeight = 720f;
        private const int TitleFontSize = 52;
        private const int HintFontSize = 32;
        private const float InputWidth = 720f;
        private const float InputHeight = 140f;
        private const int InputFontSize = 60;
        private const float DecideButtonWidth = 420f;
        private const float DecideButtonHeight = 130f;
        private const int DecideButtonFontSize = 46;

        private static readonly Color OverlayColor = new Color(0f, 0f, 0f, 0.85f);
        private static readonly Color PanelColor = new Color(0.14f, 0.16f, 0.24f);
        private static readonly Color DecideColor = new Color(0.18f, 0.55f, 0.9f);

        /// <summary>
        /// 確認ダイアログ（CommonDialog）より背面に置くため、UIDialogBuilder.BuildDialogs より先に呼ぶこと
        /// </summary>
        public static NameEntryDialog Build(Transform canvas)
        {
            // 全画面の不透明に近い幕で背面のボタンへのタップを止める
            var dialogObj = UIDialogBuilder.CreateUIObject("NameEntryDialog", canvas);
            UIDialogBuilder.SetStretchAll(dialogObj.GetComponent<RectTransform>());
            dialogObj.AddComponent<Image>().color = OverlayColor;

            var panelObj = UIDialogBuilder.CreateUIObject("Panel", dialogObj.transform);
            SetCenteredRect(panelObj.GetComponent<RectTransform>(), Vector2.zero, new Vector2(PanelWidth, PanelHeight));
            panelObj.AddComponent<Image>().color = PanelColor;

            var title = CreateLabel(panelObj.transform, "TitleText", "ユーザー名を決めてね", TitleFontSize, Color.white);
            title.fontStyle = FontStyle.Bold;
            SetCenteredRect(title.rectTransform, new Vector2(0, 250f), new Vector2(PanelWidth, 100f));

            var input = CreateNameInput(panelObj.transform, new Vector2(0, 80f), new Vector2(InputWidth, InputHeight));
            var webCover = CreateWebInputCover(input.transform);

            var hint = CreateLabel(panelObj.transform, "HintText",
                $"{UserProfile.MaxNameLength}文字まで / あとから変更できません", HintFontSize, new Color(0.7f, 0.75f, 0.85f));
            SetCenteredRect(hint.rectTransform, new Vector2(0, -50f), new Vector2(PanelWidth, 60f));

            var decideObj = UIDialogBuilder.CreateButton(panelObj.transform, "Btn_Decide", "決定", DecideButtonWidth, DecideButtonHeight, DecideColor);
            SetCenteredRect(decideObj.GetComponent<RectTransform>(), new Vector2(0, -220f), new Vector2(DecideButtonWidth, DecideButtonHeight));
            decideObj.GetComponentInChildren<Text>().fontSize = DecideButtonFontSize;

            var dialog = dialogObj.AddComponent<NameEntryDialog>();
            var so = new SerializedObject(dialog);
            so.FindProperty("_inputField").objectReferenceValue = input;
            so.FindProperty("_decideButton").objectReferenceValue = decideObj.GetComponent<Button>();
            so.FindProperty("_webInputCover").objectReferenceValue = webCover;
            so.ApplyModifiedProperties();

            dialogObj.SetActive(false);
            return dialog;
        }

        private static InputField CreateNameInput(Transform parent, Vector2 position, Vector2 size)
        {
            var obj = UIDialogBuilder.CreateUIObject("NameInput", parent);
            SetCenteredRect(obj.GetComponent<RectTransform>(), position, size);
            obj.AddComponent<Image>().color = Color.white;

            Text placeholder = CreateInputText(obj.transform, "Placeholder", "なまえ", new Color(0.5f, 0.5f, 0.5f));
            Text text = CreateInputText(obj.transform, "Text", "", Color.black);
            // 入力中に < > を打たれてもタグとして解釈させない
            text.supportRichText = false;

            var input = obj.AddComponent<InputField>();
            input.textComponent = text;
            input.placeholder = placeholder;
            input.characterLimit = UserProfile.MaxNameLength;
            return input;
        }

        /// <summary>ブラウザ版だけ有効化される透明ボタン。入力欄全体を覆い、InputField より先にタップを受ける</summary>
        private static Button CreateWebInputCover(Transform inputField)
        {
            var obj = UIDialogBuilder.CreateUIObject("WebInputCover", inputField);
            UIDialogBuilder.SetStretchAll(obj.GetComponent<RectTransform>());
            var img = obj.AddComponent<Image>();
            img.color = Color.clear;
            var button = obj.AddComponent<Button>();
            button.targetGraphic = img;
            obj.SetActive(false);
            return button;
        }

        private static Text CreateInputText(Transform parent, string name, string content, Color color)
        {
            var text = CreateLabel(parent, name, content, InputFontSize, color);
            var rect = text.rectTransform;
            UIDialogBuilder.SetStretchAll(rect);
            rect.offsetMin = new Vector2(20f, 8f);
            rect.offsetMax = new Vector2(-20f, -8f);
            return text;
        }

        private static Text CreateLabel(Transform parent, string name, string content, int fontSize, Color color)
        {
            var obj = UIDialogBuilder.CreateUIObject(name, parent);
            var text = obj.AddComponent<Text>();
            text.text = content;
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = color;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private static void SetCenteredRect(RectTransform rect, Vector2 anchoredPosition, Vector2 size)
        {
            var center = new Vector2(0.5f, 0.5f);
            rect.anchorMin = center;
            rect.anchorMax = center;
            rect.pivot = center;
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPosition;
        }
    }
}
