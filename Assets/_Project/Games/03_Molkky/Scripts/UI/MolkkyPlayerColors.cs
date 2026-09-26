using UnityEngine;

namespace MiniGame.Molkky
{
    /// <summary>プレイヤーの識別色（§10.1：P1赤・P2青・P3黄・P4緑）。スコア・手番表示・設定画面で共有する</summary>
    public static class MolkkyPlayerColors
    {
        private static readonly Color[] Colors =
        {
            new Color(1f, 0.38f, 0.35f),
            new Color(0.4f, 0.65f, 1f),
            new Color(1f, 0.88f, 0.3f),
            new Color(0.45f, 0.9f, 0.45f),
        };

        public static Color Get(int playerIndex)
        {
            return Colors[playerIndex % Colors.Length];
        }
    }
}
