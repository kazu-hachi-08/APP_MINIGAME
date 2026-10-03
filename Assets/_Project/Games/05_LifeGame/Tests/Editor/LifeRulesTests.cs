using System.Collections.Generic;
using NUnit.Framework;

namespace MiniGame.LifeGame.Tests
{
    public class LifeRulesTests
    {
        // 区間B（Middle）の中の固定マスの位置（LifeBoardShape と同じ値）
        private const int MiddlePayday = 5;
        private const int MiddleMarriage = 10;

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

        /// <summary>P1 を区間Bの先頭に置き、1つ先のマスを指定の種類にする</summary>
        private LifeCell PrepareNextCell(LifeCellType type, int amount = 0)
        {
            _p1.Position = _middle;
            LifeCell cell = _state.Board[_middle + 1];
            cell.Type = type;
            cell.Amount = amount;
            return cell;
        }

        [Test]
        public void 止まるマスの予想は実際に動いた位置と同じ()
        {
            // スタート（分岐の手前）と区間B（給料日・結婚をまたぐ）から、全部の出目で比べる
            foreach (int start in new[] { LifeBoard.StartIndex, _middle })
            {
                for (int roll = 1; roll <= LifeRuleConfig.RouletteMax; roll++)
                {
                    LifeGameState state = LifeGameState.Create(2, 1, new LifeRuleConfig());
                    LifePlayerState player = state.Players[0];
                    player.Position = start;
                    int preview = LifeRules.PreviewStop(state, roll);

                    LifeRules.Move(state, roll);

                    Assert.AreEqual(player.Position, preview, $"start={start} roll={roll}");
                }
            }
        }

        [Test]
        public void 分岐で止まり残りの歩数で進む()
        {
            LifeRules.Move(_state, 5);

            Assert.AreEqual(LifePending.Branch, _state.Pending);
            Assert.AreEqual(1, _p1.Position);
            Assert.AreEqual(4, _state.StepsLeft);

            int university = _state.Board.BranchChoiceOf(_state.CurrentCell, LifeSection.University);
            int first = _state.CurrentCell.Next[university];
            LifeRules.Apply(_state, new LifeCommand(0, LifeCommandType.ChooseBranch, university));

            Assert.AreEqual(first + 3, _p1.Position);
            Assert.AreEqual(0, _state.StepsLeft);
        }

        [Test]
        public void フリーターを選ぶとフリーターになる()
        {
            LifeRules.Move(_state, 2);
            int freeter = _state.Board.BranchChoiceOf(_state.CurrentCell, LifeSection.Freeter);

            LifeRules.Apply(_state, new LifeCommand(0, LifeCommandType.ChooseBranch, freeter));

            Assert.AreEqual(_state.Config.FreeterJobId, _p1.JobId);
        }

        [Test]
        public void 就職マスで止まり職業カードから選ぶ()
        {
            LifeRules.Move(_state, 10);
            int job = _state.Board.BranchChoiceOf(_state.CurrentCell, LifeSection.Job);
            LifeRules.Apply(_state, new LifeCommand(0, LifeCommandType.ChooseBranch, job));

            Assert.AreEqual(LifeCellType.JobOffer, _state.CurrentCell.Type);
            Assert.AreEqual(LifePending.JobCard, _state.Pending);
            Assert.AreEqual(2, _state.JobCards.Count);
            foreach (int card in _state.JobCards) Assert.IsFalse(_state.Config.Jobs[card].IsAdvanced);

            int chosen = _state.JobCards[1];
            LifeRules.Apply(_state, new LifeCommand(0, LifeCommandType.ChooseJob, 1));

            Assert.AreEqual(chosen, _p1.JobId);
            Assert.AreEqual(1, _state.CurrentSeat);
        }

