using System;

namespace MiniGame.PenguinWars.Battle
{
    /// <summary>陣営ごとのさかな（仕様書 §4.1）と働きペンギンのレベル（§4.2）</summary>
    public class WalletState
    {
        private readonly WalletTable _table;
        // 1ステップ（1/30秒）の増分は1未満なので float で溜める。表示は切り捨てで整数にする
        private float _fish;

        public WalletState(WalletTable table)
        {
            _table = table;
            Level = 1;
        }

        public int Level { get; private set; }
        public int Fish => (int)_fish;
        public int Cap => _table.GetCap(Level);
        public float RatePerSecond => _table.GetRate(Level);
        public bool IsMaxLevel => Level >= _table.MaxLevel;
        /// <summary>次のレベルに必要な額。最大レベルでは 0</summary>
        public int LevelUpCost => _table.GetLevelUpCost(Level);
        public bool CanLevelUp => !IsMaxLevel && Fish >= LevelUpCost;

        public void Tick(float deltaTime)
        {
            _fish = Math.Min(Cap, _fish + RatePerSecond * deltaTime);
        }

        public bool CanAfford(int cost) => Fish >= cost;

        public bool TrySpend(int cost)
        {
            if (!CanAfford(cost)) return false;

            _fish -= cost;
            return true;
        }

        public bool TryLevelUp()
        {
            if (!CanLevelUp) return false;

            _fish -= LevelUpCost;
            Level++;
            return true;
        }

        /// <summary>オンラインのゲスト用。ホストから届いた値をそのまま写す（ゲストは自分で計算しない）</summary>
        internal void Restore(int level, float fish)
        {
            Level = level;
            _fish = fish;
        }

        /// <summary>撃破報酬（Phase 4）用。上限は超えない</summary>
        public void Add(int amount)
        {
            _fish = Math.Min(Cap, _fish + amount);
        }
    }
}
