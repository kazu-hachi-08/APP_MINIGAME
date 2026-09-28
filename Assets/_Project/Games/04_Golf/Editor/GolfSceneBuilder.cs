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

namespace MiniGame.Golf.Editor
{
    /// <summary>
    /// GolfScene を自動生成するエディタユーティリティ。
    /// Scene をコードから作ることで、2人開発での Scene コンフリクトを避ける。
    /// ホールは Scene に置かず、HoleLoader が実行時にカタログから生成する。
    /// </summary>
    public static class GolfSceneBuilder
    {
        private const string RootDirectory = "Assets/_Project/Games/04_Golf";
        private const string SceneDirectory = RootDirectory + "/Scenes";
        private const string ScenePath = SceneDirectory + "/GolfScene.unity";
        private const string DataDirectory = RootDirectory + "/Data";
        private const string SettingsPath = DataDirectory + "/GolfPhysicsSettings.asset";
        private const string TerrainSettingsPath = DataDirectory + "/GolfTerrainSettings.asset";
        private const string NpcDifficultyPath = DataDirectory + "/GolfNpcDifficulty.asset";

        private const string GameTitle = "2D Golf";
        private const string OfflineModeLabel = "1台で遊ぶ";

        // タイトルと同じく縦画面基準
        private static readonly Vector2 ReferenceResolution = new Vector2(1080, 1920);

        // ボールの周りと狙う先まで見える広さ（縦10ユニット）
        private const float CameraOrthographicSize = 5f;
        private static readonly Vector3 CameraPosition = new Vector3(0f, 0f, -10f);

        // コース外まで続く遠くの地面は、ホールの地面（0）より下に描く
        private const int FarGroundSortingOrder = -10;
        // 地面・カップ（ホールのプレハブ側）→ 狙いの線 → 影 → ボールの順に重ねる
        private const int AimGuideSortingOrder = 8;
        private const int ShadowSortingOrder = 10;
        private const int OtherBallSortingOrder = 15;
        private const int BallSortingOrder = 20;
        // プレイヤー色の縁取りは、ボール本体のすぐ下に描く
        private const int BallRingSortingOrder = BallSortingOrder - 1;
        // 背後視点ではゴルファーが一番手前に立つ。立ち位置をボールの横にずらして、ボールを隠さないようにしている。
        // 背中越しに見るので、腕は体の向こう側（下半身・上半身の下）、頭は一番手前に描く
        private const int GolferArmSortingOrder = 21;
        private const int GolferSleeveSortingOrder = 22;
        private const int GolferLegsSortingOrder = 24;
        private const int GolferSortingOrder = 25;
        private const int GolferHeadSortingOrder = GolferSortingOrder + 1;
        private const int GolferCapSortingOrder = GolferSortingOrder + 2;
        // クラブは背中越しに見て体の向こう側にあり、構えたヘッドがボールの横に届くので、体とボール（リング含む）の下に描く
        private const int GolferClubSortingOrder = BallSortingOrder - 2;

        private static readonly Color AimLineColor = new Color(1f, 1f, 1f, 0.7f);
        private static readonly Color LandingMarkerColor = new Color(1f, 1f, 1f, 0.45f);

        private static readonly Vector2 CenterAnchor = new Vector2(0.5f, 0.5f);
        private static readonly Vector2 TopLeftAnchor = new Vector2(0f, 1f);
        private static readonly Vector2 TopRightAnchor = new Vector2(1f, 1f);
        private static readonly Vector2 BottomCenterAnchor = new Vector2(0.5f, 0f);
        private static readonly Vector2 MiddleLeftAnchor = new Vector2(0f, 0.5f);

        // 明るい芝の上でも HUD・風の文字が読めるように縁取りする
        private static readonly Color OverlayTextOutlineColor = new Color(0f, 0f, 0f, 0.6f);

        private const int HudFontSize = 52;
        private const float HudTopMargin = 120f;
        private const float HudHeight = 240f;

        // 下部の操作UIとぶつからないよう、PAUSE ボタンは右上（HUDより上）に小さく置く
        private const float PauseButtonSize = 100f;
        private const int PauseButtonFontSize = 44;
        private const float PauseButtonMargin = 16f;
        private static readonly Color PauseButtonColor = new Color(0.15f, 0.17f, 0.22f, 0.8f);

        // 「全体」ボタンは PAUSE ボタンの下。中央寄せの HUD の文字と重ならない幅にする
        private const float OverviewButtonWidth = 160f;
        private const float OverviewButtonHeight = 100f;
        private const int OverviewButtonFontSize = 40;
        private const float OverviewButtonTop = PauseButtonMargin * 2f + PauseButtonSize;

