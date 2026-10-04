using MiniGame.PenguinWars.Battle;
using UnityEngine;

namespace MiniGame.PenguinWars.Editor
{
    /// <summary>
    /// 戦場の絵（仕様書 §7.3）: 氷の城（青・赤）・エンドレスの出現ゲート（洞窟）・地面・遠くの山・空。
    /// 大きい絵は文字パターンだと読みにくいので、四角・三角・楕円の組み合わせで描く
    /// </summary>
    public static class FieldArtGenerator
    {
        public const int CastleWidth = 48;
        public const int CastleHeight = 64;
        private const int GateWidth = 64;
        private const int GateHeight = 48;
        /// <summary>地面は縦に繰り返さないよう、SceneBuilder の地面の厚さ（ワールド単位）と同じ高さで1枚にする</summary>
        public const int GroundTileHeight = 96;
        private const int GroundTileWidth = 32;
        private const int MountainTileWidth = 64;
        public const int MountainTileHeight = 48;
        private const int SkyWidth = 4;
        public const int SkyHeight = 128;

        private static readonly Vector2 BottomCenter = new Vector2(0.5f, 0f);
        private static readonly Vector2 Center = new Vector2(0.5f, 0.5f);

        public static string CastlePath(Side side) => $"{PenguinSpriteWriter.SpriteDirectory}/Castle_{side}.png";
        public static readonly string GatePath = PenguinSpriteWriter.SpriteDirectory + "/Gate.png";
        public static readonly string GroundPath = PenguinSpriteWriter.SpriteDirectory + "/Ground.png";
        public static readonly string MountainsPath = PenguinSpriteWriter.SpriteDirectory + "/Mountains.png";
        public static readonly string SkyPath = PenguinSpriteWriter.SpriteDirectory + "/Sky.png";

        /// <summary>空の一番上の色。カメラの背景色もこれにして、空の絵の上に切れ目が出ないようにする</summary>
        public static readonly Color32 SkyTopColor = new Color32(110, 170, 230, 255);
        private static readonly Color32 SkyHorizonColor = new Color32(215, 235, 250, 255);

        private readonly struct CastleColors
        {
            public readonly Color32 Ice;
            public readonly Color32 IceLine;
            public readonly Color32 Outline;
            public readonly Color32 Roof;
            public readonly Color32 RoofShade;
            public readonly Color32 Dark;

            public CastleColors(Color32 ice, Color32 iceLine, Color32 outline, Color32 roof, Color32 roofShade, Color32 dark)
            {
                Ice = ice;
                IceLine = iceLine;
                Outline = outline;
                Roof = roof;
                RoofShade = roofShade;
                Dark = dark;
            }
        }

        // 自城は青い氷、敵城は同じ形の赤いお城（仕様書 §7.3）
        private static readonly CastleColors LeftCastle = new CastleColors(
            new Color32(195, 232, 250, 255), new Color32(130, 190, 230, 255), new Color32(40, 70, 120, 255),
            new Color32(60, 110, 220, 255), new Color32(35, 70, 160, 255), new Color32(30, 45, 80, 255));
        private static readonly CastleColors RightCastle = new CastleColors(
            new Color32(250, 212, 210, 255), new Color32(220, 140, 140, 255), new Color32(110, 35, 40, 255),
            new Color32(215, 55, 55, 255), new Color32(150, 30, 35, 255), new Color32(80, 30, 35, 255));

        public static bool IsMissing()
        {
            return PenguinSpriteWriter.Load(CastlePath(Side.Left)) == null ||
                   PenguinSpriteWriter.Load(CastlePath(Side.Right)) == null ||
                   PenguinSpriteWriter.Load(GatePath) == null ||
                   PenguinSpriteWriter.Load(GroundPath) == null ||
                   PenguinSpriteWriter.Load(MountainsPath) == null ||
                   PenguinSpriteWriter.Load(SkyPath) == null;
        }

