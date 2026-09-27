using System.IO;
using MiniGame.Common.Audio;
using MiniGame.Common.Scene;
using MiniGame.Common.UI;
using MiniGame.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace MiniGame.Golf.Editor
{
    /// <summary>
    /// GolfScene を自動生成するエディタユーティリティ（Phase 1：平らな地面でボールを打って転がす）。
    /// Scene をコードから作ることで、2人開発での Scene コンフリクトを避ける。
    /// </summary>
    public static class GolfSceneBuilder
    {
        private const string RootDirectory = "Assets/_Project/Games/04_Golf";
        private const string SceneDirectory = RootDirectory + "/Scenes";
        private const string ScenePath = SceneDirectory + "/GolfScene.unity";
        private const string DataDirectory = RootDirectory + "/Data";
        private const string SettingsPath = DataDirectory + "/GolfPhysicsSettings.asset";

        // タイトルと同じく縦画面基準
        private static readonly Vector2 ReferenceResolution = new Vector2(1080, 1920);

        // §4.3：ボールの周りの狙う先まで見える広さ（縦10ユニット）
        private const float CameraOrthographicSize = 5f;

        // §5 の OB（コースの外側）の灰色。仮の地面の外に出たときの背景
        private static readonly Color OutOfBoundsColor = new Color(0.55f, 0.57f, 0.55f);

        // 地面 → 影 → ボールの順に重ねる
        private const int GroundSortingOrder = 0;
        private const int ShadowSortingOrder = 10;
        private const int BallSortingOrder = 20;

        private const int HudFontSize = 52;
        private const float HudTopMargin = 120f;
        private const float HudHeight = 240f;
        private const float ReturnButtonWidth = 480f;
        private const float ReturnButtonHeight = 130f;
        private const int ReturnButtonFontSize = 44;
        private const float ReturnButtonBottomMargin = 200f;

        [MenuItem("Tools/MiniGame/Build Golf Scene", false, 5)]
        public static void BuildGolfScene()
        {
            if (!Directory.Exists(SceneDirectory))
            {
                Directory.CreateDirectory(SceneDirectory);
            }

            UnityEngine.SceneManagement.Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // NewScene(Single) は未使用アセットをアンロードするため、ScriptableObject は必ずシーンを作った後に読み込む
            GolfPhysicsSettings settings = EnsureSettings();

            Camera camera = CreateCamera();
            CreateEventSystem();
            CreateManagers();
            CreateGround();
            GolfBall ball = CreateBall(settings);
            SetRefs(camera.gameObject.AddComponent<GolfCameraFollower>(), ("_ball", ball));
            PrototypeShotInput input = CreateInput(ball, camera);
            CreateCanvas(ball, input, settings);

            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[GolfSceneBuilder] GolfScene を生成しました: {ScenePath}");

            RegisterSceneInBuildSettings(ScenePath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        /// <summary>調整用パラメータの ScriptableObject が無ければ初期値で作る（既にあれば調整済みの値を残す）</summary>
        private static GolfPhysicsSettings EnsureSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<GolfPhysicsSettings>(SettingsPath);
            if (settings != null) return settings;

            if (!Directory.Exists(DataDirectory))
            {
                Directory.CreateDirectory(DataDirectory);
            }

            settings = ScriptableObject.CreateInstance<GolfPhysicsSettings>();
            AssetDatabase.CreateAsset(settings, SettingsPath);
            AssetDatabase.SaveAssets();
            return settings;
        }

        private static Camera CreateCamera()
        {
            var cameraObj = new GameObject("Main Camera");
            var camera = cameraObj.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = OutOfBoundsColor;
            camera.orthographic = true;
            camera.orthographicSize = CameraOrthographicSize;
            cameraObj.transform.position = new Vector3(0f, 0f, -10f);
            cameraObj.AddComponent<AudioListener>();
            cameraObj.tag = "MainCamera";
            return camera;
        }

        private static void CreateGround()
        {
            var groundObj = new GameObject("FlatGround");
            groundObj.AddComponent<SpriteRenderer>().sortingOrder = GroundSortingOrder;
            groundObj.AddComponent<FlatGroundView>();
        }

        /// <summary>ロジック（GolfBall）と見た目（BallView / ShadowView）を別の GameObject に分ける</summary>
        private static GolfBall CreateBall(GolfPhysicsSettings settings)
        {
            var ball = new GameObject("GolfBall").AddComponent<GolfBall>();
            SetRefs(ball, ("_settings", settings));

            var viewRoot = new GameObject("--- View ---");

            var shadowObj = new GameObject("BallShadow");
            shadowObj.transform.SetParent(viewRoot.transform);
            shadowObj.AddComponent<SpriteRenderer>().sortingOrder = ShadowSortingOrder;
            SetRefs(shadowObj.AddComponent<ShadowView>(), ("_ball", ball));

            var ballViewObj = new GameObject("BallView");
            ballViewObj.transform.SetParent(viewRoot.transform);
            ballViewObj.AddComponent<SpriteRenderer>().sortingOrder = BallSortingOrder;
            SetRefs(ballViewObj.AddComponent<BallView>(), ("_ball", ball));

            return ball;
        }

        private static PrototypeShotInput CreateInput(GolfBall ball, Camera camera)
        {
            var input = new GameObject("PrototypeShotInput").AddComponent<PrototypeShotInput>();
            SetRefs(input, ("_ball", ball), ("_camera", camera));
            return input;
        }

        private static void CreateEventSystem()
        {
            // UIのタッチ／クリック判定に必須
            var eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<EventSystem>();
            eventSystemObj.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }

        private static void CreateManagers()
        {
            // タイトルへ戻るときのフェード遷移に SceneLoader を使う
            var managersRoot = new GameObject("--- Managers ---");
            CreateManager<SceneLoader>("SceneLoader", managersRoot.transform);
            CreateManager<AudioManager>("AudioManager", managersRoot.transform);
        }

        private static void CreateCanvas(GolfBall ball, PrototypeShotInput input, GolfPhysicsSettings settings)
        {
            var canvasObj = new GameObject("Canvas");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0f;
            canvasObj.AddComponent<GraphicRaycaster>();

            // ボタンはノッチ・ホームバーを避けるため SafeArea 内に置く
            var safeAreaObj = UIDialogBuilder.CreateUIObject("SafeArea", canvasObj.transform);
            UIDialogBuilder.SetStretchAll(safeAreaObj.GetComponent<RectTransform>());
            safeAreaObj.AddComponent<SafeAreaFitter>();

            CreateShotHud(safeAreaObj.transform, ball, input, settings);
            CreateReturnButton(safeAreaObj.transform);
        }

        private static void CreateShotHud(Transform parent, GolfBall ball, PrototypeShotInput input,
            GolfPhysicsSettings settings)
        {
            var obj = UIDialogBuilder.CreateUIObject("PrototypeShotHud", parent);
            var rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -HudTopMargin);
            rect.sizeDelta = new Vector2(0f, HudHeight);

            var text = obj.AddComponent<Text>();
            text.fontSize = HudFontSize;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.UpperCenter;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            // 画面のどこを押しても打てるように、表示はタップを奪わない
            text.raycastTarget = false;
            // 明るい芝の上でも読めるように縁取りする
            obj.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.6f);

            var hud = obj.AddComponent<PrototypeShotHud>();
            SetRefs(hud, ("_ball", ball), ("_input", input), ("_settings", settings), ("_text", text));
        }

        private static void CreateReturnButton(Transform parent)
        {
            var buttonObj = UIDialogBuilder.CreateButton(parent, "Btn_ReturnToTitle", "タイトルへ戻る",
                ReturnButtonWidth, ReturnButtonHeight, new Color(0.15f, 0.17f, 0.22f, 0.9f));
            var rect = buttonObj.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, ReturnButtonBottomMargin);
            buttonObj.GetComponentInChildren<Text>().fontSize = ReturnButtonFontSize;

            var returnButton = buttonObj.AddComponent<GolfTitleReturnButton>();
            SetRefs(returnButton, ("_button", buttonObj.GetComponent<Button>()));
        }

        private static void SetRefs(Object target, params (string property, Object value)[] refs)
        {
            var so = new SerializedObject(target);
            foreach (var (property, value) in refs)
            {
                so.FindProperty(property).objectReferenceValue = value;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static T CreateManager<T>(string name, Transform parent) where T : Component
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent);
            return obj.AddComponent<T>();
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
            Debug.Log($"[GolfSceneBuilder] Build Settings に {scenePath} を登録しました。");
        }
    }
}
