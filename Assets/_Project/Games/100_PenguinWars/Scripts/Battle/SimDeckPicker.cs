using System.Collections.Generic;

namespace MiniGame.PenguinWars.Battle
{
    /// <summary>
    /// 検証用の編成。「そのステージを遊ぶ時点で解放されている」キャラから、役割が偏らないよう
    /// 壁3・アタッカー3・遠距離2・妨害/大型2 を目安に、コストの高い順に選ぶ（人間も解放したての強いキャラを使うため）
    /// </summary>
    public static class SimDeckPicker
    {
        private struct Quota
        {
            public UnitRole[] Roles;
            public int Count;
        }

        private static readonly Quota[] Quotas =
        {
            new Quota { Roles = new[] { UnitRole.Wall }, Count = 3 },
            new Quota { Roles = new[] { UnitRole.Attacker }, Count = 3 },
            new Quota { Roles = new[] { UnitRole.Ranged }, Count = 2 },
            new Quota { Roles = new[] { UnitRole.Disruptor, UnitRole.Large }, Count = 2 },
        };

        public static List<int> Pick(StageDefinition stage)
        {
            return Pick(AvailableNos(stage), stage);
        }

        /// <summary>
        /// ステージの編成制限に当たるキャラは選ばない（人間も外すため）。役割の目安で埋まらなかった枠は残りから高い順に埋める。
        /// 候補が10体に満たなければ、いるだけ返す
        /// </summary>
        public static List<int> Pick(IEnumerable<int> availableNos, StageDefinition stage)
        {
            List<UnitDefinition> candidates = ByCostDescending(availableNos, stage);
            var deck = new List<int>();
            foreach (Quota quota in Quotas)
            {
                int taken = 0;
                foreach (UnitDefinition unit in candidates)
                {
                    if (taken >= quota.Count) break;
                    if (!HasRole(quota.Roles, unit.Role) || deck.Contains(unit.No)) continue;

                    deck.Add(unit.No);
                    taken++;
                }
            }

            foreach (UnitDefinition unit in candidates)
            {
                if (deck.Count >= DeckRules.DeckSize) break;
                if (!deck.Contains(unit.No)) deck.Add(unit.No);
            }
            return deck;
        }

        /// <summary>初期キャラ ＋ 遊ぶ順でこのステージより前のステージの解放キャラ（前のステージはすべてクリアしている前提）</summary>
        public static List<int> AvailableNos(StageDefinition stage)
        {
            var nos = new List<int>(CampaignUnlocks.InitialNos);
            foreach (StageDefinition before in StageDefinitions.All)
            {
                if (before.Id == stage.Id) break;
                foreach (int no in before.UnlockNos)
                {
                    if (!nos.Contains(no)) nos.Add(no);
                }
            }
            return nos;
        }

        private static List<UnitDefinition> ByCostDescending(IEnumerable<int> availableNos, StageDefinition stage)
        {
            var available = new HashSet<int>(availableNos);
            var candidates = new List<UnitDefinition>();
            foreach (UnitDefinition unit in UnitDefinitions.All)
            {
                if (available.Contains(unit.No) && DeckRules.IsAllowed(stage, unit.No)) candidates.Add(unit);
            }
            candidates.Sort((a, b) => DeckRules.CompareByCost(-a.Cost, a.No, -b.Cost, b.No));
            return candidates;
        }

        private static bool HasRole(UnitRole[] roles, UnitRole role)
        {
            foreach (UnitRole candidate in roles)
            {
                if (candidate == role) return true;
            }
            return false;
        }
    }
}
