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
    /// SoccerScene（Phase 5: 11 vs 11）を自動生成・セットアップするエディタユーティリティ
    /// </summary>
    public static class SoccerSceneBuilder
    {
        private const string SceneDirectory = "Assets/_Project/Games/01_Soccer/Scenes";
        private const string ScenePath = SceneDirectory + "/SoccerScene.unity";

        private const string PrefabDirectory = "Assets/_Project/Games/01_Soccer/Prefabs";
        private const string PlayerPrefabPath = PrefabDirectory + "/FieldPlayer.prefab";

        private const string PhysicsDirectory = "Assets/_Project/Games/01_Soccer/Physics";
        private const string BallPhysicsMaterialPath = PhysicsDirectory + "/BallBounce.physicsMaterial2D";

        // コート寸法（ワールド単位）
        private const float FieldHalfWidth = 7f;
        private const float FieldHalfHeight = 4f;
        private const float GoalHalfHeight = 1.5f;
        private const float WallThickness = 0.3f;

        // フォーメーション上、人間が操作する選手のインデックス（Home側FWの1人目）
        private const int HumanControlledIndex = 9;

        private static readonly Vector2 BallStartPosition = Vector2.zero;
        private static readonly Color HomeColor = new Color(0.2f, 0.4f, 1f);
        private static readonly Color AwayColor = new Color(0.9f, 0.25f, 0.25f);

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

            // 1. Camera（コート全体ではなく、プレイヤー選手周辺をズームして映す）
            var cameraObj = new GameObject("Main Camera");
            var camera = cameraObj.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.05f, 0.07f, 0.06f);
            camera.orthographic = true;
            camera.orthographicSize = 2.5f;
            cameraObj.transform.position = new Vector3(0f, 0f, -10f);
            cameraObj.AddComponent<AudioListener>();
            cameraObj.tag = "MainCamera";
            var cameraFollow = cameraObj.AddComponent<CameraFollow>();

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

            // 4. 上下の壁（ゴールが無い辺はそのまま塞ぐ）
            CreateWall("Wall_Top", new Vector2(0f, FieldHalfHeight + WallThickness / 2f),
                new Vector2(FieldHalfWidth * 2f + WallThickness * 2f, WallThickness));
            CreateWall("Wall_Bottom", new Vector2(0f, -FieldHalfHeight - WallThickness / 2f),
                new Vector2(FieldHalfWidth * 2f + WallThickness * 2f, WallThickness));

            // 四隅の面取り（直角のポケットにボールが挟まって硬直するのを防ぐ）
            CreateCornerChamfers();

            // 5. SoccerGameManager（ゴールセンサーからの参照解決のため先に生成しておく）
            var gameManagerObj = new GameObject("SoccerGameManager");
            var gameManager = gameManagerObj.AddComponent<SoccerGameManager>();

            // 6. 左右のゴール（Home = 左を守り右へ攻める / Away = 右を守り左へ攻める）
            BuildGoal(sideSign: -1f, defendingTeam: TeamSide.Home, gameManager: gameManager);
            BuildGoal(sideSign: 1f, defendingTeam: TeamSide.Away, gameManager: gameManager);

            // 7. 選手（選手PrefabからHome/Away 11人ずつ、11 vs 11で配置）
            GameObject playerPrefab = CreateOrLoadPlayerPrefab();

            Vector2[] homeFormation = BuildHomeFormation();
            Vector2[] awayFormation = MirrorFormationX(homeFormation);

            Rigidbody2D humanPlayerRb = null;
            Vector2 humanStartPosition = Vector2.zero;

            for (int i = 0; i < homeFormation.Length; i++)
            {
                bool isHuman = i == HumanControlledIndex;
                var playerObj = SpawnFieldPlayer(playerPrefab, TeamSide.Home, HomeColor, homeFormation[i], isHuman);

                if (isHuman)
                {
                    humanPlayerRb = playerObj.GetComponent<Rigidbody2D>();
                    humanStartPosition = homeFormation[i];
                }
            }

            for (int i = 0; i < awayFormation.Length; i++)
            {
                SpawnFieldPlayer(playerPrefab, TeamSide.Away, AwayColor, awayFormation[i], isHuman: false);
            }

            // カメラの追従対象を人間操作選手に設定
            var cfSo = new SerializedObject(cameraFollow);
            cfSo.FindProperty("_target").objectReferenceValue = humanPlayerRb != null ? humanPlayerRb.transform : null;
            cfSo.FindProperty("_fieldHalfExtents").vector2Value = new Vector2(FieldHalfWidth, FieldHalfHeight);
            cfSo.ApplyModifiedProperties();

            // 8. ボール
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
            ballCollider.sharedMaterial = CreateOrLoadBallPhysicsMaterial();
            var ball = ballObj.AddComponent<Ball>();

            // 9. UI（メッセージバナー＋スコア表示）
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
            scoreText.text = "HOME 0 - 0 AWAY";
            scoreText.fontSize = 44;
            scoreText.fontStyle = FontStyle.Bold;
            scoreText.alignment = TextAnchor.MiddleCenter;
            scoreText.color = Color.white;

            // 10. SoccerGameManager の残りの参照を確定
            var gmSo = new SerializedObject(gameManager);
            gmSo.FindProperty("_gameTitle").stringValue = "2D Soccer";
            gmSo.FindProperty("_playerRigidbody").objectReferenceValue = humanPlayerRb;
            gmSo.FindProperty("_ball").objectReferenceValue = ball;
            gmSo.FindProperty("_playerStartPosition").vector2Value = humanStartPosition;
            gmSo.FindProperty("_ballStartPosition").vector2Value = BallStartPosition;
            gmSo.FindProperty("_messageText").objectReferenceValue = messageText;
            gmSo.FindProperty("_scoreText").objectReferenceValue = scoreText;
            gmSo.ApplyModifiedProperties();

            // シーンの保存
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[SoccerSceneBuilder] SoccerScene が正常に生成・保存されました: {ScenePath}");

            RegisterSceneInBuildSettings(ScenePath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// Home側のフォーメーション基準位置（GK×1, DF×4, MF×4, FW×2 = 11人）
        /// Homeは左側を守り、+X方向（右側の既存ゴール）へ攻める
        /// 人間操作のFWはキックオフ時に自チームで最もボールに近い位置に置き、
        /// 味方AIに先を越されずボールへ触れるようにする
        /// </summary>
        private static Vector2[] BuildHomeFormation()
        {
            return new[]
            {
                new Vector2(-6.5f, 0f),   // GK
                new Vector2(-4.5f, -3f),  // DF
                new Vector2(-4.5f, -1f),  // DF
                new Vector2(-4.5f, 1f),   // DF
                new Vector2(-4.5f, 3f),   // DF
                new Vector2(-1.5f, -3f),  // MF
                new Vector2(-1.5f, -1f),  // MF
                new Vector2(-1.5f, 1f),   // MF
                new Vector2(-1.5f, 3f),   // MF
                new Vector2(-0.7f, 0f),   // FW（人間操作対象。キックオフ地点に最も近い）
                new Vector2(-2.0f, 2.5f), // FW
            };
        }

        /// <summary>
        /// X座標を反転してAway側のフォーメーションを作る
        /// </summary>
        private static Vector2[] MirrorFormationX(Vector2[] source)
        {
            var mirrored = new Vector2[source.Length];
            for (int i = 0; i < source.Length; i++)
            {
                mirrored[i] = new Vector2(-source[i].x, source[i].y);
            }
            return mirrored;
        }

        /// <summary>
        /// 片側のゴール一式（視覚マーカー・ゴール口の壁・裏壁・判定センサー）を生成する
        /// </summary>
        /// <param name="sideSign">-1: 左側, +1: 右側</param>
        /// <param name="defendingTeam">この側を守るチーム（得点するのは逆側のチーム）</param>
        private static void BuildGoal(float sideSign, TeamSide defendingTeam, SoccerGameManager gameManager)
        {
            float goalX = sideSign * FieldHalfWidth;

            var goalVisual = CreateSprite($"GoalVisual_{defendingTeam}", new Color(1f, 0.9f, 0.2f, 0.6f),
                new Vector3(goalX, 0f, 0f), new Vector3(0.4f, GoalHalfHeight * 2f, 1f), "UI/Skin/UISprite.psd");
            goalVisual.GetComponent<SpriteRenderer>().sortingOrder = -5;

            float wallSideHeight = FieldHalfHeight - GoalHalfHeight;
            float wallSideCenterY = GoalHalfHeight + wallSideHeight / 2f;
            float wallX = sideSign * (FieldHalfWidth + WallThickness / 2f);
            CreateWall($"Wall_{defendingTeam}_Upper", new Vector2(wallX, wallSideCenterY), new Vector2(WallThickness, wallSideHeight));
            CreateWall($"Wall_{defendingTeam}_Lower", new Vector2(wallX, -wallSideCenterY), new Vector2(WallThickness, wallSideHeight));

            float backWallX = sideSign * (FieldHalfWidth + 1.0f);
            CreateWall($"Wall_{defendingTeam}_GoalBack", new Vector2(backWallX, 0f),
                new Vector2(WallThickness, GoalHalfHeight * 2f + WallThickness * 2f));

            var goalSensorObj = new GameObject($"GoalSensor_{defendingTeam}");
            var goalCollider = goalSensorObj.AddComponent<BoxCollider2D>();
            goalCollider.isTrigger = true;
            goalCollider.size = new Vector2(1.0f, GoalHalfHeight * 2f);
            goalSensorObj.transform.position = new Vector3(sideSign * (FieldHalfWidth + 0.5f), 0f, 0f);
            var goalTrigger = goalSensorObj.AddComponent<GoalTrigger>();

            var gtSo = new SerializedObject(goalTrigger);
            gtSo.FindProperty("_defendingTeam").enumValueIndex = (int)defendingTeam;
            gtSo.FindProperty("_gameManager").objectReferenceValue = gameManager;
            gtSo.ApplyModifiedProperties();
        }

        /// <summary>
        /// 壁や隅にボールが張り付いて止まらないよう、反発力を持つ物理マテリアルを用意する
        /// （Unity 2Dの反発係数は接触する2つのColliderのうち大きい方が採用されるため、壁側の変更は不要）
        /// </summary>
        private static PhysicsMaterial2D CreateOrLoadBallPhysicsMaterial()
        {
            if (!Directory.Exists(PhysicsDirectory))
            {
                Directory.CreateDirectory(PhysicsDirectory);
            }

            var material = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(BallPhysicsMaterialPath);
            if (material == null)
            {
                material = new PhysicsMaterial2D("BallBounce")
                {
                    friction = 0.05f,
                    bounciness = 0.75f
                };
                AssetDatabase.CreateAsset(material, BallPhysicsMaterialPath);
            }
            else
            {
                material.friction = 0.05f;
                material.bounciness = 0.75f;
                EditorUtility.SetDirty(material);
            }

            return material;
        }

        private static GameObject CreateOrLoadPlayerPrefab()
        {
            if (!Directory.Exists(PrefabDirectory))
            {
                Directory.CreateDirectory(PrefabDirectory);
            }

            // テンプレートを一時的に組み立ててPrefab化し、シーンからは削除する
            var template = CreateSprite("FieldPlayer", Color.white, Vector3.zero,
                new Vector3(2.0f, 2.0f, 1f), "UI/Skin/DropdownArrow.psd");

            var rigidbody = template.AddComponent<Rigidbody2D>();
            rigidbody.gravityScale = 0f;
            rigidbody.freezeRotation = true;
            rigidbody.mass = 5f;

            var collider = template.AddComponent<CircleCollider2D>();
            collider.radius = 0.2f;

            template.AddComponent<TeamMember>();

            GameObject prefabAsset = PrefabUtility.SaveAsPrefabAsset(template, PlayerPrefabPath);
            Object.DestroyImmediate(template);

            return prefabAsset;
        }

        private static GameObject SpawnFieldPlayer(GameObject prefab, TeamSide team, Color color, Vector2 position, bool isHuman)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = isHuman ? "Player_Human" : $"Player_{team}_{position}";
            instance.transform.position = position;

            var renderer = instance.GetComponent<SpriteRenderer>();
            renderer.color = color;
            renderer.sortingOrder = isHuman ? 2 : 1;

            var teamMember = instance.GetComponent<TeamMember>();
            teamMember.SetTeam(team);

            if (isHuman)
            {
                instance.AddComponent<PlayerController>();
            }
            else
            {
                var ai = instance.AddComponent<AIPlayerController>();
                float attackDirection = team == TeamSide.Home ? 1f : -1f;
                var so = new SerializedObject(ai);
                so.FindProperty("_homePosition").vector2Value = position;
                so.FindProperty("_opponentGoalX").floatValue = attackDirection * FieldHalfWidth;
                so.ApplyModifiedProperties();
            }

            return instance;
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

        /// <summary>
        /// コート四隅に斜めの壁を1枚ずつ配置し、直角のポケットにボールが挟まって
        /// 選手ともども動けなくなる硬直状態を防ぐ
        /// </summary>
        private static void CreateCornerChamfers()
        {
            const float chamferDepth = 0.6f;
            float[] signs = { -1f, 1f };

            foreach (float signX in signs)
            {
                foreach (float signY in signs)
                {
                    Vector2 corner = new Vector2(signX * FieldHalfWidth, signY * FieldHalfHeight);
                    Vector2 pointA = corner - new Vector2(0f, signY * chamferDepth);
                    Vector2 pointB = corner - new Vector2(signX * chamferDepth, 0f);

                    string xLabel = signX < 0f ? "L" : "R";
                    string yLabel = signY < 0f ? "B" : "T";
                    CreateDiagonalWall($"CornerChamfer_{xLabel}{yLabel}", pointA, pointB, WallThickness);
                }
            }
        }

        /// <summary>
        /// 2点を結ぶ向きに回転させた薄い壁を生成する
        /// </summary>
        private static void CreateDiagonalWall(string name, Vector2 pointA, Vector2 pointB, float thickness)
        {
            Vector2 mid = (pointA + pointB) / 2f;
            Vector2 diff = pointB - pointA;
            float angle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;

            var obj = new GameObject(name);
            obj.transform.position = new Vector3(mid.x, mid.y, 0f);
            obj.transform.rotation = Quaternion.Euler(0f, 0f, angle);
            var collider = obj.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(diff.magnitude, thickness);
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
