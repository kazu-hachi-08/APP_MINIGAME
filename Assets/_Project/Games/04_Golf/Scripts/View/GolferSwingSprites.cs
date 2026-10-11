using System;
using System.Collections.Generic;
using UnityEngine;

namespace MiniGame.Golf
{
    /// <summary>
    /// 背後視点のゴルファーのスイングを、コマ送りのドット絵として実行時に描く。
    /// 体はボールに正対させ、打つ方向の後ろにあるカメラからは横向き（ダウンザライン）に見える姿で描く。
    /// 打つ方向へ背中を向けて体の横で振る姿より、ゴルフ中継の後方カメラの見え方に近く、人の動きとして自然に見える。
    /// 1コマを「色そのまま・シャツ・帽子・髪」の4枚に分け、白で描いた3枚にプレイヤー色・キャラの色を掛ける。
    /// 姿勢は関節の位置（背の高さを1、足元を原点とした座標）で持ち、太さのある線と丸で塗るので、数字を変えるだけで絵を直せる。
    /// 画像アセットを増やさないのは GolfShapeSprites と同じ理由（2人開発でのアセット競合を避ける）。
    /// </summary>
    public static class GolferSwingSprites
    {
        /// <summary>1コマを重ねて描く4枚。並びは GolferSprite の SpriteRenderer の並びと揃える</summary>
        public enum Layer { Base, Shirt, Cap, Hair }

        public const int LayerCount = 4;

        // 背の高さを40ピクセルで描く。ドットの粗さを残しつつ、肘・膝・帽子のつばが読める大きさ
        private const int FigurePixels = 40;
        // 横は足元から左右に背の高さの0.5ずつ。縦は振り上げたクラブが頭の上へ出る分の余白をとる
        private const int Width = 40;
        private const int Height = 46;
        private static readonly Vector2 Pivot = new Vector2(0.5f, 0f);

        // 体の太さ・大きさ（背の高さを1として）。頭は背の1/6弱にして、前の図形の頭でっかちな体型から大人の体型に寄せる
        private const float TorsoRadius = 0.075f;
        private const float BackViewTorsoRadius = 0.1f; // 後ろ姿は肩幅が見えるので太くする
        private const float HipRadius = 0.08f;
        private const float ThighRadius = 0.045f;
        private const float ShinRadius = 0.038f;
        private const float ShoeRadius = 0.03f;
        private const float ShoeLength = 0.07f;
        private const float ArmRadius = 0.03f;
        private const float SleeveRadius = 0.036f;
        private const float NeckRadius = 0.035f;
        private const float HeadRadius = 0.085f;
        private const float GloveRadius = 0.032f;
        private const float ShaftRadius = 0.015f;
        private const float ClubHeadRadius = 0.028f;

        // 頭の中心からの高さで帽子と顔を分ける。つばは帽子の縁から前へ1ピクセルの厚みで出す
        private const float CapBrimHeight = 0.03f;
        private const float CapBillThickness = 0.025f;
        private const float CapBillLength = 0.06f;
        // 横顔は頭の中心より少し後ろから前を肌にする（後頭部だけ髪が見える）
        private const float FaceBack = 0.02f;

        // 袖が覆う割合（肩から）。向こう側の腕は伸びたままなので短めに見せる
        private const float LeadSleeveRatio = 0.3f;
        private const float TrailSleeveRatio = 0.6f;
        // シャツは腰から肩までのこの割合の位置から描き、下にズボンの腰を見せる
        private const float WaistRatio = 0.25f;

        // 左足（目標側）はスイング中ずっと地面に着けておく
        private static readonly Vector2 LeadAnkle = new Vector2(0f, 0.03f);

