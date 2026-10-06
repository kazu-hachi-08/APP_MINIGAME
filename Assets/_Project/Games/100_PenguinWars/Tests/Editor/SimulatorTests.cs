using System.Collections.Generic;
using MiniGame.PenguinWars.Battle;
using NUnit.Framework;

namespace MiniGame.PenguinWars.Battle.Tests
{
    /// <summary>ステージ検証ツール（StageSimulator・SimDeckPicker）。全ステージを回すのは重いので、テストは 1-1 だけ</summary>
    public class SimulatorTests
    {
        private const string FirstStageId = "1-1";
        private const int Seed = 1;
        private const float MaxSeconds = 600f;

        private static StageDefinition FirstStage => StageDefinitions.Find(FirstStageId);

        [Test]
        public void SameSeed_GivesSameResult()
        {
            List<int> deck = SimDeckPicker.Pick(FirstStage);

            StageResult first = StageSimulator.CreateDefault().Run(FirstStage, deck, BotSkill.Normal, Seed, MaxSeconds);
            StageResult second = StageSimulator.CreateDefault().Run(FirstStage, deck, BotSkill.Normal, Seed, MaxSeconds);

            Assert.AreEqual(first.Cleared, second.Cleared);
            Assert.AreEqual(first.ElapsedSeconds, second.ElapsedSeconds);
            Assert.AreEqual(first.PlayerCastleHpRatio, second.PlayerCastleHpRatio);
            Assert.AreEqual(first.KillCount, second.KillCount);
        }

        /// <summary>最初のステージで詰まないことの保証</summary>
        [Test]
        public void FirstStage_NormalBot_Clears()
        {
            StageResult result = StageSimulator.CreateDefault().Run(FirstStage, SimDeckPicker.Pick(FirstStage), BotSkill.Normal, Seed, MaxSeconds);

            Assert.IsTrue(result.Cleared, $"{result.ElapsedSeconds:0}秒 自城HP {result.PlayerCastleHpRatio:0%}");
        }

        [Test]
        public void TimeLimit_ReturnsNotClearedWithCastleLeft()
        {
            const float tooShort = 5f;

            StageResult result = StageSimulator.CreateDefault().Run(FirstStage, SimDeckPicker.Pick(FirstStage), BotSkill.Normal, Seed, tooShort);

            Assert.IsFalse(result.Cleared);
            Assert.Greater(result.PlayerCastleHpRatio, 0f);
        }

        [Test]
        public void AvailableNos_FirstStage_IsInitialUnits()
        {
            CollectionAssert.AreEquivalent(CampaignUnlocks.InitialNos, SimDeckPicker.AvailableNos(FirstStage));
        }

        [Test]
        public void AvailableNos_LaterStage_IncludesEarlierUnlocks()
        {
            StageDefinition second = StageDefinitions.Next(FirstStageId);

            List<int> nos = SimDeckPicker.AvailableNos(second);

            foreach (int no in FirstStage.UnlockNos) CollectionAssert.Contains(nos, no);
            foreach (int no in second.UnlockNos) CollectionAssert.DoesNotContain(nos, no);
        }

        [Test]
        public void Pick_TakesRoleQuotaByHighestCost()
        {
            // 壁4体から高い3体、残りの役割は1体ずつしかいないので、目安で埋まらない枠は残りの壁で埋まる
            var stage = new StageDefinition { Id = "test" };
            int[] walls = NosOfRole(UnitRole.Wall, 4);

            List<int> deck = SimDeckPicker.Pick(walls, stage);

            Assert.AreEqual(4, deck.Count);
            Assert.AreEqual(walls[3], deck[3], "目安（壁3）から外れた一番安い壁は最後に入る");
        }

        [Test]
        public void Pick_SkipsUnitsBannedByStage()
        {
            var stage = new StageDefinition { Id = "test", BannedRoles = new[] { UnitRole.Wall } };

            List<int> deck = SimDeckPicker.Pick(CampaignUnlocks.InitialNos, stage);

            foreach (int no in deck) Assert.IsTrue(DeckRules.IsAllowed(stage, no));
            Assert.Less(deck.Count, DeckRules.DeckSize);
        }

        /// <summary>定義表からその役割のキャラを count 体、コストの高い順に</summary>
        private static int[] NosOfRole(UnitRole role, int count)
        {
            var units = new List<UnitDefinition>();
            foreach (UnitDefinition unit in UnitDefinitions.All)
            {
                if (unit.Role == role) units.Add(unit);
            }
            units.Sort((a, b) => b.Cost != a.Cost ? b.Cost.CompareTo(a.Cost) : a.No.CompareTo(b.No));
            var nos = new int[count];
            for (int i = 0; i < count; i++) nos[i] = units[i].No;
            return nos;
        }
    }
}
