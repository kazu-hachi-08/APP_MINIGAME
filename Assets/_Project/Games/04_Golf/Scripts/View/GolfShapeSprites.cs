using System;
using UnityEngine;

namespace MiniGame.Golf
{
    /// <summary>
    /// 図形のスプライトを実行時に作る（§5、Phase 10 で陰影付きの見た目に置き換え）。
    /// 画像アセットを増やさずに済むので、2人開発でのアセット競合も起きない。
    /// モルックの ShapeSprites と同じ作りだが、ミニゲーム同士を依存させないためゴルフ側にも持つ。
    /// 旗以外は1ワールド単位の大きさで、中心がピボット。
    /// </summary>
    public static class GolfShapeSprites
    {
        private const int CircleResolution = 64;

        // 左上から光が当たっている前提で陰影をつける（ボール・木で揃える）
        private static readonly Vector2 HighlightCenter = new Vector2(-0.35f, 0.35f);

        private static readonly Color BallShadeColor = new Color(0.72f, 0.75f, 0.82f);
        private static readonly Color BallEdgeColor = new Color(0.55f, 0.58f, 0.65f);
        private const float BallEdgeStart = 0.85f;

        // 影の縁をぼかし始める位置（半径に対する割合）
        private const float SoftEdgeStart = 0.45f;

        private static readonly Color CupInsideColor = new Color(0.05f, 0.07f, 0.05f);
        private static readonly Color CupRimColor = new Color(0.95f, 0.95f, 0.92f);
        private const float CupRimStart = 0.72f;

        private static readonly Color TreeColor = new Color(0.12f, 0.4f, 0.16f);
        private static readonly Color TreeLightColor = new Color(0.3f, 0.62f, 0.26f);
        private static readonly Color TreeDarkColor = new Color(0.05f, 0.2f, 0.08f);
        private static readonly Color TreeShadowColor = new Color(0f, 0f, 0f, 0.3f);
        // 葉のかたまりのでこぼこ（半径の揺れ）と、影を落とす向き
        private const float TreeRadius = 0.78f;
        private const float TreeBumpAmount = 0.07f;
        private const int TreeBumpCount = 7;
        private static readonly Vector2 TreeShadowOffset = new Vector2(0.14f, -0.14f);

        // 旗：幅32×高さ64ピクセルを1.2ユニットの高さで描く。ピボットはポールの根元（カップの中心に立てる）
        private const int FlagWidth = 32;
        private const int FlagHeight = 64;
        private const float FlagPixelsPerUnit = 64f / 1.2f;
        private const int PoleX = 2;
        private const int PoleWidth = 2;
        private const int PennantBottom = 42;
        private const int PennantTop = 62;
        private static readonly Color PoleColor = new Color(0.95f, 0.95f, 0.95f);
        private static readonly Color PennantColor = new Color(0.9f, 0.15f, 0.15f);
        private static readonly Color PennantShadeColor = new Color(0.68f, 0.08f, 0.1f);

        // ゴルファーの胴と脚：幅32×高さ64ピクセルを1ユニットの高さで描く。ピボットは足元（地面に立てる）。
        // 白で描いてプレイヤー色を掛けるので、脚は暗い灰色にしてズボンに見せる
        private const int GolferWidth = 32;
        private const int GolferHeight = 64;
        private const int LegTop = 26;
        private const int LegHalfGap = 2;
        private const int LegWidth = 6;
        private const int TorsoBottom = 22;
        private const int TorsoTop = 58;
        private const int TorsoHalfWidth = 12;
        private const int TorsoCornerRadius = 6;
        private static readonly Color GolferLegColor = new Color(0.4f, 0.4f, 0.42f);
        private static readonly Color GolferShadeColor = new Color(0.72f, 0.72f, 0.75f);

        private static Sprite _circle;
        private static Sprite _square;
        private static Sprite _ball;
        private static Sprite _softCircle;
        private static Sprite _cup;
        private static Sprite _flag;
        private static Sprite _tree;
        private static Sprite _golferBody;

