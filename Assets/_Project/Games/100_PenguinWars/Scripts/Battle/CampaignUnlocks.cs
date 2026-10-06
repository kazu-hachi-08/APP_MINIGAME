using System.Collections.Generic;

namespace MiniGame.PenguinWars.Battle
{
    /// <summary>
    /// ステージモードで使えるキャラ。解放状態はセーブに持たず、クリア状況から毎回計算する
    /// （定義表で解放先を入れ替えても、セーブと定義表が食い違わないように）。オンライン対戦はこれを見ない（全50体のまま）
    /// </summary>
    public static class CampaignUnlocks
    {
        /// <summary>
        /// 最初から使える10体。各役割の安いキャラを2〜3体ずつ選び、大型は入れない（ボスステージの報酬にするため）。
        /// 壁3（ペンギン・ゆきだま・ひな）/ アタッカー3（おの・さかなけん・にんじゃ）/ 遠距離2（ゆみ・つりざお）/ 妨害2（ハリセン・ねばねば）
        /// </summary>
        public static readonly IReadOnlyList<int> InitialNos = new[] { 1, 3, 6, 11, 12, 17, 23, 25, 37, 38 };

        public static bool IsUnlocked(CampaignProgress progress, int unitNo)
        {
            if (Contains(InitialNos, unitNo)) return true;
            foreach (StageDefinition stage in StageDefinitions.All)
            {
                if (Contains(stage.UnlockNos, unitNo) && progress.IsCleared(stage.Id)) return true;
            }
            return false;
        }

        /// <summary>解放済みのキャラを No 順に。定義表に無い No は入れない</summary>
        public static List<int> UnlockedNos(CampaignProgress progress)
        {
            var nos = new List<int>();
            foreach (UnitDefinition unit in UnitDefinitions.All)
            {
                if (IsUnlocked(progress, unit.No)) nos.Add(unit.No);
            }
            nos.Sort();
            return nos;
        }

        /// <summary>このキャラが仲間になるステージ。初期キャラ・どこにも割り振っていないキャラは null</summary>
        public static StageDefinition FindUnlockStage(int unitNo)
        {
            foreach (StageDefinition stage in StageDefinitions.All)
            {
                if (Contains(stage.UnlockNos, unitNo)) return stage;
            }
            return null;
        }

        private static bool Contains(IReadOnlyList<int> nos, int unitNo)
        {
            foreach (int no in nos)
            {
                if (no == unitNo) return true;
            }
            return false;
        }
    }
}
