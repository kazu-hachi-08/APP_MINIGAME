using System.IO;
using UnityEditor;
using UnityEngine;

namespace MiniGame.Molkky.Editor
{
    /// <summary>
    /// モルックのドット絵素材（ピン・棒・影・背景）をコードから生成するエディタユーティリティ（§14 Phase 8）。
    /// 外部素材に依存せず同じ絵をいつでも作り直せるよう、卓球の TableTennisArtGenerator と同じ方針でコード生成にしている。
    /// 生成物は通常のPNGなので、手描き素材へ差し替えても構わない（表示側は大きさを地面単位で合わせる）。
    /// </summary>
    public static class MolkkyArtGenerator
    {
        public const string SpriteDirectory = "Assets/_Project/Games/03_Molkky/Sprites";

        /// <summary>1ワールド単位あたりのピクセル数。表示サイズは各Viewが指定するため見た目の密度だけの意味を持つ</summary>
        private const int PixelsPerUnit = 32;

        public const string PinStandingName = "Pin_Standing";
        public const string PinFallenName = "Pin_Fallen";
        public const string StickName = "Stick";
        public const string ShadowName = "Shadow";
        public const string BackdropName = "Backdrop";

        /// <summary>背景の空の一番上の色。カメラの背景色もこれに合わせ、背景の上に隙間が出ても目立たないようにする</summary>
        public static readonly Color32 SkyTop = new Color32(120, 185, 235, 255);
        private static readonly Color32 SkyHorizon = new Color32(205, 230, 245, 255);

        private static readonly Color32 WoodOutline = new Color32(110, 74, 40, 255);
        private static readonly Color32 WoodBase = new Color32(226, 188, 132, 255);
        private static readonly Color32 WoodLight = new Color32(244, 216, 170, 255);
        private static readonly Color32 WoodShade = new Color32(188, 146, 94, 255);
        private static readonly Color32 WoodCut = new Color32(250, 232, 196, 255);
        // 数字を書く面。数字（濃い茶色）との差を大きくして、小さい画面でも読めるようにする
        private static readonly Color32 NumberPlate = new Color32(255, 248, 230, 255);

        private static readonly Color32 StickBase = new Color32(196, 138, 80, 255);
        private static readonly Color32 StickLight = new Color32(222, 170, 110, 255);
        private static readonly Color32 StickShade = new Color32(156, 104, 56, 255);
        private static readonly Color32 StickOutline = new Color32(96, 60, 30, 255);

        private static readonly Color32 Transparent = new Color32(0, 0, 0, 0);

        [MenuItem("Tools/MiniGame/Generate Molkky Art", false, 5)]
        public static void GenerateAll()
        {
            if (!Directory.Exists(SpriteDirectory))
            {
                Directory.CreateDirectory(SpriteDirectory);
            }

            // ピンは PinRackView の見た目の比率（幅0.4：高さ0.55）に合わせた大きさで描き、拡大時の歪みを小さくする
            SaveSprite(PinStandingName, BuildPinStanding(), 16, 22, SpriteAlignment.Center);
            SaveSprite(PinFallenName, BuildPinFallen(), 22, 16, SpriteAlignment.Center);
            SaveSprite(StickName, BuildStick(), 32, 8, SpriteAlignment.Center);
            SaveSprite(ShadowName, BuildShadow(), 32, 12, SpriteAlignment.Center);
            SaveSprite(BackdropName, BuildBackdrop(), 160, 96, SpriteAlignment.BottomCenter);

            AssetDatabase.Refresh();
            Debug.Log($"[MolkkyArtGenerator] モルックの素材を生成しました: {SpriteDirectory}");
        }

        /// <summary>素材が未生成なら生成する（MolkkySceneBuilder から呼ばれる）</summary>
        public static void EnsureGenerated()
        {
            if (Load(PinStandingName) == null || Load(BackdropName) == null)
            {
                GenerateAll();
            }
        }

