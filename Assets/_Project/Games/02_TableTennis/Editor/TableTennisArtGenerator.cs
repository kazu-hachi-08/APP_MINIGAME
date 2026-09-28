using System.IO;
using UnityEditor;
using UnityEngine;

namespace MiniGame.TableTennis.Editor
{
    /// <summary>
    /// 卓球ゲームのドット絵素材（ボール・ラケット・キャラクター・台の質感）をコードから生成するエディタユーティリティ。
    /// 外部素材に依存せず同じ絵をいつでも作り直せるようにするためコード生成にしている
    /// （SoccerArtGenerator と同じ方針）。生成物は通常のPNGなので手描き素材へ差し替えても構わない。
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
        public const string BackgroundName = "Background";

        // 選手・ラケット選択用。選手はシャツの色、ラケットはラバーの色で見分ける
        public const string MarioName = "Character_Mario";
        public const string KokiniwaName = "Character_Kokiniwa";
        public const string YokozunaName = "Character_Yokozuna";
        public const string BackSuffix = "_Back";
        public const string FrontSuffix = "_Front";

        public const string StandardRacketName = "Racket_Standard";
        public const string PowerRacketName = "Racket_Power";
        public const string TechniqueRacketName = "Racket_Technique";

        // 各素材のピクセル寸法
        private const int BallSize = 24;
        private const int RacketWidth = 40;
        private const int RacketHeight = 48;
        private const int CharacterWidth = 48;
        private const int CharacterHeight = 64;
        private const int TableTileSize = 64;
        private const int NetTileSize = 32;
        private const int BounceRingSize = 32;
        private const int BackgroundWidth = 48;
        private const int BackgroundHeight = 80;

        private static readonly Color32 MarioShirt = new Color32(214, 84, 76, 255);
        private static readonly Color32 KokiniwaShirt = new Color32(64, 168, 110, 255);
        private static readonly Color32 YokozunaShirt = new Color32(150, 88, 196, 255);
        private static readonly Color32 NpcShirt = new Color32(70, 118, 208, 255);

        private static readonly Color32 StandardRubber = new Color32(206, 62, 58, 255);
        private static readonly Color32 PowerRubber = new Color32(40, 40, 48, 255);
        private static readonly Color32 TechniqueRubber = new Color32(52, 110, 200, 255);
        private static readonly Color32 NpcRubber = new Color32(48, 52, 64, 255);

        private static readonly Color32 Transparent = new Color32(0, 0, 0, 0);
        private static readonly Color32 Black = new Color32(0, 0, 0, 255);

        [MenuItem("Tools/MiniGame/Generate Table Tennis Art", false, 4)]
        public static void GenerateAll()
        {
            if (!Directory.Exists(SpriteDirectory))
            {
                Directory.CreateDirectory(SpriteDirectory);
            }

            GenerateBallSprites();
            GenerateRacketSprites();
            GenerateCharacterSprites();
            GenerateCourtSprites();

            AssetDatabase.Refresh();
            Debug.Log($"[TableTennisArtGenerator] 卓球の素材を生成しました: {SpriteDirectory}");
        }

        /// <summary>素材が未生成なら生成する（TableTennisSceneBuilder から呼ばれる）</summary>
        public static void EnsureGenerated()
        {
            // 後から追加した素材（選手・ラケット）が無い既存環境でも作り直されるよう、両方を確認する
            if (Load(BallName) == null || Load(TechniqueRacketName) == null)
            {
                GenerateAll();
            }
        }

