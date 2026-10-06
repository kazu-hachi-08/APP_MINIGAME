using System.Collections.Generic;
using MiniGame.PenguinWars.Battle;
using NUnit.Framework;

namespace MiniGame.PenguinWars.Battle.Tests
{
    /// <summary>編成のきまり・補完・保存（ステージ計画 Phase 3）</summary>
    public class DeckRulesTests
    {
        private const int UnknownNo = 9999;

        private static List<int> Initial => new List<int>(CampaignUnlocks.InitialNos);

        // ---- IsValid ----

        [Test]
        public void IsValid_TenUniqueUnlocked_IsTrue()
        {
            Assert.IsTrue(DeckRules.IsValid(Initial, Initial));
        }

        [Test]
        public void IsValid_NineUnits_IsFalse()
        {
            List<int> deck = Initial;
            deck.RemoveAt(0);

            Assert.IsFalse(DeckRules.IsValid(deck, Initial));
        }

        [Test]
        public void IsValid_ElevenUnits_IsFalse()
        {
            List<int> unlocked = Initial;
            int extra = StageDefinitions.All[0].UnlockNos[0];
            unlocked.Add(extra);
            List<int> deck = Initial;
            deck.Add(extra);

            Assert.IsFalse(DeckRules.IsValid(deck, unlocked));
        }

        [Test]
        public void IsValid_Duplicate_IsFalse()
        {
            List<int> deck = Initial;
            deck[1] = deck[0];

            Assert.IsFalse(DeckRules.IsValid(deck, Initial));
        }

        [Test]
        public void IsValid_LockedUnit_IsFalse()
        {
            List<int> deck = Initial;
            deck[0] = StageDefinitions.All[0].UnlockNos[0];

            Assert.IsFalse(DeckRules.IsValid(deck, Initial));
        }

        [Test]
        public void IsValid_Null_IsFalse()
        {
            Assert.IsFalse(DeckRules.IsValid(null, Initial));
        }

        // ---- FillDefault / CurrentDeck ----

        [Test]
        public void FillDefault_Empty_FillsCheapestUnlocked()
        {
            List<int> deck = DeckRules.FillDefault(new List<int>(), Initial);

            Assert.IsTrue(DeckRules.IsValid(deck, Initial));
        }

        [Test]
        public void FillDefault_LockedAndUnknownUnits_AreReplaced()
        {
            List<int> saved = Initial;
            int locked = StageDefinitions.All[0].UnlockNos[0];
            saved[0] = locked;
            saved[1] = UnknownNo;

            List<int> deck = DeckRules.FillDefault(saved, Initial);

            Assert.IsTrue(DeckRules.IsValid(deck, Initial));
            CollectionAssert.DoesNotContain(deck, locked);
            CollectionAssert.DoesNotContain(deck, UnknownNo);
        }

        [Test]
        public void FillDefault_KeepsSavedUnitsFirst()
        {
            var progress = new CampaignProgress();
            progress.Record(StageDefinitions.All[0].Id, StarFlags.Clear, 100f);
            List<int> unlocked = CampaignUnlocks.UnlockedNos(progress);
            // 解放したばかりのキャラを入れた編成は、補完しても外されない
            List<int> saved = Initial;
            saved[0] = StageDefinitions.All[0].UnlockNos[0];

            List<int> deck = DeckRules.FillDefault(saved, unlocked);

            CollectionAssert.AreEquivalent(saved, deck);
        }

        [Test]
        public void FillDefault_FewerUnlockedThanDeckSize_ReturnsAll()
        {
            var unlocked = new List<int> { 1, 2, 3 };

            List<int> deck = DeckRules.FillDefault(null, unlocked);

            CollectionAssert.AreEquivalent(unlocked, deck);
        }

        [Test]
        public void CurrentDeck_SortedByCost()
        {
            List<int> deck = DeckRules.CurrentDeck(new CampaignProgress());

            var costByNo = new Dictionary<int, int>();
            foreach (UnitDefinition unit in UnitDefinitions.All) costByNo[unit.No] = unit.Cost;
            for (int i = 1; i < deck.Count; i++)
            {
                Assert.That(costByNo[deck[i - 1]], Is.LessThanOrEqualTo(costByNo[deck[i]]));
            }
        }

        // ---- 保存 ----

        [Test]
        public void LastDeck_JsonRoundTrip()
        {
            var progress = new CampaignProgress { LastDeckNos = Initial };

            CampaignProgress loaded = CampaignProgress.FromJson(progress.ToJson());

            CollectionAssert.AreEqual(Initial, loaded.LastDeckNos);
        }

        [Test]
        public void LastDeck_OldSaveWithoutDeck_IsEmpty()
        {
            CampaignProgress loaded = CampaignProgress.FromJson("{\"version\":1,\"stages\":{}}");

            CollectionAssert.IsEmpty(loaded.LastDeckNos);
            Assert.IsTrue(DeckRules.IsValid(DeckRules.CurrentDeck(loaded), CampaignUnlocks.UnlockedNos(loaded)));
        }
    }
}
