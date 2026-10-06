#nullable enable
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace CardGame.Unity.UI
{
    /// <summary>
    /// uGUI をコードで組み立てるための小さなヘルパー群。
    /// シーンやプレハブを手で編集せずに済むよう、全ての画面はこれで構築する。
    /// 座標系: 基準解像度 1920×1080(横)。anchor は左下原点。
    /// </summary>
    public static class Ui
    {
        public const float RefWidth = 1920f;
        public const float RefHeight = 1080f;

        private static Font? _font;
        private static Font? _digitFont;
        /// <summary>同梱の日本語フォント(OFL)。WebGL では OS フォントにフォールバックできないため必須。</summary>
        public static Font Font => _font ??= Resources.Load<Font>("Fonts/" + FontName) ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        /// <summary>バッジの数字用(欧文)。無ければ本文フォント。</summary>
        public static Font DigitFont => _digitFont ??= Resources.Load<Font>("Fonts/" + DigitFontName) ?? Font;
        /// <summary>カード名(リボン)用の書体。解星 徳明 ExtraBold(オーナー決定 2026-09-23、OFL)。無ければ本文フォント。</summary>
        public static Font NameFont => _nameFont ??= Resources.Load<Font>("Fonts/KaiseiTokumin-ExtraBold") ?? Font;
        private static Font? _nameFont;
        public static string FontName { get; private set; } = "NotoSerifJP-Bold";   // オーナー決定(2026-09-22): 明朝
        public static string DigitFontName { get; private set; } = "Cinzel-Bold";
        /// <summary>フォントを切り替える(以後に作る Text に反映。書体の見本出し用)。</summary>
        public static void SetFonts(string? body, string? digits)
        {
            if (body != null) { FontName = body; _font = null; }
            if (digits != null) { DigitFontName = digits; _digitFont = null; }
        }

        // ---- 色パレット(フェーズ3: 機能重視の素朴な配色) ----
        public static readonly Color Bg = new Color(0.08f, 0.09f, 0.12f);
        public static readonly Color PanelDarkColor = new Color(0.14f, 0.15f, 0.19f);
        public static readonly Color PanelColor = new Color(0.20f, 0.22f, 0.27f);
        public static readonly Color TextMain = new Color(0.95f, 0.95f, 0.95f);
        public static readonly Color TextDim = new Color(0.70f, 0.72f, 0.78f);
        public static readonly Color Accent = new Color(0.98f, 0.75f, 0.25f);
        public static readonly Color Good = new Color(0.35f, 0.85f, 0.45f);
        public static readonly Color Bad = new Color(0.90f, 0.30f, 0.30f);
        public static readonly Color Highlight = new Color(0.40f, 0.80f, 1.00f);

        /// <summary>全画面 Canvas + CanvasScaler を作る。</summary>
        public static Canvas CreateCanvas(string name, int sortingOrder = 0)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(RefWidth, RefHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            go.AddComponent<AspectFit>();
            return canvas;
        }

        /// <summary>親に対して絶対座標(左下原点, 基準解像度のピクセル)で矩形を置く。</summary>
        public static RectTransform Rect(Transform parent, string name, float x, float y, float w, float h)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = Vector2.zero;
            rt.pivot = Vector2.zero;
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(w, h);
            return rt;
        }

        /// <summary>親いっぱいに広げた矩形。</summary>
        public static RectTransform Fill(Transform parent, string name, float margin = 0)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(margin, margin);
            rt.offsetMax = new Vector2(-margin, -margin);
            return rt;
        }

        public static Image Panel(Transform parent, string name, float x, float y, float w, float h, Color color)
        {
            var rt = Rect(parent, name, x, y, w, h);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            return img;
        }

        public static Image FillPanel(Transform parent, string name, Color color, float margin = 0)
        {
            var rt = Fill(parent, name, margin);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            return img;
        }

        public static Text Label(Transform parent, string name, float x, float y, float w, float h, string text,
            int size = 28, TextAnchor align = TextAnchor.MiddleCenter, Color? color = null, FontStyle style = FontStyle.Normal)
        {
            var rt = Rect(parent, name, x, y, w, h);
            var t = rt.gameObject.AddComponent<Text>();
            Setup(t, text, size, align, color ?? TextMain, style);
            return t;
        }

        public static Text FillLabel(Transform parent, string name, string text, int size = 28,
            TextAnchor align = TextAnchor.MiddleCenter, Color? color = null, FontStyle style = FontStyle.Normal, float margin = 0)
        {
            var rt = Fill(parent, name, margin);
            var t = rt.gameObject.AddComponent<Text>();
            Setup(t, text, size, align, color ?? TextMain, style);
            return t;
        }

        private static void Setup(Text t, string text, int size, TextAnchor align, Color color, FontStyle style)
        {
            t.font = Font;
            t.text = text;
            t.fontSize = size;
            t.alignment = align;
            t.color = color;
            // 同梱フォントが太字版のときは FontStyle.Bold(合成の太字)を重ねない
            t.fontStyle = style == FontStyle.Bold && FontName.EndsWith("Bold") ? FontStyle.Normal : style;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            t.supportRichText = true;
        }

        public static Button Button(Transform parent, string name, float x, float y, float w, float h, string label,
            UnityAction onClick, Color? bg = null, int fontSize = 30)
        {
            var img = Panel(parent, name, x, y, w, h, bg ?? PanelColor);
            var btn = img.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            var colors = btn.colors;
            colors.highlightedColor = new Color(1.1f, 1.1f, 1.1f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f);
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.6f);
            btn.colors = colors;
            btn.onClick.AddListener(() => Audio.Play(Audio.Button, 0.6f));   // どのボタンでもクリック音
            btn.onClick.AddListener(onClick);
            FillLabel(img.transform, "Label", label, fontSize, TextAnchor.MiddleCenter, TextMain, FontStyle.Bold);
            return btn;
        }

        /// <summary>枠線風の見た目(外側 Image + 内側 Image)。戻り値は内側。</summary>
        public static Image Framed(Transform parent, string name, float x, float y, float w, float h, Color frame, Color inner, float border = 4)
        {
            var outer = Panel(parent, name, x, y, w, h, frame);
            var innerImg = FillPanel(outer.transform, "Inner", inner, border);
            innerImg.raycastTarget = false;
            return outer;
        }

        public static void SetBorder(Image outer, Color color) => outer.color = color;

        /// <summary>画面全体を覆う半透明オーバーレイ(クリックを吸収する)。</summary>
        public static Image Overlay(Transform parent, string name, float alpha = 0.7f)
        {
            var img = FillPanel(parent, name, new Color(0, 0, 0, alpha));
            img.raycastTarget = true;
            return img;
        }

        public static void Clear(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--)
                Object.Destroy(t.GetChild(i).gameObject);
        }

        /// <summary>スライダー(0..1)。羊皮紙の設定パネルに合う見た目で作る。</summary>
        public static UnityEngine.UI.Slider Slider(Transform parent, string name, float x, float y, float w, float h, float value, UnityEngine.Events.UnityAction<float> onChanged)
        {
            var root = Rect(parent, name, x, y, w, h);
            var slider = root.gameObject.AddComponent<UnityEngine.UI.Slider>();

            // 溝(暗い線)
            float trackH = Mathf.Max(6f, h * 0.22f);
            var track = Panel(root, "Track", 0, (h - trackH) / 2, w, trackH, new Color(0.30f, 0.20f, 0.10f, 0.9f));
            track.raycastTarget = false;

            // 満ちている部分(金)
            var fillArea = Rect(root, "FillArea", 0, (h - trackH) / 2, w, trackH);
            var fill = Panel(fillArea, "Fill", 0, 0, w, trackH, FantasyUi.GoldTrim);
            fill.raycastTarget = false;
            var fillRt = fill.rectTransform;
            fillRt.anchorMin = Vector2.zero; fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = Vector2.zero; fillRt.offsetMax = Vector2.zero;

            // つまみ(金属の円)
            var handleArea = Rect(root, "HandleArea", h / 2, 0, w - h, h);
            var handle = Rect(handleArea, "Handle", 0, 0, h, h).gameObject.AddComponent<Image>();
            handle.sprite = Icons.Ring(FantasyUi.Gold, null);
            var handleRt = handle.rectTransform;
            handleRt.anchorMin = new Vector2(0, 0.5f); handleRt.anchorMax = new Vector2(0, 0.5f);
            handleRt.pivot = new Vector2(0.5f, 0.5f);
            handleRt.sizeDelta = new Vector2(h, 0);   // 高さは親に合わせる(Slider が縦のアンカーを 0..1 にするため)

            slider.fillRect = fillRt;
            slider.handleRect = handleRt;
            slider.targetGraphic = handle;
            slider.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
            slider.minValue = 0; slider.maxValue = 1;
            slider.SetValueWithoutNotify(value);
            slider.onValueChanged.AddListener(onChanged);
            return slider;
        }
    }
}