        public static void GenerateAll()
        {
            int ppu = PenguinSpriteWriter.PixelsPerUnit;
            PenguinSpriteWriter.Save(CastlePath(Side.Left), BuildCastle(Side.Left), CastleWidth, CastleHeight, ppu, BottomCenter);
            PenguinSpriteWriter.Save(CastlePath(Side.Right), BuildCastle(Side.Right), CastleWidth, CastleHeight, ppu, BottomCenter);
            PenguinSpriteWriter.Save(GatePath, BuildGate(), GateWidth, GateHeight, ppu, BottomCenter);
            PenguinSpriteWriter.Save(GroundPath, BuildGround(), GroundTileWidth, GroundTileHeight, ppu, Center);
            PenguinSpriteWriter.Save(MountainsPath, BuildMountains(), MountainTileWidth, MountainTileHeight, ppu, BottomCenter);
            PenguinSpriteWriter.Save(SkyPath, BuildSky(), SkyWidth, SkyHeight, ppu, BottomCenter);
        }

        // ------------------------------------------------------------------
        // 城（左右の塔＋城壁＋中央の塔。とんがり屋根と旗）
        // ------------------------------------------------------------------
        private static Color32[] BuildCastle(Side side)
        {
            CastleColors colors = side == Side.Left ? LeftCastle : RightCastle;
            var canvas = new PixelCanvas(CastleWidth, CastleHeight);

            IceBlock(canvas, colors, 6, 0, 41, 33);
            Battlements(canvas, colors, 6, 41, 34);
            IceBlock(canvas, colors, 2, 0, 13, 43);
            IceBlock(canvas, colors, 34, 0, 45, 43);
            IceBlock(canvas, colors, 17, 0, 30, 47);

            Roof(canvas, colors, 1, 14, 44, 12);
            Roof(canvas, colors, 33, 46, 44, 12);
            Roof(canvas, colors, 16, 31, 48, 13);
            Flag(canvas, colors, 24, 60);

            Door(canvas, colors, 20, 27, 13);
            canvas.Fill(7, 30, 8, 35, colors.Dark);
            canvas.Fill(39, 30, 40, 35, colors.Dark);
            canvas.Fill(23, 34, 24, 39, colors.Dark);
            return canvas.Pixels;
        }

        /// <summary>氷のブロック積み。段ごとに継ぎ目をずらして積んだように見せる</summary>
        private static void IceBlock(PixelCanvas canvas, CastleColors colors, int x0, int y0, int x1, int y1)
        {
            const int blockHeight = 6;
            const int blockWidth = 8;
            for (int y = y0; y <= y1; y++)
            {
                int course = (y - y0) / blockHeight;
                for (int x = x0; x <= x1; x++)
                {
                    bool joint = (y - y0) % blockHeight == blockHeight - 1 ||
                                 (x - x0 + course * blockWidth / 2) % blockWidth == 0;
                    canvas.Set(x, y, joint ? colors.IceLine : colors.Ice);
                }
            }
            canvas.Outline(x0, y0, x1, y1, colors.Outline);
        }

        /// <summary>城壁の上のでこぼこ（4ドットの凸を8ドットおき）</summary>
        private static void Battlements(PixelCanvas canvas, CastleColors colors, int x0, int x1, int y)
        {
            const int merlonWidth = 4;
            const int merlonHeight = 4;
            for (int x = x0; x + merlonWidth - 1 <= x1; x += merlonWidth * 2)
            {
                IceBlock(canvas, colors, x, y, x + merlonWidth - 1, y + merlonHeight - 1);
            }
        }

        /// <summary>とんがり屋根。右半分を暗くして立体に見せる</summary>
        private static void Roof(PixelCanvas canvas, CastleColors colors, int x0, int x1, int baseY, int height)
        {
            float center = (x0 + x1) * 0.5f;
            float halfBase = (x1 - x0) * 0.5f;
            for (int i = 0; i < height; i++)
            {
                float half = halfBase * (height - i) / height;
                int left = Mathf.RoundToInt(center - half);
                int right = Mathf.RoundToInt(center + half);
                for (int x = left; x <= right; x++)
                {
                    bool edge = x == left || x == right || i == 0;
                    canvas.Set(x, baseY + i, edge ? colors.Outline : x > center ? colors.RoofShade : colors.Roof);
                }
            }
        }

