using System.Collections.Generic;
using MiniGame.PenguinWars.Battle;
using NUnit.Framework;

namespace MiniGame.PenguinWars.Battle.Tests
{
    /// <summary>Phase 5: ノックバック・特殊能力</summary>
    public class AbilityTests
    {
        private const float StepTime = 1f / 30f;
        private const float FieldLength = 30f;
        // 城から離して湧かせ、ノックバックが城の位置で止まって距離が測れなくなるのを防ぐ
        private const float SpawnOffset = 5f;
        // 左の攻撃役がその場から右の相手に届くようにする（動かさずに当て続けるため）
        private const float LongRange = 30f;
        private const float Tolerance = 0.01f;

        /// <summary>左に置く攻撃役。動かず、射程内の相手を一定間隔で殴る</summary>
        private static UnitStats Attacker(int attack, float interval = 1f, bool area = false, params UnitAbility[] abilities)
        {
            return new UnitStats
            {
                UnitNo = 1, MaxHp = 100000, Attack = attack, Range = LongRange,
                AttackInterval = interval, Windup = 0.1f, MoveSpeed = 0f, IsAreaAttack = area,
                Abilities = abilities,
            };
        }

        /// <summary>右に置く殴られ役。攻撃しない</summary>
        private static UnitStats Target(int hp = 100000, int knockbackCount = 1, float speed = 0f, params UnitAbility[] abilities)
        {
            return new UnitStats
            {
                UnitNo = 2, MaxHp = hp, Attack = 0, Range = 0.1f,
                AttackInterval = 1f, Windup = 0.1f, MoveSpeed = speed,
                KnockbackCount = knockbackCount, Abilities = abilities,
            };
        }

        private static BattleWorld CreateWorld(UnitStats left, UnitStats right, bool rightInvincible = true, float cannonChargeTime = 1000f)
        {
            var world = new BattleWorld(new BattleSettings
            {
                FieldLength = FieldLength,
                SpawnOffset = SpawnOffset,
                RightCastleHp = 1000,
                RightCastleInvincible = rightInvincible,
                RightSpawnsFree = true,
                CannonChargeTime = cannonChargeTime,
            });
            world.SetDeck(Side.Left, new[] { left });
            world.SetDeck(Side.Right, new[] { right });
            world.Enqueue(BattleCommand.Spawn(Side.Left, 0));
            world.Enqueue(BattleCommand.Spawn(Side.Right, 0));
            return world;
        }

