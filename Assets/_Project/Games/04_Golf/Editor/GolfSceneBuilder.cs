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
    /// GolfScene を自動生成するエディタユーティリティ（Phase 10：ビジュアル置き換えまで）。
    /// Scene をコードから作ることで、2人開発での Scene コンフリクトを避ける。
    /// ホールは Scene に置かず、HoleLoader が実行時にカタログから生成する（§9.1）。
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

        // タイトルと同じく縦画面基準
        private static readonly Vector2 ReferenceResolution = new Vector2(1080, 1920);

        // §4.3：ボールの周りの狙う先まで見える広さ（縦10ユニット）
        private const float CameraOrthographicSize = 5f;

        // 地面・カップ（ホールのプレハブ側）→ 狙いの線 → 影 → ボールの順に重ねる
        private const int AimGuideSortingOrder = 8;
        private const int ShadowSortingOrder = 10;
        private const int OtherBallSortingOrder = 15;
        private const int BallSortingOrder = 20;

        private static readonly Color AimLineColor = new Color(1f, 1f, 1f, 0.7f);
        private static readonly Color LandingMarkerColor = new Color(1f, 1f, 1f, 0.45f);

        private const int HudFontSize = 52;
        private const float HudTopMargin = 120f;
        private const float HudHeight = 240f;

        // 下部の操作UIとぶつからないよう、PAUSE ボタンは右上（HUDより上）に小さく置く
        private const float PauseButtonSize = 100f;
        private const int PauseButtonFontSize = 44;
        private const float PauseButtonMargin = 16f;

        // 演出メッセージは画面中央のボールより少し上に出し、ボールの止まった場所を隠さない
        private const float MessageOffsetY = 280f;
        private const float MessageWidth = 1000f;
        private const float MessageHeight = 360f;
        private const int MessageFontSize = 120;

        // 風は PAUSE ボタンと反対の左上（HUDより上）に、矢印＋強さで出す
        private const float WindViewMargin = 16f;
        private const float WindArrowSize = 90f;
        private const float WindLabelWidth = 200f;
        private const int WindArrowFontSize = 72;
        private const int WindLabelFontSize = 40;

        // 下から ◀ クラブ ▶ の行 → ゲージ の順に積む。合計の高さは GolfCameraFollower の _bottomUiRatio に収める
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

        [MenuItem("Tools/MiniGame/Build Golf Scene", false, 5)]
        public static void BuildGolfScene()
        {
            if (!Directory.Exists(SceneDirectory))
            {
                Directory.CreateDirectory(SceneDirectory);
            }

            UnityEngine.SceneManagement.Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // NewScene(Single) は未使用アセットをアンロードするため、ScriptableObject は必ずシーンを作った後に読み込む
            var settings = EnsureAsset<GolfPhysicsSettings>(SettingsPath);
            var terrainSettings = EnsureAsset<GolfTerrainSettings>(TerrainSettingsPath);
            var npcDifficulty = EnsureAsset<GolfNpcDifficulty>(NpcDifficultyPath);
            GolfHoleCatalog catalog = GolfHoleAssetBuilder.EnsureAssets();
            GolfClubData[] clubs = GolfClubAssetBuilder.EnsureClubs();

            Camera camera = CreateCamera();
            CreateEventSystem();
            UIManager uiManager = CreateManagers();
            GolfBall ball = CreateBall(settings, terrainSettings, out BallView ballView);
            HoleLoader holeLoader = CreateHoleLoader(catalog, ball);
            SetRefs(new GameObject("CourseScenery").AddComponent<CourseScenery>(), ("_holeLoader", holeLoader));
            ClubSelector clubSelector = CreateClubSelector(ball, clubs);
            ShotInput input = CreateInput(ball, clubSelector, settings);
            AimGuideView aimGuide = CreateAimGuide(ball, input, clubSelector);
            SetRefs(camera.gameObject.AddComponent<GolfCameraFollower>(), ("_ball", ball), ("_aimGuide", aimGuide));
            var manager = new GameObject("GolfGameManager").AddComponent<GolfGameManager>();
            SetRefs(manager, ("_npcGolfer", CreateNpcGolfer(ball, input, clubSelector, npcDifficulty)),
                ("_clubs", clubSelector), ("_audio", CreateAudio(manager, ball, clubSelector)));
            SetGameTitle(manager);
            CreateOtherBalls(manager);
            CreateCanvas(manager, ball, ballView, input, clubSelector, holeLoader, settings, uiManager);

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

            if (!Directory.Exists(DataDirectory))
            {
                Directory.CreateDirectory(DataDirectory);
            }

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            return asset;
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
            cameraObj.transform.position = new Vector3(0f, 0f, -10f);
            cameraObj.AddComponent<AudioListener>();
            cameraObj.tag = "MainCamera";
            return camera;
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

            var viewRoot = new GameObject("--- View ---");

            var shadowObj = new GameObject("BallShadow");
            shadowObj.transform.SetParent(viewRoot.transform);
            shadowObj.AddComponent<SpriteRenderer>().sortingOrder = ShadowSortingOrder;
            SetRefs(shadowObj.AddComponent<ShadowView>(), ("_ball", ball));

            var ballViewObj = new GameObject("BallView");
            ballViewObj.transform.SetParent(viewRoot.transform);
            ballViewObj.AddComponent<SpriteRenderer>().sortingOrder = BallSortingOrder;

            // プレイヤー色の縁取り。ボール本体のすぐ下に描く
            var ring = new GameObject("Ring").AddComponent<SpriteRenderer>();
            ring.transform.SetParent(ballViewObj.transform, false);
            ring.sortingOrder = BallSortingOrder - 1;

            ballView = ballViewObj.AddComponent<BallView>();
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

            var so = new SerializedObject(selector);
            SerializedProperty clubsProperty = so.FindProperty("_clubs");
            clubsProperty.arraySize = clubs.Length;
            for (int i = 0; i < clubs.Length; i++)
            {
                clubsProperty.GetArrayElementAtIndex(i).objectReferenceValue = clubs[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
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

            var line = new GameObject("Line").AddComponent<SpriteRenderer>();
            line.transform.SetParent(guide.transform);
            line.color = AimLineColor;
            line.sortingOrder = AimGuideSortingOrder;

            var landing = new GameObject("LandingMarker").AddComponent<SpriteRenderer>();
            landing.transform.SetParent(guide.transform);
            landing.color = LandingMarkerColor;
            landing.sortingOrder = AimGuideSortingOrder;

            SetRefs(guide, ("_ball", ball), ("_input", input), ("_clubs", clubSelector), ("_line", line),
                ("_landingMarker", landing));
            return guide;
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

        private static GolfAudio CreateAudio(GolfGameManager manager, GolfBall ball, ClubSelector clubSelector)
        {
            var audio = manager.gameObject.AddComponent<GolfAudio>();
            SetRefs(audio, ("_ball", ball), ("_clubs", clubSelector));
            return audio;
        }

        private static void SetGameTitle(GolfGameManager manager)
        {
            var so = new SerializedObject(manager);
            so.FindProperty("_gameTitle").stringValue = "2D Golf";
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateCanvas(GolfGameManager manager, GolfBall ball, BallView ballView, ShotInput input,
            ClubSelector clubSelector, HoleLoader holeLoader, GolfPhysicsSettings settings, UIManager uiManager)
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

            CreateShotHud(safeAreaObj.transform, manager, ball, input, holeLoader, settings);
            CreateWindView(safeAreaObj.transform, holeLoader);
            CreateShotGauge(safeAreaObj.transform, input);
            CreateAimControls(safeAreaObj.transform, input, clubSelector, settings);
            GolfMessageView message = CreateMessageView(safeAreaObj.transform);

            // 試合進行の全画面UIはショット操作より手前に出す
            GolfSetupPanel setupPanel = GolfMatchUiBuilder.CreateSetupPanel(canvasObj.transform);
            GolfTurnBannerView turnBanner = GolfMatchUiBuilder.CreateTurnBanner(canvasObj.transform);
            ScoreCardView scoreCard = GolfMatchUiBuilder.CreateScoreCard(canvasObj.transform);
            SetRefs(manager, ("_ball", ball), ("_holeLoader", holeLoader), ("_input", input), ("_ballView", ballView),
                ("_setupPanel", setupPanel), ("_turnBanner", turnBanner), ("_scoreCard", scoreCard),
                ("_message", message));

            // 設定画面や「○○の番」の間も PAUSE からタイトルへ戻れるよう、PAUSE ボタンは試合進行のUIより手前に置く
            var topSafeAreaObj = UIDialogBuilder.CreateUIObject("SafeAreaTop", canvasObj.transform);
            UIDialogBuilder.SetStretchAll(topSafeAreaObj.GetComponent<RectTransform>());
            topSafeAreaObj.AddComponent<SafeAreaFitter>();
            CreatePauseButton(topSafeAreaObj.transform, manager);

            // 共通ダイアログ（PAUSE / リザルト）は PAUSE ボタンより手前に出す
            UIDialogBuilder.BuildDialogs(canvasObj.transform, uiManager);

            CreateOnline(canvasObj.transform, manager);
        }

        /// <summary>
        /// オンライン対戦（§14）。NetworkManager は OnlineSession が実行時に作るので、Scene には置かない。
        /// モード選択は試合前に最初に出すので、戻るボタンも含めて一番手前に置く（待機中はキャンセルで戻れる）
        /// </summary>
        private static void CreateOnline(Transform canvas, GolfGameManager manager)
        {
            var onlineObj = new GameObject("Online");
            var session = onlineObj.AddComponent<OnlineSession>();
            var link = onlineObj.AddComponent<GolfOnlineLink>();
            ModeSelectPanel modeSelectPanel = ModeSelectPanelBuilder.Create(canvas, session, "1台で遊ぶ");
            SetRefs(manager, ("_modeSelectPanel", modeSelectPanel), ("_onlineSession", session), ("_onlineLink", link));
        }

        private static void CreateShotGauge(Transform parent, ShotInput input)
        {
            GameObject gaugeObj = UIDialogBuilder.CreateButton(parent, "ShotGauge", string.Empty,
                GaugeWidth, GaugeHeight, GaugeBackgroundColor);
            SetBottomCenter(gaugeObj.GetComponent<RectTransform>(), 0f, GaugeBottomMargin);

            Text label = gaugeObj.GetComponentInChildren<Text>();
            label.fontSize = GaugeLabelFontSize;
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
            rect.pivot = new Vector2(0.5f, 0.5f);
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

            GameObject clubObj = UIDialogBuilder.CreateButton(parent, "Btn_Club", string.Empty,
                ClubButtonWidth, AimControlHeight, ControlButtonColor);
            SetBottomCenter(clubObj.GetComponent<RectTransform>(), 0f, AimControlBottomMargin);
            Text clubLabel = clubObj.GetComponentInChildren<Text>();
            clubLabel.fontSize = ClubButtonFontSize;

            var clubView = clubObj.AddComponent<ClubButtonView>();
            SetRefs(clubView, ("_input", input), ("_clubs", clubSelector), ("_settings", settings),
                ("_button", clubObj.GetComponent<Button>()), ("_label", clubLabel));
        }

        private static HoldButton CreateRotateButton(Transform parent, string name, string label, float offsetX)
        {
            GameObject obj = UIDialogBuilder.CreateButton(parent, name, label, RotateButtonWidth, AimControlHeight,
                ControlButtonColor);
            SetBottomCenter(obj.GetComponent<RectTransform>(), offsetX, AimControlBottomMargin);
            obj.GetComponentInChildren<Text>().fontSize = RotateButtonFontSize;
            return obj.AddComponent<HoldButton>();
        }

        private static void SetBottomCenter(RectTransform rect, float x, float bottomMargin)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(x, bottomMargin);
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

            var hud = obj.AddComponent<GolfHudView>();
            SetRefs(hud, ("_ball", ball), ("_input", input), ("_holeLoader", holeLoader), ("_manager", manager),
                ("_settings", settings), ("_text", text));
        }

        /// <summary>左上に「↑（風の向きへ回す）＋ 風 3m」を並べる</summary>
        private static void CreateWindView(Transform parent, HoleLoader holeLoader)
        {
            GameObject root = UIDialogBuilder.CreateUIObject("WindView", parent);
            var rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = rootRect.anchorMax = rootRect.pivot = new Vector2(0f, 1f);
            rootRect.anchoredPosition = new Vector2(WindViewMargin, -WindViewMargin);
            rootRect.sizeDelta = new Vector2(WindArrowSize + WindLabelWidth, WindArrowSize);

            Text arrow = CreateWindText(root.transform, "Arrow", "↑", WindArrowFontSize, TextAnchor.MiddleCenter);
            var arrowRect = arrow.GetComponent<RectTransform>();
            arrowRect.anchorMin = arrowRect.anchorMax = new Vector2(0f, 0.5f);
            // 中心で回すため、ピボットを真ん中にしてから左端に寄せる
            arrowRect.pivot = new Vector2(0.5f, 0.5f);
            arrowRect.anchoredPosition = new Vector2(WindArrowSize * 0.5f, 0f);
            arrowRect.sizeDelta = new Vector2(WindArrowSize, WindArrowSize);

            Text label = CreateWindText(root.transform, "Strength", string.Empty, WindLabelFontSize, TextAnchor.MiddleLeft);
            var labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = labelRect.anchorMax = labelRect.pivot = new Vector2(0f, 0.5f);
            labelRect.anchoredPosition = new Vector2(WindArrowSize, 0f);
            labelRect.sizeDelta = new Vector2(WindLabelWidth, WindArrowSize);

            SetRefs(root.AddComponent<WindView>(), ("_holeLoader", holeLoader), ("_arrow", arrowRect),
                ("_strengthLabel", label));
        }

        private static Text CreateWindText(Transform parent, string name, string content, int fontSize, TextAnchor alignment)
        {
            GameObject obj = UIDialogBuilder.CreateUIObject(name, parent);
            var text = obj.AddComponent<Text>();
            text.text = content;
            text.fontSize = fontSize;
            text.fontStyle = FontStyle.Bold;
            text.alignment = alignment;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            // 画面のどこを押しても打てるように、表示はタップを奪わない
            text.raycastTarget = false;
            obj.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.6f);
            return text;
        }

        private static void CreatePauseButton(Transform parent, GolfGameManager manager)
        {
            var buttonObj = UIDialogBuilder.CreateButton(parent, "Btn_Pause", "II", PauseButtonSize, PauseButtonSize,
                new Color(0.15f, 0.17f, 0.22f, 0.8f));
            var rect = buttonObj.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-PauseButtonMargin, -PauseButtonMargin);
            buttonObj.GetComponentInChildren<Text>().fontSize = PauseButtonFontSize;

            SetRefs(buttonObj.AddComponent<PauseButton>(), ("_gameManager", manager));
        }

        /// <summary>「バーディー！」「池ポチャ…」などを大きく出す（§13.3）。表示中だけ有効にする</summary>
        private static GolfMessageView CreateMessageView(Transform parent)
        {
            GameObject obj = UIDialogBuilder.CreateUIObject("MessageView", parent);
            var rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, MessageOffsetY);
            rect.sizeDelta = new Vector2(MessageWidth, MessageHeight);

            var text = obj.AddComponent<Text>();
            text.fontSize = MessageFontSize;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            // 演出中もタップを奪わない
            text.raycastTarget = false;
            obj.AddComponent<Outline>().effectDistance = new Vector2(5f, -5f);

            var view = obj.AddComponent<GolfMessageView>();
            SetRefs(view, ("_text", text));
            obj.SetActive(false);
            return view;
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
