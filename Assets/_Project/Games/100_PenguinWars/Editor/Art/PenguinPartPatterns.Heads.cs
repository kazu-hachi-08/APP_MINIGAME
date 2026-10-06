using System.Collections.Generic;
using UnityEngine;

namespace MiniGame.PenguinWars.Editor
{
    public static partial class PenguinPartPatterns
    {
        /// <summary>
        /// 頭パーツ。Anchor はパターン内で体の目印 H（頭のてっぺん）に重ねる位置。
        /// 顔まわりのパーツ（ドリル・ずきん）は H より下に描くので、Anchor の行が負になることがある
        /// </summary>
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
            // 段ボール箱。目とくちばしの所だけ穴を空けて、中にペンギンがいると分かるようにする
            ["box"] = new PartPattern(PartAttach.Head, PartLayer.Front, new Vector2Int(7, 3), new[]
            {
                "ooooooooooooooooo",
                "occccccchccccccco",
                "occccccchccccccco",
                "ohhhhhhhhhhhhhhho",
                "occccccccccccccco",
                "occccccccoooooooo",
                "occcccccco.....oo",
                "occcccccco.....oo",
                "occcccccco.....oo",
                "occccccccoooooooo",
                "ooooooooooooooooo",
            }),
            ["chonmage"] = new PartPattern(PartAttach.Head, PartLayer.Front, new Vector2Int(4, 4), new[]
            {
                "...ooo.",
                "..onnno",
                ".onnnno",
                "onnnoo.",
                "ooooo..",
            }),
            // 黒ずきん。目の所だけ細く空ける。後ろに結び目をなびかせてシルエットで忍者と分かるようにする
            ["hood"] = new PartPattern(PartAttach.Head, PartLayer.Front, new Vector2Int(9, 1), new[]
            {
                ".......ooooooo....",
                "oo...oonnnnnnnoo..",
                "onoo.onnnnnnnnnno.",
                ".onnnonnnnnnnnnnno",
                "..oonnnnnnnnnnnnno",
                "....onnnnnnnn...no",
                "....onnnnnnnnnnnno",
                "....onnnnnnnnnnnno",
                ".....onnnnnnnnnoo.",
                ".....ooooooooooo..",
            }),
            ["wizard_hat"] = new PartPattern(PartAttach.Head, PartLayer.Front, new Vector2Int(7, 9), new[]
            {
                "...oo..........",
                "..ovvo.........",
                "..oovvo........",
                "....ovvo.......",
                "....ovvvo......",
                "...ovvvvvo.....",
                "...ovvgvvvo....",
                "..ovvvvvvvvo...",
                "..ogggggggggo..",
                "ovvvvvvvvvvvvvo",
                "ooooooooooooooo",
            }),
            // 後ろに垂れたナイトキャップ。先のぼんぼりを体の後ろ側に出す
            ["nightcap"] = new PartPattern(PartAttach.Head, PartLayer.Front, new Vector2Int(9, 5), new[]
            {
                "......oooooo.....",
                "....oopppppoo....",
                ".oooppppppppppo..",
                "owwopppppppppppo.",
                "owwooopppppppppo.",
                ".oo..owwwwwwwwwwo",
                ".....ooooooooooo.",
            }),
            ["ribbon"] = new PartPattern(PartAttach.Head, PartLayer.Front, new Vector2Int(5, 3), new[]
            {
                "oo...oo",
                "oppoppo",
                "oprrrpo",
                "oppoppo",
                "oo...oo",
            }),
            ["crown"] = new PartPattern(PartAttach.Head, PartLayer.Front, new Vector2Int(4, 4), new[]
            {
                "o...o...o",
                "ogoogoogo",
                "ogggrgggo",
                "ogggggggo",
                "ooooooooo",
            }),
            // 頭から少し浮かせた光の輪
            ["halo"] = new PartPattern(PartAttach.Head, PartLayer.Front, new Vector2Int(5, 5), new[]
            {
                "..ooooooo..",
                ".oggggggggo",
                "og.......go",
                ".oggggggggo",
                "..ooooooo..",
            }),
            // くちばしをドリルに置き換える（体の目印 H から右下へずらして描く）
            ["drill"] = new PartPattern(PartAttach.Head, PartLayer.Front, new Vector2Int(-5, -3), new[]
            {
                "ooo......",
                "osmoo....",
                "omsmsoo..",
                "osmsmsmoo",
                "omsmsoo..",
                "osmoo....",
                "ooo......",
            }),
            // 頭にかかった雪どけ水。ぽたぽた垂れる筋で「ねばねば」を出す
            ["slime"] = new PartPattern(PartAttach.Head, PartLayer.Front, new Vector2Int(6, 1), new[]
            {
                ".....oooooo....",
                "...oolllllloo..",
                "..olllllllllio.",
                ".olliollllioio.",
                ".olo.olio.oio..",
                ".oio..oio..o...",
                "..o....o.......",
                ".......o.......",
                "......oio......",
                ".......o.......",
            }),
        };
    }
}
