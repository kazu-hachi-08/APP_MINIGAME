using System.Collections.Generic;
using UnityEngine;

namespace MiniGame.PenguinWars.Art
{
    /// <summary>パーツをどこに付けるか。Ground は乗り物用で、体ではなく足元の中央に付ける</summary>
    public enum PartAttach
    {
        Head,
        Hand,
        Back,
        Ground,
    }

    /// <summary>体より奥に描くか手前に描くか（背負い物は奥、持ち物・乗り物は手前）</summary>
    public enum PartLayer
    {
        Behind,
        Front,
    }

    /// <summary>
    /// 重ね描きするパーツ1つ分。右向きで描く。Anchor はパターン内で体の目印（H/A/R）に重ねる位置（列, 上からの行）
    /// </summary>
    public class PartPattern
    {
        public PartAttach Attach { get; }
        public PartLayer Layer { get; }
        public Vector2Int Anchor { get; }
        public string[] Rows { get; }
        /// <summary>体を持ち上げるドット数（乗り物に座らせるため）</summary>
        public int BodyLift { get; }
        public bool HideFeet { get; }

        public PartPattern(PartAttach attach, PartLayer layer, Vector2Int anchor, string[] rows,
            int bodyLift = 0, bool hideFeet = false)
        {
            Attach = attach;
            Layer = layer;
            Anchor = anchor;
            Rows = rows;
            BodyLift = bodyLift;
            HideFeet = hideFeet;
        }
    }

    /// <summary>
    /// 頭・手・背中（乗り物）パーツのドットパターン（仕様書 §7.2）。ID 文字列で引くので、パーツを足すときは辞書に1項目足すだけでよい。
    /// 部位ごとにファイルを分けている（Heads / Hands / Backs）。1ファイルが長くなりすぎず、2人で別の部位を触ってもぶつからないため
    /// </summary>
    public static partial class PenguinPartPatterns
    {
        /// <summary>同じ形の色違い（マントの青・赤など）を、パターンを二重に書かずに作る</summary>
        private static string[] Recolor(string[] rows, params (char from, char to)[] pairs)
        {
            var result = new string[rows.Length];
            for (int r = 0; r < rows.Length; r++)
            {
                string row = rows[r];
                foreach ((char from, char to) in pairs) row = row.Replace(from, to);
                result[r] = row;
            }
            return result;
        }
    }
}
