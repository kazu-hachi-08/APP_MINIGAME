using System;
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
        private const int RestTurnCount = 1;
        public const int NoTarget = -1;

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
                case LifeCommandType.ChooseBet:
                    return state.Pending == LifePending.Bet && (value == 0 || value == 1);
                case LifeCommandType.ChooseTarget:
                    return state.Pending == LifePending.ChooseTarget && IsValidTarget(state, value);
                case LifeCommandType.ChooseLottery:
                    return state.Pending == LifePending.Lottery && value >= 1 && value <= LifeRuleConfig.RouletteMax;
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
                case LifeCommandType.ChooseBet:
                    Bet(state, command.Value == 1, events);
                    break;
                case LifeCommandType.ChooseTarget:
                    ChooseTarget(state, command.Value, events);
                    break;
                case LifeCommandType.ChooseLottery:
                    DrawLottery(state, command.Value, events);
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

        /// <summary>
        /// 指名・入れ替えで選べる相手か。自分は選べない。ゴールした人は選べる（ゴール後もお金は動くので）。
        /// 入れ替えは職業を持っている人だけで、NoTarget（やめる）も選べる
        /// </summary>
        public static bool IsValidTarget(LifeGameState state, int seat)
        {
            bool isSwap = state.CurrentCell.Type == LifeCellType.SwapJob;
            if (seat == NoTarget) return isSwap;
            if (seat < 0 || seat >= state.Players.Count || seat == state.CurrentSeat) return false;

            return !isSwap || state.Players[seat].JobId != LifeRuleConfig.NoJob;
        }

        private static bool HasAnyTarget(LifeGameState state)
        {
            for (int seat = 0; seat < state.Players.Count; seat++)
            {
                if (IsValidTarget(state, seat)) return true;
            }

            return false;
        }

        /// <summary>プレゼントを受け取る人：自分以外で所持金が一番少ない人。同額なら席番号の若い人。いなければ NoTarget</summary>
        public static int PresentReceiver(LifeGameState state)
        {
            int receiver = NoTarget;
            foreach (LifePlayerState other in state.Players)
            {
                if (other.Seat == state.CurrentSeat) continue;
                if (receiver == NoTarget || other.Money < state.Players[receiver].Money) receiver = other.Seat;
            }

            return receiver;
        }

        public static bool CanReroll(LifePlayerState player)
        {
            return player.Ability == LifeAbility.Reroll && !player.RerollUsed;
        }

        /// <summary>
        /// 出目 roll で手番の人が止まるマス（状態は変えない）。振り直すか決める前に、このままだとどこに止まるかを見せるため。
        /// 分岐に着いたら、その先は道の選び方で変わるので分岐のマスを返す。止まり方は ContinueMove と揃えること
        /// </summary>
        public static int PreviewStop(LifeGameState state, int roll)
        {
            int index = state.Current.Position;
            for (int steps = roll; steps > 0; steps--)
            {
                LifeCell cell = state.Board[index];
                if (cell.IsBranch) return index;

                index = cell.Next[0];
                if (LifeCellTypes.IsStop(state.Board[index].Type)) return index;
            }

            return index;
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
            LifeSection from = state.Board[player.Position].Section;
            player.Position = index;
            state.StepsLeft--;
            events.Add(new LifeEvent(LifeEventType.Moved, player.Seat, value: index));

            LifeCell cell = state.Board[index];
            if (cell.Section != from) ChangeEraIfFirst(state, cell.Section, events);
            if (cell.Type == LifeCellType.Payday) PaySalary(state, player, events);
            if (LifeCellTypes.IsStop(cell.Type)) state.StepsLeft = 0;
        }

        /// <summary>
        /// 先頭の人が区間B・分岐②の道・区間C に入ったときだけ時代を引く（給料日ごとだと1試合に約20回変わって覚えきれないため）。
        /// 区間をまたいだときだけ呼ぶ（テストで区間の途中に置いたコマを動かしても引かないように）
        /// </summary>
        private static void ChangeEraIfFirst(LifeGameState state, LifeSection section, List<LifeEvent> events)
        {
            int stage = LifeEras.StageOf(section);
            if (stage <= state.EraStage) return;

            state.EraStage = stage;
            state.Era = LifeEras.Draw(state.Random, state.Era);
            events.Add(new LifeEvent(LifeEventType.EraChanged, state.CurrentSeat, value: (int)state.Era));
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

            // 進むマスで着いたマスは効果を出さない（「進む → 戻る → 進む」の連鎖を避けるため）。
            // 必ず止まるマスだけは効果を出す（ゴール・卒業・就職を素通りすると職業なしやゴールなしになるため）
            bool warped = state.IsWarping;
            state.IsWarping = false;
            if (warped && !LifeCellTypes.IsStop(cell.Type))
            {
                EndTurn(state, events);
                return;
            }

            events.Add(new LifeEvent(LifeEventType.Landed, player.Seat, cell.Amount, cell.Index));

            if (ApplyCell(state, player, cell, events)) return;

            EndTurn(state, events);
        }

        /// <summary>マスの効果を適用する。選択待ちになったか、進むマスで移動の続きを引き受けたら true（ここでは手番を終えない）</summary>
        private static bool ApplyCell(LifeGameState state, LifePlayerState player, LifeCell cell, List<LifeEvent> events)
        {
            LifeRuleConfig config = state.Config;
            switch (cell.Type)
            {
                case LifeCellType.Income:
                    LifeMoney.Receive(player, LifeEras.Multiply(state, cell.Type, cell.Amount), events);
                    return false;
                case LifeCellType.Expense:
                case LifeCellType.Tuition:
                    LifeMoney.PayExpense(state, player, LifeEras.Multiply(state, cell.Type, cell.Amount), LifeEvent.Bank, events);
                    return false;
                case LifeCellType.Sickness:
                    int sickness = LifeEras.Multiply(state, cell.Type, cell.Amount);
                    LifeMoney.PayMishap(state, player, sickness, LifeInsurance.Life, LifeJobRole.Healer, events);
                    return false;
                case LifeCellType.Accident:
                    int accident = LifeEras.Multiply(state, cell.Type, cell.Amount);
                    LifeMoney.PayMishap(state, player, accident, LifeInsurance.Auto, LifeJobRole.Police, events);
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
                case LifeCellType.Bet:
                    return WaitFor(state, LifePending.Bet, true);
                case LifeCellType.Present:
                    Present(state, player, cell.Amount, events);
                    return false;
                case LifeCellType.Nominate:
                case LifeCellType.SwapJob:
                    return WaitFor(state, LifePending.ChooseTarget, HasAnyTarget(state));
                case LifeCellType.Forward:
                    WarpForward(state, cell.Amount, events);
                    return true;
                case LifeCellType.Back:
                    WarpBack(state, player, cell.Amount, events);
                    return false;
                case LifeCellType.Rest:
                    player.RestTurns = RestTurnCount;
                    return false;
                case LifeCellType.Lottery:
                    return WaitFor(state, LifePending.Lottery, true);
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
            int damage = LifeEras.Multiply(state, LifeCellType.Fire, FireDamage(state.Config, player));
            if (!player.HasHouse)
            {
                // 家なしは小額の出費だけ。保険・手数料の対象外（仕様書 §5.2）
                LifeMoney.PayExpense(state, player, damage, LifeEvent.Bank, events);
                return;
            }

            LifeMoney.PayMishap(state, player, damage, LifeInsurance.Fire, LifeJobRole.Repair, events);
        }

        /// <summary>時代の倍率をかける前の火事の額。家なしは小額、家ありは家の値段の半分</summary>
        public static int FireDamage(LifeRuleConfig config, LifePlayerState player)
        {
            return player.HasHouse ? config.Houses[player.HouseId].Price / 2 : config.FireWithoutHouse;
        }

        /// <summary>
        /// 普通の移動と同じく、途中の給料日はもらい分岐では道を選ばせる。
        /// 着地（または分岐の後の着地）は ContinueMove → Land が IsWarping を見て効果なしで手番を終える
        /// </summary>
        private static void WarpForward(LifeGameState state, int steps, List<LifeEvent> events)
        {
            events.Add(new LifeEvent(LifeEventType.Warped, state.CurrentSeat, steps));
            state.IsWarping = true;
            state.StepsLeft = steps;
            ContinueMove(state, events);
        }

        /// <summary>区間の先頭で止まる（分岐・合流を逆走させないため）。給料日は通ってももらえない</summary>
        private static void WarpBack(LifeGameState state, LifePlayerState player, int steps, List<LifeEvent> events)
        {
            events.Add(new LifeEvent(LifeEventType.Warped, player.Seat, -steps));
            for (int i = 0; i < steps; i++)
            {
                LifeCell cell = state.Board[player.Position];
                if (cell.Prev == cell.Index) return;

                player.Position = cell.Prev;
                events.Add(new LifeEvent(LifeEventType.Moved, player.Seat, value: player.Position));
            }
        }

        private static void Present(LifeGameState state, LifePlayerState player, int amount, List<LifeEvent> events)
        {
            int receiver = PresentReceiver(state);
            if (receiver != NoTarget) LifeMoney.Pay(state, player, amount, receiver, events);
        }

        private static void Birth(LifeGameState state, LifePlayerState player, List<LifeEvent> events)
        {
            if (player.Children >= state.Config.MaxChildren) return;

            player.Children++;
            events.Add(new LifeEvent(LifeEventType.ChildBorn, player.Seat, value: player.Children));
            LifeMoney.CollectGifts(state, player, LifeEras.Multiply(state, LifeCellType.Birth, state.Config.BirthGift), events);
        }

        private static void Marry(LifeGameState state, LifePlayerState player, List<LifeEvent> events)
        {
            if (player.IsMarried) return;

            player.IsMarried = true;
            events.Add(new LifeEvent(LifeEventType.Married, player.Seat));
            LifeMoney.CollectGifts(state, player, LifeEras.Multiply(state, LifeCellType.Marriage, state.Config.MarriageGift), events);
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

        /// <summary>
        /// 賭けのルーレット。移動ではないので株の配当は出さず、LastRoll も変えない（振り直しの出目と混ざらないように）。
        /// 負けは所持金が足りなければ手形になる（ほかの支払いと同じ）
        /// </summary>
        private static void Bet(LifeGameState state, bool bet, List<LifeEvent> events)
        {
            if (bet)
            {
                LifePlayerState player = state.Current;
                int stake = state.CurrentCell.Amount;
                int roll = RollDice(state);
                bool won = roll >= state.Config.BetWinMin;
                events.Add(new LifeEvent(LifeEventType.BetResult, player.Seat, won ? stake : -stake, roll));

                if (won)
                {
                    player.GambleWinnings += stake;
                    LifeMoney.Receive(player, stake, events);
                }
                else LifeMoney.Pay(state, player, stake, LifeEvent.Bank, events);
            }

            EndTurn(state, events);
        }

        private static void ChooseTarget(LifeGameState state, int target, List<LifeEvent> events)
        {
            if (target != NoTarget)
            {
                LifePlayerState player = state.Current;
                LifePlayerState other = state.Players[target];
                if (state.CurrentCell.Type == LifeCellType.SwapJob)
                {
                    (player.JobId, other.JobId) = (other.JobId, player.JobId);
                    events.Add(new LifeEvent(LifeEventType.JobSwapped, player.Seat, value: player.JobId, otherSeat: target));
                }
                else
                {
                    // 指名：相手から受け取る。相手が払えなければ手形（ご祝儀と同じ）
                    LifeMoney.Pay(state, other, LifeEras.Multiply(state, LifeCellType.Nominate, state.CurrentCell.Amount), player.Seat, events);
                }
            }

            EndTurn(state, events);
        }

        /// <summary>
        /// 宝くじ。移動ではないので株の配当は出さず、LastRoll も変えない（賭けと同じ）。
        /// 番号は見た目側が全員分を並べてからルーレットを回せるよう、出目より先にイベントにする
        /// </summary>
        private static void DrawLottery(LifeGameState state, int number, List<LifeEvent> events)
        {
            int[] numbers = DealLotteryNumbers(state, number);
            for (int seat = 0; seat < numbers.Length; seat++)
            {
                events.Add(new LifeEvent(LifeEventType.LotteryTicket, seat, value: numbers[seat]));
            }

            int roll = RollDice(state);
            int winner = Array.IndexOf(numbers, roll);
            events.Add(new LifeEvent(LifeEventType.LotteryDrawn, state.CurrentSeat, value: roll, otherSeat: winner));

            if (winner >= 0)
            {
                state.Players[winner].GambleWinnings += state.Config.LotteryPrize;
                LifeMoney.Receive(state.Players[winner], state.Config.LotteryPrize, events);
            }
            else LifeMoney.Receive(state.Current, state.Config.LotteryConsolation, events);

            EndTurn(state, events);
        }

        /// <summary>
        /// 席ごとの番号。止まった人以外は、残りの番号をルールの乱数で混ぜて席順に配る
        /// （他の人に選ばせると手番以外の操作が要り、オンラインの送受信の作りを変えることになるため）
        /// </summary>
        private static int[] DealLotteryNumbers(LifeGameState state, int chosen)
        {
            var pool = new List<int>();
            for (int n = 1; n <= LifeRuleConfig.RouletteMax; n++)
            {
                if (n != chosen) pool.Add(n);
            }

            state.Random.Shuffle(pool);
            var numbers = new int[state.Players.Count];
            int next = 0;
            for (int seat = 0; seat < numbers.Length; seat++)
            {
                numbers[seat] = seat == state.CurrentSeat ? chosen : pool[next++];
            }

            return numbers;
        }

        // ---- 手番 ----

        private static void EndTurn(LifeGameState state, List<LifeEvent> events)
        {
            events.Add(new LifeEvent(LifeEventType.TurnEnded, state.CurrentSeat));

            var rested = new List<int>();
            int next = LifeTurnOrder.NextSeat(state.Players, state.CurrentSeat, rested);
            foreach (int seat in rested) events.Add(new LifeEvent(LifeEventType.Rested, seat));

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
