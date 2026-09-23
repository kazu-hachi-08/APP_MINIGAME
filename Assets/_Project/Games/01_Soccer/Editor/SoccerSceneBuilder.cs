using System.Collections.Generic;
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

namespace MiniGame.Soccer.Editor
{
    /// <summary>
    /// SoccerScene（Phase 8: UI・演出）を自動生成・セットアップするエディタユーティリティ
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
        private const float FieldHalfWidth = 16.5f;
        private const float FieldHalfHeight = 9f;
        private const float GoalHalfHeight = 2f;
        private const float WallThickness = 0.3f;
        private const float GoalPocketDepth = 1.0f; // ゴール判定センサー(ポケット)の奥行き

        // 切り替え候補（GKを除くHome選手）のうち、キックオフ時に操作する選手のインデックス
        // GKはゴールを空けないよう切り替え候補から外すため、フォーメーション配列より1つ手前になる
        private const int DefaultControlledCandidateIndex = 8;

        // 仮想コントロールのレイアウト（Canvas参照解像度 1920x1080 基準）
        private const float JoystickBackgroundSize = 300f;
        private const float JoystickHandleSize = 130f;
        private const float JoystickHandleRange = 110f;
        private const string CircleSpritePath = "UI/Skin/Knob.psd";

        // 描画順。選手とボールは YSortRenderer が毎フレーム決めるため、背景は十分小さい固定値にする
        private const int CrowdSortingOrder = -1200;
        private const int CourtSortingOrder = -1000;
        private const int GoalSortingOrder = -900;

        private static readonly Vector2 BallStartPosition = Vector2.zero;

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

            // ドット絵素材（選手・コート・ゴール等）が未生成なら先に作る
            SoccerArtGenerator.EnsureGenerated();

            if (!Directory.Exists(SceneDirectory))
            {
                Directory.CreateDirectory(SceneDirectory);
            }

