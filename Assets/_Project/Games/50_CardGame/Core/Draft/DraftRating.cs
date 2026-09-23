using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;
using CardGame.Core.Definitions;
using CardGame.Core.State;

namespace CardGame.Core.Draft
{
    /// <summary>
    /// AI のドラフト(07-draft.md「AI のドラフト」)。カードの評価点の合計が最も高い組を取る。
    /// 評価点は評価表(Data/Resources/draft/ratings.json)を使う。表に無いカードは仮の式で代用する。
    /// 評価表は `CardGame.Cli draft --rate-out` で作り直す(カードを変えたら更新する)。
    /// </summary>
    public static class DraftRating
    {
        /// <summary>効果 1 つあたりの加点。</summary>
        public const double EffectValue = 1.5;
        /// <summary>スペル・アミュレットの一律の点。</summary>
        public const double NonFollowerValue = 1.5;

        private static Dictionary<string, double> _table = LoadEmbeddedTable();

        /// <summary>評価表を JSON から読み込む(Unity は Resources から渡す)。</summary>
        public static void LoadTable(string json)
        {
            var ratings = (JObject?)JObject.Parse(json)["ratings"] ?? throw new InvalidDataException("ratings が無い");
            _table = ratings.Properties().ToDictionary(p => p.Name, p => (double)p.Value, StringComparer.Ordinal);
        }

        /// <summary>評価表が読み込まれているか。</summary>
        public static bool HasTable => _table.Count > 0;

        private static Dictionary<string, double> LoadEmbeddedTable()
        {
            var asm = typeof(DraftRating).GetTypeInfo().Assembly;
            var name = asm.GetManifestResourceNames().FirstOrDefault(n => CardDatabase.NormalizeResourceName(n) == "draft/ratings.json");
            if (name == null) return new Dictionary<string, double>(StringComparer.Ordinal);
            using var reader = new StreamReader(asm.GetManifestResourceStream(name)!);
            var ratings = (JObject?)JObject.Parse(reader.ReadToEnd())["ratings"];
            return ratings?.Properties().ToDictionary(p => p.Name, p => (double)p.Value, StringComparer.Ordinal)
                   ?? new Dictionary<string, double>(StringComparer.Ordinal);
        }

        /// <summary>カード 1 枚の評価点(評価表の値。表に無ければ仮の式)。</summary>
        public static double Rate(CardDefinition c) => _table.TryGetValue(c.Id, out var v) ? v : RateByFormula(c);

        /// <summary>仮の式: スタッツの余剰 + キーワード + 効果の数。</summary>
        public static double RateByFormula(CardDefinition c)
        {
            if (!c.IsFollower) return NonFollowerValue;
            double v = c.Attack + c.Health - StandardStats(c.Cost);
            foreach (var k in c.Keywords)
                v += k == Keyword.Rush || k == Keyword.Drain ? 0.5 : 1.0;
            v += c.Effects.Count * EffectValue;
            return v;
        }

        /// <summary>標準スタッツの合計(03-cards.md の表: コスト 1 = 3、以降コスト × 2)。</summary>
        public static int StandardStats(int cost) => cost <= 0 ? 1 : cost == 1 ? 3 : cost * 2;

        /// <summary>組の番号を選ぶ(同点は乱数)。</summary>
        public static int ChooseBundle(IReadOnlyList<IReadOnlyList<CardDefinition>> offer, DeterministicRandom rng)
        {
            int best = 0, ties = 0;
            double bestScore = double.NegativeInfinity;
            for (int i = 0; i < offer.Count; i++)
            {
                double s = offer[i].Sum(Rate);
                if (s > bestScore + 1e-9) { bestScore = s; best = i; ties = 1; }
                else if (s > bestScore - 1e-9 && rng.Next(++ties) == 0) best = i;
            }
            return best;
        }

        /// <summary>AI が最初から最後まで自動でドラフトしたデッキ。</summary>
        public static DeckDefinition AutoDraft(CardDatabase db, ulong seed, string name = "AI のドラフトデッキ")
        {
            var session = new DraftSession(db, seed);
            var rng = new DeterministicRandom(seed ^ 0x5EED);
            while (!session.IsComplete) session.Pick(ChooseBundle(session.CurrentOffer, rng));
            return session.ToDeck(name);
        }
    }
}
