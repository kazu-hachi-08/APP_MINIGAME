using System.Collections.Generic;
using NUnit.Framework;
using static MiniGame.PenguinWars.Battle.Tests.BattleTestUtil;

namespace MiniGame.PenguinWars.Battle.Tests
{
    /// <summary>ステージのギミック（開始さかな・働きペンギン上限・編成制限・敵の砲・なだれ・ボス）。どれも StageDefinition.ApplyTo を通して確かめる</summary>
    public class GimmickTests
    {
        private const float FieldLength = 30f;
        private const int UnitHp = 1000;
        private const int CannonDamage = 50;
        // 敵の砲の範囲は右城から 15（X 15〜30）、危ない距離は 3（X 27〜30）
        private const float InRangeX = 20f;
        private const float OutOfRangeX = 5f;
        private const float DangerX = 28f;

        /// <summary>動かず攻撃もしない置物</summary>
        private static UnitStats Dummy(int no = 1, int cost = 100, UnitRole role = UnitRole.Wall)
        {
            return new UnitStats
            {
                UnitNo = no, Cost = cost, Role = role, MaxHp = UnitHp, Attack = 0, Range = 0.1f,
                AttackInterval = 1f, Windup = 0.1f, MoveSpeed = 0f, KnockbackCount = 1,
            };
        }

        private static BattleWorld CreateWorld(StageDefinition stage)
        {
            var settings = new BattleSettings { RightSpawnsFree = true, CannonChargeTime = 1000f };
            stage.ApplyTo(settings);
            return new BattleWorld(settings);
        }

        private static StageDefinition CannonStage()
        {
            return new StageDefinition
            {
                FieldLength = FieldLength,
                EnemyCannon = new EnemyCannonSettings { ChargeTime = 1f, RangeRatio = 0.5f, Damage = CannonDamage, MinTargets = 3, DangerRatio = 0.1f },
            };
        }

        private static void PlaceLeft(BattleWorld world, params float[] xs)
        {
            foreach (float x in xs) world.SpawnAt(Side.Left, Dummy(), x);
        }

        // ---- 開始さかな・働きペンギン上限 ----

        [Test]
        public void 開始さかなは上限を超えても減らずに持てる()
        {
            BattleWorld world = CreateWorld(new StageDefinition { StartingFish = 1500 });
            RunAndDrain(world, 1f);

            WalletState wallet = world.GetWallet(Side.Left);
            Assert.Greater(1500, wallet.Cap, "上限（Lv1）より多い開始さかなで確かめる");
            Assert.AreEqual(1500, wallet.Fish);
        }

        [Test]
        public void 開始さかな0なら通常どおり0から()
        {
            BattleWorld world = CreateWorld(new StageDefinition());
            Assert.AreEqual(0, world.GetWallet(Side.Left).Fish);
        }

        [Test]
        public void 働きペンギンは上限レベルより上げられない()
        {
            BattleWorld world = CreateWorld(new StageDefinition { StartingFish = 5000, MaxWalletLevel = 2 });
            WalletState wallet = world.GetWallet(Side.Left);

            Assert.IsTrue(wallet.TryLevelUp());
            Assert.AreEqual(2, wallet.Level);
            Assert.IsTrue(wallet.IsMaxLevel);
            Assert.AreEqual(0, wallet.LevelUpCost);
            Assert.IsFalse(wallet.TryLevelUp());
            Assert.AreEqual(2, wallet.Level);
        }

        // ---- 編成制限 ----

        [Test]
        public void コスト上限を超えるキャラは出撃できない()
        {
            BattleWorld world = CreateWorld(new StageDefinition { StartingFish = 5000, MaxUnitCost = 300 });
            world.SetDeck(Side.Left, new[] { Dummy(1, 300), Dummy(2, 450) });
            RunAndDrain(world, StepTime);

            Assert.IsTrue(world.CanSpawn(Side.Left, 0));
            Assert.IsFalse(world.CanSpawn(Side.Left, 1));
            Assert.IsFalse(world.IsAllowedByRules(Side.Left, 1));

            world.Enqueue(BattleCommand.Spawn(Side.Left, 1));
            RunAndDrain(world, StepTime);
            Assert.AreEqual(0, world.CountUnits(Side.Left));
        }

        [Test]
        public void 禁止の役割のキャラは出撃できない()
        {
            BattleWorld world = CreateWorld(new StageDefinition { StartingFish = 5000, BannedRoles = new[] { UnitRole.Large } });
            world.SetDeck(Side.Left, new[] { Dummy(1, 100, UnitRole.Wall), Dummy(43, 100, UnitRole.Large) });
            RunAndDrain(world, StepTime);

            Assert.IsTrue(world.CanSpawn(Side.Left, 0));
            Assert.IsFalse(world.CanSpawn(Side.Left, 1));
        }

        [Test]
        public void 編成制限は敵には掛からない()
        {
            BattleWorld world = CreateWorld(new StageDefinition { MaxUnitCost = 100 });
            world.SetDeck(Side.Right, new[] { Dummy(2, 450) });

            Assert.IsTrue(world.CanSpawn(Side.Right, 0));
        }

