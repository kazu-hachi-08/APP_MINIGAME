namespace MiniGame.Molkky
{
    /// <summary>
    /// 得点の単位（チーム）の点数・ミス回数・失格状態。
    /// チーム戦ではチーム内の誰が投げてもここに積み上がる。個人戦は「1人チーム × 人数分」として扱う。
    /// 席（人間/NPC・キャラ）は PlayerSlot に分けているのは、チーム戦で1つの得点を複数の席が共有するため。
    /// </summary>
    public class TeamScore
    {
        public int Score { get; set; }
        public int MissCount { get; set; }
        public bool IsDisqualified { get; set; }

        /// <summary>50点ちょうどまでの残り点数</summary>
        public int Remaining => MolkkyRules.TargetScore - Score;
    }
}
