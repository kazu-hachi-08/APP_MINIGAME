using System.Collections.Generic;
using NUnit.Framework;

namespace MiniGame.LifeGame.Tests
{
    /// <summary>称号ボーナス（B1）と、称号の判定に使う数え方</summary>
    public class LifeTitleTests
    {
        private LifeGameState _state;
        private LifePlayerState _p1;
        private LifePlayerState _p2;
        private int _middle;

        [SetUp]
        public void SetUp()
        {
            _state = LifeGameState.Create(2, 1, new LifeRuleConfig());
            _p1 = _state.Players[0];
            _p2 = _state.Players[1];
            _middle = _state.Board.Cells.FindIndex(c => c.Section == LifeSection.Middle);
            _p1.GoalOrder = 0;
            _p2.GoalOrder = 1;
        }

        private List<LifeSettlementEntry> Settle()
        {
            _state.Pending = LifePending.Finished;
            return LifeSettlement.Settle(_state);
        }

        [Test]
        public void 一番の人が称号をもらいボーナスが入る()
        {
            _p1.Children = 2;
            _p2.Children = 1;

            List<LifeSettlementEntry> entries = Settle();

            CollectionAssert.AreEqual(new[] { LifeTitle.ManyChildren }, entries[0].Titles);
            Assert.AreEqual(_state.Config.TitleBonus, entries[0].TitleBonus);
            Assert.AreEqual(300 + _state.Config.TitleBonus, entries[0].Total);
            Assert.IsEmpty(entries[1].Titles);
        }

        [Test]
        public void 同点なら全員がもらう()
        {
            _p1.GambleWinnings = 200;
            _p2.GambleWinnings = 200;

            List<LifeSettlementEntry> entries = Settle();

            CollectionAssert.AreEqual(new[] { LifeTitle.Gambler }, entries[0].Titles);
            CollectionAssert.AreEqual(new[] { LifeTitle.Gambler }, entries[1].Titles);
        }

        [Test]
        public void 誰も当てはまらない称号は出さない()
        {
            List<LifeSettlementEntry> entries = Settle();

            Assert.IsEmpty(entries[0].Titles);
            Assert.IsEmpty(entries[1].Titles);
            Assert.AreEqual(300, entries[0].Total);
        }

        [Test]
        public void 称号ボーナスで順位が入れ替わる()
        {
            _p1.Money = 350;
            _p2.NotesIssued = 1;
            _p2.PaidToOthers = 10;

            List<LifeSettlementEntry> entries = Settle();

            Assert.AreEqual(300 + _state.Config.TitleBonus * 2, entries[1].Total);
            Assert.AreEqual(1, entries[1].Rank);
            Assert.AreEqual(2, entries[0].Rank);
        }

        [Test]
        public void 人に払った額と手形の枚数を数える()
        {
            _p2.Money = 0;
            _p1.Position = _middle;
            LifeCell cell = _state.Board[_middle + 1];
            cell.Type = LifeCellType.Birth;

            LifeRules.Move(_state, 1);

            Assert.AreEqual(_state.Config.BirthGift, _p2.PaidToOthers);
            Assert.AreEqual(1, _p2.NotesIssued);
            Assert.AreEqual(0, _p1.PaidToOthers);
        }

        [Test]
        public void 手形を返しても切った枚数は減らない()
        {
            _p1.Money = 0;
            LifeMoney.Pay(_state, _p1, 150, LifeEvent.Bank, new List<LifeEvent>());
            _p1.Money += 500;
            LifeMoney.Repay(_state.Config, _p1, 2, new List<LifeEvent>());

            Assert.AreEqual(0, _p1.Notes);
            Assert.AreEqual(2, _p1.NotesIssued);
        }

        [Test]
        public void 賭けに勝った額を数える()
        {
            bool sawWin = false;
            for (int seed = 0; seed < 50 && !sawWin; seed++)
            {
                _state = LifeGameState.Create(2, seed, new LifeRuleConfig());
                _p1 = _state.Players[0];
                _p1.Position = _middle;
                LifeCell cell = _state.Board[_middle + 1];
                cell.Type = LifeCellType.Bet;
                cell.Amount = 100;
                LifeRules.Move(_state, 1);

                List<LifeEvent> events = LifeRules.Apply(_state, new LifeCommand(0, LifeCommandType.ChooseBet, 1));

                bool won = events.Exists(e => e.Type == LifeEventType.BetResult && e.Amount > 0);
                Assert.AreEqual(won ? 100 : 0, _p1.GambleWinnings, $"seed={seed}");
                sawWin = won;
            }

            Assert.IsTrue(sawWin);
        }
    }
}