        [Test]
        public void 給料日は通過するだけで給料がもらえる()
        {
            _p1.JobId = 1;
            _p1.Position = _middle + MiddlePayday - 2;
            LifeCell landing = _state.Board[_middle + MiddlePayday + 1];
            landing.Type = LifeCellType.Income;
            landing.Amount = 0;

            LifeRules.Move(_state, 3);

            Assert.AreEqual(_middle + MiddlePayday + 1, _p1.Position);
            Assert.AreEqual(300 + _state.Config.Jobs[1].Salary, _p1.Money);
        }

        [Test]
        public void 結婚マスで止まり全員からご祝儀()
        {
            _p1.Position = _middle + MiddleMarriage - 2;

            LifeRules.Move(_state, 8);

            Assert.AreEqual(_middle + MiddleMarriage, _p1.Position);
            Assert.IsTrue(_p1.IsMarried);
            Assert.AreEqual(330, _p1.Money);
            Assert.AreEqual(270, _p2.Money);
        }

        [Test]
        public void ご祝儀が払えなければ手番でなくても手形になる()
        {
            _p2.Money = 10;
            PrepareNextCell(LifeCellType.Birth);

            LifeRules.Move(_state, 1);

            Assert.AreEqual(1, _p1.Children);
            Assert.AreEqual(1, _p2.Notes);
            Assert.AreEqual(10 + 100 - 20, _p2.Money);
        }

        [Test]
        public void 生命保険で病気の出費が無効()
        {
            _p1.Insurances = LifeInsurance.Life;
            PrepareNextCell(LifeCellType.Sickness, 100);

            LifeRules.Move(_state, 1);

            Assert.AreEqual(300, _p1.Money);
        }

        [Test]
        public void 病気は治療係に払う()
        {
            _p2.JobId = 6;
            PrepareNextCell(LifeCellType.Sickness, 100);

            LifeRules.Move(_state, 1);

            Assert.AreEqual(200, _p1.Money);
            Assert.AreEqual(400, _p2.Money);
        }

        [Test]
        public void 払う本人が係なら銀行に払う()
        {
            _p1.JobId = 2;
            _p2.JobId = 2;
            PrepareNextCell(LifeCellType.Accident, 100);

            LifeRules.Move(_state, 1);

            Assert.AreEqual(200, _p1.Money);
            Assert.AreEqual(300, _p2.Money);
        }

        [Test]
        public void 家ありの火事は半額で火災保険なら無効()
        {
            _p1.HouseId = 1;
            PrepareNextCell(LifeCellType.Fire);
            LifeRules.Move(_state, 1);
            Assert.AreEqual(0, _p1.Money);

            SetUp();
            _p1.HouseId = 1;
            _p1.Insurances = LifeInsurance.Fire;
            PrepareNextCell(LifeCellType.Fire);
            LifeRules.Move(_state, 1);
            Assert.AreEqual(300, _p1.Money);
        }

        [Test]
        public void 家なしの火事は小額で保険は効かない()
        {
            _p1.Insurances = LifeInsurance.Fire;
            PrepareNextCell(LifeCellType.Fire);

            LifeRules.Move(_state, 1);

            Assert.AreEqual(300 - _state.Config.FireWithoutHouse, _p1.Money);
        }

        [Test]
        public void 足りない分は手形を自動で切り途中返済できる()
        {
            _p1.Money = 30;
            PrepareNextCell(LifeCellType.Expense, 150);

            LifeRules.Move(_state, 1);

            Assert.AreEqual(2, _p1.Notes);
            Assert.AreEqual(80, _p1.Money);

            // P2 を分岐①に止めて（何も起きないマス）手番を P1 に戻す
            LifeRules.Move(_state, 1);
            Assert.AreEqual(0, _state.CurrentSeat);

            Assert.IsFalse(LifeRules.IsValid(_state, new LifeCommand(0, LifeCommandType.Repay, 1)), "80では1枚も返せない");

            _p1.Money = 250;
            LifeRules.Apply(_state, new LifeCommand(0, LifeCommandType.Repay, 2));

            Assert.AreEqual(0, _p1.Notes);
            Assert.AreEqual(50, _p1.Money);
            Assert.AreEqual(LifePending.Spin, _state.Pending);
        }

