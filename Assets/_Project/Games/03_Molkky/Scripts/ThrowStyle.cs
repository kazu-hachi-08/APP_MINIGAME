namespace MiniGame.Molkky
{
    /// <summary>
    /// 棒の投げ方（§7.5）。真上から見た棒の向きだけが違い、当たり幅が変わる。
    /// 縦＝当たり幅が細く1本を狙える／横＝当たり幅が広くまとめて倒せる。
    /// </summary>
    public enum ThrowStyle
    {
        Vertical,
        Horizontal,
    }
}
