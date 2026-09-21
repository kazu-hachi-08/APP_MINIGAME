using System.IO;
using MiniGame.Common.Audio;
using MiniGame.Common.Input;
using MiniGame.Common.Scene;
using MiniGame.Common.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.Soccer.Editor
{
    /// <summary>
    /// SoccerScene（Phase 1: 最小プロトタイプ）を自動生成・セットアップするエディタユーティリティ
    /// </summary>
    public static class SoccerSceneBuilder
    {
        private const string SceneDirectory = "Assets/_Project/Games/01_Soccer/Scenes";
        private const string ScenePath = SceneDirectory + "/SoccerScene.unity";

        // コート寸法（ワールド単位）
        private const float FieldHalfWidth = 7f;
        private const float FieldHalfHeight = 4f;
        private const float GoalHalfHeight = 1.5f;
        private const float WallThickness = 0.3f;

        private static readonly Vector2 PlayerStartPosition = new Vector2(-4f, 0f);
        private static readonly Vector2 BallStartPosition = new Vector2(-1.5f, 0f);

        [MenuItem("Tools/MiniGame/Build Soccer Scene", false, 2)]
        public static void BuildSoccerScene()
        {
            BuildSoccerSceneInternal();
        }

        [InitializeOnLoadMethod]
        private static void OnProjectLoaded()
        {
            if (!File.Exists(ScenePath))
            {
                Debug.Log($"[SoccerSceneBuilder] {ScenePath} が存在しないため、自動生成を実行します。");
                BuildSoccerSceneInternal();
            }
        }

        private static void BuildSoccerSceneInternal()
        {
            Debug.Log("[SoccerSceneBuilder] SoccerScene の構築を開始します...");

            if (!Directory.Exists(SceneDirectory))
            {
                Directory.CreateDirectory(SceneDirectory);
            }

            UnityEngine.SceneManagement.Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. Camera
            var cameraObj = new GameObject("Main Camera");
            var camera = cameraObj.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.05f, 0.07f, 0.06f);
            camera.orthographic = true;
            camera.orthographicSize = FieldHalfHeight + 1.5f;
            cameraObj.transform.position = new Vector3(0f, 0f, -10f);
            cameraObj.AddComponent<AudioListener>();
            cameraObj.tag = "MainCamera";

            // 2. Managers（共通基盤の再利用）
            var managersRoot = new GameObject("--- Managers ---");
            CreateManager<SceneLoader>("SceneLoader", managersRoot.transform);
            CreateManager<AudioManager>("AudioManager", managersRoot.transform);
            CreateManager<UIManager>("UIManager", managersRoot.transform);
            CreateManager<InputManager>("InputManager", managersRoot.transform);

            // 3. コート
            var fieldObj = CreateSprite("Field", new Color(0.13f, 0.5f, 0.2f), Vector3.zero,
                new Vector3(FieldHalfWidth * 2f, FieldHalfHeight * 2f, 1f), "UI/Skin/UISprite.psd");
            fieldObj.GetComponent<SpriteRenderer>().sortingOrder = -10;

            // ゴール視覚マーカー
            var goalVisual = CreateSprite("GoalVisual", new Color(1f, 0.9f, 0.2f, 0.6f),
                new Vector3(FieldHalfWidth, 0f, 0f), new Vector3(0.4f, GoalHalfHeight * 2f, 1f), "UI/Skin/UISprite.psd");
            goalVisual.GetComponent<SpriteRenderer>().sortingOrder = -5;

            // 4. 壁（コートの境界。ゴール口のみ開ける）
            CreateWall("Wall_Top", new Vector2(0f, FieldHalfHeight + WallThickness / 2f),
                new Vector2(FieldHalfWidth * 2f + WallThickness * 2f, WallThickness));
            CreateWall("Wall_Bottom", new Vector2(0f, -FieldHalfHeight - WallThickness / 2f),
                new Vector2(FieldHalfWidth * 2f + WallThickness * 2f, WallThickness));
            CreateWall("Wall_Left", new Vector2(-FieldHalfWidth - WallThickness / 2f, 0f),
                new Vector2(WallThickness, FieldHalfHeight * 2f + WallThickness * 2f));

            float wallSideHeight = FieldHalfHeight - GoalHalfHeight;
            float wallSideCenterY = GoalHalfHeight + wallSideHeight / 2f;
            CreateWall("Wall_Right_Upper", new Vector2(FieldHalfWidth + WallThickness / 2f, wallSideCenterY),
                new Vector2(WallThickness, wallSideHeight));
            CreateWall("Wall_Right_Lower", new Vector2(FieldHalfWidth + WallThickness / 2f, -wallSideCenterY),
                new Vector2(WallThickness, wallSideHeight));
            CreateWall("Wall_GoalBack", new Vector2(FieldHalfWidth + 1.0f, 0f),
                new Vector2(WallThickness, GoalHalfHeight * 2f + WallThickness * 2f));

            // 5. ゴールセンサー（トリガー）
            var goalSensorObj = new GameObject("GoalSensor");
            var goalCollider = goalSensorObj.AddComponent<BoxCollider2D>();
            goalCollider.isTrigger = true;
            goalCollider.size = new Vector2(1.0f, GoalHalfHeight * 2f);
            goalSensorObj.transform.position = new Vector3(FieldHalfWidth + 0.5f, 0f, 0f);
            var goalTrigger = goalSensorObj.AddComponent<GoalTrigger>();

            // 6. 選手（矢印アイコンでひと目で向きが分かるようにする）
            // DropdownArrow.psd はテクスチャ内の余白が大きく、見た目がボールより
            // 小さくなりすぎるため、ボール(0.5)より一回り大きく見えるスケールにする
            var playerObj = CreateSprite("Player", new Color(0.2f, 0.4f, 1f), PlayerStartPosition,
                new Vector3(2.0f, 2.0f, 1f), "UI/Skin/DropdownArrow.psd");
            playerObj.GetComponent<SpriteRenderer>().sortingOrder = 1;
            var playerRb = playerObj.AddComponent<Rigidbody2D>();
            playerRb.gravityScale = 0f;
            playerRb.freezeRotation = true;
            playerRb.mass = 5f;
            var playerCollider = playerObj.AddComponent<CircleCollider2D>();
            playerCollider.radius = 0.2f;
            playerObj.AddComponent<PlayerController>();

            // 7. ボール
            var ballObj = CreateSprite("Ball", Color.white, BallStartPosition,
                new Vector3(0.5f, 0.5f, 1f), "UI/Skin/Knob.psd");
            ballObj.GetComponent<SpriteRenderer>().sortingOrder = 0;
            var ballRb = ballObj.AddComponent<Rigidbody2D>();
            ballRb.gravityScale = 0f;
            ballRb.freezeRotation = true;
            ballRb.mass = 1f;
            ballRb.linearDamping = 1.5f;
            var ballCollider = ballObj.AddComponent<CircleCollider2D>();
            ballCollider.radius = 0.25f;
            var ball = ballObj.AddComponent<Ball>();

            // 8. UI（メッセージバナー＋スコア表示）
            var canvasObj = new GameObject("Canvas");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            // GOAL!/KICK OFF! など共通で使う中央メッセージ表示
            var messageTextObj = new GameObject("MessageText");
            messageTextObj.transform.SetParent(canvasObj.transform, false);
            var messageRect = messageTextObj.AddComponent<RectTransform>();
            messageRect.anchorMin = new Vector2(0.5f, 0.5f);
            messageRect.anchorMax = new Vector2(0.5f, 0.5f);
            messageRect.pivot = new Vector2(0.5f, 0.5f);
            messageRect.sizeDelta = new Vector2(900, 220);
            var messageText = messageTextObj.AddComponent<Text>();
            messageText.text = "";
            messageText.fontSize = 96;
            messageText.fontStyle = FontStyle.Bold;
            messageText.alignment = TextAnchor.MiddleCenter;
            messageText.color = new Color(1f, 0.85f, 0.1f);
            messageTextObj.SetActive(false);

            // スコア表示（常時表示）
            var scoreTextObj = new GameObject("ScoreText");
            scoreTextObj.transform.SetParent(canvasObj.transform, false);
            var scoreRect = scoreTextObj.AddComponent<RectTransform>();
            scoreRect.anchorMin = new Vector2(0.5f, 1f);
            scoreRect.anchorMax = new Vector2(0.5f, 1f);
            scoreRect.pivot = new Vector2(0.5f, 1f);
            scoreRect.sizeDelta = new Vector2(400, 80);
            scoreRect.anchoredPosition = new Vector2(0f, -20f);
            var scoreText = scoreTextObj.AddComponent<Text>();
            scoreText.text = "SCORE: 0";
            scoreText.fontSize = 44;
            scoreText.fontStyle = FontStyle.Bold;
            scoreText.alignment = TextAnchor.MiddleCenter;
            scoreText.color = Color.white;

            // 9. SoccerGameManager
            var gameManagerObj = new GameObject("SoccerGameManager");
            var gameManager = gameManagerObj.AddComponent<SoccerGameManager>();

            var gmSo = new SerializedObject(gameManager);
            gmSo.FindProperty("_gameTitle").stringValue = "2D Soccer";
            gmSo.FindProperty("_playerRigidbody").objectReferenceValue = playerRb;
            gmSo.FindProperty("_ball").objectReferenceValue = ball;
            gmSo.FindProperty("_playerStartPosition").vector2Value = PlayerStartPosition;
            gmSo.FindProperty("_ballStartPosition").vector2Value = BallStartPosition;
            gmSo.FindProperty("_messageText").objectReferenceValue = messageText;
            gmSo.FindProperty("_scoreText").objectReferenceValue = scoreText;
            gmSo.ApplyModifiedProperties();

            var gtSo = new SerializedObject(goalTrigger);
            gtSo.FindProperty("_gameManager").objectReferenceValue = gameManager;
            gtSo.ApplyModifiedProperties();

            // シーンの保存
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[SoccerSceneBuilder] SoccerScene が正常に生成・保存されました: {ScenePath}");

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

        private static GameObject CreateSprite(string name, Color color, Vector3 position, Vector3 scale, string builtinSpritePath)
        {
            var obj = new GameObject(name);
            obj.transform.position = position;
            obj.transform.localScale = scale;
            var renderer = obj.AddComponent<SpriteRenderer>();
            renderer.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>(builtinSpritePath);
            renderer.color = color;
            return obj;
        }

        private static void CreateWall(string name, Vector2 position, Vector2 size)
        {
            var obj = new GameObject(name);
            obj.transform.position = position;
            var collider = obj.AddComponent<BoxCollider2D>();
            collider.size = size;
        }

        private static void RegisterSceneInBuildSettings(string scenePath)
        {
            var scenes = EditorBuildSettings.scenes;
            foreach (var s in scenes)
            {
                if (s.path == scenePath)
                {
                    Debug.Log($"[SoccerSceneBuilder] Build Settings に既に登録されています: {scenePath}");
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
            Debug.Log($"[SoccerSceneBuilder] Build Settings に {scenePath} を登録しました。");
        }
    }
}
