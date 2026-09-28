using System;

namespace MiniGame.Golf
{
    /// <summary>
    /// 1本のクラブの飛び方。
    /// BallPhysicsConfig と同じく UnityEngine に依存させず、ScriptableObject（GolfClubData）の中に表示する。
    /// </summary>
    [Serializable]
    public class ClubConfig
    {
        /// <summary>パワー100%のときの初速</summary>
        public float MaxLaunchSpeed = 10f;

        /// <summary>打ち出し角（度）。弾道の高さを決める。パターでは使わない</summary>
        public float LaunchAngleDegrees = 25f;

        /// <summary>曲がりやすさ。インパクトのずれにこれを掛けて横向きの加速度にする</summary>
        public float CurveFactor = 1f;

        /// <summary>高さを持たせず転がして打つ</summary>
        public bool IsPutter;

        public ClubConfig()
        {
        }

        public ClubConfig(float maxLaunchSpeed, float launchAngleDegrees, float curveFactor, bool isPutter = false)
        {
            MaxLaunchSpeed = maxLaunchSpeed;
            LaunchAngleDegrees = launchAngleDegrees;
            CurveFactor = curveFactor;
            IsPutter = isPutter;
        }
    }
}
