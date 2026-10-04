using System;
using System.Collections.Generic;

namespace MiniGame.PenguinWars.Battle
{
    /// <summary>
    /// エンドレスの敵の湧き（仕様書 §8.1）。時間でレベルが上がり、出現間隔・出せるキャラ・倍率が変わる。
    /// 出すキャラを決めるだけで、実際の出撃は BattleWorld が行う
    /// </summary>
    public class EnemyWaveDirector
    {
        private readonly EnemyWaveSettings _settings;
        private readonly IReadOnlyList<UnitStats> _pool;
        private readonly Random _random;
        // 毎回 List を作らないよう使い回す
        private readonly List<UnitStats> _candidates = new List<UnitStats>();
        // 5の倍数レベルで確定で出す大型の候補（仕様書 §8.1）
        private readonly List<UnitStats> _bosses;
        private float _levelTimer;
        private float _spawnTimer;

        /// <param name="pool">敵として出せる全キャラ（味方と同じデータ。§8.1）</param>
        public EnemyWaveDirector(EnemyWaveSettings settings, IReadOnlyList<UnitStats> pool, int seed)
        {
            _settings = settings;
            _pool = pool;
            _random = new Random(seed);
            _bosses = FindBosses(pool);
            _levelTimer = settings.LevelUpInterval;
            // 開始直後に湧くと、味方を出す前に押し込まれるので1間隔待つ
            _spawnTimer = SpawnInterval;
        }

        public int Level { get; private set; } = 1;

        public float SpawnInterval =>
            Math.Max(_settings.MinSpawnInterval, _settings.BaseSpawnInterval * (float)Math.Pow(_settings.SpawnIntervalMultiplier, Level - 1));

        public int CostLimit => _settings.CostLimitBase + Level * _settings.CostLimitPerLevel;

        public float StatMultiplier => _settings.StatMultiplierBase + Level * _settings.StatMultiplierPerLevel;

        /// <summary>このステップで出すキャラ（倍率をかけ済み）を spawns に入れる。レベルが上がったら true</summary>
        public bool Tick(float deltaTime, List<UnitStats> spawns)
        {
            spawns.Clear();
            bool leveledUp = TickLevel(deltaTime);
            if (leveledUp && Level % _settings.BossLevelInterval == 0 && _bosses.Count > 0)
            {
                spawns.Add(_bosses[_random.Next(_bosses.Count)].Scaled(StatMultiplier));
            }

            _spawnTimer -= deltaTime;
            if (_spawnTimer > 0f) return leveledUp;

            _spawnTimer += SpawnInterval;
            UnitStats picked = PickWithinCostLimit();
            if (picked != null) spawns.Add(picked.Scaled(StatMultiplier));
            return leveledUp;
        }

        private bool TickLevel(float deltaTime)
        {
            _levelTimer -= deltaTime;
            if (_levelTimer > 0f) return false;

            _levelTimer += _settings.LevelUpInterval;
            Level++;
            return true;
        }

        private UnitStats PickWithinCostLimit()
        {
            _candidates.Clear();
            foreach (UnitStats stats in _pool)
            {
                if (stats.Cost <= CostLimit) _candidates.Add(stats);
            }
            return _candidates.Count > 0 ? _candidates[_random.Next(_candidates.Count)] : null;
        }

        /// <summary>
        /// 大型（コスト上限を無視して出す）。大型が1体もいないプール（テスト用の小さい編成など）では最もコストの高いキャラで代わりにする
        /// </summary>
        private static List<UnitStats> FindBosses(IReadOnlyList<UnitStats> pool)
        {
            var bosses = new List<UnitStats>();
            UnitStats mostExpensive = null;
            foreach (UnitStats stats in pool)
            {
                if (stats.Role == UnitRole.Large) bosses.Add(stats);
                if (mostExpensive == null || stats.Cost > mostExpensive.Cost) mostExpensive = stats;
            }
            if (bosses.Count == 0 && mostExpensive != null) bosses.Add(mostExpensive);
            return bosses;
        }
    }
}
