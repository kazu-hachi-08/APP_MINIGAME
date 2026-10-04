using System;

namespace MiniGame.PenguinWars.Battle
{
    /// <summary>ノックバックと、攻撃が当たったときの特殊能力（ふっとばす・止める・遅くする）</summary>
    public partial class BattleWorld
    {
        /// <summary>撃破されなかった相手にだけ呼ぶ。確率判定は持っている能力の分だけ乱数を引く</summary>
        private void ApplyHitAbilities(UnitStats attacker, UnitState target)
        {
            if (attacker.TryGetAbility(UnitAbilityType.Knockback, out UnitAbility knockback) && AbilityResolver.Roll(_random, knockback.Chance))
            {
                StartKnockback(target);
            }
            TryApplyStatus(attacker, target, UnitAbilityType.Freeze, UnitStatusType.Freeze);
            TryApplyStatus(attacker, target, UnitAbilityType.Slow, UnitStatusType.Slow);
        }

        private void TryApplyStatus(UnitStats attacker, UnitState target, UnitAbilityType abilityType, UnitStatusType statusType)
        {
            if (!attacker.TryGetAbility(abilityType, out UnitAbility ability)) return;
            if (!AbilityResolver.Roll(_random, ability.Chance)) return;

            target.Status.Apply(statusType, ability.Duration);
            _events.Add(new BattleEvent(BattleEventType.StatusApplied, target.Side, target.Id, target.X, (int)statusType));
        }

        /// <summary>
        /// 攻撃中（Windup / Cooldown）でもキャンセルして飛ばす（仕様書 §5.2）。
        /// 飛ばされている最中の追加ノックバックは無視する（連続で当たって画面外まで飛ばされないように）
        /// </summary>
        private void StartKnockback(UnitState unit)
        {
            if (unit.IsDead || unit.Action == UnitAction.Knockback) return;
            if (!KnockbackRule.CanBeKnockedBack(unit.Stats)) return;

            unit.Action = UnitAction.Knockback;
            unit.ActionTimer = _settings.KnockbackDuration;
            _events.Add(new BattleEvent(BattleEventType.Knockback, unit.Side, unit.Id, unit.X));
        }

        /// <summary>
        /// 飛ばされる距離を KnockbackDuration かけて等速で動かす。ロジック側で少しずつ動かすことで、
        /// View もオンラインのゲストも X を描くだけで「後ろに跳ねる」ように見える
        /// </summary>
        private void TickKnockback(UnitState unit, float deltaTime)
        {
            float duration = _settings.KnockbackDuration;
            // 端数のステップで飛びすぎないよう、残り時間を超えて動かさない
            float moveTime = Math.Min(deltaTime, unit.ActionTimer);
            float speed = duration > 0f ? _settings.KnockbackDistance / duration : 0f;
            float x = unit.X - unit.Side.Forward() * speed * moveTime;
            // 自城より後ろには下がらない
            float homeX = GetCastle(unit.Side).X;
            unit.X = unit.Side == Side.Left ? Math.Max(x, homeX) : Math.Min(x, homeX);

            unit.ActionTimer -= deltaTime;
            // 射程外に出ていれば、復帰後は前進から（射程内なら Walk がすぐ攻撃に移る）
            if (unit.ActionTimer <= 0f) unit.Action = UnitAction.Walk;
        }
    }
}
