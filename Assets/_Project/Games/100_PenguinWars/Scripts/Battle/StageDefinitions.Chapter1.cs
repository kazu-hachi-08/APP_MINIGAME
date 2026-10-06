using System.Collections.Generic;

namespace MiniGame.PenguinWars.Battle
{
    /// <summary>
    /// 1章「こおりの海岸」。戦場は短め・敵は壁とアタッカーが中心で、数で押してくる。
    /// 解放は安いキャラから配り、次のステージで役に立つものを選ぶ（1-4 の範囲攻撃 → 1-5 の行列など）
    /// </summary>
    public static partial class StageDefinitions
    {
        private const int Chapter1Number = 1;
        private const string Chapter1Tint = "#EAF4FF";

        private static IEnumerable<StageDefinition> Chapter1()
        {
            yield return new StageDefinition
            {
                Id = "1-1",
                Chapter = Chapter1Number,
                Index = 1,
                Name = "はじまりの海岸",
                Description = "敵の城を落とそう。壁で受け止めて、アタッカーでたたく。",
                FieldLength = 18f,
                EnemyCastleHp = 1500,
                TintHex = Chapter1Tint,
                TargetSeconds = 60f,
                UnlockNos = new[] { WallPenguin, BoxerPenguin },
                Entries = new[]
                {
                    Stream(Penguin, 4f, 8f),
                    Stream(SnowballPenguin, 30f, 18f),
                },
            };

            yield return new StageDefinition
            {
                Id = "1-2",
                Chapter = Chapter1Number,
                Index = 2,
                Name = "ひなの浜辺",
                Description = "足の速いひなが次々に来る。さかなけんの敵は壁を並べて止めよう。",
                FieldLength = 20f,
                EnemyCastleHp = 2000,
                TintHex = Chapter1Tint,
                TargetSeconds = 90f,
                UnlockNos = new[] { CardboardPenguin, BalloonPenguin },
                Entries = new[]
                {
                    Stream(ChickPenguin, 3f, 5f, 1.2f),
                    Stream(Penguin, 10f, 9f, 1.2f),
                    Stream(FishSwordPenguin, 25f, 20f, 1.2f),
                },
            };

            yield return new StageDefinition
            {
                Id = "1-3",
                Chapter = Chapter1Number,
                Index = 3,
                Name = "ペンギンの行列",
                Description = "城をたたくとボクサーが出てくる。ふうせんで押し返そう。",
                FieldLength = 20f,
                EnemyCastleHp = 2200,
                TintHex = Chapter1Tint,
                TargetSeconds = 130f,
                UnlockNos = new[] { RoundPenguin, DrillPenguin },
                Entries = new[]
                {
                    Stream(Penguin, 2f, 4f, 1.1f),
                    Stream(CardboardPenguin, 8f, 6f, 1.1f),
                    Stream(FishSwordPenguin, 30f, 20f, 1.1f),
                    OnCastle(BoxerPenguin, 0.7f, 10f, 1.1f),
                },
            };

            yield return new StageDefinition
            {
                Id = "1-4",
                Chapter = Chapter1Number,
                Index = 4,
                Name = "さかな祭り",
                Description = "さかなを最初からたくさん持っている。時間がたつほど敵が強くなるので、いきなり攻めこもう。",
                FieldLength = 22f,
                EnemyCastleHp = 2500,
                TintHex = Chapter1Tint,
                StartingFish = 1500,
                TargetSeconds = 60f,
                UnlockNos = new[] { SumoPenguin, SnowThrowPenguin },
                Entries = new[]
                {
                    Stream(SnowballPenguin, 3f, 5f),
                    Stream(BoxerPenguin, 40f, 10f, 1.2f),
                    Stream(SumoPenguin, 70f, 12f, 1.3f),
                },
            };

            yield return new StageDefinition
            {
                Id = "1-5",
                Chapter = Chapter1Number,
                Index = 5,
                Name = "とおくから",
                Description = "壁の行列の後ろから、ゆみペンギンが矢を撃ってくる。範囲攻撃で壁ごとまとめてたおそう。",
                FieldLength = 22f,
                EnemyCastleHp = 2800,
                TintHex = Chapter1Tint,
                TargetSeconds = 180f,
                UnlockNos = new[] { IcePenguin, LongLegPenguin },
                Entries = new[]
                {
                    Stream(Penguin, 2f, 3.5f, 1.3f),
                    Stream(CardboardPenguin, 6f, 5f, 1.3f),
                    Stream(BowPenguin, 20f, 15f, 1.3f),
                    OnCastle(FishSwordPenguin, 0.6f, 8f, 1.3f),
                },
            };

            yield return new StageDefinition
            {
                Id = "1-6",
                Chapter = Chapter1Number,
                Index = 6,
                Name = "ボス: きょだいペンギン",
                Description = "城をたたくと、すぐにきょだいペンギンがボクサーを連れて出てくる。壁で足止めして、アタッカーを集めてたおそう。",
                FieldLength = 24f,
                EnemyCastleHp = 3000,
                TintHex = Chapter1Tint,
                TargetSeconds = 180f,
                UnlockNos = new[] { IcebergPenguin, WhaleRiderPenguin, FutonPenguin },
                Entries = new[]
                {
                    Stream(Penguin, 2f, 4f, 2f),
                    Stream(SnowballPenguin, 10f, 7f, 2f),
                    Stream(BoxerPenguin, 30f, 15f, 2f),
                    Boss(GiantPenguin, 0.9f, 3f),
                    OnCastle(BoxerPenguin, 0.9f, 2f, 2f, 3),
                    OnCastle(FishSwordPenguin, 0.5f, 8f, 2f),
                },
            };
        }
    }
}
