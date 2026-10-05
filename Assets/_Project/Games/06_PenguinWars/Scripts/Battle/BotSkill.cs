namespace MiniGame.PenguinWars.Battle
{
    /// <summary>
    /// 検証用 bot（SimpleBot）の強さ。ステージの難しさを測る物差しなので、人間より強くしない
    /// （強い bot に合わせて調整すると、人間には難しすぎるステージになるため）
    /// </summary>
    public class BotSkill
    {
        public static readonly BotSkill Normal = new BotSkill("ふつう", 1.5f, 3);
        public static readonly BotSkill Skilled = new BotSkill("うまい", 0.5f, 5);

        public BotSkill(string name, float decisionInterval, int walletTargetLevel)
        {
            Name = name;
            DecisionInterval = decisionInterval;
            WalletTargetLevel = walletTargetLevel;
        }

        public string Name { get; }
        /// <summary>何秒ごとに判断するか。短いほど出撃・砲の遅れが少ない</summary>
        public float DecisionInterval { get; }
        /// <summary>働きペンギンをこのレベルまでは出撃より優先して上げる</summary>
        public int WalletTargetLevel { get; }
    }
}
