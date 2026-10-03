using System.Collections.Generic;

namespace MiniGame.LifeGame
{
    /// <summary>
    /// NPCの選択（仕様書 §10.2 の表どおり）。運のゲームなので難易度は持たず、単純なルールだけで決める。
    /// </summary>
    public static class LifeNpcPlanner
    {
        private const int UniversityPercent = 45;
        private const int JobPercent = 40;
        private const int KeepMoney = 200;
        private const int RepayThreshold = 500;
        private const int RerollAtOrBelow = 3;

        // 賭けに負けても賭け金の分は手元に残る（手形を切らずに済む）ときだけ賭ける
        private const int BetMoneyMultiple = 2;

        /// <param name="random">
        /// NPC専用の乱数。ルールの乱数（state.Random）を使うと、NPCの考えた回数で出目の列がずれてしまうので分ける
        /// </param>
        public static LifeCommand Plan(LifeGameState state, LifeRandom random)
        {
            LifePlayerState player = state.Current;
            int seat = player.Seat;
            switch (state.Pending)
            {
                case LifePending.Spin:
                    int repay = PlanRepay(state.Config, player);
                    return repay > 0 ? new LifeCommand(seat, LifeCommandType.Repay, repay) : new LifeCommand(seat, LifeCommandType.Spin);
                case LifePending.Reroll:
                    return new LifeCommand(seat, LifeCommandType.ChooseReroll, state.LastRoll <= RerollAtOrBelow ? 1 : 0);
                case LifePending.Branch:
                    return new LifeCommand(seat, LifeCommandType.ChooseBranch, PlanBranch(state, random));
                case LifePending.JobCard:
                    return new LifeCommand(seat, LifeCommandType.ChooseJob, BestCard(state));
                case LifePending.ChangeJob:
                    return new LifeCommand(seat, LifeCommandType.ChooseJob, PlanChangeJob(state));
                case LifePending.House:
                    return new LifeCommand(seat, LifeCommandType.ChooseHouse, PlanHouse(state.Config, player));
                case LifePending.Insurance:
                    return new LifeCommand(seat, LifeCommandType.ChooseInsurance, (int)PlanInsurance(state.Config, player));
                case LifePending.Stock:
                    return new LifeCommand(seat, LifeCommandType.ChooseStock, PlanStock(state.Config, player, random));
                case LifePending.Bet:
                    bool bet = player.Money >= state.CurrentCell.Amount * BetMoneyMultiple;
                    return new LifeCommand(seat, LifeCommandType.ChooseBet, bet ? 1 : 0);
                case LifePending.ChooseTarget:
                    return new LifeCommand(seat, LifeCommandType.ChooseTarget, PlanTarget(state));
                case LifePending.Lottery:
                    // どの番号でも当たる確率は同じなので、考えずにランダム
                    return new LifeCommand(seat, LifeCommandType.ChooseLottery, random.Range(1, LifeRuleConfig.RouletteMax));
                default:
                    return new LifeCommand(seat, LifeCommandType.Spin);
            }
        }

        /// <summary>所持金が500以上なら返せるだけ返す。返さないなら 0</summary>
        private static int PlanRepay(LifeRuleConfig config, LifePlayerState player)
        {
            if (player.Notes == 0 || player.Money < RepayThreshold) return 0;

            int affordable = player.Money / config.NoteUnit;
            return affordable < player.Notes ? affordable : player.Notes;
        }

        private static int PlanBranch(LifeGameState state, LifeRandom random)
        {
            LifeBoard board = state.Board;
            LifeCell branch = state.CurrentCell;

            if (board.BranchChoiceOf(branch, LifeSection.University) >= 0)
            {
                int roll = random.Next(100);
                if (roll < UniversityPercent) return board.BranchChoiceOf(branch, LifeSection.University);
                if (roll < UniversityPercent + JobPercent) return board.BranchChoiceOf(branch, LifeSection.Job);
                return board.BranchChoiceOf(branch, LifeSection.Freeter);
            }

            // 負けている人ほど一発逆転を狙う
            LifeSection route = state.Current.Money < AverageMoney(state.Players) ? LifeSection.Gamble : LifeSection.Safe;
            return board.BranchChoiceOf(branch, route);
        }

        private static int AverageMoney(List<LifePlayerState> players)
        {
            int sum = 0;
            foreach (LifePlayerState player in players) sum += player.Money;
            return sum / players.Count;
        }

        private static int BestCard(LifeGameState state)
        {
            List<int> cards = state.JobCards;
            int best = 0;
            for (int i = 1; i < cards.Count; i++)
            {
                if (state.Config.SalaryOf(cards[i]) > state.Config.SalaryOf(cards[best])) best = i;
            }

            return best;
        }

        private static int PlanChangeJob(LifeGameState state)
        {
            int best = BestCard(state);
            int bestSalary = state.Config.SalaryOf(state.JobCards[best]);
            return bestSalary > state.Config.SalaryOf(state.Current.JobId) ? best : -1;
        }

        /// <summary>手形を切らずに買える一番高い家。買えなければ -1</summary>
        private static int PlanHouse(LifeRuleConfig config, LifePlayerState player)
        {
            int best = LifeRuleConfig.NoHouse;
            for (int i = 0; i < config.Houses.Length; i++)
            {
                int price = config.Houses[i].Price;
                if (price > player.Money) continue;
                if (best == LifeRuleConfig.NoHouse || price > config.Houses[best].Price) best = i;
            }

            return best;
        }

        private static LifeInsurance PlanInsurance(LifeRuleConfig config, LifePlayerState player)
        {
            LifeInsurance available = LifeRules.AvailableInsurances(player);
            LifeInsurance chosen = LifeInsurance.None;
            int money = player.Money;

            foreach (LifeInsurance insurance in new[] { LifeInsurance.Life, LifeInsurance.Auto, LifeInsurance.Fire })
            {
                if ((available & insurance) == 0) continue;

                int price = config.InsurancePrice(insurance);
                if (money - price < KeepMoney) continue;

                chosen |= insurance;
                money -= price;
            }

            return chosen;
        }

        /// <summary>指名は所持金が一番多い人、入れ替えは自分より給料が高い人の中で一番高い人（いなければやめる）</summary>
        private static int PlanTarget(LifeGameState state)
        {
            bool isSwap = state.CurrentCell.Type == LifeCellType.SwapJob;
            int best = LifeRules.NoTarget;
            int bestScore = isSwap ? state.Config.SalaryOf(state.Current.JobId) : int.MinValue;
            foreach (LifePlayerState other in state.Players)
            {
                if (!LifeRules.IsValidTarget(state, other.Seat)) continue;

                int score = isSwap ? state.Config.SalaryOf(other.JobId) : other.Money;
                if (score > bestScore)
                {
                    best = other.Seat;
                    bestScore = score;
                }
            }

            return best;
        }

        /// <summary>買った後に200以上残るなら、持っていない番号をランダムで1枚。買わないなら 0</summary>
        private static int PlanStock(LifeRuleConfig config, LifePlayerState player, LifeRandom random)
        {
            if (player.Money - config.StockPrice < KeepMoney) return 0;

            var candidates = new List<int>();
            for (int number = 1; number <= LifeRuleConfig.RouletteMax; number++)
            {
                if (!player.Stocks.Contains(number)) candidates.Add(number);
            }

            return candidates[random.Next(candidates.Count)];
        }
    }
}
