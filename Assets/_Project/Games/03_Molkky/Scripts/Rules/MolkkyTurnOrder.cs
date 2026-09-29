using System.Collections.Generic;

namespace MiniGame.Molkky
{
    /// <summary>
    /// 手番の計算。チームが番号順に交互に投げ、チームの中では席番号の若い順に投げる人が交代する。
    /// 個人戦は「1人チーム × 人数分」として同じ計算に乗せる。
    /// オンラインでも全端末がこの計算で手番を進めるので、手番そのものは送らない。
    /// </summary>
    public class MolkkyTurnOrder
    {
        private readonly List<int>[] _members;

        // チームごとの投げた回数。チーム内の誰が投げるかをこの回数で決める
        private readonly int[] _throwCounts;

        public int TeamCount => _members.Length;
        public int CurrentTeam { get; private set; }
        public int CurrentSeat => _members[CurrentTeam][_throwCounts[CurrentTeam] % _members[CurrentTeam].Count];

        /// <param name="seatTeams">席ごとのチーム番号（0 = チームA）。どのチームにも1人以上いること</param>
        public MolkkyTurnOrder(IReadOnlyList<int> seatTeams)
        {
            int teamCount = 0;
            foreach (int team in seatTeams)
            {
                if (team + 1 > teamCount) teamCount = team + 1;
            }

            _members = new List<int>[teamCount];
            for (int team = 0; team < teamCount; team++) _members[team] = new List<int>();

            // 席番号の若い順に並べておくと、そのままチーム内の投げる順になる
            for (int seat = 0; seat < seatTeams.Count; seat++) _members[seatTeams[seat]].Add(seat);

            _throwCounts = new int[teamCount];
        }

        /// <summary>個人戦：席ごとに別のチームにする</summary>
        public static MolkkyTurnOrder Individual(int playerCount)
        {
            var seatTeams = new int[playerCount];
            for (int seat = 0; seat < playerCount; seat++) seatTeams[seat] = seat;

            return new MolkkyTurnOrder(seatTeams);
        }

        /// <summary>チームの中で席番号が一番若い人。失格で勝ったときの勝利演出に使う</summary>
        public int FirstSeatOf(int team)
        {
            return _members[team][0];
        }

        /// <summary>今の人が投げ終わったので次へ進める。失格したチームは飛ばす。投げられるチームがなければ false</summary>
        public bool Advance(IReadOnlyList<TeamScore> teams)
        {
            _throwCounts[CurrentTeam]++;

            for (int step = 1; step <= TeamCount; step++)
            {
                int team = (CurrentTeam + step) % TeamCount;
                if (teams[team].IsDisqualified) continue;

                CurrentTeam = team;
                return true;
            }

            return false;
        }
    }
}
