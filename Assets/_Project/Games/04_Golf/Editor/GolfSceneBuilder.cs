using System.IO;
using MiniGame.Common.Audio;
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
    /// GolfScene を自動生成するエディタユーティリティ（Phase 4：池・OB・風のあるホールを3タップゲージとクラブで打つ）。
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

        // タイトルと同じく縦画面基準
        private static readonly Vector2 ReferenceResolution = new Vector2(1080, 1920);

        // §4.3：ボールの周りの狙う先まで見える広さ（縦10ユニット）
        private const float CameraOrthographicSize = 5f;

        // §5 の OB（コースの外側）の灰色。Tilemap の外側がこの色で見える
        private static readonly Color OutOfBoundsColor = new Color(0.55f, 0.57f, 0.55f);

        // 地面・カップ（ホールのプレハブ側）→ 狙いの線 → 影 → ボールの順に重ねる
        private const int AimGuideSortingOrder = 8;
        private const int ShadowSortingOrder = 10;
        private const int BallSortingOrder = 20;

        private static readonly Color AimLineColor = new Color(1f, 1f, 1f, 0.7f);
        private static readonly Color LandingMarkerColor = new Color(1f, 1f, 1f, 0.45f);

        private const int HudFontSize = 52;
        private const float HudTopMargin = 120f;
        private const float HudHeight = 240f;

        // 下部の操作UIとぶつからないよう、戻るボタンは右上（HUDより上）に小さく置く
        private const float ReturnButtonWidth = 280f;
        private const float ReturnButtonHeight = 90f;
        private const int ReturnButtonFontSize = 34;
        private const float ReturnButtonMargin = 16f;

        // 風は戻るボタンと反対の左上（HUDより上）に、矢印＋強さで出す
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
            GolfHoleCatalog catalog = GolfHoleAssetBuilder.EnsureAssets();
            GolfClubData[] clubs = GolfClubAssetBuilder.EnsureClubs();

            Camera camera = CreateCamera();
            CreateEventSystem();
            CreateManagers();
            GolfBall ball = CreateBall(settings, terrainSettings);
            HoleLoader holeLoader = CreateHoleLoader(catalog, ball);
            ClubSelector clubSelector = CreateClubSelector(ball, clubs);
            ShotInput input = CreateInput(ball, clubSelector, settings);
            AimGuideView aimGuide = CreateAimGuide(ball, input, clubSelector);
            SetRefs(camera.gameObject.AddComponent<GolfCameraFollower>(), ("_ball", ball), ("_aimGuide", aimGuide));
            CreateCanvas(ball, input, clubSelector, holeLoader, settings);

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
            camera.backgroundColor = OutOfBoundsColor;
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
        private static GolfBall CreateBall(GolfPhysicsSettings settings, GolfTerrainSettings terrainSettings)
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
            SetRefs(ballViewObj.AddComponent<BallView>(), ("_ball", ball));

            return ball;
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

        private static void CreateManagers()
        {
            // タイトルへ戻るときのフェード遷移に SceneLoader を使う
            var managersRoot = new GameObject("--- Managers ---");
            CreateManager<SceneLoader>("SceneLoader", managersRoot.transform);
            CreateManager<AudioManager>("AudioManager", managersRoot.transform);
        }

        private static void CreateCanvas(GolfBall ball, ShotInput input, ClubSelector clubSelector,
            HoleLoader holeLoader, GolfPhysicsSettings settings)
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

            CreateShotHud(safeAreaObj.transform, ball, input, holeLoader, settings);
            CreateWindView(safeAreaObj.transform, holeLoader);
            CreateShotGauge(safeAreaObj.transform, input);
            CreateAimControls(safeAreaObj.transform, input, clubSelector, settings);
            CreateReturnButton(safeAreaObj.transform);
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

        private static void CreateShotHud(Transform parent, GolfBall ball, ShotInput input,
            HoleLoader holeLoader, GolfPhysicsSettings settings)
        {
            var obj = UIDialogBuilder.CreateUIObject("PrototypeShotHud", parent);
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

            var hud = obj.AddComponent<PrototypeShotHud>();
            SetRefs(hud, ("_ball", ball), ("_input", input), ("_holeLoader", holeLoader), ("_settings", settings),
                ("_text", text));
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

        private static void CreateReturnButton(Transform parent)
        {
            var buttonObj = UIDialogBuilder.CreateButton(parent, "Btn_ReturnToTitle", "タイトルへ戻る",
                ReturnButtonWidth, ReturnButtonHeight, new Color(0.15f, 0.17f, 0.22f, 0.9f));
            var rect = buttonObj.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-ReturnButtonMargin, -ReturnButtonMargin);
            buttonObj.GetComponentInChildren<Text>().fontSize = ReturnButtonFontSize;

            var returnButton = buttonObj.AddComponent<GolfTitleReturnButton>();
            SetRefs(returnButton, ("_button", buttonObj.GetComponent<Button>()));
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
