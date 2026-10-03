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

        public static string SignedMoney(int amount) => amount >= 0 ? $"+{Money(amount)}" : $"-{Money(-amount)}";

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

        /// <summary>
        /// マスのアイコンの下に描く1行。金額が決まっているマスは金額（シャッフル後に決まった値を見せるため）、
        /// それ以外は名前（アイコンだけだと覚えるまで何のマスか分からないため）
        /// </summary>
        public static string CellLabel(LifeCell cell)
        {
            return cell.Amount > 0 ? cell.Amount.ToString() : CellName(cell.Type);
        }

        /// <summary>マスの効果の1行。手番の人の能力・保険・家を反映した額を出す（押した人がそのまま比べられるように）</summary>
        public static string CellEffect(LifeGameState state, LifeCell cell)
        {
            LifeRuleConfig config = state.Config;
            LifePlayerState player = state.Current;
            switch (cell.Type)
            {
                case LifeCellType.Income: return $"{SignedMoney(cell.Amount)} もらえる";
                case LifeCellType.Expense:
                case LifeCellType.Tuition: return $"{SignedMoney(-LifeMoney.Discounted(config, player, cell.Amount))} 払う";
                case LifeCellType.Sickness: return MishapEffect(config, player, cell.Amount, LifeInsurance.Life);
                case LifeCellType.Accident: return MishapEffect(config, player, cell.Amount, LifeInsurance.Auto);
                case LifeCellType.Fire:
                    if (!player.HasHouse) return $"{SignedMoney(-LifeMoney.Discounted(config, player, config.FireWithoutHouse))} 払う（家なし）";
                    return MishapEffect(config, player, config.Houses[player.HouseId].Price / 2, LifeInsurance.Fire);
                case LifeCellType.Birth: return $"子供が生まれる。全員からお祝い {Money(config.BirthGift)}ずつ";
                case LifeCellType.Marriage: return $"必ず止まる。結婚して全員からお祝い {Money(config.MarriageGift)}ずつ";
                case LifeCellType.Payday: return $"通るだけで給料 {Money(LifeRules.SalaryOf(config, player, player.JobId))}";
                case LifeCellType.JobOffer: return "必ず止まる。職業カード2枚から選ぶ";
                case LifeCellType.Graduation: return "必ず止まる。上級職を含む職業カード2枚から選ぶ";
                case LifeCellType.ChangeJob: return "転職できる（今のままでもOK）";
                case LifeCellType.House: return player.HasHouse ? "家を持っているので何もなし" : "家を買える";
                case LifeCellType.Insurance: return "保険に入れる";
                case LifeCellType.Stock: return $"株を1枚 {Money(config.StockPrice)} で買える";
                case LifeCellType.Branch: return "道を選ぶ";
                case LifeCellType.Goal: return $"必ず止まる。1着ボーナス {Money(config.GoalBonuses[0])}";
                default: return "何もなし";
            }
        }

        private static string MishapEffect(LifeRuleConfig config, LifePlayerState player, int amount, LifeInsurance insurance)
        {
            if ((player.Insurances & insurance) != 0) return $"{InsuranceName(insurance)}があるので払わない";

            return $"{SignedMoney(-LifeMoney.Discounted(config, player, amount))} 払う（{InsuranceName(insurance)}なら0）";
        }

        /// <summary>給料は手番の人の能力込みの額を見せる（がんばり屋が実際にもらえる額で比べられるように）</summary>
        public static string JobCard(LifeGameState state, int jobId) =>
            $"{JobName(jobId)}\n給料 {Money(LifeRules.SalaryOf(state.Config, state.Current, jobId))}";

        public static string HouseChoice(LifeRuleConfig config, int houseId) => $"{HouseName(houseId)}\n{Money(config.Houses[houseId].Price)}";

        public static string InsuranceChoice(LifeRuleConfig config, LifeInsurance insurance) =>
            $"{InsuranceName(insurance)}\n{Money(config.InsurancePrice(insurance))}";

        /// <summary>
        /// イベント表示の1行。表示しないイベント（移動・手番の切り替え・止まったマス）は null。
        /// 止まったマスは名前とアイコンを別の欄で大きく出すので、ここでは行にしない
        /// </summary>
        /// <param name="nameplateSeat">名札に出ている席。その人の行は名前を省く（同じ名前が何度も並んで読みにくいため）</param>
        public static string Describe(LifeGameState state, LifeEvent e, int nameplateSeat)
        {
            string who = e.Seat == nameplateSeat ? "" : $"{PlayerName(e.Seat)} ";
            switch (e.Type)
            {
                case LifeEventType.Dividend:
                    return $"{who}株の配当（{e.Value}番） {SignedMoney(e.Amount)}";
                case LifeEventType.Salary:
                    return $"{who}給料日！ {SignedMoney(e.Amount)}";
                case LifeEventType.Income:
                    return $"{who}{SignedMoney(e.Amount)}";
                case LifeEventType.Payment:
                    string to = e.OtherSeat == LifeEvent.Bank ? "" : $"（{PlayerName(e.OtherSeat)}へ）";
                    return $"{who}{SignedMoney(-e.Amount)}{to}";
                case LifeEventType.InsuranceCovered:
                    return $"{who}{InsuranceName((LifeInsurance)e.Value)}で支払いなし！";
                case LifeEventType.NoteIssued:
                    return $"{who}お金が足りない！ 約束手形 {e.Value}枚";
                case LifeEventType.NoteRepaid:
                    return $"{who}約束手形を{e.Value}枚返済";
                case LifeEventType.JobChanged:
                    int salary = LifeRules.SalaryOf(state.Config, state.Players[e.Seat], e.Value);
                    return $"{who}{JobName(e.Value)}になった（給料 {Money(salary)}）";
                case LifeEventType.Married:
                    return $"{who}結婚おめでとう！";
                case LifeEventType.ChildBorn:
                    return $"{who}子供が生まれた！（{e.Value}人目）";
                case LifeEventType.HouseBought:
                    return $"{who}{HouseName(e.Value)}を買った";
                case LifeEventType.InsuranceBought:
                    return $"{who}{InsuranceName((LifeInsurance)e.Value)}に入った";
                case LifeEventType.StockBought:
                    return $"{who}株（{e.Value}番）を買った";
                case LifeEventType.Goal:
                    return $"{who}ゴール！ {e.Value + 1}着 ボーナス {SignedMoney(e.Amount)}";
                default:
                    return null;
            }
        }

        /// <summary>止まったマスのテーマの一言。無いマスは null</summary>
        public static string CellFlavor(LifeCell cell) => _theme.CellText(cell.Type, cell.TextVariant);

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

        // ------------------------------------------------------------------
        // 精算（SettlementView に1行ずつ出す）
        // ------------------------------------------------------------------
        public static string SettlementMoney(int money) => $"所持金 {Money(money)}";

        public static string SettlementTotal(int total) => $"総資産 {Money(total)}";

        public static string HouseSale(LifeSettlementEntry entry) =>
            entry.HouseRoll > 0 ? $"家を売った（出目 {entry.HouseRoll}） {SignedMoney(entry.HouseSale)}" : "家 なし";

        public static string StockSale(LifeSettlementEntry entry) =>
            entry.StockSale > 0 ? $"株を買値で売った {SignedMoney(entry.StockSale)}" : "株 なし";

        public static string InsuranceRefund(LifeSettlementEntry entry) =>
            entry.InsuranceRefund > 0 ? $"生命保険の満期返戻 {SignedMoney(entry.InsuranceRefund)}" : "生命保険 なし";

        public static string NoteRepayment(LifeSettlementEntry entry) =>
            entry.NoteRepayment > 0 ? $"約束手形を返済 {SignedMoney(-entry.NoteRepayment)}" : "約束手形 なし";

        /// <summary>順位発表の行（見出しはイベント表示のタイトル欄に出す）</summary>
        /// <param name="displayName">席番号 → 「P1 らっきー」のような表示名</param>
        public static string Ranking(List<LifeSettlementEntry> entries, System.Func<int, string> displayName)
        {
            var ranking = new List<LifeSettlementEntry>(entries);
            ranking.Sort((a, b) => a.Rank.CompareTo(b.Rank));

            var lines = new List<string>();
            foreach (LifeSettlementEntry entry in ranking)
            {
                lines.Add($"{entry.Rank}位 {displayName(entry.Seat)}  {Money(entry.Total)}");
            }

            return string.Join("\n", lines);
        }
    }
}
