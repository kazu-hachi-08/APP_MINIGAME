namespace MiniGame.Molkky
{
    /// <summary>
    /// 1人分の点数・ミス回数・失格状態。
    /// オンライン対戦（§19）で相手端末の結果をそのまま上書きできるよう、値は外から設定できるようにしている。
    /// </summary>
    public class PlayerSlot
    {
        public string Name { get; }
        public int Score { get; set; }
        public int MissCount { get; set; }
        public bool IsDisqualified { get; set; }

        /// <summary>50点ちょうどまでの残り点数</summary>
        public int Remaining => MolkkyRules.TargetScore - Score;

        public PlayerSlot(string name)
        {
            Name = name;
        }
    }
}
