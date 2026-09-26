using System.Collections.Generic;

namespace MiniGame.Molkky
{
    /// <summary>
    /// モルックの得点ルール（§6）。
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

        /// <summary>1投の結果をプレイヤーに反映する</summary>
        public static ThrowResult ApplyThrow(PlayerSlot player, IReadOnlyList<int> fallenNumbers)
        {
            int points = CalculatePoints(fallenNumbers);
            int single = fallenNumbers.Count == 1 ? fallenNumbers[0] : 0;

            if (points == 0)
            {
                player.MissCount++;
                if (player.MissCount >= MaxConsecutiveMisses)
                {
                    player.IsDisqualified = true;
                    return new ThrowResult(ThrowOutcome.Disqualified, 0, 0, 0);
                }

                return new ThrowResult(ThrowOutcome.Miss, 0, 0, 0);
            }

            player.MissCount = 0;
            player.Score += points;

            if (player.Score == TargetScore)
            {
                return new ThrowResult(ThrowOutcome.Win, points, fallenNumbers.Count, single);
            }

            if (player.Score > TargetScore)
            {
                player.Score = OverResetScore;
                return new ThrowResult(ThrowOutcome.OverTo25, points, fallenNumbers.Count, single);
            }

            return new ThrowResult(ThrowOutcome.Scored, points, fallenNumbers.Count, single);
        }

        /// <summary>次に投げるプレイヤーの番号。失格者は飛ばす。投げられる人がいなければ -1</summary>
        public static int NextPlayerIndex(IReadOnlyList<PlayerSlot> players, int currentIndex)
        {
            for (int step = 1; step <= players.Count; step++)
            {
                int index = (currentIndex + step) % players.Count;
                if (!players[index].IsDisqualified) return index;
            }

            return -1;
        }

        /// <summary>失格していないプレイヤーが1人だけならその人（§6.7）。それ以外は null</summary>
        public static PlayerSlot FindSoleSurvivor(IReadOnlyList<PlayerSlot> players)
        {
            PlayerSlot survivor = null;
            foreach (PlayerSlot player in players)
            {
                if (player.IsDisqualified) continue;
                if (survivor != null) return null;

                survivor = player;
            }

            return survivor;
        }
    }
}
