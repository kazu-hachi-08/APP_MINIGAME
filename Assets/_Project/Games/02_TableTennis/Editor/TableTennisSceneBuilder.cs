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

namespace MiniGame.TableTennis.Editor
{
    /// <summary>
    /// TableTennisScene（Phase 1〜3: 最小プロトタイプ〜フリック打球）を自動生成するエディタユーティリティ。
    /// Scene をコードから作ることで、2人開発での Scene コンフリクトを避ける。
    /// </summary>
    public static class TableTennisSceneBuilder
    {
        private const string SceneDirectory = "Assets/_Project/Games/02_TableTennis/Scenes";
        private const string ScenePath = SceneDirectory + "/TableTennisScene.unity";

        // 仮素材（円・矩形・線）は Unity 組み込みスプライトで済ませ、Phase 9 で差し替える
        private const string CircleSpritePath = "UI/Skin/Knob.psd";
        private const string SpritesDefaultMaterialPath = "Sprites-Default.mat";

        private const int ShadowSortingOrder = 10;
        private const int BallSortingOrder = 20;
        private const int RacketSortingOrder = 30;

        [MenuItem("Tools/MiniGame/Build Table Tennis Scene", false, 3)]
        public static void BuildTableTennisScene()
        {
            BuildInternal();
        }

        [InitializeOnLoadMethod]
        private static void OnProjectLoaded()
        {
            if (!File.Exists(ScenePath))
            {
                Debug.Log($"[TableTennisSceneBuilder] {ScenePath} が存在しないため、自動生成を実行します。");
                BuildInternal();
            }
        }

        private static void BuildInternal()
        {
            Debug.Log("[TableTennisSceneBuilder] TableTennisScene の構築を開始します...");

            if (!Directory.Exists(SceneDirectory))
            {
                Directory.CreateDirectory(SceneDirectory);
            }

            UnityEngine.SceneManagement.Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            Sprite circleSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>(CircleSpritePath);
            Material spritesDefault = AssetDatabase.GetBuiltinExtraResource<Material>(SpritesDefaultMaterialPath);

            // 1. Camera（台の手前から奥を見る擬似3Dの画角に合わせる）
            var cameraObj = new GameObject("Main Camera");
            var camera = cameraObj.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.07f, 0.08f, 0.11f);
            camera.orthographic = true;
            camera.orthographicSize = 4.5f;
            cameraObj.transform.position = new Vector3(0f, -0.5f, -10f);
            cameraObj.AddComponent<AudioListener>();
            cameraObj.tag = "MainCamera";

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

            // 4. 卓球台（寸法と擬似3D変換の基準）
            var tableObj = new GameObject("Table");
            var tableLayout = tableObj.AddComponent<TableLayout>();
            var tableView = tableObj.AddComponent<TableView>();
            var tvSo = new SerializedObject(tableView);
            tvSo.FindProperty("_table").objectReferenceValue = tableLayout;
            tvSo.FindProperty("_material").objectReferenceValue = spritesDefault;
            tvSo.ApplyModifiedProperties();

            // 5. ボール（影は奥行きを読み取る手がかりになるので別オブジェクトで用意する）
            var shadowObj = CreateSpriteObject("BallShadow", circleSprite,
                new Color(0f, 0f, 0f, 0.35f), ShadowSortingOrder);

            var ballObj = CreateSpriteObject("Ball", circleSprite,
                new Color(1f, 0.95f, 0.75f), BallSortingOrder);
            var ballMotion = ballObj.AddComponent<BallMotion>();
            var ballView = ballObj.AddComponent<BallView>();

            var bmSo = new SerializedObject(ballMotion);
            bmSo.FindProperty("_table").objectReferenceValue = tableLayout;
            bmSo.ApplyModifiedProperties();

            var bvSo = new SerializedObject(ballView);
            bvSo.FindProperty("_table").objectReferenceValue = tableLayout;
            bvSo.FindProperty("_ball").objectReferenceValue = ballMotion;
            bvSo.FindProperty("_renderer").objectReferenceValue = ballObj.GetComponent<SpriteRenderer>();
            bvSo.FindProperty("_shadow").objectReferenceValue = shadowObj.transform;
            bvSo.ApplyModifiedProperties();

            // 6. 入力（タップでラケット移動 / フリックで打球）
            var playerRigObj = new GameObject("PlayerRig");
            var flickInput = playerRigObj.AddComponent<FlickInput>();
            var shotCalculator = playerRigObj.AddComponent<ShotCalculator>();
            var playerSwing = playerRigObj.AddComponent<PlayerSwing>();

            // 7. ラケット
            var racketObj = CreateSpriteObject("Racket", circleSprite,
                new Color(0.85f, 0.25f, 0.25f), RacketSortingOrder);
            var racket = racketObj.AddComponent<RacketController>();

