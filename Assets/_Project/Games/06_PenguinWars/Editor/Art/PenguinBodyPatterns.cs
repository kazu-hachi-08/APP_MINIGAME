using System.Collections.Generic;
using UnityEngine;

namespace MiniGame.PenguinWars.Editor
{
    /// <summary>
    /// 体の形1種類分。右向きで描き、最後の1行は足（歩きのコマで動かすため本体と分けて扱う）。
    /// パーツの取り付け位置は目印の文字で書く: H=頭のてっぺん、A=手（持ち物）、R=背中。目印のドットは下地の色で塗る
    /// </summary>
    public class BodyShape
    {
        private static readonly Dictionary<char, char> MarkerBaseColors = new Dictionary<char, char>
        {
            ['H'] = 'o',
            ['A'] = 'w',
            ['R'] = 'k',
        };

        public string[] Rows { get; }
        public Vector2Int Head { get; }
        public Vector2Int Hand { get; }
        public Vector2Int Back { get; }

        public int Width => Rows[0].Length;
        public int Height => Rows.Length;

        public BodyShape(params string[] rows)
        {
            Head = FindMarker(rows, 'H');
            Hand = FindMarker(rows, 'A');
            Back = FindMarker(rows, 'R');
            Rows = ReplaceMarkers(rows);
        }

        /// <summary>パターン内の位置（列, 上からの行）</summary>
        private static Vector2Int FindMarker(string[] rows, char marker)
        {
            for (int r = 0; r < rows.Length; r++)
            {
                int c = rows[r].IndexOf(marker);
                if (c >= 0) return new Vector2Int(c, r);
            }
            Debug.LogError($"[BodyShape] 目印 '{marker}' がありません");
            return Vector2Int.zero;
        }

        private static string[] ReplaceMarkers(string[] rows)
        {
            var result = new string[rows.Length];
            for (int r = 0; r < rows.Length; r++)
            {
                string row = rows[r];
                foreach (KeyValuePair<char, char> pair in MarkerBaseColors) row = row.Replace(pair.Key, pair.Value);
                result[r] = row;
            }
            return result;
        }
    }

    /// <summary>
    /// 体の形のドットパターン（仕様書 §7.2「体の形」）。ID 文字列で引くので、Phase 9 で形を足すときはここに1項目足すだけでよい。
    /// 文字の意味は PenguinPalette を参照
    /// </summary>
    public static class PenguinBodyPatterns
    {
        public const string DefaultId = "basic";

        public static readonly Dictionary<string, BodyShape> Shapes = new Dictionary<string, BodyShape>
        {
            // 基本形（16x19）
            ["basic"] = new BodyShape(
                ".....ooHooo.....",
                "....okkkkkkoo...",
                "...okkkkkkkkko..",
                "...okkkkkkwwko..",
                "..okkkkkkkwekoo.",
                "..okkkkkkkwwkyyo",
                "..okkkkkkkkkoyyo",
                "..okkkkkkkwwoo..",
                ".okkkkkkkwwwwo..",
                ".okkkkkkwwwwwwo.",
                ".oRkkkkkwwwwwwo.",
                ".okkkkkkwwwAwwo.",
                ".okkkkkkwwwwwwo.",
                ".okkkkkkwwwwwwo.",
                ".okkkkkkwwwwwwo.",
                "..okkkkkwwwwwo..",
                "..okkkkkwwwwwo..",
                "...ooooooooooo..",
                "....yyy...yyy..."),

            // 横に広い四角い体（かべペンギン。22x17）
            ["wide"] = new BodyShape(
                "...ooooooooHooooooo...",
                "..okkkkkkkkkkkkkkkkoo.",
                ".okkkkkkkkkkkkkwwkkko.",
                ".okkkkkkkkkkkkwwekkkoo",
                ".okkkkkkkkkkkkwwwkkyyo",
                ".okkkkkkkkkkkkkkkkoyyo",
                ".okkkkkkkkkwwwwwwwooo.",
                ".okkkkkkkkwwwwwwwwwo..",
                ".oRkkkkkkkwwwwwwwwwo..",
                ".okkkkkkkkwwwwwAwwwo..",
                ".okkkkkkkkwwwwwwwwwo..",
                ".okkkkkkkkwwwwwwwwwo..",
                ".okkkkkkkkwwwwwwwwwo..",
                ".okkkkkkkkwwwwwwwwwo..",
                "..okkkkkkkwwwwwwwwo...",
                "...oooooooooooooooo...",
                "....yyyy.....yyyy....."),

            // 首が長い（のっぽペンギン。16x26）
            ["tall"] = new BodyShape(
                ".......ooHooo...",
                "......okkkkkoo..",
                ".....okkkkwwkko.",
                ".....okkkkwekoyo",
                ".....okkkkwwkyyo",
                ".....okkkkkkkoo.",
                "......okkkwwo...",
                "......okkkwwo...",
                "......okkkwwo...",
                "......okkkwwo...",
                "......okkkwwo...",
                "......okkkwwo...",
                ".....okkkkwwo...",
                "....okkkkkwwwo..",
                "...okkkkkkwwwwo.",
                "..okkkkkkwwwwwwo",
                "..oRkkkkkwwwwwwo",
                "..okkkkkkwwwAwwo",
                "..okkkkkkwwwwwwo",
                "..okkkkkkwwwwwwo",
                "..okkkkkkwwwwwwo",
                "..okkkkkkwwwwwwo",
                "...okkkkkwwwwwo.",
                "...okkkkkwwwwwo.",
                "....ooooooooooo.",
                ".....yyy...yyy.."),
        };
    }
}