        private static readonly Color SkinColor = new Color32(246, 206, 168, 255);
        private static readonly Color SkinShadeColor = new Color32(214, 170, 136, 255);
        private static readonly Color PantsColor = new Color32(60, 64, 88, 255);
        private static readonly Color PantsShadeColor = new Color32(42, 45, 64, 255);
        private static readonly Color ShoeColor = new Color32(240, 240, 240, 255);
        private static readonly Color OutlineColor = new Color32(34, 30, 38, 255);
        private static readonly Color ShaftColor = new Color32(200, 200, 208, 255);
        private static readonly Color ClubHeadColor = new Color32(70, 70, 80, 255);
        private static readonly Color GloveColor = Color.white;
        // 色を掛ける部分の陰。上半身は手前の袖（白）と見分けられる程度に、向こう側の袖とつばははっきり暗くする
        private static readonly Color TorsoShade = new Color(0.9f, 0.9f, 0.92f);
        private static readonly Color FarShade = new Color(0.72f, 0.72f, 0.75f);

        // 構え → トップ。ダウンスイングは同じコマを逆にたどる
        private static readonly Pose[] BackswingPoses =
        {
            // 構え：前傾して膝を軽く曲げ、お尻を後ろへ引く。腕は肩の真下に垂らしてヘッドをボールの横に置く
            new Pose(hip: V(-0.06f, 0.48f), shoulder: V(0.16f, 0.76f), head: V(0.24f, 0.86f),
                leadKnee: V(0.08f, 0.27f), trailKnee: V(0.07f, 0.27f), trailAnkle: V(-0.01f, 0.03f),
                trailElbow: V(0.18f, 0.58f), hands: V(0.18f, 0.4f), clubHead: V(0.34f, 0.02f)),
            // 始動：ヘッドが手元より先に体の後ろ（カメラ側）へ引かれる
            new Pose(hip: V(-0.06f, 0.48f), shoulder: V(0.155f, 0.76f), head: V(0.24f, 0.86f),
                leadKnee: V(0.08f, 0.27f), trailKnee: V(0.07f, 0.27f), trailAnkle: V(-0.01f, 0.03f),
                trailElbow: V(0.17f, 0.59f), hands: V(0.14f, 0.42f), clubHead: V(-0.04f, 0.2f)),
            // クラブが地面と平行：シャフトがカメラの方を向くので短く見える
            new Pose(hip: V(-0.06f, 0.48f), shoulder: V(0.15f, 0.76f), head: V(0.24f, 0.86f),
                leadKnee: V(0.08f, 0.27f), trailKnee: V(0.07f, 0.27f), trailAnkle: V(-0.01f, 0.03f),
                trailElbow: V(0.15f, 0.61f), hands: V(0.08f, 0.53f), clubHead: V(-0.13f, 0.6f)),
            // 左腕が地面と平行：右肘をたたんでシャフトを立てる
            new Pose(hip: V(-0.05f, 0.48f), shoulder: V(0.14f, 0.76f), head: V(0.24f, 0.86f),
                leadKnee: V(0.09f, 0.27f), trailKnee: V(0.07f, 0.27f), trailAnkle: V(-0.01f, 0.03f),
                trailElbow: V(0.07f, 0.6f), hands: V(0.02f, 0.71f), clubHead: V(-0.04f, 1f)),
            // トップ：手元は右肩の上。シャフトは目標を向くので、頭の上を横切って短く見える
            new Pose(hip: V(-0.05f, 0.48f), shoulder: V(0.13f, 0.76f), head: V(0.24f, 0.86f),
                leadKnee: V(0.1f, 0.27f), trailKnee: V(0.07f, 0.27f), trailAnkle: V(-0.01f, 0.03f),
                trailElbow: V(0.04f, 0.74f), hands: V(0.08f, 0.93f), clubHead: V(0.34f, 0.99f)),
        };

