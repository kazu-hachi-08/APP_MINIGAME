using MiniGame.Common.UI;
using MiniGame.Editor;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.PenguinWars.Editor
{
    /// <summary>Canvas と HUD（時間表示・お知らせ・中央メッセージ・ボスのHPバー・ボス登場の WARNING 帯・ポーズボタン）</summary>
    public static partial class PenguinWarsSceneBuilder
    {
        // 横画面なのでサッカーと同じ 1920x1080 基準
        private static readonly Vector2 ReferenceResolution = new Vector2(1920, 1080);
        private const float CanvasMatchWidthOrHeight = 0.5f;

        private static readonly Vector2 TopCenterAnchor = new Vector2(0.5f, 1f);
        private static readonly Vector2 TopRightAnchor = new Vector2(1f, 1f);
        private static readonly Vector2 CenterAnchor = new Vector2(0.5f, 0.5f);

        private const int TimeFontSize = 56;
        private static readonly Vector2 TimeLabelSize = new Vector2(600f, 90f);
        private static readonly Vector2 TimeLabelPosition = new Vector2(0f, -20f);

        private const int LevelUpFontSize = 64;
        private static readonly Vector2 LevelUpLabelSize = new Vector2(600f, 90f);
        private static readonly Vector2 LevelUpLabelPosition = new Vector2(0f, -115f);
        private static readonly Color LevelUpColor = new Color(1f, 0.45f, 0.35f);

        private const int MessageFontSize = 180;
        private static readonly Vector2 MessageLabelSize = new Vector2(1400f, 300f);
        private static readonly Color MessageColor = new Color(1f, 0.92f, 0.3f);

        // ボスのHPバー: お知らせ（ボス出現・なだれ注意）の下
        private static readonly Vector2 BossBarPosition = new Vector2(0f, -215f);
        private static readonly Vector2 BossBarSize = new Vector2(900f, 100f);
        private const int BossNameFontSize = 40;
        private static readonly Vector2 BossNamePosition = new Vector2(0f, 25f);
        private static readonly Vector2 BossNameSize = new Vector2(900f, 50f);
        private static readonly Vector2 BossGaugePosition = new Vector2(0f, -25f);
        private static readonly Vector2 BossGaugeSize = new Vector2(800f, 32f);
        private static readonly Color BossGaugeBackColor = new Color(0f, 0f, 0f, 0.6f);
        private static readonly Color BossGaugeFillColor = new Color(0.9f, 0.2f, 0.2f);

        // WARNING の帯: お知らせ（なだれ注意）と同じ高さ。横は画面より広くして端が切れて見えないようにする
        private static readonly Vector2 BossWarningPosition = new Vector2(0f, -110f);
        private static readonly Vector2 BossWarningSize = new Vector2(2600f, 120f);
        private static readonly Color BossWarningBandColor = new Color(0.85f, 0.05f, 0.1f, 0.85f);
        private const int BossWarningFontSize = 84;
        private const string BossWarningText = "WARNING!      WARNING!      WARNING!";

        private const float PauseButtonSize = 100f;
        private const int PauseFontSize = 44;
        private static readonly Vector2 PauseButtonPosition = new Vector2(-30f, -20f);
        private static readonly Color HudButtonColor = new Color(0.15f, 0.17f, 0.22f, 0.85f);

        // 雪の白い地面や空の上でも文字が読めるように縁取る
        private static readonly Vector2 TextOutline = new Vector2(3f, -3f);
        private static readonly Color TextOutlineColor = new Color(0f, 0f, 0f, 0.8f);

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

        /// <summary>ボタンや時間表示はノッチを避けるため SafeArea 内に置く（全画面のパネルは Canvas 直下）</summary>
        private static Transform CreateSafeArea(Transform canvas)
        {
            var safeAreaObj = UIDialogBuilder.CreateUIObject("SafeArea", canvas);
            UIDialogBuilder.SetStretchAll(safeAreaObj.GetComponent<RectTransform>());
            safeAreaObj.AddComponent<SafeAreaFitter>();
            return safeAreaObj.transform;
        }

        private static BattleHud CreateHud(Transform safeArea, Transform canvas)
        {
            Text timeLabel = CreateText(safeArea, "TimeLabel", TimeFontSize, TopCenterAnchor, TimeLabelPosition, TimeLabelSize, Color.white);
            Text levelUpLabel = CreateText(safeArea, "LevelUpLabel", LevelUpFontSize, TopCenterAnchor, LevelUpLabelPosition, LevelUpLabelSize, LevelUpColor);
            levelUpLabel.gameObject.SetActive(false);
            Text messageLabel = CreateText(canvas, "MessageLabel", MessageFontSize, CenterAnchor, Vector2.zero, MessageLabelSize, MessageColor);

            var hud = safeArea.gameObject.AddComponent<BattleHud>();
            SetRefs(hud, ("_timeLabel", timeLabel), ("_messageLabel", messageLabel), ("_levelUpLabel", levelUpLabel));
            return hud;
        }

        private static BossHpBar CreateBossHpBar(Transform safeArea, BattleRunner battleRunner, PenguinUnitCatalog catalog)
        {
            GameObject barObj = UIDialogBuilder.CreateUIObject("BossHpBar", safeArea);
            RectTransform barRect = barObj.GetComponent<RectTransform>();
            SetAnchor(barRect, TopCenterAnchor, BossBarPosition);
            barRect.sizeDelta = BossBarSize;

            // 表示を切り替えるのは子の Root（BossHpBar 自身を消すと Update が止まって二度と出なくなるため）
            GameObject root = UIDialogBuilder.CreateUIObject("Root", barObj.transform);
            UIDialogBuilder.SetStretchAll(root.GetComponent<RectTransform>());
            Text name = CreateText(root.transform, "Name", BossNameFontSize, CenterAnchor, BossNamePosition, BossNameSize, LevelUpColor);
            Image gauge = CreateImage(root.transform, "Gauge", CenterAnchor, BossGaugePosition, BossGaugeSize, BossGaugeBackColor);
            Image fill = CreateImage(gauge.transform, "Fill", CenterAnchor, Vector2.zero, Vector2.zero, BossGaugeFillColor);
            UIDialogBuilder.SetStretchAll(fill.rectTransform);
            root.SetActive(false);

            var bar = barObj.AddComponent<BossHpBar>();
            SetRefs(bar, ("_battleRunner", battleRunner), ("_catalog", catalog), ("_root", root), ("_fill", fill.rectTransform), ("_nameLabel", name));
            return bar;
        }

        /// <summary>ボスの HPバーより奥（帯が消えた後に HPバーが見えるように、HPバーより先に作る）</summary>
        private static BossWarningBanner CreateBossWarningBanner(Transform safeArea)
        {
            GameObject bannerObj = UIDialogBuilder.CreateUIObject("BossWarning", safeArea);
            RectTransform bannerRect = bannerObj.GetComponent<RectTransform>();
            SetAnchor(bannerRect, TopCenterAnchor, BossWarningPosition);
            bannerRect.sizeDelta = BossWarningSize;

            // 表示を切り替えるのは子の Root（BossWarningBanner 自身を消すと Update が止まるため）
            GameObject root = UIDialogBuilder.CreateUIObject("Root", bannerObj.transform);
            UIDialogBuilder.SetStretchAll(root.GetComponent<RectTransform>());
            Image band = CreateImage(root.transform, "Band", CenterAnchor, Vector2.zero, BossWarningSize, BossWarningBandColor);
            Text text = CreateText(root.transform, "Text", BossWarningFontSize, CenterAnchor, Vector2.zero, BossWarningSize, Color.white);
            text.text = BossWarningText;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            root.SetActive(false);

            var banner = bannerObj.AddComponent<BossWarningBanner>();
            SetRefs(banner, ("_root", root), ("_band", band), ("_scrollText", text.rectTransform));
            return banner;
        }

        private static PauseButton CreatePauseButton(Transform safeArea)
        {
            GameObject buttonObj = UIDialogBuilder.CreateButton(safeArea, "Btn_Pause", "II", PauseButtonSize, PauseButtonSize, HudButtonColor);
            SetAnchor(buttonObj.GetComponent<RectTransform>(), TopRightAnchor, PauseButtonPosition);
            buttonObj.GetComponentInChildren<Text>().fontSize = PauseFontSize;
            return buttonObj.AddComponent<PauseButton>();
        }

        private static Text CreateText(Transform parent, string name, int fontSize,
            Vector2 anchor, Vector2 anchoredPosition, Vector2 size, Color color)
        {
            var obj = UIDialogBuilder.CreateUIObject(name, parent);
            RectTransform rect = obj.GetComponent<RectTransform>();
            SetAnchor(rect, anchor, anchoredPosition);
            rect.sizeDelta = size;

            var text = obj.AddComponent<Text>();
            text.fontSize = fontSize;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = color;
            // 既定の Truncate だと、1行の高さが枠を超えた瞬間にその行ごと描画されなくなるため
            text.verticalOverflow = VerticalWrapMode.Overflow;
            // 文字の上でもドラッグで戦場をスクロールできるようにする
            text.raycastTarget = false;

            var outline = obj.AddComponent<Outline>();
            outline.effectDistance = TextOutline;
            outline.effectColor = TextOutlineColor;
            return text;
        }

        /// <summary>アンカー・ピボットを同じ点に揃えて、その点からの位置で置く</summary>
        private static void SetAnchor(RectTransform rect, Vector2 anchor, Vector2 anchoredPosition)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = anchoredPosition;
        }
    }
}
