using System.Collections.Generic;
using MiniGame.PenguinWars.Battle;
using NUnit.Framework;
using static MiniGame.PenguinWars.Battle.Tests.BattleTestUtil;

namespace MiniGame.PenguinWars.Battle.Tests
{
    /// <summary>ステージの敵の出方（EnemyScriptDirector）と、敵の城を落として勝つ流れ</summary>
    public class StageScriptTests
    {
        private const float FieldLength = 10f;
        private const int CastleHp = 1000;
        private const int EnemyNo = 2;
        private const int BossNo = 43;
        private const float Full = 1f;

        private static UnitStats Unit(int no, int hp = 100, int attack = 10, float speed = 0f, float range = 0.1f)
        {
            return new UnitStats
            {
                UnitNo = no, Cost = 100, MaxHp = hp, Attack = attack, Range = range,
                AttackInterval = 1f, Windup = 0.1f, MoveSpeed = speed,
            };
        }

        private static Dictionary<int, UnitStats> StatsByNo(params UnitStats[] units)
        {
            var statsByNo = new Dictionary<int, UnitStats>();
            foreach (UnitStats stats in units) statsByNo[stats.UnitNo] = stats;
            return statsByNo;
        }

        private static EnemyScriptDirector Director(params EnemySpawnEntry[] entries)
        {
            return new EnemyScriptDirector(entries, StatsByNo(Unit(EnemyNo), Unit(BossNo, hp: 1000)));
        }

        /// <summary>城HP割合を固定して seconds 秒進め、出たキャラをすべて返す</summary>
        private static List<EnemySpawn> TickDirector(EnemyScriptDirector director, float seconds, float castleHpRatio = Full)
        {
            var all = new List<EnemySpawn>();
            var spawns = new List<EnemySpawn>();
            int steps = (int)System.Math.Round(seconds / StepTime);
            for (int i = 0; i < steps; i++)
            {
                director.Tick(StepTime, castleHpRatio, spawns);
                all.AddRange(spawns);
            }
            return all;
        }

        private static BattleWorld CreateWorld(UnitStats left, int maxUnits = 30, params EnemySpawnEntry[] entries)
        {
            var world = new BattleWorld(new BattleSettings
            {
                FieldLength = FieldLength,
                LeftCastleHp = CastleHp,
                RightCastleHp = CastleHp,
                RightSpawnsFree = true,
                MaxUnitsPerSide = maxUnits,
            });
            world.SetDeck(Side.Left, new[] { left });
            world.SetEnemyScript(new EnemyScriptDirector(entries, StatsByNo(Unit(EnemyNo), Unit(BossNo, hp: 1000))));
            return world;
        }

        [Test]
        public void Entry_DoesNotSpawnBeforeStartTime()
        {
            EnemyScriptDirector director = Director(new EnemySpawnEntry { UnitNo = EnemyNo, StartTime = 3f, Interval = 1f });

            Assert.AreEqual(0, TickDirector(director, 2.9f).Count);
            Assert.AreEqual(1, TickDirector(director, 0.2f).Count);
        }

        [Test]
        public void Entry_SpawnsCountTimesAtIntervalThenStops()
        {
            EnemyScriptDirector director = Director(new EnemySpawnEntry { UnitNo = EnemyNo, StartTime = 1f, Interval = 2f, Count = 3 });

            // 1秒・3秒・5秒に出る
            Assert.AreEqual(2, TickDirector(director, 4f).Count);
            Assert.AreEqual(1, TickDirector(director, 2f).Count);
            Assert.AreEqual(0, TickDirector(director, 30f).Count);
        }

        [Test]
        public void Entry_CountZero_KeepsSpawning()
        {
            EnemyScriptDirector director = Director(new EnemySpawnEntry { UnitNo = EnemyNo, Interval = 1f });

            Assert.AreEqual(60, TickDirector(director, 59.9f).Count);
        }

        [Test]
        public void TriggeredEntry_WaitsForCastleHpThenStartTime()
        {
            EnemyScriptDirector director = Director(new EnemySpawnEntry
            {
                UnitNo = EnemyNo, StartTime = 2f, Count = 1, TriggerCastleHpRatio = 0.5f,
            });

            Assert.AreEqual(0, TickDirector(director, 10f, 0.51f).Count);
            // 下回ってからの StartTime（2秒）を数える
            Assert.AreEqual(0, TickDirector(director, 1.9f, 0.49f).Count);
            Assert.AreEqual(1, TickDirector(director, 0.2f, 0.49f).Count);
        }

