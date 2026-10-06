#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace CardGame.Unity.UI
{
    /// <summary>
    /// フィールド案 A(酒場のテーブル)用の質感テクスチャを実行時にノイズで生成する。
    /// 木目 / 革 / 羊皮紙 / ビネット。すべてタイル可能(Repeat)。
    /// 画像素材が用意できたら Resources/Field/ の画像に差し替える。
    /// </summary>
    public static class Materials
    {
        private static readonly Dictionary<string, Sprite> Cache = new();

        public static Sprite Wood() => Get("wood", 512, 512, (u, v) =>
        {
            // 横方向に流れる木目: 低周波の縞 + 細かいノイズ
            float grain = Mathf.PerlinNoise(u * 2.0f + 13f, v * 18f + 7f);
            float rings = Mathf.Sin((v * 9f + Mathf.PerlinNoise(u * 3f, v * 3f) * 2.5f) * Mathf.PI) * 0.5f + 0.5f;
            float fine = Mathf.PerlinNoise(u * 40f, v * 400f) * 0.15f;
            float t = Mathf.Clamp01(grain * 0.5f + rings * 0.35f + fine);
            var dark = new Color(0.20f, 0.11f, 0.05f);
            var light = new Color(0.52f, 0.33f, 0.16f);
            return Color.Lerp(dark, light, t);
        }, tile: true);

        public static Sprite Leather(Color baseColor) => Get("leather:" + ColorUtility.ToHtmlStringRGB(baseColor), 256, 256, (u, v) =>
        {
            float n1 = Mathf.PerlinNoise(u * 30f + 3f, v * 30f + 9f);
            float n2 = Mathf.PerlinNoise(u * 120f + 31f, v * 120f + 17f);
            float crease = Mathf.PerlinNoise(u * 6f + 51f, v * 6f + 23f);
            float t = 0.72f + (n1 - 0.5f) * 0.25f + (n2 - 0.5f) * 0.18f + (crease - 0.5f) * 0.2f;
            var c = baseColor * Mathf.Clamp(t, 0.45f, 1.05f);
            c.a = 1;
            return c;
        }, tile: true);

        public static Sprite Parchment() => Get("parchment", 256, 256, (u, v) =>
        {
            float stain = Mathf.PerlinNoise(u * 5f + 71f, v * 5f + 29f);
            float fiber = Mathf.PerlinNoise(u * 90f + 5f, v * 90f + 41f);
            float t = 0.86f + (stain - 0.5f) * 0.18f + (fiber - 0.5f) * 0.08f;
            var c = new Color(0.93f, 0.85f, 0.66f) * Mathf.Clamp(t, 0.6f, 1.02f);
            c.a = 1;
            return c;
        }, tile: true);

        /// <summary>周辺減光(中央が透明、外側が黒)。画面全体に重ねて奥行きを出す。</summary>
        public static Sprite Vignette() => Get("vignette", 256, 256, (u, v) =>
        {
            float dx = (u - 0.5f) * 2f, dy = (v - 0.5f) * 2f;
            float r = Mathf.Sqrt(dx * dx * 0.7f + dy * dy);
            float a = Mathf.Clamp01((r - 0.55f) / 0.7f);
            return new Color(0, 0, 0, a * a * 0.85f);
        }, tile: false);

        /// <summary>柔らかい影(角丸の黒、外側に向けて透明)。パネルの下に敷く。</summary>
        public static Sprite SoftShadow() => Get("shadow", 64, 64, (u, v) =>
        {
            float dx = Mathf.Max(0, Mathf.Abs(u - 0.5f) * 2f - 0.6f) / 0.4f;
            float dy = Mathf.Max(0, Mathf.Abs(v - 0.5f) * 2f - 0.6f) / 0.4f;
            float d = Mathf.Sqrt(dx * dx + dy * dy);
            return new Color(0, 0, 0, Mathf.Clamp01(1f - d) * 0.6f);
        }, tile: false);

        private static Sprite Get(string key, int w, int h, System.Func<float, float, Color> shader, bool tile)
        {
            if (Cache.TryGetValue(key, out var s)) return s;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = tile ? TextureWrapMode.Mirror : TextureWrapMode.Clamp, // Mirror なら継ぎ目が出ない
            };
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                px[y * w + x] = shader((x + 0.5f) / w, (y + 0.5f) / h);
            tex.SetPixels32(px);
            tex.Apply();
            // タイル用は Image.Type.Tiled で使うので pixelsPerUnit を実寸に合わせる
            s = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            Cache[key] = s;
            return s;
        }
    }
}
