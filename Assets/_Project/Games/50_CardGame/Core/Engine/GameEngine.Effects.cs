using System;
using System.Collections.Generic;
using System.Linq;
using CardGame.Core.Definitions;
using CardGame.Core.Events;
using CardGame.Core.State;

namespace CardGame.Core.Engine
{
    /// <summary>キューに積まれた未解決の効果。</summary>
    internal sealed class PendingEffect
    {
        public int Player { get; }
        public int SourceInstanceId { get; }
        public CardDefinition SourceDef { get; }
        public EffectDefinition Effect { get; }
        public TargetRef? ChosenTarget { get; }

        public PendingEffect(int player, int sourceInstanceId, CardDefinition sourceDef, EffectDefinition effect, TargetRef? chosenTarget)
        {
            Player = player;
            SourceInstanceId = sourceInstanceId;
            SourceDef = sourceDef;
            Effect = effect;
            ChosenTarget = chosenTarget;
        }
    }

    /// <summary>効果解決(Docs/50_CardGame/spec/02-card-effects.md「アクション」「対象」「解決順序」)。</summary>
    public sealed partial class GameEngine
    {
        /// <summary>
        /// キューが空になるまで効果を解決する。
        /// 解決中に発生したトリガー(ラストワード)はキュー末尾に積まれ、再帰しない。
        /// </summary>
        private void ResolveEffectQueue()
        {
            while (_effectQueue.Count > 0)
            {
                if (State.IsFinished)
                {
                    _effectQueue.Clear();
                    return;
                }
                var pending = _effectQueue.Dequeue();
                Emit(new EffectTriggeredEvent(pending.Player, pending.SourceInstanceId, pending.SourceDef.Id, pending.Effect.Trigger));
                foreach (var action in pending.Effect.Actions)
                {
                    if (State.IsFinished) break;
                    ApplyAction(pending, action);
                    ProcessDeaths();
                }
            }
        }

        private void ApplyAction(PendingEffect ctx, ActionDefinition action)
        {
            var ps = State.PlayerOf(ctx.Player);
            switch (action.Type)
            {
                case ActionType.Damage:
                    foreach (var t in ResolveTargets(ctx, action.Target!.Value))
                        DealDamage(t, action.Amount, ctx.SourceInstanceId);
                    break;

                case ActionType.Heal:
                    foreach (var t in ResolveTargets(ctx, action.Target!.Value))
                        Heal(t, action.Amount);
                    break;

                case ActionType.Draw:
                    for (int i = 0; i < action.Count; i++)
                    {
                        Draw(ps);
                        if (State.IsFinished) return;
                    }
                    break;

                case ActionType.Destroy:
                    foreach (var t in ResolveTargets(ctx, action.Target!.Value))
                    {
                        var e = State.Resolve(t);
                        if (e != null) e.PendingDestroy = true;
                    }
                    break;

                case ActionType.Buff:
                    foreach (var t in ResolveTargets(ctx, action.Target!.Value))
                    {
                        var e = State.Resolve(t);
                        if (e == null || !e.IsFollower) continue;
                        e.Attack = Math.Max(0, e.Attack + action.AttackDelta);
                        e.Health += action.HealthDelta;
                        e.MaxHealth = Math.Max(1, e.MaxHealth + action.HealthDelta);
                        Emit(new BuffedEvent(e.InstanceId, action.AttackDelta, action.HealthDelta, e.Attack, e.Health));
                    }
                    break;

                case ActionType.Summon:
                    {
                        var def = Db.Get(action.CardId!);
                        for (int i = 0; i < action.Count; i++)
                        {
                            if (ps.BoardIsFull) break;
                            var entity = new BoardEntity(State.AllocateInstanceId(), def, ctx.Player);
                            ps.Board.Add(entity); // 効果による召喚は場の右端に出す
                            Emit(new EntityEnteredBoardEvent(ctx.Player, entity.InstanceId, def.Id, ps.Board.Count - 1, entity.Attack, entity.Health, byEffect: true));
                        }
                    }
                    break;

                case ActionType.GainPP:
                    ps.Pp = Math.Min(ps.MaxPp, ps.Pp + action.Amount);
                    Emit(new PpChangedEvent(ctx.Player, ps.Pp, ps.MaxPp));
                    break;

                case ActionType.RampPP:
                    {
                        int before = ps.MaxPp;
                        ps.MaxPp = Math.Min(GameRules.PpMax, ps.MaxPp + action.Amount);
                        ps.Pp = Math.Min(ps.MaxPp, ps.Pp + (ps.MaxPp - before));
                        Emit(new PpChangedEvent(ctx.Player, ps.Pp, ps.MaxPp));
                    }
                    break;

                default:
                    throw new InvalidOperationException($"未対応のアクション {action.Type}");
            }
        }

        // ------------------------------------------------------------------
        // 対象解決
        // ------------------------------------------------------------------

        /// <summary>
        /// Select* 対象として選べる候補。source は場にいる効果元(スペルなら null)。
        /// </summary>
        public IReadOnlyList<TargetRef> ValidSelectTargets(int player, TargetKind kind, BoardEntity? source)
        {
            var me = State.PlayerOf(player);
            var enemy = State.OpponentOf(player);
            var list = new List<TargetRef>();
            switch (kind)
            {
                case TargetKind.SelectEnemyFollower:
                    list.AddRange(enemy.Followers.Select(TargetRef.Entity));
                    break;
                case TargetKind.SelectAllyFollower:
                    list.AddRange(me.Followers.Where(f => f != source).Select(TargetRef.Entity));
                    break;
                case TargetKind.SelectFollower:
                    list.AddRange(me.Followers.Where(f => f != source).Select(TargetRef.Entity));
                    list.AddRange(enemy.Followers.Select(TargetRef.Entity));
                    break;
                case TargetKind.SelectEnemy:
                    list.Add(TargetRef.Leader(enemy.Index));
                    list.AddRange(enemy.Followers.Select(TargetRef.Entity));
                    break;
                default:
                    throw new ArgumentException($"{kind} は選択対象ではない");
            }
            return list;
        }

