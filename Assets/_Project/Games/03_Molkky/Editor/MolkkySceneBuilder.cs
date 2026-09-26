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
    /// MolkkyScene（Phase 0〜3）を自動生成するエディタユーティリティ。
    /// Scene をコードから作ることで、2人開発での Scene コンフリクトを避ける。
    /// </summary>
    public static class MolkkySceneBuilder
    {
        private const string RootDirectory = "Assets/_Project/Games/03_Molkky";
        private const string SceneDirectory = RootDirectory + "/Scenes";
        private const string ScenePath = SceneDirectory + "/MolkkyScene.unity";
        private const string DataDirectory = RootDirectory + "/Data";
        private const string SettingsPath = DataDirectory + "/MolkkyPhysicsSettings.asset";

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
            SetRefs(input, ("_settings", settings));

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

            // 画面最上部はインカメラのノッチと重なるため、その下まで下げて表示する
            Text scoreText = CreateText(canvasObj.transform, "ScoreText", 48,
                new Vector2(0.5f, 1f), new Vector2(0f, -170f), new Vector2(1000f, 80f), Color.white);
            Text remainingText = CreateText(canvasObj.transform, "RemainingText", 40,
                new Vector2(0.5f, 1f), new Vector2(0f, -250f), new Vector2(1000f, 70f), Color.white);
            Text messageText = CreateText(canvasObj.transform, "MessageText", 96,
                new Vector2(0.5f, 0.5f), new Vector2(0f, 200f), new Vector2(1000f, 220f), new Color(1f, 0.85f, 0.1f));
            messageText.gameObject.AddComponent<Outline>().effectDistance = new Vector2(3f, -3f);
            messageText.gameObject.SetActive(false);

            var hud = canvasObj.AddComponent<MolkkyHud>();
            SetRefs(hud, ("_scoreText", scoreText), ("_remainingText", remainingText), ("_messageText", messageText));

            var pauseButtonObj = UIDialogBuilder.CreateButton(canvasObj.transform, "Btn_Pause", "II", 110, 110,
                new Color(0.15f, 0.17f, 0.22f, 0.8f));
            var pauseRect = pauseButtonObj.GetComponent<RectTransform>();
            pauseRect.anchorMin = pauseRect.anchorMax = pauseRect.pivot = new Vector2(1f, 1f);
            pauseRect.anchoredPosition = new Vector2(-40f, -40f);
            var pauseButton = pauseButtonObj.AddComponent<PauseButton>();

            // 共通ダイアログ（PAUSE / リザルト）は最前面に置くため最後に生成する
            UIDialogBuilder.BuildDialogs(canvasObj.transform, uiManager);

            // 9. GameManager
            var gameManagerObj = new GameObject("MolkkyGameManager");
            var gameManager = gameManagerObj.AddComponent<MolkkyGameManager>();
            var settleWatcher = gameManagerObj.AddComponent<ThrowSettleWatcher>();
            SetRefs(settleWatcher, ("_settings", settings), ("_pinRack", pinRack), ("_stick", stick));

            var gmSo = new SerializedObject(gameManager);
            gmSo.FindProperty("_gameTitle").stringValue = "2D Molkky";
            gmSo.ApplyModifiedPropertiesWithoutUndo();
            SetRefs(gameManager, ("_pinRack", pinRack), ("_stick", stick), ("_input", input),
                ("_settleWatcher", settleWatcher), ("_hud", hud));

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