            var rcSo = new SerializedObject(racket);
            rcSo.FindProperty("_table").objectReferenceValue = tableLayout;
            rcSo.FindProperty("_flickInput").objectReferenceValue = flickInput;
            rcSo.FindProperty("_camera").objectReferenceValue = camera;
            rcSo.FindProperty("_renderer").objectReferenceValue = racketObj.GetComponent<SpriteRenderer>();
            rcSo.ApplyModifiedProperties();

            var psSo = new SerializedObject(playerSwing);
            psSo.FindProperty("_ball").objectReferenceValue = ballMotion;
            psSo.FindProperty("_racket").objectReferenceValue = racket;
            psSo.FindProperty("_flickInput").objectReferenceValue = flickInput;
            psSo.FindProperty("_shotCalculator").objectReferenceValue = shotCalculator;
            psSo.ApplyModifiedProperties();

            // 8. UI
            var canvasObj = new GameObject("Canvas");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();

            Text messageText = CreateText(canvasObj.transform, "MessageText", "", 96,
                new Vector2(0.5f, 0.5f), new Vector2(0f, 120f), new Vector2(900f, 220f),
                new Color(1f, 0.85f, 0.1f));
            messageText.gameObject.SetActive(false);

            // Phase 3 の確認用。フリックが打球・回転へどう変換されたかを常時表示する
            Text shotInfoText = CreateText(canvasObj.transform, "ShotInfoText", "", 30,
                new Vector2(0f, 1f), new Vector2(40f, -40f), new Vector2(900f, 160f),
                new Color(0.85f, 0.92f, 1f));
            shotInfoText.alignment = TextAnchor.UpperLeft;

            var pauseButtonObj = UIDialogBuilder.CreateButton(canvasObj.transform, "Btn_Pause", "II", 90, 90,
                new Color(0.15f, 0.17f, 0.22f, 0.8f));
            SetAnchoredRect(pauseButtonObj.GetComponent<RectTransform>(), new Vector2(1f, 1f),
                new Vector2(-80f, -70f), new Vector2(90f, 90f));
            var pauseButton = pauseButtonObj.AddComponent<PauseButton>();

            // 共通ダイアログ（PAUSE / リザルト）は最前面に置くため最後に生成する
            UIDialogBuilder.BuildDialogs(canvasObj.transform, uiManager);

            // 9. GameManager
            var gameManagerObj = new GameObject("TableTennisGameManager");
            var gameManager = gameManagerObj.AddComponent<TableTennisGameManager>();

            var gmSo = new SerializedObject(gameManager);
            gmSo.FindProperty("_gameTitle").stringValue = "2D Table Tennis";
            gmSo.FindProperty("_ball").objectReferenceValue = ballMotion;
            gmSo.FindProperty("_playerSwing").objectReferenceValue = playerSwing;
            gmSo.FindProperty("_messageText").objectReferenceValue = messageText;
            gmSo.FindProperty("_shotInfoText").objectReferenceValue = shotInfoText;
            gmSo.ApplyModifiedProperties();

            var pauseSo = new SerializedObject(pauseButton);
            pauseSo.FindProperty("_gameManager").objectReferenceValue = gameManager;
            pauseSo.ApplyModifiedProperties();

            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[TableTennisSceneBuilder] TableTennisScene を生成しました: {ScenePath}");

            RegisterSceneInBuildSettings(ScenePath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static T CreateManager<T>(string name, Transform parent) where T : Component
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent);
            return obj.AddComponent<T>();
        }

        private static GameObject CreateSpriteObject(string name, Sprite sprite, Color color, int sortingOrder)
        {
            var obj = new GameObject(name);
            var renderer = obj.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            return obj;
        }

        private static Text CreateText(Transform parent, string name, string content, int fontSize,
            Vector2 anchor, Vector2 anchoredPosition, Vector2 size, Color color)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            var rect = obj.AddComponent<RectTransform>();
            SetAnchoredRect(rect, anchor, anchoredPosition, size);

            var text = obj.AddComponent<Text>();
            text.text = content;
            text.fontSize = fontSize;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = color;
            text.raycastTarget = false; // 画面全体がフリック入力領域なので、文字で入力を遮らない
            return text;
        }

        private static void SetAnchoredRect(RectTransform rect, Vector2 anchor, Vector2 anchoredPosition, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPosition;
        }

        private static void RegisterSceneInBuildSettings(string scenePath)
        {
            var scenes = EditorBuildSettings.scenes;
            foreach (var s in scenes)
            {
                if (s.path == scenePath)
                {
                    Debug.Log($"[TableTennisSceneBuilder] Build Settings に既に登録されています: {scenePath}");
                    return;
                }
            }

            var newScenes = new EditorBuildSettingsScene[scenes.Length + 1];
            for (int i = 0; i < scenes.Length; i++)
            {
                newScenes[i] = scenes[i];
            }
            newScenes[scenes.Length] = new EditorBuildSettingsScene(scenePath, true);

            EditorBuildSettings.scenes = newScenes;
            Debug.Log($"[TableTennisSceneBuilder] Build Settings に {scenePath} を登録しました。");
        }
    }
}
