using System;
using System.Collections.Generic;

namespace MiniGame.PenguinWars.Battle
{
    /// <summary>ドラフトの候補選び（Pick）と出撃ボタンの並べ替え（SortByCost）</summary>
    public static class DeckRandomizer
    {
        /// <summary>
        /// 出撃ボタンを安い順に並べる。序盤は手持ちのさかなが少ないので、すぐ出せるキャラを左に固めて探しやすくするため。
        /// 同じコストは UnitNo 順にして、並びを毎回同じにする
        /// </summary>
        public static void SortByCost(List<UnitStats> deck)
        {
            deck.Sort((a, b) => DeckRules.CompareByCost(a.Cost, a.UnitNo, b.Cost, b.UnitNo));
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
