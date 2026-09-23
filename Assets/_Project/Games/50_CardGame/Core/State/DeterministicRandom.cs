using System;
using System.Collections.Generic;

namespace CardGame.Core.State
{
    /// <summary>
    /// シード付きの決定論的乱数(xorshift64*)。
    /// System.Random は実装がランタイム/バージョンで変わり得るため使わない(ADR-0001)。
    /// </summary>
    public sealed class DeterministicRandom
    {
        private ulong _state;

        public DeterministicRandom(ulong seed)
        {
            // 0 は xorshift の不動点なので避ける
            _state = seed == 0 ? 0x9E3779B97F4A7C15UL : seed;
        }

        private DeterministicRandom(ulong state, bool _) { _state = state; }

        public DeterministicRandom Clone() => new DeterministicRandom(_state, true);

        public ulong NextUInt64()
        {
            ulong x = _state;
            x ^= x >> 12;
            x ^= x << 25;
            x ^= x >> 27;
            _state = x;
            return x * 0x2545F4914F6CDD1DUL;
        }

        /// <summary>[0, maxExclusive) の整数。</summary>
        public int Next(int maxExclusive)
        {
            if (maxExclusive <= 0) throw new ArgumentOutOfRangeException(nameof(maxExclusive));
            return (int)(NextUInt64() % (ulong)maxExclusive);
        }

        /// <summary>Fisher–Yates シャッフル(in-place)。</summary>
        public void Shuffle<T>(IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
