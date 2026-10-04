namespace MiniGame.PenguinWars.Battle
{
    /// <summary>特殊能力の種類（仕様書 §5.3）</summary>
    public enum UnitAbilityType
    {
        /// <summary>ふっとばす: 攻撃が当たると確率で相手をノックバックさせる</summary>
        Knockback,
        /// <summary>止める: 攻撃が当たると確率で相手の動き・攻撃を一定時間止める</summary>
        Freeze,
        /// <summary>遅くする: 攻撃が当たると確率で相手の移動速度を一定時間遅くする</summary>
        Slow,
        /// <summary>城キラー: 城へのダメージが増える</summary>
        CastleKiller,
        /// <summary>ふんばる: ノックバックしない</summary>
        Steadfast,
        // 以下の役割キラーは末尾に足す（アセットは enum を番号で保存しているので、途中に挟むと既存キャラの能力がずれる）
        /// <summary>大型キラー: 大型へのダメージが増える</summary>
        LargeKiller,
        /// <summary>遠距離キラー: 遠距離へのダメージが増える</summary>
        RangedKiller,
        /// <summary>妨害キラー: 妨害へのダメージが増える</summary>
        DisruptorKiller,
    }

    /// <summary>1つの特殊能力とそのパラメータ。確率・秒数を使わない能力では 0 のままでよい</summary>
    public readonly struct UnitAbility
    {
        public UnitAbilityType Type { get; }
        /// <summary>発動確率（0〜1）。ふっとばす・止める・遅くするで使う</summary>
        public float Chance { get; }
        /// <summary>効果の秒数。止める・遅くするで使う</summary>
        public float Duration { get; }

        public UnitAbility(UnitAbilityType type, float chance = 0f, float duration = 0f)
        {
            Type = type;
            Chance = chance;
            Duration = duration;
        }
    }
}
