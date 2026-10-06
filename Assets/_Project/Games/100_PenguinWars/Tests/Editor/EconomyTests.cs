using MiniGame.PenguinWars.Battle;
using NUnit.Framework;
using static MiniGame.PenguinWars.Battle.Tests.BattleTestUtil;

namespace MiniGame.PenguinWars.Battle.Tests
{
    public class EconomyTests
    {
        private const int Cost = 100;
        private const float Cooldown = 2f;

        // レベル1: 上限200・20/秒、レベル2: 上限400・40/秒（最大）。数字を読みやすくするための小さい表
        private static WalletTable SmallTable()
        {
            return new WalletTable(new[] { 200, 400 }, new[] { 20f, 40f }, new[] { 50 });
        }

        private static BattleWorld CreateWorld(bool rightFree = false)
        {
            var world = new BattleWorld(new BattleSettings { FieldLength = 10f, WalletTable = SmallTable(), RightSpawnsFree = rightFree });
            var stats = new UnitStats
            {
                UnitNo = 1, Cost = Cost, Cooldown = Cooldown, MaxHp = 100, Attack = 1, Range = 1f,
                AttackInterval = 1f, Windup = 0.1f, MoveSpeed = 1f,
            };
            world.SetDeck(Side.Left, new[] { stats });
            world.SetDeck(Side.Right, new[] { stats });
            return world;
        }

        [Test]
        public void Fish_IncreasesOverTimeAndStopsAtCap()
        {
            BattleWorld world = CreateWorld();
            WalletState wallet = world.GetWallet(Side.Left);

            Run(world, 1f);
            Assert.That(wallet.Fish, Is.InRange(19, 20));

            Run(world, 30f);
            Assert.AreEqual(200, wallet.Fish);
        }

        [Test]
        public void NotEnoughFish_CannotSpawn()
        {
            BattleWorld world = CreateWorld();
            Run(world, 1f);

            world.Enqueue(BattleCommand.Spawn(Side.Left, 0));
            world.Step(StepTime);

            Assert.AreEqual(0, world.CountUnits(Side.Left));
        }

        [Test]
        public void Spawn_SpendsFish()
        {
            BattleWorld world = CreateWorld();
            Run(world, 6f);
            int before = world.GetWallet(Side.Left).Fish;

            world.Enqueue(BattleCommand.Spawn(Side.Left, 0));
            world.Step(StepTime);

            Assert.AreEqual(1, world.CountUnits(Side.Left));
            Assert.That(world.GetWallet(Side.Left).Fish, Is.InRange(before - Cost, before - Cost + 1));
        }

        [Test]
        public void DuringCooldown_CannotSpawnUntilFinished()
        {
            BattleWorld world = CreateWorld();
            Run(world, 30f);
            world.Enqueue(BattleCommand.Spawn(Side.Left, 0));
            world.Step(StepTime);

            world.Enqueue(BattleCommand.Spawn(Side.Left, 0));
            world.Step(StepTime);
            Assert.AreEqual(1, world.CountUnits(Side.Left));
            Assert.IsFalse(world.GetSlot(Side.Left, 0).IsReady);

            Run(world, Cooldown);
            world.Enqueue(BattleCommand.Spawn(Side.Left, 0));
            world.Step(StepTime);
            Assert.AreEqual(2, world.CountUnits(Side.Left));
        }

        [Test]
        public void LevelUp_ChangesCapAndRateAndStopsAtMax()
        {
            BattleWorld world = CreateWorld();
            WalletState wallet = world.GetWallet(Side.Left);
            Run(world, 30f);

            world.Enqueue(BattleCommand.LevelUpWallet(Side.Left));
            world.Step(StepTime);

            Assert.AreEqual(2, wallet.Level);
            Assert.AreEqual(400, wallet.Cap);
            Assert.AreEqual(40f, wallet.RatePerSecond);
            Assert.IsTrue(wallet.IsMaxLevel);

            Run(world, 30f);
            world.Enqueue(BattleCommand.LevelUpWallet(Side.Left));
            world.Step(StepTime);
            Assert.AreEqual(2, wallet.Level);
            Assert.AreEqual(400, wallet.Fish);
        }

        [Test]
        public void LevelUp_NotEnoughFish_DoesNothing()
        {
            BattleWorld world = CreateWorld();
            Run(world, 1f);

            world.Enqueue(BattleCommand.LevelUpWallet(Side.Left));
            world.Step(StepTime);

            Assert.AreEqual(1, world.GetWallet(Side.Left).Level);
        }

        [Test]
        public void RightSpawnsFree_IgnoresFishAndCooldown()
        {
            BattleWorld world = CreateWorld(rightFree: true);

            world.Enqueue(BattleCommand.Spawn(Side.Right, 0));
            world.Enqueue(BattleCommand.Spawn(Side.Right, 0));
            world.Step(StepTime);

            Assert.AreEqual(2, world.CountUnits(Side.Right));
        }
    }
}
