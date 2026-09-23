using MiniGame.Common.Online;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.Editor
{
    /// <summary>
    /// 試合前に「コンピュータと対戦／部屋を作る／コードで参加」を選ばせるパネルを生成する。
    /// オンライン対戦を持つミニゲームのSceneBuilderから共有して使う。
    /// </summary>
    public static class ModeSelectPanelBuilder
    {
        /// <param name="offlineLabel">オフライン対戦ボタンの文言（卓球: NPCと対戦 / サッカー: CPUと対戦）</param>
        public static ModeSelectPanel Create(Transform canvas, OnlineSession session, string offlineLabel)
        {
            var panelObj = UIDialogBuilder.CreateUIObject("ModeSelectPanel", canvas);
            UIDialogBuilder.SetStretchAll(panelObj.GetComponent<RectTransform>());
            panelObj.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.75f);

            var boxObj = UIDialogBuilder.CreateUIObject("Panel", panelObj.transform);
            SetCenteredRect(boxObj.GetComponent<RectTransform>(), Vector2.zero, new Vector2(860f, 520f));
            boxObj.AddComponent<Image>().color = new Color(0.12f, 0.14f, 0.18f);

            // メニュー（モード選択）
            var menuObj = UIDialogBuilder.CreateUIObject("Menu", boxObj.transform);
            UIDialogBuilder.SetStretchAll(menuObj.GetComponent<RectTransform>());

            CreatePanelText(menuObj.transform, "TitleText", "対戦モードを選んでください", 30, new Vector2(0f, 200f), new Vector2(800f, 60f));

            var npcButton = CreatePanelButton(menuObj.transform, "Btn_Npc", offlineLabel, new Vector2(-190f, 80f),
                new Vector2(340f, 110f), new Color(0.18f, 0.55f, 0.9f));
            var hostButton = CreatePanelButton(menuObj.transform, "Btn_Host", "部屋を作る", new Vector2(190f, 80f),
                new Vector2(340f, 110f), new Color(0.2f, 0.62f, 0.4f));

            CreatePanelText(menuObj.transform, "JoinLabel", "参加コードで入る", 22, new Vector2(0f, -40f), new Vector2(800f, 40f));
            var codeInput = CreateCodeInput(menuObj.transform, new Vector2(-150f, -120f), new Vector2(400f, 90f));
            var joinButton = CreatePanelButton(menuObj.transform, "Btn_Join", "参加する", new Vector2(230f, -120f),
                new Vector2(240f, 90f), new Color(0.3f, 0.33f, 0.4f));

            // 接続待ち
            var waitingObj = UIDialogBuilder.CreateUIObject("Waiting", boxObj.transform);
            UIDialogBuilder.SetStretchAll(waitingObj.GetComponent<RectTransform>());

            Text statusText = CreatePanelText(waitingObj.transform, "StatusText", "", 30, new Vector2(0f, 60f), new Vector2(800f, 300f));
            var cancelButton = CreatePanelButton(waitingObj.transform, "Btn_Cancel", "キャンセル", new Vector2(0f, -180f),
                new Vector2(300f, 90f), new Color(0.3f, 0.33f, 0.4f));
            waitingObj.SetActive(false);

            var panel = panelObj.AddComponent<ModeSelectPanel>();
            var so = new SerializedObject(panel);
            so.FindProperty("_session").objectReferenceValue = session;
            so.FindProperty("_menuGroup").objectReferenceValue = menuObj;
            so.FindProperty("_npcButton").objectReferenceValue = npcButton;
            so.FindProperty("_hostButton").objectReferenceValue = hostButton;
            so.FindProperty("_joinButton").objectReferenceValue = joinButton;
            so.FindProperty("_codeInput").objectReferenceValue = codeInput;
            so.FindProperty("_waitingGroup").objectReferenceValue = waitingObj;
            so.FindProperty("_statusText").objectReferenceValue = statusText;
            so.FindProperty("_cancelButton").objectReferenceValue = cancelButton;
            so.ApplyModifiedProperties();

            panelObj.SetActive(false);
            return panel;
        }

        private static Button CreatePanelButton(Transform parent, string name, string label, Vector2 position, Vector2 size, Color color)
        {
            var obj = UIDialogBuilder.CreateButton(parent, name, label, size.x, size.y, color);
            SetCenteredRect(obj.GetComponent<RectTransform>(), position, size);
            obj.GetComponentInChildren<Text>().fontSize = 30;
            return obj.GetComponent<Button>();
        }

        private static Text CreatePanelText(Transform parent, string name, string content, int fontSize, Vector2 position, Vector2 size)
        {
            var obj = UIDialogBuilder.CreateUIObject(name, parent);
            SetCenteredRect(obj.GetComponent<RectTransform>(), position, size);

            var text = obj.AddComponent<Text>();
            text.text = content;
            text.fontSize = fontSize;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        /// <summary>参加コードの入力欄（Relayの参加コードは英数字のみ）</summary>
        private static InputField CreateCodeInput(Transform parent, Vector2 position, Vector2 size)
        {
            var obj = UIDialogBuilder.CreateUIObject("CodeInput", parent);
            SetCenteredRect(obj.GetComponent<RectTransform>(), position, size);
            obj.AddComponent<Image>().color = Color.white;

            Text placeholder = CreateInputText(obj.transform, "Placeholder", "コードを入力", new Color(0.5f, 0.5f, 0.5f));
            Text text = CreateInputText(obj.transform, "Text", "", Color.black);
            text.supportRichText = false;

            var input = obj.AddComponent<InputField>();
            input.textComponent = text;
            input.placeholder = placeholder;
            input.characterValidation = InputField.CharacterValidation.Alphanumeric;
            input.characterLimit = 12;
            return input;
        }

        private static Text CreateInputText(Transform parent, string name, string content, Color color)
        {
            var obj = UIDialogBuilder.CreateUIObject(name, parent);
            var rect = obj.GetComponent<RectTransform>();
            UIDialogBuilder.SetStretchAll(rect);
            rect.offsetMin = new Vector2(16f, 8f);
            rect.offsetMax = new Vector2(-16f, -8f);

            var text = obj.AddComponent<Text>();
            text.text = content;
            text.fontSize = 36;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = color;
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
