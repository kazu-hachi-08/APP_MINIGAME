namespace MiniGame.LifeGame
{
    public enum LifeCommandType
    {
        Spin,

        /// <summary>Value = 分岐の道の番号（LifeCell.Next の添字）</summary>
        ChooseBranch,

        /// <summary>Value = 引いた2枚のどちらか（0/1）。転職のときは -1 で今の職業のまま</summary>
        ChooseJob,

        /// <summary>Value = 家の番号。-1 で買わない</summary>
        ChooseHouse,

        /// <summary>Value = 入る保険（LifeInsurance のビットの組み合わせ）。0 で入らない</summary>
        ChooseInsurance,

        /// <summary>Value = 買う株の番号（1〜10）。0 で買わない</summary>
        ChooseStock,

        /// <summary>Value = 返済する約束手形の枚数</summary>
        Repay,

        /// <summary>Value = 1 で振り直す、0 でこのまま進む</summary>
        ChooseReroll,

        /// <summary>Value = 1 で賭ける、0 でやめる</summary>
        ChooseBet,

        /// <summary>Value = 相手の席。入れ替えのときだけ -1 でやめる</summary>
        ChooseTarget,
    }

    /// <summary>
    /// プレイヤーの操作。オンラインではこれだけを送る（出目は送らない）ので、席番号と値だけの小さな形にしている。
    /// </summary>
    public readonly struct LifeCommand
    {
        public readonly int Seat;
        public readonly LifeCommandType Type;
        public readonly int Value;

        public LifeCommand(int seat, LifeCommandType type, int value = 0)
        {
            Seat = seat;
            Type = type;
            Value = value;
        }

        public override string ToString() => $"P{Seat + 1} {Type} {Value}";
    }
}
