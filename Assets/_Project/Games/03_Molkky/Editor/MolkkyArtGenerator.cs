using System.IO;
using UnityEditor;
using UnityEngine;

namespace MiniGame.Molkky.Editor
{
    /// <summary>
    /// モルックのドット絵素材（ピン・棒・影・背景・キャラ）をコードから生成するエディタユーティリティ。
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

        /// <summary>キャラの仮素材の種類。MolkkyCharacterGenerator がこの並びでキャラを作る</summary>
        public static readonly string[] CharacterIds = { "Balance", "Power", "Precision", "Long", "Short", "PowerLong" };

        // 各素材のピクセル寸法。ピンは PinRackView の見た目の比率（幅0.4：高さ0.55）に合わせ、拡大時の歪みを小さくする
        private const int PinWidth = 16;
        private const int PinHeight = 22;
        private const int StickWidth = 32;
        private const int StickHeight = 8;
        private const int ShadowWidth = 32;
        private const int ShadowHeight = 12;
        private const int BackdropWidth = 160;
        private const int BackdropHeight = 96;
        private const int CharacterWidth = 16;
        private const int CharacterHeight = 24;

        /// <summary>背景の空の一番上の色。カメラの背景色もこれに合わせ、背景の上に隙間が出ても目立たないようにする</summary>
        public static readonly Color32 SkyTop = new Color32(120, 185, 235, 255);
        private static readonly Color32 SkyHorizon = new Color32(205, 230, 245, 255);
        private static readonly Color32 Cloud = new Color32(255, 255, 255, 235);

        // 遠くの木立は空に溶ける明るい色、手前の木立は濃い色にして奥行きを出す
        private static readonly Color32 FarTree = new Color32(110, 162, 122, 255);
        private static readonly Color32 FarTreeHighlight = new Color32(130, 180, 138, 255);
        private static readonly Color32 NearTree = new Color32(56, 116, 66, 255);
        private static readonly Color32 NearTreeHighlight = new Color32(82, 146, 84, 255);
        private static readonly Color32 Hedge = new Color32(46, 98, 56, 255);
        private static readonly Color32 HedgeTop = new Color32(70, 128, 72, 255);

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

        private static readonly Color32 Skin = new Color32(246, 206, 168, 255);
        private static readonly Color32 Pants = new Color32(60, 64, 88, 255);
        private static readonly Color32 Shoes = new Color32(40, 34, 30, 255);
        private static readonly Color32 CharacterOutline = new Color32(34, 30, 38, 255);

        /// <summary>
        /// キャラごとの服・髪の色と体の幅。CharacterIds と同じ並び。
        /// 席の色（赤青黄緑）と区別しやすいよう、服は少しくすんだ色にしている
        /// </summary>
        private static readonly (Color32 Shirt, Color32 Hair, int HalfWidth)[] CharacterLooks =
        {
            (new Color32(70, 150, 120, 255), new Color32(96, 60, 36, 255), 4),
            (new Color32(200, 90, 50, 255), new Color32(40, 36, 40, 255), 5),
            (new Color32(130, 100, 190, 255), new Color32(230, 196, 110, 255), 3),
            (new Color32(230, 160, 60, 255), new Color32(70, 46, 30, 255), 4),
            (new Color32(80, 120, 160, 255), new Color32(220, 220, 225, 255), 3),
            (new Color32(150, 60, 80, 255), new Color32(150, 60, 40, 255), 5),
        };

        // ピンの頭を斜めに切る深さ（ピクセル）
        private const float PinCutDepth = 3f;

        public static void GenerateAll()
        {
            if (!Directory.Exists(SpriteDirectory))
            {
                Directory.CreateDirectory(SpriteDirectory);
            }

            GeneratePropSprites();
            GenerateCharacterSprites();

            AssetDatabase.Refresh();
            Debug.Log($"[MolkkyArtGenerator] モルックの素材を生成しました: {SpriteDirectory}");
        }

