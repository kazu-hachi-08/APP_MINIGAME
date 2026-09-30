using System.Collections.Generic;

namespace MiniGame.LifeGame
{
    /// <summary>
    /// コマンドを受けて状態を進め、起きたこと（イベント列）を返す。
    /// 人間・NPC・オンラインの相手・EditModeテストがすべてここを通るので、ここ以外で状態を書き換えない。
    /// </summary>
    public static class LifeRules
    {
        private const int JobCardCount = 2;
        private const int KeepCurrentJob = -1;

        public static bool IsValid(LifeGameState state, LifeCommand command)
        {
            if (state.Pending == LifePending.Finished) return false;
            if (command.Seat != state.CurrentSeat) return false;

            LifePlayerState player = state.Current;
            int value = command.Value;
            switch (command.Type)
            {
                case LifeCommandType.Repay:
                    return LifeMoney.CanRepay(state.Config, player, value);
                case LifeCommandType.Spin:
                    return state.Pending == LifePending.Spin;
                case LifeCommandType.ChooseReroll:
                    return state.Pending == LifePending.Reroll && (value == 0 || value == 1);
                case LifeCommandType.ChooseBranch:
                    return state.Pending == LifePending.Branch && value >= 0 && value < state.CurrentCell.Next.Count;
                case LifeCommandType.ChooseJob:
                    int min = state.Pending == LifePending.ChangeJob ? KeepCurrentJob : 0;
                    bool waitingJob = state.Pending == LifePending.JobCard || state.Pending == LifePending.ChangeJob;
                    return waitingJob && value >= min && value < state.JobCards.Count;
                case LifeCommandType.ChooseHouse:
                    return state.Pending == LifePending.House && value >= LifeRuleConfig.NoHouse && value < state.Config.Houses.Length;
                case LifeCommandType.ChooseInsurance:
                    return state.Pending == LifePending.Insurance && value >= 0 && (value & ~(int)AvailableInsurances(player)) == 0;
                case LifeCommandType.ChooseStock:
                    return state.Pending == LifePending.Stock && value >= 0 && value <= LifeRuleConfig.RouletteMax;
                default:
                    return false;
            }
        }

        /// <summary>
        /// コマンドを適用する。手番以外・範囲外のコマンドは何もせず空の列を返す（オンラインで不正な値が届いても状態を壊さないため）。
        /// </summary>
        public static List<LifeEvent> Apply(LifeGameState state, LifeCommand command)
        {
            var events = new List<LifeEvent>();
            if (!IsValid(state, command)) return events;

            switch (command.Type)
            {
                case LifeCommandType.Repay:
                    LifeMoney.Repay(state.Config, state.Current, command.Value, events);
                    break;
                case LifeCommandType.Spin:
                    Spin(state, events);
                    break;
                case LifeCommandType.ChooseReroll:
                    ChooseReroll(state, command.Value == 1, events);
                    break;
                case LifeCommandType.ChooseBranch:
                    ChooseBranch(state, command.Value, events);
                    break;
                case LifeCommandType.ChooseJob:
                    ChooseJob(state, command.Value, events);
                    break;
                case LifeCommandType.ChooseHouse:
                    BuyHouse(state, command.Value, events);
                    break;
                case LifeCommandType.ChooseInsurance:
                    BuyInsurances(state, (LifeInsurance)command.Value, events);
                    break;
                case LifeCommandType.ChooseStock:
                    BuyStock(state, command.Value, events);
                    break;
            }

            return events;
        }

        /// <summary>まだ入っていない保険のうち今入れるもの。火災保険は家を持っている人だけ</summary>
        public static LifeInsurance AvailableInsurances(LifePlayerState player)
        {
            LifeInsurance available = LifeInsurance.Life | LifeInsurance.Auto;
            if (player.HasHouse) available |= LifeInsurance.Fire;

            return available & ~player.Insurances;
        }

        /// <summary>給料日に受け取る額（キャラの能力込み）。職業なしは 0</summary>
        public static int SalaryOf(LifeRuleConfig config, LifePlayerState player, int jobId)
        {
            int salary = config.SalaryOf(jobId);
            if (player.Ability == LifeAbility.Salary) salary += salary * config.AbilitySalaryPercent / 100;
            return salary;
        }

