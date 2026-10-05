using System.Collections.Generic;

namespace MiniGame.PenguinWars.Battle
{
    /// <summary>1章のステージ</summary>
    public static partial class StageDefinitions
    {
        private const int Penguin = 1;
        private const int FishSwordPenguin = 12;
        private const int GiantPenguin = 43;

        private static IEnumerable<StageDefinition> Chapter1()
        {
            // 仮のステージ（Phase 1）。壁と安いアタッカーが湧き、城を半分削ると大型が1体出る
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
        }
    }
}
