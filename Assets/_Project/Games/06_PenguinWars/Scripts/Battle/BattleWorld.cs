using System;
using System.Collections.Generic;

namespace MiniGame.PenguinWars.Battle
{
    /// <summary>
    /// 戦闘のすべて（ユニット・城・お金・砲）を持つ純C#の世界。MonoBehaviour は Enqueue で操作を渡し、Step で進め、状態とイベントを読むだけにする。
    /// オンラインではホストだけがこれを動かす（INDEX「全体設計」）。
    /// このファイルは準備・状態の読み出し・操作の処理。ユニットの行動とダメージは BattleWorld.Combat.cs
    /// </summary>
    public partial class BattleWorld
    {
        private static readonly Predicate<UnitState> IsDeadPredicate = unit => unit.IsDead;

        private readonly BattleSettings _settings;
        private readonly CastleState _leftCastle;
        private readonly CastleState _rightCastle;
        private readonly List<UnitState> _units = new List<UnitState>();
        private readonly IReadOnlyList<UnitStats>[] _decks = new IReadOnlyList<UnitStats>[2];
        private readonly DeckSlotState[][] _slots = new DeckSlotState[2][];
        private readonly WalletState[] _wallets = new WalletState[2];
        private readonly CannonState[] _cannons = new CannonState[2];
        private readonly int[] _killCounts = new int[2];
        private readonly Queue<BattleCommand> _commands = new Queue<BattleCommand>();
        private readonly List<BattleEvent> _events = new List<BattleEvent>();
        // 毎攻撃・毎ステップで List を作らないよう使い回す
        private readonly List<UnitState> _targets = new List<UnitState>();
        private readonly List<UnitStats> _waveSpawns = new List<UnitStats>();
        private readonly Random _random;
        private EnemyWaveDirector _enemyWaves;
        private int _nextUnitId = 1;

        public IReadOnlyList<UnitState> Units => _units;
        public bool IsFinished { get; private set; }
        /// <summary>城を落とされた側。IsFinished のときだけ意味がある</summary>
        public Side Loser { get; private set; }
        /// <summary>エンドレスの敵レベル。湧きを設定していなければ 0</summary>
        public int EnemyLevel => _enemyWaves?.Level ?? 0;

        public BattleWorld(BattleSettings settings)
        {
            _settings = settings;
            _random = new Random(settings.RandomSeed);
            _leftCastle = new CastleState(Side.Left, 0f, settings.LeftCastleHp, false);
            _rightCastle = new CastleState(Side.Right, settings.FieldLength, settings.RightCastleHp, settings.RightCastleInvincible);
            for (int side = 0; side < 2; side++)
            {
                _wallets[side] = new WalletState(settings.WalletTable);
                _cannons[side] = new CannonState(settings.CannonChargeTime);
            }
        }

        public CastleState GetCastle(Side side)
        {
            return side == Side.Left ? _leftCastle : _rightCastle;
        }

        /// <summary>出撃ボタンの並び（slot → キャラ）。陣営ごとに別の編成を持てる</summary>
        public void SetDeck(Side side, IReadOnlyList<UnitStats> deck)
        {
            _decks[(int)side] = deck;
            var slots = new DeckSlotState[deck.Count];
            for (int i = 0; i < slots.Length; i++) slots[i] = new DeckSlotState();
            _slots[(int)side] = slots;
        }

        /// <summary>エンドレスの敵の湧き（右陣営）。設定すると Step のたびに進む</summary>
        public void SetEnemyWaves(EnemyWaveDirector enemyWaves)
        {
            _enemyWaves = enemyWaves;
        }

        /// <summary>編成。未設定なら空</summary>
        public IReadOnlyList<UnitStats> GetDeck(Side side)
        {
            return _decks[(int)side] ?? Array.Empty<UnitStats>();
        }

        public DeckSlotState GetSlot(Side side, int slotIndex)
        {
            return _slots[(int)side][slotIndex];
        }

        public WalletState GetWallet(Side side)
        {
            return _wallets[(int)side];
        }

        public CannonState GetCannon(Side side)
        {
            return _cannons[(int)side];
        }

        /// <summary>side が倒した敵ユニットの数（リザルトの撃破数）</summary>
        public int GetKillCount(Side side)
        {
            return _killCounts[(int)side];
        }

        /// <summary>今出撃できるか。ボタンを暗くする判定と実際の出撃で同じ条件を使うため、ここ1か所にまとめる</summary>
        public bool CanSpawn(Side side, int slotIndex)
        {
            IReadOnlyList<UnitStats> deck = _decks[(int)side];
            if (deck == null || slotIndex < 0 || slotIndex >= deck.Count) return false;
            if (CountUnits(side) >= _settings.MaxUnitsPerSide) return false;
            if (IsSpawnFree(side)) return true;

            return GetSlot(side, slotIndex).IsReady && GetWallet(side).CanAfford(deck[slotIndex].Cost);
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

            TickEconomy(deltaTime);
            TickEnemyWaves(deltaTime);
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
                switch (command.Type)
                {
                    case BattleCommandType.Spawn:
                        TrySpawn(command.Side, command.SlotIndex);
                        break;
                    case BattleCommandType.LevelUpWallet:
                        if (!IsSpawnFree(command.Side)) GetWallet(command.Side).TryLevelUp();
                        break;
                    case BattleCommandType.FireCannon:
                        if (GetCannon(command.Side).TryFire()) FireCannon(command.Side);
                        break;
                }
            }
        }

        private bool IsSpawnFree(Side side)
        {
            return side == Side.Right && _settings.RightSpawnsFree;
        }

        private void TickEconomy(float deltaTime)
        {
            for (int side = 0; side < _wallets.Length; side++)
            {
                _wallets[side].Tick(deltaTime);
                _cannons[side].Tick(deltaTime);
                if (_slots[side] == null) continue;

                foreach (DeckSlotState slot in _slots[side]) slot.Tick(deltaTime);
            }
        }

        private void TickEnemyWaves(float deltaTime)
        {
            if (_enemyWaves == null) return;

            if (_enemyWaves.Tick(deltaTime, _waveSpawns))
            {
                _events.Add(new BattleEvent(BattleEventType.EnemyLevelUp, Side.Right, BattleEvent.CastleId, _rightCastle.X, _enemyWaves.Level));
            }
            foreach (UnitStats stats in _waveSpawns)
            {
                // 上限を超えた分は捨てる（溜めておくと、空いた瞬間に一斉に湧いて理不尽になるため）
                if (CountUnits(Side.Right) < _settings.MaxUnitsPerSide) AddUnit(Side.Right, stats);
            }
        }

        /// <summary>条件を満たさなければ黙って何もしない（連打やオンラインの遅れて届いた操作で出せないのは普通のことなので）</summary>
        private void TrySpawn(Side side, int slotIndex)
        {
            if (!CanSpawn(side, slotIndex)) return;

            UnitStats stats = _decks[(int)side][slotIndex];
            if (!IsSpawnFree(side))
            {
                GetWallet(side).TrySpend(stats.Cost);
                GetSlot(side, slotIndex).StartCooldown(stats.Cooldown);
            }
            AddUnit(side, stats);
        }

        private void AddUnit(Side side, UnitStats stats)
        {
            float x = GetCastle(side).X + side.Forward() * _settings.SpawnOffset;
            var unit = new UnitState(_nextUnitId++, side, stats, x);
            _units.Add(unit);
            _events.Add(new BattleEvent(BattleEventType.Spawned, side, unit.Id, x));
        }
    }
}