        public static bool CanReroll(LifePlayerState player)
        {
            return player.Ability == LifeAbility.Reroll && !player.RerollUsed;
        }

        // ---- 移動 ----

        /// <summary>
        /// 出目を決める。振り直せる人は出目を見せて選ばせ、決まるまで配当も移動もしない
        /// （振り直す前の出目で配当が出ないようにするため。仕様書 §9.2）
        /// </summary>
        private static void Spin(LifeGameState state, List<LifeEvent> events)
        {
            int roll = RollDice(state);
            if (!CanReroll(state.Current))
            {
                Move(state, roll, events);
                return;
            }

            state.LastRoll = roll;
            state.Pending = LifePending.Reroll;
        }

        private static void ChooseReroll(LifeGameState state, bool reroll, List<LifeEvent> events)
        {
            if (!reroll)
            {
                Move(state, state.LastRoll, events);
                return;
            }

            state.Current.RerollUsed = true;
            Move(state, RollDice(state), events);
        }

        private static int RollDice(LifeGameState state) => state.Random.Range(1, LifeRuleConfig.RouletteMax);

        /// <summary>出目を決めた後の処理。テストでは出目を指定してここを直接呼ぶ</summary>
        internal static void Move(LifeGameState state, int roll, List<LifeEvent> events)
        {
            state.LastRoll = roll;
            events.Add(new LifeEvent(LifeEventType.Rolled, state.CurrentSeat, value: roll));
            LifeMoney.PayDividends(state, roll, events);

            state.StepsLeft = roll;
            ContinueMove(state, events);
        }

        internal static List<LifeEvent> Move(LifeGameState state, int roll)
        {
            var events = new List<LifeEvent>();
            Move(state, roll, events);
            return events;
        }

        private static void ContinueMove(LifeGameState state, List<LifeEvent> events)
        {
            while (state.StepsLeft > 0)
            {
                LifeCell cell = state.CurrentCell;
                if (cell.IsBranch)
                {
                    // 分岐では止まって道を選ばせ、残りの歩数は ChooseBranch で使う
                    state.Pending = LifePending.Branch;
                    events.Add(new LifeEvent(LifeEventType.BranchReached, state.CurrentSeat, value: cell.Index));
                    return;
                }

                StepTo(state, cell.Next[0], events);
            }

            Land(state, events);
        }

        private static void ChooseBranch(LifeGameState state, int choice, List<LifeEvent> events)
        {
            int next = state.CurrentCell.Next[choice];
            if (state.Board[next].Section == LifeSection.Freeter) ChangeJob(state, state.Config.FreeterJobId, events);

            StepTo(state, next, events);
            ContinueMove(state, events);
        }

        private static void StepTo(LifeGameState state, int index, List<LifeEvent> events)
        {
            LifePlayerState player = state.Current;
            player.Position = index;
            state.StepsLeft--;
            events.Add(new LifeEvent(LifeEventType.Moved, player.Seat, value: index));

            LifeCell cell = state.Board[index];
            if (cell.Type == LifeCellType.Payday) PaySalary(state, player, events);
            if (LifeCellTypes.IsStop(cell.Type)) state.StepsLeft = 0;
        }

        private static void PaySalary(LifeGameState state, LifePlayerState player, List<LifeEvent> events)
        {
            int salary = SalaryOf(state.Config, player, player.JobId);
            if (salary <= 0) return;

            player.Money += salary;
            events.Add(new LifeEvent(LifeEventType.Salary, player.Seat, salary));
        }

        // ---- マス効果 ----

        private static void Land(LifeGameState state, List<LifeEvent> events)
        {
            LifePlayerState player = state.Current;
            LifeCell cell = state.CurrentCell;
            events.Add(new LifeEvent(LifeEventType.Landed, player.Seat, cell.Amount, cell.Index));

            if (ApplyCell(state, player, cell, events)) return;

            EndTurn(state, events);
        }

