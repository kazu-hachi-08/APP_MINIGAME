using System.Collections.Generic;
using NUnit.Framework;

namespace MiniGame.LifeGame.Tests
{
    /// <summary>時代イベント（B4）</summary>
    public class LifeEraTests
    {
        private const int MiddleMarriage = 10;
        private const int GameCount = 50;
        private const int MaxCommands = 2000;

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
        }

        /// <summary>P1 を区間Bの先頭に置き、1つ先のマスを指定の種類にして止まらせる（区間をまたがないので時代は変わらない）</summary>
        private List<LifeEvent> LandOn(LifeEra era, LifeCellType type, int amount = 0)
        {
            _state.Era = era;
            _p1.Position = _middle;
            LifeCell cell = _state.Board[_middle + 1];
            cell.Type = type;
            cell.Amount = amount;
            return LifeRules.Move(_state, 1);
        }

        [Test]
        public void 好景気は収入と指名が1_5倍()
        {
            LandOn(LifeEra.Boom, LifeCellType.Income, 100);
            Assert.AreEqual(300 + 150, _p1.Money);

            SetUp();
            LandOn(LifeEra.Boom, LifeCellType.Nominate, 100);
            LifeRules.Apply(_state, new LifeCommand(0, LifeCommandType.ChooseTarget, 1));
            Assert.AreEqual(300 + 150, _p1.Money);
            Assert.AreEqual(300 - 150, _p2.Money);
        }

        [Test]
        public void 不況は収入が半分で出費が1_5倍()
        {
            LandOn(LifeEra.Recession, LifeCellType.Income, 100);
            Assert.AreEqual(300 + 50, _p1.Money);

            SetUp();
            LandOn(LifeEra.Recession, LifeCellType.Expense, 100);
            Assert.AreEqual(300 - 150, _p1.Money);
        }

        [Test]
        public void 株ブームは配当が2倍()
        {
            _p2.Stocks.Add(1);
            LandOn(LifeEra.StockBoom, LifeCellType.Income, 0);

            Assert.AreEqual(300 + _state.Config.Dividend * 2, _p2.Money);
            Assert.AreEqual(_state.Config.Dividend * 2, _p2.DividendTotal);
        }

        [Test]
        public void ベビーブームはご祝儀が2倍()
        {
            _state.Era = LifeEra.BabyBoom;
            _p1.Position = _middle + MiddleMarriage - 1;
            LifeRules.Move(_state, 1);
            Assert.AreEqual(300 + _state.Config.MarriageGift * 2, _p1.Money);

            SetUp();
            LandOn(LifeEra.BabyBoom, LifeCellType.Birth);
            Assert.AreEqual(300 + _state.Config.BirthGift * 2, _p1.Money);
        }

        [Test]
        public void 平和な時代は病気事故火事が半分()
        {
            LandOn(LifeEra.Peace, LifeCellType.Sickness, 100);
            Assert.AreEqual(300 - 50, _p1.Money);

            SetUp();
            LandOn(LifeEra.Peace, LifeCellType.Accident, 100);
            Assert.AreEqual(300 - 50, _p1.Money);

            SetUp();
            _p1.HouseId = 0;
            LandOn(LifeEra.Peace, LifeCellType.Fire);
            Assert.AreEqual(300 - _state.Config.Houses[0].Price / 2 / 2, _p1.Money);
        }

        [Test]
        public void 効かないマスは倍率がかからない()
        {
            LandOn(LifeEra.Peace, LifeCellType.Income, 100);
            Assert.AreEqual(300 + 100, _p1.Money);

            SetUp();
            LandOn(LifeEra.Boom, LifeCellType.Expense, 100);
            Assert.AreEqual(300 - 100, _p1.Money);
        }

        [Test]
        public void 同じ時代は続かない()
        {
            var random = new LifeRandom(7);
            LifeEra era = LifeEra.None;
            for (int i = 0; i < 1000; i++)
            {
                LifeEra next = LifeEras.Draw(random, era);
                Assert.AreNotEqual(era, next);
                Assert.AreNotEqual(LifeEra.None, next);
                era = next;
            }
        }

        [Test]
        public void 先頭が区間Bに入ったときだけ時代が変わる()
        {
            int routeEnd = _state.Board.Cells.FindIndex(c => c.Section == LifeSection.Job && c.Next.Contains(_middle));
            _p1.Position = routeEnd;

            List<LifeEvent> events = LifeRules.Move(_state, 1);

            LifeEvent changed = events.Find(e => e.Type == LifeEventType.EraChanged);
            Assert.AreEqual(LifeEventType.EraChanged, changed.Type);
            Assert.AreEqual((int)_state.Era, changed.Value);
            Assert.AreNotEqual(LifeEra.None, _state.Era);

            // 後から入った人では引き直さない
            _state.CurrentSeat = 1;
            _p2.Position = routeEnd;
            events = LifeRules.Move(_state, 1);

            Assert.IsFalse(events.Exists(e => e.Type == LifeEventType.EraChanged));
        }

        [Test]
        public void 一試合で時代はちょうど3回変わる()
        {
            for (int seed = 0; seed < GameCount; seed++)
            {
                LifeGameState state = LifeGameState.Create(4, seed, new LifeRuleConfig());
                var npcRandom = new LifeRandom(seed + 10000);
                var eras = new List<int>();
                for (int i = 0; i < MaxCommands && state.Pending != LifePending.Finished; i++)
                {
                    List<LifeEvent> events = LifeRules.Apply(state, LifeNpcPlanner.Plan(state, npcRandom));
                    foreach (LifeEvent e in events)
                    {
                        if (e.Type == LifeEventType.EraChanged) eras.Add(e.Value);
                    }
                }

                Assert.AreEqual(3, eras.Count, $"seed={seed}");
                Assert.AreNotEqual(eras[0], eras[1], $"seed={seed}");
                Assert.AreNotEqual(eras[1], eras[2], $"seed={seed}");
            }
        }
    }
}
