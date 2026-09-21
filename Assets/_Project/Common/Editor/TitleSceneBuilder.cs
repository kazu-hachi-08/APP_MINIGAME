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
            var settingsBtnObj = UIDialogBuilder.CreateButton(footerObj.transform, "Btn_Settings", "⚙ 設定", 160, 50, new Color(0.25f, 0.28f, 0.36f));
            var sRect = settingsBtnObj.GetComponent<RectTransform>();
            sRect.anchorMin = new Vector2(0, 0.5f);
            sRect.anchorMax = new Vector2(0, 0.5f);
            sRect.pivot = new Vector2(0, 0.5f);
            sRect.anchoredPosition = new Vector2(40, 0);
            var settingsButton = settingsBtnObj.GetComponent<Button>();

            // 終了ボタン
            var quitBtnObj = UIDialogBuilder.CreateButton(footerObj.transform, "Btn_Quit", "✕ 終了", 140, 50, new Color(0.35f, 0.18f, 0.2f));
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
            UIDialogBuilder.BuildDialogs(canvasObj.transform, uiManager);

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
