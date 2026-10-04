using System;
using System.Collections.Generic;

namespace MiniGame.PenguinWars.Battle
{
    /// <summary>
    /// 戦闘のすべて（ユニット・城）を持つ純C#の世界。MonoBehaviour は Enqueue で操作を渡し、Step で進め、状態とイベントを読むだけにする。
    /// オンラインではホストだけがこれを動かす（INDEX「全体設計」）
    /// </summary>
    public class BattleWorld
    {
        private static readonly Predicate<UnitState> IsDeadPredicate = unit => unit.IsDead;

        private readonly BattleSettings _settings;
        private readonly CastleState _leftCastle;
        private readonly CastleState _rightCastle;
        private readonly List<UnitState> _units = new List<UnitState>();
        private readonly IReadOnlyList<UnitStats>[] _decks = new IReadOnlyList<UnitStats>[2];
        private readonly Queue<BattleCommand> _commands = new Queue<BattleCommand>();
        private readonly List<BattleEvent> _events = new List<BattleEvent>();
        // 毎攻撃で List を作らないよう使い回す
        private readonly List<UnitState> _targets = new List<UnitState>();
        private int _nextUnitId = 1;

        public IReadOnlyList<UnitState> Units => _units;
        public bool IsFinished { get; private set; }
        /// <summary>城を落とされた側。IsFinished のときだけ意味がある</summary>
        public Side Loser { get; private set; }

        public BattleWorld(BattleSettings settings)
        {
            _settings = settings;
            _leftCastle = new CastleState(Side.Left, 0f, settings.LeftCastleHp, false);
            _rightCastle = new CastleState(Side.Right, settings.FieldLength, settings.RightCastleHp, settings.RightCastleInvincible);
        }

        public CastleState GetCastle(Side side)
        {
            return side == Side.Left ? _leftCastle : _rightCastle;
        }

        /// <summary>出撃ボタンの並び（slot → キャラ）。陣営ごとに別の編成を持てる</summary>
        public void SetDeck(Side side, IReadOnlyList<UnitStats> deck)
        {
            _decks[(int)side] = deck;
        }

        public int CountUnits(Side side)
        {
            int count = 0;
            foreach (UnitState unit in _units)
            {
                if (unit.Side == side && !unit.IsDead) count++;
            }
            return count;
        }

        public void Enqueue(BattleCommand command)
        {
            _commands.Enqueue(command);
        }

        public void Step(float deltaTime)
        {
            if (IsFinished) return;

            ProcessCommands();
            // ループ中に死んだユニットは Dead にしておき、最後にまとめて除く（途中で消すと添字がずれるため）
            foreach (UnitState unit in _units)
            {
                if (unit.IsDead) continue;

                TickUnit(unit, deltaTime);
                if (IsFinished) break;
            }
            _units.RemoveAll(IsDeadPredicate);
        }

        /// <summary>溜まったイベントを output に移す。呼ぶまで溜まり続ける</summary>
        public void DrainEvents(List<BattleEvent> output)
        {
            output.AddRange(_events);
            _events.Clear();
        }

        private void ProcessCommands()
        {
            while (_commands.Count > 0)
            {
                BattleCommand command = _commands.Dequeue();
                if (command.Type == BattleCommandType.Spawn) TrySpawn(command.Side, command.SlotIndex);
            }
        }

        private void TrySpawn(Side side, int slotIndex)
        {
            IReadOnlyList<UnitStats> deck = _decks[(int)side];
            if (deck == null || slotIndex < 0 || slotIndex >= deck.Count) return;
            if (CountUnits(side) >= _settings.MaxUnitsPerSide) return;

            float x = GetCastle(side).X + side.Forward() * _settings.SpawnOffset;
            var unit = new UnitState(_nextUnitId++, side, deck[slotIndex], x);
            _units.Add(unit);
            _events.Add(new BattleEvent(BattleEventType.Spawned, side, unit.Id, x));
        }

        private void TickUnit(UnitState unit, float deltaTime)
        {
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

            float x = unit.X + unit.Side.Forward() * unit.Stats.MoveSpeed * deltaTime;
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
            }
            if (hitCastle) DamageCastle(enemyCastle, attacker.Stats.Attack);
        }

        private void DamageUnit(UnitState target, int amount)
        {
            target.Hp = Math.Max(0, target.Hp - amount);
            _events.Add(new BattleEvent(BattleEventType.Hit, target.Side, target.Id, target.X, amount));
            if (target.Hp > 0) return;

            target.Action = UnitAction.Dead;
            _events.Add(new BattleEvent(BattleEventType.Died, target.Side, target.Id, target.X));
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