namespace CardGame.Unity.UI
{
    /// <summary>
    /// 1920×1080 の固定枠が常に画面内に収まるように CanvasScaler の基準辺を選ぶ。
    /// 画面が 16:9 より横長なら高さ基準(上下が切れない)、縦長寄りなら幅基準(左右が切れない)。
    /// 回転・ブラウザのリサイズにも追従する。
    /// </summary>
    public sealed class AspectFit : MonoBehaviour
    {
        private CanvasScaler _scaler = null!;
        private int _w, _h;

        private void Awake()
        {
            _scaler = GetComponent<CanvasScaler>();
            Apply();
        }

        private void Update()
        {
            if (Screen.width != _w || Screen.height != _h) Apply();
        }

        private void Apply()
        {
            _w = Screen.width;
            _h = Screen.height;
            float aspect = _h == 0 ? 1f : (float)_w / _h;
            _scaler.matchWidthOrHeight = aspect >= Ui.RefWidth / Ui.RefHeight ? 1f : 0f;
        }
    }
}

namespace CardGame.Unity.UI
{
    public static class UiInput
    {
        /// <summary>1 行のテキスト入力欄(uGUI InputField)。スマホではタップでキーボードが開く。</summary>
        public static InputField Field(Transform parent, string name, float x, float y, float w, float h, string placeholder, int fontSize = 32, int charLimit = 12)
        {
            var bg = Ui.Panel(parent, name, x, y, w, h, new Color(0.10f, 0.11f, 0.14f));
            var field = bg.gameObject.AddComponent<UnityEngine.UI.InputField>();
            var text = Ui.FillLabel(bg.transform, "Text", "", fontSize, TextAnchor.MiddleCenter, Ui.TextMain, FontStyle.Bold, 12);
            text.supportRichText = false;
            var ph = Ui.FillLabel(bg.transform, "Placeholder", placeholder, fontSize, TextAnchor.MiddleCenter, Ui.TextDim, FontStyle.Normal, 12);
            field.textComponent = text;
            field.placeholder = ph;
            field.characterLimit = charLimit;
            field.contentType = InputField.ContentType.Alphanumeric;
            field.caretWidth = 3;
            return field;
        }
    }
}

namespace CardGame.Unity.UI
{
    public static class UiTex
    {
        /// <summary>テクスチャ付きのパネル。tiled なら 1 タイル = tilePx ピクセルで敷き詰める。</summary>
        public static Image Panel(Transform parent, string name, float x, float y, float w, float h, Sprite sprite, Color tint, bool tiled = true, float tilePx = 256f)
        {
            var img = Ui.Panel(parent, name, x, y, w, h, tint);
            img.sprite = sprite;
            img.type = tiled ? Image.Type.Tiled : Image.Type.Simple;
            if (tiled) img.pixelsPerUnitMultiplier = sprite.rect.width / tilePx * (sprite.pixelsPerUnit / 100f);
            return img;
        }

        public static Image Fill(Transform parent, string name, Sprite sprite, Color tint, bool tiled = true, float tilePx = 256f, float margin = 0)
        {
            var img = Ui.FillPanel(parent, name, tint, margin);
            img.sprite = sprite;
            img.type = tiled ? Image.Type.Tiled : Image.Type.Simple;
            if (tiled) img.pixelsPerUnitMultiplier = sprite.rect.width / tilePx * (sprite.pixelsPerUnit / 100f);
            return img;
        }
    }
}
