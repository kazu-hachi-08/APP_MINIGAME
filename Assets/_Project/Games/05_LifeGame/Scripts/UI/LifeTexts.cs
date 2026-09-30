using System.Collections.Generic;
using System.Text;

namespace MiniGame.LifeGame
{
    /// <summary>
    /// 表示用の文言。職業名・通貨・家・ルート名・マスの文面は選んだテーマ（LifeThemeData）から引く。
    /// ルール側は番号しか持たないので、番号 → 文言の変換はすべてここに集める。
    /// </summary>
    public static class LifeTexts
    {
        // 表示のあちこち（財布・選択肢・イベント表示）から引くので、試合ごとに1回だけ差し替える
        private static LifeThemeData _theme;

        public static void SetTheme(LifeThemeData theme) => _theme = theme;

        public static string PlayerName(int seat) => $"P{seat + 1}";

        public static string Money(int amount) => $"{amount:#,0}{_theme.Currency}";

        private static string SignedMoney(int amount) => amount >= 0 ? $"+{Money(amount)}" : $"-{Money(-amount)}";

        public static string JobName(int jobId) => jobId == LifeRuleConfig.NoJob ? "なし" : _theme.JobName(jobId);

        public static string HouseName(int houseId) => houseId == LifeRuleConfig.NoHouse ? "なし" : _theme.HouseName(houseId);

        public static string InsuranceName(LifeInsurance insurance)
        {
            switch (insurance)
            {
                case LifeInsurance.Life: return "生命保険";
                case LifeInsurance.Auto: return "自動車保険";
                case LifeInsurance.Fire: return "火災保険";
                default: return "";
            }
        }

        public static string RouteName(LifeSection section) => $"{_theme.RouteName(section)}ルート";

        public static string CellName(LifeCellType type)
        {
            switch (type)
            {
                case LifeCellType.Start: return "スタート";
                case LifeCellType.Branch: return "分岐";
                case LifeCellType.JobOffer: return "就職";
                case LifeCellType.Graduation: return "卒業";
                case LifeCellType.Payday: return "給料日";
                case LifeCellType.Marriage: return "結婚";
                case LifeCellType.Tuition: return "学費";
                case LifeCellType.Goal: return "ゴール";
                case LifeCellType.Income: return "収入";
                case LifeCellType.Expense: return "出費";
                case LifeCellType.Sickness: return "病気";
                case LifeCellType.Accident: return "事故";
                case LifeCellType.Fire: return "火事";
                case LifeCellType.Birth: return "出産";
                case LifeCellType.House: return "家";
                case LifeCellType.Insurance: return "保険";
                case LifeCellType.Stock: return "株";
                case LifeCellType.ChangeJob: return "転職";
                default: return type.ToString();
            }
        }

        /// <summary>マスに描く文字。金額が決まっているマスは金額も出す（シャッフル後に決まった値を見せるため）</summary>
        public static string CellLabel(LifeCell cell)
        {
            return cell.Amount > 0 ? $"{CellName(cell.Type)}\n{cell.Amount}" : CellName(cell.Type);
        }

        /// <summary>給料は手番の人の能力込みの額を見せる（がんばり屋が実際にもらえる額で比べられるように）</summary>
        public static string JobCard(LifeGameState state, int jobId) =>
            $"{JobName(jobId)}\n給料 {Money(LifeRules.SalaryOf(state.Config, state.Current, jobId))}";

        public static string HouseChoice(LifeRuleConfig config, int houseId) => $"{HouseName(houseId)}\n{Money(config.Houses[houseId].Price)}";

        public static string InsuranceChoice(LifeRuleConfig config, LifeInsurance insurance) =>
            $"{InsuranceName(insurance)}\n{Money(config.InsurancePrice(insurance))}";

