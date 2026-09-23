#nullable enable
using System.Collections.Generic;
using System.Linq;
using CardGame.Core.Definitions;

namespace CardGame.Unity.Battle
{
    /// <summary>
    /// キーワード・トリガーの説明文(docs/spec/02-card-effects.md と一致させる)。
    /// 拡大表示とツールチップで使う。
    /// </summary>
    public static class KeywordHelp
    {
        public static string Describe(Keyword k) => k switch
        {
            Keyword.Ward => "相手はこのフォロワー以外(リーダー含む)を攻撃できない。",
            Keyword.Storm => "場に出たターンにリーダー・フォロワーを攻撃できる。",
            Keyword.Rush => "場に出たターンにフォロワーを攻撃できる(リーダーは不可)。",
            Keyword.Bane => "戦闘でダメージを与えたフォロワーを破壊する。",
            Keyword.Drain => "戦闘で与えたダメージ分、自分のリーダーを回復する。",
            _ => "",
        };

        public static string Describe(Trigger t) => t switch
        {
            Trigger.Fanfare => "手札からプレイして場に出たときに発動する(効果で出た場合は発動しない)。",
            Trigger.LastWords => "場から破壊されたときに発動する。",
            Trigger.TurnStart => "自分のターン開始時(ドローの後)に発動する。",
            _ => "",
        };

        public static string TriggerName(Trigger t) => t switch
        {
            Trigger.Fanfare => "ファンファーレ",
            Trigger.LastWords => "ラストワード",
            Trigger.TurnStart => "ターン開始時",
            _ => "",
        };

        public const string CountdownName = "カウントダウン";
        public const string CountdownText = "自分のターン開始時に 1 減り、0 になると破壊される(ラストワードが発動)。";

        /// <summary>カードが持つキーワード・トリガーの (名前, 説明) 一覧。無ければ空。</summary>
        public static List<(string name, string text)> ForCard(CardDefinition def)
        {
            var list = new List<(string, string)>();
            foreach (var k in def.Keywords)
                list.Add((CardView.KeywordLabel(k), Describe(k)));
            foreach (var t in def.Effects.Select(e => e.Trigger).Distinct())
                if (t != Trigger.Spell) list.Add((TriggerName(t), Describe(t)));
            if (def.Countdown.HasValue)
                list.Add((CountdownName, CountdownText));
            return list;
        }

        /// <summary>リッチテキスト 1 本にまとめる。</summary>
        public static string ToRichText(CardDefinition def)
        {
            var items = ForCard(def);
            if (items.Count == 0) return "";
            return string.Join("\n\n", items.Select(i => $"<color=#F8C040><b>{i.name}</b></color>\n{i.text}"));
        }
    }
}
