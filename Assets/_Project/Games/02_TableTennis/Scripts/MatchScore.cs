using UnityEngine;

namespace MiniGame.TableTennis
{
    /// <summary>
    /// 得点とサーブ権だけを持つ単純なスコア管理（MonoBehaviour ではない）。
    /// 規定点先取・一定ポイントごとにサーブ交代。規定点-1 で並んだらデュース（2点差がつくまで続行、サーブは1本交代）。
    /// </summary>
    public class MatchScore
    {
        // 公式ルールに合わせ、デュース後は2点差で決着
        private const int WinMargin = 2;

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

        /// <summary>デュースに入る合計点（11点先取なら 10-10 の 20）</summary>
        private int DeuceStartTotal => (_pointsToWin - 1) * 2;

        /// <summary>デュース圏内（両者が規定点-1 以上）か。アドバンテージ中も含む</summary>
        public bool IsInDeuce => PlayerPoints >= _pointsToWin - 1 && OpponentPoints >= _pointsToWin - 1;

        /// <summary>デュース圏内で同点（「DEUCE」と表示するタイミング）</summary>
        public bool IsDeuce => IsInDeuce && PlayerPoints == OpponentPoints;

        /// <summary>通常は一定ポイントごと、デュース圏内は1本ごとに入れ替わる現在のサーバー</summary>
        public CourtSide CurrentServer
        {
            get
            {
                // デュース直前までの交代順を引き継いだうえで、以降は1本ごとに交代する
                int turn = IsInDeuce
                    ? DeuceStartTotal / _serveInterval + (TotalPoints - DeuceStartTotal)
                    : TotalPoints / _serveInterval;
                return turn % 2 == 0 ? _firstServer : _firstServer.Opposite();
            }
        }

        public bool IsFinished
        {
            get
            {
                int leader = Mathf.Max(PlayerPoints, OpponentPoints);
                int lead = Mathf.Abs(PlayerPoints - OpponentPoints);
                return leader >= _pointsToWin && lead >= WinMargin;
            }
        }

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
