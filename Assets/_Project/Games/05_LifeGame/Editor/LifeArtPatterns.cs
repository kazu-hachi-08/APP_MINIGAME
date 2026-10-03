using System.Collections.Generic;
using UnityEngine;

namespace MiniGame.LifeGame.Editor
{
    /// <summary>
    /// マスのアイコンとコマの乗り物を1文字＝1ピクセルの文字列で持つ（LifeGameArtGenerator が PNG にする）。
    /// 絵を直したいときは文字を書き換えて Tools > MiniGame > LifeGame > Regenerate Art を実行する。
    /// </summary>
    internal static class LifeArtPatterns
    {
        public const char Empty = '.';

        /// <summary>乗り物で「席の色に染める車体」に回す文字。灰色の濃さで陰影を付け、実行時に席の色を掛ける</summary>
        public static readonly Dictionary<char, Color32> BodyPalette = new Dictionary<char, Color32>
        {
            { 'w', new Color32(255, 255, 255, 255) },
            { 's', new Color32(205, 205, 205, 255) },
            { 'd', new Color32(105, 105, 110, 255) },
        };

        /// <summary>染めずにそのまま出す色（アイコン全部と、乗り物の窓・車輪・馬など）</summary>
        public static readonly Dictionary<char, Color32> FixedPalette = new Dictionary<char, Color32>
        {
            { 'k', new Color32(40, 34, 44, 255) },
            { 'w', new Color32(255, 255, 255, 255) },
            { 's', new Color32(165, 165, 175, 255) },
            { 'y', new Color32(250, 205, 60, 255) },
            { 'Y', new Color32(205, 145, 35, 255) },
            { 'r', new Color32(225, 65, 65, 255) },
            { 'g', new Color32(70, 175, 85, 255) },
            { 'b', new Color32(70, 125, 225, 255) },
            { 'c', new Color32(165, 215, 250, 255) },
            { 'p', new Color32(245, 130, 170, 255) },
            { 'n', new Color32(140, 92, 52, 255) },
            { 'o', new Color32(250, 140, 40, 255) },
        };

