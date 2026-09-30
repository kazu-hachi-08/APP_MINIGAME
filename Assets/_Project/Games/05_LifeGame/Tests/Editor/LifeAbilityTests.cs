using System.Collections.Generic;
using NUnit.Framework;

namespace MiniGame.LifeGame.Tests
{
    /// <summary>キャラの能力（仕様書 §9.2）</summary>
    public class LifeAbilityTests
    {
        private const int MiddlePayday = 5;

        private LifeGameState _state;
        private LifePlayerState _p1;
        private LifePlayerState _p2;
        private int _middle;

        private void SetUp(LifeAbility p1Ability, LifeAbility p2Ability = LifeAbility.None)
        {
            _state = LifeGameState.Create(new[] { p1Ability, p2Ability }, 1, new LifeRuleConfig());
            _p1 = _state.Players[0];
            _p2 = _state.Players[1];
            _middle = _state.Board.Cells.FindIndex(c => c.Section == LifeSection.Middle);
        }

        private void PrepareNextCell(LifeCellType type, int amount)
        {
            _p1.Position = _middle;
            LifeCell cell = _state.Board[_middle + 1];
            cell.Type = type;
            cell.Amount = amount;
        }

        private List<LifeEvent> Apply(LifeCommandType type, int value = 0)
        {
            return LifeRules.Apply(_state, new LifeCommand(_state.CurrentSeat, type, value));
        }

        [Test]
        public void バランスは初期所持金が多い()
        {
            SetUp(LifeAbility.StartMoney);

            Assert.AreEqual(400, _p1.Money);
            Assert.AreEqual(300, _p2.Money);
        }

        [Test]
        public void がんばり屋は給料が1割増える()
        {
            SetUp(LifeAbility.Salary);
            _p1.JobId = 1;
            _p1.Position = _middle + MiddlePayday - 1;
            LifeCell landing = _state.Board[_middle + MiddlePayday + 1];
            landing.Type = LifeCellType.Income;
            landing.Amount = 0;

            LifeRules.Move(_state, 2);

            Assert.AreEqual(300 + 180 + 18, _p1.Money);
        }

        [Test]
        public void しっかり者は出費が1割減る()
        {
            SetUp(LifeAbility.Thrift);
            PrepareNextCell(LifeCellType.Expense, 100);

            LifeRules.Move(_state, 1);

            Assert.AreEqual(210, _p1.Money);
        }

        [Test]
        public void しっかり者の病気は係にも減った額を払う()
        {
            SetUp(LifeAbility.Thrift);
            _p2.JobId = 6;
            PrepareNextCell(LifeCellType.Sickness, 100);

            LifeRules.Move(_state, 1);

            Assert.AreEqual(210, _p1.Money);
            Assert.AreEqual(390, _p2.Money);
        }

        [Test]
        public void しっかり者でも家の購入は減らない()
        {
            SetUp(LifeAbility.Thrift);
            _state.Pending = LifePending.House;

            Apply(LifeCommandType.ChooseHouse, 0);

            Assert.AreEqual(0, _p1.Money);
        }

        [Test]
        public void らっきーは出目を見てから振り直すか選べる()
        {
            SetUp(LifeAbility.Reroll);

            Apply(LifeCommandType.Spin);

            Assert.AreEqual(LifePending.Reroll, _state.Pending);
            Assert.AreEqual(LifeBoard.StartIndex, _p1.Position);

            Apply(LifeCommandType.ChooseReroll, 1);

            Assert.AreNotEqual(LifePending.Reroll, _state.Pending);
            Assert.AreNotEqual(LifeBoard.StartIndex, _p1.Position);
            Assert.IsTrue(_p1.RerollUsed);
        }

        [Test]
        public void 振り直さなければ最初の出目で進み能力は残る()
        {
            SetUp(LifeAbility.Reroll);

            Apply(LifeCommandType.Spin);
            int roll = _state.LastRoll;
            List<LifeEvent> events = Apply(LifeCommandType.ChooseReroll, 0);

            Assert.AreEqual(roll, _state.LastRoll);
            Assert.AreEqual(LifeEventType.Rolled, events[0].Type);
            Assert.AreEqual(roll, events[0].Value);
            Assert.IsFalse(_p1.RerollUsed);
        }

        [Test]
        public void 振り直しは1試合に1回だけ()
        {
            SetUp(LifeAbility.Reroll);
            _p1.RerollUsed = true;

            Apply(LifeCommandType.Spin);

            Assert.AreNotEqual(LifePending.Reroll, _state.Pending);
            Assert.AreNotEqual(LifeBoard.StartIndex, _p1.Position);
        }

        [Test]
        public void 振り直す前の出目では配当が出ない()
        {
            SetUp(LifeAbility.Reroll);
            // どの出目でも配当が出るように、P2 に全番号の株を持たせる
            for (int number = 1; number <= LifeRuleConfig.RouletteMax; number++) _p2.Stocks.Add(number);

            Apply(LifeCommandType.Spin);
            Assert.AreEqual(300, _p2.Money);

            Apply(LifeCommandType.ChooseReroll, 1);
            Assert.AreEqual(300 + _state.Config.Dividend, _p2.Money);
        }

        [Test]
        public void 振り直しの選択待ちで他のコマンドは通らない()
        {
            SetUp(LifeAbility.Reroll);

            Apply(LifeCommandType.Spin);

            Assert.IsFalse(LifeRules.IsValid(_state, new LifeCommand(0, LifeCommandType.Spin)));
            Assert.IsFalse(LifeRules.IsValid(_state, new LifeCommand(0, LifeCommandType.ChooseReroll, 2)));
            Assert.IsFalse(LifeRules.IsValid(_state, new LifeCommand(1, LifeCommandType.ChooseReroll, 1)));
        }
    }
}
