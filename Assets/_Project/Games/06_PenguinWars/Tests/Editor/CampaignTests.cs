using MiniGame.PenguinWars.Battle;
using NUnit.Framework;

namespace MiniGame.PenguinWars.Battle.Tests
{
    /// <summary>ステージの解放・★の判定と保存（ステージ計画 Phase 2）</summary>
    public class CampaignTests
    {
        private const string FirstId = "1-1";
        private const string SecondId = "1-2";
        private const string ThirdId = "1-3";
        private const float TargetSeconds = 100f;
        private const float SafeRatio = 0.5f;

        private static StageDefinition MakeStage() => new StageDefinition { TargetSeconds = TargetSeconds, SafeHpRatio = SafeRatio };

        private static StageResult Cleared(float seconds, float hpRatio) => new StageResult(true, seconds, hpRatio, 0);

        // ---- 解放 ----

        [Test]
        public void IsPlayable_AtStart_OnlyFirstStage()
        {
            var progress = new CampaignProgress();

            Assert.IsTrue(progress.IsPlayable(FirstId));
            Assert.IsFalse(progress.IsPlayable(SecondId));
            Assert.IsFalse(progress.IsPlayable(ThirdId));
        }

        [Test]
        public void IsPlayable_AfterClearingFirst_OpensSecondOnly()
        {
            var progress = new CampaignProgress();

            progress.Record(FirstId, StarFlags.Clear, TargetSeconds);

            Assert.IsTrue(progress.IsPlayable(SecondId));
            Assert.IsFalse(progress.IsPlayable(ThirdId));
        }

        [Test]
        public void IsPlayable_UnknownId_IsFalse()
        {
            Assert.IsFalse(new CampaignProgress().IsPlayable("no-such-stage"));
        }

        [Test]
        public void Next_ReturnsFollowingStage_AndNullAtEnd()
        {
            Assert.AreEqual(SecondId, StageDefinitions.Next(FirstId).Id);
            string lastId = StageDefinitions.All[StageDefinitions.All.Count - 1].Id;
            Assert.IsNull(StageDefinitions.Next(lastId));
        }

        // ---- ★の判定 ----

        [Test]
        public void Evaluate_NotCleared_NoStars()
        {
            var result = new StageResult(false, 1f, 1f, 0);

            Assert.AreEqual(StarFlags.None, StarRule.Evaluate(MakeStage(), result));
        }

        [Test]
        public void Evaluate_ExactlySafeRatio_GetsSafeStar()
        {
            StarFlags stars = StarRule.Evaluate(MakeStage(), Cleared(TargetSeconds + 1f, SafeRatio));

            Assert.AreEqual(StarFlags.Clear | StarFlags.Safe, stars);
        }

        [Test]
        public void Evaluate_JustBelowSafeRatio_NoSafeStar()
        {
            StarFlags stars = StarRule.Evaluate(MakeStage(), Cleared(TargetSeconds + 1f, SafeRatio - 0.001f));

            Assert.AreEqual(StarFlags.Clear, stars);
        }

        [Test]
        public void Evaluate_ExactlyTargetTime_GetsFastStar()
        {
            StarFlags stars = StarRule.Evaluate(MakeStage(), Cleared(TargetSeconds, 0f));

            Assert.AreEqual(StarFlags.Clear | StarFlags.Fast, stars);
        }

        [Test]
        public void Evaluate_JustOverTargetTime_NoFastStar()
        {
            StarFlags stars = StarRule.Evaluate(MakeStage(), Cleared(TargetSeconds + 0.01f, 0f));

            Assert.AreEqual(StarFlags.Clear, stars);
        }

        [Test]
        public void Count_CountsEachStar()
        {
            Assert.AreEqual(0, StarRule.Count(StarFlags.None));
            Assert.AreEqual(2, StarRule.Count(StarFlags.Clear | StarFlags.Fast));
            Assert.AreEqual(StarRule.MaxStars, StarRule.Count(StarFlags.All));
        }

        // ---- 記録 ----

