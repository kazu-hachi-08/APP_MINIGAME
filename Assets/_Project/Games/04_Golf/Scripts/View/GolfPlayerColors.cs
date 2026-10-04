using MiniGame.Common.Profile;
using UnityEngine;

namespace MiniGame.Golf
{
    /// <summary>プレイヤーの識別色（モルックと同じ P1赤・P2青・P3黄・P4緑）。ボール・HUD・スコアカードで共有する</summary>
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

        /// <summary>席の呼び名。自分の席はユーザー名、それ以外は「P2」など（SeatNames が決める）</summary>
        public static string Name(int seat) => SeatNames.Get(seat);

        /// <summary>
        /// 「たろう パワー型」「P2 パワー型」。同じキャラを複数人が選べるので、キャラ名だけだと誰か分からなくなるため席番号も付ける。
        /// separator に改行を渡すと、幅の狭いスコアカードで2行に分けられる
        /// </summary>
        public static string FullName(GolfPlayerSlot slot, string separator = " ")
        {
            if (string.IsNullOrEmpty(slot.CharacterName)) return Name(slot.Seat);
            return Name(slot.Seat) + separator + slot.CharacterName;
        }

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
