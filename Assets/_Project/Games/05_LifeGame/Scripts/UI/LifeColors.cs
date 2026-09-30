using UnityEngine;

namespace MiniGame.LifeGame
{
    /// <summary>席の色。マスの色はテーマごとに変わるので LifeThemeData が持つ</summary>
    public static class LifeColors
    {
        private static readonly Color[] SeatColors =
        {
            new Color(0.95f, 0.3f, 0.3f),
            new Color(0.3f, 0.55f, 1f),
            new Color(0.3f, 0.8f, 0.35f),
            new Color(1f, 0.8f, 0.2f),
        };

        public static Color Seat(int seat) => SeatColors[seat % SeatColors.Length];
    }
}
