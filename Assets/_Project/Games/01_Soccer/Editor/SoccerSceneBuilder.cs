using System.Collections.Generic;
using System.IO;
using MiniGame.Common.Audio;
using MiniGame.Common.Input;
using MiniGame.Common.Online;
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
    /// SoccerScene を自動生成・セットアップするエディタユーティリティ
    /// </summary>
    public static class SoccerSceneBuilder
    {
        private const string SceneDirectory = "Assets/_Project/Games/01_Soccer/Scenes";
        private const string ScenePath = SceneDirectory + "/SoccerScene.unity";

        private const string PrefabDirectory = "Assets/_Project/Games/01_Soccer/Prefabs";
        private const string PlayerPrefabPath = PrefabDirectory + "/FieldPlayer.prefab";

        private const string PhysicsDirectory = "Assets/_Project/Games/01_Soccer/Physics";
        private const string BallPhysicsMaterialPath = PhysicsDirectory + "/BallBounce.physicsMaterial2D";

        private const string GameTitle = "2D Soccer";
        private const string OfflineModeLabel = "CPUと対戦";

        // コート寸法（ワールド単位）
        private const float FieldHalfWidth = 16.5f;
        private const float FieldHalfHeight = 9f;
        private const float GoalHalfHeight = 2f;
        private const float WallThickness = 0.3f;
        private const float GoalPocketDepth = 1.0f; // ゴール判定センサー(ポケット)の奥行き
        private const float CornerChamferDepth = 0.6f;

        // 観客席の帯。左右はゴールがゴールラインの外にはみ出すため、その分だけ外側へ離す
        private const float CrowdBandDepth = 3f;
        private const float CrowdSideGap = 1.5f;

        // フォーメーション配列の先頭をGKにしている
        private const int GoalkeeperIndex = 0;

        // 切り替え候補（GKを除く各チームの選手）のうち、キックオフ時に操作する選手のインデックス
        // AWAYはHOMEをX反転した配置なので、同じ番号がキックオフ地点に最も近いFWになる
        // GKはゴールを空けないよう切り替え候補から外すため、フォーメーション配列より1つ手前になる
        private const int DefaultControlledCandidateIndex = 8;

        // カメラはコート全体ではなく、プレイヤー選手周辺をズームして映す
        private const float CameraOrthographicSize = 3.5f;
        private const float CameraZ = -10f;
        private static readonly Color StadiumOutsideColor = new Color(0.09f, 0.11f, 0.13f);

        // ボールと選手の物理
        private const float BallMass = 1f;
        private const float BallLinearDamping = 1.5f;
        private const float BallRadius = 0.25f;
        private const float BallFriction = 0.05f;
        private const float BallBounciness = 0.75f;
        private const float PlayerMass = 5f;
        private const float PlayerRadius = 0.2f;

        // UI（Canvas参照解像度 1920x1080 基準）
        private static readonly Vector2 CanvasReferenceResolution = new Vector2(1920, 1080);
        private const float CanvasMatchWidthOrHeight = 0.5f;
        private static readonly Vector2 CenterAnchor = new Vector2(0.5f, 0.5f);
        private static readonly Vector2 TopCenterAnchor = new Vector2(0.5f, 1f);
        private const int MessageFontSize = 96;
        private const int ScoreFontSize = 44;
        private const int TimerFontSize = 40;
        private const int VirtualButtonFontSize = 32;
        private static readonly Color MessageColor = new Color(1f, 0.85f, 0.1f);
        private static readonly Color TimerColor = new Color(1f, 0.95f, 0.8f);
        private static readonly Color GoalFlashColor = new Color(1f, 0.95f, 0.5f, 0f);
        private const float PauseButtonSize = 90f;
        private static readonly Color PauseButtonColor = new Color(0.15f, 0.17f, 0.22f, 0.8f);

        // 仮想コントロールのレイアウト
        private const float JoystickBackgroundSize = 300f;
        private const float JoystickHandleSize = 130f;
        private const float JoystickHandleRange = 110f;
        private static readonly Color JoystickBackgroundColor = new Color(1f, 1f, 1f, 0.25f);
        private static readonly Color JoystickHandleColor = new Color(1f, 1f, 1f, 0.6f);
        private const string CircleSpritePath = "UI/Skin/Knob.psd";

        // 描画順。選手とボールは YSortRenderer が毎フレーム決めるため、背景は十分小さい固定値にする
        private const int CrowdSortingOrder = -1200;
        private const int CourtSortingOrder = -1000;
        private const int GoalSortingOrder = -900;
        private const int YSortedInitialOrder = 0; // YSortRenderer が上書きするので初期値は仮
        private const int ControlMarkerOrderOffset = -1; // 同じ位置でも選手より奥に敷く
        private const int BallOrderOffset = 1; // 足元のボールが選手に隠れないよう少しだけ手前

        private static readonly Vector2 BallStartPosition = Vector2.zero;

        [MenuItem("Tools/MiniGame/Rebuild Soccer", false, 2)]
        public static void RebuildSoccer()
        {
            // EnsureGenerated は既存PNGを使い回すため、絵のコードを直したときも反映されるようメニューからは必ず描き直す
            SoccerArtGenerator.GenerateAll();
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

        /// <summary>
        /// 生成順がそのまま Hierarchy の並び順（UIは描画の前後関係）になるため、呼び出し順を入れ替えないこと
        /// </summary>
        private static void BuildSoccerSceneInternal()
        {
            Debug.Log("[SoccerSceneBuilder] SoccerScene の構築を開始します...");

            // ドット絵素材（選手・コート・ゴール等）が未生成なら先に作る
            SoccerArtGenerator.EnsureGenerated();
            EnsureDirectory(SceneDirectory);

            UnityEngine.SceneManagement.Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CameraFollow cameraFollow = CreateCamera();
            CreateEventSystem();
            UIManager uiManager = CreateManagers();
            BuildField();

            // ゴールセンサーから参照するため、ゴールより先に生成しておく
            var gameManager = new GameObject("SoccerGameManager").AddComponent<SoccerGameManager>();

            // Home = 左を守り右へ攻める / Away = 右を守り左へ攻める
            BuildGoal(sideSign: -1f, defendingTeam: TeamSide.Home, gameManager: gameManager);
            BuildGoal(sideSign: 1f, defendingTeam: TeamSide.Away, gameManager: gameManager);

            TeamRoster roster = SpawnTeams();
            Transform defaultControlledPlayer = roster.HomeCandidates[DefaultControlledCandidateIndex].transform;
            BindCameraFollow(cameraFollow, defaultControlledPlayer);
            Transform controlMarker = CreateControlMarker(defaultControlledPlayer.position);
            Ball ball = CreateBall();

            Transform canvas = CreateCanvas();
            HudTexts hud = CreateHudTexts(canvas);
            PauseButton pauseButton = CreatePauseButton(canvas);
            VirtualControls virtualControls = BuildVirtualControls(canvas);
            GoalEffect goalEffect = CreateGoalEffect(canvas, hud.Message.rectTransform);
            // 共通ダイアログ（PAUSE / リザルト）。最後に生成して最前面に置く
            UIDialogBuilder.BuildDialogs(canvas, uiManager);

            OnlineParts online = CreateOnlineParts();
            // 対戦モード選択は試合前に最初に出すので、ダイアログよりさらに手前に置く
            ModeSelectPanel modeSelectPanel = ModeSelectPanelBuilder.Create(canvas, online.Session, OfflineModeLabel);

            PlayerSwitcher homeSwitcher = CreatePlayerSwitcher("PlayerSwitcher", roster.HomeCandidates, ball, gameManager);
            BindSwitcherView(homeSwitcher, cameraFollow, controlMarker);
            // AWAY側はオンライン対戦のホストでだけ有効にし、ゲストの入力で動かす
            // （カメラとマーカーはゲスト端末側で SoccerGuestView が動かすので持たせない）
            PlayerSwitcher awaySwitcher = CreatePlayerSwitcher("PlayerSwitcher_Away", roster.AwayCandidates, ball, gameManager);
            awaySwitcher.enabled = false;

            BindOnlineLink(online, roster.AllPlayers, ball, homeSwitcher, awaySwitcher, gameManager);
            BindGuestView(online, ball, homeSwitcher, awaySwitcher, cameraFollow, controlMarker);
            BindGameManager(gameManager, ball, homeSwitcher, awaySwitcher, hud, goalEffect, modeSelectPanel, online, virtualControls);
            BindPauseButton(pauseButton, gameManager);

            SaveScene(scene);
        }

        private static void SaveScene(UnityEngine.SceneManagement.Scene scene)
        {
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[SoccerSceneBuilder] SoccerScene が正常に生成・保存されました: {ScenePath}");

            RegisterSceneInBuildSettings(ScenePath);
            ConfigureAllowedOrientations();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        // ------------------------------------------------------------------
        // カメラ・EventSystem・共通マネージャー
        // ------------------------------------------------------------------
        private static CameraFollow CreateCamera()
        {
            var cameraObj = new GameObject("Main Camera");
            var camera = cameraObj.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = StadiumOutsideColor;
            camera.orthographic = true;
            camera.orthographicSize = CameraOrthographicSize;
            cameraObj.transform.position = new Vector3(0f, 0f, CameraZ);
            cameraObj.AddComponent<AudioListener>();
            cameraObj.tag = "MainCamera";
            return cameraObj.AddComponent<CameraFollow>();
        }

        /// <summary>
        /// 追従対象は初期操作選手にしておき、以降は PlayerSwitcher が切り替える
        /// </summary>
        private static void BindCameraFollow(CameraFollow cameraFollow, Transform target)
        {
            var so = new SerializedObject(cameraFollow);
            SetRef(so, "_target", target);
            so.FindProperty("_fieldHalfExtents").vector2Value = new Vector2(FieldHalfWidth, FieldHalfHeight);
            so.ApplyModifiedProperties();
        }

        /// <summary>
        /// 仮想ジョイスティック/ボタンのタッチ入力に必須
        /// </summary>
        private static void CreateEventSystem()
        {
            var eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<EventSystem>();
            var inputModule = eventSystemObj.AddComponent<InputSystemUIInputModule>();
            inputModule.AssignDefaultActions();
        }

        private static UIManager CreateManagers()
        {
            Transform root = new GameObject("--- Managers ---").transform;
            CreateManager<SceneLoader>("SceneLoader", root);
            var audioManager = CreateManager<AudioManager>("AudioManager", root);
            // SE素材が未用意のため、仮のSEを生成して鳴らす（正式素材を入れたらこのコンポーネントは不要）
            audioManager.gameObject.AddComponent<ProceduralSe>();
            var uiManager = CreateManager<UIManager>("UIManager", root);
            CreateManager<InputManager>("InputManager", root);
            return uiManager;
        }

        private static T CreateManager<T>(string name, Transform parent) where T : Component
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent);
            return obj.AddComponent<T>();
        }

        // ------------------------------------------------------------------
        // コート・観客席・壁・ゴール
        // ------------------------------------------------------------------
        private static void BuildField()
        {
            // ライン込みの1枚絵にし、ライン1本ずつをGameObjectにしない
            CreateSpriteObject("Court", SoccerArtGenerator.Load("Court"), Vector3.zero, CourtSortingOrder);
            BuildCrowdStands();
            BuildTopBottomWalls();
            CreateCornerChamfers();
        }

        /// <summary>
        /// コート外に観客席のタイルを敷く（雰囲気付けのみ。個別の観客やアニメーションは作らない）
        /// </summary>
        private static void BuildCrowdStands()
        {
            float standOffsetX = FieldHalfWidth + CrowdSideGap + CrowdBandDepth / 2f;
            float standOffsetY = FieldHalfHeight + CrowdBandDepth / 2f;
            float horizontalWidth = (FieldHalfWidth + CrowdSideGap + CrowdBandDepth) * 2f;
            float verticalHeight = FieldHalfHeight * 2f + CrowdBandDepth * 2f;

            CreateCrowdBand("Crowd_Top", new Vector2(0f, standOffsetY), new Vector2(horizontalWidth, CrowdBandDepth));
            CreateCrowdBand("Crowd_Bottom", new Vector2(0f, -standOffsetY), new Vector2(horizontalWidth, CrowdBandDepth));
            CreateCrowdBand("Crowd_Left", new Vector2(-standOffsetX, 0f), new Vector2(CrowdBandDepth, verticalHeight));
            CreateCrowdBand("Crowd_Right", new Vector2(standOffsetX, 0f), new Vector2(CrowdBandDepth, verticalHeight));
        }

        private static void CreateCrowdBand(string name, Vector2 center, Vector2 size)
        {
            var obj = CreateSpriteObject(name, SoccerArtGenerator.Load("Crowd"), center, CrowdSortingOrder);
            var renderer = obj.GetComponent<SpriteRenderer>();
            renderer.drawMode = SpriteDrawMode.Tiled;
            renderer.size = size;
        }

        /// <summary>
        /// 上下の辺にはゴールが無いので、辺全体をそのまま塞ぐ
        /// </summary>
        private static void BuildTopBottomWalls()
        {
            float wallY = FieldHalfHeight + WallThickness / 2f;
            var wallSize = new Vector2(FieldHalfWidth * 2f + WallThickness * 2f, WallThickness);
            CreateWall("Wall_Top", new Vector2(0f, wallY), wallSize);
            CreateWall("Wall_Bottom", new Vector2(0f, -wallY), wallSize);
        }

        /// <summary>
        /// コート四隅に斜めの壁を1枚ずつ配置し、直角のポケットにボールが挟まって
        /// 選手ともども動けなくなる硬直状態を防ぐ
        /// </summary>
        private static void CreateCornerChamfers()
        {
            float[] signs = { -1f, 1f };

            foreach (float signX in signs)
            {
                foreach (float signY in signs)
                {
                    Vector2 corner = new Vector2(signX * FieldHalfWidth, signY * FieldHalfHeight);
                    Vector2 pointA = corner - new Vector2(0f, signY * CornerChamferDepth);
                    Vector2 pointB = corner - new Vector2(signX * CornerChamferDepth, 0f);

                    string xLabel = signX < 0f ? "L" : "R";
                    string yLabel = signY < 0f ? "B" : "T";
                    CreateDiagonalWall($"CornerChamfer_{xLabel}{yLabel}", pointA, pointB, WallThickness);
                }
            }
        }

        /// <summary>
        /// 片側のゴール一式（視覚マーカー・ゴール口の壁・裏壁・判定センサー）を生成する
        /// </summary>
        /// <param name="sideSign">-1: 左側, +1: 右側</param>
        /// <param name="defendingTeam">この側を守るチーム（得点するのは逆側のチーム）</param>
        private static void BuildGoal(float sideSign, TeamSide defendingTeam, SoccerGameManager gameManager)
        {
            GoalReaction goalReaction = CreateGoalVisual(sideSign, defendingTeam);
            CreateGoalWalls(sideSign, defendingTeam);
            CreateGoalSensor(sideSign, defendingTeam, gameManager, goalReaction);
        }

        /// <summary>
        /// ゴールはゴールラインの外側に置き、右側は同じ絵をX反転して使う
        /// </summary>
        private static GoalReaction CreateGoalVisual(float sideSign, TeamSide defendingTeam)
        {
            float goalX = sideSign * FieldHalfWidth;
            float goalDepth = SoccerArtGenerator.GoalWidth / (float)SoccerArtGenerator.PixelsPerUnit;
            var goalVisual = CreateSpriteObject($"GoalVisual_{defendingTeam}", SoccerArtGenerator.Load("Goal"),
                new Vector3(goalX + sideSign * goalDepth * 0.5f, 0f, 0f), GoalSortingOrder);
            goalVisual.GetComponent<SpriteRenderer>().flipX = sideSign > 0f;
            return goalVisual.AddComponent<GoalReaction>();
        }

        private static void CreateGoalWalls(float sideSign, TeamSide defendingTeam)
        {
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
        }

        private static void CreateGoalSensor(float sideSign, TeamSide defendingTeam, SoccerGameManager gameManager, GoalReaction goalReaction)
        {
            var goalSensorObj = new GameObject($"GoalSensor_{defendingTeam}");
            var goalCollider = goalSensorObj.AddComponent<BoxCollider2D>();
            goalCollider.isTrigger = true;
            goalCollider.size = new Vector2(GoalPocketDepth, GoalHalfHeight * 2f);
            goalSensorObj.transform.position = new Vector3(sideSign * (FieldHalfWidth + GoalPocketDepth / 2f), 0f, 0f);
            var goalTrigger = goalSensorObj.AddComponent<GoalTrigger>();

            var so = new SerializedObject(goalTrigger);
            so.FindProperty("_defendingTeam").enumValueIndex = (int)defendingTeam;
            SetRef(so, "_gameManager", gameManager);
            SetRef(so, "_goalReaction", goalReaction);
            so.ApplyModifiedProperties();
        }

        private static void CreateWall(string name, Vector2 position, Vector2 size)
        {
            var obj = new GameObject(name);
            obj.transform.position = position;
            var collider = obj.AddComponent<BoxCollider2D>();
            collider.size = size;
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

        // ------------------------------------------------------------------
        // 選手・操作マーカー・ボール
        // ------------------------------------------------------------------

        /// <summary>
        /// 生成した選手の一覧（切り替え候補とオンライン同期用）
        /// </summary>
        private struct TeamRoster
        {
            // GK以外が操作切り替えの候補になる。
            // AWAY側の候補はオンライン対戦でゲストが操作するときだけ使う（CPU戦では常にAI）
            public List<GameObject> HomeCandidates;
            public List<GameObject> AwayCandidates;
            // オンライン同期で両端末の選手を番号で対応付けるため、HOME → AWAY の順で全員を記録する
            public List<TeamMember> AllPlayers;
        }

        /// <summary>
        /// 選手PrefabからHome/Away 11人ずつ、11 vs 11で配置する
        /// </summary>
        private static TeamRoster SpawnTeams()
        {
            GameObject playerPrefab = CreatePlayerPrefab();
            Vector2[] homeFormation = BuildHomeFormation();

            var roster = new TeamRoster
            {
                HomeCandidates = new List<GameObject>(),
                AwayCandidates = new List<GameObject>(),
                AllPlayers = new List<TeamMember>(),
            };
            SpawnTeam(playerPrefab, TeamSide.Home, homeFormation, roster.HomeCandidates, roster.AllPlayers);
            SpawnTeam(playerPrefab, TeamSide.Away, MirrorFormationX(homeFormation), roster.AwayCandidates, roster.AllPlayers);
            return roster;
        }

        private static void SpawnTeam(GameObject prefab, TeamSide team, Vector2[] formation,
            List<GameObject> switchCandidates, List<TeamMember> allPlayers)
        {
            for (int i = 0; i < formation.Length; i++)
            {
                bool isGoalkeeper = i == GoalkeeperIndex;
                var playerObj = SpawnFieldPlayer(prefab, team, formation[i], i, isGoalkeeper);
                allPlayers.Add(playerObj.GetComponent<TeamMember>());

                if (!isGoalkeeper)
                {
                    switchCandidates.Add(playerObj);
                }
            }
        }

        /// <summary>
        /// Home側のフォーメーション基準位置（GK×1, DF×4, MF×4, FW×2 = 11人）
        /// Homeは左側を守り、+X方向（右側のゴール）へ攻める
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
        /// テンプレートを一時的に組み立ててPrefab化し、シーンからは削除する
        /// </summary>
        private static GameObject CreatePlayerPrefab()
        {
            EnsureDirectory(PrefabDirectory);

            var template = CreateSpriteObject("FieldPlayer", SoccerArtGenerator.Load("Player_Home_Down_0"), Vector3.zero, YSortedInitialOrder);
            AddTopDownRigidbody(template, PlayerMass);

            var collider = template.AddComponent<CircleCollider2D>();
            collider.radius = PlayerRadius;

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
        /// 選手を1人生成する。GK以外（切り替え候補）にはAIとプレイヤー操作の両方を持たせ、
        /// 実行時に PlayerSwitcher がどちらか一方だけを有効にする
        /// </summary>
        private static GameObject SpawnFieldPlayer(GameObject prefab, TeamSide team, Vector2 position, int formationIndex, bool isGoalkeeper)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = $"Player_{team}_{formationIndex:00}";
            instance.transform.position = position;

            ApplyTeamSprites(instance, team, isGoalkeeper);
            instance.GetComponent<TeamMember>().SetTeam(team);
            AddAIController(instance, team, position);

            if (!isGoalkeeper)
            {
                AddDisabledPlayerControls(instance);
            }

            return instance;
        }

        /// <summary>
        /// GKは両チームとも緑のユニフォームにして、フィールドプレイヤーと区別する
        /// </summary>
        private static void ApplyTeamSprites(GameObject player, TeamSide team, bool isGoalkeeper)
        {
            string teamKey = isGoalkeeper ? "Gk" : team == TeamSide.Home ? "Home" : "Away";
            AssignPlayerSprites(player.GetComponent<PlayerSpriteAnimator>(), teamKey);
            player.GetComponent<SpriteRenderer>().sprite = SoccerArtGenerator.Load($"Player_{teamKey}_Down_0");
        }

        private static void AddAIController(GameObject player, TeamSide team, Vector2 homePosition)
        {
            var ai = player.AddComponent<AIPlayerController>();
            float attackDirection = team == TeamSide.Home ? 1f : -1f;
            var so = new SerializedObject(ai);
            so.FindProperty("_homePosition").vector2Value = homePosition;
            so.FindProperty("_opponentGoalX").floatValue = attackDirection * FieldHalfWidth;
            so.ApplyModifiedProperties();
        }

        /// <summary>
        /// 切り替え候補のプレイヤー操作は初期状態では無効にしておく
        /// </summary>
        private static void AddDisabledPlayerControls(GameObject player)
        {
            var playerController = player.AddComponent<PlayerController>();
            playerController.enabled = false;

            var slidingTackle = player.AddComponent<SlidingTackle>();
            slidingTackle.enabled = false;
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
            var frames = new Sprite[SoccerArtGenerator.FrameCount];
            for (int i = 0; i < frames.Length; i++)
            {
                frames[i] = SoccerArtGenerator.Load($"Player_{teamKey}_{direction}_{i}");
            }
            SetObjectArray(animatorSo, propertyName, frames);
        }

        /// <summary>
        /// 操作中の選手を示すマーカー。足元に見せるためボールや選手より奥に描画する
        /// </summary>
        private static Transform CreateControlMarker(Vector3 position)
        {
            var marker = CreateSpriteObject("ControlMarker", SoccerArtGenerator.Load("ControlMarker"), position, YSortedInitialOrder);
            AddYSort(marker, ControlMarkerOrderOffset);
            return marker.transform;
        }

        private static Ball CreateBall()
        {
            var ballObj = CreateSpriteObject("Ball", SoccerArtGenerator.Load("Ball"), BallStartPosition, YSortedInitialOrder);
            AddYSort(ballObj, BallOrderOffset);
            var ballRb = AddTopDownRigidbody(ballObj, BallMass);
            ballRb.linearDamping = BallLinearDamping;
            var ballCollider = ballObj.AddComponent<CircleCollider2D>();
            ballCollider.radius = BallRadius;
            ballCollider.sharedMaterial = CreateOrLoadBallPhysicsMaterial();
            return ballObj.AddComponent<Ball>();
        }

        /// <summary>
        /// 真上から見たコートなので重力は使わず、転がりで向きが変わらないよう回転も止める
        /// </summary>
        private static Rigidbody2D AddTopDownRigidbody(GameObject target, float mass)
        {
            var rigidbody = target.AddComponent<Rigidbody2D>();
            rigidbody.gravityScale = 0f;
            rigidbody.freezeRotation = true;
            rigidbody.mass = mass;
            return rigidbody;
        }

        /// <summary>
        /// 壁や隅にボールが張り付いて止まらないよう、反発力を持つ物理マテリアルを用意する
        /// （Unity 2Dの反発係数は接触する2つのColliderのうち大きい方が採用されるため、壁側の変更は不要）
        /// </summary>
        private static PhysicsMaterial2D CreateOrLoadBallPhysicsMaterial()
        {
            EnsureDirectory(PhysicsDirectory);

            var material = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(BallPhysicsMaterialPath);
            bool isNew = material == null;
            if (isNew)
            {
                material = new PhysicsMaterial2D("BallBounce");
            }

            material.friction = BallFriction;
            material.bounciness = BallBounciness;

            if (isNew)
            {
                AssetDatabase.CreateAsset(material, BallPhysicsMaterialPath);
            }
            else
            {
                EditorUtility.SetDirty(material);
            }

            return material;
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

        // ------------------------------------------------------------------
        // UI（HUD・ポーズボタン・ゴール演出・仮想コントロール）
        // ------------------------------------------------------------------
        private static Transform CreateCanvas()
        {
            var canvasObj = new GameObject("Canvas");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = CanvasReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = CanvasMatchWidthOrHeight;
            canvasObj.AddComponent<GraphicRaycaster>(); // タッチ操作のUI判定に必須
            return canvasObj.transform;
        }

        private struct HudTexts
        {
            public Text Message;
            public Text Score;
            public Text Timer;
        }

        private static HudTexts CreateHudTexts(Transform canvas)
        {
            // GOAL!/KICK OFF! など共通で使う中央メッセージ。表示するときだけ有効にする
            Text message = CreateHudText(canvas, "MessageText", "", MessageFontSize, MessageColor,
                CenterAnchor, new Vector2(900, 220), Vector2.zero);
            message.gameObject.SetActive(false);

            // スコアは常時表示、残り試合時間はその直下
            Text score = CreateHudText(canvas, "ScoreText", "HOME 0 - 0 AWAY", ScoreFontSize, Color.white,
                TopCenterAnchor, new Vector2(400, 80), new Vector2(0f, -20f));
            Text timer = CreateHudText(canvas, "TimerText", "2:00", TimerFontSize, TimerColor,
                TopCenterAnchor, new Vector2(300, 60), new Vector2(0f, -100f));

            return new HudTexts { Message = message, Score = score, Timer = timer };
        }

        /// <summary>
        /// ピボットをアンカーと同じ位置にして、画面上端に置くテキストが上端基準で並ぶようにする
        /// </summary>
        private static Text CreateHudText(Transform canvas, string name, string content, int fontSize, Color color,
            Vector2 anchor, Vector2 size, Vector2 anchoredPosition)
        {
            var textObj = UIDialogBuilder.CreateUIObject(name, canvas);
            var rect = textObj.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPosition;

            var text = textObj.AddComponent<Text>();
            ApplyLabelStyle(text, content, fontSize, color);
            return text;
        }

        private static void ApplyLabelStyle(Text text, string content, int fontSize, Color color)
        {
            text.text = content;
            text.fontSize = fontSize;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = color;
        }

        /// <summary>
        /// スマートフォンにはEscキーが無いため、ポーズボタンを画面上に置く
        /// </summary>
        private static PauseButton CreatePauseButton(Transform canvas)
        {
            var pauseButtonObj = UIDialogBuilder.CreateButton(canvas, "Btn_Pause", "II", PauseButtonSize, PauseButtonSize,
                PauseButtonColor);
            SetAnchoredRect(pauseButtonObj.GetComponent<RectTransform>(), new Vector2(1f, 1f),
                new Vector2(-80f, -70f), new Vector2(PauseButtonSize, PauseButtonSize));
            return pauseButtonObj.AddComponent<PauseButton>();
        }

        private static void BindPauseButton(PauseButton pauseButton, SoccerGameManager gameManager)
        {
            var so = new SerializedObject(pauseButton);
            SetRef(so, "_gameManager", gameManager);
            so.ApplyModifiedProperties();
        }

        /// <summary>
        /// ゴール時の画面フラッシュ（仮想コントロールより手前、ダイアログより奥に描画する）
        /// </summary>
        private static GoalEffect CreateGoalEffect(Transform canvas, RectTransform punchTarget)
        {
            var goalFlashObj = UIDialogBuilder.CreateUIObject("GoalEffect", canvas);
            UIDialogBuilder.SetStretchAll(goalFlashObj.GetComponent<RectTransform>());
            var goalFlashImage = goalFlashObj.AddComponent<Image>();
            goalFlashImage.color = GoalFlashColor;
            goalFlashImage.raycastTarget = false; // 演出中もタッチ操作を妨げない
            var goalEffect = goalFlashObj.AddComponent<GoalEffect>();

            var so = new SerializedObject(goalEffect);
            SetRef(so, "_flashImage", goalFlashImage);
            SetRef(so, "_punchTarget", punchTarget);
            so.ApplyModifiedProperties();
            return goalEffect;
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
            var root = UIDialogBuilder.CreateUIObject("VirtualControls", canvas);
            UIDialogBuilder.SetStretchAll(root.GetComponent<RectTransform>());

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
            var joystickObj = UIDialogBuilder.CreateUIObject("VirtualJoystick", parent);
            var backgroundRect = joystickObj.GetComponent<RectTransform>();
            SetAnchoredRect(backgroundRect, new Vector2(0f, 0f), anchoredPosition,
                new Vector2(JoystickBackgroundSize, JoystickBackgroundSize));

            var backgroundImage = joystickObj.AddComponent<Image>();
            backgroundImage.sprite = GetBuiltinSprite(CircleSpritePath);
            backgroundImage.color = JoystickBackgroundColor;

            var handleObj = UIDialogBuilder.CreateUIObject("Handle", joystickObj.transform);
            var handleRect = handleObj.GetComponent<RectTransform>();
            SetAnchoredRect(handleRect, CenterAnchor, Vector2.zero,
                new Vector2(JoystickHandleSize, JoystickHandleSize));

            var handleImage = handleObj.AddComponent<Image>();
            handleImage.sprite = GetBuiltinSprite(CircleSpritePath);
            handleImage.color = JoystickHandleColor;
            handleImage.raycastTarget = false; // 指の下を動くハンドルがドラッグ判定を奪わないようにする

            var joystick = joystickObj.AddComponent<VirtualJoystick>();
            var so = new SerializedObject(joystick);
            SetRef(so, "_background", backgroundRect);
            SetRef(so, "_handle", handleRect);
            so.FindProperty("_handleRange").floatValue = JoystickHandleRange;
            so.ApplyModifiedProperties();

            return joystick;
        }

        private static VirtualButton CreateVirtualButton(Transform parent, string name, string label,
            Vector2 anchoredPosition, float size, Color color)
        {
            var buttonObj = UIDialogBuilder.CreateUIObject(name, parent);
            // 画面右下を基準に配置し、解像度が変わっても親指との位置関係を保つ
            SetAnchoredRect(buttonObj.GetComponent<RectTransform>(), new Vector2(1f, 0f), anchoredPosition,
                new Vector2(size, size));

            var image = buttonObj.AddComponent<Image>();
            image.sprite = GetBuiltinSprite(CircleSpritePath);
            image.color = color;

            var labelObj = UIDialogBuilder.CreateUIObject("Label", buttonObj.transform);
            UIDialogBuilder.SetStretchAll(labelObj.GetComponent<RectTransform>());
            var text = labelObj.AddComponent<Text>();
            ApplyLabelStyle(text, label, VirtualButtonFontSize, Color.white);
            text.raycastTarget = false;

            return buttonObj.AddComponent<VirtualButton>();
        }

        private static void SetAnchoredRect(RectTransform rect, Vector2 anchor, Vector2 anchoredPosition, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = CenterAnchor;
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPosition;
        }

        private static Sprite GetBuiltinSprite(string path)
        {
            return AssetDatabase.GetBuiltinExtraResource<Sprite>(path);
        }

        // ------------------------------------------------------------------
        // 操作切り替え・オンライン・ゲーム進行の参照設定
        // ------------------------------------------------------------------
        private static PlayerSwitcher CreatePlayerSwitcher(string name, List<GameObject> candidates, Ball ball,
            SoccerGameManager gameManager)
        {
            var switcher = new GameObject(name).AddComponent<PlayerSwitcher>();

            var so = new SerializedObject(switcher);
            SetObjectArray(so, "_candidates", candidates);
            SetRef(so, "_ball", ball);
            SetRef(so, "_gameManager", gameManager);
            so.FindProperty("_defaultIndex").intValue = DefaultControlledCandidateIndex;
            so.ApplyModifiedProperties();
            return switcher;
        }

        /// <summary>
        /// この端末の画面で操作する側だけ、カメラと操作マーカーを切り替えに追従させる
        /// </summary>
        private static void BindSwitcherView(PlayerSwitcher switcher, CameraFollow cameraFollow, Transform controlMarker)
        {
            var so = new SerializedObject(switcher);
            SetRef(so, "_cameraFollow", cameraFollow);
            SetRef(so, "_controlMarker", controlMarker);
            so.ApplyModifiedProperties();
        }

        private struct OnlineParts
        {
            public OnlineSession Session;
            public SoccerOnlineLink Link;
            public SoccerGuestView GuestView;
            public RemoteInputProvider RemoteInput;
        }

        /// <summary>
        /// オンライン対戦（接続・同期）。NetworkManager は OnlineSession が実行時に作るので Scene には置かない
        /// </summary>
        private static OnlineParts CreateOnlineParts()
        {
            var onlineObj = new GameObject("Online");
            var parts = new OnlineParts();
            parts.Session = onlineObj.AddComponent<OnlineSession>();
            parts.Link = onlineObj.AddComponent<SoccerOnlineLink>();
            parts.GuestView = onlineObj.AddComponent<SoccerGuestView>();
            parts.RemoteInput = onlineObj.AddComponent<RemoteInputProvider>();
            return parts;
        }

        private static void BindOnlineLink(OnlineParts online, List<TeamMember> allPlayers, Ball ball,
            PlayerSwitcher homeSwitcher, PlayerSwitcher awaySwitcher, SoccerGameManager gameManager)
        {
            var so = new SerializedObject(online.Link);
            SetObjectArray(so, "_players", allPlayers);
            SetRef(so, "_ball", ball);
            SetRef(so, "_homeSwitcher", homeSwitcher);
            SetRef(so, "_awaySwitcher", awaySwitcher);
            SetRef(so, "_gameManager", gameManager);
            SetRef(so, "_remoteInput", online.RemoteInput);
            so.ApplyModifiedProperties();
        }

        private static void BindGuestView(OnlineParts online, Ball ball, PlayerSwitcher homeSwitcher,
            PlayerSwitcher awaySwitcher, CameraFollow cameraFollow, Transform controlMarker)
        {
            var so = new SerializedObject(online.GuestView);
            SetRef(so, "_link", online.Link);
            SetRef(so, "_ball", ball);
            SetObjectArray(so, "_switchers", new[] { homeSwitcher, awaySwitcher });
            SetRef(so, "_cameraFollow", cameraFollow);
            SetRef(so, "_controlMarker", controlMarker);
            so.ApplyModifiedProperties();
        }

        private static void BindGameManager(SoccerGameManager gameManager, Ball ball, PlayerSwitcher homeSwitcher,
            PlayerSwitcher awaySwitcher, HudTexts hud, GoalEffect goalEffect, ModeSelectPanel modeSelectPanel,
            OnlineParts online, VirtualControls virtualControls)
        {
            var so = new SerializedObject(gameManager);
            so.FindProperty("_gameTitle").stringValue = GameTitle;
            SetRef(so, "_ball", ball);
            SetRef(so, "_playerSwitcher", homeSwitcher);
            so.FindProperty("_ballStartPosition").vector2Value = BallStartPosition;
            SetRef(so, "_messageText", hud.Message);
            SetRef(so, "_scoreText", hud.Score);
            SetRef(so, "_timerText", hud.Timer);
            SetRef(so, "_goalEffect", goalEffect);
            SetRef(so, "_modeSelectPanel", modeSelectPanel);
            SetRef(so, "_onlineSession", online.Session);
            SetRef(so, "_onlineLink", online.Link);
            SetRef(so, "_guestView", online.GuestView);
            SetRef(so, "_awaySwitcher", awaySwitcher);
            SetRef(so, "_remoteInput", online.RemoteInput);
            // BaseMiniGameManager が Start 時に InputManager へ登録し、キーボードと同じ経路で入力される
            SetRef(so, "_virtualJoystick", virtualControls.Joystick);
            SetRef(so, "_actionButton1", virtualControls.PassButton);
            SetRef(so, "_actionButton2", virtualControls.ShootButton);
            SetRef(so, "_actionButton3", virtualControls.SwitchButton);
            SetRef(so, "_actionButton4", virtualControls.TackleButton);
            so.ApplyModifiedProperties();
        }

        // ------------------------------------------------------------------
        // 汎用ヘルパー
        // ------------------------------------------------------------------
        private static void SetRef(SerializedObject so, string propertyName, Object value)
        {
            so.FindProperty(propertyName).objectReferenceValue = value;
        }

        private static void SetObjectArray<T>(SerializedObject so, string propertyName, IList<T> values) where T : Object
        {
            var property = so.FindProperty(propertyName);
            property.arraySize = values.Count;
            for (int i = 0; i < values.Count; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
        }

        private static void EnsureDirectory(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
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
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (scenes.Exists(s => s.path == scenePath))
            {
                Debug.Log($"[SoccerSceneBuilder] Build Settings に既に登録されています: {scenePath}");
                return;
            }

            scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log($"[SoccerSceneBuilder] Build Settings に {scenePath} を登録しました。");
        }
    }
}
