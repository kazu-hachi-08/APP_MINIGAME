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
        public const int GoldenMultiplier = 2;

        /// <summary>特殊ピンがない（通常ルール）ことを表すピン番号</summary>
        public const int NoSpecialPin = 0;

        /// <summary>倒れたピンの数字から得点を求める（1本＝数字／複数本＝本数）</summary>
        public static int CalculatePoints(IReadOnlyList<int> fallenNumbers)
        {
            if (fallenNumbers.Count == 1) return fallenNumbers[0];

            return fallenNumbers.Count;
        }

        /// <summary>1投の結果をチームの得点に反映する（個人戦は1人チーム）</summary>
        public static ThrowResult ApplyThrow(TeamScore team, IReadOnlyList<int> fallenNumbers)
        {
            return ApplyThrow(team, fallenNumbers, MolkkyRuleMode.Normal, NoSpecialPin);
        }

        /// <summary>
        /// ルールモードと、その手番の特殊ピン（金 / 爆弾）を考慮して1投の結果を反映する
        /// </summary>
        public static ThrowResult ApplyThrow(TeamScore team, IReadOnlyList<int> fallenNumbers,
            MolkkyRuleMode mode, int specialPinNumber)
        {
            bool hitSpecial = specialPinNumber != NoSpecialPin && Contains(fallenNumbers, specialPinNumber);

            // 爆弾は「何本倒しても0点」。ミスと同じく連続回数に数え、3回続けば失格にする
            if (mode == MolkkyRuleMode.Bomb && hitSpecial) return RegisterMiss(team, ThrowOutcome.Bombed);

            bool isGolden = mode == MolkkyRuleMode.Golden && hitSpecial;
            int points = CalculatePoints(fallenNumbers) * (isGolden ? GoldenMultiplier : 1);
            int single = fallenNumbers.Count == 1 ? fallenNumbers[0] : 0;

            if (points == 0) return RegisterMiss(team, ThrowOutcome.Miss);

            // ミスの連続回数で失格を判定するので、1本でも倒したら数え直す
            team.MissCount = 0;
            team.Score += points;

            if (team.Score == TargetScore)
            {
                return new ThrowResult(ThrowOutcome.Win, points, fallenNumbers.Count, single, isGolden);
            }

            if (team.Score > TargetScore)
            {
                team.Score = OverResetScore;
                return new ThrowResult(ThrowOutcome.OverTo25, points, fallenNumbers.Count, single, isGolden);
            }

            return new ThrowResult(ThrowOutcome.Scored, points, fallenNumbers.Count, single, isGolden);
        }

        private static ThrowResult RegisterMiss(TeamScore team, ThrowOutcome missOutcome)
        {
            team.MissCount++;
            if (team.MissCount >= MaxConsecutiveMisses)
            {
                team.IsDisqualified = true;
                return new ThrowResult(ThrowOutcome.Disqualified, 0, 0, 0);
            }

            return new ThrowResult(missOutcome, 0, 0, 0);
        }

        private static bool Contains(IReadOnlyList<int> numbers, int value)
        {
            for (int i = 0; i < numbers.Count; i++)
            {
                if (numbers[i] == value) return true;
            }

            return false;
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
