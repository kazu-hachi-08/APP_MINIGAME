using System.Collections.Generic;

namespace MiniGame.PenguinWars.Battle
{
    /// <summary>
    /// 3章「オーロラ要塞」。大型が途中から何度も出てきて、倍率も高い。
    /// ギミックは2つずつ組み合わせ、最後は城HP 70%・40%・10% で大型が1体ずつ出る3段階のボス
    /// </summary>
    public static partial class StageDefinitions
    {
        private const int Chapter3Number = 3;
        private const string Chapter3Tint = "#E2D6F5";
        private const string Chapter3BossTint = "#CDEFE0";

        private static IEnumerable<StageDefinition> Chapter3()
        {
            yield return new StageDefinition
            {
                Id = "3-1",
                Chapter = Chapter3Number,
                Index = 1,
                Name = "オーロラの門",
                Description = "ひょうざんペンギンが何度も歩いてくる。大型キラーのおのや、こちらの大型でぶつかろう。",
                FieldLength = 30f,
                EnemyCastleHp = 5000,
                TintHex = Chapter3Tint,
                TargetSeconds = 150f,
                UnlockNos = new[] { HammerPenguin, CannonPenguin },
                Entries = new[]
                {
                    Stream(Penguin, 2f, 5f, 0.8f),
                    Stream(SamuraiPenguin, 15f, 22f, 0.8f),
                    Stream(IcebergPenguin, 40f, 70f, 0.6f),
                },
            };

            yield return new StageDefinition
            {
                Id = "3-2",
                Chapter = Chapter3Number,
                Index = 2,
                Name = "なだれと大砲",
                Description = "なだれと敵のペンギン砲の両方が来る。間を空けて少しずつ攻めよう。",
                FieldLength = 30f,
                EnemyCastleHp = 5500,
                TintHex = Chapter3Tint,
                AvalancheInterval = 30f,
                EnemyCannon = new EnemyCannonSettings { ChargeTime = 40f, RangeRatio = 0.5f, Damage = 100, MinTargets = 5 },
                TargetSeconds = 180f,
                UnlockNos = new[] { HeroPenguin, SleepPenguin },
                Entries = new[]
                {
                    Stream(WallPenguin, 2f, 6f, 0.6f),
                    Stream(BoxerPenguin, 15f, 18f, 0.6f),
                    Stream(TallPenguin, 40f, 35f, 0.6f),
                    OnCastle(WhaleRiderPenguin, 0.5f, 70f, 0.45f),
                },
            };

            yield return new StageDefinition
            {
                Id = "3-3",
                Chapter = Chapter3Number,
                Index = 3,
                Name = "こおりの回廊",
                Description = "大型は通れず、働きペンギンはLv5まで。止める妨害が多いので、ふんばる壁で前を固めよう。",
                FieldLength = 30f,
                EnemyCastleHp = 6000,
                TintHex = Chapter3Tint,
                MaxWalletLevel = 5,
                BannedRoles = new[] { UnitRole.Large },
                TargetSeconds = 190f,
                UnlockNos = new[] { MusclePenguin, WizardPenguin },
                Entries = new[]
                {
                    Stream(HelmetPenguin, 2f, 6f, 1.05f),
                    Stream(FreezePenguin, 15f, 22f, 1.05f),
                    Stream(OctopusPenguin, 30f, 26f, 1.05f),
                    OnCastle(HammerPenguin, 0.6f, 18f, 1.05f),
                },
            };

            yield return new StageDefinition
            {
                Id = "3-4",
                Chapter = Chapter3Number,
                Index = 4,
                Name = "巨人の行進",
                Description = "さかなを最初からたくさん持っている。大型が次々に来るので、こちらも大型を早めに出そう。",
                FieldLength = 30f,
                EnemyCastleHp = 6500,
                TintHex = Chapter3Tint,
                StartingFish = 2500,
                TargetSeconds = 160f,
                UnlockNos = new[] { IdolPenguin, SniperPenguin },
                Entries = new[]
                {
                    Stream(IglooPenguin, 2f, 7f, 1.05f),
                    Stream(GiantPenguin, 30f, 80f, 0.8f),
                    Stream(WhaleRiderPenguin, 60f, 80f, 0.8f),
                    OnCastle(RocketPenguin, 0.6f, 30f, 1.05f),
                },
            };

            yield return new StageDefinition
            {
                Id = "3-5",
                Chapter = Chapter3Number,
                Index = 5,
                Name = "じょおうの間",
                Description = "止める・遅くする敵が遠距離に守られている。なだれもあり、働きペンギンはLv6まで。",
                FieldLength = 30f,
                EnemyCastleHp = 7000,
                TintHex = Chapter3Tint,
                AvalancheInterval = 30f,
                MaxWalletLevel = 6,
                TargetSeconds = 220f,
                UnlockNos = new[] { IceQueenPenguin, TowerPenguin },
                Entries = new[]
                {
                    Stream(IglooPenguin, 2f, 7f, 0.6f),
                    Stream(SleepPenguin, 15f, 25f, 0.6f),
                    Stream(WizardPenguin, 35f, 35f, 0.6f),
                    OnCastle(AuroraPenguin, 0.5f, 80f, 0.5f),
                },
            };

            yield return new StageDefinition
            {
                Id = "3-6",
                Chapter = Chapter3Number,
                Index = 6,
                Name = "最終ボス: ペンギンかみさま",
                Description = "城を70%・40%・10%までけずるたびに、大型のボスが1体ずつ出てくる。敵の砲にも気をつけて。",
                FieldLength = 30f,
                EnemyCastleHp = 8000,
                TintHex = Chapter3BossTint,
                EnemyCannon = new EnemyCannonSettings { ChargeTime = 40f, RangeRatio = 0.5f, Damage = 100, MinTargets = 5 },
                TargetSeconds = 400f,
                UnlockNos = new[] { RobotPenguin, KingPenguin, GodPenguin },
                Entries = new[]
                {
                    Stream(IglooPenguin, 2f, 6f, 0.6f),
                    Stream(HeroPenguin, 20f, 22f, 0.6f),
                    Stream(TowerPenguin, 40f, 40f, 0.6f),
                    Boss(RobotPenguin, 0.7f, 1.3f),
                    Boss(KingPenguin, 0.4f, 1.3f),
                    Boss(GodPenguin, 0.1f, 1.3f),
                },
            };
        }
    }
}
