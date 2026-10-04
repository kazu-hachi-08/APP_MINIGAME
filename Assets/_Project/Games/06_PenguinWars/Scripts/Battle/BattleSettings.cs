namespace MiniGame.PenguinWars.Battle
{
    /// <summary>BattleWorld を作るときの数値。PenguinWarsBalance から詰め替えて渡す</summary>
    public class BattleSettings
    {
        public float FieldLength { get; set; } = 30f;
        public int LeftCastleHp { get; set; } = 3000;
        public int RightCastleHp { get; set; } = 3000;
        /// <summary>エンドレスでは右端が無敵の出現ゲートになる</summary>
        public bool RightCastleInvincible { get; set; }
        /// <summary>城の中心から出撃位置までの距離。城の絵の中から出てこないようにする</summary>
        public float SpawnOffset { get; set; } = 1.5f;
        /// <summary>仕様書 §4.3。スマホの処理負荷対策</summary>
        public int MaxUnitsPerSide { get; set; } = 30;
        /// <summary>働きペンギンのレベル表（仕様書 §4.2）</summary>
        public WalletTable WalletTable { get; set; } = WalletTable.CreateDefault();
        /// <summary>
        /// true なら右陣営はさかな・再生産を気にせず出撃できる。エンドレスの敵は §8 のルールで湧くので、お金で縛らない
        /// </summary>
        public bool RightSpawnsFree { get; set; }
        /// <summary>撃破報酬 = 倒した敵のコスト × この値（仕様書 §4.1・§8.2）</summary>
        public float KillRewardRate { get; set; } = 0.5f;
        /// <summary>ペンギン砲（仕様書 §4.4）</summary>
        public float CannonChargeTime { get; set; } = 40f;
        /// <summary>自城から戦場の長さのこの割合までが砲の範囲</summary>
        public float CannonRangeRatio { get; set; } = 0.6f;
        public int CannonDamage { get; set; } = 100;
        /// <summary>ノックバックで後ろに飛ばされる距離と、その間動けない秒数（仕様書 §5.4）</summary>
        public float KnockbackDistance { get; set; } = 1.5f;
        public float KnockbackDuration { get; set; } = 0.5f;
        /// <summary>「遅くする」中の移動速度の倍率（仕様書 §5.3）</summary>
        public float SlowSpeedMultiplier { get; set; } = 0.5f;
        /// <summary>城キラーの城へのダメージ倍率（仕様書 §5.3）</summary>
        public float CastleKillerMultiplier { get; set; } = 3f;
        /// <summary>対戦の制限時間（秒。仕様書 §2.2）。0 なら時間切れなし（エンドレス）</summary>
        public float TimeLimit { get; set; }
        /// <summary>能力の確率判定に使う乱数のシード。テストで固定できるようにする</summary>
        public int RandomSeed { get; set; }
        /// <summary>true なら確率の能力が毎回発動する。あそびかたのデモで、外れて何も起きない回をなくすため</summary>
        public bool AlwaysProcAbilities { get; set; }
    }
}
