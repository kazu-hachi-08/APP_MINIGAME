using System;
using System.Collections.Generic;

namespace MiniGame.LifeGame
{
    /// <summary>1人ぶんの精算の内訳（精算演出で1行ずつ見せるため、合計だけでなく内訳を持つ）</summary>
    public sealed class LifeSettlementEntry
    {
        public int Seat;
        public int HouseRoll;
        public int HouseSale;
        public int StockSale;
        public int InsuranceRefund;
        public int NoteRepayment;
        public int Total;

        /// <summary>1 = 1位</summary>
        public int Rank;
    }

    /// <summary>
    /// 精算と順位（仕様書 §8）。家を売る → 株を買値で売る → 生命保険の返戻 → 約束手形を返済。
    /// </summary>
    public static class LifeSettlement
    {
        /// <summary>全員ゴール後に1回だけ呼ぶ。所持金を書き換え、席順の内訳を返す</summary>
        public static List<LifeSettlementEntry> Settle(LifeGameState state)
        {
            if (state.Pending != LifePending.Finished) throw new InvalidOperationException("全員ゴールする前に精算しようとした");

            var entries = new List<LifeSettlementEntry>();
            foreach (LifePlayerState player in state.Players) entries.Add(SettlePlayer(state, player));

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
