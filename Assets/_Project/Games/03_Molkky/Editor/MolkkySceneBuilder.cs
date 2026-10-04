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

namespace MiniGame.Molkky.Editor
{
    /// <summary>
    /// MolkkyScene を自動生成するエディタユーティリティ。
    /// Scene をコードから作ることで、2人開発での Scene コンフリクトを避ける。
    /// </summary>
    public static class MolkkySceneBuilder
    {
        private const string RootDirectory = "Assets/_Project/Games/03_Molkky";
        private const string SceneDirectory = RootDirectory + "/Scenes";
        private const string ScenePath = SceneDirectory + "/MolkkyScene.unity";
        private const string DataDirectory = RootDirectory + "/Data";
        private const string SettingsPath = DataDirectory + "/MolkkyPhysicsSettings.asset";
        private const string NpcWeakPath = DataDirectory + "/MolkkyNpc_Weak.asset";
        private const string NpcNormalPath = DataDirectory + "/MolkkyNpc_Normal.asset";
        private const string NpcStrongPath = DataDirectory + "/MolkkyNpc_Strong.asset";

        private const string SpritesDefaultMaterialPath = "Sprites-Default.mat";

        private const string GameTitle = "2D Molkky";
        private const string OfflineModeLabel = "1台で遊ぶ";

        private const float CameraOrthographicSize = 5f;
        private static readonly Vector3 CameraPosition = new Vector3(0f, 0f, -10f);

        // タイトルと同じく縦画面基準
        private static readonly Vector2 ReferenceResolution = new Vector2(1080, 1920);
        private const float CanvasMatchWidthOrHeight = 0.5f;

        private static readonly Vector2 CenterAnchor = new Vector2(0.5f, 0.5f);
        private static readonly Vector2 TopCenterAnchor = new Vector2(0.5f, 1f);
        private static readonly Vector2 BottomCenterAnchor = new Vector2(0.5f, 0f);
        private static readonly Vector2 TopRightAnchor = new Vector2(1f, 1f);
        private static readonly Vector2 BottomRightAnchor = new Vector2(1f, 0f);

        // 文字の縁取りの太さ。大きい文字ほど太くして芝や背景の上でも読めるようにする
        private static readonly Vector2 ThinOutline = new Vector2(3f, -3f);
        private static readonly Vector2 ThickOutline = new Vector2(4f, -4f);
        private static readonly Vector2 PopupOutline = new Vector2(5f, -5f);

        private static readonly Color ScoreCellColor = new Color(0.2f, 0.2f, 0.25f);
        private static readonly Color PauseButtonColor = new Color(0.15f, 0.17f, 0.22f, 0.8f);
        private static readonly Color ThrowOptionButtonColor = new Color(0.15f, 0.17f, 0.22f, 0.85f);
        private static readonly Color PanelOverlayColor = new Color(0f, 0f, 0f, 0.8f);
        private static readonly Color PanelBoxColor = new Color(0.12f, 0.14f, 0.18f);
        private static readonly Color ChoiceButtonColor = new Color(0.3f, 0.33f, 0.4f);
        private static readonly Color ConfirmButtonColor = new Color(0.2f, 0.7f, 0.35f);
        private static readonly Color BubbleTextColor = new Color(0.1f, 0.1f, 0.12f);

        // PAUSE ボタン
        private const float PauseButtonSize = 110f;
        private static readonly Vector2 PauseButtonPosition = new Vector2(-30f, -30f);

        // 投げ方の切り替えボタン（縦横・低め山なり）。右下に縦に積む
        private const float ThrowOptionButtonWidth = 260f;
        private const float ThrowOptionButtonHeight = 130f;
        private const float ThrowOptionButtonRightMargin = -30f;
        private const float ThrowStyleButtonY = 60f;
        private const float ThrowArcButtonY = 210f;
        private const float PowerShotButtonY = 360f;
        private const int ThrowOptionFontSize = 52;

        // 試合前パネル（人数設定・キャラ選択）の共通寸法
        private const float PanelBoxWidth = 920f;
        private const int PanelPadding = 40;
        private const float PanelRowWidth = 820f;
        private const float RowSpacing = 20f;
        private const int ConfirmButtonFontSize = 56;

        private struct PhysicsParts
        {
            public PinRack PinRack;
            public StickThrower Stick;
        }

        private struct UiParts
        {
            public ScoreBoardView ScoreBoard;
            public ScorePopupView ScorePopup;
            public TurnBannerView TurnBanner;
            public PlayerSetupPanel SetupPanel;
            public CharacterSelectPanel CharacterSelectPanel;
            public VictoryShowView VictoryShow;
            public PauseButton PauseButton;
            public ThrowStyleButton StyleButton;
            public ThrowArcButton ArcButton;
            public PowerShotButton PowerShotButton;
            public ModeSelectPanel ModeSelectPanel;
        }

        private struct OnlineParts
        {
            public OnlineSession Session;
            public MolkkyOnlineLink Link;
        }

        [MenuItem("Tools/MiniGame/Rebuild Molkky", false, 4)]
        public static void RebuildMolkky()
        {
            // EnsureGenerated は既存PNGを使い回すため、絵のコードを直したときも反映されるようメニューからは必ず描き直す
            MolkkyArtGenerator.GenerateAll();
            BuildInternal();
        }

