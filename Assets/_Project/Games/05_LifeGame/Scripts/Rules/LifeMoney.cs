using System.Collections.Generic;

namespace MiniGame.LifeGame
{
    /// <summary>
    /// お金の出し入れ。足りない分の約束手形・ご祝儀・係の手数料をここに集める（どのマスからも同じ払い方をするため）。
    /// </summary>
    public static class LifeMoney
    {
        public static void Receive(LifePlayerState player, int amount, List<LifeEvent> events)
        {
            if (amount <= 0) return;

            player.Money += amount;
            events.Add(new LifeEvent(LifeEventType.Income, player.Seat, amount));
        }

        /// <summary>払う。足りなければ自動で約束手形を切る（仕様書 §7.6）。toSeat が -1 なら銀行へ</summary>
        public static void Pay(LifeGameState state, LifePlayerState payer, int amount, int toSeat, List<LifeEvent> events)
        {
            if (amount <= 0) return;

            IssueNotesIfShort(state.Config, payer, amount, events);
            payer.Money -= amount;
            if (toSeat != LifeEvent.Bank) state.Players[toSeat].Money += amount;

            events.Add(new LifeEvent(LifeEventType.Payment, payer.Seat, amount, otherSeat: toSeat));
        }

        private static void IssueNotesIfShort(LifeRuleConfig config, LifePlayerState payer, int amount, List<LifeEvent> events)
        {
            int shortfall = amount - payer.Money;
            if (shortfall <= 0) return;

            // 1枚単位で切り上げる
            int notes = (shortfall + config.NoteUnit - 1) / config.NoteUnit;
            payer.Notes += notes;
            payer.Money += notes * config.NoteUnit;
            events.Add(new LifeEvent(LifeEventType.NoteIssued, payer.Seat, notes * config.NoteUnit, notes));
        }

        public static bool CanRepay(LifeRuleConfig config, LifePlayerState player, int notes)
        {
            return notes >= 1 && notes <= player.Notes && player.Money >= notes * config.NoteUnit;
        }

        public static void Repay(LifeRuleConfig config, LifePlayerState player, int notes, List<LifeEvent> events)
        {
            player.Notes -= notes;
            player.Money -= notes * config.NoteUnit;
            events.Add(new LifeEvent(LifeEventType.NoteRepaid, player.Seat, notes * config.NoteUnit, notes));
        }

        /// <summary>他の全員から1人ずつご祝儀を受け取る。払えない人は手形になる（手番でなくても）</summary>
        public static void CollectGifts(LifeGameState state, LifePlayerState receiver, int amount, List<LifeEvent> events)
        {
            foreach (LifePlayerState other in state.Players)
            {
                if (other.Seat == receiver.Seat) continue;

                Pay(state, other, amount, receiver.Seat, events);
            }
        }

        /// <summary>
        /// 病気・事故・火事の支払い。保険があれば無効、係の人がいればその人へ払う。
        /// </summary>
        public static void PayMishap(LifeGameState state, LifePlayerState payer, int amount, LifeInsurance insurance, LifeJobRole role, List<LifeEvent> events)
        {
            if ((payer.Insurances & insurance) != 0)
            {
                events.Add(new LifeEvent(LifeEventType.InsuranceCovered, payer.Seat, amount, (int)insurance));
                return;
            }

            Pay(state, payer, amount, FindFeeReceiver(state, payer.Seat, role), events);
        }

        /// <summary>
        /// 手数料を受け取る席。払う人自身が係なら銀行（自分に払っても意味がないため）、
        /// それ以外は係の人のうち席番号の若い人（仕様書 §5.2・§7.1）。
        /// </summary>
        public static int FindFeeReceiver(LifeGameState state, int payerSeat, LifeJobRole role)
        {
            if (state.Config.RoleOf(state.Players[payerSeat].JobId) == role) return LifeEvent.Bank;

            foreach (LifePlayerState player in state.Players)
            {
                if (player.Seat != payerSeat && state.Config.RoleOf(player.JobId) == role) return player.Seat;
            }

            return LifeEvent.Bank;
        }

        /// <summary>出目と同じ番号の株を持つ人全員に、1枚ごとに配当を払う（仕様書 §7.4）</summary>
        public static void PayDividends(LifeGameState state, int roll, List<LifeEvent> events)
        {
            foreach (LifePlayerState player in state.Players)
            {
                int count = 0;
                foreach (int stock in player.Stocks)
                {
                    if (stock == roll) count++;
                }

                if (count == 0) continue;

                player.Money += count * state.Config.Dividend;
                events.Add(new LifeEvent(LifeEventType.Dividend, player.Seat, count * state.Config.Dividend, roll));
            }
        }
    }
}
