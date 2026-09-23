#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardGame.Unity.UI
{
    /// <summary>
    /// 画像アセットなしでアイコン・バッジを用意するため、実行時にピクセルを計算して Sprite を作る。
    /// - Badge: 円 / 六角形の土台(グラデーション + 暗い縁 + ハイライト)。色は生成時に焼き込む
    /// - Glyph: 剣 / ハート / 砂時計 / 盾(白塗り + 暗い縁取り)
    /// フェーズ5でイラスト素材が入ったら差し替える。
    /// </summary>
    public static class Icons
    {
        public enum Kind { Sword, Heart, Gem, Hourglass, Shield, Circle }

        private static readonly Dictionary<string, Sprite> Cache = new();
        private const int Size = 160;

        /// <summary>白い図形 + 暗い縁取りのグリフ(色は Image.color で付ける)。</summary>
        public static Sprite Glyph(Kind kind) => Build($"glyph:{kind}", (x, y) =>
        {
            float d = Distance(kind, x, y);
            return Shade(d, Color.white, new Color(0.10f, 0.08f, 0.08f, 1f), y);
        });

        /// <summary>
        /// 金属リングの円バッジ(バッジ案 C): 外側に鋼のリング、内側に色付きの艶あり円盤、中央に薄いエンボスのグリフ。
        /// </summary>
        public static Sprite Ring(Color color, Kind? glyph) => Build($"ring:{ColorUtility.ToHtmlStringRGBA(color)}:{glyph}", (x, y) =>
        {
            float r = Mathf.Sqrt(x * x + y * y);
            float px = 2f / Size;
            float outer = r - 0.97f;                 // リングの外周
            float innerEdge = r - 0.76f;             // リングと円盤の境
            // 鋼のリング: 斜めの光沢
            float sheen = Mathf.Clamp01(0.5f + 0.5f * Mathf.Sin((x - y) * 2.2f + 0.6f));
            var steel = Color.Lerp(new Color(0.42f, 0.43f, 0.47f), new Color(0.93f, 0.94f, 0.96f), sheen);
            // 色付き円盤: 上が明るく、上部にハイライト
            float t = Mathf.Clamp01((y + 1f) / 2f);
            var disc = Color.Lerp(color * 0.55f, color * 1.2f, t); disc.a = 1;
            float hl = Mathf.Clamp01(1f - Mathf.Sqrt(x * x + (y - 0.45f) * (y - 0.45f)) / 0.55f);
            disc = Color.Lerp(disc, Color.white, hl * 0.30f);
            // 薄いエンボスのグリフ(中央、少し小さく)
            if (glyph.HasValue)
            {
                float gd = Distance(glyph.Value, x / 0.55f, y / 0.55f) * 0.55f;
                float ga = Mathf.Clamp01(0.5f - gd / px);
                disc = Color.Lerp(disc, Color.white, ga * 0.28f);
            }
            var dark = new Color(0.10f, 0.09f, 0.10f, 1f);
            Color c;
            if (innerEdge < 0) c = Color.Lerp(dark, disc, Mathf.Clamp01(0.5f - (innerEdge + 0.05f) / px)); // 円盤(境に細い暗線)
            else c = Color.Lerp(dark, steel, Mathf.Clamp01(0.5f - (outer + 0.06f) / px));                 // リング(外周に暗い縁)
            c.a = Mathf.Clamp01(0.5f - outer / px);
            return c;
        });

        /// <summary>カボション宝石(リングなし、外縁は暗い線)。画像のリムと組み合わせて使う。</summary>
        public static Sprite Gem(Color color, Kind? glyph) => Build($"gem:{ColorUtility.ToHtmlStringRGBA(color)}:{glyph}", (x, y) =>
        {
            float r = Mathf.Sqrt(x * x + y * y);
            float px = 2f / Size;
            float t = Mathf.Clamp01(1f - r / 0.96f);
            var gem = Color.Lerp(color * 0.40f, color * 1.30f, Mathf.Pow(t, 0.75f)); gem.a = 1;
            float spec = Mathf.Clamp01(1f - Mathf.Sqrt((x + 0.32f) * (x + 0.32f) + (y - 0.40f) * (y - 0.40f)) / 0.30f);
            gem = Color.Lerp(gem, Color.white, Mathf.Pow(spec, 1.5f) * 0.85f);
            float refl = Mathf.Clamp01(1f - Mathf.Sqrt(x * x * 0.35f + (y + 0.62f) * (y + 0.62f)) / 0.34f);
            gem = Color.Lerp(gem, Color.white, refl * 0.22f);
            if (glyph.HasValue)
            {
                float gd = Distance(glyph.Value, x / 0.62f, y / 0.62f) * 0.62f;
                gem = Color.Lerp(gem, Color.white, Mathf.Clamp01(0.5f - gd / px) * 0.15f);
            }
            var dark = new Color(0.06f, 0.04f, 0.03f, 1f);
            var c = Color.Lerp(gem, dark, Mathf.Clamp01(0.5f - (0.96f - r - 0.03f) / px) * 0.85f);
            c.a = Mathf.Clamp01(0.5f - (r - 0.98f) / px);
            return c;
        });

        /// <summary>色付きの土台(円 / 六角形)。上が明るく下が暗いグラデーション、暗い縁、上部ハイライト。</summary>
        public static Sprite Badge(Kind shape, Color color) => Build($"badge:{shape}:{ColorUtility.ToHtmlStringRGBA(color)}", (x, y) =>
        {
            float d = shape == Kind.Gem ? Hexagon(x, y, 0.98f) : Circle(x, y, 0.96f);
            // 上下グラデーション
            float t = Mathf.Clamp01((y + 1f) / 2f);
            var fill = Color.Lerp(color * 0.62f, color * 1.15f, t);
            fill.a = 1;
            // 上部の艶
            float hl = Mathf.Clamp01(1f - Mathf.Sqrt(x * x + (y - 0.55f) * (y - 0.55f)) / 0.7f);
            fill = Color.Lerp(fill, Color.white, hl * 0.28f);
            return Shade(d, fill, new Color(0.12f, 0.09f, 0.06f, 1f), y, outlineW: 0.09f);
        });

        /// <summary>上が白・下が暗い縦グラデーション(Image.color で着色して背景に使う)。</summary>
        public static Sprite VerticalGradient() => Build("vgrad", (x, y) =>
        {
            float t = Mathf.Clamp01((y + 1f) / 2f);
            float v = Mathf.Lerp(0.35f, 1.0f, t);
            return new Color(v, v, v, 1f);
        });

        /// <summary>
        /// 輪郭に沿った柔らかい光(強調表示用)。形の縁(画像の 84% の位置)で最も明るく、内側は素早く、外側はゆっくり消える。
        /// ellipse = 楕円(場のフォロワー)、false = 角丸の四角(手札のカード)。
        /// </summary>
        public static Sprite GlowOutline(bool ellipse) => Build(ellipse ? "glow:e" : "glow:r", (x, y) =>
        {
            float d = ellipse ? Mathf.Sqrt(x * x + y * y) : Mathf.Pow(Mathf.Pow(Mathf.Abs(x), 10f) + Mathf.Pow(Mathf.Abs(y), 10f), 0.1f);
            float q = d / 0.84f - 1f;                       // 0 = 形の縁
            float a = q < 0 ? Mathf.Exp(-(q * q) / 0.004f) : Mathf.Exp(-(q * q) / 0.012f);
            return new Color(1f, 1f, 1f, Mathf.Clamp01(a));
        });

        /// <summary>白い塗りつぶしの角丸の四角(スペルの絵の窓。Mask 用)。</summary>
        public static Sprite RoundedRect() => Build("roundrect", (x, y) =>
        {
            float d = Mathf.Pow(Mathf.Pow(Mathf.Abs(x), 8f) + Mathf.Pow(Mathf.Abs(y), 8f), 1f / 8f);
            return new Color(1f, 1f, 1f, Mathf.Clamp01((1f - d) * Size / 2f + 0.5f));
        });

        /// <summary>白い塗りつぶしの円(矩形に合わせて伸ばせば楕円。Mask 用)。</summary>
        public static Sprite Ellipse() => Build("ellipse", (x, y) =>
        {
            float d = Mathf.Sqrt(x * x + y * y);
            return new Color(1f, 1f, 1f, Mathf.Clamp01((1f - d) * Size / 2f + 0.5f));
        });

        private static Sprite Build(string key, Func<float, float, Color> shader)
        {
            if (Cache.TryGetValue(key, out var s)) return s;
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color32[Size * Size];
            for (int y = 0; y < Size; y++)
            for (int x = 0; x < Size; x++)
            {
                float px = (x + 0.5f) / Size * 2 - 1;
                float py = (y + 0.5f) / Size * 2 - 1;
                pixels[y * Size + x] = shader(px, py);
            }
            tex.SetPixels32(pixels);
            tex.Apply();
            s = Sprite.Create(tex, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), Size);
            Cache[key] = s;
            return s;
        }

        /// <summary>符号付き距離 d から、塗り・縁取り・透明のいずれかの色を決める(1px アンチエイリアス)。</summary>
        private static Color Shade(float d, Color fill, Color outline, float y, float outlineW = 0.10f)
        {
            float px = 2f / Size; // 1px の幅(正規化座標)
            float aOuter = Mathf.Clamp01(0.5f - d / px);              // 縁の外側
            float aInner = Mathf.Clamp01(0.5f - (d + outlineW) / px); // 塗りの内側
            var c = Color.Lerp(outline, fill, aInner);
            c.a = aOuter;
            return c;
        }

        // ---- 形(符号付き距離の近似) ----

        private static float Distance(Kind kind, float x, float y) => kind switch
        {
            Kind.Sword => Sword(x, y),
            Kind.Heart => Heart(x, y),
            Kind.Gem => Hexagon(x, y, 0.9f),
            Kind.Hourglass => Hourglass(x, y),
            Kind.Shield => Shield(x, y),
            Kind.Circle => Circle(x, y, 0.9f),
            _ => 1f,
        };

        private static float Circle(float x, float y, float r) => Mathf.Sqrt(x * x + y * y) - r;

        private static float Segment(float x, float y, float ax, float ay, float bx, float by, float r)
        {
            float px = x - ax, py = y - ay, bx2 = bx - ax, by2 = by - ay;
            float h = Mathf.Clamp01((px * bx2 + py * by2) / (bx2 * bx2 + by2 * by2));
            float dx = px - bx2 * h, dy = py - by2 * h;
            return Mathf.Sqrt(dx * dx + dy * dy) - r;
        }

        private static float Sword(float x, float y)
        {
            float blade = Segment(x, y, -0.12f, -0.12f, 0.66f, 0.66f, 0.14f);
            float tip = Segment(x, y, 0.55f, 0.55f, 0.80f, 0.80f, 0.03f);
            float guard = Segment(x, y, -0.48f, 0.08f, -0.08f, -0.48f, 0.10f);
            float grip = Segment(x, y, -0.26f, -0.26f, -0.60f, -0.60f, 0.11f);
            float pommel = Circle(x + 0.68f, y + 0.68f, 0.17f);
            return Mathf.Min(Mathf.Min(blade, tip), Mathf.Min(Mathf.Min(guard, grip), pommel));
        }

        private static float Heart(float x, float y)
        {
            y += 0.1f;
            float c1 = Circle(x + 0.38f, y - 0.3f, 0.42f);
            float c2 = Circle(x - 0.38f, y - 0.3f, 0.42f);
            float tri = Mathf.Max(y - 0.3f, Mathf.Max(-(y + 0.85f), Mathf.Abs(x) * 1.45f - (y + 0.85f)));
            return Mathf.Min(Mathf.Min(c1, c2), tri);
        }

        private static float Hexagon(float x, float y, float r)
        {
            x = Mathf.Abs(x); y = Mathf.Abs(y);
            return Mathf.Max(x * 0.866f + y * 0.5f, y) - r * 0.87f;
        }

        private static float Hourglass(float x, float y)
        {
            float top = Mathf.Max(-y, Mathf.Abs(x) - (0.15f + y * 0.75f));
            top = Mathf.Max(top, y - 0.75f);
            float bottom = Mathf.Max(y, Mathf.Abs(x) - (0.15f - y * 0.75f));
            bottom = Mathf.Max(bottom, -y - 0.75f);
            float bar1 = Segment(x, y, -0.7f, 0.8f, 0.7f, 0.8f, 0.08f);
            float bar2 = Segment(x, y, -0.7f, -0.8f, 0.7f, -0.8f, 0.08f);
            return Mathf.Min(Mathf.Min(top, bottom), Mathf.Min(bar1, bar2));
        }

        private static float Shield(float x, float y)
        {
            float w = y > 0 ? 0.75f : 0.75f - (-y) * 0.8f;
            return Mathf.Max(Mathf.Abs(x) - w, Mathf.Max(y - 0.8f, -y - 0.9f));
        }
    }
}
