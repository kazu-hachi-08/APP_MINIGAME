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

    /// <summary>ずかんのしぼりこみ1つ分（役割・能力のドロップダウンの1項目）</summary>
    public class ZukanFilter
    {
        private readonly Func<UnitStats, bool> _matches;

        public string Label { get; }

        public ZukanFilter(string label, Func<UnitStats, bool> matches)
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
        private const string AllLabel = "すべて";

        public static readonly ZukanStatColumn[] Columns =
        {
            new ZukanStatColumn("コスト", s => s.Cost, IntegerFormat),
            new ZukanStatColumn("攻撃", s => s.Attack, IntegerFormat),
            new ZukanStatColumn("体力", s => s.MaxHp, IntegerFormat),
            new ZukanStatColumn("射程", s => s.Range, DecimalFormat),
            new ZukanStatColumn("速度", s => s.MoveSpeed, DecimalFormat),
            new ZukanStatColumn("再生産", s => s.Cooldown, DecimalFormat),
        };

        public static readonly ZukanFilter[] Roles = BuildRoles();
        public static readonly ZukanFilter[] Abilities = BuildAbilities();

        public static List<string> ColumnLabels()
        {
            var labels = new List<string>();
            foreach (ZukanStatColumn column in Columns) labels.Add(column.Label);
            return labels;
        }

        public static List<string> FilterLabels(ZukanFilter[] filters)
        {
            var labels = new List<string>();
            foreach (ZukanFilter filter in filters) labels.Add(filter.Label);
            return labels;
        }

        /// <summary>column の値で並べ、同じ値どうしは No 順にする（押すたびに並びが入れ替わって見えないように）</summary>
        public static int Compare(UnitStats a, UnitStats b, ZukanStatColumn column, bool descending)
        {
            int byValue = column.Value(a).CompareTo(column.Value(b));
            if (descending) byValue = -byValue;
            return byValue != 0 ? byValue : a.UnitNo.CompareTo(b.UnitNo);
        }

        /// <summary>enum から作るので、役割を足せばドロップダウンの選択肢にも自動で並ぶ</summary>
        private static ZukanFilter[] BuildRoles()
        {
            var filters = new List<ZukanFilter> { new ZukanFilter(AllLabel, _ => true) };
            foreach (UnitRole role in Enum.GetValues(typeof(UnitRole)))
            {
                filters.Add(new ZukanFilter(UnitLabels.Role(role), s => s.Role == role));
            }
            return filters.ToArray();
        }

        /// <summary>enum から作るので、能力を足せばドロップダウンの選択肢にも自動で並ぶ</summary>
        private static ZukanFilter[] BuildAbilities()
        {
            var filters = new List<ZukanFilter> { new ZukanFilter(AllLabel, _ => true) };
            foreach (UnitAbilityType type in Enum.GetValues(typeof(UnitAbilityType)))
            {
                filters.Add(new ZukanFilter(UnitLabels.Ability(type), s => s.HasAbility(type)));
            }
            // 範囲攻撃は能力ではないが「敵をまとめて叩ける」というできることなので、役割ではなく能力側に置く
            filters.Add(new ZukanFilter(UnitLabels.AttackRange(true), s => s.IsAreaAttack));
            return filters.ToArray();
        }
    }
}
