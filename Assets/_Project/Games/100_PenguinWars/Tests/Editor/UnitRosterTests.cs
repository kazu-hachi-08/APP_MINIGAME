using System.Collections.Generic;
using MiniGame.PenguinWars.Battle;
using NUnit.Framework;

namespace MiniGame.PenguinWars.Battle.Tests
{
    /// <summary>50体の定義表と計算式（§5.5）</summary>
    public class UnitRosterTests
    {
        private const int RosterSize = 50;

        private static List<UnitStats> AllStats()
        {
            var stats = new List<UnitStats>();
            foreach (UnitDefinition def in UnitDefinitions.All) stats.Add(UnitStatFormula.Calculate(def));
            return stats;
        }

        [Test]
        public void Definitions_Have50UnitsNumbered1To50()
        {
            var numbers = new HashSet<int>();
            foreach (UnitDefinition def in UnitDefinitions.All)
            {
                Assert.IsFalse(string.IsNullOrEmpty(def.Name), $"No.{def.No}");
                numbers.Add(def.No);
            }
            Assert.AreEqual(RosterSize, UnitDefinitions.All.Count);
            for (int no = 1; no <= RosterSize; no++) Assert.IsTrue(numbers.Contains(no), $"No.{no} がありません");
        }

        [Test]
        public void Definitions_CostFitsInMaxWalletCap()
        {
            // 上限より高いキャラは働きペンギンを最大にしても一生出せない（オンラインで発覚したため）
            WalletTable table = WalletTable.CreateDefault();
            int maxCap = table.GetCap(table.MaxLevel);
            foreach (UnitDefinition def in UnitDefinitions.All)
            {
                Assert.LessOrEqual(def.Cost, maxCap, $"No.{def.No} {def.Name} コスト{def.Cost}");
            }
        }

        [Test]
        public void Definitions_CostIsWithinRoleRange()
        {
            foreach (UnitDefinition def in UnitDefinitions.All)
            {
                Assert.IsTrue(UnitStatFormula.IsCostInRoleRange(def.Role, def.Cost), $"No.{def.No} {def.Role} コスト{def.Cost}");
            }
        }

        [Test]
        public void Formula_AllStatsArePositiveAndWindupBeforeInterval()
        {
            foreach (UnitStats s in AllStats())
            {
                string label = $"No.{s.UnitNo}";
                Assert.Greater(s.Cost, 0, label);
                Assert.Greater(s.Cooldown, 0f, label);
                Assert.Greater(s.MaxHp, 0, label);
                Assert.Greater(s.Attack, 0, label);
                Assert.Greater(s.Range, 0f, label);
                Assert.Greater(s.MoveSpeed, 0f, label);
                Assert.Greater(s.Windup, 0f, label);
                Assert.Less(s.Windup, s.AttackInterval, label);
                Assert.GreaterOrEqual(s.KnockbackCount, 1, label);
            }
        }

        [Test]
        public void Formula_AbilityLowersBaseStats()
        {
            var plain = new UnitDefinition(1, "a", UnitRole.Attacker, 400, false);
            var withAbility = new UnitDefinition(2, "b", UnitRole.Attacker, 400, false, new UnitAbility(UnitAbilityType.CastleKiller));

            UnitStats a = UnitStatFormula.Calculate(plain);
            UnitStats b = UnitStatFormula.Calculate(withAbility);

            Assert.Less(b.MaxHp, a.MaxHp);
            Assert.Less(b.Attack, a.Attack);
        }

        [Test]
        public void Formula_RangedOutrangesWalls()
        {
            float wallRange = UnitStatFormula.Calculate(new UnitDefinition(1, "w", UnitRole.Wall, 150, false)).Range;
            float rangedRange = UnitStatFormula.Calculate(new UnitDefinition(2, "r", UnitRole.Ranged, 400, false)).Range;

            Assert.Greater(rangedRange, wallRange);
        }

        [Test]
        public void SortByCost_OrdersByCostThenNo()
        {
            var deck = new List<UnitStats>
            {
                new UnitStats { UnitNo = 3, Cost = 200 },
                new UnitStats { UnitNo = 2, Cost = 100 },
                new UnitStats { UnitNo = 1, Cost = 200 },
            };

            DeckRandomizer.SortByCost(deck);

            CollectionAssert.AreEqual(new[] { 2, 1, 3 }, deck.ConvertAll(s => s.UnitNo));
        }
    }
}
