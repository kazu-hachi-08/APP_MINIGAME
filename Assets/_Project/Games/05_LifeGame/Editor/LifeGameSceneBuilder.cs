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

namespace MiniGame.LifeGame.Editor
{
    /// <summary>
    /// LifeGameScene を自動生成するエディタユーティリティ。
    /// Scene をコードから作ることで、2人開発での Scene コンフリクトを避ける。
    /// 盤面・コマは試合ごとにシードで変わるので Scene には置かず、実行時に BoardView / LifeGameManager が作る。
    /// </summary>
    public static class LifeGameSceneBuilder
    {
        private const string RootDirectory = "Assets/_Project/Games/05_LifeGame";
        private const string SceneDirectory = RootDirectory + "/Scenes";
        private const string ScenePath = SceneDirectory + "/LifeGameScene.unity";

        private const string GameTitle = "2D Life Game";

        private const float CameraOrthographicSize = 6f;
        // テーマ選択の後ろに見える色。選んだら LifeGameManager がテーマの背景色に変える
        private static readonly Color CameraBackground = new Color(0.2f, 0.22f, 0.28f);

        // タイトルと同じく縦画面基準
        private static readonly Vector2 ReferenceResolution = new Vector2(1080, 1920);
        private const float CanvasMatchWidthOrHeight = 0.5f;

        private static readonly Vector2 CenterAnchor = new Vector2(0.5f, 0.5f);
        private static readonly Vector2 TopLeftAnchor = new Vector2(0f, 1f);
        private static readonly Vector2 TopRightAnchor = new Vector2(1f, 1f);
        private static readonly Vector2 BottomLeftAnchor = new Vector2(0f, 0f);
        private static readonly Vector2 BottomCenterAnchor = new Vector2(0.5f, 0f);

        private static readonly Vector2 ThinOutline = new Vector2(3f, -3f);

        private static readonly Color HudButtonColor = new Color(0.15f, 0.17f, 0.22f, 0.85f);
        private static readonly Color PanelOverlayColor = new Color(0f, 0f, 0f, 0.7f);
        private static readonly Color PanelBoxColor = new Color(0.12f, 0.14f, 0.18f);
        private static readonly Color ChoiceButtonColor = new Color(0.3f, 0.33f, 0.4f);
        private static readonly Color ConfirmButtonColor = new Color(0.2f, 0.7f, 0.35f);

        // PAUSE（右上）と「全体」（その下）
        private const float PauseButtonSize = 110f;
        private static readonly Vector2 PauseButtonPosition = new Vector2(-30f, -30f);
        private static readonly Vector2 OverviewButtonSize = new Vector2(160f, 100f);
        private static readonly Vector2 OverviewButtonPosition = new Vector2(-30f, -160f);

        // 所持金バー。PAUSE の左に4人ぶん並べる
        private const int MoneyBarSeats = 4;
        private static readonly Vector2 MoneyCellSize = new Vector2(215f, 110f);
        private const float MoneyCellSpacing = 8f;
        private static readonly Vector2 MoneyBarPosition = new Vector2(20f, -30f);
        private const int MoneyFontSize = 36;

        // ルーレット（下部中央）。フリックの判定はルーレットより広い下部の帯で取り、指が少し外れても回せるようにする
        private const float RouletteAreaHeight = 700f;
        private const float WheelSize = 440f;
        private const float WheelY = 90f;
        private const int WheelLabelFontSize = 56;
        private const int PointerFontSize = 72;
        private const int RouletteHintFontSize = 44;

        // 財布ボタン（左下）
        private static readonly Vector2 WalletButtonSize = new Vector2(220f, 130f);
        private static readonly Vector2 WalletButtonPosition = new Vector2(30f, 60f);

        // パネル共通
        private const float PanelBoxWidth = 920f;
        private const float PanelInnerWidth = 820f;
        private const int PanelPadding = 40;
        private const int PanelBodyFontSize = 44;
        private const int PanelBodyMinFontSize = 26;
        private const int ButtonFontSize = 44;

        [MenuItem("Tools/MiniGame/Rebuild LifeGame", false, 6)]
        public static void RebuildLifeGame()
        {
            BuildInternal();
        }