        public static Sprite Load(string spriteName)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteDirectory}/{spriteName}.png");
        }

        // ------------------------------------------------------------------
        // ピン（立っている）。頭を斜めに切った木の円柱で、上寄りに数字の面を置く
        // ------------------------------------------------------------------
        private static Color32[] BuildPinStanding()
        {
            const int width = 16;
            const int height = 22;
            var pixels = NewCanvas(width, height);

            for (int x = 0; x < width; x++)
            {
                // 本物のモルックのピンと同じく頭を斜めに切る（左が高く右が低い）
                int top = Mathf.RoundToInt(x * 3f / (width - 1));
                for (int y = top; y < height; y++)
                {
                    Color32 color = CylinderShade(x, width);
                    if (y <= top + 1) color = WoodCut; // 斜めの切り口
                    if (x == 0 || x == width - 1 || y == top || y == height - 1) color = WoodOutline;
                    SetPixel(pixels, width, height, x, y, color);
                }
            }

            // 数字の面は PinView の数字位置（高さの72%）に合わせる
            FillEllipse(pixels, width, height, 8f, 6.5f, 5.5f, 4.2f, NumberPlate);
            return pixels;
        }

        // ------------------------------------------------------------------
        // ピン（倒れた）。頭（斜めの切り口）を右に向け、中央に数字の面を置く
        // ------------------------------------------------------------------
        private static Color32[] BuildPinFallen()
        {
            const int width = 22;
            const int height = 16;
            var pixels = NewCanvas(width, height);

            for (int y = 0; y < height; y++)
            {
                int right = width - 1 - Mathf.RoundToInt(y * 3f / (height - 1));
                for (int x = 0; x <= right; x++)
                {
                    Color32 color = CylinderShade(y, height);
                    if (x >= right - 1) color = WoodCut;
                    if (y == 0 || y == height - 1 || x == 0 || x == right) color = WoodOutline;
                    SetPixel(pixels, width, height, x, y, color);
                }
            }

            FillEllipse(pixels, width, height, 10.5f, 8f, 5f, 5f, NumberPlate);
            return pixels;
        }

        /// <summary>円柱の断面方向の陰影。片側を明るく、反対側を暗くして丸みを出す</summary>
        private static Color32 CylinderShade(int position, int size)
        {
            float t = position / (float)(size - 1);
            if (t < 0.25f) return WoodLight;
            if (t > 0.72f) return WoodShade;
            return WoodBase;
        }

        // ------------------------------------------------------------------
        // 棒（モルック）。ピンより濃い木の色にして、ピンの中に入っても見失わないようにする
        // ------------------------------------------------------------------
        private static Color32[] BuildStick()
        {
            const int width = 32;
            const int height = 8;
            var pixels = NewCanvas(width, height);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Color32 color = y <= 2 ? StickLight : (y >= 5 ? StickShade : StickBase);

                    // 木目。座標から決まるノイズで短い筋を入れる
                    if (y >= 2 && y <= 5 && Hash(x / 4, y) % 5 == 0) color = StickShade;

                    // 両端の年輪
                    if (x <= 2 || x >= width - 3) color = StickShade;

                    bool edge = y == 0 || y == height - 1 || x == 0 || x == width - 1;
                    // 角を1ピクセル削って丸みを出す
                    bool corner = (x == 0 || x == width - 1) && (y == 0 || y == height - 1);
                    if (corner) continue;
                    if (edge) color = StickOutline;

                    SetPixel(pixels, width, height, x, y, color);
                }
            }

            return pixels;
        }

        // ------------------------------------------------------------------
        // 影。白で描き、StickView 側の色（半透明の黒）を乗算して使う
        // ------------------------------------------------------------------
        private static Color32[] BuildShadow()
        {
            const int width = 32;
            const int height = 12;
            var pixels = NewCanvas(width, height);
            float cx = width * 0.5f;
            float cy = height * 0.5f;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float dx = (x + 0.5f - cx) / cx;
                    float dy = (y + 0.5f - cy) / cy;
                    float d = dx * dx + dy * dy;
                    if (d > 1f) continue;

                    // 中心ほど濃く、縁に向かって薄くする
                    byte alpha = (byte)(255f * Mathf.Clamp01(1.3f * (1f - d)));
                    pixels[(height - 1 - y) * width + x] = new Color32(255, 255, 255, alpha);
                }
            }

            return pixels;
        }

        // ------------------------------------------------------------------
        // 背景（空・雲・遠くの木立・手前の木立・生け垣）。下端中央をピボットにして地面の奥に立てる
        // ------------------------------------------------------------------
        private static Color32[] BuildBackdrop()
        {
            const int width = 160;
            const int height = 96;
            var pixels = NewCanvas(width, height);

            for (int y = 0; y < height; y++)
            {
                Color32 sky = Lerp(SkyTop, SkyHorizon, y / (float)(height - 1));
                FillRect(pixels, width, height, 0, y, width, 1, sky);
            }

            DrawCloud(pixels, width, height, 30, 22);
            DrawCloud(pixels, width, height, 104, 14);
            DrawCloud(pixels, width, height, 140, 34);

            // 遠くの木立は空に溶ける明るい色、手前の木立は濃い色にして奥行きを出す
            DrawTreeLine(pixels, width, height, 66, 8, 7, new Color32(110, 162, 122, 255), new Color32(130, 180, 138, 255), 1);
            DrawTreeLine(pixels, width, height, 76, 13, 10, new Color32(56, 116, 66, 255), new Color32(82, 146, 84, 255), 7);

            FillRect(pixels, width, height, 0, 90, width, 6, new Color32(46, 98, 56, 255));
            FillRect(pixels, width, height, 0, 90, width, 1, new Color32(70, 128, 72, 255));
            return pixels;
        }

        private static void DrawCloud(Color32[] pixels, int width, int height, int cx, int cy)
        {
            var cloud = new Color32(255, 255, 255, 235);
            FillCircle(pixels, width, height, cx, cy, 6f, cloud);
            FillCircle(pixels, width, height, cx + 7, cy - 2, 7f, cloud);
            FillCircle(pixels, width, height, cx + 14, cy, 5f, cloud);
            FillRect(pixels, width, height, cx - 4, cy, 22, 5, cloud);
        }

        /// <summary>丸い樹冠を横に並べ、その下を塗りつぶして木立にする。樹冠の高さは座標ノイズでばらつかせる</summary>
        private static void DrawTreeLine(Color32[] pixels, int width, int height, int baseY, int spacing, int radius,
            Color32 color, Color32 highlight, int seed)
        {
            for (int x = -spacing; x < width + spacing; x += spacing)
            {
                int cy = baseY - Hash(x, seed) % 6;
                float r = radius + Hash(seed, x) % 4;
                FillCircle(pixels, width, height, x, cy, r, color);
                // 左上を明るくして、日が当たっているように見せる
                FillCircle(pixels, width, height, x - 2, cy - 2, r * 0.45f, highlight);
            }

            FillRect(pixels, width, height, 0, baseY, width, height - baseY, color);
        }

        // ------------------------------------------------------------------
        // 描画ヘルパー（x,yは左上原点。Texture2Dは左下原点のため SetPixel で反転する）
        // ------------------------------------------------------------------
        private static Color32[] NewCanvas(int width, int height)
        {
            var pixels = new Color32[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Transparent;
            }
            return pixels;
        }

        private static void SetPixel(Color32[] pixels, int width, int height, int x, int y, Color32 color)
        {
            if (x < 0 || x >= width || y < 0 || y >= height) return;
            if (color.a == 0) return;

            int index = (height - 1 - y) * width + x;

            if (color.a < 255)
            {
                Color32 baseColor = pixels[index];
                float alpha = color.a / 255f;
                pixels[index] = new Color32(
                    (byte)(color.r * alpha + baseColor.r * (1f - alpha)),
                    (byte)(color.g * alpha + baseColor.g * (1f - alpha)),
                    (byte)(color.b * alpha + baseColor.b * (1f - alpha)),
                    (byte)Mathf.Max(color.a, baseColor.a));
                return;
            }

            pixels[index] = color;
        }

        private static void FillRect(Color32[] pixels, int width, int height, int x0, int y0, int w, int h, Color32 color)
        {
            for (int y = y0; y < y0 + h; y++)
            {
                for (int x = x0; x < x0 + w; x++)
                {
                    SetPixel(pixels, width, height, x, y, color);
                }
            }
        }

        private static void FillCircle(Color32[] pixels, int width, int height, int cx, int cy, float radius, Color32 color)
        {
            FillEllipse(pixels, width, height, cx, cy, radius, radius, color);
        }

        private static void FillEllipse(Color32[] pixels, int width, int height, float cx, float cy, float rx, float ry, Color32 color)
        {
            for (int y = Mathf.FloorToInt(cy - ry) - 1; y <= Mathf.CeilToInt(cy + ry) + 1; y++)
            {
                for (int x = Mathf.FloorToInt(cx - rx) - 1; x <= Mathf.CeilToInt(cx + rx) + 1; x++)
                {
                    float dx = (x + 0.5f - cx) / rx;
                    float dy = (y + 0.5f - cy) / ry;
                    if (dx * dx + dy * dy <= 1f)
                    {
                        SetPixel(pixels, width, height, x, y, color);
                    }
                }
            }
        }

        private static Color32 Lerp(Color32 from, Color32 to, float t)
        {
            return Color32.Lerp(from, to, Mathf.Clamp01(t));
        }

        /// <summary>毎回同じ絵になるよう、乱数ではなく座標から決まるノイズを使う</summary>
        private static int Hash(int x, int y)
        {
            int value = x * 73856093 ^ y * 19349663;
            return Mathf.Abs(value % 1000);
        }

        // ------------------------------------------------------------------
        // 保存とインポート設定
        // ------------------------------------------------------------------
        private static void SaveSprite(string spriteName, Color32[] pixels, int width, int height, SpriteAlignment alignment)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.SetPixels32(pixels);
            texture.Apply();

            string path = $"{SpriteDirectory}/{spriteName}.png";
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            ConfigureImporter(path, alignment);
        }

        private static void ConfigureImporter(string path, SpriteAlignment alignment)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.filterMode = FilterMode.Point; // ドット絵をぼかさない
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteAlignment = (int)alignment;
            importer.SetTextureSettings(settings);

            importer.SaveAndReimport();
        }
    }
}
