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

namespace MiniGame.TableTennis.Editor
{
    /// <summary>
    /// TableTennisScene を自動生成するエディタユーティリティ。
    /// Scene をコードから作ることで、2人開発での Scene コンフリクトを避ける。
    /// </summary>
    public static class TableTennisSceneBuilder
    {
        private const string SceneDirectory = "Assets/_Project/Games/02_TableTennis/Scenes";
        private const string ScenePath = SceneDirectory + "/TableTennisScene.unity";

        private const string SpritesDefaultMaterialPath = "Sprites-Default.mat";

        private const string GameTitle = "2D Table Tennis";
        private const string OfflineModeLabel = "NPCと対戦";

        // 手前にあるものほど大きい値。キャラクターは自分のラケットより後ろに描く
        // TableView側の台パーツが-130〜-50を使っているため、それより奥に置く
        private const int BackgroundSortingOrder = -200;
        private const int NpcCharacterSortingOrder = 3;
        private const int NpcRacketSortingOrder = 5;
        private const int ShadowSortingOrder = 10;
        private const int MascotSortingOrder = 11;
        private const int BounceSortingOrder = 12;
        private const int PlayerCharacterSortingOrder = 15;
        private const int BallSortingOrder = 20;
        private const int RacketSortingOrder = 30;

        // カメラ（台の手前から奥を見る擬似3Dの画角に合わせる）
        // 背景スプライトが常に画面を覆うが、リサイズが追いつくまでの隙間用に壁の色に合わせておく
        private static readonly Color32 CameraBackgroundColor = new Color32(30, 34, 46, 255);
        private const float CameraOrthographicSize = 4.5f;
        // 画面上部のHUD（スコア・打球結果）と台・キャラクターが重ならないよう、
        // カメラを上げて描画全体をHUDの下へ落とす
        private static readonly Vector3 CameraPosition = new Vector3(0f, 0.6f, -10f);

        private static readonly Color ShadowColor = new Color(0f, 0f, 0f, 0.45f);

        // キャラクターの立ち位置と見た目の大きさ
        private const float NpcCharacterCourtZ = 2.1f;
        private const float NpcCharacterFollowRatio = 0.8f;
        private const float NpcCharacterDisplayHeight = 1.45f;
        private const float NpcCharacterBaseYOffset = 0f;
        // プレイヤーは背中側。台を隠さないよう、画面下の帯にはみ出させる
        private const float PlayerCharacterCourtZ = -1.7f;
        private const float PlayerCharacterFollowRatio = 0.5f;
        private const float PlayerCharacterDisplayHeight = 2.6f;
        private const float PlayerCharacterBaseYOffset = -2.3f;

        // UI（Canvas参照解像度 1920x1080 基準）
        private static readonly Vector2 CanvasReferenceResolution = new Vector2(1920, 1080);
        private const float CanvasMatchWidthOrHeight = 0.5f;
        private static readonly Vector2 CenterAnchor = new Vector2(0.5f, 0.5f);
        private static readonly Vector2 TopCenterAnchor = new Vector2(0.5f, 1f);

        // 画面最上部はインカメラのノッチと重なるため、その下まで下げて表示する
        private const int ScoreFontSize = 64;
        private static readonly Vector2 ScorePosition = new Vector2(0f, -180f);
        private static readonly Vector2 ScoreSize = new Vector2(760f, 90f);
        private static readonly Color ScoreColor = new Color(1f, 1f, 1f);

        private const int MessageFontSize = 96;
        private static readonly Vector2 MessagePosition = new Vector2(0f, 120f);
        private static readonly Vector2 MessageSize = new Vector2(900f, 220f);
        private static readonly Color MessageColor = new Color(1f, 0.85f, 0.1f);

        // 打球の手応え（タイミング・回転）はスコアのすぐ下に返す。
        // 画面下は自分の操作（ラケット・指）で隠れて読めないため
        private const int ShotInfoFontSize = 44;
        private static readonly Vector2 ShotInfoPosition = new Vector2(0f, -290f);
        private static readonly Vector2 ShotInfoSize = new Vector2(900f, 150f);
        private static readonly Color ShotInfoColor = new Color(0.85f, 0.92f, 1f);

        private const float PauseButtonSize = 90f;
        private static readonly Vector2 TopRightAnchor = new Vector2(1f, 1f);
        private static readonly Vector2 PauseButtonPosition = new Vector2(-80f, -70f);
        private static readonly Color PauseButtonColor = new Color(0.15f, 0.17f, 0.22f, 0.8f);

        // 必殺技ボタン。画面下は指とラケットで隠れるため、右端の中ほどに縦に並べる
        private const float SpecialButtonWidth = 220f;
        private const float SpecialButtonHeight = 150f;
        private const float SpecialButtonRightMargin = -40f;
        private const float WeakSpecialButtonY = -85f;
        private const float StrongSpecialButtonY = 85f;
        private const int SpecialButtonFontSize = 30;
        private static readonly Vector2 RightMiddleAnchor = new Vector2(1f, 0.5f);
        private static readonly Color SpecialButtonColor = new Color(0.95f, 0.45f, 0.1f);