            UnityEngine.SceneManagement.Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. Camera（コート全体ではなく、プレイヤー選手周辺をズームして映す）
            var cameraObj = new GameObject("Main Camera");
            var camera = cameraObj.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.09f, 0.11f, 0.13f); // スタジアムの外周
            camera.orthographic = true;
            camera.orthographicSize = 3.5f;
            cameraObj.transform.position = new Vector3(0f, 0f, -10f);
            cameraObj.AddComponent<AudioListener>();
            cameraObj.tag = "MainCamera";
            var cameraFollow = cameraObj.AddComponent<CameraFollow>();

            // 2. EventSystem（仮想ジョイスティック/ボタンのタッチ入力に必須）
            var eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<EventSystem>();
            var inputModule = eventSystemObj.AddComponent<InputSystemUIInputModule>();
            inputModule.AssignDefaultActions();

            // 3. Managers（共通基盤の再利用）
            var managersRoot = new GameObject("--- Managers ---");
            CreateManager<SceneLoader>("SceneLoader", managersRoot.transform);
            var audioManager = CreateManager<AudioManager>("AudioManager", managersRoot.transform);
            // SE素材が未用意のため、仮のSEを生成して鳴らす（正式素材を入れたらこのコンポーネントは不要）
            audioManager.gameObject.AddComponent<ProceduralSe>();
            var uiManager = CreateManager<UIManager>("UIManager", managersRoot.transform);
            CreateManager<InputManager>("InputManager", managersRoot.transform);

            // 4. コート（ライン込みの1枚絵。ライン1本ずつをGameObjectにしない）
            CreateSpriteObject("Court", SoccerArtGenerator.Load("Court"), Vector3.zero, CourtSortingOrder);
            BuildCrowdStands();

            // 5. 上下の壁（ゴールが無い辺はそのまま塞ぐ）
            CreateWall("Wall_Top", new Vector2(0f, FieldHalfHeight + WallThickness / 2f),
                new Vector2(FieldHalfWidth * 2f + WallThickness * 2f, WallThickness));
            CreateWall("Wall_Bottom", new Vector2(0f, -FieldHalfHeight - WallThickness / 2f),
                new Vector2(FieldHalfWidth * 2f + WallThickness * 2f, WallThickness));

            // 四隅の面取り（直角のポケットにボールが挟まって硬直するのを防ぐ）
            CreateCornerChamfers();

            // 6. SoccerGameManager（ゴールセンサーからの参照解決のため先に生成しておく）
            var gameManagerObj = new GameObject("SoccerGameManager");
            var gameManager = gameManagerObj.AddComponent<SoccerGameManager>();

            // 7. 左右のゴール（Home = 左を守り右へ攻める / Away = 右を守り左へ攻める）
            BuildGoal(sideSign: -1f, defendingTeam: TeamSide.Home, gameManager: gameManager);
            BuildGoal(sideSign: 1f, defendingTeam: TeamSide.Away, gameManager: gameManager);

            // 8. 選手（選手PrefabからHome/Away 11人ずつ、11 vs 11で配置）
            GameObject playerPrefab = CreateOrLoadPlayerPrefab();

            Vector2[] homeFormation = BuildHomeFormation();
            Vector2[] awayFormation = MirrorFormationX(homeFormation);

            // GK（インデックス0）以外のHome選手が操作切り替えの候補になる
            var switchCandidates = new List<GameObject>();

            for (int i = 0; i < homeFormation.Length; i++)
            {
                bool isGoalkeeper = i == 0;
                var playerObj = SpawnFieldPlayer(playerPrefab, TeamSide.Home, homeFormation[i], i, isGoalkeeper, isSwitchCandidate: !isGoalkeeper);

                if (!isGoalkeeper)
                {
                    switchCandidates.Add(playerObj);
                }
            }

            for (int i = 0; i < awayFormation.Length; i++)
            {
                SpawnFieldPlayer(playerPrefab, TeamSide.Away, awayFormation[i], i, isGoalkeeper: i == 0, isSwitchCandidate: false);
            }

            GameObject defaultControlledPlayer = switchCandidates[DefaultControlledCandidateIndex];

            // カメラの追従対象を初期操作選手に設定し、以降は PlayerSwitcher が切り替える
            var cfSo = new SerializedObject(cameraFollow);
            cfSo.FindProperty("_target").objectReferenceValue = defaultControlledPlayer.transform;
            cfSo.FindProperty("_fieldHalfExtents").vector2Value = new Vector2(FieldHalfWidth, FieldHalfHeight);
            cfSo.ApplyModifiedProperties();

            // 操作中の選手を示すマーカー（足元に見せるためボールや選手より奥に描画する）
            var controlMarker = CreateSpriteObject("ControlMarker", SoccerArtGenerator.Load("ControlMarker"),
                defaultControlledPlayer.transform.position, 0);
            AddYSort(controlMarker, orderOffset: -1); // 同じ位置でも選手より奥に敷く

            // 9. ボール
            var ballObj = CreateSpriteObject("Ball", SoccerArtGenerator.Load("Ball"), BallStartPosition, 0);
            AddYSort(ballObj, orderOffset: 1); // 足元のボールが選手に隠れないよう少しだけ手前
            var ballRb = ballObj.AddComponent<Rigidbody2D>();
            ballRb.gravityScale = 0f;
            ballRb.freezeRotation = true;
            ballRb.mass = 1f;
            ballRb.linearDamping = 1.5f;
            var ballCollider = ballObj.AddComponent<CircleCollider2D>();
            ballCollider.radius = 0.25f;
            ballCollider.sharedMaterial = CreateOrLoadBallPhysicsMaterial();
            var ball = ballObj.AddComponent<Ball>();

            // 10. UI（メッセージバナー＋スコア表示）
            var canvasObj = new GameObject("Canvas");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>(); // タッチ操作のUI判定に必須

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

            // 残り試合時間（スコアの直下）
            var timerTextObj = new GameObject("TimerText");
            timerTextObj.transform.SetParent(canvasObj.transform, false);
            var timerRect = timerTextObj.AddComponent<RectTransform>();
            timerRect.anchorMin = new Vector2(0.5f, 1f);
            timerRect.anchorMax = new Vector2(0.5f, 1f);
            timerRect.pivot = new Vector2(0.5f, 1f);
            timerRect.sizeDelta = new Vector2(300, 60);
            timerRect.anchoredPosition = new Vector2(0f, -100f);
            var timerText = timerTextObj.AddComponent<Text>();
            timerText.text = "2:00";
            timerText.fontSize = 40;
            timerText.fontStyle = FontStyle.Bold;
            timerText.alignment = TextAnchor.MiddleCenter;
            timerText.color = new Color(1f, 0.95f, 0.8f);

            // ポーズボタン（スマートフォンにはEscキーが無いため画面上に置く）
            var pauseButtonObj = UIDialogBuilder.CreateButton(canvasObj.transform, "Btn_Pause", "II", 90, 90,
                new Color(0.15f, 0.17f, 0.22f, 0.8f));
            SetAnchoredRect(pauseButtonObj.GetComponent<RectTransform>(), new Vector2(1f, 1f),
                new Vector2(-80f, -70f), new Vector2(90f, 90f));
            var pauseButton = pauseButtonObj.AddComponent<PauseButton>();

            // 11. 仮想コントロール（Phase 7: スマートフォン操作）
            VirtualControls virtualControls = BuildVirtualControls(canvasObj.transform);

            // ゴール時の画面フラッシュ（仮想コントロールより手前、ダイアログより奥に描画する）
            var goalFlashObj = CreateUIObject("GoalEffect", canvasObj.transform);
            SetStretchAll(goalFlashObj.GetComponent<RectTransform>());
            var goalFlashImage = goalFlashObj.AddComponent<Image>();
            goalFlashImage.color = new Color(1f, 0.95f, 0.5f, 0f);
            goalFlashImage.raycastTarget = false; // 演出中もタッチ操作を妨げない
            var goalEffect = goalFlashObj.AddComponent<GoalEffect>();

            var geSo = new SerializedObject(goalEffect);
            geSo.FindProperty("_flashImage").objectReferenceValue = goalFlashImage;
            geSo.FindProperty("_punchTarget").objectReferenceValue = messageRect;
            geSo.ApplyModifiedProperties();

            // 共通ダイアログ（PAUSE / リザルト）。最後に生成して最前面に置く
            UIDialogBuilder.BuildDialogs(canvasObj.transform, uiManager);

            // 12. PlayerSwitcher（Phase 6: 操作対象の自動/手動切り替え）
            var switcherObj = new GameObject("PlayerSwitcher");
            var playerSwitcher = switcherObj.AddComponent<PlayerSwitcher>();

            var psSo = new SerializedObject(playerSwitcher);
            var candidatesProp = psSo.FindProperty("_candidates");
            candidatesProp.arraySize = switchCandidates.Count;
            for (int i = 0; i < switchCandidates.Count; i++)
            {
                candidatesProp.GetArrayElementAtIndex(i).objectReferenceValue = switchCandidates[i];
            }
            psSo.FindProperty("_ball").objectReferenceValue = ball;
            psSo.FindProperty("_cameraFollow").objectReferenceValue = cameraFollow;
            psSo.FindProperty("_controlMarker").objectReferenceValue = controlMarker.transform;
            psSo.FindProperty("_gameManager").objectReferenceValue = gameManager;
            psSo.FindProperty("_defaultIndex").intValue = DefaultControlledCandidateIndex;
            psSo.ApplyModifiedProperties();

            // 13. SoccerGameManager の残りの参照を確定
            var gmSo = new SerializedObject(gameManager);
            gmSo.FindProperty("_gameTitle").stringValue = "2D Soccer";
            gmSo.FindProperty("_ball").objectReferenceValue = ball;
            gmSo.FindProperty("_playerSwitcher").objectReferenceValue = playerSwitcher;
            gmSo.FindProperty("_ballStartPosition").vector2Value = BallStartPosition;
            gmSo.FindProperty("_messageText").objectReferenceValue = messageText;
            gmSo.FindProperty("_scoreText").objectReferenceValue = scoreText;
            gmSo.FindProperty("_timerText").objectReferenceValue = timerText;
            gmSo.FindProperty("_goalEffect").objectReferenceValue = goalEffect;
            // BaseMiniGameManager が Start 時に InputManager へ登録し、キーボードと同じ経路で入力される
            gmSo.FindProperty("_virtualJoystick").objectReferenceValue = virtualControls.Joystick;
            gmSo.FindProperty("_actionButton1").objectReferenceValue = virtualControls.PassButton;
            gmSo.FindProperty("_actionButton2").objectReferenceValue = virtualControls.ShootButton;
            gmSo.FindProperty("_actionButton3").objectReferenceValue = virtualControls.SwitchButton;
            gmSo.FindProperty("_actionButton4").objectReferenceValue = virtualControls.TackleButton;
            gmSo.ApplyModifiedProperties();

            var pauseSo = new SerializedObject(pauseButton);
            pauseSo.FindProperty("_gameManager").objectReferenceValue = gameManager;
            pauseSo.ApplyModifiedProperties();

            // シーンの保存
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[SoccerSceneBuilder] SoccerScene が正常に生成・保存されました: {ScenePath}");

            RegisterSceneInBuildSettings(ScenePath);
            ConfigureAllowedOrientations();

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
                new Vector2(-15.8f, 0f),   // GK
                new Vector2(-10.7f, -6.9f), // DF
                new Vector2(-10.7f, -2.4f), // DF
                new Vector2(-10.7f, 2.4f),  // DF
                new Vector2(-10.7f, 6.9f),  // DF
                new Vector2(-3.5f, -6.9f), // MF
                new Vector2(-3.5f, -2.4f), // MF
                new Vector2(-3.5f, 2.4f),  // MF
                new Vector2(-3.5f, 6.9f),  // MF
                new Vector2(-1.1f, 0f),    // FW（人間操作対象。キックオフ地点に最も近い）
                new Vector2(-4.8f, 5.6f),  // FW
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

            // ゴールはゴールラインの外側に置き、右側は同じ絵をX反転して使う
            float goalDepth = SoccerArtGenerator.GoalWidth / (float)SoccerArtGenerator.PixelsPerUnit;
            var goalVisual = CreateSpriteObject($"GoalVisual_{defendingTeam}", SoccerArtGenerator.Load("Goal"),
                new Vector3(goalX + sideSign * goalDepth * 0.5f, 0f, 0f), GoalSortingOrder);
            goalVisual.GetComponent<SpriteRenderer>().flipX = sideSign > 0f;
            var goalReaction = goalVisual.AddComponent<GoalReaction>();

            float wallSideHeight = FieldHalfHeight - GoalHalfHeight;
            float wallSideCenterY = GoalHalfHeight + wallSideHeight / 2f;
            // ゴール判定センサーの奥行き(GoalPocketDepth)全体を塞ぐ。
            // 以前はWallThickness分しか奥行きがなく、ポスト脇の未カバー領域からボールが
            // 回り込んでゴール扱いになってしまっていた
            float wallSideDepth = GoalPocketDepth + WallThickness;
            float wallX = sideSign * (FieldHalfWidth + wallSideDepth / 2f);
            CreateWall($"Wall_{defendingTeam}_Upper", new Vector2(wallX, wallSideCenterY), new Vector2(wallSideDepth, wallSideHeight));
            CreateWall($"Wall_{defendingTeam}_Lower", new Vector2(wallX, -wallSideCenterY), new Vector2(wallSideDepth, wallSideHeight));

            float backWallX = sideSign * (FieldHalfWidth + GoalPocketDepth);
            CreateWall($"Wall_{defendingTeam}_GoalBack", new Vector2(backWallX, 0f),
                new Vector2(WallThickness, GoalHalfHeight * 2f + WallThickness * 2f));

            var goalSensorObj = new GameObject($"GoalSensor_{defendingTeam}");
            var goalCollider = goalSensorObj.AddComponent<BoxCollider2D>();
            goalCollider.isTrigger = true;
            goalCollider.size = new Vector2(GoalPocketDepth, GoalHalfHeight * 2f);
            goalSensorObj.transform.position = new Vector3(sideSign * (FieldHalfWidth + GoalPocketDepth / 2f), 0f, 0f);
            var goalTrigger = goalSensorObj.AddComponent<GoalTrigger>();

            var gtSo = new SerializedObject(goalTrigger);
            gtSo.FindProperty("_defendingTeam").enumValueIndex = (int)defendingTeam;
            gtSo.FindProperty("_gameManager").objectReferenceValue = gameManager;
            gtSo.FindProperty("_goalReaction").objectReferenceValue = goalReaction;
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
            var template = CreateSpriteObject("FieldPlayer", SoccerArtGenerator.Load("Player_Home_Down_0"), Vector3.zero, 0);

            var rigidbody = template.AddComponent<Rigidbody2D>();
            rigidbody.gravityScale = 0f;
            rigidbody.freezeRotation = true;
            rigidbody.mass = 5f;

            var collider = template.AddComponent<CircleCollider2D>();
            collider.radius = 0.2f;

            template.AddComponent<TeamMember>();
            AssignPlayerSprites(template.AddComponent<PlayerSpriteAnimator>(), "Home");
            template.AddComponent<YSortRenderer>();
            // タックルで倒される演出はどの選手（AI/操作対象）にも起こり得るため、全員に付与する
            template.AddComponent<TackleReaction>();

            GameObject prefabAsset = PrefabUtility.SaveAsPrefabAsset(template, PlayerPrefabPath);
            Object.DestroyImmediate(template);

            return prefabAsset;
        }

        /// <summary>
        /// 選手を1人生成する。切り替え候補の選手にはAIとプレイヤー操作の両方を持たせ、
        /// 実行時に PlayerSwitcher がどちらか一方だけを有効にする
        /// </summary>
        private static GameObject SpawnFieldPlayer(GameObject prefab, TeamSide team, Vector2 position, int formationIndex, bool isGoalkeeper, bool isSwitchCandidate)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = $"Player_{team}_{formationIndex:00}";
            instance.transform.position = position;

            // GKは両チームとも緑のユニフォームにして、フィールドプレイヤーと区別する
            string teamKey = isGoalkeeper ? "Gk" : team == TeamSide.Home ? "Home" : "Away";
            AssignPlayerSprites(instance.GetComponent<PlayerSpriteAnimator>(), teamKey);
            instance.GetComponent<SpriteRenderer>().sprite = SoccerArtGenerator.Load($"Player_{teamKey}_Down_0");

            var teamMember = instance.GetComponent<TeamMember>();
            teamMember.SetTeam(team);

            var ai = instance.AddComponent<AIPlayerController>();
            float attackDirection = team == TeamSide.Home ? 1f : -1f;
            var so = new SerializedObject(ai);
            so.FindProperty("_homePosition").vector2Value = position;
            so.FindProperty("_opponentGoalX").floatValue = attackDirection * FieldHalfWidth;
            so.ApplyModifiedProperties();

            // 切り替え候補のプレイヤー操作は初期状態では無効にしておく
            if (isSwitchCandidate)
            {
                var playerController = instance.AddComponent<PlayerController>();
                playerController.enabled = false;

                var slidingTackle = instance.AddComponent<SlidingTackle>();
                slidingTackle.enabled = false;
            }

            return instance;
        }

        /// <summary>
        /// 生成した仮想コントロール一式（SoccerGameManager への参照設定用）
        /// </summary>
        private struct VirtualControls
        {
            public VirtualJoystick Joystick;
            public VirtualButton PassButton;
            public VirtualButton ShootButton;
            public VirtualButton SwitchButton;
            public VirtualButton TackleButton;
        }

        /// <summary>
        /// スマートフォン操作用の仮想コントロールを生成する（左: ジョイスティック / 右: パス・シュート・切り替え）
        /// PC確認時もそのまま表示し、マウスでタッチ操作を検証できるようにする
        /// </summary>
        private static VirtualControls BuildVirtualControls(Transform canvas)
        {
            var root = CreateUIObject("VirtualControls", canvas);
            SetStretchAll(root.GetComponent<RectTransform>());

            return new VirtualControls
            {
                Joystick = CreateVirtualJoystick(root.transform, new Vector2(280f, 280f)),
                // 右手親指の可動域に合わせ、使用頻度の高いシュートを手前、切り替えを上側に置く
                ShootButton = CreateVirtualButton(root.transform, "Btn_Shoot", "SHOOT",
                    new Vector2(-220f, 240f), 190f, new Color(0.9f, 0.3f, 0.2f, 0.65f)),
                PassButton = CreateVirtualButton(root.transform, "Btn_Pass", "PASS",
                    new Vector2(-450f, 150f), 160f, new Color(0.2f, 0.55f, 0.95f, 0.65f)),
                SwitchButton = CreateVirtualButton(root.transform, "Btn_Switch", "SWITCH",
                    new Vector2(-200f, 500f), 140f, new Color(0.35f, 0.4f, 0.48f, 0.65f)),
                // 他3ボタンと重ならない左上の位置に配置
                TackleButton = CreateVirtualButton(root.transform, "Btn_Tackle", "TACKLE",
                    new Vector2(-450f, 420f), 150f, new Color(0.3f, 0.75f, 0.35f, 0.65f)),
            };
        }

        private static VirtualJoystick CreateVirtualJoystick(Transform parent, Vector2 anchoredPosition)
        {
            var joystickObj = CreateUIObject("VirtualJoystick", parent);
            var backgroundRect = joystickObj.GetComponent<RectTransform>();
            SetAnchoredRect(backgroundRect, new Vector2(0f, 0f), anchoredPosition,
                new Vector2(JoystickBackgroundSize, JoystickBackgroundSize));

            var backgroundImage = joystickObj.AddComponent<Image>();
            backgroundImage.sprite = GetBuiltinSprite(CircleSpritePath);
            backgroundImage.color = new Color(1f, 1f, 1f, 0.25f);

            var handleObj = CreateUIObject("Handle", joystickObj.transform);
            var handleRect = handleObj.GetComponent<RectTransform>();
            SetAnchoredRect(handleRect, new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(JoystickHandleSize, JoystickHandleSize));

            var handleImage = handleObj.AddComponent<Image>();
            handleImage.sprite = GetBuiltinSprite(CircleSpritePath);
            handleImage.color = new Color(1f, 1f, 1f, 0.6f);
            handleImage.raycastTarget = false; // 指の下を動くハンドルがドラッグ判定を奪わないようにする

            var joystick = joystickObj.AddComponent<VirtualJoystick>();
            var so = new SerializedObject(joystick);
            so.FindProperty("_background").objectReferenceValue = backgroundRect;
            so.FindProperty("_handle").objectReferenceValue = handleRect;
            so.FindProperty("_handleRange").floatValue = JoystickHandleRange;
            so.ApplyModifiedProperties();

            return joystick;
        }

        private static VirtualButton CreateVirtualButton(Transform parent, string name, string label,
            Vector2 anchoredPosition, float size, Color color)
        {
            var buttonObj = CreateUIObject(name, parent);
            // 画面右下を基準に配置し、解像度が変わっても親指との位置関係を保つ
            SetAnchoredRect(buttonObj.GetComponent<RectTransform>(), new Vector2(1f, 0f), anchoredPosition,
                new Vector2(size, size));

            var image = buttonObj.AddComponent<Image>();
            image.sprite = GetBuiltinSprite(CircleSpritePath);
            image.color = color;

            var labelObj = CreateUIObject("Label", buttonObj.transform);
            SetStretchAll(labelObj.GetComponent<RectTransform>());
            var text = labelObj.AddComponent<Text>();
            text.text = label;
            text.fontSize = 32;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;

            return buttonObj.AddComponent<VirtualButton>();
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

        private static void SetAnchoredRect(RectTransform rect, Vector2 anchor, Vector2 anchoredPosition, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPosition;
        }

        private static Sprite GetBuiltinSprite(string path)
        {
            return AssetDatabase.GetBuiltinExtraResource<Sprite>(path);
        }

        private static T CreateManager<T>(string name, Transform parent) where T : Component
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent);
            return obj.AddComponent<T>();
        }

        private static GameObject CreateSpriteObject(string name, Sprite sprite, Vector3 position, int sortingOrder)
        {
            var obj = new GameObject(name);
            obj.transform.position = position;
            var renderer = obj.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = sortingOrder;
            return obj;
        }

        private static void AddYSort(GameObject target, int orderOffset)
        {
            var ySort = target.AddComponent<YSortRenderer>();
            var so = new SerializedObject(ySort);
            so.FindProperty("_orderOffset").intValue = orderOffset;
            so.ApplyModifiedProperties();
        }

        /// <summary>
        /// 方向別（正面・背面・横）×3コマの人型スプライトを割り当てる
        /// </summary>
        private static void AssignPlayerSprites(PlayerSpriteAnimator animator, string teamKey)
        {
            var so = new SerializedObject(animator);
            AssignFrames(so, "_downFrames", teamKey, "Down");
            AssignFrames(so, "_upFrames", teamKey, "Up");
            AssignFrames(so, "_sideFrames", teamKey, "Side");
            so.ApplyModifiedProperties();
        }

        private static void AssignFrames(SerializedObject animatorSo, string propertyName, string teamKey, string direction)
        {
            var property = animatorSo.FindProperty(propertyName);
            property.arraySize = SoccerArtGenerator.FrameCount;
            for (int i = 0; i < SoccerArtGenerator.FrameCount; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue =
                    SoccerArtGenerator.Load($"Player_{teamKey}_{direction}_{i}");
            }
        }

        /// <summary>
        /// コート外に観客席のタイルを敷く（雰囲気付けのみ。個別の観客やアニメーションは作らない）
        /// </summary>
        private static void BuildCrowdStands()
        {
            const float bandDepth = 3f;
            float standOffsetX = FieldHalfWidth + 1.5f + bandDepth / 2f;
            float standOffsetY = FieldHalfHeight + bandDepth / 2f;
            float horizontalWidth = (FieldHalfWidth + 1.5f + bandDepth) * 2f;
            float verticalHeight = FieldHalfHeight * 2f + bandDepth * 2f;

            CreateCrowdBand("Crowd_Top", new Vector2(0f, standOffsetY), new Vector2(horizontalWidth, bandDepth));
            CreateCrowdBand("Crowd_Bottom", new Vector2(0f, -standOffsetY), new Vector2(horizontalWidth, bandDepth));
            CreateCrowdBand("Crowd_Left", new Vector2(-standOffsetX, 0f), new Vector2(bandDepth, verticalHeight));
            CreateCrowdBand("Crowd_Right", new Vector2(standOffsetX, 0f), new Vector2(bandDepth, verticalHeight));
        }

        private static void CreateCrowdBand(string name, Vector2 center, Vector2 size)
        {
            var obj = CreateSpriteObject(name, SoccerArtGenerator.Load("Crowd"), center, CrowdSortingOrder);
            var renderer = obj.GetComponent<SpriteRenderer>();
            renderer.drawMode = SpriteDrawMode.Tiled;
            renderer.size = size;
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

        /// <summary>
        /// 実際の画面向きは ScreenOrientationApplier がシーンごとに切り替えるため、
        /// ここでは端末側で全方向を許可しておく（iOS は許可した向きにしか回転できない）
        /// </summary>
        private static void ConfigureAllowedOrientations()
        {
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = true;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
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
