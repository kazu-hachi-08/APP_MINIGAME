using System.Collections.Generic;
using MiniGame.PenguinWars.Battle;
using NUnit.Framework;

namespace MiniGame.PenguinWars.Battle.Tests
{
    /// <summary>Phase 4: 敵レベル・撃破報酬・ペンギン砲</summary>
    public class EndlessTests
    {
        private const float StepTime = 1f / 30f;
        private const float FieldLength = 10f;
        private const float CannonChargeTime = 1f;

        private static UnitStats Unit(int no, int cost, int hp = 100, float speed = 0f)
        {
            return new UnitStats
            {
                UnitNo = no, Cost = cost, MaxHp = hp, Attack = 10, Range = 0.1f,
                AttackInterval = 1f, Windup = 0.1f, MoveSpeed = speed,
            };
        }

        /// <summary>さかなが時間で増えない表にして、撃破報酬だけを見られるようにする</summary>
        private static BattleWorld CreateWorld(params UnitStats[] rightDeck)
        {
            var world = new BattleWorld(new BattleSettings
            {
                FieldLength = FieldLength,
                RightCastleInvincible = true,
                RightSpawnsFree = true,
                WalletTable = new WalletTable(new[] { 10000 }, new[] { 0f }, new int[0]),
                CannonChargeTime = CannonChargeTime,
            });
            world.SetDeck(Side.Left, new[] { Unit(1, 50) });
            world.SetDeck(Side.Right, rightDeck);
            return world;
        }

        private static void Run(BattleWorld world, float seconds)
        {
            int steps = (int)System.Math.Round(seconds / StepTime);
            for (int i = 0; i < steps; i++) world.Step(StepTime);
        }

        private static List<UnitStats> TickDirector(EnemyWaveDirector director, float seconds)
        {
            var all = new List<UnitStats>();
            var spawns = new List<UnitStats>();
            int steps = (int)System.Math.Round(seconds / StepTime);
            for (int i = 0; i < steps; i++)
            {
                director.Tick(StepTime, spawns);
                all.AddRange(spawns);
            }
            return all;
        }

        [Test]
        public void Director_LevelRisesEvery30Seconds()
        {
            var director = new EnemyWaveDirector(new EnemyWaveSettings(), new[] { Unit(1, 100) }, 0);
            Assert.AreEqual(1, director.Level);

            TickDirector(director, 29f);
            Assert.AreEqual(1, director.Level);

            TickDirector(director, 2f);
            Assert.AreEqual(2, director.Level);
        }

        [Test]
        public void Director_SpawnsOnlyWithinCostLimit()
        {
            // レベルを上げずに、レベル1の上限（300 + 250 = 550）だけを確かめる
            var settings = new EnemyWaveSettings { LevelUpInterval = 1000f };
            var director = new EnemyWaveDirector(settings, new[] { Unit(1, 100), Unit(2, 551), Unit(3, 3000) }, 0);

            List<UnitStats> spawned = TickDirector(director, 60f);

            Assert.That(spawned.Count, Is.GreaterThan(5));
            foreach (UnitStats stats in spawned) Assert.LessOrEqual(stats.Cost, director.CostLimit);
        }

        [Test]
        public void Director_BossAppearsAtLevel5EvenIfOverCostLimit()
        {
            var settings = new EnemyWaveSettings { LevelUpInterval = 1f, BaseSpawnInterval = 1000f };
            var director = new EnemyWaveDirector(settings, new[] { Unit(1, 100), Unit(50, 5000) }, 0);

            List<UnitStats> spawned = TickDirector(director, 4.1f);

            Assert.AreEqual(5, director.Level);
            Assert.AreEqual(1, spawned.Count);
            Assert.AreEqual(50, spawned[0].UnitNo);
        }