        // 試合前パネル（難易度・選手選択）の共通見た目
        private static readonly Color PanelOverlayColor = new Color(0f, 0f, 0f, 0.75f);
        private static readonly Color PanelBoxColor = new Color(0.12f, 0.14f, 0.18f);
        private static readonly Color ChoiceButtonColor = new Color(0.3f, 0.33f, 0.4f);
        private const int PanelTitleFontSize = 30;
        private const float ButtonRowSpacing = 16f;

        private static readonly Vector2 DifficultyBoxSize = new Vector2(860, 360);
        private const float DifficultyButtonWidth = 140f;
        private const float DifficultyButtonHeight = 96f;
        private const int DifficultyButtonFontSize = 20;
        // 初めて遊ぶ人が迷わないよう「ふつう」を目立たせる
        private const int RecommendedDifficultyIndex = 2;
        private static readonly Color RecommendedDifficultyColor = new Color(0.18f, 0.55f, 0.9f);
        private static readonly string[] DifficultyLabels =
            { "Lv.1\nやさしい", "Lv.2", "Lv.3\nふつう", "Lv.4", "Lv.5\nむずかしい" };

        private const float LoadoutBoxWidth = 900f;
        private const int LoadoutBoxPadding = 24;
        private const float LoadoutRowSpacing = 12f;
        private const float LoadoutLabelWidth = 820f;
        private const float LoadoutTitleHeight = 56f;
        private const int LoadoutSectionFontSize = 24;
        private const float LoadoutSectionHeight = 40f;
        private const float ChoiceButtonWidth = 260f;
        private const float CharacterButtonHeight = 100f;
        private const float RacketButtonHeight = 76f;
        private const int ChoiceButtonFontSize = 24;
        private const float DecideButtonWidth = 320f;
        private const float DecideButtonHeight = 90f;
        private const int DecideButtonFontSize = 30;
        private static readonly Color DecideButtonColor = new Color(0.2f, 0.65f, 0.35f);

        private struct BallParts
        {
            public BallMotion Motion;
            public SpriteRenderer Renderer;
        }

        private struct PlayerParts
        {
            public RacketController Racket;
            public PlayerSwing Swing;
            public ShotCalculator ShotCalculator;
            public CharacterView Character;
        }

        private struct OpponentParts
        {
            public NpcController Npc;
            public NpcRacketView RacketView;
            public CharacterView Character;
        }

        private struct HudParts
        {
            public Text Score;
            public HudText Message;
            public HudText ShotInfo;
        }

        private struct PreMatchPanels
        {
            public DifficultySelectPanel Difficulty;
            public LoadoutSelectPanel Loadout;
            public ModeSelectPanel ModeSelect;
        }

        private struct OnlineParts
        {
            public OnlineSession Session;
            public OnlineMatchLink Link;
            public RemoteOpponent Remote;
        }

