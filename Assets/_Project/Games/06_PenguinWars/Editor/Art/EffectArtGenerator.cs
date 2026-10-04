using System;
using UnityEngine;

namespace MiniGame.PenguinWars.Editor
{
    /// <summary>
    /// 演出のドット絵（仕様書 §9）: 出撃の煙・ヒットの火花・撃破の魂・ペンギン砲のビーム。
    /// 小さく丸い形ばかりなので、文字パターンではなく円や十字の式で描く
    /// </summary>
    public static class EffectArtGenerator
    {
        private const string EffectDirectory = PenguinSpriteWriter.SpriteDirectory + "/Effects";
        private const int SmokeSize = 16;
        private const int SparkSize = 16;
        private const int SoulWidth = 12;
        private const int SoulHeight = 16;
        private const int BeamWidth = 16;
        private const int BeamHeight = 8;

        public static readonly string SmokePath = EffectDirectory + "/Smoke.png";
        public static readonly string SparkPath = EffectDirectory + "/Spark.png";
        public static readonly string SoulPath = EffectDirectory + "/Soul.png";
        public static readonly string BeamPath = EffectDirectory + "/Beam.png";

        private static readonly Vector2 Center = new Vector2(0.5f, 0.5f);
        // ビームは城の前を原点にして横へ伸ばすので、原点を左端にする
        private static readonly Vector2 LeftCenter = new Vector2(0f, 0.5f);

        private static readonly Color32 Clear = new Color32(0, 0, 0, 0);
        private static readonly Color32 SmokeLight = new Color32(250, 250, 250, 255);
        private static readonly Color32 SmokeShade = new Color32(205, 210, 220, 255);
        private static readonly Color32 SmokeOutline = new Color32(150, 155, 170, 255);
        private static readonly Color32 SparkCore = new Color32(255, 255, 255, 255);
        private static readonly Color32 SparkEdge = new Color32(255, 240, 150, 255);
        private static readonly Color32 SoulBody = new Color32(235, 250, 255, 230);
        private static readonly Color32 SoulOutline = new Color32(140, 200, 240, 230);
        private static readonly Color32 SoulEye = new Color32(40, 60, 90, 255);
        // ビームは中心が白く、外側ほど濃い青。縦1列ぶんの色（下から上）
        private static readonly Color32[] BeamColumn =
        {
            new Color32(60, 120, 255, 110), new Color32(60, 140, 255, 255), new Color32(140, 210, 255, 255), new Color32(255, 255, 255, 255),
            new Color32(255, 255, 255, 255), new Color32(140, 210, 255, 255), new Color32(60, 140, 255, 255), new Color32(60, 120, 255, 110),
        };

        public static void GenerateAll()
        {
            int ppu = PenguinSpriteWriter.PixelsPerUnit;
            PenguinSpriteWriter.Save(SmokePath, BuildSmoke(), SmokeSize, SmokeSize, ppu, Center);
            PenguinSpriteWriter.Save(SparkPath, BuildSpark(), SparkSize, SparkSize, ppu, Center);
            PenguinSpriteWriter.Save(SoulPath, BuildSoul(), SoulWidth, SoulHeight, ppu, Center);
            PenguinSpriteWriter.Save(BeamPath, BuildBeam(), BeamWidth, BeamHeight, ppu, LeftCenter);
        }

        /// <summary>3つの円を重ねたもくもく。縁を灰色で囲み、下側に影を付けて立体に見せる</summary>
        internal static Color32[] BuildSmoke()
        {
            Func<int, int, bool> inside = (x, y) =>
                InCircle(x, y, 4.5f, 5f, 3.8f) || InCircle(x, y, 11f, 5.5f, 3.8f) || InCircle(x, y, 7.5f, 9.5f, 4.6f);

            return Paint(SmokeSize, SmokeSize, (x, y) =>
            {
                if (!inside(x, y)) return Clear;
                if (IsEdge(inside, x, y)) return SmokeOutline;
                // 下に2ドットずらした位置が外なら、下の縁に近い＝影の部分
                return inside(x, y - 2) ? SmokeLight : SmokeShade;
            });
        }

        /// <summary>縦横に長く、斜めに短く光る星。中心ほど白い</summary>
        internal static Color32[] BuildSpark()
        {
            const float center = (SparkSize - 1) * 0.5f;
            const float longArm = center;
            const float shortArm = 3.5f;
            const float coreRadius = 3f;
            return Paint(SparkSize, SparkSize, (x, y) =>
            {
                float dx = Mathf.Abs(x - center);
                float dy = Mathf.Abs(y - center);
                bool cross = (dx < 1f && dy <= longArm) || (dy < 1f && dx <= longArm);
                bool diagonal = Mathf.Abs(dx - dy) < 0.6f && dx < shortArm;
                if (!cross && !diagonal) return Clear;
                return dx + dy < coreRadius ? SparkCore : SparkEdge;
            });
        }

        /// <summary>下が丸く、上がとがって少し傾いた火の玉に目を付けた魂</summary>
        internal static Color32[] BuildSoul()
        {
            const float centerX = (SoulWidth - 1) * 0.5f;
            const float bodyY = 5f;
            const float radius = 5f;
            const float tipHeight = 10f;
            const float lean = 0.25f;
            Func<int, int, bool> inside = (x, y) =>
            {
                if (y <= bodyY) return InCircle(x, y, centerX, bodyY, radius);
                // 上に行くほど細く、少し横に傾ける
                float rise = y - bodyY;
                float halfWidth = radius * (1f - rise / tipHeight);
                return Mathf.Abs(x - centerX - rise * lean) <= halfWidth;
            };

            return Paint(SoulWidth, SoulHeight, (x, y) =>
            {
                if (!inside(x, y)) return Clear;
                if (IsEdge(inside, x, y)) return SoulOutline;
                bool eye = y == (int)bodyY + 1 && (x == (int)centerX - 1 || x == (int)centerX + 2);
                return eye ? SoulEye : SoulBody;
            });
        }

        /// <summary>横には同じ色が続くので、縦1列の色を横に並べるだけ</summary>
        internal static Color32[] BuildBeam()
        {
            return Paint(BeamWidth, BeamHeight, (x, y) => BeamColumn[y]);
        }

        private static bool InCircle(int x, int y, float centerX, float centerY, float radius)
        {
            float dx = x - centerX;
            float dy = y - centerY;
            return dx * dx + dy * dy <= radius * radius;
        }

        private static bool IsEdge(Func<int, int, bool> inside, int x, int y)
        {
            return !inside(x - 1, y) || !inside(x + 1, y) || !inside(x, y - 1) || !inside(x, y + 1);
        }

        /// <summary>Texture2D.SetPixels32 と同じく、左下から右へ、下の行から上の行へ並べる</summary>
        private static Color32[] Paint(int width, int height, Func<int, int, Color32> colorAt)
        {
            var pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    pixels[y * width + x] = colorAt(x, y);
                }
            }
            return pixels;
        }
    }
}
