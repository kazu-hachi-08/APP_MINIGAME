using UnityEngine;

namespace MiniGame.PenguinWars.Art
{
    /// <summary>
    /// 体より大きいパーツ（かまくら・氷山・オーロラ・乗り物）。行数が多く Backs の辞書が読みにくくなるので、メソッドに分けてここに置く。
    /// 曲線の形は楕円・円の式で下書きしてから手で整えた
    /// </summary>
    public static partial class PenguinPartPatterns
    {
        /// <summary>かまくら。入口の穴から中のペンギンが見えるよう、体の手前に描く</summary>
        private static PartPattern Kamakura()
        {
            return new PartPattern(PartAttach.Ground, PartLayer.Front, new Vector2Int(14, 22), new[]
            {
                "............ooooo............",
                "..........oowwwlloo..........",
                "........oowwwwwlllloo........",
                ".......owllllllllllllo.......",
                "......owwwwwwwwwwwwlllo......",
                ".....owwwwwwwwwwwwwwlllo.....",
                "....owwwwwwwwwwwwwwwwlllo....",
                "....owwwwwwwwwwoooowwwllo....",
                "...owlllllllllo....olllllo...",
                "...owwwwwwwwwo......owwllo...",
                "..owwwwwwwwwwo......owwwllo..",
                "..owwwwwwwwwo........owwllo..",
                "..owwwwwwwwwo........owwwlo..",
                ".owlllllllllo........olllllo.",
                ".owwwwwwwwwo..........owwllo.",
                ".owwwwwwwwwo..........owwwlo.",
                ".owwwwwwwwwo..........owwwlo.",
                "owwwwwwwwwwo..........owwwllo",
                "owlllllllllo..........olllllo",
                "owwwwwwwwwwo..........owwwllo",
                "owwwwwwwwwo............owwwlo",
                "owwwwwwwwwo............owwwlo",
                "owwwwwwwwwo............owwwlo",
            }, hideFeet: true);
        }

        /// <summary>背負った氷山。頭より高くそびえさせて大型らしく見せる</summary>
        private static PartPattern Iceberg()
        {
            return new PartPattern(PartAttach.Back, PartLayer.Behind, new Vector2Int(10, 22), new[]
            {
                "........o............",
                ".......owo...........",
                "......owwwo..........",
                "......owwwo..........",
                "......owwwo..........",
                ".....owwwwwo.........",
                "....owwwwwwwo........",
                ".....ollliio.........",
                "....olllliiio........",
                "...oolllliiioo.......",
                "..olllllliiiiio......",
                "...ollllliiiio.......",
                "..ollllllliiiio......",
                "..ollllllliiiio......",
                ".olllllllliiiiio.....",
                ".olllllllliiiiio.....",
                ".olllllllliiiiio.....",
                "ollllllllliiiiiio....",
                "lllllllllliiiiiiio...",
                "ollllllllliiiiiio....",
                "lllllllllliiiiiiio...",
                "lllllllllliiiiiiioo..",
                "lllllllllliiiiiiiiio.",
                "lllllllllliiiiiiiio..",
                "llllllllllliiiiiiiio.",
                "llllllllllliiiiiiiio.",
                "llllllllllliiiiiiiiio",
                "llllllllllliiiiiiiiio",
                "oooooooooooooooooooo.",
            });
        }

        /// <summary>体の後ろの虹の輪（オーロラペンギン）。虹色は陣営で変わらないので敵でも見分けられる</summary>
        private static PartPattern Aurora()
        {
            return new PartPattern(PartAttach.Back, PartLayer.Behind, new Vector2Int(10, 11), new[]
            {
                "............111111111...........",
                ".........111122222221111........",
                "........11222333333322211.......",
                "......112233344444443332211.....",
                ".....11233444555555544433211....",
                "....1223344556666666554433221...",
                "...112344556666666666655443211..",
                "...1234456666.......6666544321..",
                "..123345666...........666543321.",
                ".112345666.............666543211",
                ".12344566...............66544321",
                ".12345666...............66654321",
                "11234566.................6654321",
                "12334566.................6654332",
                "12345666.................6665432",
                "12345666.................6665432",
                "12345666.................6665432",
                "12334566.................6654332",
            });
        }

        /// <summary>戦車。体を沈めて車体から上半身だけ出し、砲身を前に向ける</summary>
        private static PartPattern Tank()
        {
            return new PartPattern(PartAttach.Ground, PartLayer.Front, new Vector2Int(14, 7), new[]
            {
                "..oooooooooooooooooooooo......",
                ".oqqqqqqqqqqqqqqqqqqqqqqoooooo",
                ".oqqqqqqqqqqqqqqqqqqqqqqqmmmmo",
                ".ozzzzzzzzzzzzzzzzzzzzzzoooooo",
                "oooooooooooooooooooooooooo....",
                "onmnnmnnmnnmnnmnnmnnmnnmno....",
                "onnmnnmnnmnnmnnmnnmnnmnnno....",
                ".oooooooooooooooooooooooo.....",
            }, bodyLift: 5, hideFeet: true);
        }

        /// <summary>クジラの背中に乗る</summary>
        private static PartPattern Whale()
        {
            return new PartPattern(PartAttach.Ground, PartLayer.Front, new Vector2Int(16, 13), new[]
            {
                "..............ooooooo..........",
                "..........ooooaaaaaaaoooo......",
                ".oo.....ooaaaaaaaaaaaaaaaoo....",
                "oaao...oaaaaaaaaaaaaaaaaaaao...",
                ".oaao.oaaaaaaaaaaaaaaaaaaaaao..",
                "..oaaoaaaaaaaaaaaaaaaaaaeaaaao.",
                "...oaaaaaaaaaaaaaaaaaaaaaaaaaao",
                "..oaaaaaaaaaaaaaaaaaaaaaaaaaaao",
                ".oaaoaaaaaaaaaaaaaaaaaaaaaaaaao",
                "oaao.oaaaaaaaaaaaaaaaaaaaaaaao.",
                ".oo...oaawwwwwwwwwwwwwwwwwwwo..",
                ".......oawwwwwwwwwwwwwwwwwwo...",
                "........oowwwwwwwwwwwwwwwoo....",
                "..........oooowwwwwwwoooo......",
            }, bodyLift: 10, hideFeet: true);
        }
    }
}