        /// <summary>
        /// 生成順がそのまま Hierarchy の並び順（UIは描画の前後関係）になるため、呼び出し順を入れ替えないこと
        /// </summary>
        private static void BuildInternal()
        {
            EnsureDirectory(SceneDirectory);
            MolkkyArtGenerator.EnsureGenerated();

            UnityEngine.SceneManagement.Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // NewScene(Single) は未使用アセットをアンロードするため、先に読み込んだ ScriptableObject は破棄されて
            // 参照が null で保存されてしまう。必ずシーンを作った後に読み込む。
            MolkkyPhysicsSettings settings = EnsureSettings();
            MolkkyNpcDifficulty[] npcDifficulties = EnsureNpcDifficulties();
            MolkkyCharacterCatalog characterCatalog = MolkkyCharacterGenerator.EnsureGenerated();

            Camera camera = CreateCamera();
            CreateEventSystem();
            UIManager uiManager = CreateManagers();

            DepthProjector projector = CreateGroundAndBackdrop(settings);
            PhysicsParts physics = CreatePhysics(settings);
            ThrowerView throwerView = CreateViews(physics, projector, settings);
            ThrowInput input = CreateThrowInput(settings, projector, camera);

            Transform canvas = CreateCanvas();
            UiParts ui = CreateGameUi(canvas, characterCatalog, input, uiManager);

            OnlineParts online = CreateOnline();
            // 対戦モード選択は試合前に最初に出すので最前面に置く
            ui.ModeSelectPanel = ModeSelectPanelBuilder.Create(canvas, online.Session, OfflineModeLabel);

            MolkkyGameManager gameManager = CreateGameManager(settings, npcDifficulties, characterCatalog,
                physics, input, throwerView, ui, online);
            SetRefs(ui.PauseButton, ("_gameManager", gameManager));

            SaveScene(scene);
        }

