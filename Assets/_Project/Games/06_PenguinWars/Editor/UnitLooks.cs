using System.Collections.Generic;

namespace MiniGame.PenguinWars.Editor
{
    /// <summary>
    /// 全キャラの見た目（仕様書 §5.5「見た目」列）。ここに無い No は基本ペンギンになる。
    /// 数値（UnitDefinitions）と分けているのは、2人で「バランス調整」と「見た目づくり」を同時に進めてもコンフリクトしないようにするため
    /// </summary>
    public static class UnitLooks
    {
        // 大型は同じドット数のまま引き伸ばして大きく見せる（きょだいペンギンだけ仕様書どおり3倍）
        private const int LargeScale = 2;
        private const int GiantScale = 3;

        private static readonly Dictionary<int, PenguinLook> Looks = new Dictionary<int, PenguinLook>
        {
            // 壁
            [2] = new PenguinLook(body: "wide"),
            [3] = new PenguinLook(hand: "big_snowball"),
            [4] = new PenguinLook(head: "helmet"),
            [5] = new PenguinLook(bodyColor: "ice"),
            [6] = new PenguinLook(body: "small", bodyColor: "gray"),
            [7] = new PenguinLook(head: "box"),
            [8] = new PenguinLook(body: "round"),
            [9] = new PenguinLook(back: "futon"),
            [10] = new PenguinLook(back: "kamakura"),
            // アタッカー
            [11] = new PenguinLook(hand: "axe"),
            [12] = new PenguinLook(hand: "fish_sword"),
            [13] = new PenguinLook(hand: "glove"),
            [14] = new PenguinLook(body: "wide", back: "mawashi"),
            [15] = new PenguinLook(back: "long_legs"),
            [16] = new PenguinLook(hand: "hammer"),
            [17] = new PenguinLook(head: "hood"),
            [18] = new PenguinLook(head: "chonmage", hand: "katana"),
            [19] = new PenguinLook(back: "bike"),
            [20] = new PenguinLook(head: "drill"),
            [21] = new PenguinLook(hand: "sword_shield"),
            [22] = new PenguinLook(body: "wide", hand: "muscle_arm"),
            // 遠距離
            [23] = new PenguinLook(hand: "bow"),
            [24] = new PenguinLook(hand: "snowball_throw"),
            [25] = new PenguinLook(hand: "fishing_rod"),
            [26] = new PenguinLook(body: "tall"),
            [27] = new PenguinLook(hand: "cannon"),
            [28] = new PenguinLook(head: "wizard_hat", hand: "staff"),
            [29] = new PenguinLook(hand: "sniper_rifle"),
            [30] = new PenguinLook(hand: "boomerang"),
            [31] = new PenguinLook(back: "rocket"),
            [32] = new PenguinLook(body: "tower"),
            // 妨害
            [33] = new PenguinLook(bodyColor: "ice", back: "snow_aura"),
            [34] = new PenguinLook(back: "freezer"),
            [35] = new PenguinLook(hand: "fan"),
            [36] = new PenguinLook(head: "nightcap"),
            [37] = new PenguinLook(head: "slime"),
            [38] = new PenguinLook(hand: "harisen"),
            [39] = new PenguinLook(head: "ribbon", hand: "mic"),
            [40] = new PenguinLook(back: "octopus_legs"),
            [41] = new PenguinLook(hand: "balloon"),
            [42] = new PenguinLook(bodyColor: "ice", head: "crown", back: "cape_blue"),
            // 大型
            [43] = new PenguinLook(scale: GiantScale),
            [44] = new PenguinLook(body: "robot", bodyColor: "gray", scale: LargeScale),
            [45] = new PenguinLook(back: "tank", scale: LargeScale),
            [46] = new PenguinLook(back: "iceberg", scale: LargeScale),
            [47] = new PenguinLook(back: "whale", scale: LargeScale),
            [48] = new PenguinLook(head: "crown", back: "cape_red", scale: LargeScale),
            [49] = new PenguinLook(bodyColor: "aurora", back: "aurora", scale: LargeScale),
            [50] = new PenguinLook(head: "halo", back: "wings", scale: LargeScale),
        };

        public static PenguinLook Get(int no)
        {
            return Looks.TryGetValue(no, out PenguinLook look) ? look : new PenguinLook();
        }
    }
}
