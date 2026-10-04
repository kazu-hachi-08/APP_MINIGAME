using System;
using System.Collections.Generic;

namespace MiniGame.PenguinWars.Battle
{
    /// <summary>エンドレスのランダム編成（仕様書 §2.1）。全キャラから重複なしで抽選する</summary>
    public static class DeckRandomizer
    {
        /// <summary>pool が count より少なければ全員を並べ替えて返す</summary>
        public static List<T> Pick<T>(IReadOnlyList<T> pool, int count, Random random)
        {
            var shuffled = new List<T>(pool);
            // 先頭から count 個だけ Fisher-Yates で確定させる（全体を混ぜる必要はないため）
            int take = Math.Min(count, shuffled.Count);
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
