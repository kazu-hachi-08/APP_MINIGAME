namespace MiniGame.PenguinWars.Battle
{
    /// <summary>
    /// 計算式の値に個別にかける倍率（1 = 式のまま）。「にんじゃは速い」「スナイパーは射程最長」のような個性をここで足す
    /// </summary>
    public class StatTweak
    {
        public float Hp { get; set; } = 1f;
        public float Attack { get; set; } = 1f;
        public float Range { get; set; } = 1f;
        public float Speed { get; set; } = 1f;
        public float Cooldown { get; set; } = 1f;
    }
}
