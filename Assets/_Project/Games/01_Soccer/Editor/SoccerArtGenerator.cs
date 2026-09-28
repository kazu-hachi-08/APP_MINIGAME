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

        /// <summary>1ワールド単位あたりのピクセル数（コート33x18単位 = 528x288px）</summary>
        public const int PixelsPerUnit = 16;

        public const int PlayerSize = 16;
        public const int CourtWidth = 33 * PixelsPerUnit;
        public const int CourtHeight = 18 * PixelsPerUnit;
        public const int GoalWidth = 14;
        public const int GoalHeight = 4 * PixelsPerUnit + 4; // ゴール枠64px + 上下ポスト
        private const int BallSize = 8;
        private const int MarkerSize = 16;
        private const int CrowdTileSize = 16;

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

            GeneratePlayerSprites();
            GenerateFieldSprites();

            AssetDatabase.Refresh();
            Debug.Log($"[SoccerArtGenerator] ドット絵素材を生成しました: {SpriteDirectory}");
        }

        private static void GeneratePlayerSprites()
        {
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
        }

        private static void GenerateFieldSprites()
        {
            SaveSprite("Court", BuildCourt(), CourtWidth, CourtHeight, null, repeat: false);
            SaveSprite("Goal", BuildGoal(), GoalWidth, GoalHeight, null, repeat: false);
            SaveSprite("Ball", BuildBall(), BallSize, BallSize, null, repeat: false);
            SaveSprite("ControlMarker", BuildMarker(), MarkerSize, MarkerSize, null, repeat: false);
            // 観客席はコート外に敷き詰めるので繰り返し前提にする
            SaveSprite("Crowd", BuildCrowd(), CrowdTileSize, CrowdTileSize, null, repeat: true);
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

        // コートのライン（px）。各エリアはコート拡大に合わせて実際のピッチと同じ比率で広げている
        private const int CourtLineThickness = 2;
        private const int CourtMargin = 3; // 外周ラインの外側の余白
        private const int CourtSpotSize = 2;
        private const float CenterCircleRadius = 50f;
        private const float CornerArcRadius = 10f;
        private const int PenaltyAreaWidth = 80;
        private const int PenaltyAreaHeight = 180;
        private const int GoalAreaWidth = 32;
        private const int GoalAreaHeight = 92;
        private const int PenaltySpotDistance = 54; // ゴールラインからペナルティスポットまで

        private static readonly Color32 NetFillColor = new Color32(210, 220, 230, 60);
        private static readonly Color32 NetLineColor = new Color32(226, 232, 238, 90);
        private static readonly Color32 GoalPostColor = new Color32(250, 250, 252, 255);
        private const int GoalPostThickness = 2;
        private const int NetSpacing = 3;

        private static readonly Color32 BallColor = new Color32(250, 250, 250, 255);
        private static readonly Color32 BallOutlineColor = new Color32(28, 26, 34, 255);
        private static readonly Color32 BallPatchColor = new Color32(40, 40, 48, 255);
        private const float BallFillRadius = 3.6f;
        private const float BallOutlineRadius = 4.0f;
        private static readonly Vector2Int[] BallPatchPixels =
        {
            new Vector2Int(3, 2),
            new Vector2Int(4, 2),
            new Vector2Int(2, 4),
            new Vector2Int(5, 5),
            new Vector2Int(3, 5)
        };

        // 足元に敷くので横長の楕円にする
        private static readonly Color32 MarkerColor = new Color32(255, 226, 70, 220);
        private const float MarkerRadiusX = 7.5f;
        private const float MarkerRadiusY = 4.5f;
        private const float MarkerInnerRatio = 0.72f; // リング内径（外径に対する比）

        private static readonly Color32 CrowdBackColor = new Color32(58, 62, 74, 255);
        private static readonly Color32 CrowdEdgeColor = new Color32(40, 44, 54, 255);
        private static readonly Color32[] CrowdColors =
        {
            new Color32(206, 76, 66, 255),
            new Color32(66, 110, 206, 255),
            new Color32(226, 206, 120, 255),
            new Color32(150, 150, 158, 255),
            new Color32(96, 176, 110, 255)
        };
        private const int CrowdSeatSpacing = 3;
        private const int CrowdHeadSize = 2;

        private static Color32[] BuildCourt()
        {
            var c = NewCanvas(CourtWidth, CourtHeight);
            DrawGrassStripes(c);
            DrawCenterMarkings(c);
            DrawPenaltyArea(c, isLeft: true);
            DrawPenaltyArea(c, isLeft: false);
            DrawCornerArcs(c);
            FillOutsideMargin(c);
            return c;
        }

        /// <summary>芝の縞（1ワールド単位ごとに明暗を切り替える）</summary>
        private static void DrawGrassStripes(Color32[] c)
        {
            for (int x = 0; x < CourtWidth; x++)
            {
                FillRect(c, CourtWidth, CourtHeight, x, 0, 1, CourtHeight,
                    (x / PixelsPerUnit) % 2 == 0 ? GrassLight : GrassDark);
            }
        }

        /// <summary>外周ライン・ハーフウェーライン・センターサークル・センタースポット</summary>
        private static void DrawCenterMarkings(Color32[] c)
        {
            OutlineRect(c, CourtWidth, CourtHeight, CourtMargin, CourtMargin,
                CourtWidth - CourtMargin * 2, CourtHeight - CourtMargin * 2, CourtLineThickness, LineColor);
            // 2px幅のライン・スポットを中央に揃えるため1pxずらす
            FillRect(c, CourtWidth, CourtHeight, CourtWidth / 2 - 1, CourtMargin, CourtLineThickness, CourtHeight - CourtMargin * 2, LineColor);
            OutlineCircle(c, CourtWidth, CourtHeight, CourtWidth / 2, CourtHeight / 2, CenterCircleRadius, CourtLineThickness, LineColor);
            FillRect(c, CourtWidth, CourtHeight, CourtWidth / 2 - 1, CourtHeight / 2 - 1, CourtSpotSize, CourtSpotSize, LineColor);
        }

        /// <summary>片側のペナルティエリア・ゴールエリア・ペナルティスポット</summary>
        private static void DrawPenaltyArea(Color32[] c, bool isLeft)
        {
            int penaltyX = isLeft ? CourtMargin : CourtWidth - CourtMargin - PenaltyAreaWidth;
            OutlineRect(c, CourtWidth, CourtHeight, penaltyX, (CourtHeight - PenaltyAreaHeight) / 2,
                PenaltyAreaWidth, PenaltyAreaHeight, CourtLineThickness, LineColor);

            int goalAreaX = isLeft ? CourtMargin : CourtWidth - CourtMargin - GoalAreaWidth;
            OutlineRect(c, CourtWidth, CourtHeight, goalAreaX, (CourtHeight - GoalAreaHeight) / 2,
                GoalAreaWidth, GoalAreaHeight, CourtLineThickness, LineColor);

            int spotX = isLeft ? CourtMargin + PenaltySpotDistance : CourtWidth - CourtMargin - PenaltySpotDistance;
            FillRect(c, CourtWidth, CourtHeight, spotX, CourtHeight / 2 - 1, CourtSpotSize, CourtSpotSize, LineColor);
        }

        /// <summary>コーナーアーク（外周より外は最後に塗り潰すので円のまま描いてよい）</summary>
        private static void DrawCornerArcs(Color32[] c)
        {
            int[] cornerXs = { CourtMargin, CourtWidth - CourtMargin };
            int[] cornerYs = { CourtMargin, CourtHeight - CourtMargin };
            foreach (int y in cornerYs)
            {
                foreach (int x in cornerXs)
                {
                    OutlineCircle(c, CourtWidth, CourtHeight, x, y, CornerArcRadius, CourtLineThickness, LineColor);
                }
            }
        }

        private static void FillOutsideMargin(Color32[] c)
        {
            FillRect(c, CourtWidth, CourtHeight, 0, 0, CourtMargin, CourtHeight, OutsideColor);
            FillRect(c, CourtWidth, CourtHeight, CourtWidth - CourtMargin, 0, CourtMargin, CourtHeight, OutsideColor);
            FillRect(c, CourtWidth, CourtHeight, 0, 0, CourtWidth, CourtMargin, OutsideColor);
            FillRect(c, CourtWidth, CourtHeight, 0, CourtHeight - CourtMargin, CourtWidth, CourtMargin, OutsideColor);
        }

        /// <summary>左側ゴールの見た目。右側は SpriteRenderer.flipX で反転して使う</summary>
        private static Color32[] BuildGoal()
        {
            var c = NewCanvas(GoalWidth, GoalHeight);
            const int post = GoalPostThickness;

            FillRect(c, GoalWidth, GoalHeight, post, post, GoalWidth - post, GoalHeight - post * 2, NetFillColor);
            for (int x = post; x < GoalWidth; x += NetSpacing)
            {
                FillRect(c, GoalWidth, GoalHeight, x, post, 1, GoalHeight - post * 2, NetLineColor);
            }
            for (int y = post; y < GoalHeight - post; y += NetSpacing)
            {
                FillRect(c, GoalWidth, GoalHeight, post, y, GoalWidth - post, 1, NetLineColor);
            }

            FillRect(c, GoalWidth, GoalHeight, 0, 0, GoalWidth, post, GoalPostColor);                 // 上ポスト
            FillRect(c, GoalWidth, GoalHeight, 0, GoalHeight - post, GoalWidth, post, GoalPostColor); // 下ポスト
            FillRect(c, GoalWidth, GoalHeight, 0, 0, post, GoalHeight, GoalPostColor);                // 奥のバー
            return c;
        }

        private static Color32[] BuildBall()
        {
            var c = NewCanvas(BallSize, BallSize);
            const int center = BallSize / 2;
            FillCircle(c, BallSize, BallSize, center, center, BallFillRadius, BallColor);
            OutlineCircle(c, BallSize, BallSize, center, center, BallOutlineRadius, 1, BallOutlineColor);

            foreach (Vector2Int patch in BallPatchPixels)
            {
                SetPixel(c, BallSize, BallSize, patch.x, patch.y, BallPatchColor);
            }
            return c;
        }

        /// <summary>操作中の選手の足元に敷く楕円リング</summary>
        private static Color32[] BuildMarker()
        {
            var c = NewCanvas(MarkerSize, MarkerSize);
            const int center = MarkerSize / 2;

            for (int y = 0; y < MarkerSize; y++)
            {
                for (int x = 0; x < MarkerSize; x++)
                {
                    float dx = (x - center + 0.5f) / MarkerRadiusX;
                    float dy = (y - center + 0.5f) / MarkerRadiusY;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    if (d >= MarkerInnerRatio && d <= 1.0f)
                    {
                        SetPixel(c, MarkerSize, MarkerSize, x, y, MarkerColor);
                    }
                }
            }
            return c;
        }

        /// <summary>コート外に敷き詰める観客席タイル（繰り返し前提）</summary>
        private static Color32[] BuildCrowd()
        {
            var c = NewCanvas(CrowdTileSize, CrowdTileSize);
            FillRect(c, CrowdTileSize, CrowdTileSize, 0, 0, CrowdTileSize, CrowdTileSize, CrowdBackColor);

            int index = 0;
            for (int y = 1; y < CrowdTileSize; y += CrowdSeatSpacing)
            {
                for (int x = 1; x < CrowdTileSize; x += CrowdSeatSpacing)
                {
                    // 色が縞状に並ばないよう、位置と通し番号を混ぜて選ぶ
                    Color32 color = CrowdColors[(x * 7 + y * 5 + index) % CrowdColors.Length];
                    FillRect(c, CrowdTileSize, CrowdTileSize, x, y, CrowdHeadSize, CrowdHeadSize, color);
                    index++;
                }
            }

            FillRect(c, CrowdTileSize, CrowdTileSize, 0, 0, CrowdTileSize, 1, CrowdEdgeColor);
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