        // インパクト → フィニッシュ
        private static readonly Pose[] FollowThroughPoses =
        {
            // インパクト：腰が先に回って右膝が送り込まれ、右のかかとが浮き始める
            new Pose(hip: V(-0.03f, 0.49f), shoulder: V(0.16f, 0.76f), head: V(0.24f, 0.86f),
                leadKnee: V(0.07f, 0.27f), trailKnee: V(0.1f, 0.27f), trailAnkle: V(0f, 0.05f),
                trailElbow: V(0.2f, 0.57f), hands: V(0.21f, 0.4f), clubHead: V(0.34f, 0.02f)),
            // 振り抜き：両腕が目標へ伸び、シャフトが目標を向いて短く見える
            new Pose(hip: V(-0.01f, 0.5f), shoulder: V(0.14f, 0.77f), head: V(0.22f, 0.87f),
                leadKnee: V(0.06f, 0.28f), trailKnee: V(0.12f, 0.28f), trailAnkle: V(0.01f, 0.07f),
                trailElbow: V(0.19f, 0.66f), hands: V(0.23f, 0.55f), clubHead: V(0.37f, 0.78f)),
            // 前傾が起き上がりながら体が目標へ向き、手元が左肩の上へ上がる
            new Pose(hip: V(0f, 0.51f), shoulder: V(0.08f, 0.79f), head: V(0.13f, 0.89f),
                leadKnee: V(0.04f, 0.28f), trailKnee: V(0.1f, 0.29f), trailAnkle: V(0f, 0.08f),
                trailElbow: V(0.18f, 0.8f), hands: V(0.12f, 0.9f), clubHead: V(-0.1f, 1.02f)),
            // フィニッシュ：目標を向いてまっすぐ立ち、背中がカメラに見える。クラブは首の後ろに担ぐ
            new Pose(hip: V(0f, 0.52f), shoulder: V(0f, 0.8f), head: V(0.01f, 0.9f),
                leadKnee: V(0.03f, 0.28f), trailKnee: V(0.05f, 0.29f), trailAnkle: V(0.02f, 0.08f),
                trailElbow: V(0.08f, 0.9f), hands: V(-0.1f, 0.97f), clubHead: V(0.22f, 0.78f), facingBall: false),
        };

        private static Sprite[][] _backswing;
        private static Sprite[][] _followThrough;

        /// <summary>構え → トップのコマ。1コマは Layer の並びの4枚</summary>
        public static Sprite[][] Backswing => Cached(ref _backswing, BackswingPoses);

        /// <summary>インパクト → フィニッシュのコマ。1コマは Layer の並びの4枚</summary>
        public static Sprite[][] FollowThrough => Cached(ref _followThrough, FollowThroughPoses);

        /// <summary>再生終了で破棄されたテクスチャを null と見なすため、Unity の == null で判定する（GolfShapeSprites と同じ）</summary>
        private static Sprite[][] Cached(ref Sprite[][] cache, Pose[] poses)
        {
            if (cache == null || cache[0][0] == null) cache = Array.ConvertAll(poses, Draw);
            return cache;
        }

        private static Vector2 V(float x, float y) => new Vector2(x, y);

        /// <summary>奥の部位から順に塗り重ねる。縁取りはクラブより先に付け、細いシャフトが太って見えないようにする</summary>
        private static Sprite[] Draw(Pose pose)
        {
            var canvas = new Canvas();
            DrawLeg(canvas, pose.Hip, pose.LeadKnee, LeadAnkle, PantsShadeColor);
            DrawLeg(canvas, pose.Hip, pose.TrailKnee, pose.TrailAnkle, PantsColor);
            DrawBody(canvas, pose);
            DrawLeadArm(canvas, pose);
            DrawHead(canvas, pose);
            DrawTrailArm(canvas, pose);
            canvas.Outline(OutlineColor);
            DrawClub(canvas, pose);
            return canvas.ToSprites();
        }

        /// <summary>つま先は地面に着けたまま。足首が上がるとかかとが浮いて靴が傾き、蹴り出した足に見える</summary>
        private static void DrawLeg(Canvas canvas, Vector2 hip, Vector2 knee, Vector2 ankle, Color pants)
        {
            canvas.Segment(hip, knee, ThighRadius, Layer.Base, pants);
            canvas.Segment(knee, ankle, ShinRadius, Layer.Base, pants);
            canvas.Segment(ankle, new Vector2(ankle.x + ShoeLength, ShoeRadius), ShoeRadius, Layer.Base, ShoeColor);
        }

