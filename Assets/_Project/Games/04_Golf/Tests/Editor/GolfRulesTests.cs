using System;
using System.Collections.Generic;
using System.Numerics;
using NUnit.Framework;

namespace MiniGame.Golf.Tests
{
    public class GolfRulesTests
    {
        private static readonly Vector2 Cup = new Vector2(0f, 20f);
        private static readonly Vector2 Tee = Vector2.Zero;

        private static List<GolfPlayerSlot> CreateSlots(int count)
        {
            var slots = new List<GolfPlayerSlot>();
            for (int i = 0; i < count; i++)
            {
                var slot = new GolfPlayerSlot(i);
                slot.StartHole(Tee);
                slots.Add(slot);
            }

            return slots;
        }

        private static void Shoot(GolfPlayerSlot slot, Vector2 stoppedAt)
        {
            slot.AddStrokes(1);
            slot.Position = stoppedAt;
        }

        [Test]
        public void ティーショットはティーの順番で打つ()
        {
            List<GolfPlayerSlot> slots = CreateSlots(3);
            var teeOrder = new List<int> { 2, 0, 1 };

            Assert.AreEqual(2, GolfRules.NextPlayer(slots, teeOrder, Cup));

            // 先に打った人がカップの近くに止まっても、まだ打っていない人のティーショットが先
            Shoot(slots[2], new Vector2(0f, 19f));
            Assert.AreEqual(0, GolfRules.NextPlayer(slots, teeOrder, Cup));
        }

        [Test]
        public void 全員打った後はカップから遠い人から打つ()
        {
            List<GolfPlayerSlot> slots = CreateSlots(2);
            var teeOrder = new List<int> { 0, 1 };
            Shoot(slots[0], new Vector2(0f, 15f));
            Shoot(slots[1], new Vector2(0f, 10f));

            Assert.AreEqual(1, GolfRules.NextPlayer(slots, teeOrder, Cup));

            Shoot(slots[1], new Vector2(0f, 18f));
            Assert.AreEqual(0, GolfRules.NextPlayer(slots, teeOrder, Cup));
        }

        [Test]
        public void 同じ距離ならティーの順番が先の人()
        {
            List<GolfPlayerSlot> slots = CreateSlots(2);
            var teeOrder = new List<int> { 1, 0 };
            Shoot(slots[0], new Vector2(-3f, 20f));
            Shoot(slots[1], new Vector2(3f, 20f));

            Assert.AreEqual(1, GolfRules.NextPlayer(slots, teeOrder, Cup));
        }

        [Test]
        public void カップインした人は飛ばし全員終わると負の値()
        {
            List<GolfPlayerSlot> slots = CreateSlots(2);
            var teeOrder = new List<int> { 0, 1 };
            Shoot(slots[0], new Vector2(0f, 5f));
            Shoot(slots[1], Cup);
            slots[1].HoleOut();

            Assert.AreEqual(0, GolfRules.NextPlayer(slots, teeOrder, Cup));

            slots[0].GiveUp(6);
            Assert.AreEqual(-1, GolfRules.NextPlayer(slots, teeOrder, Cup));
        }

        [Test]
        public void パーの2倍で打ち切り()
        {
            Assert.IsFalse(GolfRules.ShouldGiveUp(5, 3));
            Assert.IsTrue(GolfRules.ShouldGiveUp(6, 3));
            Assert.IsTrue(GolfRules.ShouldGiveUp(7, 3));
            Assert.AreEqual(8, GolfRules.StrokeLimit(4));
        }

        [Test]
        public void スコアはホールごとに記録され合計される()
        {
            var slot = new GolfPlayerSlot(0);
            slot.StartHole(Tee);
            slot.AddStrokes(4);
            slot.HoleOut();

            slot.StartHole(Tee);
            slot.AddStrokes(9);
            slot.GiveUp(GolfRules.StrokeLimit(4));

            CollectionAssert.AreEqual(new[] { 4, 8 }, slot.HoleScores);
            Assert.AreEqual(12, slot.Total);
            Assert.IsTrue(slot.IsGivenUp);
        }

        [Test]
        public void 次のホールは打数が少ない順で同じなら前の順番()
        {
            List<GolfPlayerSlot> slots = CreateSlots(3);
            int[] scores = { 5, 3, 5 };
            for (int i = 0; i < slots.Count; i++)
            {
                slots[i].AddStrokes(scores[i]);
                slots[i].HoleOut();
            }

            List<int> order = GolfRules.NextTeeOrder(new List<int> { 2, 0, 1 }, slots);

            CollectionAssert.AreEqual(new[] { 1, 2, 0 }, order);
        }

        [Test]
        public void 同じ合計打数は同じ順位()
        {
            int[] ranks = GolfRules.Ranks(new[] { 12, 10, 12, 15 });

            CollectionAssert.AreEqual(new[] { 2, 1, 2, 4 }, ranks);
        }

        [Test]
        public void ホールは重複なしで選ばれる()
        {
            var random = new Random(1);
            for (int trial = 0; trial < 50; trial++)
            {
                List<int> holes = GolfRules.PickHoles(5, 3, random);

                Assert.AreEqual(3, holes.Count);
                CollectionAssert.AllItemsAreUnique(holes);
                Assert.That(holes, Has.All.InRange(0, 4));
            }
        }

        [Test]
        public void 登録ホールより多くは選べない()
        {
            Assert.Throws<ArgumentException>(() => GolfRules.PickHoles(2, 3, new Random(1)));
        }

        [TestCase(2, 4, "イーグル！")]
        [TestCase(3, 4, "バーディー！")]
        [TestCase(4, 4, "パー")]
        [TestCase(5, 4, "ボギー")]
        [TestCase(6, 4, "ダブルボギー")]
        [TestCase(8, 4, "+4")]
        public void カップインの呼び名はパーとの差で決まる(int strokes, int par, string expected)
        {
            Assert.AreEqual(expected, GolfRules.ScoreName(strokes, par));
        }

        [Test]
        public void 一打目のカップインはホールインワン()
        {
            Assert.AreEqual("ホールインワン！", GolfRules.ScoreName(1, 3));
        }
    }
}
