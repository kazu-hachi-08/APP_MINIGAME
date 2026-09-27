namespace MiniGame.Molkky
{
    /// <summary>
    /// 棒の軌道（§7.6）。棒の向き（ThrowStyle）とは独立して選べる。
    /// 低め＝すぐ着地して滑りながら当たる／山なり＝手前のピンを飛び越えて、落ちた近くで止まる。
    /// </summary>
    public enum ThrowArc
    {
        Low,
        High,
    }
}