        public static Sprite Square
        {
            get
            {
                if (_square == null)
                {
                    // 1ピクセルの白を1ユニットに引き伸ばす
                    var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                    texture.SetPixel(0, 0, Color.white);
                    texture.Apply();
                    _square = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
                }

                return _square;
            }
        }

        /// <summary>白い円。色を付けて使う（プレイヤー色の縁取り・着地予測など）</summary>
        public static Sprite Circle => Cached(ref _circle, () => CreateRound(p => Color.white));

        /// <summary>陰影と縁のついた白いボール。明るいグリーンの上でも輪郭が埋もれないようにする</summary>
        public static Sprite Ball => Cached(ref _ball, () => CreateRound(BallPixel));

        /// <summary>縁のぼけた円（影用）</summary>
        public static Sprite SoftCircle => Cached(ref _softCircle, () => CreateRound(p =>
            new Color(1f, 1f, 1f, Mathf.InverseLerp(1f, SoftEdgeStart, p.magnitude))));

        /// <summary>白い縁取りのある黒い穴。グリーンの上でカップの位置をはっきり見せる</summary>
        public static Sprite Cup => Cached(ref _cup, () =>
            CreateRound(p => p.magnitude < CupRimStart ? CupInsideColor : CupRimColor));

        /// <summary>葉のかたまりと、右下に落ちる影。コース外の飾り用</summary>
        public static Sprite Tree => Cached(ref _tree, () => CreateSprite(CircleResolution, CircleResolution,
            TreePixel, new Vector2(0.5f, 0.5f), CircleResolution));

        /// <summary>ポールと三角の旗。ピボットはポールの根元</summary>
        public static Sprite Flag => Cached(ref _flag, () => CreateSprite(FlagWidth, FlagHeight, FlagPixel,
            new Vector2((PoleX + PoleWidth * 0.5f) / FlagWidth, 0f), FlagPixelsPerUnit));

        /// <summary>背中から見たゴルファーの胴と脚（頭は別の円で重ねる）。ピボットは足元</summary>
        public static Sprite GolferBody => Cached(ref _golferBody, () => CreateSprite(GolferWidth, GolferHeight,
            GolferBodyPixel, new Vector2(0.5f, 0f), GolferHeight));

        /// <summary>
        /// ??= だと破棄済み（再生終了で消えたテクスチャ）を null と見なさないため、Unity の == null で判定する
        /// </summary>
        private static Sprite Cached(ref Sprite cache, Func<Sprite> create)
        {
            if (cache == null) cache = create();
            return cache;
        }

        private static Color BallPixel(Vector2 p)
        {
            float shade = Mathf.Clamp01(Vector2.Distance(p, HighlightCenter) / 1.6f);
            Color color = Color.Lerp(Color.white, BallShadeColor, shade);
            if (p.magnitude > BallEdgeStart) color = BallEdgeColor;
            return color;
        }

        private static Color TreePixel(int x, int y)
        {
            Vector2 p = ToUnitCircle(x, y, CircleResolution);
            float canopy = CanopyAlpha(p);
            float shadow = CanopyAlpha(p - TreeShadowOffset) * TreeShadowColor.a;

            float light = Mathf.Clamp01(1f - Vector2.Distance(p, HighlightCenter * TreeRadius) / TreeRadius);
            Color leaves = light > 0.5f ? Color.Lerp(TreeColor, TreeLightColor, (light - 0.5f) * 2f)
                : Color.Lerp(TreeDarkColor, TreeColor, light * 2f);

            // 葉の上に影を重ねず、葉の外側にだけ影を見せる
            float alpha = canopy + shadow * (1f - canopy);
            if (alpha <= 0f) return Color.clear;
            Color rgb = (leaves * canopy + TreeShadowColor * shadow * (1f - canopy)) / alpha;
            rgb.a = alpha;
            return rgb;
        }

