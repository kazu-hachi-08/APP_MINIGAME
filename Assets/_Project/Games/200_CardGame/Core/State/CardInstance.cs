using CardGame.Core.Definitions;

namespace CardGame.Core.State
{
    /// <summary>
    /// デッキ・手札にあるカードの個体。場に出ると <see cref="BoardEntity"/> になる。
    /// </summary>
    public sealed class CardInstance
    {
        /// <summary>ゲーム内で一意な ID。場に出ても引き継ぐ。</summary>
        public int InstanceId { get; }
        public CardDefinition Definition { get; }

        public CardInstance(int instanceId, CardDefinition definition)
        {
            InstanceId = instanceId;
            Definition = definition;
        }

        public string CardId => Definition.Id;
        public int Cost => Definition.Cost;

        public override string ToString() => $"#{InstanceId} {Definition.Name}";
    }

    /// <summary>
    /// 場に出ているフォロワーまたはアミュレットの個体。
    /// </summary>
    public sealed class BoardEntity
    {
        public int InstanceId { get; }
        public CardDefinition Definition { get; }
        public int Owner { get; }

        public int Attack { get; set; }
        public int Health { get; set; }
        public int MaxHealth { get; set; }
        /// <summary>カウントダウンアミュレットの残り。通常アミュレット/フォロワーは null。</summary>
        public int? Countdown { get; set; }

        /// <summary>このターンに場に出た(召喚酔い)。</summary>
        public bool SummonedThisTurn { get; set; }
        /// <summary>このターンに既に攻撃した。</summary>
        public bool HasAttackedThisTurn { get; set; }
        /// <summary>効果(破壊・必殺)により次の死亡チェックで破壊される。</summary>
        public bool PendingDestroy { get; set; }

        public BoardEntity(int instanceId, CardDefinition definition, int owner)
        {
            InstanceId = instanceId;
            Definition = definition;
            Owner = owner;
            Attack = definition.Attack;
            Health = definition.Health;
            MaxHealth = definition.Health;
            Countdown = definition.Countdown;
            SummonedThisTurn = true;
        }

        private BoardEntity(BoardEntity src)
        {
            InstanceId = src.InstanceId;
            Definition = src.Definition;
            Owner = src.Owner;
            Attack = src.Attack;
            Health = src.Health;
            MaxHealth = src.MaxHealth;
            Countdown = src.Countdown;
            SummonedThisTurn = src.SummonedThisTurn;
            HasAttackedThisTurn = src.HasAttackedThisTurn;
            PendingDestroy = src.PendingDestroy;
        }

        public BoardEntity Clone() => new BoardEntity(this);

        public bool IsFollower => Definition.IsFollower;
        public bool IsAmulet => Definition.IsAmulet;
        public bool HasKeyword(Keyword k) => Definition.HasKeyword(k);
        public bool IsDead => PendingDestroy || (IsFollower ? Health <= 0 : (Countdown.HasValue && Countdown.Value <= 0));

        public override string ToString() => IsFollower
            ? $"#{InstanceId} {Definition.Name} {Attack}/{Health}"
            : $"#{InstanceId} {Definition.Name}" + (Countdown.HasValue ? $" CD{Countdown}" : "");
    }
}
