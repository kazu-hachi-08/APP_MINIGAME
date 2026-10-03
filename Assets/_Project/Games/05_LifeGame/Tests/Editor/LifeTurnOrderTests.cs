using System.Collections.Generic;
using NUnit.Framework;

namespace MiniGame.LifeGame.Tests
{
    public class LifeTurnOrderTests
    {
        private static List<LifePlayerState> Players(params bool[] goaled)
        {
            var players = new List<LifePlayerState>();
            for (int seat = 0; seat < goaled.Length; seat++)
            {
                players.Add(new LifePlayerState { Seat = seat, GoalOrder = goaled[seat] ? seat : LifePlayerState.NotGoaled });
            }

            return players;
        }

        [Test]
        public void 席順に回る()
        {
            List<LifePlayerState> players = Players(false, false, false);

            Assert.AreEqual(1, LifeTurnOrder.NextSeat(players, 0));
            Assert.AreEqual(0, LifeTurnOrder.NextSeat(players, 2));
        }

        [Test]
        public void ゴール済みの人は飛ばす()
        {
            List<LifePlayerState> players = Players(false, true, false, true);

            Assert.AreEqual(2, LifeTurnOrder.NextSeat(players, 0));
            Assert.AreEqual(0, LifeTurnOrder.NextSeat(players, 2));
        }

        [Test]
        public void 残り一人ならその人が続けて回す()
        {
            Assert.AreEqual(1, LifeTurnOrder.NextSeat(Players(true, false, true), 1));
        }

        [Test]
        public void 全員ゴールなら手番はない()
        {
            Assert.AreEqual(LifeTurnOrder.None, LifeTurnOrder.NextSeat(Players(true, true), 0));
        }

        [Test]
        public void 休みの人は休みを1減らして飛ばす()
        {
            List<LifePlayerState> players = Players(false, false, false);
            players[1].RestTurns = 1;
            var rested = new List<int>();

            Assert.AreEqual(2, LifeTurnOrder.NextSeat(players, 0, rested));
            CollectionAssert.AreEqual(new[] { 1 }, rested);
            Assert.AreEqual(0, players[1].RestTurns);
            Assert.AreEqual(1, LifeTurnOrder.NextSeat(players, 0), "休みが明けたら普通に回る");
        }

        [Test]
        public void 全員休みでも止まらず一周後に回る()
        {
            List<LifePlayerState> players = Players(false, true, false, false);
            foreach (LifePlayerState player in players) player.RestTurns = 1;
            var rested = new List<int>();

            Assert.AreEqual(2, LifeTurnOrder.NextSeat(players, 0, rested));
            CollectionAssert.AreEqual(new[] { 2, 3, 0 }, rested, "ゴール済みの人は休みを数えない");
            Assert.AreEqual(1, players[1].RestTurns);
        }
    }
}
