using System.Collections.Generic;
using MiniGame.PenguinWars.Battle;
using UnityEngine;

namespace MiniGame.PenguinWars.Editor
{
    /// <summary>
    /// 基本体＋パーツを重ね描きして1コマ分のピクセルを作る。1キャラを丸ごと描かずに済むよう、コマごとの違いは「ずらし」だけで表す
    /// </summary>
    public static class PenguinFrameComposer
    {
        public const int CanvasSize = 32;
        
        // 体を中央に置いたときの X（足元の中央がスプライトの原点になる）
        private const int CenterX = CanvasSize / 2;

        private readonly struct Pose
        {
            public readonly int BodyBob;
            public readonly int FeetShift;
            public readonly int Lean;
            public readonly Vector2Int HandOffset;

            public Pose(int bodyBob, int feetShift, int lean, Vector2Int handOffset)
            {
                BodyBob = bodyBob;
                FeetShift = feetShift;
                Lean = lean;
                HandOffset = handOffset;
            }
        }

        // 歩き2コマ目は体を1ドット跳ねさせ足を前に出す。攻撃は「のけぞって持ち物を振り上げる → 前に乗り出して振り下ろす」
        private static readonly Pose Walk0Pose = new Pose(0, 0, 0, Vector2Int.zero);
        private static readonly Pose Walk1Pose = new Pose(1, 1, 0, Vector2Int.zero);
        private static readonly Pose WindupPose = new Pose(0, 0, -1, new Vector2Int(-1, 2));
        private static readonly Pose StrikePose = new Pose(0, 0, 1, new Vector2Int(2, -1));

        public static Color32[] Compose(PenguinLook look, Side side, PenguinFrame frame)
        {
            switch (frame)
            {
                case PenguinFrame.Walk1: return Draw(look, side, Walk1Pose);
                case PenguinFrame.AttackWindup: return Draw(look, side, WindupPose);
                case PenguinFrame.AttackStrike: return Draw(look, side, StrikePose);
                // 後ろ向きに90度倒して、宙に浮いて飛ばされているように見せる
                case PenguinFrame.Knockback: return RotateBackward(Draw(look, side, Walk0Pose));
                default: return Draw(look, side, Walk0Pose);
            }
        }

        private static Color32[] Draw(PenguinLook look, Side side, Pose pose)
        {
            var pixels = new Color32[CanvasSize * CanvasSize];
            BodyShape body = PenguinBodyPatterns.Shapes[look.Body];
            List<PartPattern> parts = CollectParts(look);

            int lift = 0;
            bool hideFeet = false;
            foreach (PartPattern part in parts)
            {
                lift += part.BodyLift;
                hideFeet |= part.HideFeet;
            }

            // 体の左上のキャンバス座標（Y は上向き）。足の行が Y=0 に来る
            int bodyLeft = CenterX - body.Width / 2 + pose.Lean;
            int bodyTop = body.Height - 1 + pose.BodyBob + lift;
            var painter = new Painter(pixels, side, look.BodyColor);

            DrawParts(painter, parts, PartLayer.Behind, body, bodyLeft, bodyTop, pose);
            painter.Draw(body.Rows, 0, body.Height - 1, bodyLeft, bodyTop);
            if (!hideFeet)
            {
                string[] feet = { body.Rows[body.Height - 1] };
                painter.Draw(feet, 0, 1, bodyLeft - pose.Lean + pose.FeetShift, 0);
            }
            DrawParts(painter, parts, PartLayer.Front, body, bodyLeft, bodyTop, pose);
            return pixels;
        }

        /// <summary>描く順: 乗り物・背中 → 頭 → 手。手の持ち物を一番手前にして、攻撃が見えるようにする</summary>
        private static List<PartPattern> CollectParts(PenguinLook look)
        {
            var parts = new List<PartPattern>();
            AddPart(parts, PenguinPartPatterns.Backs, look.Back);
            AddPart(parts, PenguinPartPatterns.Heads, look.Head);
            AddPart(parts, PenguinPartPatterns.Hands, look.Hand);
            return parts;
        }

        private static void AddPart(List<PartPattern> parts, Dictionary<string, PartPattern> table, string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            if (table.TryGetValue(id, out PartPattern part)) parts.Add(part);
            else Debug.LogWarning($"[PenguinFrameComposer] パーツ '{id}' が見つかりません");
        }

        private static void DrawParts(Painter painter, List<PartPattern> parts, PartLayer layer,
            BodyShape body, int bodyLeft, int bodyTop, Pose pose)
        {
            foreach (PartPattern part in parts)
            {
                if (part.Layer != layer) continue;

                Vector2Int anchor = AnchorOnCanvas(part.Attach, body, bodyLeft, bodyTop);
                if (part.Attach == PartAttach.Hand) anchor += pose.HandOffset;
                painter.Draw(part.Rows, 0, part.Rows.Length, anchor.x - part.Anchor.x, anchor.y + part.Anchor.y);
            }
        }

        private static Vector2Int AnchorOnCanvas(PartAttach attach, BodyShape body, int bodyLeft, int bodyTop)
        {
            switch (attach)
            {
                case PartAttach.Head: return ToCanvas(body.Head, bodyLeft, bodyTop);
                case PartAttach.Hand: return ToCanvas(body.Hand, bodyLeft, bodyTop);
                case PartAttach.Back: return ToCanvas(body.Back, bodyLeft, bodyTop);
                // 乗り物は体が跳ねても揺れないよう、キャンバスの足元に固定する
                default: return new Vector2Int(CenterX, 0);
            }
        }

        private static Vector2Int ToCanvas(Vector2Int marker, int bodyLeft, int bodyTop)
        {
            return new Vector2Int(bodyLeft + marker.x, bodyTop - marker.y);
        }

        /// <summary>反時計回りに90度。右向きのペンギンの頭が後ろ（左）に倒れる</summary>
        private static Color32[] RotateBackward(Color32[] source)
        {
            var result = new Color32[source.Length];
            for (int y = 0; y < CanvasSize; y++)
            {
                for (int x = 0; x < CanvasSize; x++)
                {
                    int newX = CanvasSize - 1 - y;
                    int newY = x;
                    result[newY * CanvasSize + newX] = source[y * CanvasSize + x];
                }
            }
            return result;
        }

        /// <summary>文字パターンをキャンバスに塗る。はみ出した分は捨てる（攻撃のずらしで端から出ることがあるため）</summary>
        private readonly struct Painter
        {
            private readonly Color32[] _pixels;
            private readonly Side _side;
            private readonly string _bodyColor;

            public Painter(Color32[] pixels, Side side, string bodyColor)
            {
                _pixels = pixels;
                _side = side;
                _bodyColor = bodyColor;
            }

            /// <param name="rowStart">rows のうち描き始める行</param>
            /// <param name="rowCount">描く行数</param>
            /// <param name="left">パターン左端のキャンバス X</param>
            /// <param name="top">rows[rowStart] を置くキャンバス Y（上向き）</param>
            public void Draw(string[] rows, int rowStart, int rowCount, int left, int top)
            {
                for (int r = 0; r < rowCount; r++)
                {
                    string row = rows[rowStart + r];
                    int y = top - r;
                    if (y < 0 || y >= CanvasSize) continue;

                    for (int c = 0; c < row.Length; c++)
                    {
                        int x = left + c;
                        if (row[c] == PenguinPalette.Transparent || x < 0 || x >= CanvasSize) continue;
                        _pixels[y * CanvasSize + x] = PenguinPalette.Resolve(row[c], _side, _bodyColor);
                    }
                }
            }
        }
    }
}
