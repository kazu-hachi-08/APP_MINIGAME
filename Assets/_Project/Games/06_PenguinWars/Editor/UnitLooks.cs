using System.Collections.Generic;

namespace MiniGame.PenguinWars.Editor
{
    /// <summary>
    /// 全キャラの見た目（仕様書 §5.5「見た目」列）。ここに無い No は基本ペンギンになる。
    /// 数値（UnitDefinitions）と分けているのは、2人で「バランス調整」と「見た目づくり」を同時に進めてもコンフリクトしないようにするため
    /// </summary>
    public static class UnitLooks
    {
        // 大型の仮の拡大率（Phase 9 でパーツを付けるまで、大きさだけで大型と分かるように）
        private const int LargeScale = 2;
        private const int GiantScale = 3;

        // Phase 8 時点: パーツがあるのは Phase 6 の10体だけ。残りは体の形・体色・拡大率だけの仮（パーツは Phase 9）
        private static readonly Dictionary<int, PenguinLook> Looks = new Dictionary<int, PenguinLook>
        {
            [2] = new PenguinLook(body: "wide"),
            [4] = new PenguinLook(head: "helmet"),
            [5] = new PenguinLook(bodyColor: "ice"),
            [6] = new PenguinLook(bodyColor: "gray"),
            [11] = new PenguinLook(hand: "axe"),
            [13] = new PenguinLook(hand: "glove"),
            [14] = new PenguinLook(body: "wide"),
            [15] = new PenguinLook(body: "tall"),
            [19] = new PenguinLook(back: "bike"),
            [22] = new PenguinLook(body: "wide"),
            [23] = new PenguinLook(hand: "bow"),
            [26] = new PenguinLook(body: "tall"),
            [32] = new PenguinLook(body: "tall"),
            [33] = new PenguinLook(bodyColor: "ice"),
            [34] = new PenguinLook(back: "freezer"),
            [42] = new PenguinLook(bodyColor: "ice"),
            [43] = new PenguinLook(scale: GiantScale),
            [44] = new PenguinLook(bodyColor: "gray", scale: LargeScale),
            [45] = new PenguinLook(scale: LargeScale),
            [46] = new PenguinLook(bodyColor: "ice", scale: LargeScale),
            [47] = new PenguinLook(scale: LargeScale),
            [48] = new PenguinLook(scale: LargeScale),
            [49] = new PenguinLook(scale: LargeScale),
            [50] = new PenguinLook(scale: LargeScale),
        };

        public static PenguinLook Get(int no)
        {
            return Looks.TryGetValue(no, out PenguinLook look) ? look : new PenguinLook();
        }
    }
}
