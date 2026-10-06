using System.Collections.Generic;
using NUnit.Framework;

namespace MiniGame.PenguinWars.Battle.Tests
{
    /// <summary>ステージ「なだれの谷」のなだれ（仕様書 §3.4）</summary>
    public class AvalancheTests
    {
        private const float StepTime = 1f / 30f;
        private const float FieldLength = 30f;
        private const float Interval = 2f;
        private const float WarningTime = 1f;
        private const int Damage = 50;
        private const int UnitHp = 1000;
        // 範囲は 0.4〜0.6 = X 12〜18
        private const float InsideX = 15f;
        private const float OutsideX = 25f;

        /// <summary>動かず攻撃もしない置物。射程を短くして、互いに戦い出さないようにする</summary>
        private static UnitStats Dummy(int hp = UnitHp)
        {
            return new UnitStats
            {
                UnitNo = 1, MaxHp = hp, Attack = 0, Range = 0.1f,
                AttackInterval = 1f, Windup = 0.1f, MoveSpeed = 0f, KnockbackCount = 1,
            };
        }

        private static BattleWorld CreateWorld()
        {
            return new BattleWorld(new BattleSettings
            {
                FieldLength = FieldLength,
                AvalancheInterval = Interval,
                AvalancheWarningTime = WarningTime,
                AvalancheStartRatio = 0.4f,
                AvalancheEndRatio = 0.6f,
                AvalancheDamage = Damage,
                CannonChargeTime = 1000f,
            });
        }

        private static List<BattleEvent> Run(BattleWorld world, float seconds)
        {
            int steps = (int)System.Math.Round(seconds / StepTime);
            for (int i = 0; i < steps; i++) world.Step(StepTime);
            var events = new List<BattleEvent>();
            world.DrainEvents(events);
            return events;
        }

        private static int Count(List<BattleEvent> events, BattleEventType type)
        {
            return events.FindAll(e => e.Type == type).Count;
        }

        private static UnitState FindAt(BattleWorld world, Side side)
        {
            foreach (UnitState unit in world.Units)
            {
                if (unit.Side == side) return unit;
            }
            return null;
        }

        [Test]
        public void 予告が先に出て間隔ごとになだれが起きる()
        {
            BattleWorld world = CreateWorld();

            List<BattleEvent> beforeWarning = Run(world, Interval - WarningTime - 0.1f);
            Assert.AreEqual(0, Count(beforeWarning, BattleEventType.AvalancheWarning));

            List<BattleEvent> warned = Run(world, 0.5f);
            Assert.AreEqual(1, Count(warned, BattleEventType.AvalancheWarning));
            Assert.AreEqual(0, Count(warned, BattleEventType.Avalanche));

            List<BattleEvent> fired = Run(world, 1f);
            Assert.AreEqual(1, Count(fired, BattleEventType.Avalanche));
        }

        [Test]
        public void 範囲内のユニットだけ敵味方関係なく当たる()
        {
            BattleWorld world = CreateWorld();
            world.SpawnAt(Side.Left, Dummy(), InsideX);
            world.SpawnAt(Side.Right, Dummy(), OutsideX);

            Run(world, Interval + 0.1f);

            Assert.AreEqual(UnitHp - Damage, FindAt(world, Side.Left).Hp);
            Assert.AreEqual(UnitHp, FindAt(world, Side.Right).Hp);
        }

        [Test]
        public void なだれで倒れても相手に撃破報酬が入らない()
        {
            BattleWorld world = CreateWorld();
            world.SpawnAt(Side.Left, Dummy(Damage), InsideX);

            Run(world, Interval + 0.1f);

            Assert.IsNull(FindAt(world, Side.Left));
            Assert.AreEqual(0, world.GetKillCount(Side.Right));
        }

        [Test]
        public void 間隔0ならなだれは起きない()
        {
            var world = new BattleWorld(new BattleSettings { FieldLength = FieldLength });

            List<BattleEvent> events = Run(world, 10f);

            Assert.AreEqual(0, Count(events, BattleEventType.AvalancheWarning));
            Assert.AreEqual(0, Count(events, BattleEventType.Avalanche));
        }
    }
}
