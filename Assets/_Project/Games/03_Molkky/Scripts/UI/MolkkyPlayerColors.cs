using UnityEngine;

namespace MiniGame.Molkky
{
    /// <summary>プレイヤーの識別色（P1赤・P2青・P3黄・P4緑）。スコア・手番表示・設定画面で共有する</summary>
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

        /// <summary>
        /// 全画面演出の背景色。プレイヤー色に寄せつつ、上に乗る文字や立ち絵が読めるよう黒寄りの半透明にする
        /// </summary>
        public static Color TintedBackground(Color playerColor, float tint, float alpha)
        {
            Color background = Color.Lerp(Color.black, playerColor, tint);
            background.a = alpha;
            return background;
        }
    }
}