        [MenuItem("Tools/MiniGame/Rebuild Table Tennis", false, 3)]
        public static void RebuildTableTennis()
        {
            // EnsureGenerated は既存PNGを使い回すため、絵のコードを直したときも反映されるようメニューからは必ず描き直す
            TableTennisArtGenerator.GenerateAll();
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

        /// <summary>
        /// 生成順がそのまま Hierarchy の並び順（UIは描画の前後関係）になるため、呼び出し順を入れ替えないこと
        /// </summary>
        private static void BuildInternal()
        {
            Debug.Log("[TableTennisSceneBuilder] TableTennisScene の構築を開始します...");

            if (!Directory.Exists(SceneDirectory))
            {
                Directory.CreateDirectory(SceneDirectory);
            }

            UnityEngine.SceneManagement.Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            TableTennisArtGenerator.EnsureGenerated();

            Camera camera = CreateCamera();
            CreateBackground(camera);
            CreateEventSystem();
            UIManager uiManager = CreateManagers(out TableTennisAudio tableTennisAudio);

            TableLayout table = CreateTable();
            BallParts ball = CreateBall(table);
            CreateBounceEffect(table, ball.Motion);
            PlayerParts player = CreatePlayer(table, ball.Motion, camera);
            OpponentParts opponent = CreateOpponent(table, ball.Motion, player.Racket);
            // キャラクターはラケットの左右移動に合わせて立たせるだけの表示
            opponent.Character = CreateCharacter("NpcCharacter", TableTennisArtGenerator.NpcCharacterName,
                NpcCharacterSortingOrder, table, opponent.Npc,
                NpcCharacterCourtZ, NpcCharacterFollowRatio, NpcCharacterDisplayHeight, NpcCharacterBaseYOffset);
            player.Character = CreateCharacter("PlayerCharacter", TableTennisArtGenerator.PlayerCharacterName,
                PlayerCharacterSortingOrder, table, player.Racket,
                PlayerCharacterCourtZ, PlayerCharacterFollowRatio, PlayerCharacterDisplayHeight, PlayerCharacterBaseYOffset);

            Transform canvas = CreateCanvas();
            HudParts hud = CreateHud(canvas);
            PauseButton pauseButton = CreatePauseButton(canvas);
            GameObject weakButtonObj = CreateSpecialButton(canvas, "Btn_WeakSpecial", WeakSpecialButtonY);
            GameObject strongButtonObj = CreateSpecialButton(canvas, "Btn_StrongSpecial", StrongSpecialButtonY);

            // 共通ダイアログ（PAUSE / リザルト）は最前面に置くため、ゲーム中のUIより後に生成する
            UIDialogBuilder.BuildDialogs(canvas, uiManager);

            // 試合前パネルは開始前に出すので、ダイアログよりさらに手前に置く
            var panels = new PreMatchPanels();
            panels.Difficulty = CreateDifficultySelectPanel(canvas);
            LoadoutCatalog loadoutCatalog = TableTennisLoadoutGenerator.EnsureGenerated();
            panels.Loadout = CreateLoadoutSelectPanel(canvas, loadoutCatalog);

            SpecialController special = CreateSpecial(loadoutCatalog, table, ball, player, opponent.Npc,
                weakButtonObj, strongButtonObj);

            OnlineParts online = CreateOnline(player.Racket, opponent);
            // 対戦モード選択は試合前に最初に出すので最前面に置く
            panels.ModeSelect = ModeSelectPanelBuilder.Create(canvas, online.Session, OfflineModeLabel);

            TableTennisGameManager gameManager = CreateGameManager(ball.Motion, player, opponent, tableTennisAudio,
                hud, panels, loadoutCatalog, special, online);
            SetReference(pauseButton, "_gameManager", gameManager);

            SaveScene(scene);
        }

        private static void SaveScene(UnityEngine.SceneManagement.Scene scene)
        {
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[TableTennisSceneBuilder] TableTennisScene を生成しました: {ScenePath}");

            RegisterSceneInBuildSettings(ScenePath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        // ------------------------------------------------------------------
        // カメラ・背景・EventSystem・共通マネージャー
        // ------------------------------------------------------------------
        private static Camera CreateCamera()
        {
            var cameraObj = new GameObject("Main Camera");
            var camera = cameraObj.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = CameraBackgroundColor;
            camera.orthographic = true;
            camera.orthographicSize = CameraOrthographicSize;
            cameraObj.transform.position = CameraPosition;
            cameraObj.AddComponent<AudioListener>();
            cameraObj.tag = "MainCamera";

            // 縦長のスマートフォンでもラケットの可動範囲が切れないようにする
            var cameraFitter = cameraObj.AddComponent<CameraFitter>();
            SetReference(cameraFitter, "_camera", camera);
            return camera;
        }

        /// <summary>体育館の壁と床。カメラの表示範囲いっぱいに常に引き伸ばす</summary>
        private static void CreateBackground(Camera camera)
        {
            var backgroundObj = CreateSpriteObject("Background", LoadSprite(TableTennisArtGenerator.BackgroundName),
                Color.white, BackgroundSortingOrder);
            var backgroundView = backgroundObj.AddComponent<BackgroundView>();
            SetReference(backgroundView, "_camera", camera);
        }

        /// <summary>UIのタッチ判定に必須</summary>
        private static void CreateEventSystem()
        {
            var eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<EventSystem>();
            eventSystemObj.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }

        private static UIManager CreateManagers(out TableTennisAudio tableTennisAudio)
        {
            var managersRoot = new GameObject("--- Managers ---");
            CreateManager<SceneLoader>("SceneLoader", managersRoot.transform);
            var audioManager = CreateManager<AudioManager>("AudioManager", managersRoot.transform);
            audioManager.gameObject.AddComponent<ProceduralSe>(); // 正式なSE素材が入るまでの仮音

            // 卓球固有のSE（打球・バウンド・ネット）は共通の SeId に持たせず、ここで生成して鳴らす
            tableTennisAudio = CreateManager<TableTennisAudio>("TableTennisAudio", managersRoot.transform);
            var uiManager = CreateManager<UIManager>("UIManager", managersRoot.transform);
            CreateManager<InputManager>("InputManager", managersRoot.transform);
            return uiManager;
        }

        private static T CreateManager<T>(string name, Transform parent) where T : Component
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent);
            return obj.AddComponent<T>();
        }

        // ------------------------------------------------------------------
        // 台・ボール
        // ------------------------------------------------------------------
        /// <summary>寸法と擬似3D変換の基準になる卓球台</summary>
        private static TableLayout CreateTable()
        {
            var tableObj = new GameObject("Table");
            var tableLayout = tableObj.AddComponent<TableLayout>();
            var tableView = tableObj.AddComponent<TableView>();

            var so = new SerializedObject(tableView);
            so.FindProperty("_table").objectReferenceValue = tableLayout;
            so.FindProperty("_material").objectReferenceValue =
                AssetDatabase.GetBuiltinExtraResource<Material>(SpritesDefaultMaterialPath);
            so.FindProperty("_surfaceSprite").objectReferenceValue = LoadSprite(TableTennisArtGenerator.TableSurfaceName);
            so.FindProperty("_netSprite").objectReferenceValue = LoadSprite(TableTennisArtGenerator.NetName);
            so.ApplyModifiedProperties();
            return tableLayout;
        }

        /// <summary>影は奥行きを読み取る手がかりになるので、ボールとは別オブジェクトで用意する</summary>
        private static BallParts CreateBall(TableLayout table)
        {
            var shadowObj = CreateSpriteObject("BallShadow", LoadSprite(TableTennisArtGenerator.ShadowName),
                ShadowColor, ShadowSortingOrder);

            var ballObj = CreateSpriteObject("Ball", LoadSprite(TableTennisArtGenerator.BallName),
                Color.white, BallSortingOrder);
            var ballMotion = ballObj.AddComponent<BallMotion>();
            var ballView = ballObj.AddComponent<BallView>();
            var ballRenderer = ballObj.GetComponent<SpriteRenderer>();

            SetReference(ballMotion, "_table", table);

            var so = new SerializedObject(ballView);
            so.FindProperty("_table").objectReferenceValue = table;
            so.FindProperty("_ball").objectReferenceValue = ballMotion;
            so.FindProperty("_renderer").objectReferenceValue = ballRenderer;
            so.FindProperty("_shadow").objectReferenceValue = shadowObj.transform;
            so.FindProperty("_shadowRenderer").objectReferenceValue = shadowObj.GetComponent<SpriteRenderer>();
            so.ApplyModifiedProperties();

            return new BallParts { Motion = ballMotion, Renderer = ballRenderer };
        }

        /// <summary>バウンド位置を輪で見せる（着地点が読み取りやすくなる）</summary>
        private static void CreateBounceEffect(TableLayout table, BallMotion ball)
        {
            var bounceObj = CreateSpriteObject("BounceEffect", LoadSprite(TableTennisArtGenerator.BounceRingName),
                Color.white, BounceSortingOrder);
            var bounceEffect = bounceObj.AddComponent<BounceEffect>();

            var so = new SerializedObject(bounceEffect);
            so.FindProperty("_table").objectReferenceValue = table;
            so.FindProperty("_ball").objectReferenceValue = ball;
            so.FindProperty("_renderer").objectReferenceValue = bounceObj.GetComponent<SpriteRenderer>();
            so.ApplyModifiedProperties();
        }

        // ------------------------------------------------------------------
        // プレイヤー（入力・ラケット）と NPC
        // ------------------------------------------------------------------
        /// <summary>タップでラケット移動 / フリックで打球する入力と、自分のラケット</summary>
        private static PlayerParts CreatePlayer(TableLayout table, BallMotion ball, Camera camera)
        {
            var playerRigObj = new GameObject("PlayerRig");
            var flickInput = playerRigObj.AddComponent<FlickInput>();
            var timingJudge = playerRigObj.AddComponent<SwingTimingJudge>();
            var shotCalculator = playerRigObj.AddComponent<ShotCalculator>();
            var playerSwing = playerRigObj.AddComponent<PlayerSwing>();

            var racketObj = CreateSpriteObject("Racket", LoadSprite(TableTennisArtGenerator.PlayerRacketName),
                Color.white, RacketSortingOrder);
            var racket = racketObj.AddComponent<RacketController>();
            var racketSwingView = racketObj.AddComponent<RacketSwingView>();

            var racketSo = new SerializedObject(racket);
            racketSo.FindProperty("_table").objectReferenceValue = table;
            racketSo.FindProperty("_flickInput").objectReferenceValue = flickInput;
            racketSo.FindProperty("_camera").objectReferenceValue = camera;
            racketSo.FindProperty("_renderer").objectReferenceValue = racketObj.GetComponent<SpriteRenderer>();
            racketSo.ApplyModifiedProperties();

            SetReference(shotCalculator, "_ball", ball);

            var swingSo = new SerializedObject(playerSwing);
            swingSo.FindProperty("_ball").objectReferenceValue = ball;
            swingSo.FindProperty("_racket").objectReferenceValue = racket;
            swingSo.FindProperty("_flickInput").objectReferenceValue = flickInput;
            swingSo.FindProperty("_timingJudge").objectReferenceValue = timingJudge;
            swingSo.FindProperty("_shotCalculator").objectReferenceValue = shotCalculator;
            swingSo.ApplyModifiedProperties();

            var swingViewSo = new SerializedObject(racketSwingView);
            swingViewSo.FindProperty("_table").objectReferenceValue = table;
            swingViewSo.FindProperty("_racket").objectReferenceValue = racket;
            swingViewSo.FindProperty("_playerSwing").objectReferenceValue = playerSwing;
            swingViewSo.ApplyModifiedProperties();

            return new PlayerParts { Racket = racket, Swing = playerSwing, ShotCalculator = shotCalculator };
        }

        /// <summary>思考と表示を分け、返球内容は NpcController が決める</summary>
        private static OpponentParts CreateOpponent(TableLayout table, BallMotion ball, RacketController playerRacket)
        {
            var npcRacketObj = CreateSpriteObject("NpcRacket", LoadSprite(TableTennisArtGenerator.NpcRacketName),
                Color.white, NpcRacketSortingOrder);
            var npc = npcRacketObj.AddComponent<NpcController>();
            var npcView = npcRacketObj.AddComponent<NpcRacketView>();

            var npcSo = new SerializedObject(npc);
            npcSo.FindProperty("_table").objectReferenceValue = table;
            npcSo.FindProperty("_ball").objectReferenceValue = ball;
            npcSo.FindProperty("_playerRacket").objectReferenceValue = playerRacket;
            npcSo.ApplyModifiedProperties();

            var viewSo = new SerializedObject(npcView);
            viewSo.FindProperty("_table").objectReferenceValue = table;
            viewSo.FindProperty("_npc").objectReferenceValue = npc;
            viewSo.FindProperty("_renderer").objectReferenceValue = npcRacketObj.GetComponent<SpriteRenderer>();
            viewSo.ApplyModifiedProperties();

            return new OpponentParts { Npc = npc, RacketView = npcView };
        }

        /// <summary>ラケットに追従して立つキャラクターを1体作る</summary>
        private static CharacterView CreateCharacter(string name, string spriteName, int sortingOrder, TableLayout table,
            MonoBehaviour actor, float courtZ, float followRatio, float displayHeight, float baseYOffset)
        {
            var obj = CreateSpriteObject(name, LoadSprite(spriteName), Color.white, sortingOrder);
            var view = obj.AddComponent<CharacterView>();

            var so = new SerializedObject(view);
            so.FindProperty("_table").objectReferenceValue = table;
            so.FindProperty("_actorSource").objectReferenceValue = actor;
            so.FindProperty("_renderer").objectReferenceValue = obj.GetComponent<SpriteRenderer>();
            so.FindProperty("_courtZ").floatValue = courtZ;
            so.FindProperty("_followRatio").floatValue = followRatio;
            so.FindProperty("_displayHeight").floatValue = displayHeight;
            so.FindProperty("_baseYOffset").floatValue = baseYOffset;
            so.ApplyModifiedProperties();
            return view;
        }

        // ------------------------------------------------------------------
        // ゲーム中のUI（HUD・PAUSE・必殺技ボタン）
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
            canvasObj.AddComponent<GraphicRaycaster>();
            return canvasObj.transform;
        }

