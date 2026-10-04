using System.Collections.Generic;
using UnityEngine;

namespace MiniGame.PenguinWars.Editor
{
    public static partial class PenguinPartPatterns
    {
        // 青いマント（こおりのじょおう）。赤いマント（キング）はこれの色違い
        private static readonly string[] CapeRows =
        {
            ".......ooo",
            "......offo",
            ".....offfo",
            ".....offfo",
            "....offffo",
            "....offffo",
            "...offfffo",
            "...offfffo",
            "..offffffo",
            ".offfffffo",
            "ojfjfjfjfo",
            "oooooooooo",
        };

        /// <summary>
        /// 背中・乗り物・体のまわりのパーツ。Anchor はパターン内で体の目印 R（背中）に重ねる位置。乗り物（Ground）は足元の中央に重ねる。
        /// 大きいパーツは PenguinPartPatterns.Large.cs にある
        /// </summary>
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
            ["rocket"] = new PartPattern(PartAttach.Back, PartLayer.Behind, new Vector2Int(6, 8), new[]
            {
                "...oo...",
                "..orro..",
                ".orrrro.",
                ".osssso.",
                ".oslsso.",
                ".osssso.",
                ".osssso.",
                "oossssoo",
                "orssssro",
                "orooooro",
                "oo2332oo",
                "..2332..",
                "...22...",
            }),
            ["cape_blue"] = new PartPattern(PartAttach.Back, PartLayer.Behind, new Vector2Int(7, 4), CapeRows),
            // 白いふちの付いた赤いマント
            ["cape_red"] = new PartPattern(PartAttach.Back, PartLayer.Behind, new Vector2Int(7, 4),
                Recolor(CapeRows, ('f', 'r'), ('j', 'w'))),
            // 背中から広げた白い羽（体の後ろ側だけ見える）
            ["wings"] = new PartPattern(PartAttach.Back, PartLayer.Behind, new Vector2Int(10, 13), new[]
            {
                "oo..........",
                "owoo........",
                "owwwoo......",
                ".owwwwoo....",
                ".owwwwwwoo..",
                "..owwwwwwwoo",
                "oowwwwwwwwwo",
                "owwwwwwwwwwo",
                ".oowwwwwwwwo",
                "...owwwwwwwo",
                "..oowwwwwwo.",
                ".owwwwwwwo..",
                "..oooowwo...",
                "......oo....",
            }),
            // 首から下を布団でくるむ（手前に描いて体を隠す）
            ["futon"] = new PartPattern(PartAttach.Back, PartLayer.Front, new Vector2Int(2, 3), new[]
            {
                "..ooooooooooooo..",
                ".oppppppppppppppo",
                "opwppppwppppwpppo",
                "opppppppppppppppo",
                "oppppwppppwppppwo",
                "owwwwwwwwwwwwwwwo",
                "opppppppppppppppo",
                "opwppppwppppwpppo",
                "opppppppppppppppo",
                ".oppppwppppwpppo.",
                "..ooooooooooooo..",
            }),
            // まわし。横長の体（wide）の腰に巻く。目印 R より下に描くので Anchor の行が負になる
            ["mawashi"] = new PartPattern(PartAttach.Back, PartLayer.Front, new Vector2Int(1, -3), new[]
            {
                "ooooooooooooooooooo",
                "ottttttttttttttttto",
                "ouuuuuuuuuuuuuuuuuo",
                "oooooooooootototooo",
                "..........otototo..",
                "..........o.o.o.o..",
            }),
            // 体のまわりに舞う雪
            ["snow_aura"] = new PartPattern(PartAttach.Back, PartLayer.Front, new Vector2Int(9, 17), new[]
            {
                "..........i..................",
                ".........iwi..........i......",
                ".i........i..........iwi.....",
                "iwi...................i......",
                ".i...........................",
                ".............................",
                "..........................i..",
                ".........................iwi.",
                "..........................i..",
                "...i.........................",
                "..iwi........................",
                "...i.........................",
                ".............................",
                ".............................",
                ".............................",
                ".............................",
                ".............................",
                "...........................i.",
                "..........................iwi",
                "...........................i.",
                ".....i.......................",
                "....iwi......................",
                ".....i.......................",
            }),
            ["kamakura"] = Kamakura(),
            ["iceberg"] = Iceberg(),
            ["aurora"] = Aurora(),

            // ---- 乗り物・足（体を持ち上げて足を隠す） ----
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
            ["long_legs"] = new PartPattern(PartAttach.Ground, PartLayer.Front, new Vector2Int(8, 6), new[]
            {
                ".....oyo..oyo...",
                ".....oyo..oyo...",
                ".....oyo..oyo...",
                ".....oyo..oyo...",
                ".....oyo..oyo...",
                ".....oyo..oyo...",
                ".....oyyyyoyyyyo",
            }, bodyLift: 6, hideFeet: true),
            ["octopus_legs"] = new PartPattern(PartAttach.Ground, PartLayer.Front, new Vector2Int(8, 5), new[]
            {
                ".ooooooooooooooo.",
                "orrrrrrrrrrrrrrro",
                "orpprpprpprpprrro",
                "orro.orro.orro.oo",
                "oro..oro..oro..ro",
                "oo...oo...oo...oo",
            }, bodyLift: 4, hideFeet: true),
            ["tank"] = Tank(),
            ["whale"] = Whale(),
        };
    }
}
