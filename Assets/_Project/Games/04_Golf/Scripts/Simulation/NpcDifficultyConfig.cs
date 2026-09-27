using System;

namespace MiniGame.Golf
{
    /// <summary>
    /// NPC 1段階ぶんの強さ（§10.3）。
    /// BallPhysicsConfig と同じく UnityEngine に依存させず、ScriptableObject（GolfNpcDifficulty）の中に表示する。
    /// </summary>
    [Serializable]
    public class NpcDifficultyConfig
    {
        /// <summary>方向のブレ（最大 ± 度）</summary>
        public float DirectionErrorDegrees = 4f;

        /// <summary>パワーのブレ（最大 ± 割合）。狙ったパワーにこの割合を掛けてずらす</summary>
        public float PowerErrorRate = 0.08f;

        /// <summary>インパクトのずれ（最大 ±、ShotRequest.ImpactOffset と同じ単位）。1 を超えるとミスショットが出る</summary>
        public float ImpactError = 0.7f;

        /// <summary>③ 狙いの先が池・OBなら左右にずらして避ける</summary>
        public bool AvoidHazards;

        /// <summary>④ 風を読んで狙いを補正する</summary>
        public bool ReadWind;

        /// <summary>⑤ パットで傾斜を読んで狙いを補正する</summary>
        public bool ReadSlope;

        public NpcDifficultyConfig()
        {
        }

        public NpcDifficultyConfig(float directionErrorDegrees, float powerErrorRate, float impactError,
            bool avoidHazards, bool readWind, bool readSlope)
        {
            DirectionErrorDegrees = directionErrorDegrees;
            PowerErrorRate = powerErrorRate;
            ImpactError = impactError;
            AvoidHazards = avoidHazards;
            ReadWind = readWind;
            ReadSlope = readSlope;
        }
    }
}
