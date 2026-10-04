using System.Collections.Generic;
using MiniGame.PenguinWars.Battle;
using NUnit.Framework;

namespace MiniGame.PenguinWars.Battle.Tests
{
    public class BattleWorldTests
    {
        private const float StepTime = 1f / 30f;
        private const float FieldLength = 10f;
        private const int CastleHp = 1000;

        private static UnitStats Melee(int hp = 100, int attack = 10, bool area = false)
        {
            return new UnitStats
            {
                UnitNo = 1, MaxHp = hp, Attack = attack, Range = 1.4f,
                AttackInterval = 1.2f, Windup = 0.3f, MoveSpeed = 1f, IsAreaAttack = area,
            };
        }

        private static BattleWorld CreateWorld(UnitStats left, UnitStats right, bool rightInvincible = false, int rightCastleHp = CastleHp)
        {
            var world = new BattleWorld(new BattleSettings
            {
                FieldLength = FieldLength,
                LeftCastleHp = CastleHp,
                RightCastleHp = rightCastleHp,
                RightCastleInvincible = rightInvincible,
            });
            world.SetDeck(Side.Left, new[] { left });
            world.SetDeck(Side.Right, new[] { right });
            return world;
        }

        private static void Run(BattleWorld world, float seconds)
        {
            int steps = (int)(seconds / StepTime);
            for (int i = 0; i < steps; i++) world.Step(StepTime);
        }

        private static UnitState FindFirst(BattleWorld world, Side side)
        {
            foreach (UnitState unit in world.Units)
            {
                if (unit.Side == side) return unit;
            }
            return null;
        }

        [Test]
        public void Spawn_UnitsAdvanceTowardEnemy()
        {
            BattleWorld world = CreateWorld(Melee(), Melee());
            world.Enqueue(BattleCommand.Spawn(Side.Left, 0));
            world.Enqueue(BattleCommand.Spawn(Side.Right, 0));
            world.Step(StepTime);
            float leftStart = FindFirst(world, Side.Left).X;
            float rightStart = FindFirst(world, Side.Right).X;

            Run(world, 1f);

            Assert.Greater(FindFirst(world, Side.Left).X, leftStart);
            Assert.Less(FindFirst(world, Side.Right).X, rightStart);
        }

        [Test]
        public void EnemyInRange_StopsAndAttacks()
        {
            BattleWorld world = CreateWorld(Melee(hp: 1000, attack: 1), Melee(hp: 1000, attack: 1));
            world.Enqueue(BattleCommand.Spawn(Side.Left, 0));
            world.Enqueue(BattleCommand.Spawn(Side.Right, 0));
            Run(world, 8f);
            float stoppedX = FindFirst(world, Side.Left).X;

            Run(world, 2f);

            UnitState left = FindFirst(world, Side.Left);
            UnitState right = FindFirst(world, Side.Right);
            Assert.AreEqual(stoppedX, left.X);
            Assert.LessOrEqual(right.X - left.X, left.Stats.Range);
            Assert.Less(right.Hp, right.Stats.MaxHp);
        }

        [Test]
        public void HpZero_UnitIsRemovedAndDiedEventRaised()
        {
            BattleWorld world = CreateWorld(Melee(hp: 1000, attack: 100), Melee(hp: 10, attack: 1));
            world.Enqueue(BattleCommand.Spawn(Side.Left, 0));
            world.Enqueue(BattleCommand.Spawn(Side.Right, 0));

            Run(world, 8f);

            Assert.AreEqual(0, world.CountUnits(Side.Right));
            Assert.IsNull(FindFirst(world, Side.Right));
            var events = new List<BattleEvent>();
            world.DrainEvents(events);
            Assert.IsTrue(events.Exists(e => e.Type == BattleEventType.Died && e.Side == Side.Right));
        }

        [TestCase(true, 3)]
        [TestCase(false, 1)]
        public void Attack_AreaHitsAllInRange_SingleHitsOne(bool area, int expectedDamaged)
        {
            BattleWorld world = CreateWorld(Melee(hp: 1000, attack: 10, area: area), Melee(hp: 1000, attack: 0));
            world.Enqueue(BattleCommand.Spawn(Side.Left, 0));
            // 同じステップで出すと同じ位置に重なる
            for (int i = 0; i < 3; i++) world.Enqueue(BattleCommand.Spawn(Side.Right, 0));

            Run(world, 8f);

            int damaged = 0;
            foreach (UnitState unit in world.Units)
            {
                if (unit.Side == Side.Right && unit.Hp < unit.Stats.MaxHp) damaged++;
            }
            Assert.AreEqual(expectedDamaged, damaged);
        }

        [Test]
        public void NoEnemies_UnitDamagesEnemyCastle()
        {
            BattleWorld world = CreateWorld(Melee(), Melee());
            world.Enqueue(BattleCommand.Spawn(Side.Left, 0));

            Run(world, 12f);

            Assert.Less(world.GetCastle(Side.Right).Hp, CastleHp);
        }

        [Test]
        public void InvincibleGate_IsNotDamagedAndBlocksAdvance()
        {
            BattleWorld world = CreateWorld(Melee(), Melee(), rightInvincible: true);
            world.Enqueue(BattleCommand.Spawn(Side.Left, 0));

            Run(world, 15f);

            Assert.AreEqual(CastleHp, world.GetCastle(Side.Right).Hp);
            Assert.AreEqual(FieldLength, FindFirst(world, Side.Left).X);
        }

        [Test]
        public void CastleHpZero_FinishesBattle()
        {
            BattleWorld world = CreateWorld(Melee(attack: 100), Melee(), rightCastleHp: 50);
            world.Enqueue(BattleCommand.Spawn(Side.Left, 0));

            Run(world, 12f);

            Assert.IsTrue(world.IsFinished);
            Assert.AreEqual(Side.Right, world.Loser);
            var events = new List<BattleEvent>();
            world.DrainEvents(events);
            Assert.IsTrue(events.Exists(e => e.Type == BattleEventType.CastleDestroyed && e.Side == Side.Right));
        }

        [Test]
        public void Spawn_DoesNotExceedMaxUnits()
        {
            BattleWorld world = CreateWorld(Melee(), Melee());
            const int max = 30;
            for (int i = 0; i < max + 1; i++) world.Enqueue(BattleCommand.Spawn(Side.Left, 0));

            world.Step(StepTime);

            Assert.AreEqual(max, world.CountUnits(Side.Left));
        }

        [Test]
        public void Spawn_InvalidSlotIsIgnored()
        {
            BattleWorld world = CreateWorld(Melee(), Melee());
            world.Enqueue(BattleCommand.Spawn(Side.Left, 5));

            world.Step(StepTime);

            Assert.AreEqual(0, world.CountUnits(Side.Left));
        }

        [Test]
        public void SimpleEnemySpawner_SpawnsAfterInterval()
        {
            BattleWorld world = CreateWorld(Melee(), Melee());
            var spawner = new SimpleEnemySpawner(1f, 1, 0);

            for (int i = 0; i < 20; i++)
            {
                spawner.Tick(world, StepTime);
                world.Step(StepTime);
            }
            Assert.AreEqual(0, world.CountUnits(Side.Right));

            for (int i = 0; i < 15; i++)
            {
                spawner.Tick(world, StepTime);
                world.Step(StepTime);
            }
            Assert.AreEqual(1, world.CountUnits(Side.Right));
        }
    }
}
