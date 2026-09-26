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

        // Unity標準の角丸・円スプライト。画像素材を追加せずに柔らかい見た目にするため
        private const string RoundedSpritePath = "UI/Skin/UISprite.psd";
        private const string CircleSpritePath = "UI/Skin/Knob.psd";

        // タイトルは ScreenOrientationApplier で縦画面固定のため、縦長を基準解像度にする
        private static readonly Vector2 ReferenceResolution = new Vector2(1080, 1920);

        // サイズは基準幅1080換算。縦持ちスマホで指（約8〜9mm）で確実に押せる大きさにしている
        private const int TitleFontSize = 96;
        private const int SubtitleFontSize = 38;
        private const float HeaderHeight = 250f;
        private const float HeaderTopMargin = 180f;

        private const float GameButtonWidth = 820f;
        private const float GameButtonHeight = 140f;
        private const float GameButtonSpacing = 32f;
        private const int GameButtonFontSize = 52;
        private const int GameButtonArrowFontSize = 40;
        private const float GameButtonAccentWidth = 22f;
        private const float GameButtonLabelPadding = 64f;
        private const float MenuCenterOffsetY = -10f;

        private const float FooterButtonWidth = 280f;
        private const float FooterButtonHeight = 110f;
        private const int FooterButtonFontSize = 38;
        private const float FooterButtonSpacing = 48f;
        // 画面下端から離し、親指が自然に届く高さに置く
        private const float FooterBottomMargin = 160f;

        private const int VersionFontSize = 24;

        private static readonly Color BaseBgColor = new Color(0.06f, 0.07f, 0.12f);
        private static readonly Color CardColor = new Color(0.14f, 0.16f, 0.24f);
        private static readonly Color AccentColor = new Color(0.25f, 0.65f, 1f);

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
            scaler.referenceResolution = ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            // 縦長スマホは機種ごとに高さだけが大きく違うため、幅基準にしてボタンが左右にはみ出さないようにする
            scaler.matchWidthOrHeight = 0f;

            canvasObj.AddComponent<GraphicRaycaster>();

            // UI Root / Controller
            var titleController = canvasObj.AddComponent<TitleController>();

            // --- UI 階層作成 ---
            // 背景は画面全体、操作UIはノッチ・ホームバーを避けるため SafeArea 内に置く
            BuildBackground(canvasObj.transform);

            var safeAreaObj = CreateUIObject("SafeArea", canvasObj.transform);
            SetStretchAll(safeAreaObj.GetComponent<RectTransform>());
            safeAreaObj.AddComponent<SafeAreaFitter>();

            BuildHeader(safeAreaObj.transform);
            BuildGameMenu(safeAreaObj.transform);
            BuildFooter(safeAreaObj.transform, out var settingsButton, out var quitButton);
            var versionText = BuildVersionText(safeAreaObj.transform);

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

        private static void BuildBackground(Transform parent)
        {
            var bgObj = CreateUIObject("Background", parent);
            SetStretchAll(bgObj.GetComponent<RectTransform>());
            var bgImg = bgObj.AddComponent<Image>();
            bgImg.color = BaseBgColor;
            bgImg.raycastTarget = false;

            // 単色だと寂しいので、画面外にはみ出す大きな半透明の円で奥行きを出す
            CreateGlowCircle(bgObj.transform, "GlowTopRight", new Vector2(1, 1), 1000f, new Color(AccentColor.r, AccentColor.g, AccentColor.b, 0.12f));
            CreateGlowCircle(bgObj.transform, "GlowBottomLeft", new Vector2(0, 0), 800f, new Color(0.6f, 0.3f, 0.9f, 0.10f));

            var topBar = CreateUIObject("TopAccentBar", bgObj.transform);
            var topBarRect = topBar.GetComponent<RectTransform>();
            topBarRect.anchorMin = new Vector2(0, 1);
            topBarRect.anchorMax = new Vector2(1, 1);
            topBarRect.pivot = new Vector2(0.5f, 1);
            topBarRect.sizeDelta = new Vector2(0, 6);
            topBarRect.anchoredPosition = Vector2.zero;
            var topBarImg = topBar.AddComponent<Image>();
            topBarImg.color = AccentColor;
            topBarImg.raycastTarget = false;
        }

        private static void CreateGlowCircle(Transform parent, string name, Vector2 anchor, float diameter, Color color)
        {
            var obj = CreateUIObject(name, parent);
            var rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(diameter, diameter);
            rect.anchoredPosition = Vector2.zero;
            var img = obj.AddComponent<Image>();
            img.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>(CircleSpritePath);
            img.color = color;
            img.raycastTarget = false;
        }

        private static void BuildHeader(Transform parent)
        {
            var headerObj = CreateUIObject("Header", parent);
            var headerRect = headerObj.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0, 1);
            headerRect.anchorMax = new Vector2(1, 1);
            headerRect.pivot = new Vector2(0.5f, 1);
            headerRect.sizeDelta = new Vector2(0, HeaderHeight);
            headerRect.anchoredPosition = new Vector2(0, -HeaderTopMargin);

            var titleText = CreateText(headerObj.transform, "TitleText", "MINI GAME HUB", TitleFontSize, new Color(0.96f, 0.97f, 1f), TextAnchor.MiddleCenter);
            titleText.fontStyle = FontStyle.Bold;
            SetAnchors(titleText.rectTransform, new Vector2(0, 0.35f), Vector2.one);
            // アクセント色の影を落として、フォントを追加せずに発光っぽい立体感を出す
            var shadow = titleText.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(AccentColor.r, AccentColor.g, AccentColor.b, 0.7f);
            shadow.effectDistance = new Vector2(0, -6);

            var subtitleText = CreateText(headerObj.transform, "SubtitleText", "週末 2D ミニゲームコレクション", SubtitleFontSize, new Color(0.65f, 0.75f, 0.9f), TextAnchor.MiddleCenter);
            SetAnchors(subtitleText.rectTransform, Vector2.zero, new Vector2(1, 0.35f));
        }

        private static void BuildGameMenu(Transform parent)
        {
            var menuContainerObj = CreateUIObject("GameSelectionContainer", parent);
            var menuRect = menuContainerObj.GetComponent<RectTransform>();
            menuRect.anchorMin = new Vector2(0.5f, 0.5f);
            menuRect.anchorMax = new Vector2(0.5f, 0.5f);
            menuRect.pivot = new Vector2(0.5f, 0.5f);
            menuRect.sizeDelta = new Vector2(GameButtonWidth, 0);
            menuRect.anchoredPosition = new Vector2(0, MenuCenterOffsetY);

            var menuLayout = menuContainerObj.AddComponent<VerticalLayoutGroup>();
            menuLayout.spacing = GameButtonSpacing;
            menuLayout.childAlignment = TextAnchor.MiddleCenter;
            menuLayout.childControlHeight = false;
            menuLayout.childControlWidth = true;
            menuLayout.childForceExpandHeight = false;
            menuLayout.childForceExpandWidth = true;

            // ゲーム数が増えても高さを手計算しなくて済むように、中身に合わせて伸縮させる
            var fitter = menuContainerObj.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // 絵文字は Legacy Text だと実機で□になりやすいため使わない
            CreateGameSelectButton(menuContainerObj.transform, "Btn_Game_Soccer", "2D サッカー", SceneNames.Soccer, true, new Color(0.2f, 0.55f, 1f));
            CreateGameSelectButton(menuContainerObj.transform, "Btn_Game_TableTennis", "2D 卓球", SceneNames.TableTennis, true, new Color(1f, 0.5f, 0.25f));
            CreateGameSelectButton(menuContainerObj.transform, "Btn_Game_Molkky", "2D モルック", SceneNames.Molkky, true, new Color(0.35f, 0.75f, 0.3f));
            CreateGameSelectButton(menuContainerObj.transform, "Btn_Game_CardGame", "カードゲーム", SceneNames.CardGame, true, new Color(0.7f, 0.4f, 0.95f));
        }

        private static void BuildFooter(Transform parent, out Button settingsButton, out Button quitButton)
        {
            // 左下の端だと押しにくいため、ゲームボタンの真下の中央に置く
            var footerObj = CreateUIObject("FooterBar", parent);
            var footerRect = footerObj.GetComponent<RectTransform>();
            footerRect.anchorMin = new Vector2(0.5f, 0);
            footerRect.anchorMax = new Vector2(0.5f, 0);
            footerRect.pivot = new Vector2(0.5f, 0);
            footerRect.sizeDelta = new Vector2(FooterButtonWidth * 2 + FooterButtonSpacing, FooterButtonHeight);
            footerRect.anchoredPosition = new Vector2(0, FooterBottomMargin);

            // iOSで終了ボタンを消したとき、残った設定ボタンが自動で中央に寄るようにレイアウトで並べる
            var layout = footerObj.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = FooterButtonSpacing;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            settingsButton = CreateFooterButton(footerObj.transform, "Btn_Settings", "設定", new Color(0.22f, 0.26f, 0.36f));
            quitButton = CreateFooterButton(footerObj.transform, "Btn_Quit", "終了", new Color(0.38f, 0.2f, 0.24f));
        }

        private static Button CreateFooterButton(Transform parent, string name, string label, Color color)
        {
            var btnObj = UIDialogBuilder.CreateButton(parent, name, label, FooterButtonWidth, FooterButtonHeight, color);
            ApplyRoundedSprite(btnObj.GetComponent<Image>());
            btnObj.GetComponentInChildren<Text>().fontSize = FooterButtonFontSize;
            return btnObj.GetComponent<Button>();
        }

        private static Text BuildVersionText(Transform parent)
        {
            var versionText = CreateText(parent, "VersionText", "v0.1.0", VersionFontSize, new Color(0.5f, 0.55f, 0.65f), TextAnchor.LowerRight);
            var verRect = versionText.rectTransform;
            verRect.anchorMin = new Vector2(1, 0);
            verRect.anchorMax = new Vector2(1, 0);
            verRect.pivot = new Vector2(1, 0);
            verRect.sizeDelta = new Vector2(240, 40);
            verRect.anchoredPosition = new Vector2(-40, 30);
            return versionText;
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
            rect.sizeDelta = new Vector2(GameButtonWidth, GameButtonHeight);

            var img = btnObj.AddComponent<Image>();
            ApplyRoundedSprite(img);
            img.color = isPlayable ? CardColor : new Color(CardColor.r, CardColor.g, CardColor.b, 0.6f);

            var btn = btnObj.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f);
            colors.pressedColor = new Color(0.75f, 0.75f, 0.75f);
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.6f);
            btn.colors = colors;

            // ゲームごとの色は左端の帯と矢印だけに使い、全体はカード色で統一して落ち着かせる
            var accentObj = CreateUIObject("AccentStrip", btnObj.transform);
            var accentRect = accentObj.GetComponent<RectTransform>();
            accentRect.anchorMin = new Vector2(0, 0);
            accentRect.anchorMax = new Vector2(0, 1);
            accentRect.pivot = new Vector2(0, 0.5f);
            accentRect.sizeDelta = new Vector2(GameButtonAccentWidth, 0);
            accentRect.anchoredPosition = Vector2.zero;
            var accentImg = accentObj.AddComponent<Image>();
            ApplyRoundedSprite(accentImg);
            accentImg.color = baseColor;
            accentImg.raycastTarget = false;

            var text = CreateText(btnObj.transform, "Text", gameTitle, GameButtonFontSize,
                isPlayable ? Color.white : new Color(0.7f, 0.7f, 0.7f), TextAnchor.MiddleLeft);
            text.fontStyle = FontStyle.Bold;
            SetPaddedStretch(text.rectTransform, GameButtonLabelPadding);

            // 「押せる」ことが一目で分かるように右端に矢印を置く
            var arrow = CreateText(btnObj.transform, "Arrow", "▶", GameButtonArrowFontSize, baseColor, TextAnchor.MiddleRight);
            SetPaddedStretch(arrow.rectTransform, GameButtonLabelPadding);

            // ロックオーバーレイ
            var lockObj = CreateUIObject("LockOverlay", btnObj.transform);
            SetStretchAll(lockObj.GetComponent<RectTransform>());
            var lockImg = lockObj.AddComponent<Image>();
            ApplyRoundedSprite(lockImg);
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

        private static Text CreateText(Transform parent, string name, string content, int fontSize, Color color, TextAnchor alignment)
        {
            var obj = CreateUIObject(name, parent);
            SetStretchAll(obj.GetComponent<RectTransform>());
            var text = obj.AddComponent<Text>();
            text.text = content;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = alignment;
            text.raycastTarget = false;
            // 大きいフォントで枠をはみ出しても文字が切れて消えないようにする
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private static void ApplyRoundedSprite(Image img)
        {
            img.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>(RoundedSpritePath);
            img.type = Image.Type.Sliced;
        }

        private static void SetAnchors(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void SetPaddedStretch(RectTransform rect, float horizontalPadding)
        {
            SetStretchAll(rect);
            rect.offsetMin = new Vector2(horizontalPadding, 0);
            rect.offsetMax = new Vector2(-horizontalPadding, 0);
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
