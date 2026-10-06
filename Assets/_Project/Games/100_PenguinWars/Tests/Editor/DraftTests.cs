using System;
using System.Collections.Generic;
using MiniGame.PenguinWars.Battle;
using NUnit.Framework;

namespace MiniGame.PenguinWars.Battle.Tests
{
    /// <summary>ドラフト（仕様書 §6）</summary>
    public class DraftTests
    {
        private const int PoolSize = 50;
        private const int Rounds = 10;
        private const int OfferSize = 3;
        private const float PickTime = 15f;
        private const float SmallStep = 0.1f;

        private static DraftSession Create(int seed = 1)
        {
            var pool = new List<int>();
            for (int no = 1; no <= PoolSize; no++) pool.Add(no);
            return new DraftSession(pool, Rounds, OfferSize, PickTime, new Random(seed));
        }

        [Test]
        public void TenRounds_GiveTenUnitsEach()
        {
            DraftSession draft = Create();
            while (!draft.IsComplete)
            {
                Assert.AreEqual(OfferSize, draft.GetOffer(Side.Left).Count);
                Assert.AreEqual(OfferSize, draft.GetOffer(Side.Right).Count);
                draft.Pick(Side.Left, 0);
                draft.Pick(Side.Right, 2);
                Assert.IsTrue(draft.Tick(SmallStep));
            }

            Assert.AreEqual(Rounds, draft.GetPicks(Side.Left).Count);
            Assert.AreEqual(Rounds, draft.GetPicks(Side.Right).Count);
        }

        [Test]
        public void OwnPicks_AreNeverOfferedAgain()
        {
            // シードを変えて何回か回し、たまたま出なかっただけの合格を防ぐ
            for (int seed = 0; seed < 20; seed++)
            {
                DraftSession draft = Create(seed);
                while (!draft.IsComplete)
                {
                    foreach (int offered in draft.GetOffer(Side.Left))
                    {
                        CollectionAssert.DoesNotContain(draft.GetPicks(Side.Left), offered);
                    }
                    CollectionAssert.AllItemsAreUnique(draft.GetOffer(Side.Left));
                    draft.Pick(Side.Left, 1);
                    draft.Pick(Side.Right, 1);
                    draft.Tick(SmallStep);
                }
                CollectionAssert.AllItemsAreUnique(draft.GetPicks(Side.Left));
            }
        }

        [Test]
        public void TimeUp_PicksFromOfferAutomatically()
        {
            DraftSession draft = Create();
            var leftOffer = new List<int>(draft.GetOffer(Side.Left));
            var rightOffer = new List<int>(draft.GetOffer(Side.Right));

            Assert.IsFalse(draft.Tick(PickTime - SmallStep));
            Assert.IsTrue(draft.Tick(SmallStep * 2f));

            Assert.AreEqual(1, draft.Round);
            CollectionAssert.Contains(leftOffer, draft.GetPicks(Side.Left)[0]);
            CollectionAssert.Contains(rightOffer, draft.GetPicks(Side.Right)[0]);
        }

        [Test]
        public void TimeUp_KeepsManualPick()
        {
            DraftSession draft = Create();
            int chosen = draft.GetOffer(Side.Left)[2];
            draft.Pick(Side.Left, 2);

            draft.Tick(PickTime + SmallStep);

            Assert.AreEqual(chosen, draft.GetPicks(Side.Left)[0]);
            Assert.AreEqual(1, draft.GetPicks(Side.Right).Count);
        }

        [Test]
        public void OnlyOneSidePicked_DoesNotAdvance()
        {
            DraftSession draft = Create();
            draft.Pick(Side.Left, 0);

            Assert.IsFalse(draft.Tick(SmallStep));
            Assert.AreEqual(0, draft.Round);
            Assert.IsTrue(draft.HasPicked(Side.Left));
            Assert.IsFalse(draft.HasPicked(Side.Right));
        }

        [Test]
        public void SecondPickInSameRound_IsRejected()
        {
            DraftSession draft = Create();
            Assert.IsTrue(draft.Pick(Side.Right, 0));
            Assert.IsFalse(draft.Pick(Side.Right, 1));
            Assert.IsFalse(draft.Pick(Side.Left, OfferSize));

            Assert.AreEqual(1, draft.GetPicks(Side.Right).Count);
            Assert.AreEqual(0, draft.GetPicks(Side.Left).Count);
        }

        [Test]
        public void NewRound_ResetsTimer()
        {
            DraftSession draft = Create();
            draft.Tick(PickTime * 0.5f);
            draft.Pick(Side.Left, 0);
            draft.Pick(Side.Right, 0);
            draft.Tick(SmallStep);

            Assert.AreEqual(PickTime, draft.RemainingTime, 1e-4f);
        }
    }
}