        private static void Flag(PixelCanvas canvas, CastleColors colors, int poleX, int poleBottom)
        {
            canvas.Fill(poleX, poleBottom, poleX, CastleHeight - 1, colors.Outline);
            canvas.Fill(poleX + 1, CastleHeight - 4, poleX + 6, CastleHeight - 1, colors.Roof);
            canvas.Outline(poleX + 1, CastleHeight - 4, poleX + 6, CastleHeight - 1, colors.Outline);
        }

        /// <summary>上が丸いアーチの扉</summary>
        private static void Door(PixelCanvas canvas, CastleColors colors, int x0, int x1, int height)
        {
            float center = (x0 + x1) * 0.5f;
            float radius = (x1 - x0) * 0.5f + 0.5f;
            int archStart = height - Mathf.CeilToInt(radius);
            for (int y = 0; y <= height; y++)
            {
                for (int x = x0 - 1; x <= x1 + 1; x++)
                {
                    float dx = x - center;
                    float dy = Mathf.Max(0, y - archStart);
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);
                    if (distance <= radius - 1f) canvas.Set(x, y, colors.Dark);
                    else if (distance <= radius) canvas.Set(x, y, colors.Outline);
                }
            }
        }

        // ------------------------------------------------------------------
        // エンドレスの出現ゲート（雪をかぶった岩山の洞窟）
        // ------------------------------------------------------------------
        private static Color32[] BuildGate()
        {
            var rock = new Color32(110, 122, 145, 255);
            var rockShade = new Color32(85, 95, 118, 255);
            var outline = new Color32(35, 40, 55, 255);
            var snow = new Color32(240, 247, 255, 255);
            var hole = new Color32(18, 18, 28, 255);
            var holeEdge = new Color32(45, 45, 60, 255);

            const float mountRadiusX = 31.5f;
            const float mountRadiusY = 46f;
            const float holeRadiusX = 14f;
            const float holeRadiusY = 26f;
            const int snowLine = 36;
            const float center = (GateWidth - 1) * 0.5f;

            var canvas = new PixelCanvas(GateWidth, GateHeight);
            for (int y = 0; y < GateHeight; y++)
            {
                for (int x = 0; x < GateWidth; x++)
                {
                    float mount = Ellipse(x - center, y, mountRadiusX, mountRadiusY);
                    if (mount > 1f) continue;

                    float hollow = Ellipse(x - center, y, holeRadiusX, holeRadiusY);
                    Color32 color;
                    if (mount > 0.93f) color = outline;
                    else if (hollow <= 0.85f) color = hole;
                    else if (hollow <= 1f) color = holeEdge;
                    else if (y >= snowLine - (Hash(x, 0) % 3)) color = snow;
                    else color = Hash(x, y) % 7 == 0 || x > center + 12 ? rockShade : rock;
                    canvas.Set(x, y, color);
                }
            }
            return canvas.Pixels;
        }

        // ------------------------------------------------------------------
        // 地面・遠くの山・空（横に敷き詰めるので左右の端がつながるように描く）
        // ------------------------------------------------------------------
        private static Color32[] BuildGround()
        {
            var snowTop = new Color32(248, 252, 255, 255);
            var snowEdge = new Color32(205, 225, 242, 255);
            var deepTop = new Color32(205, 228, 246, 255);
            var deepBottom = new Color32(120, 165, 210, 255);
            var sparkle = new Color32(255, 255, 255, 255);
            const int snowDepth = 5;

            var canvas = new PixelCanvas(GroundTileWidth, GroundTileHeight);
            for (int x = 0; x < GroundTileWidth; x++)
            {
                // 雪の縁を波打たせる。周期をタイル幅の約数にして、敷き詰めたときにつながるようにする
                int edge = snowDepth + Mathf.RoundToInt(Mathf.Sin(x * Mathf.PI * 2f / (GroundTileWidth / 2f)));
                for (int y = 0; y < GroundTileHeight; y++)
                {
                    int depth = GroundTileHeight - 1 - y;
                    Color32 color;
                    if (depth < edge) color = snowTop;
                    else if (depth == edge) color = snowEdge;
                    else color = Color32.Lerp(deepTop, deepBottom, (float)depth / GroundTileHeight);
                    if (depth > edge && Hash(x, y) % 41 == 0) color = sparkle;
                    canvas.Set(x, y, color);
                }
            }
            return canvas.Pixels;
        }