        private IEnumerable<TargetRef> ResolveTargets(PendingEffect ctx, TargetKind kind)
        {
            var me = State.PlayerOf(ctx.Player);
            var enemy = State.OpponentOf(ctx.Player);
            var self = State.FindEntity(ctx.SourceInstanceId);

            switch (kind)
            {
                case TargetKind.EnemyLeader:
                    return new[] { TargetRef.Leader(enemy.Index) };
                case TargetKind.AllyLeader:
                    return new[] { TargetRef.Leader(me.Index) };
                case TargetKind.Self:
                    return self != null ? new[] { TargetRef.Entity(self) } : Array.Empty<TargetRef>();

                case TargetKind.SelectEnemyFollower:
                case TargetKind.SelectAllyFollower:
                case TargetKind.SelectFollower:
                case TargetKind.SelectEnemy:
                    // 選択済みの対象が今も存在する場合のみ(解決中に破壊されていたら何もしない)
                    if (!ctx.ChosenTarget.HasValue) return Array.Empty<TargetRef>();
                    var t = ctx.ChosenTarget.Value;
                    if (!t.IsLeader && State.FindEntity(t.InstanceId) == null) return Array.Empty<TargetRef>();
                    return new[] { t };

                case TargetKind.AllEnemyFollowers:
                    return enemy.Followers.Select(TargetRef.Entity).ToList();
                case TargetKind.AllAllyFollowers:
                    return me.Followers.Select(TargetRef.Entity).ToList();
                case TargetKind.AllFollowers:
                    // 手番プレイヤー側 → 相手側の順(死亡処理の順序と揃える)
                    return State.Current.Followers.Select(TargetRef.Entity)
                        .Concat(State.Opponent.Followers.Select(TargetRef.Entity)).ToList();

                case TargetKind.RandomEnemyFollower:
                    {
                        var pool = enemy.Followers.ToList();
                        return pool.Count == 0 ? Array.Empty<TargetRef>() : new[] { TargetRef.Entity(pool[State.Rng.Next(pool.Count)]) };
                    }
                case TargetKind.RandomAllyFollower:
                    {
                        var pool = me.Followers.Where(f => f != self).ToList();
                        return pool.Count == 0 ? Array.Empty<TargetRef>() : new[] { TargetRef.Entity(pool[State.Rng.Next(pool.Count)]) };
                    }
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind));
            }
        }

        // ------------------------------------------------------------------
        // ダメージ・回復・死亡
        // ------------------------------------------------------------------

        /// <summary>ダメージを与える。アミュレットや存在しない対象には 0。実際に与えた量を返す。</summary>
        private int DealDamage(TargetRef target, int amount, int? sourceInstanceId)
        {
            if (amount <= 0) return 0;
            if (target.IsLeader)
            {
                var ps = State.PlayerOf(target.PlayerIndex);
                ps.Hp -= amount;
                Emit(new DamageDealtEvent(target, amount, sourceInstanceId, ps.Hp));
                return amount;
            }
            var e = State.FindEntity(target.InstanceId);
            if (e == null || !e.IsFollower) return 0;
            e.Health -= amount;
            Emit(new DamageDealtEvent(target, amount, sourceInstanceId, e.Health));
            return amount;
        }

        private void Heal(TargetRef target, int amount)
        {
            if (amount <= 0) return;
            if (target.IsLeader)
            {
                var ps = State.PlayerOf(target.PlayerIndex);
                int before = ps.Hp;
                ps.Hp = Math.Min(GameRules.LeaderMaxHp, ps.Hp + amount);
                if (ps.Hp != before) Emit(new HealedEvent(target, ps.Hp - before, ps.Hp));
                return;
            }
            var e = State.FindEntity(target.InstanceId);
            if (e == null || !e.IsFollower) return;
            int b = e.Health;
            e.Health = Math.Min(e.MaxHealth, e.Health + amount);
            if (e.Health != b) Emit(new HealedEvent(target, e.Health - b, e.Health));
        }

        /// <summary>
        /// 死亡チェック: 体力 0 以下 / カウントダウン 0 / 破壊指定 のエンティティを
        /// 手番プレイヤーの場 → 相手の場、それぞれ左から順に墓場へ送り、ラストワードをキュー末尾に積む。
        /// </summary>
        private void ProcessDeaths()
        {
            var order = new[] { State.Current, State.Opponent };
            foreach (var ps in order)
            {
                var dead = ps.Board.Where(e => e.IsDead).ToList();
                foreach (var e in dead)
                {
                    ps.Board.Remove(e);
                    ps.Graveyard.Add(e.Definition);
                    Emit(new EntityDestroyedEvent(ps.Index, e.InstanceId, e.Definition.Id));
                    foreach (var effect in e.Definition.EffectsFor(Trigger.LastWords))
                        _effectQueue.Enqueue(new PendingEffect(ps.Index, e.InstanceId, e.Definition, effect, null));
                }
            }
        }
    }
}