        /// <summary>生成順がそのまま Hierarchy の並び順（UIは描画の前後関係）になるため、呼び出し順を入れ替えないこと</summary>
        private static void BuildInternal()
        {
            EnsureDirectory(SceneDirectory);
            LifeCharacterCatalog characterCatalog = LifeDataGenerator.EnsureCharacters();
            LifeThemeData[] themes = LifeDataGenerator.EnsureThemes();
            LifeBoardLayout boardLayout = LifeBoardLayoutGenerator.EnsureLayout();
            UnityEngine.SceneManagement.Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            Camera camera = CreateCamera();
            CreateEventSystem();
            UIManager uiManager = CreateManagers();

            var boardView = new GameObject("Board").AddComponent<BoardView>();
            Transform carRoot = new GameObject("Cars").transform;

            Transform canvas = CreateCanvas();
            Transform safeArea = CreateSafeArea(canvas);

            MoneyBarView moneyBar = CreateMoneyBar(safeArea);
            (RouletteView rouletteView, RouletteInput rouletteInput) = CreateRoulette(safeArea);
            Button walletButton = CreateHudButton(safeArea, "Btn_Wallet", "財布", WalletButtonSize, BottomLeftAnchor, WalletButtonPosition);
            Button overviewButton = CreateHudButton(safeArea, "Btn_Overview", "全体", OverviewButtonSize, TopRightAnchor, OverviewButtonPosition);
            PauseButton pauseButton = CreatePauseButton(safeArea);

            // 選択・財布・イベント表示はゲーム画面より手前、共通ダイアログ（PAUSE / リザルト）より奥に置く
            WalletPanel walletPanel = CreateWalletPanel(canvas);
            ChoicePanel choicePanel = CreateChoicePanel(canvas);
            EventPopupView eventPopup = CreateEventPopup(canvas, out Text popupBody);
            TurnBannerView turnBanner = CreateTurnBanner(canvas);
            ThemeSelectPanel themeSelectPanel = CreateThemeSelectPanel(canvas, themes.Length);
            PlayerSetupPanel setupPanel = CreatePlayerSetupPanel(canvas);
            CharacterSelectPanel characterSelectPanel = CreateCharacterSelectPanel(canvas, characterCatalog);
            UIDialogBuilder.BuildDialogs(canvas, uiManager);

            // マスの文字はイベント表示と同じフォントを使う（ブラウザ版で日本語フォントに差し替わった後のものを借りるため）
            SetRefs(boardView, ("_fontSource", popupBody), ("_layout", boardLayout));

            var boardCamera = camera.gameObject.AddComponent<BoardCamera>();
            SetRefs(boardCamera, ("_camera", camera));

            var gameManager = new GameObject("LifeGameManager").AddComponent<LifeGameManager>();
            var so = new SerializedObject(gameManager);
            so.FindProperty("_gameTitle").stringValue = GameTitle;
            so.ApplyModifiedPropertiesWithoutUndo();
            SetArray(gameManager, "_themes", themes);
            SetRefs(gameManager,
                ("_boardView", boardView), ("_boardCamera", boardCamera), ("_carRoot", carRoot),
                ("_rouletteView", rouletteView), ("_rouletteInput", rouletteInput), ("_moneyBar", moneyBar),
                ("_eventPopup", eventPopup), ("_choicePanel", choicePanel), ("_walletPanel", walletPanel),
                ("_walletButton", walletButton), ("_overviewButton", overviewButton),
                ("_characterCatalog", characterCatalog), ("_themeSelectPanel", themeSelectPanel), ("_setupPanel", setupPanel),
                ("_characterSelectPanel", characterSelectPanel), ("_turnBanner", turnBanner));
            SetRefs(pauseButton, ("_gameManager", gameManager));

            SaveScene(scene);
        }