        private static void DrawBody(Canvas canvas, Pose pose)
        {
            canvas.Segment(pose.Hip, pose.Hip, HipRadius, Layer.Base, PantsColor);
            float torsoRadius = pose.FacingBall ? TorsoRadius : BackViewTorsoRadius;
            canvas.Segment(Vector2.Lerp(pose.Hip, pose.Shoulder, WaistRatio), pose.Shoulder, torsoRadius, Layer.Shirt, TorsoShade);
            // 首は肩と頭の中間から描き、シャツの肩口を肌で塗りつぶさないようにする
            canvas.Segment(Vector2.Lerp(pose.Shoulder, pose.Head, 0.5f), pose.Head, NeckRadius, Layer.Base, SkinColor);
        }

        /// <summary>左腕（向こう側）はゴルフのスイングと同じく伸ばしたまま、肩から手元へまっすぐ描く</summary>
        private static void DrawLeadArm(Canvas canvas, Pose pose)
        {
            canvas.Segment(pose.Shoulder, pose.Hands, ArmRadius, Layer.Base, SkinShadeColor);
            canvas.Segment(pose.Shoulder, Vector2.Lerp(pose.Shoulder, pose.Hands, LeadSleeveRatio), SleeveRadius,
                Layer.Shirt, FarShade);
        }

        /// <summary>右腕（手前）は肘で曲げる。トップで肘をたたむ動きが一番スイングらしく見える</summary>
        private static void DrawTrailArm(Canvas canvas, Pose pose)
        {
            canvas.Segment(pose.Shoulder, pose.TrailElbow, ArmRadius, Layer.Base, SkinColor);
            canvas.Segment(pose.TrailElbow, pose.Hands, ArmRadius, Layer.Base, SkinColor);
            canvas.Segment(pose.Shoulder, Vector2.Lerp(pose.Shoulder, pose.TrailElbow, TrailSleeveRatio), SleeveRadius,
                Layer.Shirt, Color.white);
        }

        /// <summary>横顔はボール側の下半分を肌、上を帽子、残りを髪にする。後ろ姿は顔もつばも見えない</summary>
        private static void DrawHead(Canvas canvas, Pose pose)
        {
            Vector2 head = pose.Head;
            canvas.Segment(head, head, HeadRadius, Layer.Hair, Color.white);
            if (pose.FacingBall)
            {
                canvas.Paint(p => IsInHead(p, head) && p.x - head.x > -FaceBack && p.y - head.y < CapBrimHeight,
                    Layer.Base, SkinColor);
                canvas.Paint(p => IsInBill(p - head), Layer.Cap, FarShade);
            }

            canvas.Paint(p => IsInHead(p, head) && p.y - head.y >= CapBrimHeight, Layer.Cap, Color.white);
        }

        private static bool IsInHead(Vector2 p, Vector2 head) => (p - head).sqrMagnitude <= HeadRadius * HeadRadius;

        private static bool IsInBill(Vector2 fromHead)
        {
            return fromHead.x >= 0f && fromHead.x <= HeadRadius + CapBillLength
                && fromHead.y >= CapBrimHeight - CapBillThickness && fromHead.y < CapBrimHeight;
        }

        /// <summary>グローブはクラブの後に描き、グリップを握っているように見せる</summary>
        private static void DrawClub(Canvas canvas, Pose pose)
        {
            canvas.Segment(pose.Hands, pose.ClubHead, ShaftRadius, Layer.Base, ShaftColor);
            canvas.Segment(pose.ClubHead, pose.ClubHead, ClubHeadRadius, Layer.Base, ClubHeadColor);
            canvas.Segment(pose.Hands, pose.Hands, GloveRadius, Layer.Base, GloveColor);
        }

