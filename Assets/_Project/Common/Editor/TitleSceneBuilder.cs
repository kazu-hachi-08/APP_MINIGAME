using System.IO;
using MiniGame.Common.Audio;
using MiniGame.Common.Scene;
using MiniGame.Common.Title;
using MiniGame.Common.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MiniGame.Editor
{
    /// <summary>
    /// TitleScene を自動生成・セットアップするエディタユーティリティ
    /// </summary>
    public static class TitleSceneBuilder
    {
        private const string SceneDirectory = "Assets/_Project/Common/Scenes";
        private const string ScenePath = SceneDirectory + "/TitleScene.unity";

        [MenuItem("Tools/MiniGame/Build Title Scene", false, 1)]
        public static void BuildTitleScene()
        {
            BuildTitleSceneInternal(force: true);
        }

        [InitializeOnLoadMethod]
        private static void OnProjectLoaded()
        {
            if (!File.Exists(ScenePath))
            {
                Debug.Log($"[TitleSceneBuilder] {ScenePath} が存在しないため、自動生成を実行します。");
                BuildTitleSceneInternal(force: false);
            }
        }

        private static void BuildTitleSceneInternal(bool force)
        {
            Debug.Log("[TitleSceneBuilder] TitleScene の構築を開始します...");

            if (!Directory.Exists(SceneDirectory))
            {
                Directory.CreateDirectory(SceneDirectory);
            }

            // 新規シーン作成
            UnityEngine.SceneManagement.Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. Camera
            var cameraObj = new GameObject("Main Camera");
            var camera = cameraObj.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.08f, 0.09f, 0.13f); // ダークブルーグレー
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            cameraObj.AddComponent<AudioListener>();
            cameraObj.tag = "MainCamera";

            // 2. EventSystem
            var eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<EventSystem>();
            var inputModule = eventSystemObj.AddComponent<InputSystemUIInputModule>();
            inputModule.AssignDefaultActions();

            // 3. Managers
            var managersRoot = new GameObject("--- Managers ---");

            var sceneLoaderObj = new GameObject("SceneLoader");
            sceneLoaderObj.transform.SetParent(managersRoot.transform);
            var sceneLoader = sceneLoaderObj.AddComponent<SceneLoader>();

            var audioManagerObj = new GameObject("AudioManager");
            audioManagerObj.transform.SetParent(managersRoot.transform);
            var audioManager = audioManagerObj.AddComponent<AudioManager>();

            var uiManagerObj = new GameObject("UIManager");
            uiManagerObj.transform.SetParent(managersRoot.transform);
            var uiManager = uiManagerObj.AddComponent<UIManager>();

            // 4. Main UI Canvas
            var canvasObj = new GameObject("Canvas");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;

            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            // UI Root / Controller
            var titleController = canvasObj.AddComponent<TitleController>();

            // --- UI 階層作成 ---
            // 4.1 背景
            var bgObj = CreateUIObject("Background", canvasObj.transform);
            SetStretchAll(bgObj.GetComponent<RectTransform>());
            var bgImg = bgObj.AddComponent<Image>();
            bgImg.color = new Color(0.07f, 0.08f, 0.12f, 1f);

            // 装飾アクセントバー（上部）
            var topBar = CreateUIObject("TopAccentBar", bgObj.transform);
            var topBarRect = topBar.GetComponent<RectTransform>();
            topBarRect.anchorMin = new Vector2(0, 1);
            topBarRect.anchorMax = new Vector2(1, 1);
            topBarRect.pivot = new Vector2(0.5f, 1);
            topBarRect.sizeDelta = new Vector2(0, 6);
            topBarRect.anchoredPosition = Vector2.zero;
            var topBarImg = topBar.AddComponent<Image>();
            topBarImg.color = new Color(0.2f, 0.6f, 1f, 0.9f);

            // 4.2 タイトルヘッダー
            var headerObj = CreateUIObject("Header", canvasObj.transform);
            var headerRect = headerObj.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0, 0.65f);
            headerRect.anchorMax = new Vector2(1, 1);
            headerRect.offsetMin = Vector2.zero;
            headerRect.offsetMax = Vector2.zero;

            var titleTextObj = CreateUIObject("TitleText", headerObj.transform);
            var titleTextRect = titleTextObj.GetComponent<RectTransform>();
            titleTextRect.anchorMin = new Vector2(0, 0.3f);
            titleTextRect.anchorMax = new Vector2(1, 0.9f);
            titleTextRect.offsetMin = Vector2.zero;
            titleTextRect.offsetMax = Vector2.zero;
            var titleText = titleTextObj.AddComponent<Text>();
            titleText.text = "MINI GAME HUB";
            titleText.fontSize = 64;
            titleText.fontStyle = FontStyle.Bold;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.color = new Color(0.95f, 0.96f, 0.98f);

            var subtitleTextObj = CreateUIObject("SubtitleText", headerObj.transform);
            var subRect = subtitleTextObj.GetComponent<RectTransform>();
            subRect.anchorMin = new Vector2(0, 0.05f);
            subRect.anchorMax = new Vector2(1, 0.35f);
            subRect.offsetMin = Vector2.zero;
            subRect.offsetMax = Vector2.zero;
            var subtitleText = subtitleTextObj.AddComponent<Text>();
            subtitleText.text = "週末 2D ミニゲームコレクション";
            subtitleText.fontSize = 26;
            subtitleText.alignment = TextAnchor.MiddleCenter;
            subtitleText.color = new Color(0.6f, 0.7f, 0.85f);

            // 4.3 ゲーム選択コンテナ
            var menuContainerObj = CreateUIObject("GameSelectionContainer", canvasObj.transform);
            var menuRect = menuContainerObj.GetComponent<RectTransform>();
            menuRect.anchorMin = new Vector2(0.5f, 0.5f);
            menuRect.anchorMax = new Vector2(0.5f, 0.5f);
            menuRect.pivot = new Vector2(0.5f, 0.5f);
            menuRect.sizeDelta = new Vector2(640, 360);
            menuRect.anchoredPosition = new Vector2(0, -30);

            var menuLayout = menuContainerObj.AddComponent<VerticalLayoutGroup>();
            menuLayout.spacing = 18;
            menuLayout.childAlignment = TextAnchor.MiddleCenter;
            menuLayout.childControlHeight = false;
            menuLayout.childControlWidth = true;
            menuLayout.childForceExpandHeight = false;
            menuLayout.childForceExpandWidth = true;

            // ミニゲームボタン①：2Dサッカー
            CreateGameSelectButton(
                menuContainerObj.transform,
                buttonName: "Btn_Game_Soccer",
                gameTitle: "⚽ 2D サッカーゲーム",
                targetScene: SceneNames.Soccer,
                isPlayable: true,
                baseColor: new Color(0.12f, 0.45f, 0.85f)
            );

            // ミニゲームボタン②：今後追加
            CreateGameSelectButton(
                menuContainerObj.transform,
                buttonName: "Btn_Game_MiniGame2",
                gameTitle: "🔒 ミニゲーム ② (Coming Soon)",
                targetScene: "",
                isPlayable: false,
                baseColor: new Color(0.2f, 0.22f, 0.28f)
            );

            // ミニゲームボタン③：今後追加
            CreateGameSelectButton(
                menuContainerObj.transform,
                buttonName: "Btn_Game_MiniGame3",
                gameTitle: "🔒 ミニゲーム ③ (Coming Soon)",
                targetScene: "",
                isPlayable: false,
                baseColor: new Color(0.2f, 0.22f, 0.28f)
            );

            // 4.4 フッターバー
            var footerObj = CreateUIObject("FooterBar", canvasObj.transform);
            var footerRect = footerObj.GetComponent<RectTransform>();
            footerRect.anchorMin = new Vector2(0, 0);
            footerRect.anchorMax = new Vector2(1, 0);
            footerRect.pivot = new Vector2(0.5f, 0);
            footerRect.sizeDelta = new Vector2(0, 80);
            footerRect.anchoredPosition = Vector2.zero;

            // 設定ボタン
            var settingsBtnObj = CreateButton(footerObj.transform, "Btn_Settings", "⚙ 設定", 160, 50, new Color(0.25f, 0.28f, 0.36f));
            var sRect = settingsBtnObj.GetComponent<RectTransform>();
            sRect.anchorMin = new Vector2(0, 0.5f);
            sRect.anchorMax = new Vector2(0, 0.5f);
            sRect.pivot = new Vector2(0, 0.5f);
            sRect.anchoredPosition = new Vector2(40, 0);
            var settingsButton = settingsBtnObj.GetComponent<Button>();

            // 終了ボタン
            var quitBtnObj = CreateButton(footerObj.transform, "Btn_Quit", "✕ 終了", 140, 50, new Color(0.35f, 0.18f, 0.2f));
            var qRect = quitBtnObj.GetComponent<RectTransform>();
            qRect.anchorMin = new Vector2(0, 0.5f);
            qRect.anchorMax = new Vector2(0, 0.5f);
            qRect.pivot = new Vector2(0, 0.5f);
            qRect.anchoredPosition = new Vector2(220, 0);
            var quitButton = quitBtnObj.GetComponent<Button>();

            // バージョンテキスト
            var verObj = CreateUIObject("VersionText", footerObj.transform);
            var verRect = verObj.GetComponent<RectTransform>();
            verRect.anchorMin = new Vector2(1, 0.5f);
            verRect.anchorMax = new Vector2(1, 0.5f);
            verRect.pivot = new Vector2(1, 0.5f);
            verRect.sizeDelta = new Vector2(200, 40);
            verRect.anchoredPosition = new Vector2(-40, 0);
            var versionText = verObj.AddComponent<Text>();
            versionText.text = "v0.1.0";
            versionText.fontSize = 20;
            versionText.alignment = TextAnchor.MiddleRight;
            versionText.color = new Color(0.5f, 0.55f, 0.65f);

            // TitleController のインスペクター参照セット
            var titleSo = new SerializedObject(titleController);
            titleSo.FindProperty("_versionText").objectReferenceValue = versionText;
            titleSo.FindProperty("_settingsButton").objectReferenceValue = settingsButton;
            titleSo.FindProperty("_quitButton").objectReferenceValue = quitButton;
            titleSo.ApplyModifiedProperties();

            // 5. 共通ダイアログ群の作成と配置（初期非アクティブ）
            var dialogsRoot = CreateUIObject("--- Dialogs ---", canvasObj.transform);
            SetStretchAll(dialogsRoot.GetComponent<RectTransform>());

            var commonDialog = CreateCommonDialog(dialogsRoot.transform);
            var pauseDialog = CreatePauseDialog(dialogsRoot.transform);
            var resultDialog = CreateResultDialog(dialogsRoot.transform);

            // UIManager にダイアログ参照をセット
            var uiMgrSo = new SerializedObject(uiManager);
            uiMgrSo.FindProperty("_commonDialog").objectReferenceValue = commonDialog;
            uiMgrSo.FindProperty("_pauseDialog").objectReferenceValue = pauseDialog;
            uiMgrSo.FindProperty("_resultDialog").objectReferenceValue = resultDialog;
            uiMgrSo.ApplyModifiedProperties();

            // シーンの保存
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[TitleSceneBuilder] TitleScene が正常に生成・保存されました: {ScenePath}");

            // Build Settings に登録
            RegisterSceneInBuildSettings(ScenePath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static GameObject CreateGameSelectButton(
            Transform parent,
            string buttonName,
            string gameTitle,
            string targetScene,
            bool isPlayable,
            Color baseColor)
        {
            var btnObj = CreateUIObject(buttonName, parent);
            var rect = btnObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(600, 72);

            var img = btnObj.AddComponent<Image>();
            img.color = isPlayable ? baseColor : new Color(baseColor.r, baseColor.g, baseColor.b, 0.6f);

            var btn = btnObj.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f);
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f);
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.6f);
            btn.colors = colors;

            // テキスト
            var textObj = CreateUIObject("Text", btnObj.transform);
            SetStretchAll(textObj.GetComponent<RectTransform>());
            var text = textObj.AddComponent<Text>();
            text.text = gameTitle;
            text.fontSize = 28;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = isPlayable ? Color.white : new Color(0.7f, 0.7f, 0.7f);

            // ロックオーバーレイ
            var lockObj = CreateUIObject("LockOverlay", btnObj.transform);
            SetStretchAll(lockObj.GetComponent<RectTransform>());
            var lockImg = lockObj.AddComponent<Image>();
            lockImg.color = new Color(0f, 0f, 0f, 0.35f);
            lockObj.SetActive(!isPlayable);

            // MiniGameSelectButton コンポーネント
            var selectButton = btnObj.AddComponent<MiniGameSelectButton>();
            var so = new SerializedObject(selectButton);
            so.FindProperty("_targetSceneName").stringValue = targetScene;
            so.FindProperty("_gameTitle").stringValue = gameTitle;
            so.FindProperty("_isPlayable").boolValue = isPlayable;
            so.FindProperty("_button").objectReferenceValue = btn;
            so.FindProperty("_titleText").objectReferenceValue = text;
            so.FindProperty("_lockOverlay").objectReferenceValue = lockObj;
            so.ApplyModifiedProperties();

            return btnObj;
        }

        private static GameObject CreateButton(Transform parent, string name, string label, float width, float height, Color color)
        {
            var btnObj = CreateUIObject(name, parent);
            var rect = btnObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(width, height);

            var img = btnObj.AddComponent<Image>();
            img.color = color;

            var btn = btnObj.AddComponent<Button>();

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

        private static CommonDialog CreateCommonDialog(Transform parent)
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

        private static PauseDialog CreatePauseDialog(Transform parent)
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

        private static ResultDialog CreateResultDialog(Transform parent)
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

        private static GameObject CreateUIObject(string name, Transform parent)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.AddComponent<RectTransform>();
            return obj;
        }

        private static void SetStretchAll(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
        }

        private static void RegisterSceneInBuildSettings(string scenePath)
        {
            var scenes = EditorBuildSettings.scenes;
            foreach (var s in scenes)
            {
                if (s.path == scenePath)
                {
                    Debug.Log($"[TitleSceneBuilder] Build Settings に既に登録されています: {scenePath}");
                    return;
                }
            }

            var newScenes = new EditorBuildSettingsScene[scenes.Length + 1];
            newScenes[0] = new EditorBuildSettingsScene(scenePath, true);
            for (int i = 0; i < scenes.Length; i++)
            {
                newScenes[i + 1] = scenes[i];
            }

            EditorBuildSettings.scenes = newScenes;
            Debug.Log($"[TitleSceneBuilder] Build Settings に {scenePath} (Index 0) を登録しました。");
        }
    }
}