        private static void SaveScene(UnityEngine.SceneManagement.Scene scene)
        {
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[LifeGameSceneBuilder] LifeGameScene を生成しました: {ScenePath}");

            RegisterSceneInBuildSettings(ScenePath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        // ------------------------------------------------------------------
        // カメラ・EventSystem・共通マネージャー
        // ------------------------------------------------------------------
        private static Camera CreateCamera()
        {
            var cameraObj = new GameObject("Main Camera");
            var camera = cameraObj.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = CameraBackground;
            camera.orthographic = true;
            camera.orthographicSize = CameraOrthographicSize;
            cameraObj.transform.position = new Vector3(0f, 0f, -10f);
            cameraObj.AddComponent<AudioListener>();
            cameraObj.tag = "MainCamera";
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
        // HUD
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

        /// <summary>ボタンや所持金はノッチ・ホームバーを避けるため SafeArea 内に置く（全画面のパネルは Canvas 直下）</summary>
        private static Transform CreateSafeArea(Transform canvas)
        {
            var safeAreaObj = UIDialogBuilder.CreateUIObject("SafeArea", canvas);
            UIDialogBuilder.SetStretchAll(safeAreaObj.GetComponent<RectTransform>());
            safeAreaObj.AddComponent<SafeAreaFitter>();
            return safeAreaObj.transform;
        }

        private static MoneyBarView CreateMoneyBar(Transform safeArea)
        {
            var barObj = UIDialogBuilder.CreateUIObject("MoneyBar", safeArea);
            float width = MoneyCellSize.x * MoneyBarSeats + MoneyCellSpacing * (MoneyBarSeats - 1);
            SetAnchoredRect(barObj.GetComponent<RectTransform>(), TopLeftAnchor, MoneyBarPosition, new Vector2(width, MoneyCellSize.y));
            ConfigureLayout(barObj.AddComponent<HorizontalLayoutGroup>(), MoneyCellSpacing, TextAnchor.MiddleLeft);

            var texts = new Text[MoneyBarSeats];
            var backgrounds = new Image[MoneyBarSeats];
            for (int seat = 0; seat < MoneyBarSeats; seat++)
            {
                var cellObj = UIDialogBuilder.CreateUIObject($"Seat{seat + 1}", barObj.transform);
                cellObj.GetComponent<RectTransform>().sizeDelta = MoneyCellSize;
                backgrounds[seat] = cellObj.AddComponent<Image>();
                backgrounds[seat].raycastTarget = false;

                texts[seat] = CreateStretchText(cellObj.transform, "Text", MoneyFontSize);
                AddOutline(texts[seat].gameObject);
            }

            var bar = barObj.AddComponent<MoneyBarView>();
            SetArray(bar, "_cells", texts);
            SetArray(bar, "_backgrounds", backgrounds);
            return bar;
        }

        private static (RouletteView, RouletteInput) CreateRoulette(Transform safeArea)
        {
            // フリックを受ける透明な帯。回転するホイールに判定を持たせると回転中に判定の形が変わるので分ける
            var areaObj = UIDialogBuilder.CreateUIObject("Roulette", safeArea);
            var areaRect = areaObj.GetComponent<RectTransform>();
            areaRect.anchorMin = Vector2.zero;
            areaRect.anchorMax = new Vector2(1f, 0f);
            areaRect.pivot = BottomCenterAnchor;
            areaRect.sizeDelta = new Vector2(0f, RouletteAreaHeight);
            areaRect.anchoredPosition = Vector2.zero;
            areaObj.AddComponent<Image>().color = Color.clear;
            var input = areaObj.AddComponent<RouletteInput>();

            var wheelObj = UIDialogBuilder.CreateUIObject("Wheel", areaObj.transform);
            var wheelRect = wheelObj.GetComponent<RectTransform>();
            SetAnchoredRect(wheelRect, BottomCenterAnchor, new Vector2(0f, WheelY), Vector2.one * WheelSize);
            // 回転の中心をホイールの中心にする（SetAnchoredRect はピボットを下端に揃えるので戻す）
            wheelRect.pivot = CenterAnchor;
            wheelRect.anchoredPosition = new Vector2(0f, WheelY + WheelSize * 0.5f);
            var wheelImage = wheelObj.AddComponent<Image>();
            wheelImage.raycastTarget = false;

            Text labelTemplate = CreateText(wheelObj.transform, "LabelTemplate", WheelLabelFontSize, CenterAnchor,
                Vector2.zero, new Vector2(100f, 80f), new Color(0.2f, 0.15f, 0.1f));

            // 針はホイールの真上に固定する（出目は針の指す数字）
            Text pointer = CreateText(areaObj.transform, "Pointer", PointerFontSize, BottomCenterAnchor,
                new Vector2(0f, WheelY + WheelSize - 30f), new Vector2(120f, 100f), new Color(0.9f, 0.2f, 0.2f));
            pointer.text = "▼";
            AddOutline(pointer.gameObject);

            Text hint = CreateText(areaObj.transform, "Hint", RouletteHintFontSize, BottomCenterAnchor,
                new Vector2(0f, WheelY + WheelSize + 60f), new Vector2(900f, 130f), Color.white);
            AddOutline(hint.gameObject);

            var view = areaObj.AddComponent<RouletteView>();
            SetRefs(view, ("_wheel", wheelRect), ("_wheelImage", wheelImage), ("_labelTemplate", labelTemplate), ("_hintText", hint));
            return (view, input);
        }

        private static Button CreateHudButton(Transform parent, string name, string label, Vector2 size, Vector2 anchor, Vector2 position)
        {
            GameObject buttonObj = UIDialogBuilder.CreateButton(parent, name, label, size.x, size.y, HudButtonColor);
            SetAnchor(buttonObj.GetComponent<RectTransform>(), anchor, position);
            buttonObj.GetComponentInChildren<Text>().fontSize = ButtonFontSize;
            return buttonObj.GetComponent<Button>();
        }

        private static PauseButton CreatePauseButton(Transform safeArea)
        {
            GameObject buttonObj = UIDialogBuilder.CreateButton(safeArea, "Btn_Pause", "II", PauseButtonSize, PauseButtonSize,
                HudButtonColor);
            SetAnchor(buttonObj.GetComponent<RectTransform>(), TopRightAnchor, PauseButtonPosition);
            buttonObj.GetComponentInChildren<Text>().fontSize = ButtonFontSize;
            return buttonObj.AddComponent<PauseButton>();
        }

        // ------------------------------------------------------------------
        // パネル
        // ------------------------------------------------------------------
        private static WalletPanel CreateWalletPanel(Transform canvas)
        {
            const float bodyHeight = 700f;
            const float buttonHeight = 130f;

            GameObject overlay = CreatePanelOverlay(canvas, "WalletPanel");
            Transform box = CreatePanelBox(overlay.transform);

            Text body = CreateBodyText(box, bodyHeight);
            body.alignment = TextAnchor.UpperLeft;

            Transform row = CreateRow(box, "Buttons", PanelInnerWidth, buttonHeight);
            Button repay = CreatePanelButton(row, "Btn_Repay", "手形を1枚返す", 520f, buttonHeight, ConfirmButtonColor);
            Button close = CreatePanelButton(row, "Btn_Close", "閉じる", 260f, buttonHeight, ChoiceButtonColor);

            var panel = overlay.AddComponent<WalletPanel>();
            SetRefs(panel, ("_bodyText", body), ("_repayButton", repay), ("_closeButton", close));
            overlay.SetActive(false);
            return panel;
        }

        private static ChoicePanel CreateChoicePanel(Transform canvas)
        {
            const float titleHeight = 200f;
            const float confirmHeight = 130f;
            // 株は1〜10＋「買わない」の11個あるので、2列に並べて縦に収める
            var optionSize = new Vector2(400f, 130f);
            const float optionSpacing = 20f;

            GameObject overlay = CreatePanelOverlay(canvas, "ChoicePanel");
            Transform box = CreatePanelBox(overlay.transform);

            Text title = CreateBodyText(box, titleHeight);

            var optionsObj = UIDialogBuilder.CreateUIObject("Options", box);
            optionsObj.GetComponent<RectTransform>().sizeDelta = new Vector2(PanelInnerWidth, 0f);
            var grid = optionsObj.AddComponent<GridLayoutGroup>();
            grid.cellSize = optionSize;
            grid.spacing = Vector2.one * optionSpacing;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;
            grid.childAlignment = TextAnchor.UpperCenter;
            // 選択肢の数に合わせて高さを変え、下の決定ボタンを詰めて置く
            optionsObj.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            Button template = CreatePanelButton(optionsObj.transform, "OptionTemplate", "", optionSize.x, optionSize.y, ChoiceButtonColor);
            template.gameObject.SetActive(false);

            Button confirm = CreatePanelButton(box, "Btn_Confirm", "決定", 400f, confirmHeight, ConfirmButtonColor);

            var panel = overlay.AddComponent<ChoicePanel>();
            SetRefs(panel, ("_titleText", title), ("_optionRoot", optionsObj.transform), ("_optionTemplate", template),
                ("_confirmButton", confirm));
            overlay.SetActive(false);
            return panel;
        }

        /// <summary>画面全体をボタンにして、どこをタップしても閉じられるようにする</summary>
        private static EventPopupView CreateEventPopup(Transform canvas, out Text body)
        {
            const float bodyHeight = 1100f;
            const int hintFontSize = 36;

            GameObject overlay = CreatePanelOverlay(canvas, "EventPopup");
            var tapArea = overlay.AddComponent<Button>();
            tapArea.transition = Selectable.Transition.None;

            Transform box = CreatePanelBox(overlay.transform);
            body = CreateBodyText(box, bodyHeight);
            Text hint = CreateText(box, "Hint", hintFontSize, CenterAnchor, Vector2.zero, new Vector2(PanelInnerWidth, 60f),
                new Color(0.7f, 0.75f, 0.85f));
            hint.text = "タップで閉じる";

            var popup = overlay.AddComponent<EventPopupView>();
            SetRefs(popup, ("_bodyText", body), ("_tapArea", tapArea), ("_hintText", hint));
            overlay.SetActive(false);
            return popup;
        }

        /// <summary>「○○の番」の全画面表示。画面全体をボタンにして、どこをタップしても開始できるようにする</summary>
        private static TurnBannerView CreateTurnBanner(Transform canvas)
        {
            const int titleFontSize = 100;
            // 「P1 しっかり者（NPC） の番」が折り返して下のヒントと重ならないよう、1行に収まる大きさまで縮める
            const int titleMinFontSize = 50;
            const int hintFontSize = 52;

            GameObject overlay = CreatePanelOverlay(canvas, "TurnBanner");
            var tapArea = overlay.AddComponent<Button>();
            tapArea.transition = Selectable.Transition.None;

            Text title = CreateText(overlay.transform, "TitleText", titleFontSize, CenterAnchor, new Vector2(0f, 80f),
                new Vector2(1000f, 200f), Color.white);
            title.resizeTextForBestFit = true;
            title.resizeTextMinSize = titleMinFontSize;
            title.resizeTextMaxSize = titleFontSize;
            title.horizontalOverflow = HorizontalWrapMode.Wrap;
            title.verticalOverflow = VerticalWrapMode.Truncate;
            AddOutline(title.gameObject);

            Text hint = CreateText(overlay.transform, "HintText", hintFontSize, CenterAnchor, new Vector2(0f, -80f),
                new Vector2(1000f, 90f), Color.white);
            hint.text = "タップで開始";

            var banner = overlay.AddComponent<TurnBannerView>();
            SetRefs(banner, ("_titleText", title), ("_hintText", hint), ("_tapArea", tapArea),
                ("_background", overlay.GetComponent<Image>()));
            overlay.SetActive(false);
            return banner;
        }

        /// <summary>テーマごとのボタンを縦に並べる。文字（テーマ名）は ThemeSelectPanel が実行時にテーマから入れる</summary>
        private static ThemeSelectPanel CreateThemeSelectPanel(Transform canvas, int themeCount)
        {
            const int titleFontSize = 60;
            var buttonSize = new Vector2(640f, 160f);

            GameObject overlay = CreatePanelOverlay(canvas, "ThemeSelectPanel");
            Transform box = CreatePanelBox(overlay.transform);

            Text title = CreateText(box, "TitleText", titleFontSize, CenterAnchor, Vector2.zero,
                new Vector2(PanelInnerWidth, 100f), Color.white);
            title.text = "テーマを選ぶ";

            var buttons = new Button[themeCount];
            for (int i = 0; i < themeCount; i++)
            {
                buttons[i] = CreatePanelButton(box, $"Btn_Theme{i + 1}", "", buttonSize.x, buttonSize.y, ChoiceButtonColor);
            }

            var panel = overlay.AddComponent<ThemeSelectPanel>();
            SetArray(panel, "_themeButtons", buttons);
            overlay.SetActive(false);
            return panel;
        }

        /// <summary>人数と各席の人間/NPCを選ぶ。使わない席の行は VerticalLayoutGroup で詰める</summary>
        private static PlayerSetupPanel CreatePlayerSetupPanel(Transform canvas)
        {
            const int titleFontSize = 60;
            const float rowHeight = 120f;
            const float countButtonWidth = 240f;
            const float seatLabelWidth = 160f;
            const float kindButtonWidth = 480f;
            const int seatLabelFontSize = 56;
            int countOptions = PlayerSetupPanel.MaxPlayers - PlayerSetupPanel.MinPlayers + 1;

            GameObject overlay = CreatePanelOverlay(canvas, "PlayerSetupPanel");
            Transform box = CreatePanelBox(overlay.transform);

            Text title = CreateText(box, "TitleText", titleFontSize, CenterAnchor, Vector2.zero,
                new Vector2(PanelInnerWidth, 100f), Color.white);
            title.text = "プレイヤー設定";

            Transform countRow = CreateRow(box, "CountRow", PanelInnerWidth, rowHeight);
            var countButtons = new Button[countOptions];
            for (int i = 0; i < countOptions; i++)
            {
                int count = PlayerSetupPanel.MinPlayers + i;
                countButtons[i] = CreatePanelButton(countRow, $"Btn_{count}Players", $"{count}人", countButtonWidth, rowHeight,
                    ChoiceButtonColor);
            }

            var rows = new GameObject[PlayerSetupPanel.MaxPlayers];
            var kindButtons = new Button[PlayerSetupPanel.MaxPlayers];
            var kindTexts = new Text[PlayerSetupPanel.MaxPlayers];
            for (int seat = 0; seat < PlayerSetupPanel.MaxPlayers; seat++)
            {
                Transform row = CreateRow(box, $"Row_P{seat + 1}", PanelInnerWidth, rowHeight);
                rows[seat] = row.gameObject;

                Text label = CreateText(row, "Label", seatLabelFontSize, CenterAnchor, Vector2.zero,
                    new Vector2(seatLabelWidth, rowHeight), LifeColors.Seat(seat));
                label.text = LifeTexts.PlayerName(seat);
                AddOutline(label.gameObject);

                kindButtons[seat] = CreatePanelButton(row, "Btn_Kind", "", kindButtonWidth, rowHeight, ChoiceButtonColor);
                kindTexts[seat] = kindButtons[seat].GetComponentInChildren<Text>();
            }

            Transform buttonRow = CreateRow(box, "ButtonRow", PanelInnerWidth, 140f);
            Button back = CreatePanelButton(buttonRow, "Btn_Back", "戻る", 240f, 140f, ChoiceButtonColor);
            Button start = CreatePanelButton(buttonRow, "Btn_Start", "キャラ選択へ", 480f, 140f, ConfirmButtonColor);

            var panel = overlay.AddComponent<PlayerSetupPanel>();
            SetArray(panel, "_countButtons", countButtons);
            SetArray(panel, "_playerRows", rows);
            SetArray(panel, "_kindButtons", kindButtons);
            SetArray(panel, "_kindTexts", kindTexts);
            SetRefs(panel, ("_startButton", start), ("_backButton", back));
            overlay.SetActive(false);
            return panel;
        }

        /// <summary>1人ずつキャラを選ぶ。立ち絵はフェーズ6なので、名前と能力の説明だけを大きく出す</summary>
        private static CharacterSelectPanel CreateCharacterSelectPanel(Transform canvas, LifeCharacterCatalog catalog)
        {
            // 「P1（NPC） のキャラを選んでね」が1行に収まる大きさ
            const int titleFontSize = 44;
            const int nameFontSize = 80;
            const int abilityFontSize = 44;
            const float arrowSize = 150f;
            const float nameWidth = 480f;
            const float nameRowHeight = 200f;
            const float buttonRowHeight = 140f;

            GameObject overlay = CreatePanelOverlay(canvas, "CharacterSelectPanel");
            Transform box = CreatePanelBox(overlay.transform);

            Text title = CreateText(box, "TitleText", titleFontSize, CenterAnchor, Vector2.zero,
                new Vector2(PanelInnerWidth, 100f), Color.white);
            AddOutline(title.gameObject);

            Transform nameRow = CreateRow(box, "NameRow", PanelInnerWidth, nameRowHeight);
            Button prev = CreatePanelButton(nameRow, "Btn_Prev", "◀", arrowSize, arrowSize, ChoiceButtonColor);
            Text nameText = CreateText(nameRow, "NameText", nameFontSize, CenterAnchor, Vector2.zero,
                new Vector2(nameWidth, nameRowHeight), Color.white);
            Button next = CreatePanelButton(nameRow, "Btn_Next", "▶", arrowSize, arrowSize, ChoiceButtonColor);

            Text abilityText = CreateBodyText(box, 160f);
            abilityText.resizeTextMaxSize = abilityFontSize;

            Transform buttonRow = CreateRow(box, "ButtonRow", PanelInnerWidth, buttonRowHeight);
            Button back = CreatePanelButton(buttonRow, "Btn_Back", "戻る", 280f, buttonRowHeight, ChoiceButtonColor);
            Button confirm = CreatePanelButton(buttonRow, "Btn_Confirm", "決定", 440f, buttonRowHeight, ConfirmButtonColor);

            var panel = overlay.AddComponent<CharacterSelectPanel>();
            SetRefs(panel, ("_catalog", catalog), ("_titleText", title), ("_nameText", nameText),
                ("_abilityText", abilityText), ("_prevButton", prev), ("_nextButton", next),
                ("_confirmButton", confirm), ("_backButton", back));
            overlay.SetActive(false);
            return panel;
        }

        // ------------------------------------------------------------------
        // UI ヘルパー
        // ------------------------------------------------------------------
        /// <summary>画面全体を暗くして、後ろのゲーム画面への入力を遮る</summary>
        private static GameObject CreatePanelOverlay(Transform canvas, string name)
        {
            var panelObj = UIDialogBuilder.CreateUIObject(name, canvas);
            UIDialogBuilder.SetStretchAll(panelObj.GetComponent<RectTransform>());
            panelObj.AddComponent<Image>().color = PanelOverlayColor;
            return panelObj;
        }

        /// <summary>中身を上から縦に並べる中央の箱。高さは中身に合わせる</summary>
        private static Transform CreatePanelBox(Transform parent)
        {
            const float spacing = 30f;

            var boxObj = UIDialogBuilder.CreateUIObject("Panel", parent);
            var boxRect = boxObj.GetComponent<RectTransform>();
            boxRect.anchorMin = boxRect.anchorMax = boxRect.pivot = CenterAnchor;
            boxRect.sizeDelta = new Vector2(PanelBoxWidth, 0f);
            boxObj.AddComponent<Image>().color = PanelBoxColor;
            var layout = boxObj.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(PanelPadding, PanelPadding, PanelPadding, PanelPadding);
            ConfigureLayout(layout, spacing, TextAnchor.UpperCenter);
            boxObj.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return boxObj.transform;
        }

        /// <summary>複数行の本文。長い文面（精算の内訳など）は枠に収まるまで縮める</summary>
        private static Text CreateBodyText(Transform parent, float height)
        {
            Text text = CreateText(parent, "Body", PanelBodyFontSize, CenterAnchor, Vector2.zero,
                new Vector2(PanelInnerWidth, height), Color.white);
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = PanelBodyMinFontSize;
            text.resizeTextMaxSize = PanelBodyFontSize;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        private static Transform CreateRow(Transform parent, string name, float width, float height)
        {
            const float spacing = 20f;

            var rowObj = UIDialogBuilder.CreateUIObject(name, parent);
            rowObj.GetComponent<RectTransform>().sizeDelta = new Vector2(width, height);
            ConfigureLayout(rowObj.AddComponent<HorizontalLayoutGroup>(), spacing, TextAnchor.MiddleCenter);
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

        private static Button CreatePanelButton(Transform parent, string name, string label, float width, float height, Color color)
        {
            GameObject buttonObj = UIDialogBuilder.CreateButton(parent, name, label, width, height, color);
            buttonObj.GetComponentInChildren<Text>().fontSize = ButtonFontSize;
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
            // 既定の Truncate だと、1行の高さが枠を超えた瞬間にその行ごと描画されなくなるため
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        private static Text CreateStretchText(Transform parent, string name, int fontSize)
        {
            Text text = CreateText(parent, name, fontSize, CenterAnchor, Vector2.zero, Vector2.zero, Color.white);
            UIDialogBuilder.SetStretchAll(text.rectTransform);
            return text;
        }

        private static void AddOutline(GameObject obj)
        {
            obj.AddComponent<Outline>().effectDistance = ThinOutline;
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
            Debug.Log($"[LifeGameSceneBuilder] Build Settings に {scenePath} を登録しました。");
        }
    }
}
