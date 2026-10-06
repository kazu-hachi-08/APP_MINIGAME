using System;
using System.Collections.Generic;
using MiniGame.PenguinWars.Battle;

namespace MiniGame.PenguinWars
{
    /// <summary>ずかんの並べ替えに使う数値1つ分。マスの左上に出す数字と、並べ替えの値を同じものにする</summary>
    public class ZukanStatColumn
    {
        private readonly Func<UnitStats, float> _value;
        private readonly string _format;

        public string Label { get; }

        public ZukanStatColumn(string label, Func<UnitStats, float> value, string format)
        {
            Label = label;
            _value = value;
            _format = format;
        }

        public float Value(UnitStats stats) => _value(stats);
        public string Format(UnitStats stats) => _value(stats).ToString(_format);
    }

    /// <summary>ずかんの特性しぼりこみ1つ分</summary>
    public class ZukanTraitFilter
    {
        private readonly Func<UnitStats, bool> _matches;

        public string Label { get; }

        public ZukanTraitFilter(string label, Func<UnitStats, bool> matches)
        {
            Label = label;
            _matches = matches;
        }

        public bool Matches(UnitStats stats) => _matches(stats);
    }

    /// <summary>
    /// ずかん・編成画面のドロップダウンに並べる選択肢と、並べ替えの決まり（仕様書 §2.0）。先頭が開いたときの状態になる
    /// </summary>
    public static class ZukanListOptions
    {
        private const string IntegerFormat = "0";
        private const string DecimalFormat = "0.##";

        public static readonly ZukanStatColumn[] Columns =
        {
            new ZukanStatColumn("コスト", s => s.Cost, IntegerFormat),
            new ZukanStatColumn("攻撃", s => s.Attack, IntegerFormat),
            new ZukanStatColumn("体力", s => s.MaxHp, IntegerFormat),
            new ZukanStatColumn("射程", s => s.Range, DecimalFormat),
            new ZukanStatColumn("速度", s => s.MoveSpeed, DecimalFormat),
            new ZukanStatColumn("再生産", s => s.Cooldown, DecimalFormat),
        };

        public static readonly ZukanTraitFilter[] Traits = BuildTraits();

        public static List<string> ColumnLabels()
        {
            var labels = new List<string>();
            foreach (ZukanStatColumn column in Columns) labels.Add(column.Label);
            return labels;
        }

        /// <summary>column の値で並べ、同じ値どうしは No 順にする（押すたびに並びが入れ替わって見えないように）</summary>
        public static int Compare(UnitStats a, UnitStats b, ZukanStatColumn column, bool descending)
        {
            int byValue = column.Value(a).CompareTo(column.Value(b));
            if (descending) byValue = -byValue;
            return byValue != 0 ? byValue : a.UnitNo.CompareTo(b.UnitNo);
        }

        /// <summary>enum から作るので、能力や役割を足せばドロップダウンの選択肢にも自動で並ぶ</summary>
        private static ZukanTraitFilter[] BuildTraits()
        {
            var traits = new List<ZukanTraitFilter> { new ZukanTraitFilter("すべて", _ => true) };
            foreach (UnitAbilityType type in Enum.GetValues(typeof(UnitAbilityType)))
            {
                traits.Add(new ZukanTraitFilter(UnitLabels.Ability(type), s => s.HasAbility(type)));
            }
            traits.Add(new ZukanTraitFilter(UnitLabels.AttackRange(true), s => s.IsAreaAttack));
            foreach (UnitRole role in Enum.GetValues(typeof(UnitRole)))
            {
                traits.Add(new ZukanTraitFilter(UnitLabels.Role(role), s => s.Role == role));
            }
            return traits.ToArray();
        }
    }
}
