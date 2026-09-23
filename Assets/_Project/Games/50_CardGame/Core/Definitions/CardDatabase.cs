using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CardGame.Core.Definitions
{
    /// <summary>
    /// 全カード定義の辞書。JSON(Docs/50_CardGame/spec/02-card-effects.md の形式)から読み込む。
    /// </summary>
    public sealed class CardDatabase
    {
        private readonly Dictionary<string, CardDefinition> _cards = new Dictionary<string, CardDefinition>(StringComparer.Ordinal);

        public IReadOnlyCollection<CardDefinition> All => _cards.Values;
        public int Count => _cards.Count;

        public CardDefinition Get(string id)
        {
            if (!_cards.TryGetValue(id, out var def))
                throw new KeyNotFoundException($"カード ID '{id}' は定義されていません");
            return def;
        }

        public bool TryGet(string id, out CardDefinition def) => _cards.TryGetValue(id, out def!);
        public bool Contains(string id) => _cards.ContainsKey(id);

        /// <summary>JSON 文字列(カード配列)を読み込んで追加する。</summary>
        public void LoadJson(string json)
        {
            var dtos = JsonConvert.DeserializeObject<List<CardDto>>(json, JsonSettings)
                       ?? throw new InvalidDataException("カード JSON の解析に失敗しました");
            foreach (var dto in dtos)
            {
                var def = dto.ToDefinition();
                if (_cards.ContainsKey(def.Id))
                    throw new InvalidDataException($"カード ID '{def.Id}' が重複しています");
                _cards[def.Id] = def;
            }
        }

        /// <summary>
        /// .NET 側: アセンブリに埋め込まれた Data/cards/*.json を全て読み込む。
        /// Unity 側では TextAsset から <see cref="LoadJson"/> を呼ぶこと。
        /// </summary>
        public static CardDatabase LoadEmbedded()
        {
            var db = new CardDatabase();
            var asm = typeof(CardDatabase).GetTypeInfo().Assembly;
            var names = asm.GetManifestResourceNames()
                .Where(n => NormalizeResourceName(n).StartsWith("cards/", StringComparison.Ordinal) && n.EndsWith(".json", StringComparison.Ordinal))
                .OrderBy(n => n, StringComparer.Ordinal);
            foreach (var name in names)
            {
                using var stream = asm.GetManifestResourceStream(name)!;
                using var reader = new StreamReader(stream);
                db.LoadJson(reader.ReadToEnd());
            }
            db.Validate();
            return db;
        }

        /// <summary>埋め込みリソース名の区切りを '/' に揃える(Windows では RecursiveDir が '\' になる)。</summary>
        internal static string NormalizeResourceName(string name) => name.Replace('\\', '/');

        /// <summary>定義間の整合性(Summon 先の存在など)を検証する。</summary>
        public void Validate()
        {
            foreach (var def in _cards.Values)
            {
                if (def.IsFollower && def.Health <= 0)
                    throw new InvalidDataException($"{def}: フォロワーの体力は 1 以上");
                if (!def.IsFollower && (def.Attack != 0 || def.Health != 0))
                    throw new InvalidDataException($"{def}: フォロワー以外に攻撃力/体力は指定できない");
                if (def.Countdown.HasValue && !def.IsAmulet)
                    throw new InvalidDataException($"{def}: countdown はアミュレット専用");
                if (def.Cost < 0 || def.Cost > 10)
                    throw new InvalidDataException($"{def}: コストは 0〜10");

                int selectKinds = def.Effects
                    .Where(e => e.Trigger == Trigger.Fanfare || e.Trigger == Trigger.Spell)
                    .SelectMany(e => e.Actions)
                    .Where(a => a.Target.HasValue && a.Target.Value.IsSelect())
                    .Select(a => a.Target!.Value)
                    .Distinct()
                    .Count();
                if (selectKinds > 1)
                    throw new InvalidDataException($"{def}: Select* 対象は 1 種類まで");

                foreach (var e in def.Effects)
                {
                    if (def.IsSpell && e.Trigger != Trigger.Spell)
                        throw new InvalidDataException($"{def}: スペルのトリガーは Spell のみ");
                    if (!def.IsSpell && e.Trigger == Trigger.Spell)
                        throw new InvalidDataException($"{def}: Spell トリガーはスペル専用");
                    if (e.Trigger == Trigger.TurnStart && e.Actions.Any(a => a.Target.HasValue && a.Target.Value.IsSelect()))
                        throw new InvalidDataException($"{def}: TurnStart の効果は Select* 対象を使えない");
                    foreach (var a in e.Actions)
                    {
                        if (a.Type == ActionType.Summon)
                        {
                            if (a.CardId == null || !_cards.TryGetValue(a.CardId, out var target) || !target.IsFollower)
                                throw new InvalidDataException($"{def}: Summon 先 '{a.CardId}' はフォロワーである必要がある");
                        }
                        bool needsTarget = a.Type == ActionType.Damage || a.Type == ActionType.Heal
                                           || a.Type == ActionType.Destroy || a.Type == ActionType.Buff;
                        if (needsTarget && !a.Target.HasValue)
                            throw new InvalidDataException($"{def}: {a.Type} には target が必要");
                        if (a.Target == TargetKind.Self && def.IsSpell)
                            throw new InvalidDataException($"{def}: スペルは Self を使えない");
                        if (a.Target == TargetKind.RandomAllyFollower && def.IsSpell)
                        {
                            // スペルは場にいないので Self 除外の意味はないが、動作は許容する
                        }
                    }
                }
            }
        }

        internal static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            Converters = { new StringEnumConverter() },
            MissingMemberHandling = MissingMemberHandling.Error,
        };

        // ---- JSON DTO ----

        private sealed class CardDto
        {
#pragma warning disable CS0649, CS8618
            [JsonProperty("id", Required = Required.Always)] public string id;
            [JsonProperty("name", Required = Required.Always)] public string name;
            [JsonProperty("class", Required = Required.Always)] public CardClass @class;
            [JsonProperty("type", Required = Required.Always)] public CardType type;
            [JsonProperty("cost", Required = Required.Always)] public int cost;
            [JsonProperty("attack")] public int attack;
            [JsonProperty("health")] public int health;
            [JsonProperty("countdown")] public int? countdown;
            [JsonProperty("keywords")] public List<Keyword>? keywords;
            [JsonProperty("effects")] public List<EffectDto>? effects;
            [JsonProperty("text")] public string? text;
            [JsonProperty("flavor")] public string? flavor;
            [JsonProperty("token")] public bool token;
#pragma warning restore CS0649, CS8618

            public CardDefinition ToDefinition()
            {
                return new CardDefinition(id, name, @class, type, cost, attack, health, countdown,
                    keywords, effects?.Select(e => e.ToDefinition()), text, flavor, token);
            }
        }

        private sealed class EffectDto
        {
#pragma warning disable CS0649, CS8618
            [JsonProperty("trigger", Required = Required.Always)] public Trigger trigger;
            [JsonProperty("actions", Required = Required.Always)] public List<ActionDto> actions;
#pragma warning restore CS0649, CS8618

            public EffectDefinition ToDefinition() => new EffectDefinition(trigger, actions.Select(a => a.ToDefinition()));
        }

        private sealed class ActionDto
        {
#pragma warning disable CS0649
            [JsonProperty("type", Required = Required.Always)] public ActionType type;
            [JsonProperty("target")] public TargetKind? target;
            [JsonProperty("amount")] public int amount;
            [JsonProperty("count")] public int count = 1;
            [JsonProperty("cardId")] public string? cardId;
            [JsonProperty("attack")] public int attack;
            [JsonProperty("health")] public int health;
#pragma warning restore CS0649

            public ActionDefinition ToDefinition() => new ActionDefinition(type, target, amount, count, cardId, attack, health);
        }
    }
}
