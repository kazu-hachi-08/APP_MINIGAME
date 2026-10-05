using System.Collections.Generic;
using MiniGame.PenguinWars.Battle;
using NUnit.Framework;

namespace MiniGame.PenguinWars.Battle.Tests
{
    /// <summary>撃破報酬・ペンギン砲・ランダム編成</summary>
    public class CannonRewardTests
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
