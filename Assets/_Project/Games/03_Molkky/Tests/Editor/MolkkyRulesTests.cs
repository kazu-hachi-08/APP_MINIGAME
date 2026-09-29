using NUnit.Framework;

namespace MiniGame.Molkky.Tests
{
    public class MolkkyRulesTests
    {
        private static readonly int[] NoPins = new int[0];

        [Test]
        public void 一本倒すとその数字が得点になる()
        {
            var team = new TeamScore();

            ThrowResult result = MolkkyRules.ApplyThrow(team, new[] { 12 });

            Assert.AreEqual(ThrowOutcome.Scored, result.Outcome);
            Assert.AreEqual(12, result.SinglePinNumber);
            Assert.AreEqual(12, team.Score);
        }

        [Test]
        public void 複数本倒すと本数が得点になる()
        {
            var team = new TeamScore();

            MolkkyRules.ApplyThrow(team, new[] { 12, 11, 10 });

            Assert.AreEqual(3, team.Score);
        }

        [Test]
        public void 五十点ちょうどで勝利()
        {
            var team = new TeamScore() { Score = 38 };

            ThrowResult result = MolkkyRules.ApplyThrow(team, new[] { 12 });

            Assert.AreEqual(ThrowOutcome.Win, result.Outcome);
            Assert.AreEqual(50, team.Score);
        }

        [Test]
        public void 五十点を超えると二十五点に戻る()
        {
            var team = new TeamScore() { Score = 45 };

            ThrowResult result = MolkkyRules.ApplyThrow(team, new[] { 6 });

            Assert.AreEqual(ThrowOutcome.OverTo25, result.Outcome);
            Assert.AreEqual(25, team.Score);
        }

        [Test]
        public void 三回連続ミスで失格()
        {
            var team = new TeamScore();

            MolkkyRules.ApplyThrow(team, NoPins);
            MolkkyRules.ApplyThrow(team, NoPins);
            ThrowResult result = MolkkyRules.ApplyThrow(team, NoPins);

            Assert.AreEqual(ThrowOutcome.Disqualified, result.Outcome);
            Assert.IsTrue(team.IsDisqualified);
        }

        [Test]
        public void 得点するとミス回数がリセットされる()
        {
            var team = new TeamScore();

            MolkkyRules.ApplyThrow(team, NoPins);
            MolkkyRules.ApplyThrow(team, NoPins);
            MolkkyRules.ApplyThrow(team, new[] { 1 });
            ThrowResult result = MolkkyRules.ApplyThrow(team, NoPins);

            Assert.AreEqual(ThrowOutcome.Miss, result.Outcome);
            Assert.AreEqual(1, team.MissCount);
            Assert.IsFalse(team.IsDisqualified);
        }

        [Test]
        public void 残り一チームならその番号が勝者()
        {
            var teams = new[] { new TeamScore { IsDisqualified = true }, new TeamScore() };

            Assert.AreEqual(1, MolkkyRules.FindSoleSurvivor(teams));
        }

        [Test]
        public void 二チーム以上残っていれば勝者なし()
        {
            var teams = new[] { new TeamScore(), new TeamScore() };

            Assert.AreEqual(-1, MolkkyRules.FindSoleSurvivor(teams));
        }
    }
}
