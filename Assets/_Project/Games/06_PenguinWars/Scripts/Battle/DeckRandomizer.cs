using System;
using System.Collections.Generic;

namespace MiniGame.PenguinWars.Battle
{
    /// <summary>ドラフトの候補選びと出撃ボタンの並べ替え。PickDeck（ランダム編成）は Phase 3 の編成画面で使わなくなった（テストだけが使っている）</summary>
    public static class DeckRandomizer
    {
        /// <summary>
        /// 壁を最低 minWalls 体入れて count 体選ぶ（壁がいないと序盤で詰むため）。並び順はコストの低い順。
        /// 壁が minWalls 体に満たないプールでは、いる壁を全員入れる
        /// </summary>
        public static List<UnitStats> PickDeck(IReadOnlyList<UnitStats> pool, int count, int minWalls, Random random)
        {
            var walls = new List<UnitStats>();
            var others = new List<UnitStats>();
            foreach (UnitStats stats in pool)
            {
                if (stats.Role == UnitRole.Wall) walls.Add(stats);
                else others.Add(stats);
            }

            List<UnitStats> deck = Pick(walls, Math.Min(minWalls, count), random);
            // 確定枠に選ばれなかった壁も、残りの枠の候補に戻す
            foreach (UnitStats wall in walls)
            {
                if (!deck.Contains(wall)) others.Add(wall);
            }
            deck.AddRange(Pick(others, count - deck.Count, random));
            SortByCost(deck);
            return deck;
        }

        /// <summary>
        /// 出撃ボタンを安い順に並べる。序盤は手持ちのさかなが少ないので、すぐ出せるキャラを左に固めて探しやすくするため。
        /// 同じコストは UnitNo 順にして、並びを毎回同じにする
        /// </summary>
        public static void SortByCost(List<UnitStats> deck)
        {
            deck.Sort((a, b) =>
            {
                int byCost = a.Cost.CompareTo(b.Cost);
                return byCost != 0 ? byCost : a.UnitNo.CompareTo(b.UnitNo);
            });
        }

        /// <summary>pool が count より少なければ全員を並べ替えて返す</summary>
        public static List<T> Pick<T>(IReadOnlyList<T> pool, int count, Random random)
        {
            var shuffled = new List<T>(pool);
            // 先頭から count 個だけ Fisher-Yates で確定させる（全体を混ぜる必要はないため）
            int take = Math.Max(0, Math.Min(count, shuffled.Count));
            for (int i = 0; i < take; i++)
            {
                int j = random.Next(i, shuffled.Count);
                (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
            }
            shuffled.RemoveRange(take, shuffled.Count - take);
            return shuffled;
        }
    }
}
