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

        private static IEnumerable<StageDefinition> Chapter1()
        {
            // 仮のステージ（Phase 2 までの3つ。中身は Phase 6 で作り直す）。壁と安いアタッカーが湧き、城を半分削ると大型が1体出る
            yield return new StageDefinition
            {
                Id = "1-1",
                Chapter = 1,
                Index = 1,
                Name = "はじまりの氷原",
                Description = "敵の城を落とそう。城を半分けずると大きな敵が出てくる。",
                EnemyCastleHp = 3000,
                TargetSeconds = 120f,
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
                Description = "うしろから弓で撃ってくる敵がいる。壁で受け止めよう。",
                EnemyCastleHp = 4000,
                TargetSeconds = 150f,
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
                Description = "重たいすもうペンギンが押してくる。数で押し返そう。",
                EnemyCastleHp = 5000,
                TargetSeconds = 180f,
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
