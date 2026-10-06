namespace MiniGame.PenguinWars.Battle
{
    /// <summary>
    /// ステージの敵の出方1行。「条件を満たしてから StartTime 秒後に1体、その後 Interval 秒ごとに Count 体まで」出す。
    /// 定義表（StageDefinitions）を1行1種類で書けるよう、オブジェクト初期化子で埋める
    /// </summary>
    public class EnemySpawnEntry
    {
        public int UnitNo { get; set; }
        /// <summary>条件を満たしてから最初の1体が出るまでの秒数</summary>
        public float StartTime { get; set; }
        public float Interval { get; set; }
        /// <summary>出す数。0 なら出し続ける</summary>
        public int Count { get; set; }
        /// <summary>体力・攻撃の倍率。同じキャラでもステージの後半ほど強くできるように</summary>
        public float StatMultiplier { get; set; } = 1f;
        /// <summary>敵城HPの割合がこの値以下になったら始まる。1 なら最初から（にゃんこの「城を叩くと出てくる」）</summary>
        public float TriggerCastleHpRatio { get; set; } = 1f;
        /// <summary>true なら最初の1体が出たときに BossAppeared を出す。場の上限で捨てない</summary>
        public bool IsBoss { get; set; }
    }
}