        /// <summary>素材が未生成なら生成する（MolkkySceneBuilder から呼ばれる）</summary>
        public static void EnsureGenerated()
        {
            // 後から追加したキャラ素材が無い既存環境でも作り直されるよう、最後に作るキャラまで確認する
            if (Load(PinStandingName) == null || Load(BackdropName) == null ||
                Load(CharacterBackName(CharacterIds[CharacterIds.Length - 1])) == null)
            {
                GenerateAll();
            }
        }

        public static Sprite Load(string spriteName)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteDirectory}/{spriteName}.png");
        }

        public static string CharacterFrontName(string id) => $"Char_{id}_Front";

        public static string CharacterBackName(string id) => $"Char_{id}_Back";

        private static void GeneratePropSprites()
        {
            SaveSprite(PinStandingName, BuildPinStanding(), PinWidth, PinHeight, SpriteAlignment.Center);
            SaveSprite(PinFallenName, BuildPinFallen(), PinHeight, PinWidth, SpriteAlignment.Center);
            SaveSprite(StickName, BuildStick(), StickWidth, StickHeight, SpriteAlignment.Center);
            SaveSprite(ShadowName, BuildShadow(), ShadowWidth, ShadowHeight, SpriteAlignment.Center);
            // 下端中央をピボットにして地面の奥に立てる
            SaveSprite(BackdropName, BuildBackdrop(), BackdropWidth, BackdropHeight, SpriteAlignment.BottomCenter);
        }

        /// <summary>足元をピボットにして、ThrowerView が地面の立ち位置にそのまま置けるようにする</summary>
        private static void GenerateCharacterSprites()
        {
            for (int i = 0; i < CharacterIds.Length; i++)
            {
                SaveSprite(CharacterFrontName(CharacterIds[i]), BuildCharacter(CharacterLooks[i], true),
                    CharacterWidth, CharacterHeight, SpriteAlignment.BottomCenter);
                SaveSprite(CharacterBackName(CharacterIds[i]), BuildCharacter(CharacterLooks[i], false),
                    CharacterWidth, CharacterHeight, SpriteAlignment.BottomCenter);
            }
        }

        // ------------------------------------------------------------------
        // ピン（立っている）。頭を斜めに切った木の円柱で、上寄りに数字の面を置く
        // ------------------------------------------------------------------
        private static Color32[] BuildPinStanding()
        {
            const int width = PinWidth;
            const int height = PinHeight;
            var pixels = NewCanvas(width, height);

            for (int x = 0; x < width; x++)
            {
                // 本物のモルックのピンと同じく頭を斜めに切る（左が高く右が低い）
                int top = Mathf.RoundToInt(x * PinCutDepth / (width - 1));
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
            const int width = PinHeight;
            const int height = PinWidth;
            var pixels = NewCanvas(width, height);

            for (int y = 0; y < height; y++)
            {
                int right = width - 1 - Mathf.RoundToInt(y * PinCutDepth / (height - 1));
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
            const float lightEnd = 0.25f;
            const float shadeStart = 0.72f;

            float t = position / (float)(size - 1);
            if (t < lightEnd) return WoodLight;
            if (t > shadeStart) return WoodShade;
            return WoodBase;
        }

        // ------------------------------------------------------------------
        // 棒（モルック）。ピンより濃い木の色にして、ピンの中に入っても見失わないようにする
        // ------------------------------------------------------------------
        private static Color32[] BuildStick()
        {
            const int width = StickWidth;
            const int height = StickHeight;
            const int lightBottomY = 2;
            const int shadeTopY = 5;
            const int grainLength = 4;
            const int grainRarity = 5;
            const int endRingWidth = 3;

            var pixels = NewCanvas(width, height);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Color32 color = y <= lightBottomY ? StickLight : (y >= shadeTopY ? StickShade : StickBase);

                    // 木目。座標から決まるノイズで短い筋を入れる
                    if (y >= lightBottomY && y <= shadeTopY && Hash(x / grainLength, y) % grainRarity == 0) color = StickShade;

                    // 両端の年輪
                    if (x < endRingWidth || x >= width - endRingWidth) color = StickShade;

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
            const int width = ShadowWidth;
            const int height = ShadowHeight;
            // 1より大きくして、中心付近は不透明のまま縁だけを薄くする
            const float falloff = 1.3f;

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
                    byte alpha = (byte)(255f * Mathf.Clamp01(falloff * (1f - d)));
                    pixels[(height - 1 - y) * width + x] = new Color32(255, 255, 255, alpha);
                }
            }

            return pixels;
        }

        // ------------------------------------------------------------------
        // 背景（空・雲・遠くの木立・手前の木立・生け垣）
        // ------------------------------------------------------------------
        private static Color32[] BuildBackdrop()
        {
            const int width = BackdropWidth;
            const int height = BackdropHeight;
            var pixels = NewCanvas(width, height);

            FillSkyGradient(pixels, width, height);

            DrawCloud(pixels, width, height, 30, 22);
            DrawCloud(pixels, width, height, 104, 14);
            DrawCloud(pixels, width, height, 140, 34);

            DrawTreeLine(pixels, width, height, 66, 8, 7, FarTree, FarTreeHighlight, 1);
            DrawTreeLine(pixels, width, height, 76, 13, 10, NearTree, NearTreeHighlight, 7);

            DrawHedge(pixels, width, height);
            return pixels;
        }

        private static void FillSkyGradient(Color32[] pixels, int width, int height)
        {
            for (int y = 0; y < height; y++)
            {
                Color32 sky = Lerp(SkyTop, SkyHorizon, y / (float)(height - 1));
                FillRect(pixels, width, height, 0, y, width, 1, sky);
            }
        }

        private static void DrawCloud(Color32[] pixels, int width, int height, int cx, int cy)
        {
            FillCircle(pixels, width, height, cx, cy, 6f, Cloud);
            FillCircle(pixels, width, height, cx + 7, cy - 2, 7f, Cloud);
            FillCircle(pixels, width, height, cx + 14, cy, 5f, Cloud);
            FillRect(pixels, width, height, cx - 4, cy, 22, 5, Cloud);
        }

        /// <summary>丸い樹冠を横に並べ、その下を塗りつぶして木立にする。樹冠の高さは座標ノイズでばらつかせる</summary>
        private static void DrawTreeLine(Color32[] pixels, int width, int height, int baseY, int spacing, int radius,
            Color32 color, Color32 highlight, int seed)
        {
            const int heightJitter = 6;
            const int radiusJitter = 4;
            const int highlightOffset = 2;
            const float highlightRatio = 0.45f;

            for (int x = -spacing; x < width + spacing; x += spacing)
            {
                int cy = baseY - Hash(x, seed) % heightJitter;
                float r = radius + Hash(seed, x) % radiusJitter;
                FillCircle(pixels, width, height, x, cy, r, color);
                // 左上を明るくして、日が当たっているように見せる
                FillCircle(pixels, width, height, x - highlightOffset, cy - highlightOffset, r * highlightRatio, highlight);
            }

            FillRect(pixels, width, height, 0, baseY, width, height - baseY, color);
        }

        /// <summary>背景の最下段。上端だけ明るくして、地面との境目をはっきりさせる</summary>
        private static void DrawHedge(Color32[] pixels, int width, int height)
        {
            const int hedgeTopY = 90;
            const int hedgeHeight = 6;

            FillRect(pixels, width, height, 0, hedgeTopY, width, hedgeHeight, Hedge);
            FillRect(pixels, width, height, 0, hedgeTopY, width, 1, HedgeTop);
        }

        // ------------------------------------------------------------------
        // キャラ（仮素材）。頭・胴・腕・脚の単純な人型。正面は顔を、背中は後頭部（髪）を描く。
        // 背中は投擲ラインの手前に小さく出るので、服の色と体の幅で見分けられるようにしている
        // ------------------------------------------------------------------
        private static Color32[] BuildCharacter((Color32 Shirt, Color32 Hair, int HalfWidth) look, bool front)
        {
            const int width = CharacterWidth;
            const int height = CharacterHeight;
            var pixels = NewCanvas(width, height);

            DrawLegs(pixels, width, height);
            DrawBody(pixels, width, height, look.Shirt, look.HalfWidth);
            DrawHead(pixels, width, height, look.Hair, front);

            AddOutline(pixels, width, height, CharacterOutline);
            return pixels;
        }

        private static void DrawLegs(Color32[] pixels, int width, int height)
        {
            const int center = CharacterWidth / 2;
            const int legWidth = 2;
            const int legTopY = 17;
            const int legHeight = 6;
            const int shoeY = 22;

            FillRect(pixels, width, height, center - 3, legTopY, legWidth, legHeight, Pants);
            FillRect(pixels, width, height, center + 1, legTopY, legWidth, legHeight, Pants);
            FillRect(pixels, width, height, center - 3, shoeY, legWidth, 1, Shoes);
            FillRect(pixels, width, height, center + 1, shoeY, legWidth, 1, Shoes);
        }

        /// <summary>腕（肌）と胴（服）。胴の幅をキャラごとに変えて体格の違いを出す</summary>
        private static void DrawBody(Color32[] pixels, int width, int height, Color32 shirt, int halfWidth)
        {
            const int center = CharacterWidth / 2;
            const int armTopY = 10;
            const int armHeight = 6;
            const int torsoTopY = 9;
            const int torsoHeight = 8;

            FillRect(pixels, width, height, center - halfWidth - 1, armTopY, 1, armHeight, Skin);
            FillRect(pixels, width, height, center + halfWidth, armTopY, 1, armHeight, Skin);
            FillRect(pixels, width, height, center - halfWidth, torsoTopY, halfWidth * 2, torsoHeight, shirt);
        }

        /// <summary>正面は髪を上だけにして顔を見せ、背中は髪で全部覆う</summary>
        private static void DrawHead(Color32[] pixels, int width, int height, Color32 hair, bool front)
        {
            const int center = CharacterWidth / 2;
            const float headCenterY = 5f;
            const float headRadiusX = 3.6f;
            const float headRadiusY = 4f;
            const int eyeY = 5;

            FillEllipse(pixels, width, height, center, headCenterY, headRadiusX, headRadiusY, front ? Skin : hair);
            if (!front) return;

            FillRect(pixels, width, height, center - 4, 1, 8, 2, hair);
            SetPixel(pixels, width, height, center - 2, eyeY, CharacterOutline);
            SetPixel(pixels, width, height, center + 1, eyeY, CharacterOutline);
        }

        /// <summary>塗った部分を1ピクセルの縁で囲む。背景の芝の上でも輪郭が埋もれないようにする</summary>
        private static void AddOutline(Color32[] pixels, int width, int height, Color32 color)
        {
            var source = (Color32[])pixels.Clone();
            for (int i = 0; i < pixels.Length; i++)
            {
                if (source[i].a != 0) continue;

                int x = i % width;
                int y = i / width;
                bool touches = IsFilled(source, width, height, x - 1, y) || IsFilled(source, width, height, x + 1, y) ||
                               IsFilled(source, width, height, x, y - 1) || IsFilled(source, width, height, x, y + 1);
                if (touches) pixels[i] = color;
            }
        }

        private static bool IsFilled(Color32[] pixels, int width, int height, int x, int y)
        {
            if (x < 0 || x >= width || y < 0 || y >= height) return false;

            return pixels[y * width + x].a != 0;
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