        private static HudParts CreateHud(Transform canvas)
        {
            Text scoreText = CreateText(canvas, "ScoreText", ScoreFontSize,
                TopCenterAnchor, ScorePosition, ScoreSize, ScoreColor);

            HudText messageHud = CreateHudText(canvas, "MessageText", MessageFontSize,
                CenterAnchor, MessagePosition, MessageSize, MessageColor);
            messageHud.gameObject.SetActive(false);

            HudText shotInfoHud = CreateHudText(canvas, "ShotInfoText", ShotInfoFontSize,
                TopCenterAnchor, ShotInfoPosition, ShotInfoSize, ShotInfoColor);

            return new HudParts { Score = scoreText, Message = messageHud, ShotInfo = shotInfoHud };
        }

        private static HudText CreateHudText(Transform parent, string name, int fontSize,
            Vector2 anchor, Vector2 anchoredPosition, Vector2 size, Color color)
        {
            Text text = CreateText(parent, name, fontSize, anchor, anchoredPosition, size, color);
            var hudText = text.gameObject.AddComponent<HudText>();
            SetReference(hudText, "_text", text);
            return hudText;
        }

        private static PauseButton CreatePauseButton(Transform canvas)
        {
            var buttonObj = UIDialogBuilder.CreateButton(canvas, "Btn_Pause", "II", PauseButtonSize, PauseButtonSize,
                PauseButtonColor);
            SetAnchoredRect(buttonObj.GetComponent<RectTransform>(), TopRightAnchor,
                PauseButtonPosition, new Vector2(PauseButtonSize, PauseButtonSize));
            return buttonObj.AddComponent<PauseButton>();
        }

