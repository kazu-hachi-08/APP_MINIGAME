using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MiniGame.Golf.Editor
{
    /// <summary>
    /// 地面タイルの画像を手続き的に作る。
    /// 色だけでなく模様（芝の刈り目・砂粒・波）でも塗り分け、小さい画面でもライの違いが分かるようにする。
    /// 画像は「無ければ作る」だけなので、あとで手描きの PNG に差し替えても上書きしない。
    /// </summary>
    public static class GolfTileArtBuilder
    {
        private const string ArtDirectory = "Assets/_Project/Games/04_Golf/Tiles/Art";

        // 1タイル＝1ユニット＝16ピクセル。ドット絵風にして、縮小表示でも模様をくっきり見せる
        private const int TileSize = 16;

        // 波の周期（ピクセル）はタイルの大きさで割り切れる値にして、タイルのつなぎ目で模様を途切れさせない
        private const int WaterFrameCount = 4;
        private const int WavePeriod = 8;
        private const int WaveLength = 3;

        // コースの外側（カメラの背景）と揃える色。OB タイルとの境目を目立たせない
        public static readonly Color OutOfBoundsColor = new Color(0.16f, 0.32f, 0.2f);

        // 明るさの順を ラフ < フェアウェイ < ティー < グリーン にし、色だけでもライを見分けられるようにする
        private static readonly Color RoughColor = new Color(0.2f, 0.46f, 0.2f);
        private static readonly Color FairwayColor = new Color(0.42f, 0.72f, 0.3f);
        private static readonly Color TeeColor = new Color(0.52f, 0.8f, 0.36f);
        private static readonly Color GreenColor = new Color(0.64f, 0.9f, 0.46f);
        private static readonly Color BunkerColor = new Color(0.93f, 0.84f, 0.6f);
        private static readonly Color WaterColor = new Color(0.2f, 0.47f, 0.85f);
        private static readonly Color WaveColor = new Color(0.62f, 0.8f, 1f);

        // 模様の強さ（明るさの足し引き）
        private const float RoughNoise = 0.04f;
        private const float RoughTuft = 0.08f;
        private const float FairwayStripe = 0.035f;
        private const float TeeChecker = 0.03f;
        private const float GreenStripe = 0.015f;
        private const float BunkerGrain = 0.12f;
        private const float OutOfBoundsBlotch = 0.04f;
        private const float FineNoise = 0.015f;

        // 刈り目・市松の幅（ピクセル）
        private const int TeeCheckerSize = 4;
        private const int GreenStripeWidth = 4;
        private const int BlotchSize = 4;

        // ノイズの値がこの範囲の外にあるピクセルだけ、芝の房や砂粒として目立たせる
        private const float SpeckleLow = 0.1f;
        private const float SpeckleHigh = 0.88f;

        public static Sprite EnsureSprite(GroundType type)
        {
            return EnsureSprite($"Ground_{type}", (x, y) => Pixel(type, x, y));
        }

        /// <summary>池の波のコマ。波の周期ぶんをコマ数で割って横にずらし、最後のコマから最初へ自然につなげる</summary>
        public static Sprite[] EnsureWaterFrames()
        {
            var frames = new Sprite[WaterFrameCount];
            for (int i = 0; i < WaterFrameCount; i++)
            {
                int frame = i;
                frames[i] = EnsureSprite($"Ground_Water_{i}", (x, y) => Water(x, y, frame));
            }

            return frames;
        }

        private static Color Pixel(GroundType type, int x, int y)
        {
            switch (type)
            {
                case GroundType.Tee: return Tee(x, y);
                case GroundType.Fairway: return Fairway(x, y);
                case GroundType.Rough: return Rough(x, y);
                case GroundType.Bunker: return Bunker(x, y);
                case GroundType.Green: return Green(x, y);
                case GroundType.Water: return Water(x, y, 0);
                default: return OutOfBounds(x, y);
            }
        }

        /// <summary>1タイルに明暗1組の刈り目。縦に並ぶと横縞になり、フェアウェイらしく見える</summary>
        private static Color Fairway(int x, int y)
        {
            float stripe = y < TileSize / 2 ? FairwayStripe : -FairwayStripe;
            return Shade(FairwayColor, stripe + Jitter(x, y, 1, FineNoise));
        }

        /// <summary>ラフは芝の房（明暗の点）を多くして、フェアウェイより荒れて見せる</summary>
        private static Color Rough(int x, int y)
        {
            float n = Noise(x, y, 2);
            float tuft = n > SpeckleHigh ? RoughTuft : n < SpeckleLow ? -RoughTuft : 0f;
            return Shade(RoughColor, tuft + Jitter(x, y, 3, RoughNoise));
        }

        /// <summary>ティーは市松模様にして、似た明るさのフェアウェイ・グリーンと区別する</summary>
        private static Color Tee(int x, int y)
        {
            bool even = (x / TeeCheckerSize + y / TeeCheckerSize) % 2 == 0;
            return Shade(TeeColor, (even ? TeeChecker : -TeeChecker) + Jitter(x, y, 4, FineNoise));
        }

        /// <summary>グリーンは細かく薄い刈り目だけにして、一番なめらかに見せる</summary>
        private static Color Green(int x, int y)
        {
            bool light = y / GreenStripeWidth % 2 == 0;
            return Shade(GreenColor, light ? GreenStripe : -GreenStripe);
        }

        private static Color Bunker(int x, int y)
        {
            float n = Noise(x, y, 5);
            float grain = n > SpeckleHigh ? -BunkerGrain : n < SpeckleLow ? BunkerGrain * 0.5f : 0f;
            return Shade(BunkerColor, grain + Jitter(x, y, 6, FineNoise));
        }

        /// <summary>波は8ピクセルおきの行に短い線を置き、行ごとに半周期ずらしてうろこ状にする</summary>
        private static Color Water(int x, int y, int frame)
        {
            int row = y / WavePeriod;
            int shifted = x + frame * (WavePeriod / WaterFrameCount) + row * (WavePeriod / 2);
            bool wave = y % WavePeriod == 0 && shifted % WavePeriod < WaveLength;
            return wave ? WaveColor : Shade(WaterColor, Jitter(x, y, 7, FineNoise));
        }

        /// <summary>OB は暗い林床。大きめのまだら模様で、コースの中ではないことを示す</summary>
        private static Color OutOfBounds(int x, int y)
        {
            float blotch = Jitter(x / BlotchSize, y / BlotchSize, 8, OutOfBoundsBlotch);
            return Shade(OutOfBoundsColor, blotch + Jitter(x, y, 9, FineNoise));
        }

        private static Color Shade(Color color, float amount)
        {
            return new Color(Mathf.Clamp01(color.r + amount), Mathf.Clamp01(color.g + amount),
                Mathf.Clamp01(color.b + amount), 1f);
        }

        private static float Jitter(int x, int y, int seed, float amplitude)
        {
            return (Noise(x, y, seed) - 0.5f) * 2f * amplitude;
        }

        /// <summary>座標から決まる疑似乱数（0〜1）。毎回同じ画像になり、Git に余計な差分を出さない</summary>
        private static float Noise(int x, int y, int seed)
        {
            unchecked
            {
                int h = x * 374761393 + y * 668265263 + seed * 1442695041;
                h = (h ^ (h >> 13)) * 1274126177;
                h ^= h >> 16;
                return (h & 0xFFFF) / 65535f;
            }
        }

        private static Sprite EnsureSprite(string name, Func<int, int, Color> pixel)
        {
            string path = $"{ArtDirectory}/{name}.png";
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null) return sprite;

            if (!Directory.Exists(ArtDirectory)) Directory.CreateDirectory(ArtDirectory);

            var texture = new Texture2D(TileSize, TileSize, TextureFormat.RGBA32, false);
            for (int y = 0; y < TileSize; y++)
            {
                for (int x = 0; x < TileSize; x++)
                {
                    texture.SetPixel(x, y, pixel(x, y));
                }
            }

            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = TileSize;
            // ドットをぼかさず、タイル同士の境目もにじませない
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