        public static Sprite Load(string spriteName)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteDirectory}/{spriteName}.png");
        }

        private static void GenerateBallSprites()
        {
            SaveSprite(BallName, BuildBall(), BallSize, BallSize, SpriteAlignment.Center, repeat: false);
            SaveSprite(ShadowName, BuildShadow(), BallSize, BallSize, SpriteAlignment.Center, repeat: false);
            SaveSprite(BounceRingName, BuildBounceRing(), BounceRingSize, BounceRingSize, SpriteAlignment.Center, repeat: false);
        }

        private static void GenerateRacketSprites()
        {
            // シーンに最初から置く自分のラケットは、初期選択のスタンダードと同じ色にしておく
            SaveRacket(PlayerRacketName, StandardRubber);
            SaveRacket(NpcRacketName, NpcRubber);

            SaveRacket(StandardRacketName, StandardRubber);
            SaveRacket(PowerRacketName, PowerRubber);
            SaveRacket(TechniqueRacketName, TechniqueRubber);
        }

        private static void GenerateCharacterSprites()
        {
            // シーンに最初から置く自分の選手は、初期選択の MARIO と同じ色にしておく
            SaveCharacter(PlayerCharacterName, back: true, shirt: MarioShirt);
            SaveCharacter(NpcCharacterName, back: false, shirt: NpcShirt);

            SaveCharacterPair(MarioName, MarioShirt);
            SaveCharacterPair(KokiniwaName, KokiniwaShirt);
            SaveCharacterPair(YokozunaName, YokozunaShirt);
        }

        private static void GenerateCourtSprites()
        {
            // 台とネットはメッシュへ貼るので繰り返し可能にする
            SaveSprite(TableSurfaceName, BuildTableSurface(), TableTileSize, TableTileSize, SpriteAlignment.Center, repeat: true);
            SaveSprite(NetName, BuildNet(), NetTileSize, NetTileSize, SpriteAlignment.Center, repeat: true);

            // 背景は画面いっぱいに引き伸ばして使うので繰り返し不要
            SaveSprite(BackgroundName, BuildBackground(), BackgroundWidth, BackgroundHeight, SpriteAlignment.Center, repeat: false);
        }

        private static void SaveRacket(string name, Color32 rubber)
        {
            SaveSprite(name, BuildRacket(rubber), RacketWidth, RacketHeight, SpriteAlignment.Center, repeat: false);
        }

        /// <summary>床に立たせる位置を足元で合わせられるよう、原点を足元にする</summary>
        private static void SaveCharacter(string name, bool back, Color32 shirt)
        {
            SaveSprite(name, BuildCharacter(back, shirt), CharacterWidth, CharacterHeight,
                SpriteAlignment.BottomCenter, repeat: false);
        }

        /// <summary>1人の選手につき、手前用（背中）と奥用（正面）の2枚を作る</summary>
        private static void SaveCharacterPair(string name, Color32 shirt)
        {
            SaveCharacter(name + BackSuffix, back: true, shirt: shirt);
            SaveCharacter(name + FrontSuffix, back: false, shirt: shirt);
        }

        // ------------------------------------------------------------------
        // ボール（回転が読み取れるよう縫い目を入れる）
        // ------------------------------------------------------------------
        private const float BallRadius = 11f;
        private const float BallEdgeWidth = 1.2f;
        private const int BallLightOffset = 3;        // 光源（左上）の中心からのずれ
        private const float BallLightFalloff = 1.8f;  // 半径の何倍で暗部に落ちるか
        private const int BallSeamCenterOffset = 8;   // 縫い目を描く円の中心の左ずれ
        private const float BallSeamRadius = 13f;
        private const float BallSeamHalfWidth = 1.1f;

        private static readonly Color32 BallBright = new Color32(255, 252, 238, 255);
        private static readonly Color32 BallShade = new Color32(214, 200, 170, 255);
        private static readonly Color32 BallEdge = new Color32(150, 138, 116, 255);
        private static readonly Color32 BallSeam = new Color32(228, 96, 80, 255);

        private static Color32[] BuildBall()
        {
            const int size = BallSize;
            const int center = size / 2;
            var pixels = NewCanvas(size, size);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Distance(x, y, center, center);
                    if (distance > BallRadius) continue;

                    // 左上から光が当たっているように見せる
                    float lightDistance = Distance(x, y, center - BallLightOffset, center - BallLightOffset);
                    float light = Mathf.Clamp01(1f - lightDistance / (BallRadius * BallLightFalloff));
                    Color32 color = Lerp(BallShade, BallBright, light);

                    if (distance > BallRadius - BallEdgeWidth) color = BallEdge;

                    // 別の円の輪郭でボールを横切らせ、縫い目（回転の目印）にする
                    float seamDistance = Distance(x, y, center - BallSeamCenterOffset, center);
                    if (distance < BallRadius - BallEdgeWidth && Mathf.Abs(seamDistance - BallSeamRadius) < BallSeamHalfWidth)
                    {
                        color = BallSeam;
                    }

                    SetPixel(pixels, size, size, x, y, color);
                }
            }

            return pixels;
        }

        /// <summary>中心ほど濃い影。高さ表現で縮小・減光して使う</summary>
        private static Color32[] BuildShadow()
        {
            const int size = BallSize;
            const int center = size / 2;
            var pixels = NewCanvas(size, size);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Distance(x, y, center, center);
                    if (distance > BallRadius) continue;

                    byte alpha = (byte)Mathf.RoundToInt(255f * Mathf.Clamp01(1f - distance / BallRadius));
                    SetPixel(pixels, size, size, x, y, new Color32(0, 0, 0, alpha));
                }
            }

            return pixels;
        }

        private static Color32[] BuildBounceRing()
        {
            const int size = BounceRingSize;
            var pixels = NewCanvas(size, size);
            OutlineCircle(pixels, size, size, size / 2, size / 2, 15f, 2, new Color32(255, 255, 255, 220));
            return pixels;
        }

        // ------------------------------------------------------------------
        // ラケット（ラバー面＋グリップ）
        // ------------------------------------------------------------------
        private static readonly Color32 RacketWood = new Color32(196, 150, 96, 255);
        private static readonly Color32 RacketWoodDark = new Color32(146, 106, 62, 255);
        private static readonly Color32 RacketOutline = new Color32(38, 34, 44, 255);
        private static readonly Color32 RubberHighlight = new Color32(255, 255, 255, 60);

        private static Color32[] BuildRacket(Color32 rubber)
        {
            const int width = RacketWidth;
            const int height = RacketHeight;
            var pixels = NewCanvas(width, height);

            // グリップ（先に描いてラバー面を上に重ねる）。左側を暗くして立体感を出す
            FillRect(pixels, width, height, 16, 28, 8, 19, RacketWood);
            FillRect(pixels, width, height, 16, 28, 3, 19, RacketWoodDark);
            OutlineRect(pixels, width, height, 16, 28, 8, 19, 1, RacketOutline);

            // ラバー面と、左上のツヤ
            FillCircle(pixels, width, height, 20, 17, 16f, rubber);
            OutlineCircle(pixels, width, height, 20, 17, 16f, 2, RacketOutline);
            FillCircle(pixels, width, height, 14, 11, 4.5f, RubberHighlight);

            return pixels;
        }

        // ------------------------------------------------------------------
        // キャラクター（足元原点。back=プレイヤーの背中側 / false=NPCの正面）
        // ------------------------------------------------------------------
        private static readonly Color32 CharacterSkin = new Color32(238, 196, 158, 255);
        private static readonly Color32 CharacterHair = new Color32(62, 48, 44, 255);
        private static readonly Color32 CharacterPants = new Color32(52, 58, 72, 255);
        private static readonly Color32 CharacterOutline = new Color32(34, 30, 38, 255);
        private const float ShirtShadeAmount = 0.25f;

        private static Color32[] BuildCharacter(bool back, Color32 shirt)
        {
            const int width = CharacterWidth;
            const int height = CharacterHeight;
            var pixels = NewCanvas(width, height);

            // 脚
            FillRect(pixels, width, height, 16, 44, 7, 20, CharacterPants);
            FillRect(pixels, width, height, 25, 44, 7, 20, CharacterPants);

            DrawTorso(pixels, width, height, shirt);

            // 腕
            FillRect(pixels, width, height, 8, 24, 6, 16, CharacterSkin);
            FillRect(pixels, width, height, 34, 24, 6, 16, CharacterSkin);

            // 頭（髪を少し上にずらして重ね、顔の部分だけ肌を残す）
            FillCircle(pixels, width, height, 24, 14, 9f, CharacterSkin);
            FillCircle(pixels, width, height, 24, 11, 9f, CharacterHair);
            if (!back)
            {
                DrawFace(pixels, width, height);
            }

            return pixels;
        }

        /// <summary>肩を広く腰を細くした胴。裾だけ暗くしてズボンとの境目を見せる</summary>
        private static void DrawTorso(Color32[] pixels, int width, int height, Color32 shirt)
        {
            const int top = 22;
            const int bottom = 45;
            const int hemTop = 38;
            const float shoulderHalfWidth = 13f;
            const float waistHalfWidth = 9f;
            int centerX = width / 2;
            Color32 shirtDark = Lerp(shirt, Black, ShirtShadeAmount);

            for (int y = top; y < bottom; y++)
            {
                int halfWidth = Mathf.RoundToInt(Mathf.Lerp(shoulderHalfWidth, waistHalfWidth, (y - top) / (float)(bottom - top)));
                FillRect(pixels, width, height, centerX - halfWidth, y, halfWidth * 2, 1, y > hemTop ? shirtDark : shirt);
            }
        }

        private static void DrawFace(Color32[] pixels, int width, int height)
        {
            FillRect(pixels, width, height, 20, 15, 2, 2, CharacterOutline);
            FillRect(pixels, width, height, 26, 15, 2, 2, CharacterOutline);
            FillRect(pixels, width, height, 22, 19, 4, 1, CharacterOutline);
        }

        // ------------------------------------------------------------------
        // 台の質感とネット（メッシュへ貼るタイル素材）
        // ------------------------------------------------------------------
        private static readonly Color32 TableColor = new Color32(24, 86, 132, 255);
        private const int TableNoiseAmplitude = 4;
        private static readonly Color32 NetMeshColor = new Color32(236, 240, 248, 150);
        private const int NetMeshSpacing = 4;

        private static Color32[] BuildTableSurface()
        {
            const int size = TableTileSize;
            var pixels = NewCanvas(size, size);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // 一定のノイズでわずかなムラを作り、べた塗りに見えないようにする
                    SetPixel(pixels, size, size, x, y, AddNoise(TableColor, x, y, TableNoiseAmplitude));
                }
            }

            return pixels;
        }

        private static Color32[] BuildNet()
        {
            const int size = NetTileSize;
            var pixels = NewCanvas(size, size);

            for (int i = 0; i < size; i += NetMeshSpacing)
            {
                FillRect(pixels, size, size, i, 0, 1, size, NetMeshColor);
                FillRect(pixels, size, size, 0, i, size, 1, NetMeshColor);
            }

            return pixels;
        }

        // ------------------------------------------------------------------
        // 背景（体育館の壁と床。実寸ではなくBackgroundViewで画面全体に引き伸ばして使う）
        // ------------------------------------------------------------------
        private static readonly Color32 WallTopColor = new Color32(30, 34, 46, 255);
        private static readonly Color32 WallBottomColor = new Color32(46, 52, 68, 255);
        private static readonly Color32 WallLineColor = new Color32(58, 64, 82, 255);
        private static readonly Color32 FloorColor = new Color32(64, 46, 34, 255);
        private static readonly Color32 BaseboardColor = new Color32(20, 22, 30, 255);
        private const float FloorTopRatio = 0.62f;
        private const int WallPanelSpacing = 12;
        private const int BaseboardHeight = 2;
        private const int FloorNoiseAmplitude = 3;

        private static Color32[] BuildBackground()
        {
            const int width = BackgroundWidth;
            const int height = BackgroundHeight;
            var pixels = NewCanvas(width, height);

            // 壁と床の境目のy座標（左上原点）。台の奥に壁、手前に床があるように見せる
            int floorTop = Mathf.RoundToInt(height * FloorTopRatio);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    SetPixel(pixels, width, height, x, y, BackgroundColorAt(x, y, floorTop));
                }
            }

            return pixels;
        }

        private static Color32 BackgroundColorAt(int x, int y, int floorTop)
        {
            if (y < floorTop)
            {
                // 一定間隔の縦ラインで体育館の壁パネルらしさを出す
                if (x % WallPanelSpacing == 0) return WallLineColor;
                return Lerp(WallTopColor, WallBottomColor, y / (float)floorTop);
            }

            if (y < floorTop + BaseboardHeight) return BaseboardColor;

            // 単色べた塗りに見えないよう座標由来のノイズでムラを作る
            return AddNoise(FloorColor, x, y, FloorNoiseAmplitude);
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
            DrawRing(pixels, width, height, cx, cy, float.NegativeInfinity, radius, color);
        }

        private static void OutlineCircle(Color32[] pixels, int width, int height, int cx, int cy, float radius, int thickness, Color32 color)
        {
            DrawRing(pixels, width, height, cx, cy, radius - thickness, radius, color);
        }

        /// <summary>中心からの距離が innerRadius 以上 outerRadius 以下の画素を塗る</summary>
        private static void DrawRing(Color32[] pixels, int width, int height, int cx, int cy,
            float innerRadius, float outerRadius, Color32 color)
        {
            // 画素中心で距離を測るため、境界の1px外まで走査する
            for (int y = Mathf.FloorToInt(cy - outerRadius) - 1; y <= Mathf.CeilToInt(cy + outerRadius) + 1; y++)
            {
                for (int x = Mathf.FloorToInt(cx - outerRadius) - 1; x <= Mathf.CeilToInt(cx + outerRadius) + 1; x++)
                {
                    float d = Distance(x, y, cx, cy);
                    if (d >= innerRadius && d <= outerRadius)
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

        /// <summary>RGBを -amplitude〜+amplitude の範囲で一律にずらす（不透明で返す）</summary>
        private static Color32 AddNoise(Color32 baseColor, int x, int y, int amplitude)
        {
            int noise = (Hash(x, y) % (amplitude * 2 + 1)) - amplitude;
            return new Color32(
                (byte)Mathf.Clamp(baseColor.r + noise, 0, 255),
                (byte)Mathf.Clamp(baseColor.g + noise, 0, 255),
                (byte)Mathf.Clamp(baseColor.b + noise, 0, 255),
                255);
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
