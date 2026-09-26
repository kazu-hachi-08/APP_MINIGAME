namespace MiniGame.Molkky
{
    /// <summary>
    /// 1人分の点数・ミス回数・失格状態・人間/NPC。
    /// オンライン対戦（§19）で相手端末の結果をそのまま上書きできるよう、値は外から設定できるようにしている。
    /// </summary>
    public class PlayerSlot
    {
        public string Name { get; }
        public PlayerKind Kind { get; }
        public int Score { get; set; }
        public int MissCount { get; set; }
        public bool IsDisqualified { get; set; }

        public bool IsNpc => Kind != PlayerKind.Human;

        /// <summary>50点ちょうどまでの残り点数</summary>
        public int Remaining => MolkkyRules.TargetScore - Score;

        public PlayerSlot(string name, PlayerKind kind = PlayerKind.Human)
        {
            Name = name;
            Kind = kind;
        }
    }
}
