using System.Collections.Generic;
using MiniGame.PenguinWars.Battle;
using NUnit.Framework;

namespace MiniGame.PenguinWars.Battle.Tests
{
    /// <summary>Phase 8: 50体の定義表・計算式・ランダム編成の制約</summary>
    public class UnitRosterTests
    {
        private const int RosterSize = 50;
        private const int DeckSize = 10;
        private const int MinWalls = 2;

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
        public void PickDeck_HasAtLeastTwoWallsAndNoDuplicates()
        {
            List<UnitStats> pool = AllStats();
            for (int seed = 0; seed < 200; seed++)
            {
                List<UnitStats> deck = DeckRandomizer.PickDeck(pool, DeckSize, MinWalls, new System.Random(seed));

                Assert.AreEqual(DeckSize, deck.Count, $"seed {seed}");
                var numbers = new HashSet<int>();
                int walls = 0;
                foreach (UnitStats s in deck)
                {
                    numbers.Add(s.UnitNo);
                    if (s.Role == UnitRole.Wall) walls++;
                }
                Assert.AreEqual(DeckSize, numbers.Count, $"seed {seed} で重複");
                Assert.GreaterOrEqual(walls, MinWalls, $"seed {seed}");
            }
        }

        [Test]
        public void PickDeck_VariesBySeed()
        {
            List<UnitStats> pool = AllStats();
            var decks = new HashSet<string>();
            for (int seed = 0; seed < 10; seed++)
            {
                var numbers = new List<int>();
                foreach (UnitStats s in DeckRandomizer.PickDeck(pool, DeckSize, MinWalls, new System.Random(seed))) numbers.Add(s.UnitNo);
                numbers.Sort();
                decks.Add(string.Join(",", numbers));
            }
            Assert.Greater(decks.Count, 1);
        }

        [Test]
        public void PickDeck_TakesAllWallsWhenFewerThanMinimum()
        {
            var pool = new[]
            {
                new UnitStats { UnitNo = 1, Role = UnitRole.Wall },
                new UnitStats { UnitNo = 2, Role = UnitRole.Attacker },
                new UnitStats { UnitNo = 3, Role = UnitRole.Ranged },
            };

            List<UnitStats> deck = DeckRandomizer.PickDeck(pool, 2, MinWalls, new System.Random(0));

            Assert.AreEqual(2, deck.Count);
            Assert.IsTrue(deck.Exists(s => s.UnitNo == 1));
        }

        [Test]
        public void Director_BossIsPickedFromLargeRole()
        {
            var settings = new EnemyWaveSettings { LevelUpInterval = 1f, BaseSpawnInterval = 1000f };
            var pool = new[]
            {
                new UnitStats { UnitNo = 1, Cost = 100, MaxHp = 100, Role = UnitRole.Wall },
                new UnitStats { UnitNo = 2, Cost = 9000, MaxHp = 100, Role = UnitRole.Ranged },
                new UnitStats { UnitNo = 43, Cost = 2500, MaxHp = 100, Role = UnitRole.Large },
            };
            var director = new EnemyWaveDirector(settings, pool, 0);
            var spawns = new List<UnitStats>();
            var all = new List<UnitStats>();
            for (int i = 0; i < 125; i++)
            {
                director.Tick(1f / 30f, spawns);
                all.AddRange(spawns);
            }

            Assert.AreEqual(5, director.Level);
            Assert.AreEqual(1, all.Count);
            Assert.AreEqual(43, all[0].UnitNo);
        }
    }
}
