namespace MiniGame.PenguinWars.Battle
{
    /// <summary>
    /// 検証用 bot（SimpleBot）の強さ。ステージの難しさを測る物差しなので、人間より強くしない
    /// （強い bot に合わせて調整すると、人間には難しすぎるステージになるため）
    /// </summary>
    public class BotSkill
    {
        public static readonly BotSkill Normal = new BotSkill("ふつう", 1.5f, 3, false);
        // Lv7（さかな上限3200）まで上げるのは、3章で解放される大型（〜2800）を出せるようにするため
        public static readonly BotSkill Skilled = new BotSkill("うまい", 0.5f, 7, true);

        public BotSkill(string name, float decisionInterval, int walletTargetLevel, bool savesForLarge)
        {
            Name = name;
            DecisionInterval = decisionInterval;
            WalletTargetLevel = walletTargetLevel;
            SavesForLarge = savesForLarge;
        }

        public string Name { get; }
        /// <summary>何秒ごとに判断するか。短いほど出撃・砲の遅れが少ない</summary>
        public float DecisionInterval { get; }
        /// <summary>働きペンギンをこのレベルまでは出撃より優先して上げる</summary>
        public int WalletTargetLevel { get; }
        /// <summary>大型を出すためにさかなを貯めるか（ふつうは貯めない＝大型を使いこなせない初心者の目安）</summary>
        public bool SavesForLarge { get; }
    }
}