        // 「？」ボタンは「全体」ボタンのさらに下。説明は縦画面で読める大きさにし、端に余白をとる
        private const float HelpButtonSize = 100f;
        private const int HelpButtonFontSize = 52;
        private const float HelpButtonTop = PauseButtonMargin * 3f + PauseButtonSize + OverviewButtonHeight;
        private const int HelpTextFontSize = 40;
        private const float HelpTextPadding = 60f;
        private static readonly Color HelpPanelColor = new Color(0f, 0f, 0f, 0.85f);

        // 演出メッセージは画面中央のボールより少し上に出し、ボールの止まった場所を隠さない
        private const float MessageOffsetY = 280f;
        private const float MessageWidth = 1000f;
        private const float MessageHeight = 360f;
        private const int MessageFontSize = 120;
        private static readonly Vector2 MessageOutlineDistance = new Vector2(5f, -5f);

        // 風は PAUSE ボタンと反対の左上（HUDより上）に、矢印＋強さで出す
        private const float WindViewMargin = 16f;
        private const float WindArrowSize = 90f;
        private const float WindLabelWidth = 200f;
        private const int WindArrowFontSize = 72;
        private const int WindLabelFontSize = 40;

        // 下から ◀ クラブ ▶ の行 → ゲージ の順に積む。背後視点でボールが隠れないよう、合計の高さは画面の下 2 割弱に収める
        private const float AimControlBottomMargin = 40f;
        private const float AimControlHeight = 130f;
        private const float ClubButtonWidth = 420f;
        private const int ClubButtonFontSize = 40;
        private const float RotateButtonWidth = 200f;
        private const float RotateButtonOffsetX = 340f;
        private const int RotateButtonFontSize = 60;
        private static readonly Color ControlButtonColor = new Color(0.15f, 0.17f, 0.22f, 0.85f);

        private const float GaugeBottomMargin = 200f;
        private const float GaugeWidth = 900f;
        private const float GaugeHeight = 110f;
        private const float GaugeMarkWidth = 12f;
        private const int GaugeLabelFontSize = 40;
        private static readonly Color GaugeBackgroundColor = new Color(0.1f, 0.1f, 0.12f, 0.85f);
        private static readonly Color ImpactZoneColor = new Color(0.95f, 0.75f, 0.2f);
        private static readonly Color PowerMarkColor = new Color(0.95f, 0.3f, 0.3f);

        // スピンはゲージの右上の隅に置く。背後視点で真ん中のボールを隠さないよう、中央には寄せない
        private const float SpinButtonWidth = 180f;
        private const float SpinButtonHeight = 100f;
        private const float SpinButtonOffsetX = 400f;
        private const float SpinButtonGap = 20f;
        private const float SpinButtonBottomMargin = GaugeBottomMargin + GaugeHeight + SpinButtonGap;
        private const int SpinButtonFontSize = 32;

        /// <summary>
        /// 生成順がそのまま Hierarchy の並び順（UIは描画の前後関係）になるため、呼び出し順を入れ替えないこと
        /// </summary>
        [MenuItem("Tools/MiniGame/Build Golf Scene", false, 5)]
        public static void BuildGolfScene()
        {
            EnsureDirectory(SceneDirectory);

            UnityEngine.SceneManagement.Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // NewScene(Single) は未使用アセットをアンロードするため、ScriptableObject は必ずシーンを作った後に読み込む
            var settings = EnsureAsset<GolfPhysicsSettings>(SettingsPath);
            var terrainSettings = EnsureAsset<GolfTerrainSettings>(TerrainSettingsPath);
            var npcDifficulty = EnsureAsset<GolfNpcDifficulty>(NpcDifficultyPath);
            GolfHoleCatalog catalog = GolfHoleAssetBuilder.EnsureAssets();
            GolfClubData[] clubs = GolfClubAssetBuilder.EnsureClubs();
            GolfCharacterCatalog characters = GolfCharacterGenerator.EnsureGenerated();

            Camera camera = CreateCamera();
            CreateEventSystem();
            UIManager uiManager = CreateManagers();
            GolfBall ball = CreateBall(settings, terrainSettings, out BallView ballView);
            HoleLoader holeLoader = CreateHoleLoader(catalog, ball);
            ClubSelector clubSelector = CreateClubSelector(ball, clubs);
            ShotInput input = CreateInput(ball, clubSelector, settings);
            AimGuideView aimGuide = CreateAimGuide(ball, input, clubSelector);
            GolferView golfer = CreateGolfer(ball, input, aimGuide);
            GolfCameraFollower cameraFollower = CreateCameraFollower(camera, ball, golfer, input, clubSelector, holeLoader);
            CreateBackdrop(camera, cameraFollower);
            CreateCourseScenery(holeLoader, cameraFollower, input);
            GolfGameManager manager = CreateGameManager(ball, input, clubSelector, golfer, cameraFollower, characters,
                npcDifficulty);
            CreateOtherBalls(manager);
            CreateCanvas(manager, ball, ballView, input, clubSelector, holeLoader, settings, uiManager, cameraFollower,
                characters);

            SaveScene(scene);
        }

