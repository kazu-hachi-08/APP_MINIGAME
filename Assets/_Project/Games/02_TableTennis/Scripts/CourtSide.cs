namespace MiniGame.TableTennis
{
    /// <summary>コートのどちら側か（得点・サーブ権の主体）</summary>
    public enum CourtSide
    {
        Player,
        Opponent
    }

    public static class CourtSideExtensions
    {
        public static CourtSide Opposite(this CourtSide side)
        {
            return side == CourtSide.Player ? CourtSide.Opponent : CourtSide.Player;
        }

        public static string ToLabel(this CourtSide side)
        {
            return side == CourtSide.Player ? "YOU" : "NPC";
        }
    }
}
