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

            // 1. Camera（台の手前から奥を見る擬似3Dの画角に合わせる）
            var cameraObj = new GameObject("Main Camera");
            var camera = cameraObj.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.07f, 0.08f, 0.11f);
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
            CreateCharacter("NpcCharacter", npcCharacterSprite, NpcCharacterSortingOrder, tableLayout, npc,
                courtZ: 2.1f, followRatio: 0.8f, displayHeight: 1.45f, baseYOffset: 0f);

            // プレイヤーは背中側。台を隠さないよう、画面下の帯にはみ出させる
            CreateCharacter("PlayerCharacter", playerCharacterSprite, PlayerCharacterSortingOrder, tableLayout, racket,
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

            // 10. GameManager
            var gameManagerObj = new GameObject("TableTennisGameManager");
            var gameManager = gameManagerObj.AddComponent<TableTennisGameManager>();
            var referee = gameManagerObj.AddComponent<RallyReferee>();
            var serveController = gameManagerObj.AddComponent<ServeController>();

            var refereeSo = new SerializedObject(referee);
            refereeSo.FindProperty("_ball").objectReferenceValue = ballMotion;
            refereeSo.ApplyModifiedProperties();

            var serveSo = new SerializedObject(serveController);
            serveSo.FindProperty("_ball").objectReferenceValue = ballMotion;
            serveSo.FindProperty("_racket").objectReferenceValue = racket;
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

        private static T CreateManager<T>(string name, Transform parent) where T : Component
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent);
            return obj.AddComponent<T>();
        }

        /// <summary>ラケットに追従して立つキャラクターを1体作る</summary>
        private static void CreateCharacter(string name, Sprite sprite, int sortingOrder, TableLayout table,
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
