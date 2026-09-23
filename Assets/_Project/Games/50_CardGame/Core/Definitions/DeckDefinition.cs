using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;

namespace CardGame.Core.Definitions
{
    /// <summary>
    /// デッキ定義(カード ID の並び。通常 40 枚、ドラフトの混沌デッキは 30 枚)。順序はゲーム開始時にシャッフルされるので意味を持たない。
    /// </summary>
    public sealed class DeckDefinition
    {
        public const int DeckSize = 40;
        /// <summary>ドラフトで作る混沌デッキの枚数(07-draft.md)。</summary>
        public const int DraftDeckSize = 30;
        public const int MaxCopies = 3;

        public string Name { get; }
        public CardClass Class { get; }
        public IReadOnlyList<string> CardIds { get; }

        /// <summary>このデッキに必要な枚数。混沌(ドラフト)は 30 枚、それ以外は 40 枚。</summary>
        public int RequiredSize => Class == CardClass.Chaos ? DraftDeckSize : DeckSize;

        public DeckDefinition(string name, CardClass cardClass, IEnumerable<string> cardIds)
        {
            Name = name;
            Class = cardClass;
            CardIds = cardIds.ToList();
        }

        /// <summary>デッキ構築ルール(01-rules.md)を満たしているか検証し、違反があればメッセージを返す。</summary>
        public IReadOnlyList<string> Validate(CardDatabase db)
        {
            var errors = new List<string>();
            if (CardIds.Count != RequiredSize)
                errors.Add($"デッキは {RequiredSize} 枚ちょうど(現在 {CardIds.Count} 枚)");
            foreach (var group in CardIds.GroupBy(id => id))
            {
                if (!db.TryGet(group.Key, out var def))
                {
                    errors.Add($"未定義のカード ID: {group.Key}");
                    continue;
                }
                if (group.Count() > MaxCopies)
                    errors.Add($"{def.Name} は {MaxCopies} 枚まで(現在 {group.Count()} 枚)");
                if (def.IsToken)
                    errors.Add($"{def.Name} はトークンなのでデッキに入れられない");
                // 混沌(ドラフト)のデッキはクラスを問わない
                if (Class != CardClass.Chaos && def.Class != CardClass.Neutral && def.Class != Class)
                    errors.Add($"{def.Name} は {def.Class} のカードなので {Class} デッキに入れられない");
            }
            return errors;
        }

        public static DeckDefinition FromJson(string json)
        {
            var dto = JsonConvert.DeserializeObject<DeckDto>(json, CardDatabase.JsonSettings)
                      ?? throw new InvalidDataException("デッキ JSON の解析に失敗しました");
            return new DeckDefinition(dto.name, dto.@class, dto.cards);
        }

        public string ToJson()
        {
            var dto = new DeckDto { name = Name, @class = Class, cards = CardIds.ToList() };
            return JsonConvert.SerializeObject(dto, Formatting.Indented, CardDatabase.JsonSettings);
        }

        /// <summary>.NET 側: 埋め込まれた Data/decks/*.json を全て読み込む。</summary>
        public static IReadOnlyList<DeckDefinition> LoadEmbedded()
        {
            var asm = typeof(DeckDefinition).GetTypeInfo().Assembly;
            var list = new List<DeckDefinition>();
            var names = asm.GetManifestResourceNames()
                .Where(n => CardDatabase.NormalizeResourceName(n).StartsWith("decks/", StringComparison.Ordinal) && n.EndsWith(".json", StringComparison.Ordinal))
                .OrderBy(n => n, StringComparer.Ordinal);
            foreach (var name in names)
            {
                using var stream = asm.GetManifestResourceStream(name)!;
                using var reader = new StreamReader(stream);
                list.Add(FromJson(reader.ReadToEnd()));
            }
            return list.OrderBy(d => d.Class).ToList();
        }

        private sealed class DeckDto
        {
#pragma warning disable CS0649, CS8618
            [JsonProperty("name", Required = Required.Always)] public string name;
            [JsonProperty("class", Required = Required.Always)] public CardClass @class;
            [JsonProperty("cards", Required = Required.Always)] public List<string> cards;
#pragma warning restore CS0649, CS8618
        }
    }
}
