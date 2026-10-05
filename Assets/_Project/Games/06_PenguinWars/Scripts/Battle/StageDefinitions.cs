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

        /// <summary>遊ぶ順で次のステージ。最後のステージ・見つからない ID なら null</summary>
        public static StageDefinition Next(string id)
        {
            for (int i = 0; i < All.Count - 1; i++)
            {
                if (All[i].Id == id) return All[i + 1];
            }
            return null;
        }

        public static int ChapterCount
        {
            get
            {
                int max = 0;
                foreach (StageDefinition stage in All) max = System.Math.Max(max, stage.Chapter);
                return max;
            }
        }

        /// <summary>章のステージを遊ぶ順に</summary>
        public static List<StageDefinition> InChapter(int chapter)
        {
            var stages = new List<StageDefinition>();
            foreach (StageDefinition stage in All)
            {
                if (stage.Chapter == chapter) stages.Add(stage);
            }
            return stages;
        }

        private static List<StageDefinition> BuildAll()
        {
            var all = new List<StageDefinition>();
            all.AddRange(Chapter1());
            return all;
        }
    }
}
