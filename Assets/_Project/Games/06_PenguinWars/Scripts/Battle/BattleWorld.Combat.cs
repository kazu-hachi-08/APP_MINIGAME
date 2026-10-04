using System;

namespace MiniGame.PenguinWars.Battle
{
    /// <summary>ユニットの行動（前進・攻撃）・ペンギン砲・ダメージと撃破報酬。ノックバックと状態異常は BattleWorld.Status.cs</summary>
    public partial class BattleWorld
    {
        private void TickUnit(UnitState unit, float deltaTime)
        {
            unit.Status.Tick(deltaTime);
            if (unit.Action == UnitAction.Knockback)
            {
                TickKnockback(unit, deltaTime);
                return;
            }
            // 止められている間は移動も攻撃タイマーも進めない（仕様書 §5.3）
            if (unit.Status.IsFrozen) return;

            switch (unit.Action)
            {
                case UnitAction.Walk:
                    TickWalk(unit, deltaTime);
                    break;
                case UnitAction.Windup:
                    TickWindup(unit, deltaTime);
                    break;
                case UnitAction.Cooldown:
                    unit.ActionTimer -= deltaTime;
                    if (unit.ActionTimer <= 0f) unit.Action = UnitAction.Walk;
                    break;
            }
        }

        /// <summary>ユニット同士は重なってよい（にゃんこと同じ）。止まるのは射程内に敵がいるときだけ</summary>
        private void TickWalk(UnitState unit, float deltaTime)
        {
            CastleState enemyCastle = GetCastle(unit.Side.Opponent());
            if (UnitCombat.HasTarget(unit, _units, enemyCastle))
            {
                unit.Action = UnitAction.Windup;
                unit.ActionTimer = unit.Stats.Windup;
                return;
            }

            float speed = unit.Stats.MoveSpeed * (unit.Status.IsSlowed ? _settings.SlowSpeedMultiplier : 1f);
            float x = unit.X + unit.Side.Forward() * speed * deltaTime;
            // 無敵の出現ゲートは攻撃対象にならないので、射程で止まらずゲートで止める
            unit.X = unit.Side == Side.Left ? Math.Min(x, enemyCastle.X) : Math.Max(x, enemyCastle.X);
        }

        private void TickWindup(UnitState unit, float deltaTime)
        {
            unit.ActionTimer -= deltaTime;
            if (unit.ActionTimer > 0f) return;

            PerformAttack(unit);
            unit.Action = UnitAction.Cooldown;
            unit.ActionTimer = Math.Max(0f, unit.Stats.AttackInterval - unit.Stats.Windup);
        }

        /// <summary>発生の瞬間に射程内にいる相手だけに当てる。発生前に相手が消えたら空振り（本家と同じ）</summary>
        private void PerformAttack(UnitState attacker)
        {
            CastleState enemyCastle = GetCastle(attacker.Side.Opponent());
            bool hitCastle = UnitCombat.CollectTargets(attacker, _units, enemyCastle, _targets);

            foreach (UnitState target in _targets)
            {
                DamageUnit(target, attacker.Stats.Attack);
                if (!target.IsDead) ApplyHitAbilities(attacker.Stats, target);
            }
            if (hitCastle) DamageCastle(enemyCastle, AbilityResolver.CastleDamage(attacker.Stats, _settings.CastleKillerMultiplier));
        }

        /// <summary>自城から戦場の CannonRangeRatio までにいる敵ユニット全員に当て、ノックバック1回分を起こす。城には当てない（仕様書 §4.4）</summary>
        private void FireCannon(Side side)
        {
            float reach = _settings.FieldLength * _settings.CannonRangeRatio;
            float originX = GetCastle(side).X;
            foreach (UnitState unit in _units)
            {
                if (unit.Side == side || unit.IsDead) continue;
                if ((unit.X - originX) * side.Forward() > reach) continue;

                DamageUnit(unit, _settings.CannonDamage);
                StartKnockback(unit);
            }
            _events.Add(new BattleEvent(BattleEventType.CannonFired, side, BattleEvent.CastleId, originX + side.Forward() * reach));
        }

        private void DamageUnit(UnitState target, int amount)
        {
            int hpBefore = target.Hp;
            target.Hp = Math.Max(0, target.Hp - amount);
            _events.Add(new BattleEvent(BattleEventType.Hit, target.Side, target.Id, target.X, amount));
            if (target.Hp > 0)
            {
                if (KnockbackRule.CrossesThreshold(target.Stats, hpBefore, target.Hp)) StartKnockback(target);
                return;
            }

            target.Action = UnitAction.Dead;
            int reward = RewardKill(target);
            _events.Add(new BattleEvent(BattleEventType.Died, target.Side, target.Id, target.X, reward));
        }

        /// <summary>倒した側に撃破数とさかなを入れる。報酬は元のコストから計算する（敵レベルの倍率はかけない。§8.2）</summary>
        private int RewardKill(UnitState target)
        {
            Side killer = target.Side.Opponent();
            _killCounts[(int)killer]++;
            int reward = (int)(target.Stats.Cost * _settings.KillRewardRate);
            GetWallet(killer).Add(reward);
            return reward;
        }

        private void DamageCastle(CastleState castle, int amount)
        {
            castle.Hp = Math.Max(0, castle.Hp - amount);
            _events.Add(new BattleEvent(BattleEventType.Hit, castle.Side, BattleEvent.CastleId, castle.X, amount));
            if (castle.Hp > 0 || IsFinished) return;

            IsFinished = true;
            Loser = castle.Side;
            _events.Add(new BattleEvent(BattleEventType.CastleDestroyed, castle.Side, BattleEvent.CastleId, castle.X));
        }
    }
}
