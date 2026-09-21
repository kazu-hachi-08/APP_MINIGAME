using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MiniGame.Soccer.Editor
{
    /// <summary>
    /// サッカーゲームのドット絵素材（選手・コート・ゴール・ボール等）をコードから生成するエディタユーティリティ。
    /// 外部素材に依存せず同じ絵をいつでも作り直せるようにするためコード生成にしている。
    /// 生成結果は通常のPNGアセットなので、後から手描き素材へ差し替えても構わない。
    /// </summary>
    public static class SoccerArtGenerator
    {
        public const string SpriteDirectory = "Assets/_Project/Games/01_Soccer/Sprites";

        /// <summary>1ワールド単位あたりのピクセル数（コート22x12単位 = 352x192px）</summary>
        public const int PixelsPerUnit = 16;

        public const int PlayerSize = 16;
        public const int CourtWidth = 22 * PixelsPerUnit;
        public const int CourtHeight = 12 * PixelsPerUnit;
        public const int GoalWidth = 14;
        public const int GoalHeight = 4 * PixelsPerUnit + 4; // ゴール枠64px + 上下ポスト

        /// <summary>選手スプライトの原点は足元に置く（16pxのうち下から1.5px）</summary>
        private static readonly Vector2 PlayerPivot = new Vector2(0.5f, 1.5f / PlayerSize);

        public static readonly string[] TeamKeys = { "Home", "Away", "Gk" };
        public static readonly string[] DirectionKeys = { "Down", "Up", "Side" };
        public const int FrameCount = 3; // 0 = 待機 / 1,2 = 走り

        [MenuItem("Tools/MiniGame/Generate Soccer Art", false, 1)]
        public static void GenerateAll()
        {
            if (!Directory.Exists(SpriteDirectory))
            {
                Directory.CreateDirectory(SpriteDirectory);
            }

            foreach (string team in TeamKeys)
            {
                foreach (string direction in DirectionKeys)
                {
                    for (int frame = 0; frame < FrameCount; frame++)
                    {
                        string[] rows = BuildPlayerFrame(direction, frame);
                        SaveSprite($"Player_{team}_{direction}_{frame}", RenderRows(rows, TeamPalette(team)),
                            PlayerSize, PlayerSize, PlayerPivot, repeat: false);
                    }
                }
            }

            SaveSprite("Court", BuildCourt(), CourtWidth, CourtHeight, null, repeat: false);
            SaveSprite("Goal", BuildGoal(), GoalWidth, GoalHeight, null, repeat: false);
            SaveSprite("Ball", BuildBall(), 8, 8, null, repeat: false);
            SaveSprite("ControlMarker", BuildMarker(), 16, 16, null, repeat: false);
            SaveSprite("Crowd", BuildCrowd(), 16, 16, null, repeat: true);

            AssetDatabase.Refresh();
            Debug.Log($"[SoccerArtGenerator] ドット絵素材を生成しました: {SpriteDirectory}");
        }

        /// <summary>
        /// 素材が未生成、またはコート寸法の定数と食い違っていたら生成し直す
        /// （コートを広げても古いPNGが残り、選手だけコート外に見える事故を防ぐ）
        /// </summary>
        public static void EnsureGenerated()
        {
            Sprite court = Load("Court");
            if (court == null ||
                court.texture.width != CourtWidth ||
                court.texture.height != CourtHeight)
            {
                GenerateAll();
            }
        }

        public static Sprite Load(string spriteName)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteDirectory}/{spriteName}.png");
        }

        // ------------------------------------------------------------------
        // 選手のドット絵
        // 1文字 = 1ピクセル。o=輪郭 h=髪 s=肌 e=目 j=ユニフォーム p=パンツ c=ソックス b=スパイク d=影
        // 頭（7行）+ 体（8行）+ 影（1行）を組み合わせて16x16の1コマにする
        // ------------------------------------------------------------------
        private static readonly string[] HeadDown =
        {
            "................",
            ".....oooooo.....",
            "....ohhhhhho....",
            "....ohhhhhho....",
            "....ohssssho....",
            "....osesseso....",
            ".....osssso....."
        };

        private static readonly string[] HeadUp =
        {
            "................",
            ".....oooooo.....",
            "....ohhhhhho....",
            "....ohhhhhho....",
            "....ohhhhhho....",
            "....ohhhhhho....",
            ".....osssso....."
        };

        private static readonly string[] HeadSide =
        {
            "................",
            ".....oooooo.....",
            "....ohhhhhho....",
            "....ohhhhhho....",
            "....ohhhssso....",
            "....ohhsesso....",
            ".....osssso....."
        };

        /// <summary>正面・背面の体。腕の振りと脚の開きで走りのコマにする</summary>
        private static readonly string[][] FrontBodies =
        {
            new[]
            {
                "....ojjjjjjo....",
                "...osjjjjjjso...",
                "...osjjjjjjso...",
                "....ojjjjjjo....",
                "....oppppppo....",
                "....opp..ppo....",
                "....occ..cco....",
                "....obb..bbo...."
            },
            new[]
            {
                "...osjjjjjjo....",
                "....ojjjjjjso...",
                "....ojjjjjjo....",
                "....ojjjjjjo....",
                "....oppppppo....",
                "...opp....ppo...",
                "...occ....cco...",
                "...obb....bbo..."
            },
            new[]
            {
                "....ojjjjjjso...",
                "...osjjjjjjo....",
                "....ojjjjjjo....",
                "....ojjjjjjo....",
                "....oppppppo....",
                ".....oppppo.....",
                ".....occcco.....",
                ".....obbbbo....."
            }
        };

        /// <summary>横向きの体。右向きで描き、左向きは SpriteRenderer.flipX で反転する</summary>
        private static readonly string[][] SideBodies =
        {
            new[]
            {
                "....ojjjjjo.....",
                "....ojjjjjso....",
                "....ojjjjjo.....",
                "....ojjjjjo.....",
                "....opppppo.....",
                "....opp.ppo.....",
                "....occ.cco.....",
                "....obb.bbo....."
            },
            new[]
            {
                "....ojjjjjo.....",
                "....ojjjjjjso...",
                "...sojjjjjo.....",
                "....ojjjjjo.....",
                "....opppppo.....",
                "...opp...ppo....",
                "..occ.....cco...",
                "..obb.....bbo..."
            },
            new[]
            {
                "....ojjjjjo.....",
                "...sojjjjjo.....",
                "....ojjjjjjso...",
                "....ojjjjjo.....",
                "....opppppo.....",
                ".....oppppo.....",
                ".....occcco.....",
                ".....obbbbo....."
            }
        };

        private const string ShadowRow = "....dddddddd....";

        private static string[] BuildPlayerFrame(string direction, int frame)
        {
            string[] head = direction == "Up" ? HeadUp : direction == "Side" ? HeadSide : HeadDown;
            string[] body = direction == "Side" ? SideBodies[frame] : FrontBodies[frame];

            var rows = new List<string>(PlayerSize);
            rows.AddRange(head);
            rows.AddRange(body);
            rows.Add(ShadowRow);
            return rows.ToArray();
        }

        private static Dictionary<char, Color32> TeamPalette(string team)
        {
            var palette = new Dictionary<char, Color32>
            {
                { '.', new Color32(0, 0, 0, 0) },
                { 'o', new Color32(26, 24, 34, 255) },
                { 's', new Color32(240, 190, 148, 255) },
                { 'e', new Color32(30, 28, 40, 255) },
                { 'b', new Color32(38, 36, 44, 255) },
                { 'd', new Color32(0, 0, 0, 70) }
            };

            switch (team)
            {
                case "Away":
                    palette['h'] = new Color32(74, 46, 28, 255);
                    palette['j'] = new Color32(220, 56, 48, 255);
                    palette['p'] = new Color32(140, 30, 30, 255);
                    palette['c'] = new Color32(245, 245, 250, 255);
                    break;
                case "Gk":
                    palette['h'] = new Color32(30, 28, 28, 255);
                    palette['j'] = new Color32(60, 180, 92, 255);
                    palette['p'] = new Color32(32, 110, 56, 255);
                    palette['c'] = new Color32(240, 240, 210, 255);
                    break;
                default: // Home
                    palette['h'] = new Color32(44, 36, 30, 255);
                    palette['j'] = new Color32(48, 88, 220, 255);
                    palette['p'] = new Color32(24, 44, 140, 255);
                    palette['c'] = new Color32(245, 245, 250, 255);
                    break;
            }

            return palette;
        }

        private static Color32[] RenderRows(string[] rows, Dictionary<char, Color32> palette)
        {
            var pixels = NewCanvas(PlayerSize, PlayerSize);
            for (int y = 0; y < rows.Length; y++)
            {
                string row = rows[y];
                for (int x = 0; x < row.Length; x++)
                {
                    SetPixel(pixels, PlayerSize, PlayerSize, x, y, palette[row[x]]);
                }
            }
            return pixels;
        }

        // ------------------------------------------------------------------
        // コート・ゴール・ボール・マーカー・観客席
        // ------------------------------------------------------------------
        private static readonly Color32 GrassLight = new Color32(62, 148, 64, 255);
        private static readonly Color32 GrassDark = new Color32(54, 134, 56, 255);
        private static readonly Color32 LineColor = new Color32(238, 244, 238, 255);
        private static readonly Color32 OutsideColor = new Color32(34, 74, 40, 255);

        private static Color32[] BuildCourt()
        {
            var c = NewCanvas(CourtWidth, CourtHeight);

            // 芝の縞（1ワールド単位ごとに明暗を切り替える）
            for (int x = 0; x < CourtWidth; x++)
            {
                FillRect(c, CourtWidth, CourtHeight, x, 0, 1, CourtHeight,
                    (x / PixelsPerUnit) % 2 == 0 ? GrassLight : GrassDark);
            }

            const int thickness = 2; // ライン太さ
            const int margin = 3;    // 外周ラインの余白
            OutlineRect(c, CourtWidth, CourtHeight, margin, margin,
                CourtWidth - margin * 2, CourtHeight - margin * 2, thickness, LineColor);
            FillRect(c, CourtWidth, CourtHeight, CourtWidth / 2 - 1, margin, thickness, CourtHeight - margin * 2, LineColor);
            OutlineCircle(c, CourtWidth, CourtHeight, CourtWidth / 2, CourtHeight / 2, 34f, thickness, LineColor);
            FillRect(c, CourtWidth, CourtHeight, CourtWidth / 2 - 1, CourtHeight / 2 - 1, 2, 2, LineColor);

            // コート拡大に合わせて実際のピッチと同じ比率で各エリアも広げる
            const int penaltyWidth = 54;
            const int penaltyHeight = 120;
            const int goalAreaWidth = 22;
            const int goalAreaHeight = 62;
            for (int side = 0; side < 2; side++)
            {
                int penaltyX = side == 0 ? margin : CourtWidth - margin - penaltyWidth;
                OutlineRect(c, CourtWidth, CourtHeight, penaltyX, (CourtHeight - penaltyHeight) / 2,
                    penaltyWidth, penaltyHeight, thickness, LineColor);

                int goalAreaX = side == 0 ? margin : CourtWidth - margin - goalAreaWidth;
                OutlineRect(c, CourtWidth, CourtHeight, goalAreaX, (CourtHeight - goalAreaHeight) / 2,
                    goalAreaWidth, goalAreaHeight, thickness, LineColor);

                int spotX = side == 0 ? margin + 36 : CourtWidth - margin - 36;
                FillRect(c, CourtWidth, CourtHeight, spotX, CourtHeight / 2 - 1, 2, 2, LineColor);
            }

            // コーナーアーク（外周より外は最後に塗り潰すので円のまま描いてよい）
            OutlineCircle(c, CourtWidth, CourtHeight, margin, margin, 7f, thickness, LineColor);
            OutlineCircle(c, CourtWidth, CourtHeight, CourtWidth - margin, margin, 7f, thickness, LineColor);
            OutlineCircle(c, CourtWidth, CourtHeight, margin, CourtHeight - margin, 7f, thickness, LineColor);
            OutlineCircle(c, CourtWidth, CourtHeight, CourtWidth - margin, CourtHeight - margin, 7f, thickness, LineColor);

            FillRect(c, CourtWidth, CourtHeight, 0, 0, margin, CourtHeight, OutsideColor);
            FillRect(c, CourtWidth, CourtHeight, CourtWidth - margin, 0, margin, CourtHeight, OutsideColor);
            FillRect(c, CourtWidth, CourtHeight, 0, 0, CourtWidth, margin, OutsideColor);
            FillRect(c, CourtWidth, CourtHeight, 0, CourtHeight - margin, CourtWidth, margin, OutsideColor);
            return c;
        }

        /// <summary>左側ゴールの見た目。右側は SpriteRenderer.flipX で反転して使う</summary>
        private static Color32[] BuildGoal()
        {
            var c = NewCanvas(GoalWidth, GoalHeight);
            var netFill = new Color32(210, 220, 230, 60);
            var netLine = new Color32(226, 232, 238, 90);
            var post = new Color32(250, 250, 252, 255);

            FillRect(c, GoalWidth, GoalHeight, 2, 2, GoalWidth - 2, GoalHeight - 4, netFill);
            for (int x = 2; x < GoalWidth; x += 3)
            {
                FillRect(c, GoalWidth, GoalHeight, x, 2, 1, GoalHeight - 4, netLine);
            }
            for (int y = 2; y < GoalHeight - 2; y += 3)
            {
                FillRect(c, GoalWidth, GoalHeight, 2, y, GoalWidth - 2, 1, netLine);
            }

            FillRect(c, GoalWidth, GoalHeight, 0, 0, GoalWidth, 2, post);                // 上ポスト
            FillRect(c, GoalWidth, GoalHeight, 0, GoalHeight - 2, GoalWidth, 2, post);   // 下ポスト
            FillRect(c, GoalWidth, GoalHeight, 0, 0, 2, GoalHeight, post);               // 奥のバー
            return c;
        }

        private static Color32[] BuildBall()
        {
            var c = NewCanvas(8, 8);
            FillCircle(c, 8, 8, 4, 4, 3.6f, new Color32(250, 250, 250, 255));
            OutlineCircle(c, 8, 8, 4, 4, 4.0f, 1, new Color32(28, 26, 34, 255));

            var patch = new Color32(40, 40, 48, 255);
            SetPixel(c, 8, 8, 3, 2, patch);
            SetPixel(c, 8, 8, 4, 2, patch);
            SetPixel(c, 8, 8, 2, 4, patch);
            SetPixel(c, 8, 8, 5, 5, patch);
            SetPixel(c, 8, 8, 3, 5, patch);
            return c;
        }

        /// <summary>操作中の選手の足元に敷く楕円リング</summary>
        private static Color32[] BuildMarker()
        {
            var c = NewCanvas(16, 16);
            var color = new Color32(255, 226, 70, 220);

            for (int y = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    float dx = (x - 8 + 0.5f) / 7.5f;
                    float dy = (y - 8 + 0.5f) / 4.5f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    if (d >= 0.72f && d <= 1.0f)
                    {
                        SetPixel(c, 16, 16, x, y, color);
                    }
                }
            }
            return c;
        }

        /// <summary>コート外に敷き詰める観客席タイル（繰り返し前提）</summary>
        private static Color32[] BuildCrowd()
        {
            var c = NewCanvas(16, 16);
            FillRect(c, 16, 16, 0, 0, 16, 16, new Color32(58, 62, 74, 255));

            Color32[] colors =
            {
                new Color32(206, 76, 66, 255),
                new Color32(66, 110, 206, 255),
                new Color32(226, 206, 120, 255),
                new Color32(150, 150, 158, 255),
                new Color32(96, 176, 110, 255)
            };

            int index = 0;
            for (int y = 1; y < 16; y += 3)
            {
                for (int x = 1; x < 16; x += 3)
                {
                    FillRect(c, 16, 16, x, y, 2, 2, colors[(x * 7 + y * 5 + index) % colors.Length]);
                    index++;
                }
            }

            FillRect(c, 16, 16, 0, 0, 16, 1, new Color32(40, 44, 54, 255));
            return c;
        }

        // ------------------------------------------------------------------
        // 描画ヘルパー（x,yは左上原点。Texture2Dは左下原点のため SetPixel で反転する）
        // ------------------------------------------------------------------
        private static Color32[] NewCanvas(int width, int height)
        {
            var pixels = new Color32[width * height];
            var clear = new Color32(0, 0, 0, 0);
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = clear;
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
                // 影やネットは下の色と合成する
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

        private static float Distance(int x, int y, int cx, int cy)
        {
            float dx = x - cx + 0.5f;
            float dy = y - cy + 0.5f;
            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        // ------------------------------------------------------------------
        // 保存とインポート設定
        // ------------------------------------------------------------------
        private static void SaveSprite(string spriteName, Color32[] pixels, int width, int height, Vector2? customPivot, bool repeat)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.SetPixels32(pixels);
            texture.Apply();

            string path = $"{SpriteDirectory}/{spriteName}.png";
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            ConfigureImporter(path, customPivot, repeat);
        }

        private static void ConfigureImporter(string path, Vector2? customPivot, bool repeat)
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
            settings.spriteMeshType = SpriteMeshType.FullRect; // Tiled 描画に必要
            if (customPivot.HasValue)
            {
                settings.spriteAlignment = (int)SpriteAlignment.Custom;
                settings.spritePivot = customPivot.Value;
            }
            else
            {
                settings.spriteAlignment = (int)SpriteAlignment.Center;
            }
            importer.SetTextureSettings(settings);

            importer.SaveAndReimport();
        }
    }
}
