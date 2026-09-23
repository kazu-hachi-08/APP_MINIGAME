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
    /// TableTennisScene（Phase 1〜9: 最小プロトタイプ〜UI・演出・ビジュアル置き換え）を自動生成するエディタユーティリティ。
    /// Scene をコードから作ることで、2人開発での Scene コンフリクトを避ける。
    /// </summary>
    public static class TableTennisSceneBuilder
    {
        private const string SceneDirectory = "Assets/_Project/Games/02_TableTennis/Scenes";
        private const string ScenePath = SceneDirectory + "/TableTennisScene.unity";

        private const string SpritesDefaultMaterialPath = "Sprites-Default.mat";

        // 手前にあるものほど大きい値。キャラクターは自分のラケットより後ろに描く
        // TableView側の台パーツが-130〜-50を使っているため、それより奥に置く
        private const int BackgroundSortingOrder = -200;
        private const int NpcCharacterSortingOrder = 3;
        private const int NpcRacketSortingOrder = 5;
        private const int ShadowSortingOrder = 10;
        private const int BounceSortingOrder = 12;
        private const int PlayerCharacterSortingOrder = 15;
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

            Material spritesDefault = AssetDatabase.GetBuiltinExtraResource<Material>(SpritesDefaultMaterialPath);

            // Phase 9: 仮素材（円・矩形）を卓球の絵へ置き換える
            TableTennisArtGenerator.EnsureGenerated();
            Sprite ballSprite = TableTennisArtGenerator.Load(TableTennisArtGenerator.BallName);
            Sprite shadowSprite = TableTennisArtGenerator.Load(TableTennisArtGenerator.ShadowName);
            Sprite playerRacketSprite = TableTennisArtGenerator.Load(TableTennisArtGenerator.PlayerRacketName);
            Sprite npcRacketSprite = TableTennisArtGenerator.Load(TableTennisArtGenerator.NpcRacketName);
            Sprite playerCharacterSprite = TableTennisArtGenerator.Load(TableTennisArtGenerator.PlayerCharacterName);
            Sprite npcCharacterSprite = TableTennisArtGenerator.Load(TableTennisArtGenerator.NpcCharacterName);
            Sprite tableSurfaceSprite = TableTennisArtGenerator.Load(TableTennisArtGenerator.TableSurfaceName);
            Sprite netSprite = TableTennisArtGenerator.Load(TableTennisArtGenerator.NetName);
            Sprite bounceRingSprite = TableTennisArtGenerator.Load(TableTennisArtGenerator.BounceRingName);
            Sprite backgroundSprite = TableTennisArtGenerator.Load(TableTennisArtGenerator.BackgroundName);

            // 1. Camera（台の手前から奥を見る擬似3Dの画角に合わせる）
            var cameraObj = new GameObject("Main Camera");
            var camera = cameraObj.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            // 背景スプライトが常に画面を覆うが、リサイズが追いつくまでの隙間用に壁の色に合わせておく
            camera.backgroundColor = new Color32(30, 34, 46, 255);
            camera.orthographic = true;
            camera.orthographicSize = 4.5f;
            // 画面上部のHUD（スコア・打球結果）と台・キャラクターが重ならないよう、
            // カメラを上げて描画全体をHUDの下へ落とす
            cameraObj.transform.position = new Vector3(0f, 0.6f, -10f);
            cameraObj.AddComponent<AudioListener>();
            cameraObj.tag = "MainCamera";

            // 縦長のスマートフォンでもラケットの可動範囲が切れないようにする
            var cameraFitter = cameraObj.AddComponent<CameraFitter>();
            var fitterSo = new SerializedObject(cameraFitter);
            fitterSo.FindProperty("_camera").objectReferenceValue = camera;
            fitterSo.ApplyModifiedProperties();

            // 1.5 背景（体育館の壁と床。カメラの表示範囲いっぱいに常に引き伸ばす）
            var backgroundObj = CreateSpriteObject("Background", backgroundSprite, Color.white, BackgroundSortingOrder);
            var backgroundView = backgroundObj.AddComponent<BackgroundView>();
            var bgSo = new SerializedObject(backgroundView);
            bgSo.FindProperty("_camera").objectReferenceValue = camera;
            bgSo.ApplyModifiedProperties();

            // 2. EventSystem（UIのタッチ判定に必須）
            var eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<EventSystem>();
            eventSystemObj.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();

            // 3. Managers（共通基盤の再利用）
            var managersRoot = new GameObject("--- Managers ---");
            CreateManager<SceneLoader>("SceneLoader", managersRoot.transform);
            var audioManager = CreateManager<AudioManager>("AudioManager", managersRoot.transform);
            audioManager.gameObject.AddComponent<ProceduralSe>(); // 正式なSE素材が入るまでの仮音

            // 卓球固有のSE（打球・バウンド・ネット）は共通の SeId に持たせず、ここで生成して鳴らす
            var tableTennisAudio = CreateManager<TableTennisAudio>("TableTennisAudio", managersRoot.transform);
            var uiManager = CreateManager<UIManager>("UIManager", managersRoot.transform);
            CreateManager<InputManager>("InputManager", managersRoot.transform);

            // 4. 卓球台（寸法と擬似3D変換の基準）
            var tableObj = new GameObject("Table");
            var tableLayout = tableObj.AddComponent<TableLayout>();
            var tableView = tableObj.AddComponent<TableView>();
            var tvSo = new SerializedObject(tableView);
            tvSo.FindProperty("_table").objectReferenceValue = tableLayout;
            tvSo.FindProperty("_material").objectReferenceValue = spritesDefault;
            tvSo.FindProperty("_surfaceSprite").objectReferenceValue = tableSurfaceSprite;
            tvSo.FindProperty("_netSprite").objectReferenceValue = netSprite;
            tvSo.ApplyModifiedProperties();

            // 5. ボール（影は奥行きを読み取る手がかりになるので別オブジェクトで用意する）
            var shadowObj = CreateSpriteObject("BallShadow", shadowSprite,
                new Color(0f, 0f, 0f, 0.45f), ShadowSortingOrder);

            var ballObj = CreateSpriteObject("Ball", ballSprite, Color.white, BallSortingOrder);
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
            bvSo.FindProperty("_shadowRenderer").objectReferenceValue = shadowObj.GetComponent<SpriteRenderer>();
            bvSo.ApplyModifiedProperties();

            // バウンド位置を輪で見せる（着地点が読み取りやすくなる）
            var bounceObj = CreateSpriteObject("BounceEffect", bounceRingSprite, Color.white, BounceSortingOrder);
            var bounceEffect = bounceObj.AddComponent<BounceEffect>();

            var bounceSo = new SerializedObject(bounceEffect);
            bounceSo.FindProperty("_table").objectReferenceValue = tableLayout;
            bounceSo.FindProperty("_ball").objectReferenceValue = ballMotion;
            bounceSo.FindProperty("_renderer").objectReferenceValue = bounceObj.GetComponent<SpriteRenderer>();
            bounceSo.ApplyModifiedProperties();

            // 6. 入力（タップでラケット移動 / フリックで打球）
            var playerRigObj = new GameObject("PlayerRig");
            var flickInput = playerRigObj.AddComponent<FlickInput>();
            var timingJudge = playerRigObj.AddComponent<SwingTimingJudge>();
            var shotCalculator = playerRigObj.AddComponent<ShotCalculator>();
            var playerSwing = playerRigObj.AddComponent<PlayerSwing>();

            // 7. ラケット
            var racketObj = CreateSpriteObject("Racket", playerRacketSprite, Color.white, RacketSortingOrder);
            var racket = racketObj.AddComponent<RacketController>();
            var racketSwingView = racketObj.AddComponent<RacketSwingView>();

            var rcSo = new SerializedObject(racket);
            rcSo.FindProperty("_table").objectReferenceValue = tableLayout;
            rcSo.FindProperty("_flickInput").objectReferenceValue = flickInput;
            rcSo.FindProperty("_camera").objectReferenceValue = camera;
            rcSo.FindProperty("_renderer").objectReferenceValue = racketObj.GetComponent<SpriteRenderer>();
            rcSo.ApplyModifiedProperties();

            var scSo = new SerializedObject(shotCalculator);
            scSo.FindProperty("_ball").objectReferenceValue = ballMotion;
            scSo.ApplyModifiedProperties();

            var psSo = new SerializedObject(playerSwing);
            psSo.FindProperty("_ball").objectReferenceValue = ballMotion;
            psSo.FindProperty("_racket").objectReferenceValue = racket;
            psSo.FindProperty("_flickInput").objectReferenceValue = flickInput;
            psSo.FindProperty("_timingJudge").objectReferenceValue = timingJudge;
            psSo.FindProperty("_shotCalculator").objectReferenceValue = shotCalculator;
            psSo.ApplyModifiedProperties();

            var swingViewSo = new SerializedObject(racketSwingView);
            swingViewSo.FindProperty("_table").objectReferenceValue = tableLayout;
            swingViewSo.FindProperty("_racket").objectReferenceValue = racket;
            swingViewSo.FindProperty("_playerSwing").objectReferenceValue = playerSwing;
            swingViewSo.ApplyModifiedProperties();

            // 8. NPC（思考と表示を分け、返球内容は NpcController が決める）
            var npcRacketObj = CreateSpriteObject("NpcRacket", npcRacketSprite, Color.white, NpcRacketSortingOrder);
            var npc = npcRacketObj.AddComponent<NpcController>();
            var npcView = npcRacketObj.AddComponent<NpcRacketView>();

            var npcSo = new SerializedObject(npc);
            npcSo.FindProperty("_table").objectReferenceValue = tableLayout;
            npcSo.FindProperty("_ball").objectReferenceValue = ballMotion;
            npcSo.FindProperty("_playerRacket").objectReferenceValue = racket;
            npcSo.ApplyModifiedProperties();

            var npcViewSo = new SerializedObject(npcView);
            npcViewSo.FindProperty("_table").objectReferenceValue = tableLayout;
            npcViewSo.FindProperty("_npc").objectReferenceValue = npc;
            npcViewSo.FindProperty("_renderer").objectReferenceValue = npcRacketObj.GetComponent<SpriteRenderer>();
            npcViewSo.ApplyModifiedProperties();

            // 8-2. キャラクター（ラケットの左右移動に合わせて立たせるだけの表示）
            CharacterView npcCharacter = CreateCharacter("NpcCharacter", npcCharacterSprite, NpcCharacterSortingOrder, tableLayout, npc,
                courtZ: 2.1f, followRatio: 0.8f, displayHeight: 1.45f, baseYOffset: 0f);

            // プレイヤーは背中側。台を隠さないよう、画面下の帯にはみ出させる
            CharacterView playerCharacter = CreateCharacter("PlayerCharacter", playerCharacterSprite, PlayerCharacterSortingOrder, tableLayout, racket,
                courtZ: -1.7f, followRatio: 0.5f, displayHeight: 2.6f, baseYOffset: -2.3f);

            // 9. UI
            var canvasObj = new GameObject("Canvas");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();

            // スコアとサーブ権（Phase 8 で正式なHUDへ整理する）
            // 画面最上部はインカメラのノッチと重なるため、その下まで下げて表示する
            Text scoreText = CreateText(canvasObj.transform, "ScoreText", "", 64,
                new Vector2(0.5f, 1f), new Vector2(0f, -180f), new Vector2(760f, 90f),
                new Color(1f, 1f, 1f));

            Text messageText = CreateText(canvasObj.transform, "MessageText", "", 96,
                new Vector2(0.5f, 0.5f), new Vector2(0f, 120f), new Vector2(900f, 220f),
                new Color(1f, 0.85f, 0.1f));
            HudText messageHud = messageText.gameObject.AddComponent<HudText>();
            messageText.gameObject.SetActive(false);

            // 打球の手応え（タイミング・回転）はスコアのすぐ下に返す。
            // 画面下は自分の操作（ラケット・指）で隠れて読めないため
            Text shotInfoText = CreateText(canvasObj.transform, "ShotInfoText", "", 44,
                new Vector2(0.5f, 1f), new Vector2(0f, -290f), new Vector2(900f, 150f),
                new Color(0.85f, 0.92f, 1f));
            HudText shotInfoHud = shotInfoText.gameObject.AddComponent<HudText>();

            BindHudText(messageHud, messageText);
            BindHudText(shotInfoHud, shotInfoText);

            var pauseButtonObj = UIDialogBuilder.CreateButton(canvasObj.transform, "Btn_Pause", "II", 90, 90,
                new Color(0.15f, 0.17f, 0.22f, 0.8f));
            SetAnchoredRect(pauseButtonObj.GetComponent<RectTransform>(), new Vector2(1f, 1f),
                new Vector2(-80f, -70f), new Vector2(90f, 90f));
            var pauseButton = pauseButtonObj.AddComponent<PauseButton>();

            // 共通ダイアログ（PAUSE / リザルト）は最前面に置くため最後に生成する
            UIDialogBuilder.BuildDialogs(canvasObj.transform, uiManager);

            // 試合開始前の難易度選択（さらに最前面。ダイアログより後に生成する）
            var difficultyPanel = CreateDifficultySelectPanel(canvasObj.transform);

            // 選手・ラケット選択（§25）。データは未生成なら初期値で作る
            LoadoutCatalog loadoutCatalog = TableTennisLoadoutGenerator.EnsureGenerated();
            var loadoutPanel = CreateLoadoutSelectPanel(canvasObj.transform, loadoutCatalog);

            // 9-2. オンライン対戦（接続・メッセージ送受信・相手ラケットの表示）
            // NetworkManager は OnlineSession が実行時に作るので、Scene には置かない
            var onlineObj = new GameObject("Online");
            var onlineSession = onlineObj.AddComponent<OnlineSession>();
            var onlineLink = onlineObj.AddComponent<OnlineMatchLink>();
            var remoteOpponent = onlineObj.AddComponent<RemoteOpponent>();

            var linkSo = new SerializedObject(onlineLink);
            linkSo.FindProperty("_racket").objectReferenceValue = racket;
            linkSo.ApplyModifiedProperties();

            var remoteSo = new SerializedObject(remoteOpponent);
            remoteSo.FindProperty("_link").objectReferenceValue = onlineLink;
            remoteSo.FindProperty("_racketView").objectReferenceValue = npcView;
            remoteSo.FindProperty("_characterView").objectReferenceValue = npcCharacter;
            remoteSo.ApplyModifiedProperties();

            // 対戦モード選択は試合前に最初に出すので最前面に置く
            var modeSelectPanel = ModeSelectPanelBuilder.Create(canvasObj.transform, onlineSession, "NPCと対戦");

            // 10. GameManager
            var gameManagerObj = new GameObject("TableTennisGameManager");
            var gameManager = gameManagerObj.AddComponent<TableTennisGameManager>();
            var referee = gameManagerObj.AddComponent<RallyReferee>();
            var serveController = gameManagerObj.AddComponent<ServeController>();
            var loadoutApplier = gameManagerObj.AddComponent<LoadoutApplier>();

            var applierSo = new SerializedObject(loadoutApplier);
            applierSo.FindProperty("_playerRacket").objectReferenceValue = racket;
            applierSo.FindProperty("_playerSwing").objectReferenceValue = playerSwing;
            applierSo.FindProperty("_shotCalculator").objectReferenceValue = shotCalculator;
            applierSo.FindProperty("_playerCharacter").objectReferenceValue = playerCharacter;
            applierSo.FindProperty("_npc").objectReferenceValue = npc;
            applierSo.FindProperty("_opponentRacketView").objectReferenceValue = npcView;
            applierSo.FindProperty("_opponentCharacter").objectReferenceValue = npcCharacter;
            applierSo.ApplyModifiedProperties();

            var refereeSo = new SerializedObject(referee);
            refereeSo.FindProperty("_ball").objectReferenceValue = ballMotion;
            refereeSo.ApplyModifiedProperties();

            var serveSo = new SerializedObject(serveController);
            serveSo.FindProperty("_ball").objectReferenceValue = ballMotion;
            serveSo.FindProperty("_racket").objectReferenceValue = racket;
            serveSo.FindProperty("_playerSwing").objectReferenceValue = playerSwing;
            serveSo.ApplyModifiedProperties();

            var gmSo = new SerializedObject(gameManager);
            gmSo.FindProperty("_gameTitle").stringValue = "2D Table Tennis";
            gmSo.FindProperty("_ball").objectReferenceValue = ballMotion;
            gmSo.FindProperty("_playerSwing").objectReferenceValue = playerSwing;
            gmSo.FindProperty("_referee").objectReferenceValue = referee;
            gmSo.FindProperty("_serve").objectReferenceValue = serveController;
            gmSo.FindProperty("_npc").objectReferenceValue = npc;
            gmSo.FindProperty("_audio").objectReferenceValue = tableTennisAudio;
            gmSo.FindProperty("_scoreText").objectReferenceValue = scoreText;
            gmSo.FindProperty("_messageHud").objectReferenceValue = messageHud;
            gmSo.FindProperty("_shotInfoHud").objectReferenceValue = shotInfoHud;
            gmSo.FindProperty("_difficultyPanel").objectReferenceValue = difficultyPanel;
            gmSo.FindProperty("_loadoutPanel").objectReferenceValue = loadoutPanel;
            gmSo.FindProperty("_loadoutApplier").objectReferenceValue = loadoutApplier;
            gmSo.FindProperty("_loadoutCatalog").objectReferenceValue = loadoutCatalog;
            gmSo.FindProperty("_modeSelectPanel").objectReferenceValue = modeSelectPanel;
            gmSo.FindProperty("_onlineSession").objectReferenceValue = onlineSession;
            gmSo.FindProperty("_onlineLink").objectReferenceValue = onlineLink;
            gmSo.FindProperty("_remoteOpponent").objectReferenceValue = remoteOpponent;
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

        /// <summary>試合開始前に5段階の難易度を選ばせるパネル</summary>
        private static DifficultySelectPanel CreateDifficultySelectPanel(Transform canvas)
        {
            var panelObj = UIDialogBuilder.CreateUIObject("DifficultySelectPanel", canvas);
            UIDialogBuilder.SetStretchAll(panelObj.GetComponent<RectTransform>());
            var bg = panelObj.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.75f);

            var boxObj = UIDialogBuilder.CreateUIObject("Panel", panelObj.transform);
            var boxRect = boxObj.GetComponent<RectTransform>();
            boxRect.anchorMin = new Vector2(0.5f, 0.5f);
            boxRect.anchorMax = new Vector2(0.5f, 0.5f);
            boxRect.pivot = new Vector2(0.5f, 0.5f);
            boxRect.sizeDelta = new Vector2(860, 360);
            var boxImg = boxObj.AddComponent<Image>();
            boxImg.color = new Color(0.12f, 0.14f, 0.18f);

            var titleObj = UIDialogBuilder.CreateUIObject("TitleText", boxObj.transform);
            var titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 0.72f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;
            var titleText = titleObj.AddComponent<Text>();
            titleText.text = "難易度を選んでください";
            titleText.fontSize = 30;
            titleText.fontStyle = FontStyle.Bold;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.color = Color.white;

            var btnAreaObj = UIDialogBuilder.CreateUIObject("ButtonArea", boxObj.transform);
            var btnAreaRect = btnAreaObj.GetComponent<RectTransform>();
            btnAreaRect.anchorMin = new Vector2(0.04f, 0.15f);
            btnAreaRect.anchorMax = new Vector2(0.96f, 0.68f);
            btnAreaRect.offsetMin = Vector2.zero;
            btnAreaRect.offsetMax = Vector2.zero;
            var layout = btnAreaObj.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 16;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;

            string[] labels = { "Lv.1\nやさしい", "Lv.2", "Lv.3\nふつう", "Lv.4", "Lv.5\nむずかしい" };
            var levelButtons = new Button[labels.Length];
            for (int i = 0; i < labels.Length; i++)
            {
                var btnObj = UIDialogBuilder.CreateButton(btnAreaObj.transform, $"Btn_Level{i + 1}", labels[i], 140, 96,
                    i == 2 ? new Color(0.18f, 0.55f, 0.9f) : new Color(0.3f, 0.33f, 0.4f));
                btnObj.GetComponentInChildren<Text>().fontSize = 20;
                levelButtons[i] = btnObj.GetComponent<Button>();
            }

            var panel = panelObj.AddComponent<DifficultySelectPanel>();
            var so = new SerializedObject(panel);
            var buttonsProp = so.FindProperty("_levelButtons");
            buttonsProp.arraySize = levelButtons.Length;
            for (int i = 0; i < levelButtons.Length; i++)
            {
                buttonsProp.GetArrayElementAtIndex(i).objectReferenceValue = levelButtons[i];
            }
            so.ApplyModifiedProperties();

            panelObj.SetActive(false);
            return panel;
        }

        /// <summary>
        /// 選手とラケットを1画面で選ぶパネル（§25.6）。
        /// 相手欄を隠したときに箱が縮むよう、全要素を LayoutElement で並べて ContentSizeFitter で高さを決める。
        /// ボタンの文言は実行時にカタログから付ける
        /// </summary>
        private static LoadoutSelectPanel CreateLoadoutSelectPanel(Transform canvas, LoadoutCatalog catalog)
        {
            const float boxWidth = 900f;
            const float characterButtonHeight = 100f;
            const float racketButtonHeight = 76f;

            var panelObj = UIDialogBuilder.CreateUIObject("LoadoutSelectPanel", canvas);
            UIDialogBuilder.SetStretchAll(panelObj.GetComponent<RectTransform>());
            panelObj.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.75f);

            var boxObj = UIDialogBuilder.CreateUIObject("Panel", panelObj.transform);
            var boxRect = boxObj.GetComponent<RectTransform>();
            boxRect.anchorMin = new Vector2(0.5f, 0.5f);
            boxRect.anchorMax = new Vector2(0.5f, 0.5f);
            boxRect.pivot = new Vector2(0.5f, 0.5f);
            boxRect.sizeDelta = new Vector2(boxWidth, 0f);
            boxObj.AddComponent<Image>().color = new Color(0.12f, 0.14f, 0.18f);
            AddVerticalLayout(boxObj, new RectOffset(24, 24, 24, 24));
            boxObj.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            CreateLayoutLabel(boxObj.transform, "TitleText", "選手とラケットを選んでください", 30, 56f);

            CreateLayoutLabel(boxObj.transform, "PlayerLabel", "あなた", 24, 40f);
            Button[] playerCharacterButtons = CreateChoiceRow(boxObj.transform, "PlayerCharacterRow",
                catalog.Characters.Length, characterButtonHeight);
            Button[] playerRacketButtons = CreateChoiceRow(boxObj.transform, "PlayerRacketRow",
                catalog.Rackets.Length, racketButtonHeight);

            var opponentGroup = UIDialogBuilder.CreateUIObject("OpponentGroup", boxObj.transform);
            AddVerticalLayout(opponentGroup, new RectOffset(0, 0, 0, 0));
            CreateLayoutLabel(opponentGroup.transform, "OpponentLabel", "相手（NPC）", 24, 40f);
            Button[] opponentCharacterButtons = CreateChoiceRow(opponentGroup.transform, "OpponentCharacterRow",
                catalog.Characters.Length, characterButtonHeight);
            Button[] opponentRacketButtons = CreateChoiceRow(opponentGroup.transform, "OpponentRacketRow",
                catalog.Rackets.Length, racketButtonHeight);

            var decideObj = UIDialogBuilder.CreateButton(boxObj.transform, "Btn_Decide", "決定", 320, 90,
                new Color(0.2f, 0.65f, 0.35f));
            decideObj.GetComponentInChildren<Text>().fontSize = 30;
            AddLayoutElement(decideObj, 320f, 90f);

            var panel = panelObj.AddComponent<LoadoutSelectPanel>();
            var so = new SerializedObject(panel);
            so.FindProperty("_catalog").objectReferenceValue = catalog;
            SetRowButtons(so, "_playerCharacterRow", playerCharacterButtons);
            SetRowButtons(so, "_playerRacketRow", playerRacketButtons);
            SetRowButtons(so, "_opponentCharacterRow", opponentCharacterButtons);
            SetRowButtons(so, "_opponentRacketRow", opponentRacketButtons);
            so.FindProperty("_opponentGroup").objectReferenceValue = opponentGroup;
            so.FindProperty("_decideButton").objectReferenceValue = decideObj.GetComponent<Button>();
            so.ApplyModifiedProperties();

            panelObj.SetActive(false);
            return panel;
        }

        private static void AddVerticalLayout(GameObject obj, RectOffset padding)
        {
            var layout = obj.AddComponent<VerticalLayoutGroup>();
            layout.padding = padding;
            layout.spacing = 12;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
        }

        private static void CreateLayoutLabel(Transform parent, string name, string content, int fontSize, float height)
        {
            var obj = UIDialogBuilder.CreateUIObject(name, parent);
            var text = obj.AddComponent<Text>();
            text.text = content;
            text.fontSize = fontSize;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;
            AddLayoutElement(obj, 820f, height);
        }

        /// <summary>選択肢のボタンを横一列に並べる。並び順がカタログの番号になる</summary>
        private static Button[] CreateChoiceRow(Transform parent, string name, int count, float buttonHeight)
        {
            const float buttonWidth = 260f;

            var rowObj = UIDialogBuilder.CreateUIObject(name, parent);
            var layout = rowObj.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 16;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var buttons = new Button[count];
            for (int i = 0; i < count; i++)
            {
                var btnObj = UIDialogBuilder.CreateButton(rowObj.transform, $"Btn_{i}", "", buttonWidth, buttonHeight,
                    new Color(0.3f, 0.33f, 0.4f));
                btnObj.GetComponentInChildren<Text>().fontSize = 24;
                AddLayoutElement(btnObj, buttonWidth, buttonHeight);
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

        private static void SetRowButtons(SerializedObject so, string rowName, Button[] buttons)
        {
            var buttonsProp = so.FindProperty(rowName).FindPropertyRelative("Buttons");
            buttonsProp.arraySize = buttons.Length;
            for (int i = 0; i < buttons.Length; i++)
            {
                buttonsProp.GetArrayElementAtIndex(i).objectReferenceValue = buttons[i];
            }
        }

        private static T CreateManager<T>(string name, Transform parent) where T : Component
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent);
            return obj.AddComponent<T>();
        }

        /// <summary>ラケットに追従して立つキャラクターを1体作る</summary>
        private static CharacterView CreateCharacter(string name, Sprite sprite, int sortingOrder, TableLayout table,
            MonoBehaviour actor, float courtZ, float followRatio, float displayHeight, float baseYOffset)
        {
            var obj = CreateSpriteObject(name, sprite, Color.white, sortingOrder);
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

        private static void BindHudText(HudText hudText, Text text)
        {
            var so = new SerializedObject(hudText);
            so.FindProperty("_text").objectReferenceValue = text;
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