        [Test]
        public void 出目と同じ番号の株を持つ全員に配当()
        {
            _p1.Stocks.AddRange(new[] { 4, 4 });
            _p2.Stocks.AddRange(new[] { 4, 5 });
            PrepareNextCell(LifeCellType.Income, 0);
            _p1.Position = _middle - 3;

            List<LifeEvent> events = LifeRules.Move(_state, 4);

            Assert.AreEqual(4, events[0].Value);
            Assert.AreEqual(400, _p1.Money);
            Assert.AreEqual(350, _p2.Money);
        }

        [Test]
        public void 家を買うと所持金が足りなくても手形で買える()
        {
            PrepareNextCell(LifeCellType.House);
            LifeRules.Move(_state, 1);
            Assert.AreEqual(LifePending.House, _state.Pending);

            LifeRules.Apply(_state, new LifeCommand(0, LifeCommandType.ChooseHouse, 2));

            Assert.AreEqual(2, _p1.HouseId);
            Assert.AreEqual(7, _p1.Notes);
            Assert.AreEqual(0, _p1.Money);
        }

        [Test]
        public void 火災保険は家がないと選べない()
        {
            PrepareNextCell(LifeCellType.Insurance);
            LifeRules.Move(_state, 1);

            Assert.IsFalse(LifeRules.IsValid(_state, new LifeCommand(0, LifeCommandType.ChooseInsurance, (int)LifeInsurance.Fire)));

            LifeRules.Apply(_state, new LifeCommand(0, LifeCommandType.ChooseInsurance, (int)(LifeInsurance.Life | LifeInsurance.Auto)));

            Assert.AreEqual(LifeInsurance.Life | LifeInsurance.Auto, _p1.Insurances);
            Assert.AreEqual(220, _p1.Money);
            Assert.AreEqual(50, _p1.LifeInsurancePaid);
        }

        [Test]
        public void 手番以外と範囲外のコマンドは無視する()
        {
            Assert.AreEqual(0, LifeRules.Apply(_state, new LifeCommand(1, LifeCommandType.Spin)).Count);
            Assert.AreEqual(0, LifeRules.Apply(_state, new LifeCommand(0, LifeCommandType.ChooseHouse, 0)).Count);

            PrepareNextCell(LifeCellType.Stock);
            LifeRules.Move(_state, 1);
            Assert.AreEqual(0, LifeRules.Apply(_state, new LifeCommand(0, LifeCommandType.ChooseStock, 11)).Count);
            Assert.AreEqual(LifePending.Stock, _state.Pending);
        }

        // ---- 賭けマス ----

        /// <summary>P1 を賭けマスに止める。勝ち負けは BetWinMin で決め打ちする（出目は乱数なので）</summary>
        private void LandOnBet(int stake, bool win)
        {
            _state.Config.BetWinMin = win ? 1 : LifeRuleConfig.RouletteMax + 1;
            PrepareNextCell(LifeCellType.Bet, stake);
            LifeRules.Move(_state, 1);
            Assert.AreEqual(LifePending.Bet, _state.Pending);
        }

        [Test]
        public void 賭けに勝つと賭け金の分だけ増え配当は出ない()
        {
            _p2.Stocks.AddRange(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 });
            LandOnBet(100, true);
            int p2Money = _p2.Money;

            List<LifeEvent> events = LifeRules.Apply(_state, new LifeCommand(0, LifeCommandType.ChooseBet, 1));

            Assert.AreEqual(400, _p1.Money);
            Assert.AreEqual(p2Money, _p2.Money, "賭けのルーレットでは配当を出さない");
            Assert.IsTrue(events.Exists(e => e.Type == LifeEventType.BetResult && e.Amount == 100));
            Assert.AreEqual(1, _state.CurrentSeat);
        }

        [Test]
        public void 賭けに負けると没収される()
        {
            LandOnBet(100, false);

            LifeRules.Apply(_state, new LifeCommand(0, LifeCommandType.ChooseBet, 1));

            Assert.AreEqual(200, _p1.Money);
        }