        [Test]
        public void DeckRules_定義表のキャラNoで制限を判定する()
        {
            var stage = new StageDefinition { BannedRoles = new[] { UnitRole.Large }, MaxUnitCost = 300 };

            Assert.IsTrue(DeckRules.IsAllowed(stage, 1), "ペンギン（壁 75）");
            Assert.IsFalse(DeckRules.IsAllowed(stage, 43), "きょだいペンギン（大型）");
            Assert.IsFalse(DeckRules.IsAllowed(stage, 23), "ゆみペンギン（450）");
            Assert.IsTrue(DeckRules.IsAllowed(new StageDefinition(), 43), "制限なし");
        }

        // ---- 敵のペンギン砲 ----

        [Test]
        public void 敵の砲_チャージ前は撃たない()
        {
            BattleWorld world = CreateWorld(CannonStage());
            PlaceLeft(world, InRangeX, InRangeX, InRangeX, DangerX);

            List<BattleEvent> events = RunAndDrain(world, 0.5f);

            Assert.AreEqual(0, Count(events, BattleEventType.CannonFired));
        }

        [Test]
        public void 敵の砲_範囲内に味方がそろうと撃ち味方にダメージ()
        {
            BattleWorld world = CreateWorld(CannonStage());
            PlaceLeft(world, InRangeX, InRangeX, InRangeX, OutOfRangeX);

            List<BattleEvent> events = RunAndDrain(world, 1.1f);

            Assert.AreEqual(1, Count(events, BattleEventType.CannonFired));
            foreach (UnitState unit in world.Units)
            {
                int expected = unit.X > FieldLength * 0.5f ? UnitHp - CannonDamage : UnitHp;
                Assert.AreEqual(expected, unit.Hp, $"X={unit.X}");
            }
        }

        [Test]
        public void 敵の砲_範囲内が少なければ撃たない()
        {
            BattleWorld world = CreateWorld(CannonStage());
            PlaceLeft(world, InRangeX, InRangeX, OutOfRangeX);

            List<BattleEvent> events = RunAndDrain(world, 2f);

            Assert.AreEqual(0, Count(events, BattleEventType.CannonFired));
        }

        [Test]
        public void 敵の砲_城に近い味方がいれば1体でも撃つ()
        {
            BattleWorld world = CreateWorld(CannonStage());
            PlaceLeft(world, DangerX);

            List<BattleEvent> events = RunAndDrain(world, 1.1f);

            Assert.AreEqual(1, Count(events, BattleEventType.CannonFired));
        }

        [Test]
        public void 敵の砲_設定が無いステージでは撃たない()
        {
            BattleWorld world = CreateWorld(new StageDefinition { FieldLength = FieldLength });
            PlaceLeft(world, DangerX, InRangeX, InRangeX, InRangeX);

            List<BattleEvent> events = RunAndDrain(world, 3f);

            Assert.AreEqual(0, Count(events, BattleEventType.CannonFired));
        }

        // ---- なだれ・ボス・紹介 ----

        [Test]
        public void なだれ_ステージ定義から設定すると起きる()
        {
            BattleWorld world = CreateWorld(new StageDefinition { FieldLength = FieldLength, AvalancheInterval = 2f });
            world.SpawnAt(Side.Left, Dummy(), FieldLength * 0.5f);

            List<BattleEvent> events = RunAndDrain(world, 2.1f);

            Assert.AreEqual(1, Count(events, BattleEventType.Avalanche));
            Assert.Less(world.Units[0].Hp, UnitHp);
        }

        [Test]
        public void ボスの行で出た敵には印が付く()
        {
            var stage = new StageDefinition
            {
                Entries = new[]
                {
                    new EnemySpawnEntry { UnitNo = 1, Count = 1 },
                    new EnemySpawnEntry { UnitNo = 43, Count = 1, IsBoss = true },
                },
            };
            BattleWorld world = CreateWorld(stage);
            var statsByNo = new Dictionary<int, UnitStats> { { 1, Dummy(1) }, { 43, Dummy(43, 2500, UnitRole.Large) } };
            world.SetEnemyScript(new EnemyScriptDirector(stage.Entries, statsByNo));

            RunAndDrain(world, StepTime);

            Assert.AreEqual(2, world.Units.Count);
            foreach (UnitState unit in world.Units) Assert.AreEqual(unit.UnitNo == 43, unit.IsBoss);
        }

        [Test]
        public void 出てくる敵は重複なしで行の順に並びボスが分かる()
        {
            var stage = new StageDefinition
            {
                Entries = new[]
                {
                    new EnemySpawnEntry { UnitNo = 3 },
                    new EnemySpawnEntry { UnitNo = 1 },
                    new EnemySpawnEntry { UnitNo = 3, TriggerCastleHpRatio = 0.5f },
                    new EnemySpawnEntry { UnitNo = 43, IsBoss = true },
                },
            };

            CollectionAssert.AreEqual(new[] { 3, 1, 43 }, stage.DistinctEnemyNos());
            Assert.IsTrue(stage.IsBossUnit(43));
            Assert.IsFalse(stage.IsBossUnit(3));
        }
    }
}