        [Test]
        public void Director_SpawnsScaledStats()
        {
            var settings = new EnemyWaveSettings { LevelUpInterval = 1000f, BaseSpawnInterval = 0.5f, MinSpawnInterval = 0.5f };
            var director = new EnemyWaveDirector(settings, new[] { Unit(1, 100, hp: 100) }, 0);

            List<UnitStats> spawned = TickDirector(director, 1f);

            // レベル1の倍率 1.0 + 1 × 0.15
            Assert.AreEqual(115, spawned[0].MaxHp);
            Assert.AreEqual(12, spawned[0].Attack);
            Assert.AreEqual(100, spawned[0].Cost);
        }

        [Test]
        public void World_EnemyWavesSpawnRightUnitsAndRaiseLevelEvent()
        {
            BattleWorld world = CreateWorld(Unit(2, 100));
            world.SetEnemyWaves(new EnemyWaveDirector(new EnemyWaveSettings(), new[] { Unit(2, 100) }, 0));

            Run(world, 31f);

            Assert.AreEqual(2, world.EnemyLevel);
            Assert.That(world.CountUnits(Side.Right), Is.GreaterThan(0));
            var events = new List<BattleEvent>();
            world.DrainEvents(events);
            Assert.IsTrue(events.Exists(e => e.Type == BattleEventType.EnemyLevelUp && e.Amount == 2));
        }

        [Test]
        public void Kill_GivesHalfCostToKillerAndCounts()
        {
            // 砲の範囲（自城から 6）の内側まで歩かせてから撃つ
            BattleWorld world = CreateWorld(Unit(2, 300, hp: 50, speed: 2f));
            world.Enqueue(BattleCommand.Spawn(Side.Right, 0));
            Run(world, 2f);
            int fishBefore = world.GetWallet(Side.Left).Fish;

            world.Enqueue(BattleCommand.FireCannon(Side.Left));
            world.Step(StepTime);

            Assert.AreEqual(0, world.CountUnits(Side.Right));
            Assert.AreEqual(fishBefore + 150, world.GetWallet(Side.Left).Fish);
            Assert.AreEqual(1, world.GetKillCount(Side.Left));
        }

        [Test]
        public void Cannon_CannotFireBeforeCharged()
        {
            BattleWorld world = CreateWorld(Unit(2, 100));
            world.Enqueue(BattleCommand.Spawn(Side.Right, 0));
            world.Step(StepTime);

            world.Enqueue(BattleCommand.FireCannon(Side.Left));
            world.Step(StepTime);

            Assert.AreEqual(100, world.Units[0].Hp);
            Assert.IsFalse(world.GetCannon(Side.Left).IsReady);
        }

        [Test]
        public void Cannon_HitsOnlyEnemiesWithinRange()
        {
            // slot0 は止まったまま出撃位置（8.5）に、slot1 は先に出して砲の範囲（自城から 6）の内側まで歩かせる
            BattleWorld world = CreateWorld(Unit(2, 100), Unit(3, 100, hp: 300, speed: 2f));
            world.Enqueue(BattleCommand.Spawn(Side.Right, 1));
            Run(world, 2f);
            world.Enqueue(BattleCommand.Spawn(Side.Right, 0));
            world.Step(StepTime);

            world.Enqueue(BattleCommand.FireCannon(Side.Left));
            world.Step(StepTime);

            Assert.AreEqual(2, world.Units.Count);
            foreach (UnitState unit in world.Units)
            {
                int expectedHp = unit.UnitNo == 3 ? 200 : 100;
                Assert.AreEqual(expectedHp, unit.Hp, $"No.{unit.UnitNo} X={unit.X}");
            }
            Assert.IsFalse(world.GetCannon(Side.Left).IsReady);
        }

        [Test]
        public void DeckRandomizer_PicksWithoutDuplicates()
        {
            var pool = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 };

            List<int> deck = DeckRandomizer.Pick(pool, 10, new System.Random(0));

            Assert.AreEqual(10, deck.Count);
            Assert.AreEqual(10, new HashSet<int>(deck).Count);
            Assert.AreEqual(3, DeckRandomizer.Pick(new[] { 1, 2, 3 }, 10, new System.Random(0)).Count);
        }
    }
}
