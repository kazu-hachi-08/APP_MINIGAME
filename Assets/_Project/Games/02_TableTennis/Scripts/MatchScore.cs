using UnityEngine;

namespace MiniGame.TableTennis
{
    /// <summary>
    /// 得点とサーブ権だけを持つ単純なスコア管理（MonoBehaviour ではない）。
    /// ルールは MVP 通り「11点先取・2ポイントごとにサーブ交代」のみで、デュースは扱わない。
    /// </summary>
    public class MatchScore
    {
        private readonly int _pointsToWin;
        private readonly int _serveInterval;
        private readonly CourtSide _firstServer;

        public int PlayerPoints { get; private set; }
        public int OpponentPoints { get; private set; }

        public MatchScore(int pointsToWin, int serveInterval, CourtSide firstServer)
        {
            _pointsToWin = Mathf.Max(1, pointsToWin);
            _serveInterval = Mathf.Max(1, serveInterval);
            _firstServer = firstServer;
        }

        public int TotalPoints => PlayerPoints + OpponentPoints;

        /// <summary>一定ポイントごとに入れ替わる現在のサーバー</summary>
        public CourtSide CurrentServer =>
            (TotalPoints / _serveInterval) % 2 == 0 ? _firstServer : _firstServer.Opposite();

        public bool IsFinished => PlayerPoints >= _pointsToWin || OpponentPoints >= _pointsToWin;

        public CourtSide Winner => PlayerPoints >= OpponentPoints ? CourtSide.Player : CourtSide.Opponent;

        public void AddPoint(CourtSide side)
        {
            if (side == CourtSide.Player)
            {
                PlayerPoints++;
            }
            else
            {
                OpponentPoints++;
            }
        }
    }
}