        /// <summary>イベント表示の1行。表示しないイベント（移動・手番の切り替えなど）は null</summary>
        public static string Describe(LifeGameState state, LifeEvent e)
        {
            string who = PlayerName(e.Seat);
            switch (e.Type)
            {
                case LifeEventType.Dividend:
                    return $"{who} 株の配当（{e.Value}番） {SignedMoney(e.Amount)}";
                case LifeEventType.Salary:
                    return $"{who} 給料日！ {SignedMoney(e.Amount)}";
                case LifeEventType.Landed:
                    return Landed(state.Board[e.Value]);
                case LifeEventType.Income:
                    return $"{who} {SignedMoney(e.Amount)}";
                case LifeEventType.Payment:
                    string to = e.OtherSeat == LifeEvent.Bank ? "" : $"（{PlayerName(e.OtherSeat)}へ）";
                    return $"{who} {SignedMoney(-e.Amount)}{to}";
                case LifeEventType.InsuranceCovered:
                    return $"{who} {InsuranceName((LifeInsurance)e.Value)}で支払いなし！";
                case LifeEventType.NoteIssued:
                    return $"{who} お金が足りない！ 約束手形 {e.Value}枚";
                case LifeEventType.NoteRepaid:
                    return $"{who} 約束手形を{e.Value}枚返済";
                case LifeEventType.JobChanged:
                    int salary = LifeRules.SalaryOf(state.Config, state.Players[e.Seat], e.Value);
                    return $"{who} {JobName(e.Value)}になった（給料 {Money(salary)}）";
                case LifeEventType.Married:
                    return $"{who} 結婚おめでとう！";
                case LifeEventType.ChildBorn:
                    return $"{who} 子供が生まれた！（{e.Value}人目）";
                case LifeEventType.HouseBought:
                    return $"{who} {HouseName(e.Value)}を買った";
                case LifeEventType.InsuranceBought:
                    return $"{who} {InsuranceName((LifeInsurance)e.Value)}に入った";
                case LifeEventType.StockBought:
                    return $"{who} 株（{e.Value}番）を買った";
                case LifeEventType.Goal:
                    return $"{who} ゴール！ {e.Value + 1}着 ボーナス {SignedMoney(e.Amount)}";
                default:
                    return null;
            }
        }

        /// <summary>止まったマスの名前と、テーマの一言（あれば）</summary>
        private static string Landed(LifeCell cell)
        {
            string title = $"【{CellName(cell.Type)}】";
            string flavor = _theme.CellText(cell.Type, cell.TextVariant);
            return flavor == null ? title : $"{title}\n{flavor}";
        }

        /// <param name="owner">「P1 らっきー」のような席番号＋キャラ名</param>
        /// <param name="abilityText">キャラの能力の説明</param>
        public static string Wallet(LifeGameState state, int seat, string owner, string abilityText)
        {
            LifePlayerState player = state.Players[seat];
            var text = new StringBuilder();
            text.AppendLine($"{owner} の財布");
            text.AppendLine($"能力：{abilityText}{RerollState(player)}");
            text.AppendLine($"所持金：{Money(player.Money)}");
            text.AppendLine($"職業：{JobName(player.JobId)}（給料 {Money(LifeRules.SalaryOf(state.Config, player, player.JobId))}）");
            text.AppendLine($"家：{HouseName(player.HouseId)}");
            text.AppendLine($"保険：{InsuranceList(player.Insurances)}");
            text.AppendLine($"株：{StockList(player.Stocks)}");
            text.AppendLine($"約束手形：{player.Notes}枚");
            text.Append($"家族：{(player.IsMarried ? "結婚済み" : "独身")}・子供{player.Children}人");
            return text.ToString();
        }

        private static string RerollState(LifePlayerState player)
        {
            if (player.Ability != LifeAbility.Reroll) return "";

            return player.RerollUsed ? "（使用済み）" : "（あと1回）";
        }

        private static string InsuranceList(LifeInsurance insurances)
        {
            var names = new List<string>();
            foreach (LifeInsurance insurance in new[] { LifeInsurance.Life, LifeInsurance.Auto, LifeInsurance.Fire })
            {
                if ((insurances & insurance) != 0) names.Add(InsuranceName(insurance));
            }

            return names.Count == 0 ? "なし" : string.Join("・", names);
        }

        private static string StockList(List<int> stocks)
        {
            if (stocks.Count == 0) return "なし";

            var numbers = new List<string>();
            foreach (int stock in stocks) numbers.Add($"{stock}番");
            return string.Join("・", numbers);
        }

        /// <summary>精算の内訳を順位順に並べる（フェーズ6で1人ずつの演出 SettlementView に置き換える）</summary>
        public static string Settlement(List<LifeSettlementEntry> entries)
        {
            var ranking = new List<LifeSettlementEntry>(entries);
            ranking.Sort((a, b) => a.Rank.CompareTo(b.Rank));

            var text = new StringBuilder("精算\n");
            foreach (LifeSettlementEntry entry in ranking)
            {
                text.AppendLine();
                text.AppendLine($"{entry.Rank}位 {PlayerName(entry.Seat)}  総資産 {Money(entry.Total)}");
                text.AppendLine($"家 {SignedMoney(entry.HouseSale)} / 株 {SignedMoney(entry.StockSale)}");
                text.AppendLine($"保険 {SignedMoney(entry.InsuranceRefund)} / 手形 {SignedMoney(-entry.NoteRepayment)}");
            }

            return text.ToString().TrimEnd();
        }
    }
}