        private static void SaveScene(UnityEngine.SceneManagement.Scene scene)
        {
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[MolkkySceneBuilder] MolkkyScene を生成しました: {ScenePath}");

            RegisterSceneInBuildSettings(ScenePath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        // ------------------------------------------------------------------
        // データアセット
        // ------------------------------------------------------------------
        /// <summary>調整用パラメータの ScriptableObject が無ければ初期値で作る（既にあれば調整済みの値を残す）</summary>
        private static MolkkyPhysicsSettings EnsureSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<MolkkyPhysicsSettings>(SettingsPath);
            if (settings != null) return settings;

            EnsureDirectory(DataDirectory);

            settings = ScriptableObject.CreateInstance<MolkkyPhysicsSettings>();
            AssetDatabase.CreateAsset(settings, SettingsPath);
            AssetDatabase.SaveAssets();
            return settings;
        }

        /// <summary>よわい＝大きくブレて常に密集地／ふつう＝中くらい＋ちょうどのピン／つよい＝小さく＋25点戻り回避</summary>
        private static MolkkyNpcDifficulty[] EnsureNpcDifficulties()
        {
            return new[]
            {
                EnsureNpcDifficulty(NpcWeakPath, angleNoise: 9f, speedNoise: 0.3f, aimExactPin: false, avoidOverflow: false),
                EnsureNpcDifficulty(NpcNormalPath, angleNoise: 4f, speedNoise: 0.15f, aimExactPin: true, avoidOverflow: false),
                EnsureNpcDifficulty(NpcStrongPath, angleNoise: 1.5f, speedNoise: 0.06f, aimExactPin: true, avoidOverflow: true),
            };
        }

        /// <summary>NPC難易度が無ければ作る。既にあれば調整済みの値を残す</summary>
        private static MolkkyNpcDifficulty EnsureNpcDifficulty(string path, float angleNoise, float speedNoise,
            bool aimExactPin, bool avoidOverflow)
        {
            var difficulty = AssetDatabase.LoadAssetAtPath<MolkkyNpcDifficulty>(path);
            if (difficulty != null) return difficulty;

            EnsureDirectory(DataDirectory);

            difficulty = ScriptableObject.CreateInstance<MolkkyNpcDifficulty>();
            var so = new SerializedObject(difficulty);
            so.FindProperty("_angleNoise").floatValue = angleNoise;
            so.FindProperty("_speedNoise").floatValue = speedNoise;
            so.FindProperty("_aimExactPin").boolValue = aimExactPin;
            so.FindProperty("_avoidOverflow").boolValue = avoidOverflow;
            so.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.CreateAsset(difficulty, path);
            AssetDatabase.SaveAssets();
            return difficulty;
        }

        // ------------------------------------------------------------------
        // カメラ・EventSystem・共通マネージャー
        // ------------------------------------------------------------------
        private static Camera CreateCamera()
        {
            var cameraObj = new GameObject("Main Camera");
            var camera = cameraObj.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            // 背景スプライトの空の上端と同じ色にして、背景より上が見える縦長端末でも継ぎ目を出さない
            camera.backgroundColor = MolkkyArtGenerator.SkyTop;
            camera.orthographic = true;
            camera.orthographicSize = CameraOrthographicSize;
            cameraObj.transform.position = CameraPosition;
            cameraObj.AddComponent<AudioListener>();
            cameraObj.tag = "MainCamera";

            var fitter = cameraObj.AddComponent<MolkkyCameraFitter>();
            SetRefs(fitter, ("_camera", camera));
            return camera;
        }

        /// <summary>UIのタッチ判定に必須</summary>
        private static void CreateEventSystem()
        {
            var eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<EventSystem>();
            eventSystemObj.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }

        private static UIManager CreateManagers()
        {
            var managersRoot = new GameObject("--- Managers ---");
            CreateChild<SceneLoader>("SceneLoader", managersRoot.transform);
            var audioManager = CreateChild<AudioManager>("AudioManager", managersRoot.transform);
            audioManager.gameObject.AddComponent<ProceduralSe>(); // 正式なSE素材が入るまでの仮音
            var uiManager = CreateChild<UIManager>("UIManager", managersRoot.transform);
            CreateChild<InputManager>("InputManager", managersRoot.transform);
            return uiManager;
        }

        // ------------------------------------------------------------------
        // 地面・物理・見た目・入力
        // ------------------------------------------------------------------
        /// <summary>擬似3D変換と、それを使って描く地面・背景</summary>
        private static DepthProjector CreateGroundAndBackdrop(MolkkyPhysicsSettings settings)
        {
            var projector = new GameObject("DepthProjector").AddComponent<DepthProjector>();

            var groundObj = new GameObject("Ground", typeof(MeshFilter), typeof(MeshRenderer));
            groundObj.GetComponent<MeshRenderer>().sharedMaterial =
                AssetDatabase.GetBuiltinExtraResource<Material>(SpritesDefaultMaterialPath);
            var groundView = groundObj.AddComponent<GroundView>();
            SetRefs(groundView, ("_projector", projector), ("_settings", settings));

            var backdropObj = new GameObject("Backdrop");
            backdropObj.AddComponent<SpriteRenderer>().sprite = MolkkyArtGenerator.Load(MolkkyArtGenerator.BackdropName);
            SetRefs(backdropObj.AddComponent<BackdropView>(), ("_projector", projector));

            return projector;
        }

        /// <summary>ロジック用の物理（地面平面の2D物理。見た目を持たない）</summary>
        private static PhysicsParts CreatePhysics(MolkkyPhysicsSettings settings)
        {
            var physicsRoot = new GameObject("--- Physics (Ground Plane) ---");

            var pinRack = CreateChild<PinRack>("PinRack", physicsRoot.transform);
            SetRefs(pinRack, ("_settings", settings));

            var stickObj = new GameObject("Stick", typeof(Rigidbody2D), typeof(CapsuleCollider2D));
            stickObj.transform.SetParent(physicsRoot.transform);
            var stick = stickObj.AddComponent<StickThrower>();
            SetRefs(stick, ("_settings", settings));

            return new PhysicsParts { PinRack = pinRack, Stick = stick };
        }

        /// <summary>ロジックの地面座標を DepthProjector で擬似3Dに変換して描く見た目</summary>
        private static ThrowerView CreateViews(PhysicsParts physics, DepthProjector projector, MolkkyPhysicsSettings settings)
        {
            var viewRoot = new GameObject("--- View ---");

            var pinRackView = CreateChild<PinRackView>("PinRackView", viewRoot.transform);
            SetRefs(pinRackView, ("_pinRack", physics.PinRack), ("_projector", projector), ("_settings", settings),
                ("_standingSprite", MolkkyArtGenerator.Load(MolkkyArtGenerator.PinStandingName)),
                ("_fallenSprite", MolkkyArtGenerator.Load(MolkkyArtGenerator.PinFallenName)));

            var stickView = CreateChild<StickView>("StickView", viewRoot.transform);
            SetRefs(stickView, ("_stick", physics.Stick), ("_projector", projector), ("_settings", settings),
                ("_stickSprite", MolkkyArtGenerator.Load(MolkkyArtGenerator.StickName)),
                ("_shadowSprite", MolkkyArtGenerator.Load(MolkkyArtGenerator.ShadowName)));

            // 手番のキャラの背中。棒の左右位置に追従する
            var throwerView = CreateChild<ThrowerView>("ThrowerView", viewRoot.transform);
            SetRefs(throwerView, ("_stick", physics.Stick), ("_projector", projector));

            return throwerView;
        }

        private static ThrowInput CreateThrowInput(MolkkyPhysicsSettings settings, DepthProjector projector, Camera camera)
        {
            var input = new GameObject("ThrowInput").AddComponent<ThrowInput>();
            SetRefs(input, ("_settings", settings), ("_projector", projector), ("_camera", camera));
            return input;
        }

        // ------------------------------------------------------------------
        // UI
        // ------------------------------------------------------------------
        private static Transform CreateCanvas()
        {
            var canvasObj = new GameObject("Canvas");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = CanvasMatchWidthOrHeight;
            canvasObj.AddComponent<GraphicRaycaster>();
            return canvasObj.transform;
        }

        /// <summary>対戦モード選択を除くゲーム中・試合前のUIと、共通ダイアログ</summary>
        private static UiParts CreateGameUi(Transform canvas, MolkkyCharacterCatalog characterCatalog, ThrowInput input,
            UIManager uiManager)
        {
            Transform safeArea = CreateSafeArea(canvas);

            var ui = new UiParts();
            ui.ScoreBoard = CreateScoreBoard(safeArea);
            ui.ScorePopup = CreateScorePopup(safeArea);

            ui.TurnBanner = CreateTurnBanner(canvas);
            ui.SetupPanel = CreatePlayerSetupPanel(canvas);
            ui.CharacterSelectPanel = CreateCharacterSelectPanel(canvas, characterCatalog);
            // 結果ダイアログ（BuildDialogs）より先に作り、演出の後に出る結果画面が手前に来るようにする
            ui.VictoryShow = CreateVictoryShow(canvas);

            ui.PauseButton = CreatePauseButton(safeArea);
            ui.StyleButton = CreateThrowOptionButton<ThrowStyleButton>(safeArea, "Btn_ThrowStyle", ThrowStyleButtonY, input);
            // 縦横ボタンの真上に積み、右下の同じ場所で投げ方をまとめて選べるようにする
            ui.ArcButton = CreateThrowOptionButton<ThrowArcButton>(safeArea, "Btn_ThrowArc", ThrowArcButtonY, input);
            ui.PowerShotButton = CreateThrowOptionButton<PowerShotButton>(safeArea, "Btn_PowerShot", PowerShotButtonY, input);

            // 共通ダイアログ（PAUSE / リザルト）は最前面に置くため最後に生成する
            UIDialogBuilder.BuildDialogs(canvas, uiManager);
            return ui;
        }

        /// <summary>スコアやボタンはノッチ・ホームバーを避けるため SafeArea 内に置く（全画面の表示物は Canvas 直下）</summary>
        private static Transform CreateSafeArea(Transform canvas)
        {
            var safeAreaObj = UIDialogBuilder.CreateUIObject("SafeArea", canvas);
            UIDialogBuilder.SetStretchAll(safeAreaObj.GetComponent<RectTransform>());
            safeAreaObj.AddComponent<SafeAreaFitter>();
            return safeAreaObj.transform;
        }

        /// <summary>
        /// 画面上部のスコアボードと「あと○点」。PAUSE ボタンの下に置き、4人分を横に並べても重ならないようにする。
        /// </summary>
        private static ScoreBoardView CreateScoreBoard(Transform safeArea)
        {
            const float boardWidth = 1040f;
            const float boardY = -160f;
            const float cellSpacing = 22f;
            const int remainingFontSize = 44;
            // チーム戦で枠の下に出す「▶ P3 精密型」と重ならないよう、枠から少し離す
            const float remainingY = -410f;

            var rowObj = UIDialogBuilder.CreateUIObject("ScoreBoard", safeArea);
            SetAnchoredRect(rowObj.GetComponent<RectTransform>(), TopCenterAnchor,
                new Vector2(0f, boardY), new Vector2(boardWidth, ScoreCell.Height));
            ConfigureLayout(rowObj.AddComponent<HorizontalLayoutGroup>(), cellSpacing, TextAnchor.UpperCenter);

            int count = PlayerSetupPanel.MaxPlayers;
            var cells = new RectTransform[count];
            var backgrounds = new Image[count];
            var names = new Text[count];
            var scores = new Text[count];
            var misses = new Text[count];
            var throwers = new Text[count];
            for (int i = 0; i < count; i++)
            {
                ScoreCell cell = CreateScoreCell(rowObj.transform, i);
                cells[i] = cell.Rect;
                backgrounds[i] = cell.Background;
                names[i] = cell.Name;
                scores[i] = cell.Score;
                misses[i] = cell.Miss;
                throwers[i] = cell.Thrower;
            }

            Text remainingText = CreateText(safeArea, "RemainingText", remainingFontSize,
                TopCenterAnchor, new Vector2(0f, remainingY), new Vector2(1000f, 80f), Color.white);
            AddOutline(remainingText.gameObject, ThinOutline);

            var view = rowObj.AddComponent<ScoreBoardView>();
            SetArray(view, "_cells", cells);
            SetArray(view, "_cellBackgrounds", backgrounds);
            SetArray(view, "_nameTexts", names);
            SetArray(view, "_scoreTexts", scores);
            SetArray(view, "_missTexts", misses);
            SetArray(view, "_throwerTexts", throwers);
            SetRefs(view, ("_remainingText", remainingText));
            return view;
        }

        private struct ScoreCell
        {
            public const float Width = 230f;
            public const float Height = 190f;

            public RectTransform Rect;
            public Image Background;
            public Text Name;
            public Text Score;
            public Text Miss;
            public Text Thrower;
        }

        /// <summary>1人分の枠。「名前・点数・ミス」を縦に積み、点数を一番大きく見せる</summary>
        private static ScoreCell CreateScoreCell(Transform row, int index)
        {
            const int nameFontSize = 40;
            // 「▶P1 バランス型」のように席番号＋キャラ名が入るので、枠に収まるよう縮める
            const int nameMinFontSize = 22;
            const int scoreFontSize = 80;
            const int missFontSize = 36;
            const int throwerFontSize = 30;
            const int throwerMinFontSize = 20;
            // 表示するのは手番チームの枠だけなので、隣の枠にはみ出す幅にしても重ならない
            const float throwerWidth = 420f;

            var cellObj = UIDialogBuilder.CreateUIObject($"Cell_P{index + 1}", row);
            var cell = new ScoreCell { Rect = cellObj.GetComponent<RectTransform>() };
            cell.Rect.sizeDelta = new Vector2(ScoreCell.Width, ScoreCell.Height);
            cell.Background = cellObj.AddComponent<Image>();
            cell.Background.color = ScoreCellColor;
            cell.Background.raycastTarget = false; // 画面全体が投擲の入力領域なので、UIで入力を遮らない
            AddOutline(cellObj, ThickOutline);

            cell.Name = CreateText(cellObj.transform, "Name", nameFontSize,
                TopCenterAnchor, new Vector2(0f, -8f), new Vector2(ScoreCell.Width, 50f), Color.white);
            FitToOneLine(cell.Name, nameMinFontSize);
            cell.Score = CreateText(cellObj.transform, "Score", scoreFontSize,
                CenterAnchor, new Vector2(0f, -4f), new Vector2(ScoreCell.Width, 90f), Color.white);
            cell.Score.text = "0";
            AddOutline(cell.Score.gameObject, ThinOutline);
            cell.Miss = CreateText(cellObj.transform, "Miss", missFontSize,
                BottomCenterAnchor, new Vector2(0f, 6f), new Vector2(ScoreCell.Width, 44f), Color.white);
            // チーム戦で「今回投げる人」を枠のすぐ下に添える。枠の中は名前・点数・ミスで埋まっているため
            cell.Thrower = CreateText(cellObj.transform, "Thrower", throwerFontSize,
                BottomCenterAnchor, new Vector2(0f, -40f), new Vector2(throwerWidth, 36f), Color.white);
            FitToOneLine(cell.Thrower, throwerMinFontSize);
            AddOutline(cell.Thrower.gameObject, ThinOutline);
            cell.Thrower.gameObject.SetActive(false);
            return cell;
        }

        /// <summary>得点ポップアップ。倒れたピンを隠さないよう、ピンと投擲ラインの間の空いた芝の上に出す</summary>
        private static ScorePopupView CreateScorePopup(Transform safeArea)
        {
            const int fontSize = 110;

            Text text = CreateText(safeArea, "ScorePopup", fontSize,
                CenterAnchor, new Vector2(0f, -80f), new Vector2(1040f, 300f), Color.white);
            AddOutline(text.gameObject, PopupOutline);

            var popup = text.gameObject.AddComponent<ScorePopupView>();
            SetRefs(popup, ("_text", text));
            text.gameObject.SetActive(false);
            return popup;
        }

        private static PauseButton CreatePauseButton(Transform safeArea)
        {
            var buttonObj = UIDialogBuilder.CreateButton(safeArea, "Btn_Pause", "II", PauseButtonSize, PauseButtonSize,
                PauseButtonColor);
            SetAnchor(buttonObj.GetComponent<RectTransform>(), TopRightAnchor, PauseButtonPosition);
            return buttonObj.AddComponent<PauseButton>();
        }

        /// <summary>
        /// 投げ方（縦投げ／横投げ、低め／山なり）の切り替えボタン。
        /// 右下に置き、中央の棒と横ドラッグの邪魔にならないようにする。人間の構え中だけ GameManager が表示する
        /// </summary>
        private static T CreateThrowOptionButton<T>(Transform safeArea, string name, float y, ThrowInput input)
            where T : MonoBehaviour
        {
            var buttonObj = UIDialogBuilder.CreateButton(safeArea, name, "", ThrowOptionButtonWidth, ThrowOptionButtonHeight,
                ThrowOptionButtonColor);
            SetAnchor(buttonObj.GetComponent<RectTransform>(), BottomRightAnchor, new Vector2(ThrowOptionButtonRightMargin, y));
            Text label = buttonObj.GetComponentInChildren<Text>();
            label.fontSize = ThrowOptionFontSize;

            var view = buttonObj.AddComponent<T>();
            SetRefs(view, ("_input", input), ("_button", buttonObj.GetComponent<Button>()), ("_label", label));

            buttonObj.SetActive(false);
            return view;
        }

        /// <summary>「○○の番」の全画面表示。画面全体をボタンにして、どこをタップしても開始できるようにする</summary>
        private static TurnBannerView CreateTurnBanner(Transform canvas)
        {
            const int titleFontSize = 120;
            // 「P1 バランス型 の番」が2行に折り返して下のヒントと重ならないよう、1行に収まる大きさまで縮める
            const int titleMinFontSize = 60;
            const int hintFontSize = 52;

            GameObject bannerObj = CreateTapOverlay(canvas, "TurnBanner", out Image bannerBackground, out Button tapArea);

            Text title = CreateText(bannerObj.transform, "TitleText", titleFontSize,
                CenterAnchor, new Vector2(0f, 80f), new Vector2(1000f, 200f), Color.white);
            AddOutline(title.gameObject, ThickOutline);
            FitToOneLine(title, titleMinFontSize);
            Text hint = CreateText(bannerObj.transform, "HintText", hintFontSize,
                CenterAnchor, new Vector2(0f, -80f), new Vector2(1000f, 90f), Color.white);
            hint.text = "タップで開始";

            var banner = bannerObj.AddComponent<TurnBannerView>();
            SetRefs(banner, ("_titleText", title), ("_hintText", hint), ("_tapArea", tapArea), ("_background", bannerBackground));

            bannerObj.SetActive(false);
            return banner;
        }

        /// <summary>人数・個人戦/チーム戦・各プレイヤーの人間/NPCとチームを選ぶパネル。非表示の行は VerticalLayoutGroup で詰める</summary>
        private static PlayerSetupPanel CreatePlayerSetupPanel(Transform canvas)
        {
            const float boxHeight = 1200f;
            const float boxSpacing = 28f;
            const float rowHeight = 110f;
            const int titleFontSize = 60;
            const int buttonFontSize = 44;
            const float countButtonWidth = 240f;
            const int playerLabelFontSize = 56;
            // オンラインでは「あなた」が入るので、枠に収まるよう縮める下限
            const int playerLabelMinFontSize = 32;
            const float playerLabelWidth = 160f;
            // チーム戦の A / B ボタンを横に並べても行に収まる幅にする
            const float kindButtonWidth = 480f;
            const float teamButtonWidth = 120f;
            const float modeButtonWidth = 480f;

            GameObject panelObj = CreatePanelOverlay(canvas, "PlayerSetupPanel");
            GameObject boxObj = CreatePanelBox(panelObj.transform, boxHeight, boxSpacing);

            Text title = CreateText(boxObj.transform, "TitleText", titleFontSize,
                CenterAnchor, Vector2.zero, new Vector2(PanelRowWidth, 100f), Color.white);
            title.text = "プレイヤー設定";

            Transform countRow = CreateRow(boxObj.transform, "CountRow", PanelRowWidth, rowHeight);
            var countButtons = new Button[PlayerSetupPanel.MaxPlayers - PlayerSetupPanel.MinPlayers + 1];
            for (int i = 0; i < countButtons.Length; i++)
            {
                int count = PlayerSetupPanel.MinPlayers + i;
                countButtons[i] = CreateChoiceButton(countRow, $"Btn_{count}Players", $"{count}人",
                    countButtonWidth, rowHeight, buttonFontSize);
            }

            Transform modeRow = CreateRow(boxObj.transform, "ModeRow", PanelRowWidth, rowHeight);
            Button modeButton = CreateChoiceButton(modeRow, "Btn_Mode", "", modeButtonWidth, rowHeight, buttonFontSize);

            var playerRows = new GameObject[PlayerSetupPanel.MaxPlayers];
            var kindButtons = new Button[PlayerSetupPanel.MaxPlayers];
            var kindTexts = new Text[PlayerSetupPanel.MaxPlayers];
            var teamButtons = new Button[PlayerSetupPanel.MaxPlayers];
            var teamTexts = new Text[PlayerSetupPanel.MaxPlayers];
            var seatLabels = new Text[PlayerSetupPanel.MaxPlayers];
            for (int i = 0; i < PlayerSetupPanel.MaxPlayers; i++)
            {
                Transform row = CreateRow(boxObj.transform, $"Row_P{i + 1}", PanelRowWidth, rowHeight);
                playerRows[i] = row.gameObject;

                Text label = CreateText(row, "Label", playerLabelFontSize, CenterAnchor, Vector2.zero,
                    new Vector2(playerLabelWidth, rowHeight), MolkkyPlayerColors.Get(i));
                label.text = $"P{i + 1}";
                label.resizeTextForBestFit = true;
                label.resizeTextMinSize = playerLabelMinFontSize;
                label.resizeTextMaxSize = playerLabelFontSize;
                seatLabels[i] = label;

                kindButtons[i] = CreateChoiceButton(row, "Btn_Kind", "", kindButtonWidth, rowHeight, buttonFontSize);
                kindTexts[i] = kindButtons[i].GetComponentInChildren<Text>();

                teamButtons[i] = CreateChoiceButton(row, "Btn_Team", "", teamButtonWidth, rowHeight, buttonFontSize);
                teamTexts[i] = teamButtons[i].GetComponentInChildren<Text>();
            }

            Button startButton = CreateConfirmButton(boxObj.transform, "Btn_Start", "試合開始", 560f, 140f);

            var panel = panelObj.AddComponent<PlayerSetupPanel>();
            SetArray(panel, "_countButtons", countButtons);
            SetArray(panel, "_playerRows", playerRows);
            SetArray(panel, "_kindButtons", kindButtons);
            SetArray(panel, "_kindTexts", kindTexts);
            SetArray(panel, "_teamButtons", teamButtons);
            SetArray(panel, "_teamTexts", teamTexts);
            SetArray(panel, "_seatLabels", seatLabels);
            SetRefs(panel, ("_titleText", title), ("_countRow", countRow.gameObject), ("_startButton", startButton), ("_modeRow", modeRow.gameObject), ("_modeButton", modeButton),
                ("_modeText", modeButton.GetComponentInChildren<Text>()));

            panelObj.SetActive(false);
            return panel;
        }

        /// <summary>
        /// 人数設定の後に1人ずつキャラを選ぶパネル。
        /// 立ち絵を大きく中央に置き、左右の ◀ ▶ で切り替える。能力は3行のマス表示で見せる
        /// </summary>
        private static CharacterSelectPanel CreateCharacterSelectPanel(Transform canvas, MolkkyCharacterCatalog catalog)
        {
            const float boxHeight = 1500f;
            const float boxSpacing = 24f;
            const int titleFontSize = 56;
            const int titleMinFontSize = 32;
            const float portraitWidth = 360f;
            const float portraitHeight = 540f;
            const float arrowSize = 150f;
            const int buttonFontSize = 52;
            const int nameFontSize = 64;
            const float buttonRowHeight = 140f;

            GameObject panelObj = CreatePanelOverlay(canvas, "CharacterSelectPanel");
            GameObject boxObj = CreatePanelBox(panelObj.transform, boxHeight, boxSpacing);

            Text title = CreateText(boxObj.transform, "TitleText", titleFontSize,
                CenterAnchor, Vector2.zero, new Vector2(PanelRowWidth, 90f), Color.white);
            FitToOneLine(title, titleMinFontSize);

            Transform portraitRow = CreateRow(boxObj.transform, "PortraitRow", PanelRowWidth, portraitHeight);
            Button prevButton = CreateChoiceButton(portraitRow, "Btn_Prev", "◀", arrowSize, arrowSize, buttonFontSize);
            Image portrait = CreatePortrait(portraitRow);
            portrait.rectTransform.sizeDelta = new Vector2(portraitWidth, portraitHeight);
            Button nextButton = CreateChoiceButton(portraitRow, "Btn_Next", "▶", arrowSize, arrowSize, buttonFontSize);

            Text nameText = CreateText(boxObj.transform, "NameText", nameFontSize,
                CenterAnchor, Vector2.zero, new Vector2(PanelRowWidth, 90f), Color.white);
            AddOutline(nameText.gameObject, ThinOutline);

            StatBarView powerBar = CreateStatRow(boxObj.transform, "Power", "パワー", PanelRowWidth);
            StatBarView controlBar = CreateStatRow(boxObj.transform, "Control", "コントロール", PanelRowWidth);
            StatBarView stickLengthBar = CreateStatRow(boxObj.transform, "StickLength", "棒の長さ", PanelRowWidth);

            Transform buttonRow = CreateRow(boxObj.transform, "ButtonRow", PanelRowWidth, buttonRowHeight);
            Button backButton = CreateChoiceButton(buttonRow, "Btn_Back", "戻る", 280f, buttonRowHeight, buttonFontSize);
            Button confirmButton = CreateConfirmButton(buttonRow, "Btn_Confirm", "決定", 440f, buttonRowHeight);

            var panel = panelObj.AddComponent<CharacterSelectPanel>();
            SetRefs(panel, ("_catalog", catalog), ("_titleText", title), ("_portrait", portrait), ("_nameText", nameText),
                ("_powerBar", powerBar), ("_controlBar", controlBar), ("_stickLengthBar", stickLengthBar),
                ("_prevButton", prevButton), ("_nextButton", nextButton),
                ("_confirmButton", confirmButton), ("_backButton", backButton));

            panelObj.SetActive(false);
            return panel;
        }

        /// <summary>
        /// 勝利演出。画面全体をボタンにしてどこをタップしても飛ばせるようにする。
        /// 上から 吹き出し → 立ち絵 → 名前 の順に縦に並べる
        /// </summary>
        private static VictoryShowView CreateVictoryShow(Transform canvas)
        {
            const int lineFontSize = 72;
            const int lineMinFontSize = 40;
            const int nameFontSize = 80;
            const int nameMinFontSize = 48;

            GameObject showObj = CreateTapOverlay(canvas, "VictoryShow", out Image background, out Button tapArea);

            var bubbleObj = UIDialogBuilder.CreateUIObject("Bubble", showObj.transform);
            var bubbleRect = bubbleObj.GetComponent<RectTransform>();
            SetAnchoredRect(bubbleRect, CenterAnchor, new Vector2(0f, 560f), new Vector2(860f, 200f));
            var bubbleImage = bubbleObj.AddComponent<Image>();
            bubbleImage.color = Color.white;
            bubbleImage.raycastTarget = false;
            Text line = CreateText(bubbleObj.transform, "LineText", lineFontSize,
                CenterAnchor, Vector2.zero, new Vector2(820f, 180f), BubbleTextColor);
            FitToOneLine(line, lineMinFontSize);

            Image portrait = CreatePortrait(showObj.transform);
            RectTransform portraitRect = portrait.rectTransform;
            SetAnchoredRect(portraitRect, CenterAnchor, new Vector2(0f, -40f), new Vector2(500f, 750f));

            Text nameText = CreateText(showObj.transform, "NameText", nameFontSize,
                CenterAnchor, new Vector2(0f, -520f), new Vector2(1000f, 120f), Color.white);
            AddOutline(nameText.gameObject, ThickOutline);
            FitToOneLine(nameText, nameMinFontSize);

            var show = showObj.AddComponent<VictoryShowView>();
            SetRefs(show, ("_background", background), ("_tapArea", tapArea), ("_portraitRect", portraitRect),
                ("_portrait", portrait), ("_nameText", nameText), ("_bubbleRect", bubbleRect), ("_lineText", line));

            showObj.SetActive(false);
            return show;
        }

        /// <summary>能力1行（ラベル＋5マス）</summary>
        private static StatBarView CreateStatRow(Transform parent, string name, string label, float width)
        {
            const float rowHeight = 70f;
            const float cellSize = 60f;
            const int cellCount = 5;
            const int labelFontSize = 44;
            const float labelWidth = 300f;

            Transform row = CreateRow(parent, $"Stat_{name}", width, rowHeight);
            Text labelText = CreateText(row, "Label", labelFontSize, CenterAnchor, Vector2.zero,
                new Vector2(labelWidth, rowHeight), Color.white);
            labelText.alignment = TextAnchor.MiddleLeft;
            labelText.text = label;

            var cells = new Image[cellCount];
            for (int i = 0; i < cellCount; i++)
            {
                var cellObj = UIDialogBuilder.CreateUIObject($"Cell_{i + 1}", row);
                cellObj.GetComponent<RectTransform>().sizeDelta = new Vector2(cellSize, cellSize);
                cells[i] = cellObj.AddComponent<Image>();
                cells[i].raycastTarget = false;
            }

            var bar = row.gameObject.AddComponent<StatBarView>();
            SetArray(bar, "_cells", cells);
            return bar;
        }

        // ------------------------------------------------------------------
        // オンライン・GameManager
        // ------------------------------------------------------------------
        /// <summary>NetworkManager は OnlineSession が実行時に作るので、Scene には置かない</summary>
        private static OnlineParts CreateOnline()
        {
            var onlineObj = new GameObject("Online");
            var session = onlineObj.AddComponent<OnlineSession>();
            var link = onlineObj.AddComponent<MolkkyOnlineLink>();
            return new OnlineParts { Session = session, Link = link };
        }

        private static MolkkyGameManager CreateGameManager(MolkkyPhysicsSettings settings,
            MolkkyNpcDifficulty[] npcDifficulties, MolkkyCharacterCatalog characterCatalog, PhysicsParts physics,
            ThrowInput input, ThrowerView throwerView, UiParts ui, OnlineParts online)
        {
            var gameManagerObj = new GameObject("MolkkyGameManager");
            var gameManager = gameManagerObj.AddComponent<MolkkyGameManager>();
            var molkkyAudio = gameManagerObj.AddComponent<MolkkyAudio>();
            SetRefs(molkkyAudio, ("_settings", settings), ("_pinRack", physics.PinRack), ("_stick", physics.Stick));

            var settleWatcher = gameManagerObj.AddComponent<ThrowSettleWatcher>();
            SetRefs(settleWatcher, ("_settings", settings), ("_pinRack", physics.PinRack), ("_stick", physics.Stick));

            var npcThrower = gameManagerObj.AddComponent<NpcThrower>();
            SetRefs(npcThrower, ("_settings", settings), ("_pinRack", physics.PinRack));
            SetArray(npcThrower, "_difficulties", npcDifficulties);

            var gmSo = new SerializedObject(gameManager);
            gmSo.FindProperty("_gameTitle").stringValue = GameTitle;
            gmSo.ApplyModifiedPropertiesWithoutUndo();
            SetRefs(gameManager, ("_pinRack", physics.PinRack), ("_stick", physics.Stick), ("_input", input),
                ("_npc", npcThrower), ("_settleWatcher", settleWatcher),
                ("_scoreBoard", ui.ScoreBoard), ("_scorePopup", ui.ScorePopup),
                ("_audio", molkkyAudio), ("_turnBanner", ui.TurnBanner), ("_setupPanel", ui.SetupPanel),
                ("_styleButton", ui.StyleButton), ("_arcButton", ui.ArcButton), ("_powerShotButton", ui.PowerShotButton), ("_modeSelectPanel", ui.ModeSelectPanel),
                ("_onlineSession", online.Session), ("_onlineLink", online.Link),
                ("_characterCatalog", characterCatalog), ("_characterSelectPanel", ui.CharacterSelectPanel),
                ("_throwerView", throwerView), ("_victoryShow", ui.VictoryShow));

            return gameManager;
        }

        // ------------------------------------------------------------------
        // UI ヘルパー
        // ------------------------------------------------------------------
        /// <summary>画面全体をボタンにした全画面表示。どこをタップしても先へ進めるようにする</summary>
        private static GameObject CreateTapOverlay(Transform canvas, string name, out Image background, out Button tapArea)
        {
            var obj = UIDialogBuilder.CreateUIObject(name, canvas);
            UIDialogBuilder.SetStretchAll(obj.GetComponent<RectTransform>());
            background = obj.AddComponent<Image>();
            tapArea = obj.AddComponent<Button>();
            tapArea.transition = Selectable.Transition.None;
            return obj;
        }

        /// <summary>画面全体を暗くして、後ろのゲーム画面への入力を遮る</summary>
        private static GameObject CreatePanelOverlay(Transform canvas, string name)
        {
            var panelObj = UIDialogBuilder.CreateUIObject(name, canvas);
            UIDialogBuilder.SetStretchAll(panelObj.GetComponent<RectTransform>());
            panelObj.AddComponent<Image>().color = PanelOverlayColor;
            return panelObj;
        }

        /// <summary>中身を上から縦に並べる中央の箱</summary>
        private static GameObject CreatePanelBox(Transform parent, float height, float spacing)
        {
            var boxObj = UIDialogBuilder.CreateUIObject("Panel", parent);
            var boxRect = boxObj.GetComponent<RectTransform>();
            boxRect.anchorMin = boxRect.anchorMax = boxRect.pivot = CenterAnchor;
            boxRect.sizeDelta = new Vector2(PanelBoxWidth, height);
            boxObj.AddComponent<Image>().color = PanelBoxColor;
            var layout = boxObj.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(PanelPadding, PanelPadding, PanelPadding, PanelPadding);
            ConfigureLayout(layout, spacing, TextAnchor.UpperCenter);
            return boxObj;
        }

        private static Transform CreateRow(Transform parent, string name, float width, float height)
        {
            var rowObj = UIDialogBuilder.CreateUIObject(name, parent);
            rowObj.GetComponent<RectTransform>().sizeDelta = new Vector2(width, height);
            ConfigureLayout(rowObj.AddComponent<HorizontalLayoutGroup>(), RowSpacing, TextAnchor.MiddleCenter);
            return rowObj.transform;
        }

        /// <summary>子の大きさは各自の sizeDelta のまま使い、並べるだけにする</summary>
        private static void ConfigureLayout(HorizontalOrVerticalLayoutGroup layout, float spacing, TextAnchor alignment)
        {
            layout.spacing = spacing;
            layout.childAlignment = alignment;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
        }

        private static Image CreatePortrait(Transform parent)
        {
            var portraitObj = UIDialogBuilder.CreateUIObject("Portrait", parent);
            var portrait = portraitObj.AddComponent<Image>();
            portrait.preserveAspect = true;
            portrait.raycastTarget = false;
            return portrait;
        }

        private static Button CreateChoiceButton(Transform parent, string name, string label, float width, float height, int fontSize)
        {
            var buttonObj = UIDialogBuilder.CreateButton(parent, name, label, width, height, ChoiceButtonColor);
            buttonObj.GetComponentInChildren<Text>().fontSize = fontSize;
            return buttonObj.GetComponent<Button>();
        }

        private static Button CreateConfirmButton(Transform parent, string name, string label, float width, float height)
        {
            var buttonObj = UIDialogBuilder.CreateButton(parent, name, label, width, height, ConfirmButtonColor);
            buttonObj.GetComponentInChildren<Text>().fontSize = ConfirmButtonFontSize;
            return buttonObj.GetComponent<Button>();
        }

        private static Text CreateText(Transform parent, string name, int fontSize,
            Vector2 anchor, Vector2 anchoredPosition, Vector2 size, Color color)
        {
            var obj = UIDialogBuilder.CreateUIObject(name, parent);
            SetAnchoredRect(obj.GetComponent<RectTransform>(), anchor, anchoredPosition, size);

            var text = obj.AddComponent<Text>();
            text.fontSize = fontSize;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = color;
            text.supportRichText = true;
            // 既定の Truncate だと、1行の高さが枠を超えた瞬間にその行ごと描画されなくなる。
            // フォントの行間（ブラウザ版は NotoSansJP で約1.45倍）や解像度の丸めで点数が消えるのを防ぐ
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false; // 画面全体が投擲の入力領域なので、文字で入力を遮らない
            return text;
        }

        /// <summary>
        /// 1行に収まる大きさまで文字を縮める。CreateText は行が消えないよう縦をはみ出し可にしているが、
        /// そのままだと縮小（Best Fit）が効かないので、ここでは枠内に収める設定に戻す
        /// </summary>
        private static void FitToOneLine(Text text, int minSize)
        {
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = minSize;
            text.resizeTextMaxSize = text.fontSize;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
        }

        private static void AddOutline(GameObject obj, Vector2 distance)
        {
            obj.AddComponent<Outline>().effectDistance = distance;
        }

        /// <summary>アンカー・ピボットを同じ点に揃えて、その点からの位置で置く</summary>
        private static void SetAnchor(RectTransform rect, Vector2 anchor, Vector2 anchoredPosition)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = anchoredPosition;
        }

        private static void SetAnchoredRect(RectTransform rect, Vector2 anchor, Vector2 anchoredPosition, Vector2 size)
        {
            SetAnchor(rect, anchor, anchoredPosition);
            rect.sizeDelta = size;
        }

        // ------------------------------------------------------------------
        // 汎用ヘルパー
        // ------------------------------------------------------------------
        private static void EnsureDirectory(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        private static void SetArray(Object target, string name, Object[] values)
        {
            var so = new SerializedObject(target);
            SerializedProperty property = so.FindProperty(name);
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
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

        private static T CreateChild<T>(string name, Transform parent) where T : Component
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
            Debug.Log($"[MolkkySceneBuilder] Build Settings に {scenePath} を登録しました。");
        }
    }
}