        /// <summary>必殺技のストックがあるときだけ出すボタン。色と文言は実行時に SpecialController が付ける</summary>
        private static GameObject CreateSpecialButton(Transform canvas, string name, float offsetY)
        {
            var buttonObj = UIDialogBuilder.CreateButton(canvas, name, "SP", SpecialButtonWidth, SpecialButtonHeight,
                SpecialButtonColor);
            SetAnchoredRect(buttonObj.GetComponent<RectTransform>(), RightMiddleAnchor,
                new Vector2(SpecialButtonRightMargin, offsetY), new Vector2(SpecialButtonWidth, SpecialButtonHeight));
            buttonObj.GetComponentInChildren<Text>().fontSize = SpecialButtonFontSize;
            buttonObj.SetActive(false);
            return buttonObj;
        }

        // ------------------------------------------------------------------
        // 試合前パネル（難易度・選手とラケット）
        // ------------------------------------------------------------------
        /// <summary>試合開始前に5段階の難易度を選ばせるパネル</summary>
        private static DifficultySelectPanel CreateDifficultySelectPanel(Transform canvas)
        {
            var panelObj = CreatePanelOverlay(canvas, "DifficultySelectPanel");
            var boxObj = CreatePanelBox(panelObj.transform, DifficultyBoxSize);

            var titleObj = UIDialogBuilder.CreateUIObject("TitleText", boxObj.transform);
            SetAnchorStretch(titleObj.GetComponent<RectTransform>(), new Vector2(0f, 0.72f), new Vector2(1f, 1f));
            AddBoldText(titleObj, "難易度を選んでください", PanelTitleFontSize, Color.white);

            var btnAreaObj = UIDialogBuilder.CreateUIObject("ButtonArea", boxObj.transform);
            SetAnchorStretch(btnAreaObj.GetComponent<RectTransform>(), new Vector2(0.04f, 0.15f), new Vector2(0.96f, 0.68f));
            var layout = btnAreaObj.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = ButtonRowSpacing;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;

            var levelButtons = new Button[DifficultyLabels.Length];
            for (int i = 0; i < DifficultyLabels.Length; i++)
            {
                Color color = i == RecommendedDifficultyIndex ? RecommendedDifficultyColor : ChoiceButtonColor;
                var btnObj = UIDialogBuilder.CreateButton(btnAreaObj.transform, $"Btn_Level{i + 1}", DifficultyLabels[i],
                    DifficultyButtonWidth, DifficultyButtonHeight, color);
                btnObj.GetComponentInChildren<Text>().fontSize = DifficultyButtonFontSize;
                levelButtons[i] = btnObj.GetComponent<Button>();
            }

            var panel = panelObj.AddComponent<DifficultySelectPanel>();
            var so = new SerializedObject(panel);
            SetButtonArray(so.FindProperty("_levelButtons"), levelButtons);
            so.ApplyModifiedProperties();

            panelObj.SetActive(false);
            return panel;
        }

