using System;

namespace MiniGame.PenguinWars.Battle
{
    /// <summary>取った★。セーブでは過去の分と OR して残すので、どの★かを区別できるフラグにする</summary>
    [Flags]
    public enum StarFlags
    {
        None = 0,
        /// <summary>★1: クリア</summary>
        Clear = 1 << 0,
        /// <summary>★2: 自城HPを決めた割合以上残してクリア</summary>
        Safe = 1 << 1,
        /// <summary>★3: 目標タイム以内にクリア</summary>
        Fast = 1 << 2,
        All = Clear | Safe | Fast,
    }

    /// <summary>★の判定（INDEX「決めた前提」）。ちょうど境界の値は取れた側にする（「50%以上」「目標タイム以内」）</summary>
    public static class StarRule
    {
        public const int MaxStars = 3;

        public static StarFlags Evaluate(StageDefinition stage, StageResult result)
        {
            if (!result.Cleared) return StarFlags.None;

            StarFlags stars = StarFlags.Clear;
            if (result.PlayerCastleHpRatio >= stage.SafeHpRatio) stars |= StarFlags.Safe;
            if (result.ElapsedSeconds <= stage.TargetSeconds) stars |= StarFlags.Fast;
            return stars;
        }

        public static int Count(StarFlags stars)
        {
            int count = 0;
            if ((stars & StarFlags.Clear) != 0) count++;
            if ((stars & StarFlags.Safe) != 0) count++;
            if ((stars & StarFlags.Fast) != 0) count++;
            return count;
        }

        public static bool Has(StarFlags stars, StarFlags star) => (stars & star) == star;
    }
}
