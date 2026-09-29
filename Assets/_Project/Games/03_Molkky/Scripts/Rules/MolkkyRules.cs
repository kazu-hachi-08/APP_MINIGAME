using System.Collections.Generic;

namespace MiniGame.Molkky
{
    /// <summary>
    /// モルックの得点ルール。
    /// 物理と無関係なので MonoBehaviour にせず、EditModeテストで検証できる純粋クラスにしている。
    /// </summary>
    public static class MolkkyRules
    {
        public const int TargetScore = 50;
        public const int OverResetScore = 25;
        public const int MaxConsecutiveMisses = 3;

        /// <summary>倒れたピンの数字から得点を求める（1本＝数字／複数本＝本数）</summary>
        public static int CalculatePoints(IReadOnlyList<int> fallenNumbers)
        {
            if (fallenNumbers.Count == 1) return fallenNumbers[0];

            return fallenNumbers.Count;
        }

        /// <summary>1投の結果をチームの得点に反映する（個人戦は1人チーム）</summary>
        public static ThrowResult ApplyThrow(TeamScore team, IReadOnlyList<int> fallenNumbers)
        {
            int points = CalculatePoints(fallenNumbers);
            int single = fallenNumbers.Count == 1 ? fallenNumbers[0] : 0;

            if (points == 0) return RegisterMiss(team);

            // ミスの連続回数で失格を判定するので、1本でも倒したら数え直す
            team.MissCount = 0;
            team.Score += points;

            if (team.Score == TargetScore)
            {
                return new ThrowResult(ThrowOutcome.Win, points, fallenNumbers.Count, single);
            }

            if (team.Score > TargetScore)
            {
                team.Score = OverResetScore;
                return new ThrowResult(ThrowOutcome.OverTo25, points, fallenNumbers.Count, single);
            }

            return new ThrowResult(ThrowOutcome.Scored, points, fallenNumbers.Count, single);
        }

        private static ThrowResult RegisterMiss(TeamScore team)
        {
            team.MissCount++;
            if (team.MissCount >= MaxConsecutiveMisses)
            {
                team.IsDisqualified = true;
                return new ThrowResult(ThrowOutcome.Disqualified, 0, 0, 0);
            }

            return new ThrowResult(ThrowOutcome.Miss, 0, 0, 0);
        }

        /// <summary>失格していないチームが1つだけならその番号。それ以外は -1</summary>
        public static int FindSoleSurvivor(IReadOnlyList<TeamScore> teams)
        {
            int survivor = -1;
            for (int i = 0; i < teams.Count; i++)
            {
                if (teams[i].IsDisqualified) continue;
                if (survivor >= 0) return -1;

                survivor = i;
            }

            return survivor;
        }
    }
}
