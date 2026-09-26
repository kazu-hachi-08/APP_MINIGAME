namespace MiniGame.Molkky
{
    /// <summary>1投の結果の種類。得点演出（§12.2）の出し分けに使う</summary>
    public enum ThrowOutcome
    {
        Miss,
        Scored,
        Win,
        OverTo25,
        Disqualified,
    }

    /// <summary>1投を MolkkyRules に通した結果</summary>
    public readonly struct ThrowResult
    {
        public readonly ThrowOutcome Outcome;
        public readonly int Points;
        public readonly int FallenCount;

        /// <summary>1本だけ倒したときのピンの数字。それ以外は0</summary>
        public readonly int SinglePinNumber;

        public ThrowResult(ThrowOutcome outcome, int points, int fallenCount, int singlePinNumber)
        {
            Outcome = outcome;
            Points = points;
            FallenCount = fallenCount;
            SinglePinNumber = singlePinNumber;
        }
    }
}
