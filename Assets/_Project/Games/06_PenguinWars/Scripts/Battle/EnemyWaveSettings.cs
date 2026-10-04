namespace MiniGame.PenguinWars.Battle
{
    /// <summary>エンドレスの敵の出方（仕様書 §8.1）。PenguinWarsBalance から詰め替えて渡す</summary>
    public class EnemyWaveSettings
    {
        public float LevelUpInterval { get; set; } = 30f;
        /// <summary>レベル1の出現間隔（秒）</summary>
        public float BaseSpawnInterval { get; set; } = 4f;
        /// <summary>レベルが1上がるごとに出現間隔にかける値</summary>
        public float SpawnIntervalMultiplier { get; set; } = 0.9f;
        public float MinSpawnInterval { get; set; } = 0.8f;
        /// <summary>出現キャラのコスト上限 = Base + レベル × PerLevel</summary>
        public int CostLimitBase { get; set; } = 300;
        public int CostLimitPerLevel { get; set; } = 250;
        /// <summary>体力・攻撃の倍率 = Base + レベル × PerLevel</summary>
        public float StatMultiplierBase { get; set; } = 1f;
        public float StatMultiplierPerLevel { get; set; } = 0.15f;
        /// <summary>このレベルの倍数に上がった瞬間、大型を1体確定で出す</summary>
        public int BossLevelInterval { get; set; } = 5;
    }
}
