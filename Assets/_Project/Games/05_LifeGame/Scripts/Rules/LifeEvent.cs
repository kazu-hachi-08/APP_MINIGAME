namespace MiniGame.LifeGame
{
    public enum LifeEventType
    {
        TurnStarted,
        Rolled,          // Value = 出目
        Dividend,        // Amount = 配当, Value = 株の番号
        Moved,           // Value = 移動先のマス
        Salary,          // Amount = 給料
        BranchReached,   // Value = 分岐のマス
        Landed,          // Value = 止まったマス
        Income,          // Amount = 受け取った額
        Payment,         // Amount = 払った額, OtherSeat = 受け取った人（-1 = 銀行）
        InsuranceCovered, // Value = 効いた保険（LifeInsurance）
        NoteIssued,      // Value = 増えた枚数
        NoteRepaid,      // Value = 返した枚数
        JobCardsDrawn,   // 引いたカードは LifeGameState.JobCards
        JobChanged,      // Value = 新しい職業
        Married,
        ChildBorn,       // Value = 子供の人数
        HouseBought,     // Value = 家の番号
        InsuranceBought, // Value = 保険（LifeInsurance）
        StockBought,     // Value = 株の番号
        Goal,            // Value = ゴール順（0 = 1着）, Amount = 順位ボーナス
        TurnEnded,
        AllGoaled,
    }

    /// <summary>ルールが起こしたこと。見た目側はこの列を順に演出する</summary>
    public readonly struct LifeEvent
    {
        public const int Bank = -1;

        public readonly LifeEventType Type;
        public readonly int Seat;
        public readonly int Amount;
        public readonly int Value;
        public readonly int OtherSeat;

        public LifeEvent(LifeEventType type, int seat, int amount = 0, int value = 0, int otherSeat = Bank)
        {
            Type = type;
            Seat = seat;
            Amount = amount;
            Value = value;
            OtherSeat = otherSeat;
        }

        public override string ToString() => $"{Type} P{Seat + 1} amount={Amount} value={Value} other={OtherSeat}";
    }
}
