using System.Collections.Generic;

namespace MiniGame.PenguinWars.Battle
{
    /// <summary>
    /// 全ステージの定義表。章ごとに partial で別ファイルにして、2人で別の章を触ってもぶつからないようにする。
    /// All の並び順がそのまま遊ぶ順
    /// </summary>
    public static partial class StageDefinitions
    {
        private static List<StageDefinition> s_all;

        public static IReadOnlyList<StageDefinition> All => s_all ??= BuildAll();

        /// <summary>見つからなければ null</summary>
        public static StageDefinition Find(string id)
        {
            foreach (StageDefinition stage in All)
            {
                if (stage.Id == id) return stage;
            }
            return null;
        }

        private static List<StageDefinition> BuildAll()
        {
            var all = new List<StageDefinition>();
            all.AddRange(Chapter1());
            return all;
        }
    }
}
