namespace MiniGame.Golf
{
    /// <summary>
    /// ショットにかける回転（§7.7 スピン）。最初の着地のあとの転がりだけを変える。
    /// 値はオンラインで int として送るので、並びを変えないこと
    /// </summary>
    public enum ShotSpin
    {
        None = 0,

        /// <summary>着地ですぐ止まる。グリーンを狙うとき用</summary>
        Back = 1,

        /// <summary>着地後によく転がる。ランで距離を稼ぐとき用</summary>
        Top = 2,
    }
}
