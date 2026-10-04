namespace MiniGame.PenguinWars.Battle
{
    /// <summary>
    /// 1キャラ分の数値（仕様書 §5.1）。ScriptableObject から変換して渡すことで、戦闘ロジックとテストを Unity のアセットから切り離す
    /// </summary>
    public class UnitStats
    {
        public int UnitNo { get; set; }
        public int Cost { get; set; }
        public float Cooldown { get; set; }
        public int MaxHp { get; set; }
        public int Attack { get; set; }
        public float Range { get; set; }
        public float AttackInterval { get; set; }
        public float Windup { get; set; }
        public float MoveSpeed { get; set; }
        public bool IsAreaAttack { get; set; }
    }
}
