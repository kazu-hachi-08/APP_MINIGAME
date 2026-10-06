namespace MiniGame.PenguinWars.Battle
{
    /// <summary>城。ユニットと同じく X 座標だけを持つ攻撃対象（仕様書 §3.3）</summary>
    public class CastleState
    {
        public Side Side { get; }
        public float X { get; }
        public int MaxHp { get; }
        public int Hp { get; internal set; }
        public bool IsDestroyed => Hp <= 0;

        public CastleState(Side side, float x, int maxHp)
        {
            Side = side;
            X = x;
            MaxHp = maxHp;
            Hp = maxHp;
        }
    }
}
