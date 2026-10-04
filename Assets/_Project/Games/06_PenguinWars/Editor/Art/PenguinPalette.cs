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

        /// <summary>陣営に関係なく同じ色の文字。色が足りなければここに足す（数字は虹色の帯）</summary>
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
            ['c'] = new Color32(215, 170, 110, 255), // 段ボール（明）
            ['h'] = new Color32(165, 120, 70, 255),  // 段ボール（暗）
            ['r'] = new Color32(220, 50, 55, 255),   // 赤（風船・タコ・キングのマント）
            ['x'] = new Color32(140, 25, 35, 255),   // 赤（暗）
            ['p'] = new Color32(250, 150, 190, 255), // ピンク（布団・リボン）
            ['v'] = new Color32(135, 75, 195, 255),  // 紫（魔法使いの帽子）
            ['f'] = new Color32(50, 90, 200, 255),   // 青いマント（明）
            ['j'] = new Color32(30, 55, 140, 255),   // 青いマント（暗）
            ['a'] = new Color32(70, 110, 165, 255),  // クジラ
            ['q'] = new Color32(105, 125, 65, 255),  // 戦車（明）
            ['z'] = new Color32(65, 80, 40, 255),    // 戦車（暗）
            ['1'] = new Color32(235, 70, 80, 255),   // 虹: 赤
            ['2'] = new Color32(250, 150, 50, 255),  // 虹: だいだい（ロケットの炎にも使う）
            ['3'] = new Color32(250, 225, 70, 255),  // 虹: 黄
            ['4'] = new Color32(90, 205, 110, 255),  // 虹: 緑
            ['5'] = new Color32(80, 160, 240, 255),  // 虹: 青
            ['6'] = new Color32(160, 100, 230, 255), // 虹: 紫
        };

        /// <summary>体の形とは別に指定する体色（PenguinLook.BodyColor）。"standard" は黒白のペンギン</summary>
        private static readonly Dictionary<string, Color32> BodyColors = new Dictionary<string, Color32>
        {
            ["standard"] = new Color32(20, 20, 28, 255),
            ["ice"] = new Color32(110, 190, 235, 255),
            ["gray"] = new Color32(125, 125, 135, 255),
            ["aurora"] = new Color32(150, 90, 215, 255),
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