        private static Color32[] BuildMountains()
        {
            var mountain = new Color32(185, 210, 236, 255);
            var shade = new Color32(165, 192, 224, 255);
            var snow = new Color32(240, 247, 255, 255);
            const int snowCapDepth = 5;
            const int snowCapMinHeight = 30;

            var canvas = new PixelCanvas(MountainTileWidth, MountainTileHeight);
            for (int x = 0; x < MountainTileWidth; x++)
            {
                // 2つの山を重ね、タイルの左右の端では同じ高さになるようにする
                float big = Peak(x, 18, 44, 26);
                float small = Peak(x, 48, 32, 18);
                float top = Mathf.Max(Mathf.Max(big, small), 10f);
                bool rightSlope = WrappedDelta(x, big >= small ? 18 : 48, MountainTileWidth) > 0;
                for (int y = 0; y < Mathf.RoundToInt(top); y++)
                {
                    bool cap = top >= snowCapMinHeight && y >= top - snowCapDepth;
                    canvas.Set(x, y, cap ? snow : rightSlope ? shade : mountain);
                }
            }
            return canvas.Pixels;
        }

        private static Color32[] BuildSky()
        {
            var canvas = new PixelCanvas(SkyWidth, SkyHeight);
            for (int y = 0; y < SkyHeight; y++)
            {
                Color32 color = Color32.Lerp(SkyHorizonColor, SkyTopColor, (float)y / (SkyHeight - 1));
                canvas.Fill(0, y, SkyWidth - 1, y, color);
            }
            return canvas.Pixels;
        }

        /// <summary>山1つの高さ。頂上 (peakX, height) から左右に halfWidth で裾野まで下がる。タイルの端をまたいだ山も描けるよう距離は折り返して測る</summary>
        private static float Peak(int x, int peakX, float height, float halfWidth)
        {
            float distance = Mathf.Abs(WrappedDelta(x, peakX, MountainTileWidth));
            return height * Mathf.Max(0f, 1f - distance / halfWidth);
        }

        /// <summary>x - origin を -width/2 〜 width/2 に折り返した値</summary>
        private static int WrappedDelta(int x, int origin, int width)
        {
            int delta = (x - origin) % width;
            if (delta > width / 2) delta -= width;
            if (delta < -width / 2) delta += width;
            return delta;
        }

        private static float Ellipse(float dx, float dy, float radiusX, float radiusY)
        {
            return dx * dx / (radiusX * radiusX) + dy * dy / (radiusY * radiusY);
        }

        /// <summary>雪のきらめき・岩の模様用。Random と違い何度生成しても同じ絵になる</summary>
        private static int Hash(int x, int y)
        {
            unchecked
            {
                int h = x * 73856093 ^ y * 19349663;
                return (h ^ (h >> 13)) & int.MaxValue;
            }
        }

        private class PixelCanvas
        {
            private readonly int _width;
            private readonly int _height;
            public Color32[] Pixels { get; }

            public PixelCanvas(int width, int height)
            {
                _width = width;
                _height = height;
                Pixels = new Color32[width * height];
            }

            public void Set(int x, int y, Color32 color)
            {
                if (x < 0 || x >= _width || y < 0 || y >= _height) return;
                Pixels[y * _width + x] = color;
            }

            public void Fill(int x0, int y0, int x1, int y1, Color32 color)
            {
                for (int y = y0; y <= y1; y++)
                {
                    for (int x = x0; x <= x1; x++) Set(x, y, color);
                }
            }

            public void Outline(int x0, int y0, int x1, int y1, Color32 color)
            {
                Fill(x0, y0, x1, y0, color);
                Fill(x0, y1, x1, y1, color);
                Fill(x0, y0, x0, y1, color);
                Fill(x1, y0, x1, y1, color);
            }
        }
    }
}
