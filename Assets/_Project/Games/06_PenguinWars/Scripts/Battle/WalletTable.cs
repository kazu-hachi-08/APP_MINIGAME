using System;

namespace MiniGame.PenguinWars.Battle
{
    /// <summary>
    /// 働きペンギンのレベル表（仕様書 §4.2）。PenguinWarsBalance の表を Battle 用に写したもの（Battle は ScriptableObject を知らないため）。
    /// レベルは 1 始まり
    /// </summary>
    public class WalletTable
    {
        private readonly int[] _caps;
        private readonly float[] _ratesPerSecond;
        private readonly int[] _levelUpCosts;

        /// <param name="levelUpCosts">レベル N → N+1 に必要な額。最大レベルの分は無いので caps より1つ短い</param>
        public WalletTable(int[] caps, float[] ratesPerSecond, int[] levelUpCosts)
        {
            if (caps.Length == 0 || ratesPerSecond.Length != caps.Length || levelUpCosts.Length != caps.Length - 1)
            {
                throw new ArgumentException("WalletTable: caps と rates は同じ長さ、levelUpCosts は1つ短くしてください");
            }
            _caps = caps;
            _ratesPerSecond = ratesPerSecond;
            _levelUpCosts = levelUpCosts;
        }

        public int MaxLevel => _caps.Length;

        public int GetCap(int level) => _caps[level - 1];
        public float GetRate(int level) => _ratesPerSecond[level - 1];
        /// <summary>最大レベルでは 0</summary>
        public int GetLevelUpCost(int level) => level >= MaxLevel ? 0 : _levelUpCosts[level - 1];

        /// <summary>仕様書 §4.2 の初期案。テストや Balance が無いときの既定値</summary>
        public static WalletTable CreateDefault()
        {
            return new WalletTable(
                new[] { 600, 1000, 1400, 1800, 2200, 2700, 3200, 4000 },
                new[] { 30f, 40f, 50f, 60f, 70f, 80f, 90f, 100f },
                new[] { 80, 160, 240, 320, 400, 480, 560 });
        }
    }
}