        /// <summary>
        /// 選手とラケットを1画面で選ぶパネル。
        /// 相手欄を隠したときに箱が縮むよう、全要素を LayoutElement で並べて ContentSizeFitter で高さを決める。
        /// ボタンの文言は実行時にカタログから付ける
        /// </summary>
        private static LoadoutSelectPanel CreateLoadoutSelectPanel(Transform canvas, LoadoutCatalog catalog)
        {
            var panelObj = CreatePanelOverlay(canvas, "LoadoutSelectPanel");
            // 高さは ContentSizeFitter が中身から決めるので0で作る
            var boxObj = CreatePanelBox(panelObj.transform, new Vector2(LoadoutBoxWidth, 0f));
            AddVerticalLayout(boxObj, new RectOffset(LoadoutBoxPadding, LoadoutBoxPadding, LoadoutBoxPadding, LoadoutBoxPadding));
            boxObj.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            CreateLayoutLabel(boxObj.transform, "TitleText", "選手とラケットを選んでください", PanelTitleFontSize, LoadoutTitleHeight);

            CreateLayoutLabel(boxObj.transform, "PlayerLabel", "あなた", LoadoutSectionFontSize, LoadoutSectionHeight);
            Button[] playerCharacterButtons = CreateChoiceRow(boxObj.transform, "PlayerCharacterRow",
                catalog.Characters.Length, CharacterButtonHeight);
            Button[] playerRacketButtons = CreateChoiceRow(boxObj.transform, "PlayerRacketRow",
                catalog.Rackets.Length, RacketButtonHeight);

            // 相手の選択欄はNPC戦のときだけ出すので、実行時にまとめて隠せるよう1つのグループにする
            var opponentGroup = UIDialogBuilder.CreateUIObject("OpponentGroup", boxObj.transform);
            AddVerticalLayout(opponentGroup, new RectOffset(0, 0, 0, 0));
            CreateLayoutLabel(opponentGroup.transform, "OpponentLabel", "相手（NPC）", LoadoutSectionFontSize, LoadoutSectionHeight);
            Button[] opponentCharacterButtons = CreateChoiceRow(opponentGroup.transform, "OpponentCharacterRow",
                catalog.Characters.Length, CharacterButtonHeight);
            Button[] opponentRacketButtons = CreateChoiceRow(opponentGroup.transform, "OpponentRacketRow",
                catalog.Rackets.Length, RacketButtonHeight);

            var decideObj = UIDialogBuilder.CreateButton(boxObj.transform, "Btn_Decide", "決定",
                DecideButtonWidth, DecideButtonHeight, DecideButtonColor);
            decideObj.GetComponentInChildren<Text>().fontSize = DecideButtonFontSize;
            AddLayoutElement(decideObj, DecideButtonWidth, DecideButtonHeight);

            var panel = panelObj.AddComponent<LoadoutSelectPanel>();
            var so = new SerializedObject(panel);
            so.FindProperty("_catalog").objectReferenceValue = catalog;
            SetButtonArray(so.FindProperty("_playerCharacterRow").FindPropertyRelative("Buttons"), playerCharacterButtons);
            SetButtonArray(so.FindProperty("_playerRacketRow").FindPropertyRelative("Buttons"), playerRacketButtons);
            SetButtonArray(so.FindProperty("_opponentCharacterRow").FindPropertyRelative("Buttons"), opponentCharacterButtons);
            SetButtonArray(so.FindProperty("_opponentRacketRow").FindPropertyRelative("Buttons"), opponentRacketButtons);
            so.FindProperty("_opponentGroup").objectReferenceValue = opponentGroup;
            so.FindProperty("_decideButton").objectReferenceValue = decideObj.GetComponent<Button>();
            so.ApplyModifiedProperties();

            panelObj.SetActive(false);
            return panel;
        }

        /// <summary>画面全体を暗くして、後ろのゲーム画面への入力を遮る</summary>
        private static GameObject CreatePanelOverlay(Transform canvas, string name)
        {
            var panelObj = UIDialogBuilder.CreateUIObject(name, canvas);
            UIDialogBuilder.SetStretchAll(panelObj.GetComponent<RectTransform>());
            panelObj.AddComponent<Image>().color = PanelOverlayColor;
            return panelObj;
        }

        private static GameObject CreatePanelBox(Transform parent, Vector2 size)
        {
            var boxObj = UIDialogBuilder.CreateUIObject("Panel", parent);
            var boxRect = boxObj.GetComponent<RectTransform>();
            boxRect.anchorMin = CenterAnchor;
            boxRect.anchorMax = CenterAnchor;
            boxRect.pivot = CenterAnchor;
            boxRect.sizeDelta = size;
            boxObj.AddComponent<Image>().color = PanelBoxColor;
            return boxObj;
        }

        private static void AddVerticalLayout(GameObject obj, RectOffset padding)
        {
            var layout = obj.AddComponent<VerticalLayoutGroup>();
            layout.padding = padding;
            layout.spacing = LoadoutRowSpacing;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
        }

