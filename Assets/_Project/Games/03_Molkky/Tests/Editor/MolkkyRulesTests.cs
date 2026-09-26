using NUnit.Framework;

namespace MiniGame.Molkky.Tests
{
    public class MolkkyRulesTests
    {
        [Test]
        public void 一本倒すとその数字が得点になる()
        {
            var player = new PlayerSlot("P1");

            ThrowResult result = MolkkyRules.ApplyThrow(player, new[] { 12 });

            Assert.AreEqual(ThrowOutcome.Scored, result.Outcome);
            Assert.AreEqual(12, result.SinglePinNumber);
            Assert.AreEqual(12, player.Score);
        }

        [Test]
        public void 複数本倒すと本数が得点になる()
        {
            var player = new PlayerSlot("P1");

            MolkkyRules.ApplyThrow(player, new[] { 12, 11, 10 });

            Assert.AreEqual(3, player.Score);
        }

        [Test]
        public void 五十点ちょうどで勝利()
        {
            var player = new PlayerSlot("P1") { Score = 38 };

            ThrowResult result = MolkkyRules.ApplyThrow(player, new[] { 12 });

            Assert.AreEqual(ThrowOutcome.Win, result.Outcome);
            Assert.AreEqual(50, player.Score);
        }

        [Test]
        public void 五十点を超えると二十五点に戻る()
        {
            var player = new PlayerSlot("P1") { Score = 45 };

            ThrowResult result = MolkkyRules.ApplyThrow(player, new[] { 6 });

            Assert.AreEqual(ThrowOutcome.OverTo25, result.Outcome);
            Assert.AreEqual(25, player.Score);
        }

        [Test]
        public void 三回連続ミスで失格()
        {
            var player = new PlayerSlot("P1");

            MolkkyRules.ApplyThrow(player, new int[0]);
            MolkkyRules.ApplyThrow(player, new int[0]);
            ThrowResult result = MolkkyRules.ApplyThrow(player, new int[0]);

            Assert.AreEqual(ThrowOutcome.Disqualified, result.Outcome);
            Assert.IsTrue(player.IsDisqualified);
        }

        [Test]
        public void 得点するとミス回数がリセットされる()
        {
            var player = new PlayerSlot("P1");

            MolkkyRules.ApplyThrow(player, new int[0]);
            MolkkyRules.ApplyThrow(player, new int[0]);
            MolkkyRules.ApplyThrow(player, new[] { 1 });
            ThrowResult result = MolkkyRules.ApplyThrow(player, new int[0]);

            Assert.AreEqual(ThrowOutcome.Miss, result.Outcome);
            Assert.AreEqual(1, player.MissCount);
            Assert.IsFalse(player.IsDisqualified);
        }

        [Test]
        public void 次の手番は失格者を飛ばす()
        {
            var players = new[] { new PlayerSlot("P1"), new PlayerSlot("P2") { IsDisqualified = true }, new PlayerSlot("P3") };

            Assert.AreEqual(2, MolkkyRules.NextPlayerIndex(players, 0));
            Assert.AreEqual(0, MolkkyRules.NextPlayerIndex(players, 2));
        }

        [Test]
        public void 残り一人ならその人が勝者()
        {
            var survivor = new PlayerSlot("P2");
            var players = new[] { new PlayerSlot("P1") { IsDisqualified = true }, survivor };

            Assert.AreSame(survivor, MolkkyRules.FindSoleSurvivor(players));
        }

        [Test]
        public void 二人以上残っていれば勝者なし()
        {
            var players = new[] { new PlayerSlot("P1"), new PlayerSlot("P2") };

            Assert.IsNull(MolkkyRules.FindSoleSurvivor(players));
        }
    }
}
