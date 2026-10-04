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
    }
}
