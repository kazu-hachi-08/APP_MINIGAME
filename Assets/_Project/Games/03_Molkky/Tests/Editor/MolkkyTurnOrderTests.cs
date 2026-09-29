using System.Collections.Generic;
using NUnit.Framework;

namespace MiniGame.Molkky.Tests
{
    public class MolkkyTurnOrderTests
    {
        private const int A = 0;
        private const int B = 1;

        private static readonly int[] NoPins = new int[0];

        private static TeamScore[] CreateTeams(MolkkyTurnOrder order)
        {
            var teams = new TeamScore[order.TeamCount];
            for (int i = 0; i < teams.Length; i++) teams[i] = new TeamScore();

            return teams;
        }

        /// <summary>最初の人から count 人分の席番号を順に並べる</summary>
        private static List<int> TakeSeats(MolkkyTurnOrder order, TeamScore[] teams, int count)
        {
            var seats = new List<int>();
            for (int i = 0; i < count; i++)
            {
                seats.Add(order.CurrentSeat);
                order.Advance(teams);
            }

            return seats;
        }

        [Test]
        public void 個人戦は席番号順に回る()
        {
            var order = MolkkyTurnOrder.Individual(3);
            TeamScore[] teams = CreateTeams(order);

            CollectionAssert.AreEqual(new[] { 0, 1, 2, 0, 1, 2 }, TakeSeats(order, teams, 6));
        }

        [Test]
        public void 個人戦は失格者を飛ばす()
        {
            var order = MolkkyTurnOrder.Individual(3);
            TeamScore[] teams = CreateTeams(order);
            teams[1].IsDisqualified = true;

            CollectionAssert.AreEqual(new[] { 0, 2, 0, 2 }, TakeSeats(order, teams, 4));
        }

        [Test]
        public void 二対一はチームが交互に投げチーム内で交代する()
        {
            // A = P1・P3、B = P2
            var order = new MolkkyTurnOrder(new[] { A, B, A });
            TeamScore[] teams = CreateTeams(order);

            CollectionAssert.AreEqual(new[] { 0, 1, 2, 1, 0, 1 }, TakeSeats(order, teams, 6));
        }

        [Test]
        public void 二対二はチームが交互に投げチーム内で交代する()
        {
            // A = P1・P3、B = P2・P4
            var order = new MolkkyTurnOrder(new[] { A, B, A, B });
            TeamScore[] teams = CreateTeams(order);

            CollectionAssert.AreEqual(new[] { 0, 1, 2, 3, 0, 1, 2, 3 }, TakeSeats(order, teams, 8));
        }

        [Test]
        public void 三対一は一人チームが毎回投げる()
        {
            // A = P2、B = P1・P3・P4。先攻はチームAなので P2 から
            var order = new MolkkyTurnOrder(new[] { B, A, B, B });
            TeamScore[] teams = CreateTeams(order);

            CollectionAssert.AreEqual(new[] { 1, 0, 1, 2, 1, 3, 1, 0 }, TakeSeats(order, teams, 8));
        }

        [Test]
        public void チーム内の誰が投げてもミスが連続して数えられ失格する()
        {
            // A = P1・P3、B = P2。P1ミス → P2 → P3ミス → P2 → P1ミスで失格
            var order = new MolkkyTurnOrder(new[] { A, B, A });
            TeamScore[] teams = CreateTeams(order);

            ThrowResult result = default;
            for (int i = 0; i < 5; i++)
            {
                int[] fallen = order.CurrentTeam == A ? NoPins : new[] { 1 };
                result = MolkkyRules.ApplyThrow(teams[order.CurrentTeam], fallen);
                if (i < 4) order.Advance(teams);
            }

            Assert.AreEqual(0, order.CurrentSeat);
            Assert.AreEqual(ThrowOutcome.Disqualified, result.Outcome);
            Assert.IsTrue(teams[A].IsDisqualified);
            Assert.AreEqual(B, MolkkyRules.FindSoleSurvivor(teams));
        }

        [Test]
        public void 失格したチームは飛ばしチーム内の交代は続く()
        {
            // 2チームだと片方の失格で試合が終わるので、飛ばす動きは3チーム（P1・P4 / P2 失格 / P3）で見る
            var order = new MolkkyTurnOrder(new[] { 0, 1, 2, 0 });
            TeamScore[] teams = CreateTeams(order);
            teams[1].IsDisqualified = true;

            CollectionAssert.AreEqual(new[] { 0, 2, 3, 2, 0 }, TakeSeats(order, teams, 5));
        }

        [Test]
        public void 投げられるチームがなければ進めない()
        {
            var order = MolkkyTurnOrder.Individual(2);
            TeamScore[] teams = CreateTeams(order);
            teams[0].IsDisqualified = true;
            teams[1].IsDisqualified = true;

            Assert.IsFalse(order.Advance(teams));
        }

        [Test]
        public void 失格勝ちの演出はチームで席番号が一番若い人()
        {
            var order = new MolkkyTurnOrder(new[] { B, A, B, A });

            Assert.AreEqual(1, order.FirstSeatOf(A));
            Assert.AreEqual(0, order.FirstSeatOf(B));
        }
    }
}
