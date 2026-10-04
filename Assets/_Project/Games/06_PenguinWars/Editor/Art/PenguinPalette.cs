using System.Collections.Generic;
using MiniGame.PenguinWars.Battle;
using UnityEngine;

namespace MiniGame.PenguinWars.Editor
{
    /// <summary>
    /// ドットパターンの1文字 → 色。敵味方の違いは「体の暗い部分(k)とチーム色(t/u)」の差し替えだけにする（仕様書 §7.2）。
    /// パターンを陣営ごとに描き分けずに済むため
    /// </summary>
    public static class PenguinPalette
    {
        public const char Transparent = '.';
        public const char BodyDark = 'k';
        public const char TeamAccent = 't';
        public const char TeamAccentDark = 'u';

        // 体色を陣営の色へ寄せる割合。黒い体でも青/赤が見て分かり、水色などの体色も元の色が残る程度
        private const float TeamTintAmount = 0.35f;

        private static readonly Color32 LeftTeam = new Color32(60, 120, 240, 255);
        private static readonly Color32 LeftTeamDark = new Color32(30, 60, 150, 255);
        private static readonly Color32 RightTeam = new Color32(225, 60, 60, 255);
        private static readonly Color32 RightTeamDark = new Color32(140, 30, 35, 255);

        /// <summary>陣営に関係なく同じ色の文字。Phase 9 で色が足りなければここに足す</summary>
        private static readonly Dictionary<char, Color32> FixedColors = new Dictionary<char, Color32>
        {
            ['o'] = new Color32(20, 24, 38, 255),    // 輪郭
            ['w'] = new Color32(244, 247, 251, 255), // お腹の白
            ['e'] = new Color32(10, 10, 16, 255),    // 目
            ['y'] = new Color32(245, 166, 35, 255),  // くちばし・足
            ['s'] = new Color32(200, 208, 220, 255), // 金属（明）
            ['m'] = new Color32(110, 119, 133, 255), // 金属（暗）
            ['b'] = new Color32(150, 95, 45, 255),   // 木（明）
            ['d'] = new Color32(95, 60, 28, 255),    // 木（暗）
            ['n'] = new Color32(40, 40, 46, 255),    // タイヤ・グローブのひも
            ['l'] = new Color32(170, 225, 250, 255), // 氷（明）
            ['i'] = new Color32(100, 175, 225, 255), // 氷（暗）
            ['g'] = new Color32(255, 210, 60, 255),  // 金
        };

        /// <summary>体の形とは別に指定する体色（PenguinLook.BodyColor）。"standard" は黒白のペンギン</summary>
        private static readonly Dictionary<string, Color32> BodyColors = new Dictionary<string, Color32>
        {
            ["standard"] = new Color32(20, 20, 28, 255),
            ["ice"] = new Color32(110, 190, 235, 255),
            ["gray"] = new Color32(125, 125, 135, 255),
        };

        public static bool HasBodyColor(string id)
        {
            return BodyColors.ContainsKey(id);
        }

        public static Color32 Resolve(char c, Side side, string bodyColor)
        {
            switch (c)
            {
                case BodyDark: return Color32.Lerp(BodyColors[bodyColor], Team(side), TeamTintAmount);
                case TeamAccent: return Team(side);
                case TeamAccentDark: return side == Side.Left ? LeftTeamDark : RightTeamDark;
            }

            if (FixedColors.TryGetValue(c, out Color32 color)) return color;
            Debug.LogWarning($"[PenguinPalette] 未定義の色文字 '{c}'。透明として扱います");
            return new Color32(0, 0, 0, 0);
        }

        private static Color32 Team(Side side)
        {
            return side == Side.Left ? LeftTeam : RightTeam;
        }
    }
}
