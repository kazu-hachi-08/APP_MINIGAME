using UnityEngine;

namespace MiniGame.LifeGame
{
    /// <summary>席の色と演出の文字の色。マスの色はテーマごとに変わるので LifeThemeData が持つ</summary>
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

        // コマの上に浮かべる文字の色（BoardEffects）。どのテーマの背景でも読めるよう明るめにする
        public static readonly Color Gain = new Color(0.45f, 1f, 0.45f);
        public static readonly Color Loss = new Color(1f, 0.45f, 0.4f);
        public static readonly Color Celebration = new Color(1f, 0.85f, 0.2f);
        public static readonly Color Family = new Color(1f, 0.6f, 0.8f);
        public static readonly Color Info = new Color(0.55f, 0.8f, 1f);
    }
}