        [Test]
        public void 賭けないとお金は動かない()
        {
            LandOnBet(100, false);

            LifeRules.Apply(_state, new LifeCommand(0, LifeCommandType.ChooseBet, 0));

            Assert.AreEqual(300, _p1.Money);
            Assert.AreEqual(1, _state.CurrentSeat);
        }

        [Test]
        public void ギャンブルルートの賭けマスは賭け金が2倍()
        {
            LifeCell bet = _state.Board.Cells.Find(c => c.Section == LifeSection.Gamble && c.Type == LifeCellType.Bet);

            Assert.AreEqual(_state.Config.BetStake * _state.Config.GambleMultiplier, bet.Amount);
        }

        [Test]
        public void 所持金が足りなくても賭けられ負けたら手形()
        {
            _p1.Money = 50;
            LandOnBet(100, false);

            LifeRules.Apply(_state, new LifeCommand(0, LifeCommandType.ChooseBet, 1));

            Assert.AreEqual(1, _p1.Notes);
            Assert.AreEqual(50, _p1.Money);
        }

        // ---- 人に関わるマス ----

        [Test]
        public void 指名した相手からもらい自分は選べない()
        {
            PrepareNextCell(LifeCellType.Nominate, 120);
            LifeRules.Move(_state, 1);
            Assert.AreEqual(LifePending.ChooseTarget, _state.Pending);

            Assert.IsFalse(LifeRules.IsValid(_state, new LifeCommand(0, LifeCommandType.ChooseTarget, 0)));
            Assert.IsFalse(LifeRules.IsValid(_state, new LifeCommand(0, LifeCommandType.ChooseTarget, LifeRules.NoTarget)), "指名はやめられない");

            LifeRules.Apply(_state, new LifeCommand(0, LifeCommandType.ChooseTarget, 1));

            Assert.AreEqual(420, _p1.Money);
            Assert.AreEqual(180, _p2.Money);
            Assert.AreEqual(1, _state.CurrentSeat);
        }

        [Test]
        public void 指名された相手が払えなければ手形()
        {
            _p2.Money = 20;
            PrepareNextCell(LifeCellType.Nominate, 120);
            LifeRules.Move(_state, 1);

            LifeRules.Apply(_state, new LifeCommand(0, LifeCommandType.ChooseTarget, 1));

            Assert.AreEqual(1, _p2.Notes);
            Assert.AreEqual(0, _p2.Money);
        }

        [Test]
        public void プレゼントは所持金が一番少ない人へ同額なら席の若い人へ()
        {
            _state = LifeGameState.Create(4, 1, new LifeRuleConfig());
            _p1 = _state.Players[0];
            _middle = _state.Board.Cells.FindIndex(c => c.Section == LifeSection.Middle);
            _p1.Money = 50;
            _state.Players[1].Money = 400;
            _state.Players[2].Money = 100;
            _state.Players[3].Money = 100;
            PrepareNextCell(LifeCellType.Present, 80);

            LifeRules.Move(_state, 1);

            Assert.AreEqual(180, _state.Players[2].Money, "自分より少なくても自分には渡さない");
            Assert.AreEqual(100, _state.Players[3].Money);
            Assert.AreEqual(1, _p1.Notes);
            Assert.AreEqual(1, _state.CurrentSeat);
        }

        [Test]
        public void 入れ替えで職業を交換でき交換しなくてもよい()
        {
            _p1.JobId = 1;
            _p2.JobId = 8;
            PrepareNextCell(LifeCellType.SwapJob);
            LifeRules.Move(_state, 1);
            Assert.AreEqual(LifePending.ChooseTarget, _state.Pending);

            LifeRules.Apply(_state, new LifeCommand(0, LifeCommandType.ChooseTarget, 1));

            Assert.AreEqual(8, _p1.JobId);
            Assert.AreEqual(1, _p2.JobId);

            SetUp();
            _p1.JobId = 1;
            _p2.JobId = 8;
            PrepareNextCell(LifeCellType.SwapJob);
            LifeRules.Move(_state, 1);

            LifeRules.Apply(_state, new LifeCommand(0, LifeCommandType.ChooseTarget, LifeRules.NoTarget));

            Assert.AreEqual(1, _p1.JobId);
            Assert.AreEqual(1, _state.CurrentSeat);
        }

