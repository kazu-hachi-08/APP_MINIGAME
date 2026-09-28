using System;
using UnityEngine;

namespace MiniGame.Golf
{
    /// <summary>
    /// 図形のスプライトを実行時に作る。
    /// 画像アセットを増やさずに済むので、2人開発でのアセット競合も起きない。
    /// モルックの ShapeSprites と同じ作りだが、ミニゲーム同士を依存させないためゴルフ側にも持つ。
    /// 旗以外は1ワールド単位の大きさで、中心がピボット。
    /// </summary>
    public static class GolfShapeSprites
    {
        private const int CircleResolution = 64;

        // 左上から光が当たっている前提で陰影をつける（ボール・木で揃える）
        private static readonly Vector2 HighlightCenter = new Vector2(-0.35f, 0.35f);
        // 光の中心からこの距離（半径1に対する値）で陰の色になりきる。円の反対側の縁でもまだ少し明るさを残す
        private const float ShadeFalloffDistance = 1.6f;

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

        // ゴルファーは背の高さ64ピクセルを1ユニットで描き、上半身・下半身・頭を別のスプライトにする。
        // 上半身と下半身を分けるのは、肩と腰の回り方を変えて捻転を見せるため
        private const float GolferPixelsPerUnit = 64f;
        private const int GolferPartWidth = 32;
        private static readonly Color GolferShadeColor = new Color(0.72f, 0.72f, 0.75f);

        // 上半身（シャツ）：ピボットは腰。白で描いてプレイヤー色を掛ける。腰より肩を広くして逆三角形にする
        private const int TorsoHeight = 30;
        private const float TorsoWaistHalfWidth = 8f;
        private const float TorsoShoulderHalfWidth = 12f;
        private const float TorsoShoulderRadius = 6f;
        // 腰から胸までで肩幅まで広がる（上半身の高さに対する割合）
        private const float TorsoChestRatio = 0.6f;
        private const int BeltHeight = 3;
        private static readonly Color BeltColor = new Color(0.18f, 0.16f, 0.15f);

        // 下半身（ズボン・靴）：ピボットは足元。プレイヤー色を掛けないので色をそのまま描く。
        // 足を肩幅に開いた構えに見せるため、腰から足元へ向かって脚を外へ開く
        private const int LegsHeight = 29;
        private const int HipBottom = 22;
        private const float HipHalfWidth = 8f;
        private const float LegHipX = 4f;
        private const float LegFootX = 7f;
        private const float LegHalfWidth = 3f;
        private const int ShoeHeight = 3;
        private const float ShoeHalfWidth = 3.6f;
        private static readonly Color PantsColor = new Color(0.32f, 0.32f, 0.36f);
        private static readonly Color PantsShadeColor = new Color(0.22f, 0.22f, 0.25f);
        private static readonly Color ShoeColor = new Color(0.95f, 0.95f, 0.95f);

        // 頭（後ろ姿）：髪の丸の上に帽子を重ねる。帽子の後ろのアジャスターの穴から髪が見える。
        // どちらも白で描き、キャラの帽子・髪の色を掛ける
        private const float CapBottom = -0.1f;
        private const float CapOpeningTop = 0.14f;
        private const float CapOpeningHalfWidth = 0.18f;
        private static readonly Color HeadShadeColor = new Color(0.6f, 0.6f, 0.62f);

        private static Sprite _circle;
        private static Sprite _square;
        private static Sprite _ball;
        private static Sprite _softCircle;
        private static Sprite _cup;
        private static Sprite _flag;
        private static Sprite _tree;
        private static Sprite _golferTorso;
        private static Sprite _golferLegs;
        private static Sprite _golferHead;
        private static Sprite _golferCap;

        /// <summary>1ピクセルの白を1ユニットに引き伸ばした四角</summary>
        public static Sprite Square => Cached(ref _square, CreateWhitePixel);

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

        /// <summary>背中から見たゴルファーの上半身（シャツとベルト）。ピボットは腰なので、前傾は腰から曲がる</summary>
        public static Sprite GolferTorso => Cached(ref _golferTorso, () => CreateSprite(GolferPartWidth, TorsoHeight,
            GolferTorsoPixel, new Vector2(0.5f, 0f), GolferPixelsPerUnit));

