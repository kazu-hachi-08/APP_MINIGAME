using System.IO;
using MiniGame.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.PenguinWars.Editor
{
    /// <summary>ゲーム固有のタイトル画面・音の設定パネル（ずかんは .Zukan、あそびかたは .Guide）と、Audio/ に置いたBGM素材の差し込み（仕様書 §2.0・§9）</summary>
    public static partial class PenguinWarsSceneBuilder
    {
        private const string AudioDirectory = RootDirectory + "/Audio";
        // この名前（拡張子は mp3 / ogg / wav など何でもよい）で Audio/ に置くと自動で差し込む。
        // Scene は作り直すたびに上書きされるので、Inspector で手で差し込んでも消えてしまうため
        private const string TitleBgmName = "PenguinWars_TitleBgm";
        private const string BattleBgmName = "PenguinWars_BattleBgm";
        private const string BossBgmName = "PenguinWars_BossBgm";

        // 戦場が透けて見える程度に暗くして、ロゴとボタンを読みやすくする
        private static readonly Color TitleBackColor = new Color(0f, 0.08f, 0.2f, 0.55f);

        private const string TitleLogoText = "ペンギン大戦争";
        private const string TitleSubText = "PENGUIN WARS";
        private const int TitleLogoFontSize = 190;
        private const int TitleSubFontSize = 56;
        private static readonly Vector2 TitleLogoPosition = new Vector2(0f, 300f);
        private static readonly Vector2 TitleLogoSize = new Vector2(1600f, 260f);
        private static readonly Vector2 TitleSubPosition = new Vector2(0f, -150f);
        private static readonly Vector2 TitleSubSize = new Vector2(1000f, 80f);
        private static readonly Vector2 TitleLogoOutline = new Vector2(6f, -6f);
        private static readonly Color TitleSubColor = new Color(0.75f, 0.92f, 1f);

        private const int ParadeCount = 5;
        private static readonly Vector2 ParadeIconSize = new Vector2(150f, 150f);
        private const float ParadeSpacing = 210f;
        private const float ParadeY = -10f;

        // パレードとスタートボタンの間に小さく出す
        private const int TitleStarsFontSize = 40;
        private static readonly Vector2 TitleStarsPosition = new Vector2(0f, -122f);
        private static readonly Vector2 TitleStarsSize = new Vector2(600f, 56f);

        private const int TitleButtonFontSize = 48;
        private static readonly Vector2 StartButtonSize = new Vector2(620f, 130f);
        private static readonly Vector2 StartButtonPosition = new Vector2(0f, -220f);
        private static readonly Vector2 TitleSubButtonSize = new Vector2(350f, 100f);
        // 5つを 1920 幅に収める（ボタン幅 350 + 隙間 15）
        private static readonly Vector2 ZukanButtonPosition = new Vector2(-730f, -380f);
        private static readonly Vector2 CustomButtonPosition = new Vector2(-365f, -380f);
        private static readonly Vector2 GuideButtonPosition = new Vector2(0f, -380f);
        private static readonly Vector2 SettingsButtonPosition = new Vector2(365f, -380f);
        private static readonly Vector2 BackButtonPosition = new Vector2(730f, -380f);
        // 「じぶんペンギン」「メニューに戻る」が 350 幅で折り返さないよう、縮めてよい下限
        private const int TitleSubButtonMinFontSize = 32;
        private static readonly Color StartButtonColor = new Color(0.95f, 0.55f, 0.15f);
        private static readonly Color TitleSubButtonColor = new Color(0.18f, 0.26f, 0.4f, 0.95f);

        private const float SettingsPanelWidth = 900f;
        private const int SettingsPadding = 50;
        private const float SettingsSpacing = 30f;
        private const float SettingsTitleHeight = 90f;
        private const int SettingsTitleFontSize = 60;
        private const float SettingsButtonHeight = 110f;
        private static readonly Color SettingsCloseColor = new Color(0.18f, 0.55f, 0.9f);

        /// <summary>モード選択より手前に作り、最初に見える画面にする</summary>
        private static PenguinWarsTitlePanel CreateTitlePanel(Transform canvas, PenguinUnitCatalog catalog,
            BattleRunner battleRunner, BattleCamera battleCamera)
        {
            // 背景は raycastTarget を残し、タイトル中に戦場のドラッグや下のボタンが効かないようにする
            GameObject panelObj = CreateFullScreenPanel(canvas, "TitlePanel", TitleBackColor);

            RectTransform logo = CreateTitleLogo(panelObj.transform);
            var icons = new Image[ParadeCount];
            for (int i = 0; i < ParadeCount; i++)
            {
                float x = (i - (ParadeCount - 1) * 0.5f) * ParadeSpacing;
                icons[i] = CreateImage(panelObj.transform, $"Parade{i + 1}", CenterAnchor, new Vector2(x, ParadeY), ParadeIconSize, Color.white);
                icons[i].preserveAspect = true;
            }

            Text stars = CreateText(panelObj.transform, "Stars", TitleStarsFontSize, CenterAnchor, TitleStarsPosition, TitleStarsSize, MessageColor);

            Button start = CreateTitleButton(panelObj.transform, "Btn_Start", "スタート", StartButtonSize, StartButtonPosition, StartButtonColor);
            Button zukan = CreateTitleButton(panelObj.transform, "Btn_Zukan", "ずかん", TitleSubButtonSize, ZukanButtonPosition, TitleSubButtonColor);
            Button custom = CreateTitleButton(panelObj.transform, "Btn_Custom", "じぶんペンギン", TitleSubButtonSize, CustomButtonPosition, TitleSubButtonColor);
            Button guide = CreateTitleButton(panelObj.transform, "Btn_Guide", "あそびかた", TitleSubButtonSize, GuideButtonPosition, TitleSubButtonColor);
            Button settings = CreateTitleButton(panelObj.transform, "Btn_Settings", "設定", TitleSubButtonSize, SettingsButtonPosition, TitleSubButtonColor);
            Button back = CreateTitleButton(panelObj.transform, "Btn_Back", "メニューに戻る", TitleSubButtonSize, BackButtonPosition, TitleSubButtonColor);
            FitText(custom.GetComponentInChildren<Text>(), TitleSubButtonMinFontSize);
            FitText(back.GetComponentInChildren<Text>(), TitleSubButtonMinFontSize);
            // ずかん・じぶんペンギン・設定はタイトルの上に重ねたいので、タイトルの子の一番最後に作る
            PenguinZukanPanel zukanPanel = CreateZukanPanel(panelObj.transform, catalog);
            CustomUnitPanel customPanel = CreateCustomUnitPanel(panelObj.transform);
            PenguinWarsSettingsPanel settingsPanel = CreateSettingsPanel(panelObj.transform);
            // あそびかたはタイトルの幕を閉じて戦場を見せるので、タイトルの子にせず Canvas 直下（タイトルの次）に作る
            PenguinGuidePanel guidePanel = CreateGuidePanel(canvas, battleRunner, battleCamera);

            var panel = panelObj.AddComponent<PenguinWarsTitlePanel>();
            var so = new SerializedObject(panel);
            SerializedArray(so, "_paradeIcons", icons);
            so.ApplyModifiedPropertiesWithoutUndo();
            SetRefs(panel, ("_catalog", catalog), ("_logo", logo), ("_startButton", start), ("_settingsButton", settings),
                ("_backButton", back), ("_settingsPanel", settingsPanel), ("_zukanButton", zukan), ("_zukanPanel", zukanPanel),
                ("_guideButton", guide), ("_guidePanel", guidePanel), ("_starsLabel", stars),
                ("_customButton", custom), ("_customPanel", customPanel));
            panelObj.SetActive(false);
            return panel;
        }

        /// <summary>ロゴと英字をひとまとめにして、一緒に揺らす</summary>
        private static RectTransform CreateTitleLogo(Transform panel)
        {
            Text logo = CreateText(panel, "Logo", TitleLogoFontSize, CenterAnchor, TitleLogoPosition, TitleLogoSize, MessageColor);
            logo.text = TitleLogoText;
            logo.GetComponent<Outline>().effectDistance = TitleLogoOutline;

            Text sub = CreateText(logo.transform, "SubTitle", TitleSubFontSize, CenterAnchor, TitleSubPosition, TitleSubSize, TitleSubColor);
            sub.text = TitleSubText;
            return logo.rectTransform;
        }

        /// <summary>ポーズ画面と同じく項目を上から積むだけにして、座標計算をしなくて済むようにする</summary>
        private static PenguinWarsSettingsPanel CreateSettingsPanel(Transform parent)
        {
            GameObject dimObj = CreateFullScreenPanel(parent, "SettingsPanel", DialogDimColor);

            GameObject boxObj = UIDialogBuilder.CreateUIObject("Box", dimObj.transform);
            RectTransform boxRect = boxObj.GetComponent<RectTransform>();
            SetAnchor(boxRect, CenterAnchor, Vector2.zero);
            boxRect.pivot = new Vector2(0.5f, 0.5f);
            boxRect.sizeDelta = new Vector2(SettingsPanelWidth, 0f);
            boxObj.AddComponent<Image>().color = DialogBoxColor;
            AddVerticalLayout(boxObj);

            GameObject titleObj = UIDialogBuilder.CreateUIObject("Title", boxObj.transform);
            titleObj.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, SettingsTitleHeight);
            var title = titleObj.AddComponent<Text>();
            title.text = "設定";
            title.fontSize = SettingsTitleFontSize;
            title.fontStyle = FontStyle.Bold;
            title.alignment = TextAnchor.MiddleCenter;
            title.color = Color.white;

            Button bgmToggle = CreateSettingsButton(boxObj.transform, "Btn_BgmToggle", "BGM：ON", SettingsCloseColor);
            Slider bgmSlider = UIDialogBuilder.CreateVolumeSlider("BgmSlider", boxObj.transform, "BGM 音量");
            Slider seSlider = UIDialogBuilder.CreateVolumeSlider("SeSlider", boxObj.transform, "SE 音量");
            Button close = CreateSettingsButton(boxObj.transform, "Btn_Close", "閉じる", SettingsCloseColor);

            var panel = dimObj.AddComponent<PenguinWarsSettingsPanel>();
            SetRefs(panel, ("_bgmToggleButton", bgmToggle), ("_bgmToggleLabel", bgmToggle.GetComponentInChildren<Text>()),
                ("_bgmSlider", bgmSlider), ("_seSlider", seSlider), ("_closeButton", close));
            dimObj.SetActive(false);
            return panel;
        }

        private static void AddVerticalLayout(GameObject box)
        {
            var layout = box.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(SettingsPadding, SettingsPadding, SettingsPadding, SettingsPadding);
            layout.spacing = SettingsSpacing;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            box.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        private static Button CreateSettingsButton(Transform parent, string name, string label, Color color)
        {
            // 幅はレイアウトが決めるので 0 を渡す
            GameObject buttonObj = UIDialogBuilder.CreateButton(parent, name, label, 0f, SettingsButtonHeight, color);
            buttonObj.GetComponentInChildren<Text>().fontSize = TitleButtonFontSize;
            return buttonObj.GetComponent<Button>();
        }

        /// <summary>Audio/ に決まった名前の素材があれば、PenguinWarsAudio のBGM欄に差し込む</summary>
        private static void AssignBgmClips(PenguinWarsAudio audio)
        {
            EnsureDirectory(AudioDirectory);
            SetRefs(audio, ("_titleBgmClip", FindAudioClip(TitleBgmName)), ("_bgmClip", FindAudioClip(BattleBgmName)),
                ("_bossBgmClip", FindAudioClip(BossBgmName)));
        }

        private static AudioClip FindAudioClip(string fileName)
        {
            if (!AssetDatabase.IsValidFolder(AudioDirectory)) return null;

            foreach (string guid in AssetDatabase.FindAssets($"{fileName} t:AudioClip", new[] { AudioDirectory }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                // FindAssets は部分一致なので、名前が完全に同じものだけ使う
                if (Path.GetFileNameWithoutExtension(path) == fileName) return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            }

            Debug.Log($"{LogPrefix} {AudioDirectory}/{fileName} が無いので、コードで作った仮のBGMを鳴らします");
            return null;
        }
    }
}
