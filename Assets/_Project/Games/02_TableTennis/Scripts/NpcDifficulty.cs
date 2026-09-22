using UnityEngine;

namespace MiniGame.TableTennis
{
    /// <summary>
    /// 難易度ごとのNPC強さ。docs/TABLE_TENNIS_SPEC.md 17.3 の表と一致させてある。
    /// </summary>
    public struct NpcDifficultyStats
    {
        /// <summary>ラケットが届く範囲にいるとき、返球可能判定に成功する確率</summary>
        public float SuccessRate;

        /// <summary>NPCの追従速度倍率。低いと「そもそもボールに追いつけない」が増える</summary>
        public float MoveSpeedMultiplier;

        /// <summary>打点で当てられる左右のズレ（reachX）の倍率</summary>
        public float ReachMultiplier;

        /// <summary>反応の遅れ（reactionDelay）の倍率。小さいほど反応が速い</summary>
        public float ReactionMultiplier;

        /// <summary>打球速度倍率（飛行時間を短くして表現する）</summary>
        public float SpeedMultiplier;

        /// <summary>回転量倍率</summary>
        public float SpinMultiplier;
    }

    /// <summary>5段階の難易度テーブル。値を変えるだけで強さを調整できる</summary>
    public static class NpcDifficultyTable
    {
        public const int MinLevel = 1;
        public const int MaxLevel = 5;

        private static readonly NpcDifficultyStats[] Stats =
        {
            new NpcDifficultyStats { SuccessRate = 0.55f, MoveSpeedMultiplier = 0.75f, ReachMultiplier = 0.75f, ReactionMultiplier = 1.4f, SpeedMultiplier = 0.8f, SpinMultiplier = 0.7f },
            new NpcDifficultyStats { SuccessRate = 0.72f, MoveSpeedMultiplier = 0.9f, ReachMultiplier = 0.9f, ReactionMultiplier = 1.15f, SpeedMultiplier = 0.9f, SpinMultiplier = 0.85f },
            new NpcDifficultyStats { SuccessRate = 0.85f, MoveSpeedMultiplier = 1.0f, ReachMultiplier = 1.0f, ReactionMultiplier = 1.0f, SpeedMultiplier = 1.0f, SpinMultiplier = 1.0f },
            new NpcDifficultyStats { SuccessRate = 0.93f, MoveSpeedMultiplier = 1.15f, ReachMultiplier = 1.15f, ReactionMultiplier = 0.8f, SpeedMultiplier = 1.15f, SpinMultiplier = 1.15f },
            new NpcDifficultyStats { SuccessRate = 0.99f, MoveSpeedMultiplier = 1.35f, ReachMultiplier = 1.3f, ReactionMultiplier = 0.55f, SpeedMultiplier = 1.3f, SpinMultiplier = 1.3f },
        };

        public static NpcDifficultyStats Get(int level)
        {
            int index = Mathf.Clamp(level, MinLevel, MaxLevel) - 1;
            return Stats[index];
        }
    }
}