        /// <summary>背中から見たゴルファーの下半身（ズボンと靴）。ピボットは足元</summary>
        public static Sprite GolferLegs => Cached(ref _golferLegs, () => CreateSprite(GolferPartWidth, LegsHeight,
            GolferLegsPixel, new Vector2(0.5f, 0f), GolferPixelsPerUnit));

        /// <summary>後ろから見た頭（髪）。直径1ユニットで中心がピボット。白で描いて髪の色を掛ける</summary>
        public static Sprite GolferHead => Cached(ref _golferHead, () => CreateRound(HeadShadePixel));

        /// <summary>頭に重ねる帽子。頭と同じ大きさ・ピボットなので、頭の子に置けばそのまま重なる</summary>
        public static Sprite GolferCap => Cached(ref _golferCap, () => CreateRound(GolferCapPixel));

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
            Color color = Color.Lerp(Color.white, BallShadeColor, LightShade(p));
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

        private static Color GolferTorsoPixel(int x, int y)
        {
            float fromCenter = x + 0.5f - GolferPartWidth * 0.5f;
            float chest = Mathf.Clamp01(y / (TorsoHeight * TorsoChestRatio));
            float halfWidth = Mathf.Lerp(TorsoWaistHalfWidth, TorsoShoulderHalfWidth, chest);
            if (Mathf.Abs(fromCenter) > halfWidth || !IsInsideShoulder(fromCenter, y)) return Color.clear;
            if (y < BeltHeight) return BeltColor;

            // 左上から光が当たる前提（ボール・木と揃える）で右側を暗くする
            float shade = Mathf.InverseLerp(-halfWidth, halfWidth, fromCenter);
            return Color.Lerp(Color.white, GolferShadeColor, shade);
        }

        /// <summary>肩の角を丸める。四角いままだと人に見えにくい</summary>
        private static bool IsInsideShoulder(float fromCenter, int y)
        {
            float cornerX = Mathf.Abs(fromCenter) - (TorsoShoulderHalfWidth - TorsoShoulderRadius);
            float cornerY = y + 0.5f - (TorsoHeight - TorsoShoulderRadius);
            if (cornerX <= 0f || cornerY <= 0f) return true;
            return cornerX * cornerX + cornerY * cornerY <= TorsoShoulderRadius * TorsoShoulderRadius;
        }

        private static Color GolferLegsPixel(int x, int y)
        {
            float fromCenter = x + 0.5f - GolferPartWidth * 0.5f;
            float shade = Mathf.InverseLerp(-HipHalfWidth, HipHalfWidth, fromCenter);
            Color pants = Color.Lerp(PantsColor, PantsShadeColor, shade);
            if (y >= HipBottom) return Mathf.Abs(fromCenter) <= HipHalfWidth ? pants : Color.clear;

            float legCenter = Mathf.Lerp(LegFootX, LegHipX, y / (float)HipBottom);
            float fromLeg = Mathf.Abs(Mathf.Abs(fromCenter) - legCenter);
            if (y < ShoeHeight) return fromLeg <= ShoeHalfWidth ? ShoeColor : Color.clear;
            return fromLeg <= LegHalfWidth ? pants : Color.clear;
        }

        private static Color HeadShadePixel(Vector2 p)
        {
            return Color.Lerp(Color.white, HeadShadeColor, LightShade(p));
        }

        /// <summary>左上の光の中心から離れるほど 0→1 に増える陰の濃さ（ボール・頭で揃える）</summary>
        private static float LightShade(Vector2 p)
        {
            return Mathf.Clamp01(Vector2.Distance(p, HighlightCenter) / ShadeFalloffDistance);
        }

        private static Color GolferCapPixel(Vector2 p)
        {
            bool isCap = p.y > CapBottom && !(p.y < CapOpeningTop && Mathf.Abs(p.x) < CapOpeningHalfWidth);
            return isCap ? HeadShadePixel(p) : Color.clear;
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

        private static Sprite CreateWhitePixel()
        {
            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
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
