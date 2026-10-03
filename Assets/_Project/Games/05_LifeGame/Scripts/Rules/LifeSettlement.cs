using System;
using System.Collections.Generic;

namespace MiniGame.LifeGame
{
    /// <summary>精算の最後に発表する称号（B1）。並びがそのまま発表の順</summary>
    public enum LifeTitle
    {
        ManyChildren,
        StockKing,
        Turbulent,
        Gambler,
        Generous,
    }

    /// <summary>1人ぶんの精算の内訳（精算演出で1行ずつ見せるため、合計だけでなく内訳を持つ）</summary>
    public sealed class LifeSettlementEntry
    {
        public int Seat;
        public int HouseRoll;
        public int HouseSale;
        public int StockSale;
        public int InsuranceRefund;
        public int NoteRepayment;

        /// <summary>もらった称号（発表の順）と、そのボーナスの合計</summary>
        public List<LifeTitle> Titles = new List<LifeTitle>();
        public int TitleBonus;

        /// <summary>称号ボーナス込みの総資産。順位はこの額で決める</summary>
        public int Total;

        /// <summary>1 = 1位</summary>
        public int Rank;
    }

    /// <summary>
    /// 精算と順位（仕様書 §8）。家を売る → 株を買値で売る → 生命保険の返戻 → 約束手形を返済 → 称号ボーナス。
    /// </summary>
    public static class LifeSettlement
    {
        private static readonly LifeTitle[] AllTitles =
            { LifeTitle.ManyChildren, LifeTitle.StockKing, LifeTitle.Turbulent, LifeTitle.Gambler, LifeTitle.Generous };

        /// <summary>全員ゴール後に1回だけ呼ぶ。所持金を書き換え、席順の内訳を返す</summary>
        public static List<LifeSettlementEntry> Settle(LifeGameState state)
        {
            if (state.Pending != LifePending.Finished) throw new InvalidOperationException("全員ゴールする前に精算しようとした");

            var entries = new List<LifeSettlementEntry>();
            foreach (LifePlayerState player in state.Players) entries.Add(SettlePlayer(state, player));

            foreach (LifeTitle title in AllTitles) AwardTitle(state, entries, title);
            AssignRanks(state, entries);
            return entries;
        }

        private static LifeSettlementEntry SettlePlayer(LifeGameState state, LifePlayerState player)
        {
            LifeRuleConfig config = state.Config;
            var entry = new LifeSettlementEntry { Seat = player.Seat };

            if (player.HasHouse)
            {
                // 家の売却も同じ乱数から引く（オンラインで全端末の結果をそろえるため）
                LifeHouse house = config.Houses[player.HouseId];
                entry.HouseRoll = state.Random.Range(1, LifeRuleConfig.RouletteMax);
                entry.HouseSale = house.Price * house.SalePercent(entry.HouseRoll) / 100;
                player.HouseId = LifeRuleConfig.NoHouse;
            }

            entry.StockSale = player.Stocks.Count * config.StockPrice;
            player.Stocks.Clear();

            entry.InsuranceRefund = player.LifeInsurancePaid / 2;
            entry.NoteRepayment = player.Notes * config.NoteSettlement;
            player.Notes = 0;

            player.Money += entry.HouseSale + entry.StockSale + entry.InsuranceRefund - entry.NoteRepayment;
            entry.Total = player.Money;
            return entry;
        }

        /// <summary>称号の判定に使う値（子供の人数・配当の合計など）。発表で「子供 3人」のように見せるため公開する</summary>
        public static int TitleScore(LifePlayerState player, LifeTitle title)
        {
            switch (title)
            {
                case LifeTitle.ManyChildren: return player.Children;
                case LifeTitle.StockKing: return player.DividendTotal;
                case LifeTitle.Turbulent: return player.NotesIssued;
                case LifeTitle.Gambler: return player.GambleWinnings;
                case LifeTitle.Generous: return player.PaidToOthers;
                default: return 0;
            }
        }

        /// <summary>一番の人全員に（同点なら全員）ボーナスを渡す。誰も 0 なら出さない（例: 誰も手形を切っていない）</summary>
        private static void AwardTitle(LifeGameState state, List<LifeSettlementEntry> entries, LifeTitle title)
        {
            int best = 0;
            foreach (LifePlayerState player in state.Players) best = Math.Max(best, TitleScore(player, title));
            if (best == 0) return;

            int bonus = state.Config.TitleBonus;
            foreach (LifeSettlementEntry entry in entries)
            {
                LifePlayerState player = state.Players[entry.Seat];
                if (TitleScore(player, title) != best) continue;

                entry.Titles.Add(title);
                entry.TitleBonus += bonus;
                player.Money += bonus;
                entry.Total = player.Money;
            }
        }

        private static void AssignRanks(LifeGameState state, List<LifeSettlementEntry> entries)
        {
            var ranking = new List<LifeSettlementEntry>(entries);
            // 総資産が同じなら先にゴールした人が上（仕様書 §2.3）
            ranking.Sort((a, b) =>
            {
                if (a.Total != b.Total) return b.Total.CompareTo(a.Total);
                return state.Players[a.Seat].GoalOrder.CompareTo(state.Players[b.Seat].GoalOrder);
            });

            for (int i = 0; i < ranking.Count; i++) ranking[i].Rank = i + 1;
        }
    }
}