        private static float CanopyAlpha(Vector2 p)
        {
            float angle = Mathf.Atan2(p.y, p.x);
            float radius = TreeRadius + Mathf.Sin(angle * TreeBumpCount) * TreeBumpAmount;
            return EdgeAlpha(p.magnitude, radius, CircleResolution);
        }

        private static Color GolferBodyPixel(int x, int y)
        {
            float fromCenter = x + 0.5f - GolferWidth * 0.5f;
            if (IsInsideTorso(fromCenter, y))
            {
                // 左上から光が当たる前提（ボール・木と揃える）で右側を暗くする
                float shade = Mathf.InverseLerp(-TorsoHalfWidth, TorsoHalfWidth, fromCenter);
                return Color.Lerp(Color.white, GolferShadeColor, shade);
            }

            float legX = Mathf.Abs(fromCenter);
            bool isLeg = y < LegTop && legX >= LegHalfGap && legX < LegHalfGap + LegWidth;
            return isLeg ? GolferLegColor : Color.clear;
        }

        /// <summary>角を丸めた長方形。四角いままだと人に見えにくいので肩と腰を丸める</summary>
        private static bool IsInsideTorso(float fromCenter, int y)
        {
            if (y < TorsoBottom || y > TorsoTop || Mathf.Abs(fromCenter) > TorsoHalfWidth) return false;

            float innerX = TorsoHalfWidth - TorsoCornerRadius;
            float cornerX = Mathf.Abs(fromCenter) - innerX;
            float cornerY = Mathf.Max(TorsoBottom + TorsoCornerRadius - y, y - (TorsoTop - TorsoCornerRadius));
            if (cornerX <= 0f || cornerY <= 0f) return true;
            return cornerX * cornerX + cornerY * cornerY <= TorsoCornerRadius * TorsoCornerRadius;
        }

        private static Color FlagPixel(int x, int y)
        {
            if (x >= PoleX && x < PoleX + PoleWidth) return PoleColor;
            if (x < PoleX + PoleWidth || y < PennantBottom || y > PennantTop) return Color.clear;

            // 旗は右へいくほど細くなる三角形。下半分を少し暗くして、はためいて見せる
            float center = (PennantBottom + PennantTop) * 0.5f;
            float halfHeight = (PennantTop - PennantBottom) * 0.5f;
            float t = (x - PoleX - PoleWidth) / (float)(FlagWidth - PoleX - PoleWidth);
            if (Mathf.Abs(y - center) > halfHeight * (1f - t)) return Color.clear;
            return y < center ? PennantShadeColor : PennantColor;
        }

        /// <summary>中心(0,0)・半径1の座標で色を決める丸いスプライトを作る。円の外は透明</summary>
        private static Sprite CreateRound(Func<Vector2, Color> pixel)
        {
            return CreateSprite(CircleResolution, CircleResolution, (x, y) =>
            {
                Vector2 p = ToUnitCircle(x, y, CircleResolution);
                Color color = pixel(p);
                color.a *= EdgeAlpha(p.magnitude, 1f, CircleResolution);
                return color;
            }, new Vector2(0.5f, 0.5f), CircleResolution);
        }

        private static Sprite CreateSprite(int width, int height, Func<int, int, Color> pixel, Vector2 pivot,
            float pixelsPerUnit)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    texture.SetPixel(x, y, pixel(x, y));
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, width, height), pivot, pixelsPerUnit);
        }

        private static Vector2 ToUnitCircle(int x, int y, int resolution)
        {
            float half = resolution * 0.5f;
            return new Vector2((x + 0.5f - half) / half, (y + 0.5f - half) / half);
        }

        /// <summary>縁を1ピクセルぶんぼかしてギザギザを目立たなくする</summary>
        private static float EdgeAlpha(float distance, float radius, int resolution)
        {
            return Mathf.Clamp01((radius - distance) * resolution * 0.5f);
        }
    }
}
