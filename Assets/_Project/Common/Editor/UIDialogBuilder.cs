using MiniGame.Common.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.Editor
{
    /// <summary>
    /// 共通ダイアログ（確認・ポーズ・リザルト）のUIをシーンへ生成するエディタユーティリティ
    /// タイトルと各ミニゲームのSceneBuilderから共有して使う
    /// </summary>
    public static class UIDialogBuilder
    {
        /// <summary>
        /// 共通ダイアログ一式を生成し、UIManager へ参照を設定する
        /// </summary>
        public static void BuildDialogs(Transform canvas, UIManager uiManager)
        {
            var dialogsRoot = CreateUIObject("--- Dialogs ---", canvas);
            SetStretchAll(dialogsRoot.GetComponent<RectTransform>());

            var commonDialog = CreateCommonDialog(dialogsRoot.transform);
            var pauseDialog = CreatePauseDialog(dialogsRoot.transform);
            var resultDialog = CreateResultDialog(dialogsRoot.transform);

            var so = new SerializedObject(uiManager);
            so.FindProperty("_commonDialog").objectReferenceValue = commonDialog;
            so.FindProperty("_pauseDialog").objectReferenceValue = pauseDialog;
            so.FindProperty("_resultDialog").objectReferenceValue = resultDialog;
            so.ApplyModifiedProperties();
        }

        public static CommonDialog CreateCommonDialog(Transform parent)
        {
            var dialogObj = CreateUIObject("CommonDialog", parent);
            SetStretchAll(dialogObj.GetComponent<RectTransform>());

            var bgImg = dialogObj.AddComponent<Image>();
            bgImg.color = new Color(0, 0, 0, 0.65f);

            // パネル本体
            var panelObj = CreateUIObject("Panel", dialogObj.transform);
            var pRect = panelObj.GetComponent<RectTransform>();
            pRect.anchorMin = new Vector2(0.5f, 0.5f);
            pRect.anchorMax = new Vector2(0.5f, 0.5f);
            pRect.pivot = new Vector2(0.5f, 0.5f);
            pRect.sizeDelta = new Vector2(520, 320);
            var panelImg = panelObj.AddComponent<Image>();
            panelImg.color = new Color(0.14f, 0.16f, 0.22f);

            // タイトル
            var titleObj = CreateUIObject("TitleText", panelObj.transform);
            var tRect = titleObj.GetComponent<RectTransform>();
            tRect.anchorMin = new Vector2(0, 0.75f);
            tRect.anchorMax = new Vector2(1, 1);
            tRect.offsetMin = Vector2.zero;
            tRect.offsetMax = Vector2.zero;
            var titleText = titleObj.AddComponent<Text>();
            titleText.text = "確認";
            titleText.fontSize = 26;
            titleText.fontStyle = FontStyle.Bold;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.color = Color.white;

            // メッセージ
            var msgObj = CreateUIObject("MessageText", panelObj.transform);
            var mRect = msgObj.GetComponent<RectTransform>();
            mRect.anchorMin = new Vector2(0.08f, 0.35f);
            mRect.anchorMax = new Vector2(0.92f, 0.75f);
            mRect.offsetMin = Vector2.zero;
            mRect.offsetMax = Vector2.zero;
            var msgText = msgObj.AddComponent<Text>();
            msgText.text = "メッセージ内容";
            msgText.fontSize = 20;
            msgText.alignment = TextAnchor.MiddleCenter;
            msgText.color = new Color(0.85f, 0.88f, 0.95f);

            // ボタンエリア
            var btnAreaObj = CreateUIObject("ButtonArea", panelObj.transform);
            var bRect = btnAreaObj.GetComponent<RectTransform>();
            bRect.anchorMin = new Vector2(0, 0);
            bRect.anchorMax = new Vector2(1, 0.35f);
            bRect.offsetMin = Vector2.zero;
            bRect.offsetMax = Vector2.zero;
            var layout = btnAreaObj.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 20;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;

            var cancelBtnObj = CreateButton(btnAreaObj.transform, "Btn_Cancel", "キャンセル", 150, 48, new Color(0.35f, 0.38f, 0.45f));
            var confirmBtnObj = CreateButton(btnAreaObj.transform, "Btn_Confirm", "OK", 150, 48, new Color(0.18f, 0.55f, 0.9f));

            var dialog = dialogObj.AddComponent<CommonDialog>();
            var so = new SerializedObject(dialog);
            so.FindProperty("_titleText").objectReferenceValue = titleText;
            so.FindProperty("_messageText").objectReferenceValue = msgText;
            so.FindProperty("_confirmButton").objectReferenceValue = confirmBtnObj.GetComponent<Button>();
            so.FindProperty("_confirmButtonText").objectReferenceValue = confirmBtnObj.GetComponentInChildren<Text>();
            so.FindProperty("_cancelButton").objectReferenceValue = cancelBtnObj.GetComponent<Button>();
            so.FindProperty("_cancelButtonText").objectReferenceValue = cancelBtnObj.GetComponentInChildren<Text>();
            so.ApplyModifiedProperties();

            dialogObj.SetActive(false);
            return dialog;
        }

        public static PauseDialog CreatePauseDialog(Transform parent)
        {
            var dialogObj = CreateUIObject("PauseDialog", parent);
            SetStretchAll(dialogObj.GetComponent<RectTransform>());

            var bgImg = dialogObj.AddComponent<Image>();
            bgImg.color = new Color(0, 0, 0, 0.75f);

            var panelObj = CreateUIObject("Panel", dialogObj.transform);
            var pRect = panelObj.GetComponent<RectTransform>();
            pRect.anchorMin = new Vector2(0.5f, 0.5f);
            pRect.anchorMax = new Vector2(0.5f, 0.5f);
            pRect.pivot = new Vector2(0.5f, 0.5f);
            pRect.sizeDelta = new Vector2(560, 480);
            var panelImg = panelObj.AddComponent<Image>();
            panelImg.color = new Color(0.12f, 0.14f, 0.18f);

            // タイトル
            var titleObj = CreateUIObject("TitleText", panelObj.transform);
            var tRect = titleObj.GetComponent<RectTransform>();
            tRect.anchorMin = new Vector2(0, 0.85f);
            tRect.anchorMax = new Vector2(1, 1);
            tRect.offsetMin = Vector2.zero;
            tRect.offsetMax = Vector2.zero;
            var titleText = titleObj.AddComponent<Text>();
            titleText.text = "PAUSE / 設定";
            titleText.fontSize = 28;
            titleText.fontStyle = FontStyle.Bold;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.color = Color.white;

            // ボリュームスライダーエリア
            var audioArea = CreateUIObject("AudioSettings", panelObj.transform);
            var aRect = audioArea.GetComponent<RectTransform>();
            aRect.anchorMin = new Vector2(0.1f, 0.45f);
            aRect.anchorMax = new Vector2(0.9f, 0.82f);
            aRect.offsetMin = Vector2.zero;
            aRect.offsetMax = Vector2.zero;

            var bgmSlider = CreateVolumeSlider("BgmSlider", audioArea.transform, "BGM 音量", new Vector2(0, 60));
            var seSlider = CreateVolumeSlider("SeSlider", audioArea.transform, "SE 音量", new Vector2(0, 10));

            // ボタンエリア
            var btnArea = CreateUIObject("ButtonArea", panelObj.transform);
            var bRect = btnArea.GetComponent<RectTransform>();
            bRect.anchorMin = new Vector2(0.1f, 0.05f);
            bRect.anchorMax = new Vector2(0.9f, 0.42f);
            bRect.offsetMin = Vector2.zero;
            bRect.offsetMax = Vector2.zero;
            var layout = btnArea.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 14;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;

            var resumeBtn = CreateButton(btnArea.transform, "Btn_Resume", "再開する", 380, 48, new Color(0.18f, 0.55f, 0.9f));
            var restartBtn = CreateButton(btnArea.transform, "Btn_Restart", "リトライ", 380, 48, new Color(0.35f, 0.4f, 0.5f));
            var titleBtn = CreateButton(btnArea.transform, "Btn_Title", "タイトルへ戻る", 380, 48, new Color(0.45f, 0.25f, 0.28f));

            var pause = dialogObj.AddComponent<PauseDialog>();
            var so = new SerializedObject(pause);
            so.FindProperty("_resumeButton").objectReferenceValue = resumeBtn.GetComponent<Button>();
            so.FindProperty("_restartButton").objectReferenceValue = restartBtn.GetComponent<Button>();
            so.FindProperty("_titleButton").objectReferenceValue = titleBtn.GetComponent<Button>();
            so.FindProperty("_bgmSlider").objectReferenceValue = bgmSlider;
            so.FindProperty("_seSlider").objectReferenceValue = seSlider;
            so.ApplyModifiedProperties();

            dialogObj.SetActive(false);
            return pause;
        }

        public static ResultDialog CreateResultDialog(Transform parent)
        {
            var dialogObj = CreateUIObject("ResultDialog", parent);
            SetStretchAll(dialogObj.GetComponent<RectTransform>());

            var bgImg = dialogObj.AddComponent<Image>();
            bgImg.color = new Color(0, 0, 0, 0.75f);

            var panelObj = CreateUIObject("Panel", dialogObj.transform);
            var pRect = panelObj.GetComponent<RectTransform>();
            pRect.anchorMin = new Vector2(0.5f, 0.5f);
            pRect.anchorMax = new Vector2(0.5f, 0.5f);
            pRect.pivot = new Vector2(0.5f, 0.5f);
            pRect.sizeDelta = new Vector2(560, 420);
            var panelImg = panelObj.AddComponent<Image>();
            panelImg.color = new Color(0.12f, 0.15f, 0.2f);

            var titleObj = CreateUIObject("TitleText", panelObj.transform);
            var tRect = titleObj.GetComponent<RectTransform>();
            tRect.anchorMin = new Vector2(0, 0.8f);
            tRect.anchorMax = new Vector2(1, 1);
            tRect.offsetMin = Vector2.zero;
            tRect.offsetMax = Vector2.zero;
            var titleText = titleObj.AddComponent<Text>();
            titleText.text = "RESULT";
            titleText.fontSize = 32;
            titleText.fontStyle = FontStyle.Bold;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.color = new Color(1f, 0.85f, 0.2f);

            var scoreObj = CreateUIObject("ScoreText", panelObj.transform);
            var sRect = scoreObj.GetComponent<RectTransform>();
            sRect.anchorMin = new Vector2(0.05f, 0.55f);
            sRect.anchorMax = new Vector2(0.95f, 0.8f);
            sRect.offsetMin = Vector2.zero;
            sRect.offsetMax = Vector2.zero;
            var scoreText = scoreObj.AddComponent<Text>();
            scoreText.text = "SCORE: 100";
            scoreText.fontSize = 36;
            scoreText.fontStyle = FontStyle.Bold;
            scoreText.alignment = TextAnchor.MiddleCenter;
            scoreText.color = Color.white;

            var detailObj = CreateUIObject("DetailText", panelObj.transform);
            var dRect = detailObj.GetComponent<RectTransform>();
            dRect.anchorMin = new Vector2(0.05f, 0.4f);
            dRect.anchorMax = new Vector2(0.95f, 0.55f);
            dRect.offsetMin = Vector2.zero;
            dRect.offsetMax = Vector2.zero;
            var detailText = detailObj.AddComponent<Text>();
            detailText.text = "クリアタイム: 01:23";
            detailText.fontSize = 20;
            detailText.alignment = TextAnchor.MiddleCenter;
            detailText.color = new Color(0.7f, 0.75f, 0.85f);

            var btnArea = CreateUIObject("ButtonArea", panelObj.transform);
            var bRect = btnArea.GetComponent<RectTransform>();
            bRect.anchorMin = new Vector2(0.05f, 0.08f);
            bRect.anchorMax = new Vector2(0.95f, 0.35f);
            bRect.offsetMin = Vector2.zero;
            bRect.offsetMax = Vector2.zero;
            var layout = btnArea.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 20;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;

            var retryBtn = CreateButton(btnArea.transform, "Btn_Retry", "もう一度遊ぶ", 180, 52, new Color(0.18f, 0.55f, 0.9f));
            var titleBtn = CreateButton(btnArea.transform, "Btn_Title", "タイトルへ", 180, 52, new Color(0.35f, 0.38f, 0.45f));

            var result = dialogObj.AddComponent<ResultDialog>();
            var so = new SerializedObject(result);
            so.FindProperty("_resultTitleText").objectReferenceValue = titleText;
            so.FindProperty("_scoreText").objectReferenceValue = scoreText;
            so.FindProperty("_detailText").objectReferenceValue = detailText;
            so.FindProperty("_retryButton").objectReferenceValue = retryBtn.GetComponent<Button>();
            so.FindProperty("_titleButton").objectReferenceValue = titleBtn.GetComponent<Button>();
            so.ApplyModifiedProperties();

            dialogObj.SetActive(false);
            return result;
        }

        public static GameObject CreateButton(Transform parent, string name, string label, float width, float height, Color color)
        {
            var btnObj = CreateUIObject(name, parent);
            var rect = btnObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(width, height);

            var img = btnObj.AddComponent<Image>();
            img.color = color;

            btnObj.AddComponent<Button>();

            var textObj = CreateUIObject("Text", btnObj.transform);
            SetStretchAll(textObj.GetComponent<RectTransform>());
            var text = textObj.AddComponent<Text>();
            text.text = label;
            text.fontSize = 22;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;

            return btnObj;
        }

        public static GameObject CreateUIObject(string name, Transform parent)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.AddComponent<RectTransform>();
            return obj;
        }

        public static void SetStretchAll(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
        }

        private static Slider CreateVolumeSlider(string name, Transform parent, string label, Vector2 anchoredPos)
        {
            var sliderObj = CreateUIObject(name, parent);
            var sRect = sliderObj.GetComponent<RectTransform>();
            sRect.anchorMin = new Vector2(0.5f, 0.5f);
            sRect.anchorMax = new Vector2(0.5f, 0.5f);
            sRect.pivot = new Vector2(0.5f, 0.5f);
            sRect.sizeDelta = new Vector2(360, 36);
            sRect.anchoredPosition = anchoredPos;

            var labelObj = CreateUIObject("Label", sliderObj.transform);
            var lRect = labelObj.GetComponent<RectTransform>();
            lRect.anchorMin = new Vector2(0, 0);
            lRect.anchorMax = new Vector2(0.35f, 1);
            lRect.offsetMin = Vector2.zero;
            lRect.offsetMax = Vector2.zero;
            var text = labelObj.AddComponent<Text>();
            text.text = label;
            text.fontSize = 18;
            text.alignment = TextAnchor.MiddleLeft;
            text.color = Color.white;

            var barObj = CreateUIObject("SliderBar", sliderObj.transform);
            var barRect = barObj.GetComponent<RectTransform>();
            barRect.anchorMin = new Vector2(0.4f, 0.3f);
            barRect.anchorMax = new Vector2(1f, 0.7f);
            barRect.offsetMin = Vector2.zero;
            barRect.offsetMax = Vector2.zero;
            var barImg = barObj.AddComponent<Image>();
            barImg.color = new Color(0.3f, 0.35f, 0.45f);

            var fillArea = CreateUIObject("Fill Area", barObj.transform);
            SetStretchAll(fillArea.GetComponent<RectTransform>());

            var fill = CreateUIObject("Fill", fillArea.transform);
            var fRect = fill.GetComponent<RectTransform>();
            fRect.anchorMin = Vector2.zero;
            fRect.anchorMax = Vector2.one;
            fRect.sizeDelta = Vector2.zero;
            var fillImg = fill.AddComponent<Image>();
            fillImg.color = new Color(0.2f, 0.6f, 1f);

            var slider = barObj.AddComponent<Slider>();
            slider.fillRect = fRect;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0.8f;

            return slider;
        }
    }
}
