using UnityEngine;

namespace MiniGame.Golf
{
    /// <summary>プレイヤーの識別色（§5：モルックと同じ P1赤・P2青・P3黄・P4緑）。ボール・HUD・スコアカードで共有する</summary>
    public static class GolfPlayerColors
    {
        private static readonly Color[] Colors =
        {
            new Color(1f, 0.38f, 0.35f),
            new Color(0.4f, 0.65f, 1f),
            new Color(1f, 0.88f, 0.3f),
            new Color(0.45f, 0.9f, 0.45f),
        };

        public static Color Get(int seat)
        {
            return Colors[seat % Colors.Length];
        }

        /// <summary>Text のリッチテキスト用。「P1」などを色付きで出す</summary>
        public static string Colored(int seat, string text)
        {
            return $"<color=#{ColorUtility.ToHtmlStringRGB(Get(seat))}>{text}</color>";
        }

        public static string Name(int seat) => $"P{seat + 1}";

        /// <summary>設定画面・「○○の番」に出す人間/NPCの呼び名</summary>
        public static string TypeName(GolfPlayerType type)
        {
            switch (type)
            {
                case GolfPlayerType.NpcWeak: return "NPC よわい";
                case GolfPlayerType.NpcNormal: return "NPC ふつう";
                case GolfPlayerType.NpcStrong: return "NPC つよい";
                default: return "人間";
            }
        }
    }
}
