namespace CardGame.Core.Ai
{
    /// <summary>
    /// 貪欲 AI の盤面評価の重み。playtest で調整するため、値を 1 か所にまとめている。
    /// CLI の selfplay から `--ai-hand` などで上書きして掃引できる(既定値が本番の値)。
    /// </summary>
    public static class AiTuning
    {
        /// <summary>手札 1 枚の価値。低すぎると「自分のフォロワーを破壊してドロー」する類のカードを AI が使わない。</summary>
        public static double HandCard = 1.0;

        /// <summary>フォロワー 1 体の基礎価値(攻撃力 + 体力 に加算)。場に残ること自体の価値。</summary>
        public static double FollowerBase = 0.5;

        /// <summary>アミュレットの基礎価値。</summary>
        public static double AmuletBase = 1.0;

        /// <summary>ラストワードを持つフォロワーの補正(破壊されても仕事が残るぶん、失う痛みが小さい)。</summary>
        public static double LastWordsDiscount = 1.0;

        /// <summary>相手の守護に塞がれて本体を殴れない打点 1 あたりのペナルティ。0 だと「割るとラストワードが出る守護」を AI が割らずに固まる。</summary>
        public static double BlockedByWard = 0.5;

        /// <summary>ラストワードで出るフォロワーの (攻撃力 + 体力) に掛ける重み。0 だと「割れて孵る」系のカードを AI が出し渋る。</summary>
        public static double LastWordsSummon = 0.5;

        /// <summary>デッキ切れが近いときのペナルティ(残り枚数がこれ以下で効き始める)。</summary>
        public static int DeckOutWarning = 5;

        /// <summary>既定値に戻す(テストや掃引の後始末用)。</summary>
        public static void Reset()
        {
            HandCard = 1.0;
            FollowerBase = 0.5;
            AmuletBase = 1.0;
            LastWordsDiscount = 1.0;
            BlockedByWard = 0.5;
            LastWordsSummon = 0.5;
            DeckOutWarning = 5;
        }
    }
}
