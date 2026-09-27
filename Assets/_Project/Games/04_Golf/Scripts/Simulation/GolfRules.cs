using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace MiniGame.Golf
{
    /// <summary>
    /// 打つ順番・打数上限・罰打・順位・ホールの選び方（§6）。
    /// 順番はボール位置だけで決まるので、オンラインでも全端末で同じ計算になる（§6.3）。MonoBehaviour にしないのは EditModeテストで検証するため。
    /// </summary>
    public static class GolfRules
    {
        /// <summary>§6.5 池・OBはどちらも1打罰</summary>
        public const int PenaltyStrokes = 1;

        /// <summary>§6.4 パットを外し続けても試合が長引かないよう、パー×2で打ち切る</summary>
        private const int StrokeLimitPerPar = 2;

        /// <summary>§6.2 3ホールモードのホール数</summary>
        public const int LongModeHoleCount = 3;

        public static int StrokeLimit(int par) => par * StrokeLimitPerPar;

        /// <summary>カップインしていないまま打数上限に達した</summary>
        public static bool ShouldGiveUp(int strokes, int par) => strokes >= StrokeLimit(par);

        /// <summary>
        /// 次に打つ人の添字。まだティーショットを打っていない人がいればティーの順番、
        /// 全員打った後はカップから遠い人（同じ距離ならティーの順番が先の人）。全員終わっていれば -1。
        /// </summary>
        public static int NextPlayer(IReadOnlyList<GolfPlayerSlot> slots, IReadOnlyList<int> teeOrder, Vector2 cup)
        {
            foreach (int i in teeOrder)
            {
                if (!slots[i].IsFinished && slots[i].Strokes == 0) return i;
            }

            int farthest = -1;
            float farthestDistance = -1f;
            foreach (int i in teeOrder)
            {
                if (slots[i].IsFinished) continue;

                float distance = Vector2.DistanceSquared(slots[i].Position, cup);
                if (distance > farthestDistance)
                {
                    farthest = i;
                    farthestDistance = distance;
                }
            }

            return farthest;
        }

        /// <summary>§6.3 次のホールのティーの順番。直前のホールの打数が少ない順で、同じ打数なら前の順番を引き継ぐ</summary>
        public static List<int> NextTeeOrder(IReadOnlyList<int> previousOrder, IReadOnlyList<GolfPlayerSlot> slots)
        {
            // OrderBy は安定ソートなので、同じ打数の人は前の順番のまま残る
            return previousOrder.OrderBy(i => LastScore(slots[i])).ToList();
        }

        /// <summary>§6.6 合計打数が少ないほど上位。同じ打数なら同じ順位（1始まり）</summary>
        public static int[] Ranks(IReadOnlyList<int> totals)
        {
            var ranks = new int[totals.Count];
            for (int i = 0; i < totals.Count; i++)
            {
                ranks[i] = 1 + totals.Count(t => t < totals[i]);
            }

            return ranks;
        }

        /// <summary>§6.2 登録ホールから重複なしでランダムに選んだ添字</summary>
        public static List<int> PickHoles(int availableCount, int pickCount, Random random)
        {
            if (pickCount > availableCount)
            {
                throw new ArgumentException($"登録ホール({availableCount})より多くは選べません: {pickCount}");
            }

            // 先頭から pickCount 個だけシャッフルする（Fisher-Yates の途中まで）
            var indices = Enumerable.Range(0, availableCount).ToList();
            for (int i = 0; i < pickCount; i++)
            {
                int j = random.Next(i, availableCount);
                (indices[i], indices[j]) = (indices[j], indices[i]);
            }

            return indices.GetRange(0, pickCount);
        }

        private static int LastScore(GolfPlayerSlot slot)
        {
            return slot.HoleScores.Count > 0 ? slot.HoleScores[slot.HoleScores.Count - 1] : 0;
        }
    }
}
