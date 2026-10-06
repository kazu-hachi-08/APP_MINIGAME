using System;
using System.Collections.Generic;

namespace MiniGame.PenguinWars.Battle
{
    /// <summary>じぶんペンギンで強化できる数値。画面で5行を同じコードで回せるよう enum で引く</summary>
    public enum CustomStat
    {
        Hp,
        Attack,
        Range,
        Speed,
        Cooldown,
    }

    /// <summary>
    /// じぶんペンギンの強化レベル（各 MinLevel〜MaxLevel）。レベル1つで CustomUnitRules.LevelStep ぶん良くなる。
    /// 戦闘は StatTweak しか見ないので、ToTweak で既存キャラと同じ倍率に変換する
    /// </summary>
    public class CustomStatLevels
    {
        public static readonly IReadOnlyList<CustomStat> AllStats = (CustomStat[])Enum.GetValues(typeof(CustomStat));

        private readonly int[] _levels = new int[AllStats.Count];

        public int Hp { get => Get(CustomStat.Hp); set => Set(CustomStat.Hp, value); }
        public int Attack { get => Get(CustomStat.Attack); set => Set(CustomStat.Attack, value); }
        public int Range { get => Get(CustomStat.Range); set => Set(CustomStat.Range, value); }
        public int Speed { get => Get(CustomStat.Speed); set => Set(CustomStat.Speed, value); }
        public int Cooldown { get => Get(CustomStat.Cooldown); set => Set(CustomStat.Cooldown, value); }

        public int Get(CustomStat stat) => _levels[(int)stat];

        public void Set(CustomStat stat, int level) => _levels[(int)stat] = level;

        public void Reset() => Array.Clear(_levels, 0, _levels.Length);

        public CustomStatLevels Clone()
        {
            var copy = new CustomStatLevels();
            Array.Copy(_levels, copy._levels, _levels.Length);
            return copy;
        }

        /// <summary>再生産だけは「短いほど良い」ので、+1 で倍率を下げる</summary>
        public StatTweak ToTweak()
        {
            return new StatTweak
            {
                Hp = Multiplier(Hp),
                Attack = Multiplier(Attack),
                Range = Multiplier(Range),
                Speed = Multiplier(Speed),
                Cooldown = Multiplier(-Cooldown),
            };
        }

        private static float Multiplier(int level) => 1f + level * CustomUnitRules.LevelStep;
    }
}
