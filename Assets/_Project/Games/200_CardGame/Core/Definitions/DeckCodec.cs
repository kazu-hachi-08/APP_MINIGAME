using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CardGame.Core.Definitions
{
    /// <summary>
    /// オンラインでデッキを送るための文字列化(05-online.md「デッキの送り方」)。
    /// プリセットは名前だけ、相手の端末に無いデッキ(ドラフトなど)は <c>deck:クラス|名前|ID,ID,…</c> で中身ごと送る。
    /// </summary>
    public static class DeckCodec
    {
        private const string Prefix = "deck:";

        /// <summary>中身ごとの文字列にする。</summary>
        public static string Encode(DeckDefinition deck)
        {
            if (deck.Name.Contains('|')) throw new ArgumentException("デッキ名に | は使えない");
            return $"{Prefix}{deck.Class}|{deck.Name}|{string.Join(",", deck.CardIds)}";
        }

        /// <summary>中身ごとの文字列か(false ならプリセットの名前)。</summary>
        public static bool IsEncoded(string s) => s.StartsWith(Prefix, StringComparison.Ordinal);

        /// <summary>
        /// 文字列からデッキを復元し、デッキ構築ルールで検証する。
        /// 名前だけのときは <paramref name="presets"/> から探す。見つからない・不正なら例外。
        /// </summary>
        public static DeckDefinition Decode(string s, CardDatabase db, IEnumerable<DeckDefinition> presets)
        {
            DeckDefinition deck;
            if (IsEncoded(s))
            {
                var parts = s.Substring(Prefix.Length).Split('|');
                if (parts.Length != 3 || !Enum.TryParse<CardClass>(parts[0], out var cls))
                    throw new InvalidDataException($"デッキの形式が不正: {s}");
                var ids = parts[2].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                deck = new DeckDefinition(parts[1], cls, ids);
            }
            else
            {
                deck = presets.FirstOrDefault(d => d.Name == s) ?? throw new InvalidDataException($"デッキ '{s}' が見つからない");
            }
            var errors = deck.Validate(db);
            if (errors.Count > 0) throw new InvalidDataException($"デッキ '{deck.Name}' が不正: {string.Join(" / ", errors)}");
            return deck;
        }
    }
}
