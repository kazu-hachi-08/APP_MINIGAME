using System.IO;
using UnityEditor;
using UnityEngine;

namespace MiniGame.TableTennis.Editor
{
    /// <summary>
    /// 卓球ゲームのドット絵素材（ボール・ラケット・キャラクター・台の質感）をコードから生成するエディタユーティリティ。
    /// 外部素材に依存せず同じ絵をいつでも作り直せるようにするためコード生成にしている
    /// （第1弾の SoccerArtGenerator と同じ方針）。生成物は通常のPNGなので手描き素材へ差し替えても構わない。
    /// </summary>
    public static class TableTennisArtGenerator
    {
        public const string SpriteDirectory = "Assets/_Project/Games/02_TableTennis/Sprites";

        /// <summary>1ワールド単位あたりのピクセル数。表示サイズは各Viewが指定するため見た目の密度だけの意味を持つ</summary>
        public const int PixelsPerUnit = 32;

        // 生成物の名前（SceneBuilder から参照する）
        public const string BallName = "Ball";
        public const string ShadowName = "Shadow";
        public const string PlayerRacketName = "Racket_Player";
        public const string NpcRacketName = "Racket_Npc";
        public const string PlayerCharacterName = "Character_Player";
        public const string NpcCharacterName = "Character_Npc";
        public const string TableSurfaceName = "TableSurface";
        public const string NetName = "Net";
        public const string BounceRingName = "BounceRing";

        private static readonly Color32 Transparent = new Color32(0, 0, 0, 0);

        [MenuItem("Tools/MiniGame/Generate Table Tennis Art", false, 4)]
        public static void GenerateAll()
        {
            if (!Directory.Exists(SpriteDirectory))
            {
                Directory.CreateDirectory(SpriteDirectory);
            }

            SaveSprite(BallName, BuildBall(), 24, 24, SpriteAlignment.Center, repeat: false);
            SaveSprite(ShadowName, BuildShadow(), 24, 24, SpriteAlignment.Center, repeat: false);

            SaveSprite(PlayerRacketName, BuildRacket(new Color32(206, 62, 58, 255)), 40, 48,
                SpriteAlignment.Center, repeat: false);
            SaveSprite(NpcRacketName, BuildRacket(new Color32(48, 52, 64, 255)), 40, 48,
                SpriteAlignment.Center, repeat: false);

            SaveSprite(PlayerCharacterName, BuildCharacter(back: true, shirt: new Color32(214, 84, 76, 255)),
                48, 64, SpriteAlignment.BottomCenter, repeat: false);
            SaveSprite(NpcCharacterName, BuildCharacter(back: false, shirt: new Color32(70, 118, 208, 255)),
                48, 64, SpriteAlignment.BottomCenter, repeat: false);

            // 台とネットはメッシュへ貼るので繰り返し可能にする
            SaveSprite(TableSurfaceName, BuildTableSurface(), 64, 64, SpriteAlignment.Center, repeat: true);
            SaveSprite(NetName, BuildNet(), 32, 32, SpriteAlignment.Center, repeat: true);

            SaveSprite(BounceRingName, BuildBounceRing(), 32, 32, SpriteAlignment.Center, repeat: false);

            AssetDatabase.Refresh();
            Debug.Log($"[TableTennisArtGenerator] 卓球の素材を生成しました: {SpriteDirectory}");
        }

        /// <summary>素材が未生成なら生成する（TableTennisSceneBuilder から呼ばれる）</summary>
        public static void EnsureGenerated()
        {
            if (Load(BallName) == null)
            {
                GenerateAll();
            }
        }

