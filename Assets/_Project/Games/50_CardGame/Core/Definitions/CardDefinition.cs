using System.Collections.Generic;
using System.Linq;

namespace CardGame.Core.Definitions
{
    /// <summary>
    /// カードの静的定義(JSON から読み込む不変データ)。
    /// ゲーム中のカード個体は <see cref="State.CardInstance"/> / <see cref="State.BoardEntity"/>。
    /// </summary>
    public sealed class CardDefinition
    {
        public string Id { get; }
        public string Name { get; }
        public CardClass Class { get; }
        public CardType Type { get; }
        public int Cost { get; }
        /// <summary>フォロワーのみ。それ以外は 0。</summary>
        public int Attack { get; }
        /// <summary>フォロワーのみ。それ以外は 0。</summary>
        public int Health { get; }
        /// <summary>カウントダウンアミュレットの初期値。null なら通常アミュレット。</summary>
        public int? Countdown { get; }
        public IReadOnlyList<Keyword> Keywords { get; }
        public IReadOnlyList<EffectDefinition> Effects { get; }
        public string Text { get; }
        public string Flavor { get; }
        /// <summary>トークン(効果でのみ場に出る。デッキには入れられない)。</summary>
        public bool IsToken { get; }

        public CardDefinition(
            string id, string name, CardClass cardClass, CardType type, int cost,
            int attack, int health, int? countdown,
            IEnumerable<Keyword>? keywords, IEnumerable<EffectDefinition>? effects,
            string? text, string? flavor, bool isToken)
        {
            Id = id;
            Name = name;
            Class = cardClass;
            Type = type;
            Cost = cost;
            Attack = attack;
            Health = health;
            Countdown = countdown;
            Keywords = (keywords ?? Enumerable.Empty<Keyword>()).ToList();
            Effects = (effects ?? Enumerable.Empty<EffectDefinition>()).ToList();
            Text = text ?? string.Empty;
            Flavor = flavor ?? string.Empty;
            IsToken = isToken;
        }

        public bool IsFollower => Type == CardType.Follower;
        public bool IsSpell => Type == CardType.Spell;
        public bool IsAmulet => Type == CardType.Amulet;

        public bool HasKeyword(Keyword k) => Keywords.Contains(k);

        /// <summary>
        /// プレイ時(Fanfare / Spell)に選択が必要な対象種別。なければ null。
        /// 仕様: 1 枚のカードで Select* は 1 種類まで。
        /// </summary>
        public TargetKind? PlaySelectTarget
        {
            get
            {
                foreach (var e in Effects)
                {
                    if (e.Trigger != Trigger.Fanfare && e.Trigger != Trigger.Spell) continue;
                    foreach (var a in e.Actions)
                    {
                        if (a.Target.HasValue && a.Target.Value.IsSelect()) return a.Target.Value;
                    }
                }
                return null;
            }
        }

        public IEnumerable<EffectDefinition> EffectsFor(Trigger trigger)
            => Effects.Where(e => e.Trigger == trigger);

        public override string ToString() => $"{Id} {Name}";
    }

    /// <summary>トリガーとアクション列の組。</summary>
    public sealed class EffectDefinition
    {
        public Trigger Trigger { get; }
        public IReadOnlyList<ActionDefinition> Actions { get; }

        public EffectDefinition(Trigger trigger, IEnumerable<ActionDefinition> actions)
        {
            Trigger = trigger;
            Actions = actions.ToList();
        }
    }

    /// <summary>1 つのアクション。type によって使うパラメータが異なる。</summary>
    public sealed class ActionDefinition
    {
        public ActionType Type { get; }
        public TargetKind? Target { get; }
        /// <summary>Damage / Heal / GainPP の量。</summary>
        public int Amount { get; }
        /// <summary>Draw / Summon の枚数・体数。</summary>
        public int Count { get; }
        /// <summary>Summon するカード ID。</summary>
        public string? CardId { get; }
        /// <summary>Buff の攻撃力増減。</summary>
        public int AttackDelta { get; }
        /// <summary>Buff の体力増減。</summary>
        public int HealthDelta { get; }

        public ActionDefinition(ActionType type, TargetKind? target, int amount, int count,
            string? cardId, int attackDelta, int healthDelta)
        {
            Type = type;
            Target = target;
            Amount = amount;
            Count = count;
            CardId = cardId;
            AttackDelta = attackDelta;
            HealthDelta = healthDelta;
        }
    }
}