        [Test]
        public void 入れ替えの対象がいなければ何も起きない()
        {
            _p1.JobId = 1;
            PrepareNextCell(LifeCellType.SwapJob);

            LifeRules.Move(_state, 1);

            Assert.AreEqual(1, _p1.JobId);
            Assert.AreEqual(LifePending.Spin, _state.Pending);
            Assert.AreEqual(1, _state.CurrentSeat);
        }

        [Test]
        public void NPCは指名で一番お金持ちを入れ替えで給料が高い人を選ぶ()
        {
            _state = LifeGameState.Create(3, 1, new LifeRuleConfig());
            _p1 = _state.Players[0];
            _middle = _state.Board.Cells.FindIndex(c => c.Section == LifeSection.Middle);
            _state.Players[1].Money = 500;
            _state.Players[2].Money = 400;
            PrepareNextCell(LifeCellType.Nominate, 100);
            LifeRules.Move(_state, 1);

            Assert.AreEqual(1, LifeNpcPlanner.Plan(_state, new LifeRandom(0)).Value);

            _state.Pending = LifePending.ChooseTarget;
            _state.CurrentCell.Type = LifeCellType.SwapJob;
            _p1.JobId = 5;
            _state.Players[1].JobId = 4;
            _state.Players[2].JobId = 8;
            Assert.AreEqual(2, LifeNpcPlanner.Plan(_state, new LifeRandom(0)).Value);

            _p1.JobId = 8;
            Assert.AreEqual(LifeRules.NoTarget, LifeNpcPlanner.Plan(_state, new LifeRandom(0)).Value, "自分より給料が高い人がいなければやめる");
        }

        [Test]
        public void ゴール順にボーナスをもらい全員ゴールで終わる()
        {
            _p1.Position = _state.Board.GoalIndex - 1;
            _p2.Position = _state.Board.GoalIndex - 1;

            LifeRules.Move(_state, 10);
            Assert.AreEqual(_state.Board.GoalIndex, _p1.Position);
            Assert.AreEqual(600, _p1.Money);
            Assert.AreEqual(1, _state.CurrentSeat);

            LifeRules.Move(_state, 1);
            Assert.AreEqual(500, _p2.Money);
            Assert.AreEqual(LifePending.Finished, _state.Pending);
        }

        [Test]
        public void 精算で家と株を売り保険が返り手形を返す()
        {
            _p1.Money = 100;
            _p1.HouseId = 0;
            _p1.Stocks.AddRange(new[] { 1, 2 });
            _p1.LifeInsurancePaid = 50;
            _p1.Notes = 2;
            _p1.GoalOrder = 0;
            _p2.GoalOrder = 1;
            _state.Pending = LifePending.Finished;

            List<LifeSettlementEntry> entries = LifeSettlement.Settle(_state);
            LifeSettlementEntry entry = entries[0];

            int sale = 300 * _state.Config.Houses[0].SalePercent(entry.HouseRoll) / 100;
            Assert.AreEqual(sale, entry.HouseSale);
            Assert.AreEqual(100 + sale + 200 + 25 - 250, entry.Total);
            Assert.AreEqual(0, _p1.Notes);
        }

        [Test]
        public void 総資産が同じなら先にゴールした人が上()
        {
            _p1.GoalOrder = 1;
            _p2.GoalOrder = 0;
            _state.Pending = LifePending.Finished;

            List<LifeSettlementEntry> entries = LifeSettlement.Settle(_state);

            Assert.AreEqual(2, entries[0].Rank);
            Assert.AreEqual(1, entries[1].Rank);
        }
    }
}
