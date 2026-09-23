namespace CardGame.Core.Definitions
{
    /// <summary>カードのクラス。Neutral はどのデッキにも入れられる。</summary>
    public enum CardClass
    {
        Neutral,
        Knight,
        Mage,
        Necromancer,
        Druid,
        Dragon,
        /// <summary>混沌: ドラフトで作ったデッキ専用のクラス(07-draft.md)。このクラスのカードは存在しない。</summary>
        Chaos,
    }

    /// <summary>カードの種類。</summary>
    public enum CardType
    {
        Follower,
        Spell,
        Amulet,
    }

    /// <summary>キーワード能力(Docs/50_CardGame/spec/02-card-effects.md)。</summary>
    public enum Keyword
    {
        /// <summary>守護: 相手はこのフォロワー以外を攻撃できない。</summary>
        Ward,
        /// <summary>疾走: 出たターンにリーダー・フォロワーを攻撃できる。</summary>
        Storm,
        /// <summary>突進: 出たターンにフォロワーを攻撃できる。</summary>
        Rush,
        /// <summary>必殺: ダメージを与えたフォロワーを破壊する。</summary>
        Bane,
        /// <summary>ドレイン: 与えたダメージ分、自分のリーダーを回復する。</summary>
        Drain,
    }

    /// <summary>効果の発動タイミング。</summary>
    public enum Trigger
    {
        /// <summary>手札からプレイして場に出たとき。</summary>
        Fanfare,
        /// <summary>場から破壊されたとき。</summary>
        LastWords,
        /// <summary>スペルをプレイしたとき。</summary>
        Spell,
        /// <summary>自分のターン開始時(ドロー・カウントダウンの後)。場にあるときだけ発動する。</summary>
        TurnStart,
    }

    /// <summary>効果アクションの種類。</summary>
    public enum ActionType
    {
        Damage,
        Heal,
        Draw,
        Destroy,
        Buff,
        Summon,
        GainPP,
        /// <summary>最大 PP を永続的に増やす(上限 10)。このターンの PP も同じだけ増える。</summary>
        RampPP,
    }

    /// <summary>効果の対象指定。Select* はプレイヤーが選ぶ。</summary>
    public enum TargetKind
    {
        EnemyLeader,
        AllyLeader,
        Self,
        SelectEnemyFollower,
        SelectAllyFollower,
        SelectFollower,
        SelectEnemy,
        AllEnemyFollowers,
        AllAllyFollowers,
        AllFollowers,
        RandomEnemyFollower,
        RandomAllyFollower,
    }

    public static class TargetKindExtensions
    {
        /// <summary>プレイヤーによる選択が必要な対象か。</summary>
        public static bool IsSelect(this TargetKind kind)
        {
            switch (kind)
            {
                case TargetKind.SelectEnemyFollower:
                case TargetKind.SelectAllyFollower:
                case TargetKind.SelectFollower:
                case TargetKind.SelectEnemy:
                    return true;
                default:
                    return false;
            }
        }
    }
}
