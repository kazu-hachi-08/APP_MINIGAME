using System.Collections.Generic;
using UnityEngine;

namespace MiniGame.PenguinWars.Editor
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
    /// 頭・手・背中（乗り物）パーツのドットパターン（仕様書 §7.2）。ID 文字列で引くので、Phase 9 でパーツを足すときはここに1項目足すだけでよい
    /// </summary>
    public static class PenguinPartPatterns
    {
        public static readonly Dictionary<string, PartPattern> Heads = new Dictionary<string, PartPattern>
        {
            ["helmet"] = new PartPattern(PartAttach.Head, PartLayer.Front, new Vector2Int(7, 3), new[]
            {
                "....ooooooo....",
                "..oottttttsoo..",
                ".otttttttttsso.",
                ".otttttttttttso",
                ".ouuuuuuuuuuuuo",
                ".ooooooooooooooo",
            }),
        };

        public static readonly Dictionary<string, PartPattern> Hands = new Dictionary<string, PartPattern>
        {
            ["axe"] = new PartPattern(PartAttach.Hand, PartLayer.Front, new Vector2Int(2, 11), new[]
            {
                "..ooooo...",
                ".obsssso..",
                ".obssssso.",
                ".obmsssso.",
                ".obmmmoo..",
                ".obo......",
                ".obo......",
                ".obo......",
                ".obo......",
                ".obo......",
                "okkko.....",
                "okkko.....",
                ".ooo......",
            }),
            ["glove"] = new PartPattern(PartAttach.Hand, PartLayer.Front, new Vector2Int(1, 3), new[]
            {
                "..oooo..",
                ".ottttoo",
                "okttttto",
                "okttttuo",
                "okkuuuuo",
                ".ooooo..",
            }),
            ["bow"] = new PartPattern(PartAttach.Hand, PartLayer.Front, new Vector2Int(2, 7), new[]
            {
                ".oo.......",
                ".nboo.....",
                ".n.bbo....",
                ".n..bo....",
                ".n...bo...",
                ".n...bo...",
                ".n...bo...",
                ".ndddbddso",
                ".n...bo...",
                ".n...bo...",
                ".n...bo...",
                ".n..bo....",
                ".n.bbo....",
                ".nboo.....",
                ".oo.......",
            }),
        };

        public static readonly Dictionary<string, PartPattern> Backs = new Dictionary<string, PartPattern>
        {
            ["freezer"] = new PartPattern(PartAttach.Back, PartLayer.Behind, new Vector2Int(10, 6), new[]
            {
                "oooooooooooo",
                "ollllllllllo",
                "olsssssssslo",
                "olsttttttslo",
                "olsssssssslo",
                "oiiiiiiiiiio",
                "olsssssssslo",
                "olsmsssssslo",
                "olsmsssssslo",
                "olsssssssslo",
                "olsssssssslo",
                "oiiiiiiiiiio",
                "oooooooooooo",
            }),
            // 乗り物は体を持ち上げて座らせ、足は隠す
            ["bike"] = new PartPattern(PartAttach.Ground, PartLayer.Front, new Vector2Int(12, 9), new[]
            {
                "......................oo..",
                ".....................omo..",
                "....................omo...",
                "..ooooooooooooooooooomo...",
                ".ottttttttttttttttttttoo..",
                ".ouuuuuuuuuuuuuuuuuuuuuo..",
                "..oonnoo........oonnoo....",
                "..onmmno........onmmno....",
                "..onmmno........onmmno....",
                "...onno..........onno.....",
            }, bodyLift: 5, hideFeet: true),
        };
    }
}
