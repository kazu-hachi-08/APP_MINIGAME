namespace MiniGame.Molkky
{
    /// <summary>
    /// 試合のルール。Normal 以外は公式ルールにないパーティー用のモードで、毎手番ランダムな1本が特殊ピンになる
    /// </summary>
    public enum MolkkyRuleMode
    {
        Normal,

        /// <summary>金のピンを倒した投擲は得点が2倍</summary>
        Golden,

        /// <summary>爆弾ピンを倒した投擲は0点（ミス扱い）</summary>
        Bomb,
    }
}