        public static Sprite Load(string spriteName)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteDirectory}/{spriteName}.png");
        }

        // ------------------------------------------------------------------
        // ボール（回転が読み取れるよう縫い目を入れる）
        // ------------------------------------------------------------------
        private static Color32[] BuildBall()
        {
            const int size = 24;
            const float radius = 11f;
            var pixels = NewCanvas(size, size);

            var bright = new Color32(255, 252, 238, 255);
            var shade = new Color32(214, 200, 170, 255);
            var edge = new Color32(150, 138, 116, 255);
            var seam = new Color32(228, 96, 80, 255);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Distance(x, y, size / 2, size / 2);
                    if (distance > radius) continue;

                    // 左上から光が当たっているように見せる
                    float light = Mathf.Clamp01(1f - Distance(x, y, size / 2 - 3, size / 2 - 3) / (radius * 1.8f));
                    Color32 color = Lerp(shade, bright, light);

                    if (distance > radius - 1.2f) color = edge;

                    // 別の円の輪郭でボールを横切らせ、縫い目（回転の目印）にする
                    if (distance < radius - 1.2f && Mathf.Abs(Distance(x, y, size / 2 - 8, size / 2) - 13f) < 1.1f)
                    {
                        color = seam;
                    }

                    SetPixel(pixels, size, size, x, y, color);
                }
            }

            return pixels;
        }

        /// <summary>中心ほど濃い影。高さ表現で縮小・減光して使う</summary>
        private static Color32[] BuildShadow()
        {
            const int size = 24;
            const float radius = 11f;
            var pixels = NewCanvas(size, size);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Distance(x, y, size / 2, size / 2);
                    if (distance > radius) continue;

                    byte alpha = (byte)Mathf.RoundToInt(255f * Mathf.Clamp01(1f - distance / radius));
                    SetPixel(pixels, size, size, x, y, new Color32(0, 0, 0, alpha));
                }
            }

            return pixels;
        }

        // ------------------------------------------------------------------
        // ラケット（ラバー面＋グリップ）
        // ------------------------------------------------------------------
        private static Color32[] BuildRacket(Color32 rubber)
        {
            const int width = 40;
            const int height = 48;
            var pixels = NewCanvas(width, height);

            var wood = new Color32(196, 150, 96, 255);
            var woodDark = new Color32(146, 106, 62, 255);
            var outline = new Color32(38, 34, 44, 255);

            // グリップ（先に描いてラバー面を上に重ねる）
            FillRect(pixels, width, height, 16, 28, 8, 19, wood);
            FillRect(pixels, width, height, 16, 28, 3, 19, woodDark);
            OutlineRect(pixels, width, height, 16, 28, 8, 19, 1, outline);

            // ラバー面
            FillCircle(pixels, width, height, 20, 17, 16f, rubber);
            OutlineCircle(pixels, width, height, 20, 17, 16f, 2, outline);
            FillCircle(pixels, width, height, 14, 11, 4.5f, new Color32(255, 255, 255, 60));

            return pixels;
        }

        // ------------------------------------------------------------------
        // キャラクター（足元原点。back=プレイヤーの背中側 / false=NPCの正面）
        // ------------------------------------------------------------------
        private static Color32[] BuildCharacter(bool back, Color32 shirt)
        {
            const int width = 48;
            const int height = 64;
            var pixels = NewCanvas(width, height);

            var skin = new Color32(238, 196, 158, 255);
            var hair = new Color32(62, 48, 44, 255);
            var pants = new Color32(52, 58, 72, 255);
            var outline = new Color32(34, 30, 38, 255);
            var shirtDark = Lerp(shirt, new Color32(0, 0, 0, 255), 0.25f);

            // 脚
            FillRect(pixels, width, height, 16, 44, 7, 20, pants);
            FillRect(pixels, width, height, 25, 44, 7, 20, pants);

            // 胴（肩を広く、腰を細く）
            for (int y = 22; y < 45; y++)
            {
                int halfWidth = Mathf.RoundToInt(Mathf.Lerp(13f, 9f, (y - 22) / 23f));
                FillRect(pixels, width, height, 24 - halfWidth, y, halfWidth * 2, 1, y > 38 ? shirtDark : shirt);
            }

            // 腕（打球側の腕を前に出す）
            FillRect(pixels, width, height, 8, 24, 6, 16, skin);
            FillRect(pixels, width, height, 34, 24, 6, 16, skin);

            // 頭
            FillCircle(pixels, width, height, 24, 14, 9f, skin);
            FillCircle(pixels, width, height, 24, 11, 9f, hair);
            if (!back)
            {
                FillRect(pixels, width, height, 20, 15, 2, 2, outline);
                FillRect(pixels, width, height, 26, 15, 2, 2, outline);
                FillRect(pixels, width, height, 22, 19, 4, 1, outline);
            }

            return pixels;
        }

        // ------------------------------------------------------------------
        // 台の質感とネット（メッシュへ貼るタイル素材）
        // ------------------------------------------------------------------
        private static Color32[] BuildTableSurface()
        {
            const int size = 64;
            var pixels = NewCanvas(size, size);
            var baseColor = new Color32(24, 86, 132, 255);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // 一定のノイズでわずかなムラを作り、べた塗りに見えないようにする
                    int noise = (Hash(x, y) % 9) - 4;
                    SetPixel(pixels, size, size, x, y, new Color32(
                        (byte)Mathf.Clamp(baseColor.r + noise, 0, 255),
                        (byte)Mathf.Clamp(baseColor.g + noise, 0, 255),
                        (byte)Mathf.Clamp(baseColor.b + noise, 0, 255),
                        255));
                }
            }

            return pixels;
        }

        private static Color32[] BuildNet()
        {
            const int size = 32;
            var pixels = NewCanvas(size, size);
            var mesh = new Color32(236, 240, 248, 150);

            for (int i = 0; i < size; i += 4)
            {
                FillRect(pixels, size, size, i, 0, 1, size, mesh);
                FillRect(pixels, size, size, 0, i, size, 1, mesh);
            }

            return pixels;
        }

        private static Color32[] BuildBounceRing()
        {
            const int size = 32;
            var pixels = NewCanvas(size, size);
            OutlineCircle(pixels, size, size, 16, 16, 15f, 2, new Color32(255, 255, 255, 220));
            return pixels;
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

        private static void OutlineRect(Color32[] pixels, int width, int height, int x0, int y0, int w, int h, int thickness, Color32 color)
        {
            FillRect(pixels, width, height, x0, y0, w, thickness, color);
            FillRect(pixels, width, height, x0, y0 + h - thickness, w, thickness, color);
            FillRect(pixels, width, height, x0, y0, thickness, h, color);
            FillRect(pixels, width, height, x0 + w - thickness, y0, thickness, h, color);
        }

        private static void FillCircle(Color32[] pixels, int width, int height, int cx, int cy, float radius, Color32 color)
        {
            for (int y = Mathf.FloorToInt(cy - radius) - 1; y <= Mathf.CeilToInt(cy + radius) + 1; y++)
            {
                for (int x = Mathf.FloorToInt(cx - radius) - 1; x <= Mathf.CeilToInt(cx + radius) + 1; x++)
                {
                    if (Distance(x, y, cx, cy) <= radius)
                    {
                        SetPixel(pixels, width, height, x, y, color);
                    }
                }
            }
        }

        private static void OutlineCircle(Color32[] pixels, int width, int height, int cx, int cy, float radius, int thickness, Color32 color)
        {
            for (int y = Mathf.FloorToInt(cy - radius) - 1; y <= Mathf.CeilToInt(cy + radius) + 1; y++)
            {
                for (int x = Mathf.FloorToInt(cx - radius) - 1; x <= Mathf.CeilToInt(cx + radius) + 1; x++)
                {
                    float d = Distance(x, y, cx, cy);
                    if (d >= radius - thickness && d <= radius)
                    {
                        SetPixel(pixels, width, height, x, y, color);
                    }
                }
            }
        }

        private static float Distance(int x, int y, int cx, int cy)
        {
            float dx = x - cx + 0.5f;
            float dy = y - cy + 0.5f;
            return Mathf.Sqrt(dx * dx + dy * dy);
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
        private static void SaveSprite(string spriteName, Color32[] pixels, int width, int height,
            SpriteAlignment alignment, bool repeat)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.SetPixels32(pixels);
            texture.Apply();

            string path = $"{SpriteDirectory}/{spriteName}.png";
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            ConfigureImporter(path, alignment, repeat);
        }

        private static void ConfigureImporter(string path, SpriteAlignment alignment, bool repeat)
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
            importer.wrapMode = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteAlignment = (int)alignment;
            importer.SetTextureSettings(settings);

            importer.SaveAndReimport();
        }
    }
}