        /// <summary>マスの効果を適用する。選択待ちになったら true（手番はまだ終わらない）</summary>
        private static bool ApplyCell(LifeGameState state, LifePlayerState player, LifeCell cell, List<LifeEvent> events)
        {
            LifeRuleConfig config = state.Config;
            switch (cell.Type)
            {
                case LifeCellType.Income:
                    LifeMoney.Receive(player, cell.Amount, events);
                    return false;
                case LifeCellType.Expense:
                case LifeCellType.Tuition:
                    LifeMoney.PayExpense(state, player, cell.Amount, LifeEvent.Bank, events);
                    return false;
                case LifeCellType.Sickness:
                    LifeMoney.PayMishap(state, player, cell.Amount, LifeInsurance.Life, LifeJobRole.Healer, events);
                    return false;
                case LifeCellType.Accident:
                    LifeMoney.PayMishap(state, player, cell.Amount, LifeInsurance.Auto, LifeJobRole.Police, events);
                    return false;
                case LifeCellType.Fire:
                    Fire(state, player, events);
                    return false;
                case LifeCellType.Birth:
                    Birth(state, player, events);
                    return false;
                case LifeCellType.Marriage:
                    Marry(state, player, events);
                    return false;
                case LifeCellType.Goal:
                    Goal(state, player, events);
                    return false;
                case LifeCellType.JobOffer:
                    return WaitForJobCards(state, LifePending.JobCard, includeAdvanced: false, events);
                case LifeCellType.Graduation:
                    return WaitForJobCards(state, LifePending.JobCard, includeAdvanced: true, events);
                case LifeCellType.ChangeJob:
                    return WaitForJobCards(state, LifePending.ChangeJob, includeAdvanced: false, events);
                case LifeCellType.House:
                    return WaitFor(state, LifePending.House, !player.HasHouse);
                case LifeCellType.Insurance:
                    return WaitFor(state, LifePending.Insurance, AvailableInsurances(player) != LifeInsurance.None);
                case LifeCellType.Stock:
                    return WaitFor(state, LifePending.Stock, player.Stocks.Count < config.MaxStocks);
                default:
                    return false;
            }
        }

        private static bool WaitFor(LifeGameState state, LifePending pending, bool condition)
        {
            if (condition) state.Pending = pending;
            return condition;
        }

        private static void Fire(LifeGameState state, LifePlayerState player, List<LifeEvent> events)
        {
            if (!player.HasHouse)
            {
                // 家なしは小額の出費だけ。保険・手数料の対象外（仕様書 §5.2）
                LifeMoney.PayExpense(state, player, state.Config.FireWithoutHouse, LifeEvent.Bank, events);
                return;
            }

            int damage = state.Config.Houses[player.HouseId].Price / 2;
            LifeMoney.PayMishap(state, player, damage, LifeInsurance.Fire, LifeJobRole.Repair, events);
        }

        private static void Birth(LifeGameState state, LifePlayerState player, List<LifeEvent> events)
        {
            if (player.Children >= state.Config.MaxChildren) return;

            player.Children++;
            events.Add(new LifeEvent(LifeEventType.ChildBorn, player.Seat, value: player.Children));
            LifeMoney.CollectGifts(state, player, state.Config.BirthGift, events);
        }

        private static void Marry(LifeGameState state, LifePlayerState player, List<LifeEvent> events)
        {
            if (player.IsMarried) return;

            player.IsMarried = true;
            events.Add(new LifeEvent(LifeEventType.Married, player.Seat));
            LifeMoney.CollectGifts(state, player, state.Config.MarriageGift, events);
        }

        private static void Goal(LifeGameState state, LifePlayerState player, List<LifeEvent> events)
        {
            int order = state.GoalCount++;
            player.GoalOrder = order;

            int[] bonuses = state.Config.GoalBonuses;
            int bonus = order < bonuses.Length ? bonuses[order] : 0;
            player.Money += bonus;
            events.Add(new LifeEvent(LifeEventType.Goal, player.Seat, bonus, order));
        }

        // ---- 選択 ----

