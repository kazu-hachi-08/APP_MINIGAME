using System.Collections.Generic;
using MiniGame.PenguinWars.Battle;
using NUnit.Framework;

namespace MiniGame.PenguinWars.Battle.Tests
{
    /// <summary>ステージの定義表の書き間違い（ID の重複・存在しないキャラ）を、遊ぶ前に見つける</summary>
    public class StageDefinitionTests
    {
        private const int MinUnitNo = 1;
        private const int MaxUnitNo = 50;

        [Test]
        public void All_HasAtLeastOneStage()
        {
            Assert.That(StageDefinitions.All.Count, Is.GreaterThan(0));
        }

        [Test]
        public void Ids_AreUnique()
        {
            var ids = new HashSet<string>();
            foreach (StageDefinition stage in StageDefinitions.All)
            {
                Assert.IsTrue(ids.Add(stage.Id), $"ID {stage.Id} が重複しています");
            }
        }

        [Test]
        public void Find_ReturnsStageById()
        {
            StageDefinition first = StageDefinitions.All[0];

            Assert.AreSame(first, StageDefinitions.Find(first.Id));
            Assert.IsNull(StageDefinitions.Find("no-such-stage"));
        }

        [Test]
        public void EnemyUnitNos_AreInRoster()
        {
            foreach (StageDefinition stage in StageDefinitions.All)
            {
                foreach (EnemySpawnEntry entry in stage.Entries)
                {
                    Assert.That(entry.UnitNo, Is.InRange(MinUnitNo, MaxUnitNo), $"ステージ {stage.Id} の敵 No.{entry.UnitNo}");
                }
            }
        }

        [Test]
        public void CastleHpAndFieldLength_ArePositive()
        {
            foreach (StageDefinition stage in StageDefinitions.All)
            {
                Assert.That(stage.EnemyCastleHp, Is.GreaterThan(0), stage.Id);
                Assert.That(stage.PlayerCastleHp, Is.GreaterThan(0), stage.Id);
                Assert.That(stage.FieldLength, Is.GreaterThan(0f), stage.Id);
            }
        }
    }
}