        private static void Run(BattleWorld world, float seconds)
        {
            int steps = (int)System.Math.Round(seconds / StepTime);
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

        private static List<BattleEvent> DrainEvents(BattleWorld world, BattleEventType type)
        {
            var events = new List<BattleEvent>();
            world.DrainEvents(events);
            return events.FindAll(e => e.Type == type);
        }

        private static int CountEvents(BattleWorld world, BattleEventType type)
        {
            return DrainEvents(world, type).Count;
        }

        [Test]
        public void KnockbackRule_CrossesOnlyAtThresholds()
        {
            UnitStats stats = Target(hp: 100, knockbackCount: 3);

            Assert.IsFalse(KnockbackRule.CrossesThreshold(stats, 100, 67));
            Assert.IsTrue(KnockbackRule.CrossesThreshold(stats, 67, 66));
            Assert.IsTrue(KnockbackRule.CrossesThreshold(stats, 34, 33));
            // HP 0 は撃破なのでノックバックのしきい値に含めない
            Assert.IsFalse(KnockbackRule.CrossesThreshold(stats, 33, 0));
            // 一撃で2つまたいでも1回
            Assert.IsTrue(KnockbackRule.CrossesThreshold(stats, 100, 1));
        }

        [Test]
        public void Knockback_HappensOncePerThreshold()
        {
            // HP 300・3回 → 200 と 100 でノックバック。攻撃 50 なので6発で倒れ、その間のノックバックは2回だけ
            BattleWorld world = CreateWorld(Attacker(50), Target(hp: 300, knockbackCount: 3));

            Run(world, 6.5f);

            Assert.AreEqual(2, CountEvents(world, BattleEventType.Knockback));
            Assert.IsNull(FindFirst(world, Side.Right));
        }

        [Test]
        public void Knockback_MovesBackAndCannotActForDuration()
        {
            BattleWorld world = CreateWorld(Attacker(50, interval: 10f), Target(hp: 100, knockbackCount: 2));
            Run(world, 0.2f);
            List<BattleEvent> knockbacks = DrainEvents(world, BattleEventType.Knockback);
            Assert.AreEqual(1, knockbacks.Count);
            UnitState target = FindFirst(world, Side.Right);

            Run(world, 0.2f);
            Assert.AreEqual(UnitAction.Knockback, target.Action);
            Run(world, 0.4f);

            // 右陣営の「後ろ」は +X。飛ばされ始めた位置から 1.5 戻り、0.5秒後には動けるようになる
            Assert.AreEqual(knockbacks[0].X + 1.5f, target.X, Tolerance);
            Assert.AreEqual(UnitAction.Walk, target.Action);
        }

        [Test]
        public void Steadfast_NeverKnockedBack()
        {
            var steadfast = new UnitAbility(UnitAbilityType.Steadfast);
            var push = new UnitAbility(UnitAbilityType.Knockback, 1f);
            BattleWorld world = CreateWorld(Attacker(50, 1f, false, push), Target(300, 3, 0f, steadfast));

            Run(world, 3.5f);

            Assert.AreEqual(0, CountEvents(world, BattleEventType.Knockback));
        }

        [Test]
        public void Freeze_StopsMovement()
        {
            var freeze = new UnitAbility(UnitAbilityType.Freeze, 1f, 2f);
            BattleWorld world = CreateWorld(Attacker(1, 10f, false, freeze), Target(speed: 1f));
            Run(world, 0.2f);
            UnitState frozen = FindFirst(world, Side.Right);
            Assert.IsTrue(frozen.Status.IsFrozen);
            float frozenX = frozen.X;

            Run(world, 1.5f);

            Assert.AreEqual(frozenX, frozen.X, Tolerance);
        }

        [Test]
        public void Freeze_StopsAttackTimer()
        {
            var freeze = new UnitAbility(UnitAbilityType.Freeze, 1f, 2f);
            var target = new UnitStats
            {
                UnitNo = 2, MaxHp = 100000, Attack = 10, Range = LongRange,
                AttackInterval = 1f, Windup = 0.5f, MoveSpeed = 1f,
            };
            BattleWorld world = CreateWorld(Attacker(1, 10f, false, freeze), target);
            Run(world, 0.2f);
            UnitState frozen = FindFirst(world, Side.Right);
            UnitState attacker = FindFirst(world, Side.Left);
            Assert.IsTrue(frozen.Status.IsFrozen);

            Run(world, 1.5f);

            // 止められる前に攻撃発生待ちに入っていても、止まっている間は発生しない
            Assert.AreEqual(attacker.Stats.MaxHp, attacker.Hp);
        }

        [Test]
        public void Slow_HalvesMoveSpeed()
        {
            var slow = new UnitAbility(UnitAbilityType.Slow, 1f, 3f);
            BattleWorld world = CreateWorld(Attacker(1, 10f, false, slow), Target(speed: 1f));
            Run(world, 0.2f);
            UnitState target = FindFirst(world, Side.Right);
            Assert.IsTrue(target.Status.IsSlowed);
            float startX = target.X;

            Run(world, 1f);

            Assert.AreEqual(0.5f, startX - target.X, Tolerance);
        }

        [Test]
        public void StatusEffects_OverwriteWithLonger()
        {
            var status = new UnitStatusEffects();
            status.Apply(UnitStatusType.Freeze, 2f);
            status.Apply(UnitStatusType.Freeze, 1f);

            Assert.AreEqual(2f, status.FreezeTime, Tolerance);
        }

        [Test]
        public void CastleKiller_TriplesOnlyCastleDamage()
        {
            var castleKiller = new UnitAbility(UnitAbilityType.CastleKiller);
            // 範囲攻撃にして、同じ1発がユニットと城の両方に当たるようにする
            BattleWorld world = CreateWorld(Attacker(10, 10f, true, castleKiller), Target(), rightInvincible: false);

            Run(world, 0.2f);

            UnitState target = FindFirst(world, Side.Right);
            Assert.AreEqual(target.Stats.MaxHp - 10, target.Hp);
            Assert.AreEqual(1000 - 30, world.GetCastle(Side.Right).Hp);
        }

        [Test]
        public void RoleKiller_TriplesDamageOnlyAgainstItsRole()
        {
            var largeKiller = new UnitAbility(UnitAbilityType.LargeKiller);
            UnitStats large = Target();
            large.Role = UnitRole.Large;
            UnitStats ranged = Target();
            ranged.Role = UnitRole.Ranged;

            BattleWorld vsLarge = CreateWorld(Attacker(10, 10f, false, largeKiller), large);
            BattleWorld vsRanged = CreateWorld(Attacker(10, 10f, false, largeKiller), ranged);
            Run(vsLarge, 0.2f);
            Run(vsRanged, 0.2f);

            Assert.AreEqual(large.MaxHp - 30, FindFirst(vsLarge, Side.Right).Hp);
            Assert.AreEqual(ranged.MaxHp - 10, FindFirst(vsRanged, Side.Right).Hp);
        }

        [Test]
        public void RoleKiller_DoesNotBoostCastleDamage()
        {
            var largeKiller = new UnitAbility(UnitAbilityType.LargeKiller);
            BattleWorld world = CreateWorld(Attacker(10, 10f, true, largeKiller), Target(), rightInvincible: false);

            Run(world, 0.2f);

            Assert.AreEqual(1000 - 10, world.GetCastle(Side.Right).Hp);
        }

        [Test]
        public void ChanceZero_NeverTriggers()
        {
            var push = new UnitAbility(UnitAbilityType.Knockback, 0f);
            var freeze = new UnitAbility(UnitAbilityType.Freeze, 0f, 2f);
            var slow = new UnitAbility(UnitAbilityType.Slow, 0f, 2f);
            BattleWorld world = CreateWorld(Attacker(1, 0.5f, false, push, freeze, slow), Target());

            Run(world, 5f);

            Assert.AreEqual(0, CountEvents(world, BattleEventType.Knockback));
            Assert.IsFalse(FindFirst(world, Side.Right).Status.IsFrozen);
            Assert.IsFalse(FindFirst(world, Side.Right).Status.IsSlowed);
        }

        [Test]
        public void ChanceOne_AlwaysTriggers()
        {
            var push = new UnitAbility(UnitAbilityType.Knockback, 1f);
            // 攻撃間隔をノックバック時間より長くして、毎回飛ばされ終わってから殴る
            BattleWorld world = CreateWorld(Attacker(1, 1f, false, push), Target());

            Run(world, 3.5f);

            // 0.1・1.1・2.1・3.1 秒の4発すべてで飛ぶ
            Assert.AreEqual(4, CountEvents(world, BattleEventType.Knockback));
        }

        [Test]
        public void Roll_RespectsBoundaries()
        {
            var random = new System.Random(0);
            for (int i = 0; i < 1000; i++)
            {
                Assert.IsFalse(AbilityResolver.Roll(random, 0f));
                Assert.IsTrue(AbilityResolver.Roll(random, 1f));
            }
        }

        [Test]
        public void Cannon_KnocksBackEnemies()
        {
            // 速く歩かせて、砲の範囲（自城から戦場の60% = 18）まで入れる
            BattleWorld world = CreateWorld(Attacker(0, 10f), Target(speed: 10f), cannonChargeTime: 0.1f);
            Run(world, 1f);
            UnitState target = FindFirst(world, Side.Right);
            Assert.Less(target.X, FieldLength * 0.6f);

            world.Enqueue(BattleCommand.FireCannon(Side.Left));
            world.Step(StepTime);

            Assert.AreEqual(UnitAction.Knockback, target.Action);
        }
    }
}