        private static void SaveScene(UnityEngine.SceneManagement.Scene scene)
        {
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[GolfSceneBuilder] GolfScene を生成しました: {ScenePath}");

            RegisterSceneInBuildSettings(ScenePath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        /// <summary>調整用パラメータの ScriptableObject が無ければ初期値で作る（既にあれば調整済みの値を残す）</summary>
        private static T EnsureAsset<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;

            EnsureDirectory(DataDirectory);

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            return asset;
        }

        private static void EnsureDirectory(string directory)
        {
            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
        }

        private static Camera CreateCamera()
        {
            var cameraObj = new GameObject("Main Camera");
            var camera = cameraObj.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            // Tilemap の外側は林として見せる。OB タイルと同じ色にして境目を出さない
            camera.backgroundColor = GolfTileArtBuilder.OutOfBoundsColor;
            camera.orthographic = true;
            camera.orthographicSize = CameraOrthographicSize;
            cameraObj.transform.position = CameraPosition;
            cameraObj.AddComponent<AudioListener>();
            cameraObj.tag = "MainCamera";
            return camera;
        }

        private static GolfCameraFollower CreateCameraFollower(Camera camera, GolfBall ball, GolferView golfer,
            ShotInput input, ClubSelector clubSelector, HoleLoader holeLoader)
        {
            var follower = camera.gameObject.AddComponent<GolfCameraFollower>();
            SetRefs(follower, ("_ball", ball), ("_golfer", golfer), ("_input", input), ("_clubs", clubSelector),
                ("_holeLoader", holeLoader));
            return follower;
        }

        /// <summary>背後視点の空と遠くの地面。板はカメラの子にせず、地面（Z=0）に置く</summary>
        private static void CreateBackdrop(Camera camera, GolfCameraFollower cameraFollower)
        {
            var farGround = new GameObject("FarGround").AddComponent<SpriteRenderer>();
            farGround.sortingOrder = FarGroundSortingOrder;
            SetRefs(camera.gameObject.AddComponent<ShotViewBackdrop>(), ("_follower", cameraFollower),
                ("_farGround", farGround));
        }

        private static void CreateCourseScenery(HoleLoader holeLoader, GolfCameraFollower cameraFollower, ShotInput input)
        {
            SetRefs(new GameObject("CourseScenery").AddComponent<CourseScenery>(), ("_holeLoader", holeLoader),
                ("_cameraFollower", cameraFollower), ("_input", input));
        }

        private static HoleLoader CreateHoleLoader(GolfHoleCatalog catalog, GolfBall ball)
        {
            var loader = new GameObject("HoleLoader").AddComponent<HoleLoader>();
            SetRefs(loader, ("_catalog", catalog), ("_ball", ball));
            return loader;
        }

        /// <summary>ロジック（GolfBall）と見た目（BallView / ShadowView）を別の GameObject に分ける</summary>
        private static GolfBall CreateBall(GolfPhysicsSettings settings, GolfTerrainSettings terrainSettings,
            out BallView ballView)
        {
            var ball = new GameObject("GolfBall").AddComponent<GolfBall>();
            SetRefs(ball, ("_settings", settings), ("_terrainSettings", terrainSettings));

            Transform viewRoot = new GameObject("--- View ---").transform;

            SpriteRenderer shadow = CreateSpriteChild("BallShadow", viewRoot, ShadowSortingOrder);
            SetRefs(shadow.gameObject.AddComponent<ShadowView>(), ("_ball", ball));

            SpriteRenderer ballSprite = CreateSpriteChild("BallView", viewRoot, BallSortingOrder);
            SpriteRenderer ring = CreateSpriteChild("Ring", ballSprite.transform, BallRingSortingOrder);

            ballView = ballSprite.gameObject.AddComponent<BallView>();
            SetRefs(ballView, ("_ball", ball), ("_ring", ring));

            return ball;
        }

        private static void CreateOtherBalls(GolfGameManager manager)
        {
            var view = new GameObject("OtherBalls").AddComponent<OtherBallsView>();
            SetRefs(view, ("_manager", manager));

            var so = new SerializedObject(view);
            so.FindProperty("_sortingOrder").intValue = OtherBallSortingOrder;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static ClubSelector CreateClubSelector(GolfBall ball, GolfClubData[] clubs)
        {
            var selector = new GameObject("ClubSelector").AddComponent<ClubSelector>();
            SetRefs(selector, ("_ball", ball));
            SetArray(selector, "_clubs", clubs);
            return selector;
        }

        private static NpcGolfer CreateNpcGolfer(GolfBall ball, ShotInput input, ClubSelector clubSelector,
            GolfNpcDifficulty difficulty)
        {
            var npc = new GameObject("NpcGolfer").AddComponent<NpcGolfer>();
            SetRefs(npc, ("_ball", ball), ("_input", input), ("_clubs", clubSelector), ("_difficulty", difficulty));
            return npc;
        }

        /// <summary>◀▶ボタンはキャンバスを作るときに差し込む</summary>
        private static ShotInput CreateInput(GolfBall ball, ClubSelector clubSelector, GolfPhysicsSettings settings)
        {
            var input = new GameObject("ShotInput").AddComponent<ShotInput>();
            SetRefs(input, ("_ball", ball), ("_clubs", clubSelector), ("_settings", settings));
            return input;
        }

        private static AimGuideView CreateAimGuide(GolfBall ball, ShotInput input, ClubSelector clubSelector)
        {
            var guide = new GameObject("AimGuide").AddComponent<AimGuideView>();

            SpriteRenderer line = CreateSpriteChild("Line", guide.transform, AimGuideSortingOrder);
            line.color = AimLineColor;

            SpriteRenderer landing = CreateSpriteChild("LandingMarker", guide.transform, AimGuideSortingOrder);
            landing.color = LandingMarkerColor;

            SetRefs(guide, ("_ball", ball), ("_input", input), ("_clubs", clubSelector), ("_line", line),
                ("_landingMarker", landing));
            return guide;
        }

        /// <summary>
        /// 上半身（プレイヤー色）と下半身・頭を分けて、肩と腰を別々に回せるようにする。
        /// クラブは手元（Hands）を回転の中心にするため、Hands の子に置く
        /// </summary>
        private static GolferView CreateGolfer(GolfBall ball, ShotInput input, AimGuideView aimGuide)
        {
            var golfer = new GameObject("Golfer").AddComponent<GolferView>();
            var rig = golfer.gameObject.AddComponent<GolferRig>();
            Transform root = golfer.transform;

            var hands = new GameObject("Hands").transform;
            hands.SetParent(root, false);
            // 帽子は頭の子にして、頭の位置・大きさにそのまま付いていかせる
            SpriteRenderer head = CreateSpriteChild("Head", root, GolferHeadSortingOrder);
            SpriteRenderer cap = CreateSpriteChild("Cap", head.transform, GolferCapSortingOrder);

            SetRefs(rig,
                ("_legs", CreateSpriteChild("Legs", root, GolferLegsSortingOrder)),
                ("_torso", CreateSpriteChild("Torso", root, GolferSortingOrder)),
                ("_head", head),
                ("_cap", cap),
                ("_leftArm", CreateSpriteChild("LeftArm", root, GolferArmSortingOrder)),
                ("_rightArm", CreateSpriteChild("RightArm", root, GolferArmSortingOrder)),
                ("_leftSleeve", CreateSpriteChild("LeftSleeve", root, GolferSleeveSortingOrder)),
                ("_rightSleeve", CreateSpriteChild("RightSleeve", root, GolferSleeveSortingOrder)),
                ("_glove", CreateSpriteChild("Glove", root, GolferSleeveSortingOrder)),
                ("_club", CreateSpriteChild("Club", hands, GolferClubSortingOrder)));
            SetRefs(golfer, ("_ball", ball), ("_input", input), ("_aimGuide", aimGuide), ("_rig", rig));
            return golfer;
        }

        private static SpriteRenderer CreateSpriteChild(string name, Transform parent, int sortingOrder)
        {
            var part = new GameObject(name).AddComponent<SpriteRenderer>();
            part.transform.SetParent(parent, false);
            part.sortingOrder = sortingOrder;
            return part;
        }

        private static void CreateEventSystem()
        {
            // UIのタッチ／クリック判定に必須
            var eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<EventSystem>();
            eventSystemObj.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }

        /// <summary>共通基盤の再利用。PAUSE / 結果画面は UIManager、PC の Esc キーでの PAUSE は InputManager が受け持つ</summary>
        private static UIManager CreateManagers()
        {
            // タイトルへ戻るときのフェード遷移に SceneLoader を使う
            var managersRoot = new GameObject("--- Managers ---");
            CreateManager<SceneLoader>("SceneLoader", managersRoot.transform);
            var audioManager = CreateManager<AudioManager>("AudioManager", managersRoot.transform);
            audioManager.gameObject.AddComponent<ProceduralSe>(); // ボタン音など共通SEの仮音
            CreateManager<InputManager>("InputManager", managersRoot.transform);
            return CreateManager<UIManager>("UIManager", managersRoot.transform);
        }

        /// <summary>NPC と効果音は試合進行から使うので、GolfGameManager と一緒に作って繋ぐ</summary>
        private static GolfGameManager CreateGameManager(GolfBall ball, ShotInput input, ClubSelector clubSelector,
            GolferView golfer, GolfCameraFollower cameraFollower, GolfCharacterCatalog characters,
            GolfNpcDifficulty npcDifficulty)
        {
            var manager = new GameObject("GolfGameManager").AddComponent<GolfGameManager>();
            SetRefs(manager, ("_golferView", golfer), ("_cameraFollower", cameraFollower), ("_characters", characters));

            NpcGolfer npcGolfer = CreateNpcGolfer(ball, input, clubSelector, npcDifficulty);
            GolfAudio audio = CreateAudio(manager, ball, clubSelector);
            SetRefs(manager, ("_npcGolfer", npcGolfer), ("_clubs", clubSelector), ("_audio", audio));

            SetGameTitle(manager);
            return manager;
        }

        private static GolfAudio CreateAudio(GolfGameManager manager, GolfBall ball, ClubSelector clubSelector)
        {
            var audio = manager.gameObject.AddComponent<GolfAudio>();
            SetRefs(audio, ("_ball", ball), ("_clubs", clubSelector));
            return audio;
        }

        private static void SetGameTitle(GolfGameManager manager)
        {
            var so = new SerializedObject(manager);
            so.FindProperty("_gameTitle").stringValue = GameTitle;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateCanvas(GolfGameManager manager, GolfBall ball, BallView ballView, ShotInput input,
            ClubSelector clubSelector, HoleLoader holeLoader, GolfPhysicsSettings settings, UIManager uiManager,
            GolfCameraFollower cameraFollower, GolfCharacterCatalog characters)
        {
            Transform canvas = CreateCanvasRoot();

            // ボタンはノッチ・ホームバーを避けるため SafeArea 内に置く
            Transform safeArea = CreateSafeArea("SafeArea", canvas);
            GolfMessageView message = CreateShotUi(safeArea, manager, ball, input, clubSelector, holeLoader, settings,
                cameraFollower);

            // 試合進行の全画面UIはショット操作より手前に出す
            CreateMatchUi(canvas, manager, ball, ballView, input, holeLoader, characters, message);

            // 設定画面や「○○の番」の間も PAUSE からタイトルへ戻れるよう、PAUSE ボタンは試合進行のUIより手前に置く
            CreatePauseButton(CreateSafeArea("SafeAreaTop", canvas), manager);

            // 共通ダイアログ（PAUSE / リザルト）は PAUSE ボタンより手前に出す
            UIDialogBuilder.BuildDialogs(canvas, uiManager);

            CreateOnline(canvas, manager);
        }

        private static Transform CreateCanvasRoot()
        {
            var canvasObj = new GameObject("Canvas");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            // 縦画面なので横幅に合わせ、機種による左右の見切れを防ぐ
            scaler.matchWidthOrHeight = 0f;
            canvasObj.AddComponent<GraphicRaycaster>();
            return canvasObj.transform;
        }

        private static Transform CreateSafeArea(string name, Transform canvas)
        {
            GameObject obj = UIDialogBuilder.CreateUIObject(name, canvas);
            UIDialogBuilder.SetStretchAll(obj.GetComponent<RectTransform>());
            obj.AddComponent<SafeAreaFitter>();
            return obj.transform;
        }

        /// <summary>ショット中に常に出ている HUD・操作ボタン・演出メッセージ。作った順に手前へ重なる</summary>
        private static GolfMessageView CreateShotUi(Transform safeArea, GolfGameManager manager, GolfBall ball,
            ShotInput input, ClubSelector clubSelector, HoleLoader holeLoader, GolfPhysicsSettings settings,
            GolfCameraFollower cameraFollower)
        {
            CreateShotHud(safeArea, manager, ball, input, holeLoader, settings);
            CreateWindView(safeArea, holeLoader);
            CreateShotGauge(safeArea, input);
            CreateAimControls(safeArea, input, clubSelector, settings);
            CreateSpinButton(safeArea, input, clubSelector);
            SetRefs(cameraFollower, ("_overviewButton", CreateOverviewButton(safeArea)));
            GolfMessageView message = CreateMessageView(safeArea);
            // 開いた説明はショット操作のUIを覆うように、ショット操作より後（手前）に作る
            CreateHelp(safeArea, input);
            return message;
        }

        private static void CreateMatchUi(Transform canvas, GolfGameManager manager, GolfBall ball, BallView ballView,
            ShotInput input, HoleLoader holeLoader, GolfCharacterCatalog characters, GolfMessageView message)
        {
            GolfSetupPanel setupPanel = GolfMatchUiBuilder.CreateSetupPanel(canvas);
            GolfCharacterSelectPanel characterSelectPanel = GolfMatchUiBuilder.CreateCharacterSelectPanel(canvas, characters);
            GolfTurnBannerView turnBanner = GolfMatchUiBuilder.CreateTurnBanner(canvas);
            ScoreCardView scoreCard = GolfMatchUiBuilder.CreateScoreCard(canvas);
            SetRefs(manager, ("_ball", ball), ("_holeLoader", holeLoader), ("_input", input), ("_ballView", ballView),
                ("_setupPanel", setupPanel), ("_characterSelectPanel", characterSelectPanel),
                ("_turnBanner", turnBanner), ("_scoreCard", scoreCard),
                ("_message", message));
        }

        /// <summary>
        /// オンライン対戦。NetworkManager は OnlineSession が実行時に作るので、Scene には置かない。
        /// モード選択は試合前に最初に出すので、戻るボタンも含めて一番手前に置く（待機中はキャンセルで戻れる）
        /// </summary>
        private static void CreateOnline(Transform canvas, GolfGameManager manager)
        {
            var onlineObj = new GameObject("Online");
            var session = onlineObj.AddComponent<OnlineSession>();
            var link = onlineObj.AddComponent<GolfOnlineLink>();
            ModeSelectPanel modeSelectPanel = ModeSelectPanelBuilder.Create(canvas, session, OfflineModeLabel);
            SetRefs(manager, ("_modeSelectPanel", modeSelectPanel), ("_onlineSession", session), ("_onlineLink", link));
        }

        private static void CreateShotGauge(Transform parent, ShotInput input)
        {
            GameObject gaugeObj = CreateButton(parent, "ShotGauge", string.Empty, GaugeWidth, GaugeHeight,
                GaugeBackgroundColor, GaugeLabelFontSize);
            SetBottomCenter(gaugeObj.GetComponent<RectTransform>(), 0f, GaugeBottomMargin);

            Text label = gaugeObj.GetComponentInChildren<Text>();
            label.raycastTarget = false;

            RectTransform zone = CreateGaugeBar(gaugeObj.transform, "ImpactZone", ImpactZoneColor, 0f);
            RectTransform powerMark = CreateGaugeBar(gaugeObj.transform, "PowerMark", PowerMarkColor, GaugeMarkWidth);
            RectTransform marker = CreateGaugeBar(gaugeObj.transform, "Marker", Color.white, GaugeMarkWidth);
            // ラベルはバーより手前に出す
            label.transform.SetAsLastSibling();

            var view = gaugeObj.AddComponent<ShotGaugeView>();
            SetRefs(view, ("_input", input), ("_button", gaugeObj.GetComponent<Button>()),
                ("_zone", zone), ("_marker", marker), ("_powerMark", powerMark), ("_label", label));
        }

        /// <summary>ゲージの上下いっぱいの縦棒。横位置は ShotGaugeView がアンカーで動かす</summary>
        private static RectTransform CreateGaugeBar(Transform parent, string name, Color color, float width)
        {
            GameObject obj = UIDialogBuilder.CreateUIObject(name, parent);
            var rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = CenterAnchor;
            rect.sizeDelta = new Vector2(width, 0f);

            var image = obj.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return rect;
        }

        /// <summary>◀ クラブ ▶ を1行に並べる</summary>
        private static void CreateAimControls(Transform parent, ShotInput input, ClubSelector clubSelector,
            GolfPhysicsSettings settings)
        {
            HoldButton left = CreateRotateButton(parent, "Btn_RotateLeft", "◀", -RotateButtonOffsetX);
            HoldButton right = CreateRotateButton(parent, "Btn_RotateRight", "▶", RotateButtonOffsetX);
            SetRefs(input, ("_rotateLeftButton", left), ("_rotateRightButton", right));

            GameObject clubObj = CreateButton(parent, "Btn_Club", string.Empty, ClubButtonWidth, AimControlHeight,
                ControlButtonColor, ClubButtonFontSize);
            SetBottomCenter(clubObj.GetComponent<RectTransform>(), 0f, AimControlBottomMargin);

            var clubView = clubObj.AddComponent<ClubButtonView>();
            SetRefs(clubView, ("_input", input), ("_clubs", clubSelector), ("_settings", settings),
                ("_button", clubObj.GetComponent<Button>()), ("_label", clubObj.GetComponentInChildren<Text>()));
        }

        private static void CreateSpinButton(Transform parent, ShotInput input, ClubSelector clubSelector)
        {
            GameObject obj = CreateButton(parent, "Btn_Spin", string.Empty, SpinButtonWidth, SpinButtonHeight,
                ControlButtonColor, SpinButtonFontSize);
            SetBottomCenter(obj.GetComponent<RectTransform>(), SpinButtonOffsetX, SpinButtonBottomMargin);

            SetRefs(obj.AddComponent<SpinButtonView>(), ("_input", input), ("_clubs", clubSelector),
                ("_button", obj.GetComponent<Button>()), ("_label", obj.GetComponentInChildren<Text>()));
        }

        private static HoldButton CreateRotateButton(Transform parent, string name, string label, float offsetX)
        {
            GameObject obj = CreateButton(parent, name, label, RotateButtonWidth, AimControlHeight, ControlButtonColor,
                RotateButtonFontSize);
            SetBottomCenter(obj.GetComponent<RectTransform>(), offsetX, AimControlBottomMargin);
            return obj.AddComponent<HoldButton>();
        }

        private static void SetBottomCenter(RectTransform rect, float x, float bottomMargin)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = BottomCenterAnchor;
            rect.anchoredPosition = new Vector2(x, bottomMargin);
        }

        /// <summary>右上の PAUSE ボタンの列に、上から top の位置で揃えて置く</summary>
        private static void SetTopRight(RectTransform rect, float top)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = TopRightAnchor;
            rect.anchoredPosition = new Vector2(-PauseButtonMargin, -top);
        }

        private static void Place(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void CreateShotHud(Transform parent, GolfGameManager manager, GolfBall ball, ShotInput input,
            HoleLoader holeLoader, GolfPhysicsSettings settings)
        {
            var obj = UIDialogBuilder.CreateUIObject("HudView", parent);
            var rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -HudTopMargin);
            rect.sizeDelta = new Vector2(0f, HudHeight);

            Text text = AddOverlayText(obj, HudFontSize, TextAnchor.UpperCenter);

            var hud = obj.AddComponent<GolfHudView>();
            SetRefs(hud, ("_ball", ball), ("_input", input), ("_holeLoader", holeLoader), ("_manager", manager),
                ("_settings", settings), ("_text", text));
        }

        /// <summary>左上に「↑（風の向きへ回す）＋ 風 3m」を並べる</summary>
        private static void CreateWindView(Transform parent, HoleLoader holeLoader)
        {
            GameObject root = UIDialogBuilder.CreateUIObject("WindView", parent);
            Place(root.GetComponent<RectTransform>(), TopLeftAnchor, TopLeftAnchor,
                new Vector2(WindViewMargin, -WindViewMargin), new Vector2(WindArrowSize + WindLabelWidth, WindArrowSize));

            Text arrow = CreateWindText(root.transform, "Arrow", "↑", WindArrowFontSize, TextAnchor.MiddleCenter);
            var arrowRect = arrow.GetComponent<RectTransform>();
            // 中心で回すため、ピボットを真ん中にしてから左端に寄せる
            Place(arrowRect, MiddleLeftAnchor, CenterAnchor, new Vector2(WindArrowSize * 0.5f, 0f),
                new Vector2(WindArrowSize, WindArrowSize));

            Text label = CreateWindText(root.transform, "Strength", string.Empty, WindLabelFontSize, TextAnchor.MiddleLeft);
            Place(label.GetComponent<RectTransform>(), MiddleLeftAnchor, MiddleLeftAnchor, new Vector2(WindArrowSize, 0f),
                new Vector2(WindLabelWidth, WindArrowSize));

            SetRefs(root.AddComponent<WindView>(), ("_holeLoader", holeLoader), ("_arrow", arrowRect),
                ("_strengthLabel", label));
        }

        private static Text CreateWindText(Transform parent, string name, string content, int fontSize, TextAnchor alignment)
        {
            GameObject obj = UIDialogBuilder.CreateUIObject(name, parent);
            Text text = AddOverlayText(obj, fontSize, alignment);
            text.text = content;
            return text;
        }

        /// <summary>
        /// HUD・風のように、ショット中ずっと画面に重ねておく文字。
        /// 画面のどこを押しても打てるように、表示はタップを奪わない
        /// </summary>
        private static Text AddOverlayText(GameObject obj, int fontSize, TextAnchor alignment)
        {
            var text = obj.AddComponent<Text>();
            text.fontSize = fontSize;
            text.fontStyle = FontStyle.Bold;
            text.alignment = alignment;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            obj.AddComponent<Outline>().effectColor = OverlayTextOutlineColor;
            return text;
        }

        /// <summary>
        /// 押している間ホール全体を見せる「全体」ボタン。PAUSE ボタンの下に置く。
        /// 試合進行の全画面UIより奥に置き、設定画面や「○○の番」の間は押せないようにする
        /// </summary>
        private static HoldButton CreateOverviewButton(Transform parent)
        {
            GameObject obj = CreateButton(parent, "Btn_Overview", "全体", OverviewButtonWidth, OverviewButtonHeight,
                ControlButtonColor, OverviewButtonFontSize);
            SetTopRight(obj.GetComponent<RectTransform>(), OverviewButtonTop);
            return obj.AddComponent<HoldButton>();
        }

        /// <summary>
        /// 「？」ボタンと操作説明のパネル。GolfHelpView はパネルを閉じても動き続けるよう、常に有効な親に付ける。
        /// パネル全体をボタンにして、どこをタップしても閉じられるようにする
        /// </summary>
        private static void CreateHelp(Transform parent, ShotInput input)
        {
            GameObject root = UIDialogBuilder.CreateUIObject("Help", parent);
            UIDialogBuilder.SetStretchAll(root.GetComponent<RectTransform>());

            GameObject openObj = CreateButton(root.transform, "Btn_Help", "？", HelpButtonSize, HelpButtonSize,
                ControlButtonColor, HelpButtonFontSize);
            SetTopRight(openObj.GetComponent<RectTransform>(), HelpButtonTop);

            GameObject panelObj = CreateButton(root.transform, "HelpPanel", string.Empty, 0f, 0f, HelpPanelColor,
                HelpTextFontSize);
            UIDialogBuilder.SetStretchAll(panelObj.GetComponent<RectTransform>());
            Text text = SetUpHelpText(panelObj.GetComponentInChildren<Text>());

            SetRefs(root.AddComponent<GolfHelpView>(), ("_input", input), ("_openButton", openObj.GetComponent<Button>()),
                ("_panel", panelObj.GetComponent<Button>()), ("_text", text));
        }

        /// <summary>説明文は複数行を左寄せで読ませ、見出しの強調にリッチテキストを使う</summary>
        private static Text SetUpHelpText(Text text)
        {
            var textRect = text.GetComponent<RectTransform>();
            textRect.offsetMin = new Vector2(HelpTextPadding, HelpTextPadding);
            textRect.offsetMax = new Vector2(-HelpTextPadding, -HelpTextPadding);
            text.fontStyle = FontStyle.Normal;
            text.alignment = TextAnchor.MiddleLeft;
            text.supportRichText = true;
            text.raycastTarget = false;
            return text;
        }

        private static void CreatePauseButton(Transform parent, GolfGameManager manager)
        {
            GameObject buttonObj = CreateButton(parent, "Btn_Pause", "II", PauseButtonSize, PauseButtonSize,
                PauseButtonColor, PauseButtonFontSize);
            SetTopRight(buttonObj.GetComponent<RectTransform>(), PauseButtonMargin);

            SetRefs(buttonObj.AddComponent<PauseButton>(), ("_gameManager", manager));
        }

        /// <summary>「バーディー！」「池ポチャ…」などを大きく出す。表示中だけ有効にする</summary>
        private static GolfMessageView CreateMessageView(Transform parent)
        {
            GameObject obj = UIDialogBuilder.CreateUIObject("MessageView", parent);
            Place(obj.GetComponent<RectTransform>(), CenterAnchor, CenterAnchor, new Vector2(0f, MessageOffsetY),
                new Vector2(MessageWidth, MessageHeight));

            var text = obj.AddComponent<Text>();
            text.fontSize = MessageFontSize;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            // 演出中もタップを奪わない
            text.raycastTarget = false;
            obj.AddComponent<Outline>().effectDistance = MessageOutlineDistance;

            var view = obj.AddComponent<GolfMessageView>();
            SetRefs(view, ("_text", text));
            obj.SetActive(false);
            return view;
        }

        /// <summary>共通のボタン生成に、ラベルの文字サイズ指定を足したもの（共通側は文字サイズが固定のため）</summary>
        internal static GameObject CreateButton(Transform parent, string name, string label, float width, float height,
            Color color, int fontSize)
        {
            GameObject obj = UIDialogBuilder.CreateButton(parent, name, label, width, height, color);
            obj.GetComponentInChildren<Text>().fontSize = fontSize;
            return obj;
        }

        internal static void SetRefs(Object target, params (string property, Object value)[] refs)
        {
            var so = new SerializedObject(target);
            foreach (var (property, value) in refs)
            {
                so.FindProperty(property).objectReferenceValue = value;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>配列のシリアライズフィールドを values で丸ごと置き換える</summary>
        internal static void SetArray(Object target, string property, Object[] values)
        {
            var so = new SerializedObject(target);
            SerializedProperty array = so.FindProperty(property);
            array.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
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