        [Test]
        public void Record_WorseRunAfterBest_KeepsAllStars()
        {
            var progress = new CampaignProgress();
            progress.Record(FirstId, StarFlags.All, 50f);

            progress.Record(FirstId, StarFlags.Clear, 200f);

            Assert.AreEqual(StarFlags.All, progress.GetStars(FirstId));
        }

        [Test]
        public void Record_StarsFromSeparateRuns_AreCombined()
        {
            var progress = new CampaignProgress();
            progress.Record(FirstId, StarFlags.Clear | StarFlags.Safe, 200f);

            StageRecordChange change = progress.Record(FirstId, StarFlags.Clear | StarFlags.Fast, 50f);

            Assert.AreEqual(StarFlags.All, progress.GetStars(FirstId));
            Assert.AreEqual(StarFlags.Fast, change.NewStars);
        }

        [Test]
        public void Record_FirstClear_IsReportedOnce()
        {
            var progress = new CampaignProgress();

            Assert.IsTrue(progress.Record(FirstId, StarFlags.Clear, 80f).IsFirstClear);
            Assert.IsFalse(progress.Record(FirstId, StarFlags.Clear, 80f).IsFirstClear);
        }

        [Test]
        public void Record_BestTime_KeepsShortest()
        {
            var progress = new CampaignProgress();

            Assert.IsTrue(progress.Record(FirstId, StarFlags.Clear, 80f).IsNewBest);
            Assert.IsFalse(progress.Record(FirstId, StarFlags.Clear, 90f).IsNewBest);
            Assert.IsTrue(progress.Record(FirstId, StarFlags.Clear, 70f).IsNewBest);
            Assert.AreEqual(70f, progress.GetBestSeconds(FirstId));
        }

        [Test]
        public void GetBestSeconds_NeverCleared_IsNull()
        {
            Assert.IsNull(new CampaignProgress().GetBestSeconds(FirstId));
        }

        // ---- 保存形式 ----

        [Test]
        public void Json_RoundTrip_KeepsEverything()
        {
            var progress = new CampaignProgress { LastPlayedId = SecondId };
            progress.Record(FirstId, StarFlags.All, 83.25f);
            progress.Record(SecondId, StarFlags.Clear | StarFlags.Safe, 120.5f);

            CampaignProgress loaded = CampaignProgress.FromJson(progress.ToJson());

            Assert.AreEqual(SecondId, loaded.LastPlayedId);
            Assert.AreEqual(StarFlags.All, loaded.GetStars(FirstId));
            Assert.AreEqual(83.25f, loaded.GetBestSeconds(FirstId));
            Assert.AreEqual(StarFlags.Clear | StarFlags.Safe, loaded.GetStars(SecondId));
            Assert.AreEqual(120.5f, loaded.GetBestSeconds(SecondId));
            Assert.IsTrue(loaded.IsPlayable(ThirdId));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("{")]
        [TestCase("not json")]
        [TestCase("{\"stages\":{\"1-1\":")]
        [TestCase("[1,2,3]")]
        [TestCase("{\"stages\":[1],\"lastPlayed\":5}")]
        public void FromJson_Broken_StartsEmpty(string json)
        {
            CampaignProgress progress = null;

            Assert.DoesNotThrow(() => progress = CampaignProgress.FromJson(json));
            Assert.IsFalse(progress.IsCleared(FirstId));
            Assert.IsTrue(progress.IsPlayable(FirstId));
            Assert.IsNull(progress.LastPlayedId);
        }

        [Test]
        public void FromJson_RemovedStageId_DoesNotBreakAndIsKept()
        {
            const string removedId = "9-9";
            var progress = new CampaignProgress { LastPlayedId = removedId };
            progress.Record(removedId, StarFlags.All, 10f);

            CampaignProgress loaded = CampaignProgress.FromJson(progress.ToJson());

            Assert.IsFalse(loaded.IsPlayable(removedId));
            Assert.AreEqual(StarFlags.All, loaded.GetStars(removedId));
            Assert.IsTrue(loaded.IsPlayable(FirstId));
        }
    }
}