        private static void CreateLayoutLabel(Transform parent, string name, string content, int fontSize, float height)
        {
            var obj = UIDialogBuilder.CreateUIObject(name, parent);
            Text text = AddBoldText(obj, content, fontSize, Color.white);
            text.raycastTarget = false;
            AddLayoutElement(obj, LoadoutLabelWidth, height);
        }

        /// <summary>選択肢のボタンを横一列に並べる。並び順がカタログの番号になる</summary>
        private static Button[] CreateChoiceRow(Transform parent, string name, int count, float buttonHeight)
        {
            var rowObj = UIDialogBuilder.CreateUIObject(name, parent);
            var layout = rowObj.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = ButtonRowSpacing;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var buttons = new Button[count];
            for (int i = 0; i < count; i++)
            {
                var btnObj = UIDialogBuilder.CreateButton(rowObj.transform, $"Btn_{i}", "", ChoiceButtonWidth, buttonHeight,
                    ChoiceButtonColor);
                btnObj.GetComponentInChildren<Text>().fontSize = ChoiceButtonFontSize;
                AddLayoutElement(btnObj, ChoiceButtonWidth, buttonHeight);
                buttons[i] = btnObj.GetComponent<Button>();
            }

            return buttons;
        }

        private static void AddLayoutElement(GameObject obj, float width, float height)
        {
            var element = obj.AddComponent<LayoutElement>();
            element.preferredWidth = width;
            element.preferredHeight = height;
        }

        private static void SetButtonArray(SerializedProperty arrayProp, Button[] buttons)
        {
            arrayProp.arraySize = buttons.Length;
            for (int i = 0; i < buttons.Length; i++)
            {
                arrayProp.GetArrayElementAtIndex(i).objectReferenceValue = buttons[i];
            }
        }

        // ------------------------------------------------------------------
        // 必殺技・オンライン・GameManager
        // ------------------------------------------------------------------
        /// <summary>台上に現れるキャラと、必殺技の獲得・発動をまとめる SpecialController を作る</summary>
        private static SpecialController CreateSpecial(LoadoutCatalog catalog, TableLayout table, BallParts ball,
            PlayerParts player, NpcController npc, GameObject weakButtonObj, GameObject strongButtonObj)
        {
            TableMascot mascot = CreateTableMascot(catalog, table, ball.Motion);

            var specialObj = new GameObject("Special");
            var special = specialObj.AddComponent<SpecialController>();

            var so = new SerializedObject(special);
            so.FindProperty("_ball").objectReferenceValue = ball.Motion;
            so.FindProperty("_ballRenderer").objectReferenceValue = ball.Renderer;
            so.FindProperty("_playerSwing").objectReferenceValue = player.Swing;
            so.FindProperty("_shotCalculator").objectReferenceValue = player.ShotCalculator;
            so.FindProperty("_playerRacket").objectReferenceValue = player.Racket;
            so.FindProperty("_npc").objectReferenceValue = npc;
            so.FindProperty("_mascot").objectReferenceValue = mascot;
            BindSpecialButton(so.FindProperty("_weakButton"), weakButtonObj);
            BindSpecialButton(so.FindProperty("_strongButton"), strongButtonObj);
            so.ApplyModifiedProperties();

            return special;
        }

        /// <summary>ボールを当てると必殺技を獲得できる台上のキャラ。見た目は選手の正面絵を使い回す</summary>
        private static TableMascot CreateTableMascot(LoadoutCatalog catalog, TableLayout table, BallMotion ball)
        {
            var mascotObj = CreateSpriteObject("TableMascot", null, Color.white, MascotSortingOrder);
            var mascot = mascotObj.AddComponent<TableMascot>();

            var so = new SerializedObject(mascot);
            so.FindProperty("_table").objectReferenceValue = table;
            so.FindProperty("_ball").objectReferenceValue = ball;
            so.FindProperty("_renderer").objectReferenceValue = mascotObj.GetComponent<SpriteRenderer>();
            var spritesProp = so.FindProperty("_sprites");
            spritesProp.arraySize = catalog.Characters.Length;
            for (int i = 0; i < catalog.Characters.Length; i++)
            {
                spritesProp.GetArrayElementAtIndex(i).objectReferenceValue = catalog.Characters[i].FrontSprite;
            }
            so.ApplyModifiedProperties();
            return mascot;
        }

        private static void BindSpecialButton(SerializedProperty property, GameObject buttonObj)
        {
            property.FindPropertyRelative("Button").objectReferenceValue = buttonObj.GetComponent<Button>();
            // ボタンは非表示で生成しているため、非アクティブの子も探す
            property.FindPropertyRelative("Label").objectReferenceValue = buttonObj.GetComponentInChildren<Text>(true);
        }

        /// <summary>
        /// 接続・メッセージ送受信・相手ラケットの表示。
        /// NetworkManager は OnlineSession が実行時に作るので、Scene には置かない
        /// </summary>
        private static OnlineParts CreateOnline(RacketController playerRacket, OpponentParts opponent)
        {
            var onlineObj = new GameObject("Online");
            var session = onlineObj.AddComponent<OnlineSession>();
            var link = onlineObj.AddComponent<OnlineMatchLink>();
            var remote = onlineObj.AddComponent<RemoteOpponent>();

            SetReference(link, "_racket", playerRacket);

            var so = new SerializedObject(remote);
            so.FindProperty("_link").objectReferenceValue = link;
            so.FindProperty("_racketView").objectReferenceValue = opponent.RacketView;
            so.FindProperty("_characterView").objectReferenceValue = opponent.Character;
            so.ApplyModifiedProperties();

            return new OnlineParts { Session = session, Link = link, Remote = remote };
        }