        /// <summary>1コマの関節の位置。左＝目標側（向こう）、右＝カメラ側（手前）。右打ち</summary>
        private readonly struct Pose
        {
            public readonly Vector2 Hip, Shoulder, Head, LeadKnee, TrailKnee, TrailAnkle, TrailElbow, Hands, ClubHead;

            /// <summary>false で目標を向いた後ろ姿（フィニッシュ）</summary>
            public readonly bool FacingBall;

            public Pose(Vector2 hip, Vector2 shoulder, Vector2 head, Vector2 leadKnee, Vector2 trailKnee,
                Vector2 trailAnkle, Vector2 trailElbow, Vector2 hands, Vector2 clubHead, bool facingBall = true)
            {
                Hip = hip;
                Shoulder = shoulder;
                Head = head;
                LeadKnee = leadKnee;
                TrailKnee = trailKnee;
                TrailAnkle = trailAnkle;
                TrailElbow = trailElbow;
                Hands = hands;
                ClubHead = clubHead;
                FacingBall = facingBall;
            }
        }

        /// <summary>
        /// 1ピクセルを1つの Layer だけが持つキャンバス。後から塗った部位が前に来る。
        /// 4枚のピクセルが重ならないので、SpriteRenderer 側は描く順を気にしなくてよい
        /// </summary>
        private sealed class Canvas
        {
            private const int Empty = -1;
            private readonly Color[] _colors = new Color[Width * Height];
            private readonly int[] _layers = new int[Width * Height];

            public Canvas()
            {
                Array.Fill(_layers, Empty);
            }

            /// <summary>2点を結ぶ太さのある線（両端は丸）。a == b なら丸になる</summary>
            public void Segment(Vector2 a, Vector2 b, float radius, Layer layer, Color color)
            {
                Paint(p => DistanceToSegment(p, a, b) <= radius, layer, color);
            }

            public void Paint(Func<Vector2, bool> isInside, Layer layer, Color color)
            {
                for (int i = 0; i < _colors.Length; i++)
                {
                    if (!isInside(PixelCenter(i))) continue;
                    _colors[i] = color;
                    _layers[i] = (int)layer;
                }
            }

            /// <summary>体のまわりの透明なピクセルを縁取りにする。芝の緑の上でも体の輪郭が埋もれないように</summary>
            public void Outline(Color color)
            {
                var edges = new List<int>();
                for (int y = 0; y < Height; y++)
                {
                    for (int x = 0; x < Width; x++)
                    {
                        if (IsFilled(x, y)) continue;
                        if (IsFilled(x - 1, y) || IsFilled(x + 1, y) || IsFilled(x, y - 1) || IsFilled(x, y + 1))
                            edges.Add(y * Width + x);
                    }
                }

                foreach (int i in edges)
                {
                    _colors[i] = color;
                    _layers[i] = (int)Layer.Base;
                }
            }

            public Sprite[] ToSprites()
            {
                var sprites = new Sprite[LayerCount];
                for (int layer = 0; layer < LayerCount; layer++)
                {
                    sprites[layer] = CreateSprite(layer);
                }

                return sprites;
            }

            /// <summary>ドットをぼかさないよう Point で拡大する</summary>
            private Sprite CreateSprite(int layer)
            {
                var pixels = new Color[_colors.Length];
                for (int i = 0; i < pixels.Length; i++)
                {
                    pixels[i] = _layers[i] == layer ? _colors[i] : Color.clear;
                }

                var texture = new Texture2D(Width, Height, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp,
                };
                texture.SetPixels(pixels);
                texture.Apply();
                return Sprite.Create(texture, new Rect(0, 0, Width, Height), Pivot, FigurePixels);
            }

            private bool IsFilled(int x, int y)
            {
                return x >= 0 && x < Width && y >= 0 && y < Height && _layers[y * Width + x] != Empty;
            }

            /// <summary>ピクセルの中心を、足元を原点・背の高さを1とした座標にする</summary>
            private static Vector2 PixelCenter(int i)
            {
                float x = i % Width + 0.5f - Width * Pivot.x;
                float y = i / Width + 0.5f;
                return new Vector2(x, y) / FigurePixels;
            }

            private static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
            {
                Vector2 ab = b - a;
                float lengthSq = ab.sqrMagnitude;
                float t = lengthSq > 0f ? Mathf.Clamp01(Vector2.Dot(p - a, ab) / lengthSq) : 0f;
                return Vector2.Distance(p, a + ab * t);
            }
        }
    }
}
