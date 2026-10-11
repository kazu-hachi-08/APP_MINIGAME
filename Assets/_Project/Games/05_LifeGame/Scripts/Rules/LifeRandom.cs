using System;
using System.Collections.Generic;

namespace MiniGame.LifeGame
{
    /// <summary>
    /// シード付きの決定論的乱数（xorshift64*）。
    /// System.Random は実装がランタイムで変わり得るため使わない。オンラインで全端末が同じ盤面・出目を出すため。
    /// </summary>
    public sealed class LifeRandom
    {
        // xorshift は状態 0 が不動点になるので、シードに掛けて散らす
        private const ulong SeedMixer = 0x9E3779B97F4A7C15UL;

        private ulong _state;

        public LifeRandom(int seed)
        {
            _state = ((ulong)(uint)seed + 1UL) * SeedMixer;
        }

        public ulong NextUInt64()
        {
            ulong x = _state;
            x ^= x >> 12;
            x ^= x << 25;
            x ^= x >> 27;
            _state = x;
            return x * 0x2545F4914F6CDD1DUL;
        }

        /// <summary>[0, maxExclusive) の整数</summary>
        public int Next(int maxExclusive)
        {
            if (maxExclusive <= 0) throw new ArgumentOutOfRangeException(nameof(maxExclusive));
            return (int)(NextUInt64() % (ulong)maxExclusive);
        }

        /// <summary>[minInclusive, maxInclusive] の整数</summary>
        public int Range(int minInclusive, int maxInclusive)
        {
            return minInclusive + Next(maxInclusive - minInclusive + 1);
        }

        /// <summary>Fisher–Yates シャッフル（その場で並べ替える）</summary>
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
