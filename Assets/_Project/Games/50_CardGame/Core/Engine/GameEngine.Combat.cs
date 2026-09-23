using System.Collections.Generic;
using System.Linq;
using CardGame.Core.Commands;
using CardGame.Core.Definitions;
using CardGame.Core.Events;
using CardGame.Core.State;

namespace CardGame.Core.Engine
{
    /// <summary>攻撃と戦闘ダメージ(Docs/50_CardGame/spec/01-rules.md「戦闘」)。</summary>
    public sealed partial class GameEngine
    {
        /// <summary>このフォロワーが今攻撃を宣言できるか(対象の有無は含まない)。可能なら null。</summary>
        public string? CanAttack(BoardEntity attacker)
        {
            var err = ValidateMainPhaseTurn(attacker.Owner);
            if (err != null) return err;
            if (State.FindEntity(attacker.InstanceId) != attacker) return "場にいない";
            if (!attacker.IsFollower) return "フォロワーではない";
            if (attacker.Attack <= 0) return "攻撃力が 0";
            if (attacker.HasAttackedThisTurn) return "このターン既に攻撃した";
            if (attacker.SummonedThisTurn && !attacker.HasKeyword(Keyword.Storm) && !attacker.HasKeyword(Keyword.Rush))
                return "このターンに場に出た(召喚酔い)";
            return null;
        }

        /// <summary>攻撃可能な対象一覧(守護・突進を考慮)。</summary>
        public IReadOnlyList<TargetRef> ValidAttackTargets(BoardEntity attacker)
        {
            var result = new List<TargetRef>();
            if (CanAttack(attacker) != null) return result;

            var enemy = State.OpponentOf(attacker.Owner);
            var wards = enemy.Followers.Where(f => f.HasKeyword(Keyword.Ward)).ToList();
            bool canHitLeader = !(attacker.SummonedThisTurn && !attacker.HasKeyword(Keyword.Storm));

            if (wards.Count > 0)
            {
                foreach (var w in wards) result.Add(TargetRef.Entity(w));
                return result;
            }
            if (canHitLeader) result.Add(TargetRef.Leader(enemy.Index));
            foreach (var f in enemy.Followers) result.Add(TargetRef.Entity(f));
            return result;
        }

        private string? ValidateAttack(AttackCommand c)
        {
            var err = ValidateMainPhaseTurn(c.Player);
            if (err != null) return err;
            var attacker = State.PlayerOf(c.Player).FindEntity(c.AttackerInstanceId);
            if (attacker == null) return "攻撃フォロワーが自分の場にいない";
            err = CanAttack(attacker);
            if (err != null) return err;
            if (!ValidAttackTargets(attacker).Contains(c.Target)) return $"不正な攻撃対象 {c.Target}";
            return null;
        }

        private void ExecuteAttack(AttackCommand c)
        {
            var attacker = State.PlayerOf(c.Player).FindEntity(c.AttackerInstanceId)!;
            attacker.HasAttackedThisTurn = true;
            Emit(new AttackDeclaredEvent(c.Player, attacker.InstanceId, c.Target));

            if (c.Target.IsLeader)
            {
                // リーダーへの攻撃は反撃なし
                DealCombatDamage(attacker, c.Target, attacker.Attack);
            }
            else
            {
                var defender = State.FindEntity(c.Target.InstanceId)!;
                // 同時ダメージ: 双方の攻撃力を先に確定させてから適用する
                int toDefender = attacker.Attack;
                int toAttacker = defender.Attack;
                DealCombatDamage(attacker, TargetRef.Entity(defender), toDefender);
                DealCombatDamage(defender, TargetRef.Entity(attacker), toAttacker);
            }

            ProcessDeaths();
            ResolveEffectQueue();
            CheckLeaderDefeat();
        }

        /// <summary>戦闘ダメージ。必殺・ドレインは戦闘ダメージにのみ反応する。</summary>
        private void DealCombatDamage(BoardEntity source, TargetRef target, int amount)
        {
            if (amount <= 0) return;
            int dealt = DealDamage(target, amount, source.InstanceId);
            if (dealt <= 0) return;

            if (source.HasKeyword(Keyword.Drain))
                Heal(TargetRef.Leader(source.Owner), dealt);

            if (source.HasKeyword(Keyword.Bane) && !target.IsLeader)
            {
                var victim = State.FindEntity(target.InstanceId);
                if (victim != null && victim.IsFollower) victim.PendingDestroy = true;
            }
        }
    }
}