        public static readonly Dictionary<LifeCellType, string[]> Icons = new Dictionary<LifeCellType, string[]>
        {
            {
                LifeCellType.Start, new[]
                {
                    "...kk.......",
                    "...krrrrk...",
                    "...krrrrrrk.",
                    "...krrrrrrk.",
                    "...krrrrrk..",
                    "...kkkkkk...",
                    "...k........",
                    "...k........",
                    "...k........",
                    "...k........",
                    "..kkk.......",
                    ".kkkkk......",
                }
            },
            {
                LifeCellType.Branch, new[]
                {
                    "kkk......kkk",
                    "kk........kk",
                    "k.k......k.k",
                    "...k....k...",
                    "....k..k....",
                    ".....kk.....",
                    ".....kk.....",
                    ".....kk.....",
                    ".....kk.....",
                    ".....kk.....",
                    ".....kk.....",
                    ".....kk.....",
                }
            },
            {
                LifeCellType.JobOffer, new[]
                {
                    "............",
                    "....kkkk....",
                    "....k..k....",
                    ".kkkkkkkkkk.",
                    ".knnnnnnnnk.",
                    ".knnnnnnnnk.",
                    ".kkkkyykkkk.",
                    ".knnnyynnnk.",
                    ".knnnnnnnnk.",
                    ".knnnnnnnnk.",
                    ".kkkkkkkkkk.",
                    "............",
                }
            },
            {
                LifeCellType.Graduation, new[]
                {
                    "............",
                    ".....kk.....",
                    "...kkkkkk...",
                    ".kkkkkkkkkk.",
                    "kkkkkkkkkkkk",
                    ".kkkkkkkkkky",
                    "...kkkkkk..y",
                    "...kkkkkk..y",
                    "...kkkkkk.yy",
                    "....kkkk..yy",
                    "............",
                    "............",
                }
            },
            {
                LifeCellType.Payday, new[]
                {
                    "....kkkk....",
                    ".....kk.....",
                    "....kyyk....",
                    "...kyyyyk...",
                    "..kyyyyyyk..",
                    ".kyyykkyyyk.",
                    ".kyykyyyyyk.",
                    ".kyyykkyyyk.",
                    ".kyyyyykyyk.",
                    ".kyyykkyyyk.",
                    "..kyyyyyyk..",
                    "...kkkkkk...",
                }
            },
            {
                LifeCellType.Marriage, new[]
                {
                    "............",
                    "..kkk..kkk..",
                    ".krrrkkrrrk.",
                    "krwrrrrrrrrk",
                    "krwrrrrrrrrk",
                    "krrrrrrrrrrk",
                    ".krrrrrrrrk.",
                    "..krrrrrrk..",
                    "...krrrrk...",
                    "....krrk....",
                    ".....kk.....",
                    "............",
                }
            },
            {
                LifeCellType.Tuition, new[]
                {
                    "............",
                    ".kkkkkkkkkk.",
                    ".kbbbbbbbbk.",
                    ".kbwwwwwwbk.",
                    ".kbbbbbbbbk.",
                    ".kbbbbbbbbk.",
                    ".kbbbbbbbbk.",
                    ".kbbbbbbbbk.",
                    ".kbbbbbbbbk.",
                    ".kwwwwwwwwk.",
                    ".kkkkkkkkkk.",
                    "............",
                }
            },
            {
                LifeCellType.Goal, new[]
                {
                    "..kkkkkkkk..",
                    "kkkyyyyyykkk",
                    "k.kyyyyyyk.k",
                    "k.kyyyyyyk.k",
                    ".kkyyyyyykk.",
                    "...kyyyyk...",
                    "....kyyk....",
                    ".....kk.....",
                    "....kyyk....",
                    "...kYYYYk...",
                    "...kYYYYk...",
                    "...kkkkkk...",
                }
            },
            {
                LifeCellType.Income, new[]
                {
                    "....kkkk....",
                    "..kkyyyykk..",
                    ".kyyyyyyyyk.",
                    ".kyyykkyyYk.",
                    "kyyyyykyyyYk",
                    "kyyyyykyyyYk",
                    "kyyyyykyyyYk",
                    "kyyyyykyyyYk",
                    ".kyyykkkyYk.",
                    ".kyyyyyyYYk.",
                    "..kkYYYYkk..",
                    "....kkkk....",
                }
            },
            {
                LifeCellType.Expense, new[]
                {
                    "....kkkk....",
                    "...k....k...",
                    "...k....k...",
                    ".kkkkkkkkkk.",
                    ".krrrrrrrrk.",
                    ".krrrrrrrrk.",
                    ".krrrrrrrrk.",
                    ".krrwwwwrrk.",
                    ".krrrrrrrrk.",
                    ".krrrrrrrrk.",
                    ".krrrrrrrrk.",
                    ".kkkkkkkkkk.",
                }
            },
            {
                LifeCellType.Sickness, new[]
                {
                    "............",
                    "............",
                    "............",
                    "..kkkkkkkk..",
                    ".krrrrkwwwk.",
                    "krwrrrkwwwwk",
                    "krrrrrkwwwwk",
                    ".krrrrkwwwk.",
                    "..kkkkkkkk..",
                    "............",
                    "............",
                    "............",
                }
            },
            {
                LifeCellType.Accident, new[]
                {
                    ".....kk.....",
                    "....kyyk....",
                    "....kyyk....",
                    "...kykkyk...",
                    "..kyykkyyk..",
                    "..kyykkyyk..",
                    ".kyyykkyyyk.",
                    ".kyyyyyyyyk.",
                    "kyyyykkyyyyk",
                    "kyyyyyyyyyyk",
                    "kkkkkkkkkkkk",
                    "............",
                }
            },
            {
                LifeCellType.Fire, new[]
                {
                    ".....kk.....",
                    ".....kok....",
                    "....kook....",
                    "...kooook...",
                    "...kooyok...",
                    "..kooyyook..",
                    "..koyyyyok..",
                    ".kooyyyyook.",
                    ".koyyyyyyok.",
                    ".koyyyyyyok.",
                    "..kooyyook..",
                    "...kkkkkk...",
                }
            },
            {
                LifeCellType.Birth, new[]
                {
                    ".....kk.....",
                    "....kppk....",
                    "....kppk....",
                    "...kkkkkk...",
                    "...kcccck...",
                    "...kcccck...",
                    "...kwwwwk...",
                    "...kwwwwk...",
                    "...kwwwwk...",
                    "...kwwwwk...",
                    "...kwwwwk...",
                    "...kkkkkk...",
                }
            },
            {
                LifeCellType.House, new[]
                {
                    ".....kk.....",
                    "....krrk....",
                    "...krrrrk...",
                    "..krrrrrrk..",
                    ".krrrrrrrrk.",
                    "kkkkkkkkkkkk",
                    ".kwwwwwwwwk.",
                    ".kwbbwwnnwk.",
                    ".kwbbwwnnwk.",
                    ".kwwwwwnnwk.",
                    ".kwwwwwnnwk.",
                    ".kkkkkkkkkk.",
                }
            },
            {
                LifeCellType.Insurance, new[]
                {
                    "kkkkkkkkkkkk",
                    "kbbbbbbbbbbk",
                    "kbbbbwwbbbbk",
                    "kbbbbwwbbbbk",
                    "kbbwwwwwwbbk",
                    "kbbwwwwwwbbk",
                    "kbbbbwwbbbbk",
                    ".kbbbwwbbbk.",
                    ".kbbbbbbbbk.",
                    "..kbbbbbbk..",
                    "...kbbbbk...",
                    "....kkkk....",
                }
            },
            {
                LifeCellType.Stock, new[]
                {
                    ".........kkk",
                    ".........kgk",
                    "......kkkkgk",
                    "......kgkkgk",
                    "......kgkkgk",
                    "...kkkkgkkgk",
                    "...kgkkgkkgk",
                    "...kgkkgkkgk",
                    "kkkkgkkgkkgk",
                    "kgkkgkkgkkgk",
                    "kgkkgkkgkkgk",
                    "kkkkkkkkkkkk",
                }
            },
            {
                LifeCellType.ChangeJob, new[]
                {
                    "............",
                    "........k...",
                    "........kk..",
                    ".kkkkkkkkkk.",
                    "........kk..",
                    "........k...",
                    "...k........",
                    "..kk........",
                    ".kkkkkkkkkk.",
                    "..kk........",
                    "...k........",
                    "............",
                }
            },
            {
                LifeCellType.Bet, new[]
                {
                    "............",
                    ".kkkkkkkkkk.",
                    ".kwwwwwwwwk.",
                    ".kwkkwwwwwk.",
                    ".kwkkwwwwwk.",
                    ".kwwwkkwwwk.",
                    ".kwwwkkwwwk.",
                    ".kwwwwwkkwk.",
                    ".kwwwwwkkwk.",
                    ".kwwwwwwwwk.",
                    ".kkkkkkkkkk.",
                    "............",
                }
            },
            {
                LifeCellType.Nominate, new[]
                {
                    "....kkkk....",
                    "..kkrrrrkk..",
                    ".krrwwwwrrk.",
                    ".krwwwwwwrk.",
                    "krwwrrrrwwrk",
                    "krwwrkkrwwrk",
                    "krwwrkkrwwrk",
                    "krwwrrrrwwrk",
                    ".krwwwwwwrk.",
                    ".krrwwwwrrk.",
                    "..kkrrrrkk..",
                    "....kkkk....",
                }
            },
            {
                LifeCellType.Present, new[]
                {
                    "...kk..kk...",
                    "..kyykkyyk..",
                    "...kkyykk...",
                    "kkkkkyykkkkk",
                    "krrrryyrrrrk",
                    "kkkkkyykkkkk",
                    ".krrryyrrrk.",
                    ".krrryyrrrk.",
                    ".krrryyrrrk.",
                    ".krrryyrrrk.",
                    ".krrryyrrrk.",
                    ".kkkkkkkkkk.",
                }
            },
            {
                LifeCellType.SwapJob, new[]
                {
                    "............",
                    "kkkk........",
                    "kbbk....k...",
                    "kbbkkkkkkk..",
                    "kkkk....k...",
                    "............",
                    "............",
                    "...k....kkkk",
                    "..kkkkkkkook",
                    "...k....kook",
                    "........kkkk",
                    "............",
                }
            },
            {
                LifeCellType.Forward, new[]
                {
                    "............",
                    ".....kk.....",
                    ".....kgk....",
                    ".....kggk...",
                    "kkkkkkgggk..",
                    "kgggggggggk.",
                    "kgggggggggk.",
                    "kkkkkkgggk..",
                    ".....kggk...",
                    ".....kgk....",
                    ".....kk.....",
                    "............",
                }
            },
            {
                LifeCellType.Back, new[]
                {
                    "............",
                    ".....kk.....",
                    "....krk.....",
                    "...krrk.....",
                    "..krrrkkkkkk",
                    ".krrrrrrrrrk",
                    ".krrrrrrrrrk",
                    "..krrrkkkkkk",
                    "...krrk.....",
                    "....krk.....",
                    ".....kk.....",
                    "............",
                }
            },
            {
                LifeCellType.Rest, new[]
                {
                    "............",
                    "............",
                    "bbbbbb......",
                    "....bb......",
                    "...bb.......",
                    "..bb........",
                    ".bb...bbbb..",
                    "bbbbbb..b...",
                    ".......b....",
                    "......bbbb..",
                    "............",
                    "............",
                }
            },
            {
                LifeCellType.Lottery, new[]
                {
                    "............",
                    "............",
                    "kkkkkkkkkkkk",
                    "kyyykyyyyyyk",
                    "kyyykyrrrryk",
                    "kyyykyyyyyyk",
                    "kyyykyrrrryk",
                    "kyyykyyyyyyk",
                    "kyyykyrrryyk",
                    "kkkkkkkkkkkk",
                    "............",
                    "............",
                }
            },
        };

