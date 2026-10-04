namespace MiniGame.PenguinWars.Battle
{
    /// <summary>城。ユニットと同じく X 座標だけを持つ攻撃対象（仕様書 §3.3）</summary>
    public class CastleState
    {
        public Side Side { get; }
        public float X { get; }
        public int MaxHp { get; }
        public int Hp { get; internal set; }
        /// <summary>エンドレスの出現ゲート。攻撃対象にならず、ユニットはここより先に進めない（仕様書 §2.1）</summary>
        public bool IsInvincible { get; }

        public bool IsDestroyed => !IsInvincible && Hp <= 0;

        public CastleState(Side side, float x, int maxHp, bool isInvincible)
        {
            Side = side;
            X = x;
            MaxHp = maxHp;
            Hp = maxHp;
            IsInvincible = isInvincible;
        }
    }
}
