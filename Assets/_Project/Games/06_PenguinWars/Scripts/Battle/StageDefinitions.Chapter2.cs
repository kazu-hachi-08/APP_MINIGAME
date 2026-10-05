using System.Collections.Generic;

namespace MiniGame.PenguinWars.Battle
{
    /// <summary>
    /// 2章「ゆきやまの奥」。戦場が長くなり、遠距離・妨害が混ざる。
    /// ギミック（なだれ・働きペンギンの上限・大型禁止）を1ステージに1つずつ体験させ、ボスで敵の砲が初めて出る
    /// </summary>
    public static partial class StageDefinitions
    {
        private const int Chapter2Number = 2;
        private const string Chapter2Tint = "#D6E6FA";

        private static IEnumerable<StageDefinition> Chapter2()
        {
            yield return new StageDefinition
            {
                Id = "2-1",
                Chapter = Chapter2Number,
                Index = 1,
                Name = "ゆきやまの入口",
                Description = "ねばねばの敵に当たると足が遅くなる。足止めされる前に、射程の長いキャラでたおそう。",
                FieldLength = 24f,
                EnemyCastleHp = 3000,
                TintHex = Chapter2Tint,
                TargetSeconds = 160f,
                UnlockNos = new[] { HelmetPenguin, SamuraiPenguin },
                Entries = new[]
                {
                    Stream(Penguin, 2f, 5f, 1.2f),
                    Stream(StickyPenguin, 15f, 15f, 1.2f),
                    Stream(FishSwordPenguin, 30f, 15f, 1.2f),
                },
            };

            yield return new StageDefinition
            {
                Id = "2-2",
                Chapter = Chapter2Number,
                Index = 2,
                Name = "なだれの谷",
                Description = "まん中になだれが落ちてくる。予告が出たら、キャラを出すのを少し待とう。",
                FieldLength = 26f,
                EnemyCastleHp = 3200,
                TintHex = Chapter2Tint,
                AvalancheInterval = 25f,
                TargetSeconds = 150f,
                UnlockNos = new[] { OctopusPenguin, BoomerangPenguin },
                Entries = new[]
                {
                    Stream(SnowballPenguin, 2f, 5f, 1.1f),
                    Stream(HarisenPenguin, 12f, 14f, 1.1f),
                    Stream(BowPenguin, 25f, 20f, 1.1f),
                    OnCastle(BoxerPenguin, 0.6f, 12f, 1.1f),
                },
            };

            yield return new StageDefinition
            {
                Id = "2-3",
                Chapter = Chapter2Number,
                Index = 3,
                Name = "さかなの少ない峠",
                Description = "働きペンギンはLv3まで。高いキャラは出しにくいので、安いキャラをたくさん回そう。",
                FieldLength = 26f,
                EnemyCastleHp = 3500,
                TintHex = Chapter2Tint,
                MaxWalletLevel = 3,
                TargetSeconds = 140f,
                UnlockNos = new[] { FanPenguin, TallPenguin },
                Entries = new[]
                {
                    Stream(ChickPenguin, 2f, 3f, 1.6f),
                    Stream(Penguin, 6f, 5f, 1.6f),
                    Stream(NinjaPenguin, 20f, 15f, 1.6f),
                    OnCastle(BalloonPenguin, 0.6f, 15f, 1.6f),
                },
            };

            yield return new StageDefinition
            {
                Id = "2-4",
                Chapter = Chapter2Number,
                Index = 4,
                Name = "ほそい雪道",
                Description = "大型は通れない。ふっとばす・遅くする敵がたくさん。妨害キラーや遠距離で後ろからたたこう。",
                FieldLength = 28f,
                EnemyCastleHp = 4000,
                TintHex = Chapter2Tint,
                BannedRoles = new[] { UnitRole.Large },
                TargetSeconds = 310f,
                UnlockNos = new[] { BikePenguin, FreezePenguin },
                Entries = new[]
                {
                    Stream(WallPenguin, 2f, 6f, 1.25f),
                    Stream(BalloonPenguin, 10f, 14f, 1.25f),
                    Stream(StickyPenguin, 20f, 16f, 1.25f),
                    Stream(FishingPenguin, 35f, 22f, 1.25f),
                },
            };

            yield return new StageDefinition
            {
                Id = "2-5",
                Chapter = Chapter2Number,
                Index = 5,
                Name = "長い雪原",
                Description = "壁の後ろから遠距離が撃ってくる。ながあしやゆきなげで後ろの敵をねらおう。",
                FieldLength = 30f,
                EnemyCastleHp = 4500,
                TintHex = Chapter2Tint,
                TargetSeconds = 240f,
                UnlockNos = new[] { IglooPenguin, RocketPenguin },
                Entries = new[]
                {
                    Stream(WallPenguin, 2f, 5f, 1.05f),
                    Stream(FishingPenguin, 15f, 18f, 1.05f),
                    Stream(SnowThrowPenguin, 40f, 25f, 1.05f),
                    OnCastle(SamuraiPenguin, 0.6f, 15f, 1.05f),
                },
            };

            yield return new StageDefinition
            {
                Id = "2-6",
                Chapter = Chapter2Number,
                Index = 6,
                Name = "ボス: ペンギンせんしゃ",
                Description = "敵の城がペンギン砲を撃ってくる。かたまりすぎないように。城を半分けずると、せんしゃが仲間を連れて出てくる。",
                FieldLength = 30f,
                EnemyCastleHp = 5000,
                TintHex = Chapter2Tint,
                EnemyCannon = new EnemyCannonSettings { ChargeTime = 35f, RangeRatio = 0.5f, Damage = 100, MinTargets = 4 },
                TargetSeconds = 250f,
                UnlockNos = new[] { GiantPenguin, TankPenguin, AuroraPenguin, BlizzardPenguin },
                Entries = new[]
                {
                    Stream(WallPenguin, 2f, 5f),
                    Stream(SamuraiPenguin, 20f, 18f),
                    Stream(BowPenguin, 30f, 22f),
                    Boss(TankPenguin, 0.5f, 1.2f),
                    OnCastle(BalloonPenguin, 0.5f, 10f),
                },
            };
        }
    }
}
