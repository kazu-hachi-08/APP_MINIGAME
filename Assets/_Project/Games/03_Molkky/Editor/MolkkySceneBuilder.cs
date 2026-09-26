using System.IO;
using MiniGame.Common.Audio;
using MiniGame.Common.Input;
using MiniGame.Common.Scene;
using MiniGame.Common.UI;
using MiniGame.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace MiniGame.Molkky.Editor
{
    /// <summary>
    /// MolkkyScene（Phase 0〜6）を自動生成するエディタユーティリティ。
    /// Scene をコードから作ることで、2人開発での Scene コンフリクトを避ける。
    /// </summary>
    public static class MolkkySceneBuilder
    {
        private const string RootDirectory = "Assets/_Project/Games/03_Molkky";
        private const string SceneDirectory = RootDirectory + "/Scenes";
        private const string ScenePath = SceneDirectory + "/MolkkyScene.unity";
        private const string DataDirectory = RootDirectory + "/Data";
        private const string SettingsPath = DataDirectory + "/MolkkyPhysicsSettings.asset";
        private const string NpcWeakPath = DataDirectory + "/MolkkyNpc_Weak.asset";
        private const string NpcNormalPath = DataDirectory + "/MolkkyNpc_Normal.asset";
        private const string NpcStrongPath = DataDirectory + "/MolkkyNpc_Strong.asset";

        private const string SpritesDefaultMaterialPath = "Sprites-Default.mat";

        // 空の色。地平線が画面内に入る縦長端末で、地面メッシュより上に見える部分
        private static readonly Color BackgroundColor = new Color(0.62f, 0.82f, 0.95f);

        // タイトルと同じく縦画面基準
        private static readonly Vector2 ReferenceResolution = new Vector2(1080, 1920);

        [MenuItem("Tools/MiniGame/Build Molkky Scene", false, 4)]
        public static void BuildMolkkyScene()
        {
            BuildInternal();
        }

        private static void BuildInternal()
        {
            if (!Directory.Exists(SceneDirectory))
            {
                Directory.CreateDirectory(SceneDirectory);
            }

            MolkkyPhysicsSettings settings = EnsureSettings();
            // §9.4：よわい＝大きくブレて常に密集地／ふつう＝中くらい＋ちょうどのピン／つよい＝小さく＋25点戻り回避
            MolkkyNpcDifficulty[] npcDifficulties =
            {
                EnsureNpcDifficulty(NpcWeakPath, 9f, 0.3f, false, false),
                EnsureNpcDifficulty(NpcNormalPath, 4f, 0.15f, true, false),
                EnsureNpcDifficulty(NpcStrongPath, 1.5f, 0.06f, true, true),
            };

            UnityEngine.SceneManagement.Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. Camera
            var cameraObj = new GameObject("Main Camera");
            var camera = cameraObj.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = BackgroundColor;
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            cameraObj.transform.position = new Vector3(0f, 0f, -10f);
            cameraObj.AddComponent<AudioListener>();
            cameraObj.tag = "MainCamera";

            var fitter = cameraObj.AddComponent<MolkkyCameraFitter>();
            SetRefs(fitter, ("_camera", camera));

            // 2. EventSystem（UIのタッチ判定に必須）
            var eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<EventSystem>();
            eventSystemObj.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();

            // 3. Managers（共通基盤の再利用）
            var managersRoot = new GameObject("--- Managers ---");
            CreateManager<SceneLoader>("SceneLoader", managersRoot.transform);
            var audioManager = CreateManager<AudioManager>("AudioManager", managersRoot.transform);
            audioManager.gameObject.AddComponent<ProceduralSe>(); // 正式なSE素材が入るまでの仮音
            var uiManager = CreateManager<UIManager>("UIManager", managersRoot.transform);
            CreateManager<InputManager>("InputManager", managersRoot.transform);

            // 4. 擬似3D変換と地面
            var projector = new GameObject("DepthProjector").AddComponent<DepthProjector>();

            var groundObj = new GameObject("Ground", typeof(MeshFilter), typeof(MeshRenderer));
            groundObj.GetComponent<MeshRenderer>().sharedMaterial =
                AssetDatabase.GetBuiltinExtraResource<Material>(SpritesDefaultMaterialPath);
            var groundView = groundObj.AddComponent<GroundView>();
            SetRefs(groundView, ("_projector", projector), ("_settings", settings));

            // 5. ロジック用の物理（地面平面の2D物理。見た目を持たない）
            var physicsRoot = new GameObject("--- Physics (Ground Plane) ---");

            var pinRackObj = new GameObject("PinRack");
            pinRackObj.transform.SetParent(physicsRoot.transform);
            var pinRack = pinRackObj.AddComponent<PinRack>();
            SetRefs(pinRack, ("_settings", settings));

            var stickObj = new GameObject("Stick", typeof(Rigidbody2D), typeof(CapsuleCollider2D));
            stickObj.transform.SetParent(physicsRoot.transform);
            var stick = stickObj.AddComponent<StickThrower>();
            SetRefs(stick, ("_settings", settings));

            // 6. 見た目（ロジックの地面座標を DepthProjector で擬似3Dに変換して描く）
            var viewRoot = new GameObject("--- View ---");

            var pinRackViewObj = new GameObject("PinRackView");
            pinRackViewObj.transform.SetParent(viewRoot.transform);
            var pinRackView = pinRackViewObj.AddComponent<PinRackView>();
            SetRefs(pinRackView, ("_pinRack", pinRack), ("_projector", projector), ("_settings", settings));

            var stickViewObj = new GameObject("StickView");
            stickViewObj.transform.SetParent(viewRoot.transform);
            var stickView = stickViewObj.AddComponent<StickView>();
            SetRefs(stickView, ("_stick", stick), ("_projector", projector), ("_settings", settings));

            // 7. 入力
            var input = new GameObject("ThrowInput").AddComponent<ThrowInput>();
            SetRefs(input, ("_settings", settings), ("_projector", projector), ("_camera", camera));

            // 8. UI
            var canvasObj = new GameObject("Canvas");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();

            // スコアやボタンはノッチ・ホームバーを避けるため SafeArea 内に置く（全画面の表示物は Canvas 直下）
            var safeAreaObj = UIDialogBuilder.CreateUIObject("SafeArea", canvasObj.transform);
            UIDialogBuilder.SetStretchAll(safeAreaObj.GetComponent<RectTransform>());
            safeAreaObj.AddComponent<SafeAreaFitter>();
            Transform safeArea = safeAreaObj.transform;

            // スコアは PAUSE ボタンの下に置き、4人分の横並びでも重ならないようにする
            Text scoreText = CreateText(safeArea, "ScoreText", 44,
                new Vector2(0.5f, 1f), new Vector2(0f, -160f), new Vector2(1040f, 80f), Color.white);
            Text remainingText = CreateText(safeArea, "RemainingText", 40,
                new Vector2(0.5f, 1f), new Vector2(0f, -240f), new Vector2(1000f, 70f), Color.white);
            Text messageText = CreateText(safeArea, "MessageText", 96,
                new Vector2(0.5f, 0.5f), new Vector2(0f, 200f), new Vector2(1000f, 220f), new Color(1f, 0.85f, 0.1f));
            messageText.gameObject.AddComponent<Outline>().effectDistance = new Vector2(3f, -3f);
            messageText.gameObject.SetActive(false);

            var hud = canvasObj.AddComponent<MolkkyHud>();
            SetRefs(hud, ("_scoreText", scoreText), ("_remainingText", remainingText), ("_messageText", messageText));

            TurnBannerView turnBanner = CreateTurnBanner(canvasObj.transform);
            PlayerSetupPanel setupPanel = CreatePlayerSetupPanel(canvasObj.transform);

            var pauseButtonObj = UIDialogBuilder.CreateButton(safeArea, "Btn_Pause", "II", 110, 110,
                new Color(0.15f, 0.17f, 0.22f, 0.8f));
            var pauseRect = pauseButtonObj.GetComponent<RectTransform>();
            pauseRect.anchorMin = pauseRect.anchorMax = pauseRect.pivot = new Vector2(1f, 1f);
            pauseRect.anchoredPosition = new Vector2(-30f, -30f);
            var pauseButton = pauseButtonObj.AddComponent<PauseButton>();

            // 共通ダイアログ（PAUSE / リザルト）は最前面に置くため最後に生成する
            UIDialogBuilder.BuildDialogs(canvasObj.transform, uiManager);

            // 9. GameManager
            var gameManagerObj = new GameObject("MolkkyGameManager");
            var gameManager = gameManagerObj.AddComponent<MolkkyGameManager>();
            var settleWatcher = gameManagerObj.AddComponent<ThrowSettleWatcher>();
            SetRefs(settleWatcher, ("_settings", settings), ("_pinRack", pinRack), ("_stick", stick));

            var npcThrower = gameManagerObj.AddComponent<NpcThrower>();
            SetRefs(npcThrower, ("_settings", settings), ("_pinRack", pinRack));
            SetArray(npcThrower, "_difficulties", npcDifficulties);

            var gmSo = new SerializedObject(gameManager);
            gmSo.FindProperty("_gameTitle").stringValue = "2D Molkky";
            gmSo.ApplyModifiedPropertiesWithoutUndo();
            SetRefs(gameManager, ("_pinRack", pinRack), ("_stick", stick), ("_input", input), ("_npc", npcThrower),
                ("_settleWatcher", settleWatcher), ("_hud", hud), ("_turnBanner", turnBanner), ("_setupPanel", setupPanel));

            SetRefs(pauseButton, ("_gameManager", gameManager));

            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[MolkkySceneBuilder] MolkkyScene を生成しました: {ScenePath}");

            RegisterSceneInBuildSettings(ScenePath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        /// <summary>調整用パラメータの ScriptableObject が無ければ初期値で作る（既にあれば調整済みの値を残す）</summary>
        private static MolkkyPhysicsSettings EnsureSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<MolkkyPhysicsSettings>(SettingsPath);
            if (settings != null) return settings;

            if (!Directory.Exists(DataDirectory))
            {
                Directory.CreateDirectory(DataDirectory);
            }

            settings = ScriptableObject.CreateInstance<MolkkyPhysicsSettings>();
            AssetDatabase.CreateAsset(settings, SettingsPath);
            AssetDatabase.SaveAssets();
            return settings;
        }

        /// <summary>NPC難易度が無ければ作る。既にあれば調整済みの値を残す</summary>
        private static MolkkyNpcDifficulty EnsureNpcDifficulty(string path, float angleNoise, float speedNoise,
            bool aimExactPin, bool avoidOverflow)
        {
            var difficulty = AssetDatabase.LoadAssetAtPath<MolkkyNpcDifficulty>(path);
            if (difficulty != null) return difficulty;

            if (!Directory.Exists(DataDirectory))
            {
                Directory.CreateDirectory(DataDirectory);
            }

            difficulty = ScriptableObject.CreateInstance<MolkkyNpcDifficulty>();
            var so = new SerializedObject(difficulty);
            so.FindProperty("_angleNoise").floatValue = angleNoise;
            so.FindProperty("_speedNoise").floatValue = speedNoise;
            so.FindProperty("_aimExactPin").boolValue = aimExactPin;
            so.FindProperty("_avoidOverflow").boolValue = avoidOverflow;
            so.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.CreateAsset(difficulty, path);
            AssetDatabase.SaveAssets();
            return difficulty;
        }

        /// <summary>「○○の番」の全画面表示。画面全体をボタンにして、どこをタップしても開始できるようにする</summary>
        private static TurnBannerView CreateTurnBanner(Transform canvas)
        {
            var bannerObj = UIDialogBuilder.CreateUIObject("TurnBanner", canvas);
            UIDialogBuilder.SetStretchAll(bannerObj.GetComponent<RectTransform>());
            bannerObj.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);
            var tapArea = bannerObj.AddComponent<Button>();
            tapArea.transition = Selectable.Transition.None;

            Text title = CreateText(bannerObj.transform, "TitleText", 120,
                new Vector2(0.5f, 0.5f), new Vector2(0f, 80f), new Vector2(1000f, 200f), Color.white);
            title.gameObject.AddComponent<Outline>().effectDistance = new Vector2(4f, -4f);
            Text hint = CreateText(bannerObj.transform, "HintText", 52,
                new Vector2(0.5f, 0.5f), new Vector2(0f, -80f), new Vector2(1000f, 90f), Color.white);
            hint.text = "タップで開始";

            var banner = bannerObj.AddComponent<TurnBannerView>();
            SetRefs(banner, ("_titleText", title), ("_hintText", hint), ("_tapArea", tapArea));

            bannerObj.SetActive(false);
            return banner;
        }

        /// <summary>人数と各プレイヤーの人間/NPCを選ぶパネル（§10.1）。非表示の行は VerticalLayoutGroup で詰める</summary>
        private static PlayerSetupPanel CreatePlayerSetupPanel(Transform canvas)
        {
            const float rowWidth = 820f;
            const float rowHeight = 110f;
            const int buttonFontSize = 44;

            var panelObj = UIDialogBuilder.CreateUIObject("PlayerSetupPanel", canvas);
            UIDialogBuilder.SetStretchAll(panelObj.GetComponent<RectTransform>());
            panelObj.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.8f);

            var boxObj = UIDialogBuilder.CreateUIObject("Panel", panelObj.transform);
            var boxRect = boxObj.GetComponent<RectTransform>();
            boxRect.anchorMin = boxRect.anchorMax = boxRect.pivot = new Vector2(0.5f, 0.5f);
            boxRect.sizeDelta = new Vector2(920f, 1200f);
            boxObj.AddComponent<Image>().color = new Color(0.12f, 0.14f, 0.18f);
            var layout = boxObj.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(40, 40, 40, 40);
            layout.spacing = 28f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            Text title = CreateText(boxObj.transform, "TitleText", 60,
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(rowWidth, 100f), Color.white);
            title.text = "プレイヤー設定";

            Transform countRow = CreateRow(boxObj.transform, "CountRow", rowWidth, rowHeight);
            var countButtons = new Button[PlayerSetupPanel.MaxPlayers - PlayerSetupPanel.MinPlayers + 1];
            for (int i = 0; i < countButtons.Length; i++)
            {
                int count = PlayerSetupPanel.MinPlayers + i;
                countButtons[i] = CreateSetupButton(countRow, $"Btn_{count}Players", $"{count}人", 240f, rowHeight, buttonFontSize);
            }

            var playerRows = new GameObject[PlayerSetupPanel.MaxPlayers];
            var kindButtons = new Button[PlayerSetupPanel.MaxPlayers];
            var kindTexts = new Text[PlayerSetupPanel.MaxPlayers];
            for (int i = 0; i < PlayerSetupPanel.MaxPlayers; i++)
            {
                Transform row = CreateRow(boxObj.transform, $"Row_P{i + 1}", rowWidth, rowHeight);
                playerRows[i] = row.gameObject;

                Text label = CreateText(row, "Label", 56, new Vector2(0.5f, 0.5f), Vector2.zero,
                    new Vector2(160f, rowHeight), MolkkyPlayerColors.Get(i));
                label.text = $"P{i + 1}";

                kindButtons[i] = CreateSetupButton(row, "Btn_Kind", "", 600f, rowHeight, buttonFontSize);
                kindTexts[i] = kindButtons[i].GetComponentInChildren<Text>();
            }

            var startButtonObj = UIDialogBuilder.CreateButton(boxObj.transform, "Btn_Start", "試合開始", 560f, 140f,
                new Color(0.2f, 0.7f, 0.35f));
            startButtonObj.GetComponentInChildren<Text>().fontSize = 56;

            var panel = panelObj.AddComponent<PlayerSetupPanel>();
            SetArray(panel, "_countButtons", countButtons);
            SetArray(panel, "_playerRows", playerRows);
            SetArray(panel, "_kindButtons", kindButtons);
            SetArray(panel, "_kindTexts", kindTexts);
            SetRefs(panel, ("_startButton", startButtonObj.GetComponent<Button>()));

            panelObj.SetActive(false);
            return panel;
        }

        private static Transform CreateRow(Transform parent, string name, float width, float height)
        {
            var rowObj = UIDialogBuilder.CreateUIObject(name, parent);
            rowObj.GetComponent<RectTransform>().sizeDelta = new Vector2(width, height);
            var layout = rowObj.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 20f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            return rowObj.transform;
        }

        private static Button CreateSetupButton(Transform parent, string name, string label, float width, float height, int fontSize)
        {
            var buttonObj = UIDialogBuilder.CreateButton(parent, name, label, width, height, new Color(0.3f, 0.33f, 0.4f));
            buttonObj.GetComponentInChildren<Text>().fontSize = fontSize;
            return buttonObj.GetComponent<Button>();
        }

        private static void SetArray(Object target, string name, Object[] values)
        {
            var so = new SerializedObject(target);
            SerializedProperty property = so.FindProperty(name);
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetRefs(Object target, params (string name, Object value)[] refs)
        {
            var so = new SerializedObject(target);
            foreach ((string name, Object value) in refs)
            {
                so.FindProperty(name).objectReferenceValue = value;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static T CreateManager<T>(string name, Transform parent) where T : Component
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent);
            return obj.AddComponent<T>();
        }

        private static Text CreateText(Transform parent, string name, int fontSize,
            Vector2 anchor, Vector2 anchoredPosition, Vector2 size, Color color)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            var rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPosition;

            var text = obj.AddComponent<Text>();
            text.fontSize = fontSize;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = color;
            text.supportRichText = true;
            text.raycastTarget = false; // 画面全体が投擲の入力領域なので、文字で入力を遮らない
            return text;
        }

        private static void RegisterSceneInBuildSettings(string scenePath)
        {
            var scenes = EditorBuildSettings.scenes;
            foreach (var s in scenes)
            {
                if (s.path == scenePath) return;
            }

            var newScenes = new EditorBuildSettingsScene[scenes.Length + 1];
            scenes.CopyTo(newScenes, 0);
            newScenes[scenes.Length] = new EditorBuildSettingsScene(scenePath, true);

            EditorBuildSettings.scenes = newScenes;
            Debug.Log($"[MolkkySceneBuilder] Build Settings に {scenePath} を登録しました。");
        }
    }
}
