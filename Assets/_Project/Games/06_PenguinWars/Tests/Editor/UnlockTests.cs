using System.Collections.Generic;
using MiniGame.PenguinWars.Battle;
using NUnit.Framework;

namespace MiniGame.PenguinWars.Battle.Tests
{
    /// <summary>キャラ解放（ステージ計画 Phase 3）</summary>
    public class UnlockTests
    {
        private const int InitialCount = 10;
        private const int UnitCount = 50;
        private const int UnknownNo = 9999;

        private static StageDefinition First => StageDefinitions.All[0];

        [Test]
        public void AtStart_OnlyInitialTenAreUnlocked()
        {
            var progress = new CampaignProgress();

            List<int> unlocked = CampaignUnlocks.UnlockedNos(progress);

            Assert.AreEqual(InitialCount, unlocked.Count);
            CollectionAssert.AreEquivalent(CampaignUnlocks.InitialNos, unlocked);
        }

        [Test]
        public void InitialTen_HaveNoLargeUnits()
        {
            var initial = new List<int>(CampaignUnlocks.InitialNos);
            foreach (UnitDefinition unit in UnitDefinitions.All)
            {
                if (!initial.Contains(unit.No)) continue;
                Assert.AreNotEqual(UnitRole.Large, unit.Role, $"No.{unit.No}");
            }
        }

        [Test]
        public void ClearingStage_UnlocksItsUnits()
        {
            var progress = new CampaignProgress();
            Assert.That(First.UnlockNos.Count, Is.GreaterThan(0));
            foreach (int no in First.UnlockNos) Assert.IsFalse(CampaignUnlocks.IsUnlocked(progress, no), $"No.{no}");

            progress.Record(First.Id, StarFlags.Clear, 100f);

            foreach (int no in First.UnlockNos) Assert.IsTrue(CampaignUnlocks.IsUnlocked(progress, no), $"No.{no}");
        }

        [Test]
        public void Record_FirstClear_ReturnsNewUnlocks_SecondClearReturnsNone()
        {
            var progress = new CampaignProgress();

            StageRecordChange first = progress.Record(First.Id, StarFlags.Clear, 100f);
            StageRecordChange second = progress.Record(First.Id, StarFlags.Clear, 90f);

            CollectionAssert.AreEquivalent(First.UnlockNos, first.NewUnlockNos);
            CollectionAssert.IsEmpty(second.NewUnlockNos);
        }

        [Test]
        public void IsUnlocked_UnknownNo_IsFalse()
        {
            Assert.IsFalse(CampaignUnlocks.IsUnlocked(new CampaignProgress(), UnknownNo));
        }

        [Test]
        public void InitialAndStageUnlocks_HaveNoDuplicates_AndExistInRoster()
        {
            var seen = new HashSet<int>();
            var roster = new HashSet<int>();
            foreach (UnitDefinition unit in UnitDefinitions.All) roster.Add(unit.No);

            foreach (int no in CampaignUnlocks.InitialNos) Assert.IsTrue(seen.Add(no), $"初期キャラ No.{no} が重複");
            foreach (StageDefinition stage in StageDefinitions.All)
            {
                foreach (int no in stage.UnlockNos) Assert.IsTrue(seen.Add(no), $"{stage.Id} の No.{no} が重複");
            }
            foreach (int no in seen) Assert.IsTrue(roster.Contains(no), $"No.{no} が定義表に無い");
        }

        /// <summary>全50体がステージのどこかで仲間になる（取り逃がし・二重の解放がない）</summary>
        [Test]
        public void InitialAndStageUnlocks_CoverAllUnitsExactlyOnce()
        {
            var nos = new List<int>(CampaignUnlocks.InitialNos);
            foreach (StageDefinition stage in StageDefinitions.All) nos.AddRange(stage.UnlockNos);
            nos.Sort();

            var expected = new List<int>();
            for (int no = 1; no <= UnitCount; no++) expected.Add(no);
            CollectionAssert.AreEqual(expected, nos);
        }

        [Test]
        public void FindUnlockStage_ReturnsStage_OrNullForInitial()
        {
            Assert.AreSame(First, CampaignUnlocks.FindUnlockStage(First.UnlockNos[0]));
            Assert.IsNull(CampaignUnlocks.FindUnlockStage(CampaignUnlocks.InitialNos[0]));
        }
    }
}
