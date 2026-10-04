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

namespace MiniGame.PenguinWars.Editor
{
    /// <summary>
    /// PenguinWarsScene を自動生成するエディタユーティリティ。
    /// Scene をコードから作ることで、2人開発での Scene コンフリクトを避ける。
    /// 1ファイルが巨大にならないよう partial で分割している（.Field = 戦場、.Battle = ユニット・戦闘、.Hud = 上部UI、.Controls = 下部の操作UI、.Intro = 編成発表、.Draft = ドラフト・編成確認、.Effects = 演出・音、.Online = オンライン対戦・モード選択）
    /// </summary>
    public static partial class PenguinWarsSceneBuilder
    {
        private const string RootDirectory = "Assets/_Project/Games/06_PenguinWars";
        private const string SceneDirectory = RootDirectory + "/Scenes";
        private const string ScenePath = SceneDirectory + "/PenguinWarsScene.unity";
        private const string DataDirectory = RootDirectory + "/Data";
        private const string BalancePath = DataDirectory + "/PenguinWarsBalance.asset";

        private const string GameTitle = "Penguin Wars";
        private const string LogPrefix = "[PenguinWarsSceneBuilder]";

        [MenuItem("Tools/MiniGame/Rebuild PenguinWars", false, 7)]
        public static void RebuildPenguinWars()
        {
            BuildInternal();
        }

        /// <summary>生成順がそのまま Hierarchy の並び順（UIは描画の前後関係）になるため、呼び出し順を入れ替えないこと</summary>
        private static void BuildInternal()
        {
            EnsureDirectory(SceneDirectory);
            // シーンを先に作る。Single で作るとどこからも参照されていないアセットが解放され、
            // 先に読み込んだアセットがシーンに空の参照で保存されてしまうため
            UnityEngine.SceneManagement.Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            PenguinWarsBalance balance = EnsureBalance();
            PenguinUnitCatalog catalog = PenguinUnitAssetGenerator.EnsureAssets();
            PenguinArtGenerator.EnsureGenerated(catalog);

            BattleCamera battleCamera = CreateCamera();
            CreateEventSystem();
            UIManager uiManager = CreateManagers();

            (CastleView leftCastle, CastleView rightCastle) = CreateField(balance.FieldLength);
            BattleRunner battleRunner = CreateBattle(balance, catalog, leftCastle, rightCastle);

            Transform canvas = CreateCanvas();
            Transform safeArea = CreateSafeArea(canvas);
            BattleHud hud = CreateHud(safeArea, canvas);
            (UnitButtonBar buttonBar, WalletButton walletButton, CannonButton cannonButton) = CreateControls(safeArea, battleRunner, catalog);
            SetRefs(battleRunner.GetComponent<KeyboardCommandInput>(), ("_buttonBar", buttonBar), ("_walletButton", walletButton),
                ("_cannonButton", cannonButton));
            PauseButton pauseButton = CreatePauseButton(safeArea);
            // 操作UIの上に被せて、発表中は押せないようにする
            DeckIntroPanel deckIntroPanel = CreateDeckIntroPanel(canvas, catalog);
            DraftPanel draftPanel = CreateDraftPanel(canvas, catalog);
            DeckRevealPanel deckRevealPanel = CreateDeckRevealPanel(canvas, catalog);
            // 編成発表より手前・ダイアログ（ポーズ・リザルト）より奥
            OnlineParts online = CreateOnline(canvas, battleRunner);
            UIDialogBuilder.BuildDialogs(canvas, uiManager);
            (BattleEventPresenter presenter, PenguinWarsAudio audio) =
                CreateEffects(balance, battleRunner, battleCamera, hud, leftCastle, rightCastle);

            var gameManager = new GameObject("PenguinWarsGameManager").AddComponent<PenguinWarsGameManager>();
            var so = new SerializedObject(gameManager);
            so.FindProperty("_gameTitle").stringValue = GameTitle;
            so.ApplyModifiedPropertiesWithoutUndo();
            SetRefs(gameManager, ("_balance", balance), ("_battleCamera", battleCamera), ("_hud", hud),
                ("_battleRunner", battleRunner), ("_deckIntroPanel", deckIntroPanel), ("_presenter", presenter), ("_audio", audio));
            SetRefs(gameManager, ("_modeSelectPanel", online.ModeSelectPanel), ("_onlineSession", online.Session),
                ("_onlineLink", online.Link), ("_draftPanel", draftPanel), ("_deckRevealPanel", deckRevealPanel));
            SetRefs(pauseButton, ("_gameManager", gameManager));

            SaveScene(scene);
        }

        /// <summary>遊びながら調整した数値を消さないよう、既にあれば作り直さない</summary>
        private static PenguinWarsBalance EnsureBalance()
        {
            var balance = AssetDatabase.LoadAssetAtPath<PenguinWarsBalance>(BalancePath);
            if (balance != null) return balance;

            EnsureDirectory(DataDirectory);
            balance = ScriptableObject.CreateInstance<PenguinWarsBalance>();
            AssetDatabase.CreateAsset(balance, BalancePath);
            AssetDatabase.SaveAssets();
            return balance;
        }

        private static void SaveScene(UnityEngine.SceneManagement.Scene scene)
        {
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"{LogPrefix} PenguinWarsScene を生成しました: {ScenePath}");

            RegisterSceneInBuildSettings(ScenePath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        // ------------------------------------------------------------------
        // EventSystem・共通マネージャー
        // ------------------------------------------------------------------

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
        // 汎用ヘルパー
        // ------------------------------------------------------------------
        private static void EnsureDirectory(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
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
            obj.transform.SetParent(parent, false);
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
            Debug.Log($"{LogPrefix} Build Settings に {scenePath} を登録しました。");
        }
    }
}
