using System.Collections.Generic;

namespace MiniGame.PenguinWars.Battle
{
    /// <summary>1章のステージ</summary>
    public static partial class StageDefinitions
    {
        private const int Penguin = 1;
        private const int SnowballPenguin = 3;
        private const int FishSwordPenguin = 12;
        private const int SumoPenguin = 14;
        private const int BowPenguin = 23;
        private const int GiantPenguin = 43;
        // 1-3 の編成制限。初期10体のうち壁・近接の安いキャラだけが残る値
        private const int CheapUnitCost = 300;

        private static IEnumerable<StageDefinition> Chapter1()
        {
            // 仮のステージ（Phase 2 までの3つ。中身と解放キャラは Phase 6 で作り直す）。壁と安いアタッカーが湧き、城を半分削ると大型が1体出る。
            // Phase 4 のギミックを1ステージずつ違う組み合わせで入れて、遊んで違いが分かるか確かめる（1-1 速攻 / 1-2 なだれ＋節約 / 1-3 安いキャラだけ＋敵の砲）
            yield return new StageDefinition
            {
                Id = "1-1",
                Chapter = 1,
                Index = 1,
                Name = "はじまりの氷原",
                Description = "敵の城を落とそう。さかなを最初からたくさん持っているので、いきなり攻めこめる。",
                EnemyCastleHp = 3000,
                TintHex = "#E6F2FF",
                StartingFish = 1000,
                TargetSeconds = 120f,
                UnlockNos = new[] { 2, 13 },
                Entries = new[]
                {
                    new EnemySpawnEntry { UnitNo = Penguin, StartTime = 3f, Interval = 6f },
                    new EnemySpawnEntry { UnitNo = FishSwordPenguin, StartTime = 20f, Interval = 15f },
                    new EnemySpawnEntry { UnitNo = GiantPenguin, Count = 1, TriggerCastleHpRatio = 0.5f, IsBoss = true },
                },
            };

            yield return new StageDefinition
            {
                Id = "1-2",
                Chapter = 1,
                Index = 2,
                Name = "ゆきだまの丘",
                Description = "まん中になだれが起きる。働きペンギンはLv4まで。弓の敵は壁で受け止めよう。",
                EnemyCastleHp = 4000,
                TintHex = "#FFE8CC",
                AvalancheInterval = 30f,
                MaxWalletLevel = 4,
                TargetSeconds = 150f,
                UnlockNos = new[] { 24, 41 },
                Entries = new[]
                {
                    new EnemySpawnEntry { UnitNo = SnowballPenguin, StartTime = 3f, Interval = 7f },
                    new EnemySpawnEntry { UnitNo = BowPenguin, StartTime = 25f, Interval = 20f },
                    new EnemySpawnEntry { UnitNo = GiantPenguin, Count = 1, TriggerCastleHpRatio = 0.5f, IsBoss = true },
                },
            };

            yield return new StageDefinition
            {
                Id = "1-3",
                Chapter = 1,
                Index = 3,
                Name = "すもう場",
                Description = "安いキャラしか出せない。敵の城は砲を撃ってくるので、かたまりすぎないように。",
                EnemyCastleHp = 5000,
                TintHex = "#D8D0F0",
                MaxUnitCost = CheapUnitCost,
                EnemyCannon = new EnemyCannonSettings { ChargeTime = 30f, RangeRatio = 0.5f, Damage = 150, MinTargets = 4 },
                TargetSeconds = 180f,
                UnlockNos = new[] { 7, 14 },
                Entries = new[]
                {
                    new EnemySpawnEntry { UnitNo = Penguin, StartTime = 2f, Interval = 5f },
                    new EnemySpawnEntry { UnitNo = SumoPenguin, StartTime = 15f, Interval = 25f },
                    new EnemySpawnEntry { UnitNo = FishSwordPenguin, StartTime = 0f, Interval = 10f, TriggerCastleHpRatio = 0.7f },
                    new EnemySpawnEntry { UnitNo = GiantPenguin, Count = 1, TriggerCastleHpRatio = 0.4f, IsBoss = true, StatMultiplier = 1.2f },
                },
            };
        }
    }
}