        private static bool WaitForJobCards(LifeGameState state, LifePending pending, bool includeAdvanced, List<LifeEvent> events)
        {
            var pool = new List<int>();
            LifeJob[] jobs = state.Config.Jobs;
            for (int id = 0; id < jobs.Length; id++)
            {
                if (jobs[id].IsFreeter || (jobs[id].IsAdvanced && !includeAdvanced)) continue;
                // 転職で今と同じ職業を引いても選ぶ意味がないので外す
                if (id == state.Current.JobId) continue;

                pool.Add(id);
            }

            state.Random.Shuffle(pool);
            state.JobCards.Clear();
            for (int i = 0; i < JobCardCount && i < pool.Count; i++) state.JobCards.Add(pool[i]);

            state.Pending = pending;
            events.Add(new LifeEvent(LifeEventType.JobCardsDrawn, state.CurrentSeat));
            return true;
        }

        private static void ChooseJob(LifeGameState state, int choice, List<LifeEvent> events)
        {
            if (choice != KeepCurrentJob) ChangeJob(state, state.JobCards[choice], events);

            state.JobCards.Clear();
            EndTurn(state, events);
        }

        private static void ChangeJob(LifeGameState state, int jobId, List<LifeEvent> events)
        {
            state.Current.JobId = jobId;
            events.Add(new LifeEvent(LifeEventType.JobChanged, state.CurrentSeat, value: jobId));
        }

        private static void BuyHouse(LifeGameState state, int houseId, List<LifeEvent> events)
        {
            if (houseId != LifeRuleConfig.NoHouse)
            {
                LifePlayerState player = state.Current;
                LifeMoney.Pay(state, player, state.Config.Houses[houseId].Price, LifeEvent.Bank, events);
                player.HouseId = houseId;
                events.Add(new LifeEvent(LifeEventType.HouseBought, player.Seat, value: houseId));
            }

            EndTurn(state, events);
        }

        private static void BuyInsurances(LifeGameState state, LifeInsurance chosen, List<LifeEvent> events)
        {
            BuyInsurance(state, chosen, LifeInsurance.Life, events);
            BuyInsurance(state, chosen, LifeInsurance.Auto, events);
            BuyInsurance(state, chosen, LifeInsurance.Fire, events);
            EndTurn(state, events);
        }

        private static void BuyInsurance(LifeGameState state, LifeInsurance chosen, LifeInsurance insurance, List<LifeEvent> events)
        {
            if ((chosen & insurance) == 0) return;

            LifePlayerState player = state.Current;
            int price = state.Config.InsurancePrice(insurance);
            LifeMoney.Pay(state, player, price, LifeEvent.Bank, events);
            player.Insurances |= insurance;
            if (insurance == LifeInsurance.Life) player.LifeInsurancePaid += price;

            events.Add(new LifeEvent(LifeEventType.InsuranceBought, player.Seat, price, (int)insurance));
        }

        private static void BuyStock(LifeGameState state, int number, List<LifeEvent> events)
        {
            if (number != 0)
            {
                LifePlayerState player = state.Current;
                LifeMoney.Pay(state, player, state.Config.StockPrice, LifeEvent.Bank, events);
                player.Stocks.Add(number);
                events.Add(new LifeEvent(LifeEventType.StockBought, player.Seat, state.Config.StockPrice, number));
            }

            EndTurn(state, events);
        }

        // ---- 手番 ----

        private static void EndTurn(LifeGameState state, List<LifeEvent> events)
        {
            events.Add(new LifeEvent(LifeEventType.TurnEnded, state.CurrentSeat));

            int next = LifeTurnOrder.NextSeat(state.Players, state.CurrentSeat);
            if (next == LifeTurnOrder.None)
            {
                state.Pending = LifePending.Finished;
                events.Add(new LifeEvent(LifeEventType.AllGoaled, state.CurrentSeat));
                return;
            }

            state.CurrentSeat = next;
            state.Pending = LifePending.Spin;
            events.Add(new LifeEvent(LifeEventType.TurnStarted, next));
        }
    }
}