        [Test]
        public void TriggeredEntry_DoesNotStopIfRatioGoesBackUp()
        {
            EnemyScriptDirector director = Director(new EnemySpawnEntry
            {
                UnitNo = EnemyNo, StartTime = 1f, Interval = 1f, TriggerCastleHpRatio = 0.5f,
            });

            TickDirector(director, StepTime, 0.4f);

            Assert.That(TickDirector(director, 3f, Full).Count, Is.GreaterThan(0));
        }

        [Test]
        public void Entry_SpawnsScaledStatsKeepingCost()
        {
            EnemyScriptDirector director = Director(new EnemySpawnEntry { UnitNo = EnemyNo, Count = 1, StatMultiplier = 1.5f });

            UnitStats spawned = TickDirector(director, StepTime)[0].Stats;

            Assert.AreEqual(150, spawned.MaxHp);
            Assert.AreEqual(15, spawned.Attack);
            Assert.AreEqual(100, spawned.Cost);
        }

        [Test]
        public void Entry_UnknownUnitNo_IsSkipped()
        {
            EnemyScriptDirector director = Director(new EnemySpawnEntry { UnitNo = 999, Interval = 1f });

            Assert.AreEqual(0, TickDirector(director, 5f).Count);
        }

        [Test]
        public void World_BossAppearedEventOnlyOnce()
        {
            BattleWorld world = CreateWorld(Unit(1), 30, new EnemySpawnEntry { UnitNo = BossNo, Interval = 1f, Count = 3, IsBoss = true });

            Run(world, 5f);

            var events = new List<BattleEvent>();
            world.DrainEvents(events);
            Assert.AreEqual(3, world.CountUnits(Side.Right));
            Assert.AreEqual(1, Count(events, BattleEventType.BossAppeared));
            Assert.IsTrue(events.Exists(e => e.Type == BattleEventType.BossAppeared && e.Amount == BossNo));
        }

        [Test]
        public void World_BossAppearsWhenEnemyCastleIsHalf()
        {
            // 城の目の前に置いた攻撃役で、敵城を少しずつ削る（100 ずつ。1000 → 500 で半分）
            BattleWorld world = CreateWorld(Unit(1, attack: 100, range: FieldLength), 30,
                new EnemySpawnEntry { UnitNo = BossNo, Count = 1, TriggerCastleHpRatio = 0.5f, IsBoss = true });
            world.SpawnAt(Side.Left, Unit(1, attack: 100, range: FieldLength), 0f);

            Run(world, 3.5f);
            Assert.AreEqual(0, world.CountUnits(Side.Right));

            Run(world, 2.5f);
            Assert.AreEqual(1, world.CountUnits(Side.Right));
        }

        [Test]
        public void World_EnemyCastleTakesDamageAndLosesAtZero()
        {
            BattleWorld world = CreateWorld(Unit(1, attack: 200, range: FieldLength));
            world.SpawnAt(Side.Left, Unit(1, attack: 200, range: FieldLength), 0f);

            Run(world, 1f);
            Assert.Less(world.GetCastle(Side.Right).Hp, CastleHp);

            Run(world, 10f);
            Assert.IsTrue(world.IsFinished);
            Assert.AreEqual(Side.Right, world.Loser);
        }

        [Test]
        public void World_SpawnsOverLimitAreDropped_ButBossIsNot()
        {
            BattleWorld world = CreateWorld(Unit(1), 2,
                new EnemySpawnEntry { UnitNo = EnemyNo, Interval = 0.5f },
                new EnemySpawnEntry { UnitNo = BossNo, StartTime = 3f, Count = 1, IsBoss = true });

            Run(world, 2.9f);
            Assert.AreEqual(2, world.CountUnits(Side.Right));

            Run(world, 0.2f);
            Assert.AreEqual(3, world.CountUnits(Side.Right));
        }

        [Test]
        public void World_ElapsedTimeAdvancesWithSteps()
        {
            BattleWorld world = CreateWorld(Unit(1));

            Run(world, 2f);

            Assert.AreEqual(2f, world.ElapsedTime, 0.01f);
        }
    }
}