        /// <summary>
        /// 乗り物（真上から見て上向き）。LifeThemeDefaults.All と同じ並び（現代＝車・ファンタジー＝馬車・宇宙＝宇宙船）。
        /// 家族を乗せる席（CarView.SeatPositions）が車内の灰色（s）の範囲に来るよう、3つとも車内を同じ位置に描いている
        /// </summary>
        public static readonly string[][] Vehicles =
        {
            new[]
            {
                "....dddddddd....",
                "..ddwwwwwwwwdd..",
                ".dwwwwwwwwwwwwd.",
                ".dwyywwwwwwyywd.",
                ".dwwwwwwwwwwwwd.",
                "kdwwwwwwwwwwwwdk",
                "kdwccccccccccwdk",
                "kddccccccccccddk",
                ".dwsssssssssswd.",
                ".dwsssssssssswd.",
                ".dwsssssssssswd.",
                ".dwsssssssssswd.",
                ".dwsssssssssswd.",
                ".dwsssssssssswd.",
                ".dwsssssssssswd.",
                ".dwsssssssssswd.",
                "kdwsssssssssswdk",
                "kdwccccccccccwdk",
                "kddwwwwwwwwwwddk",
                ".dwwwwwwwwwwwwd.",
                ".dwrrwwwwwwrrwd.",
                "..ddwwwwwwwwdd..",
                "...dddddddddd...",
                "................",
            },
            new[]
            {
                "......nnnn......",
                ".....nnnnnn.....",
                "......nnnn......",
                ".....nnnnnn.....",
                ".....nnnnnn.....",
                ".....nnnnnn.....",
                "......nnnn......",
                ".......kk.......",
                "...dddddddddd...",
                "kkdwwwwwwwwwwdkk",
                "kkdwsssssssswdkk",
                "kkdwsssssssswdkk",
                "..dwsssssssswd..",
                "..dwsssssssswd..",
                "..dwsssssssswd..",
                "..dwsssssssswd..",
                "..dwsssssssswd..",
                "kkdwsssssssswdkk",
                "kkdwsssssssswdkk",
                "kkdwwwwwwwwwwdkk",
                "...dddddddddd...",
                "................",
                "................",
                "................",
            },
            new[]
            {
                ".......dd.......",
                "......dwwd......",
                ".....dwwwwd.....",
                ".....dwccwd.....",
                "....dwccccwd....",
                "....dwccccwd....",
                "...dwwwwwwwwd...",
                "..dwsssssssswd..",
                "..dwsssssssswd..",
                "..dwsssssssswd..",
                "..dwsssssssswd..",
                "..dwsssssssswd..",
                ".ddwsssssssswdd.",
                "dwdwsssssssswdwd",
                "dwdwsssssssswdwd",
                "dwdwsssssssswdwd",
                "dwdwwwwwwwwwwdwd",
                "dwddddddddddddwd",
                "dd..kk....kk..dd",
                "....oo....oo....",
                "....yo....oy....",
                ".....o....o.....",
                "................",
                "................",
            },
        };
    }
}
