namespace MiniGame.PenguinWars.Battle
{
    /// <summary>
    /// ステージの敵の城が撃つペンギン砲（ボスステージ用のギミック）。StageDefinition.EnemyCannon にオブジェクト初期化子で書く。
    /// 撃つ判断は EnemyCannonAi
    /// </summary>
    public class EnemyCannonSettings
    {
        public float ChargeTime { get; set; } = 30f;
        /// <summary>敵の城から戦場の長さのこの割合までが範囲</summary>
        public float RangeRatio { get; set; } = 0.5f;
        public int Damage { get; set; } = 150;
        /// <summary>範囲内に味方がこの数以上いたら撃つ。1体ずつ撃ち落とすのではなく、まとめて吹き飛ばしたときに効くようにするため</summary>
        public int MinTargets { get; set; } = 3;
        /// <summary>敵の城から戦場の長さのこの割合までに味方が来たら、数に関係なく撃つ（城を叩かれているのに黙っていないように）</summary>
        public float DangerRatio { get; set; } = 0.15f;
    }
}
