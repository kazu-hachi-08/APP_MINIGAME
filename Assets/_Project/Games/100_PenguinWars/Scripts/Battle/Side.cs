namespace MiniGame.PenguinWars.Battle
{
    /// <summary>陣営。Left は X=0 の城から右へ、Right は X=fieldLength の城から左へ進む</summary>
    public enum Side
    {
        Left,
        Right,
    }

    public static class SideExtensions
    {
        /// <summary>前進方向の X の符号。陣営ごとの if を戦闘ロジックのあちこちに書かずに済むようにする</summary>
        public static int Forward(this Side side)
        {
            return side == Side.Left ? 1 : -1;
        }

        public static Side Opponent(this Side side)
        {
            return side == Side.Left ? Side.Right : Side.Left;
        }
    }
}