        private static TableTennisGameManager CreateGameManager(BallMotion ball, PlayerParts player, OpponentParts opponent,
            TableTennisAudio tableTennisAudio, HudParts hud, PreMatchPanels panels, LoadoutCatalog loadoutCatalog,
            SpecialController special, OnlineParts online)
        {
            var gameManagerObj = new GameObject("TableTennisGameManager");
            var gameManager = gameManagerObj.AddComponent<TableTennisGameManager>();
            var referee = gameManagerObj.AddComponent<RallyReferee>();
            var serveController = gameManagerObj.AddComponent<ServeController>();
            var loadoutApplier = gameManagerObj.AddComponent<LoadoutApplier>();

            BindLoadoutApplier(loadoutApplier, player, opponent);
            SetReference(referee, "_ball", ball);

            var serveSo = new SerializedObject(serveController);
            serveSo.FindProperty("_ball").objectReferenceValue = ball;
            serveSo.FindProperty("_racket").objectReferenceValue = player.Racket;
            serveSo.FindProperty("_playerSwing").objectReferenceValue = player.Swing;
            serveSo.ApplyModifiedProperties();

            var so = new SerializedObject(gameManager);
            so.FindProperty("_gameTitle").stringValue = GameTitle;
            so.FindProperty("_ball").objectReferenceValue = ball;
            so.FindProperty("_playerSwing").objectReferenceValue = player.Swing;
            so.FindProperty("_referee").objectReferenceValue = referee;
            so.FindProperty("_serve").objectReferenceValue = serveController;
            so.FindProperty("_npc").objectReferenceValue = opponent.Npc;
            so.FindProperty("_audio").objectReferenceValue = tableTennisAudio;
            so.FindProperty("_scoreText").objectReferenceValue = hud.Score;
            so.FindProperty("_messageHud").objectReferenceValue = hud.Message;
            so.FindProperty("_shotInfoHud").objectReferenceValue = hud.ShotInfo;
            so.FindProperty("_difficultyPanel").objectReferenceValue = panels.Difficulty;
            so.FindProperty("_loadoutPanel").objectReferenceValue = panels.Loadout;
            so.FindProperty("_loadoutApplier").objectReferenceValue = loadoutApplier;
            so.FindProperty("_loadoutCatalog").objectReferenceValue = loadoutCatalog;
            so.FindProperty("_special").objectReferenceValue = special;
            so.FindProperty("_modeSelectPanel").objectReferenceValue = panels.ModeSelect;
            so.FindProperty("_onlineSession").objectReferenceValue = online.Session;
            so.FindProperty("_onlineLink").objectReferenceValue = online.Link;
            so.FindProperty("_remoteOpponent").objectReferenceValue = online.Remote;
            so.ApplyModifiedProperties();

            return gameManager;
        }

        private static void BindLoadoutApplier(LoadoutApplier applier, PlayerParts player, OpponentParts opponent)
        {
            var so = new SerializedObject(applier);
            so.FindProperty("_playerRacket").objectReferenceValue = player.Racket;
            so.FindProperty("_playerSwing").objectReferenceValue = player.Swing;
            so.FindProperty("_shotCalculator").objectReferenceValue = player.ShotCalculator;
            so.FindProperty("_playerCharacter").objectReferenceValue = player.Character;
            so.FindProperty("_npc").objectReferenceValue = opponent.Npc;
            so.FindProperty("_opponentRacketView").objectReferenceValue = opponent.RacketView;
            so.FindProperty("_opponentCharacter").objectReferenceValue = opponent.Character;
            so.ApplyModifiedProperties();
        }

        // ------------------------------------------------------------------
        // 汎用ヘルパー
        // ------------------------------------------------------------------
        private static Sprite LoadSprite(string spriteName)
        {
            return TableTennisArtGenerator.Load(spriteName);
        }

        /// <summary>参照を1つだけ持つコンポーネント向け。SerializedObject の定型処理をまとめる</summary>
        private static void SetReference(Object target, string propertyName, Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(propertyName).objectReferenceValue = value;
            so.ApplyModifiedProperties();
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

        /// <summary>HUDの文言は実行時に書き込むので空で作る</summary>
        private static Text CreateText(Transform parent, string name, int fontSize,
            Vector2 anchor, Vector2 anchoredPosition, Vector2 size, Color color)
        {
            var obj = UIDialogBuilder.CreateUIObject(name, parent);
            SetAnchoredRect(obj.GetComponent<RectTransform>(), anchor, anchoredPosition, size);

            Text text = AddBoldText(obj, "", fontSize, color);
            text.raycastTarget = false; // 画面全体がフリック入力領域なので、文字で入力を遮らない
            return text;
        }

        private static Text AddBoldText(GameObject obj, string content, int fontSize, Color color)
        {
            var text = obj.AddComponent<Text>();
            text.text = content;
            text.fontSize = fontSize;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = color;
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

        /// <summary>親の指定範囲いっぱいに広げる（余白なし）</summary>
        private static void SetAnchorStretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
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
