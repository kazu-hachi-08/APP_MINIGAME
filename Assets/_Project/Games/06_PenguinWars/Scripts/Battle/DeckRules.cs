using System.Collections.Generic;

namespace MiniGame.PenguinWars.Battle
{
    /// <summary>ステージモードの編成のきまり（ちょうど10体・重複なし・全員解放済み）と、足りないときの補完</summary>
    public static class DeckRules
    {
        /// <summary>出撃ボタン・編成画面の枠の数（PenguinWarsBalance.DeckSize と同じ値）</summary>
        public const int DeckSize = 10;

        public static bool IsValid(IReadOnlyList<int> deckNos, IReadOnlyCollection<int> unlockedNos)
        {
            if (deckNos == null || deckNos.Count != DeckSize) return false;

            var seen = new HashSet<int>();
            var unlocked = new HashSet<int>(unlockedNos);
            foreach (int no in deckNos)
            {
                if (!unlocked.Contains(no) || !seen.Add(no)) return false;
            }
            return true;
        }

        /// <summary>
        /// 保存された編成から、未解放・重複・定義表に無いキャラを外し、空いた枠を解放済みのキャラからコストの低い順に埋める。
        /// 定義表を変えて保存済みの編成が使えなくなっても、そのまま出撃できるようにするため。
        /// 解放済みが10体に満たなければ、いるだけ返す
        /// </summary>
        public static List<int> FillDefault(IReadOnlyList<int> deckNos, IReadOnlyCollection<int> unlockedNos)
        {
            var unlocked = new HashSet<int>(unlockedNos);
            var deck = new List<int>();
            if (deckNos != null)
            {
                foreach (int no in deckNos)
                {
                    if (deck.Count >= DeckSize) break;
                    if (unlocked.Contains(no) && !deck.Contains(no)) deck.Add(no);
                }
            }

            foreach (int no in ByCost(unlockedNos))
            {
                if (deck.Count >= DeckSize) break;
                if (!deck.Contains(no)) deck.Add(no);
            }
            return deck;
        }

        /// <summary>ステージの編成制限（コスト上限・禁止の役割）に当たらないか。当たるキャラは編成に入れたままでよく、そのステージだけ出せない</summary>
        public static bool IsAllowed(StageDefinition stage, UnitStats stats)
        {
            return IsAllowed(stage.MaxUnitCost, stage.BannedRoles, stats.Cost, stats.Role);
        }

        public static bool IsAllowed(int maxUnitCost, IReadOnlyList<UnitRole> bannedRoles, UnitStats stats)
        {
            return IsAllowed(maxUnitCost, bannedRoles, stats.Cost, stats.Role);
        }

        /// <summary>UI はキャラNo しか持たないので、定義表のコスト・役割で判定する。定義表に無い No は通す（出撃側で弾かれる）</summary>
        public static bool IsAllowed(StageDefinition stage, int unitNo)
        {
            foreach (UnitDefinition unit in UnitDefinitions.All)
            {
                if (unit.No == unitNo) return IsAllowed(stage.MaxUnitCost, stage.BannedRoles, unit.Cost, unit.Role);
            }
            return true;
        }

        /// <param name="maxUnitCost">0 なら上限なし</param>
        private static bool IsAllowed(int maxUnitCost, IReadOnlyList<UnitRole> bannedRoles, int cost, UnitRole role)
        {
            if (maxUnitCost > 0 && cost > maxUnitCost) return false;
            if (bannedRoles == null) return true;

            foreach (UnitRole banned in bannedRoles)
            {
                if (banned == role) return false;
            }
            return true;
        }

        /// <summary>今の進み具合で出撃する編成（保存された編成を補完して、コストの低い順に並べたもの）</summary>
        public static List<int> CurrentDeck(CampaignProgress progress)
        {
            List<int> unlocked = CampaignUnlocks.UnlockedNos(progress);
            return ByCost(FillDefault(progress.LastDeckNos, unlocked));
        }

        /// <summary>出撃ボタンと同じ並び（コストの低い順、同じコストは No 順）。定義表に無い No は外す</summary>
        public static List<int> ByCost(IEnumerable<int> unitNos)
        {
            var costByNo = new Dictionary<int, int>();
            foreach (UnitDefinition unit in UnitDefinitions.All) costByNo[unit.No] = unit.Cost;

            var sorted = new List<int>();
            foreach (int no in unitNos)
            {
                if (costByNo.ContainsKey(no)) sorted.Add(no);
            }
            sorted.Sort((a, b) =>
            {
                int byCost = costByNo[a].CompareTo(costByNo[b]);
                return byCost != 0 ? byCost : a.CompareTo(b);
            });
            return sorted;
        }
    }
}
