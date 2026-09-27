using System.Numerics;

namespace MiniGame.Golf
{
    /// <summary>
    /// 1打の入力（§7.6）。入力処理とゲームロジックの境目で、NPC・オンラインの相手も同じ形で打つ。
    /// </summary>
    public readonly struct ShotRequest
    {
        public ShotRequest(Vector2 direction, ClubConfig club, float power, float impactOffset)
        {
            Direction = direction;
            Club = club;
            Power = power;
            ImpactOffset = impactOffset;
        }

        public Vector2 Direction { get; }
        public ClubConfig Club { get; }

        /// <summary>0〜1</summary>
        public float Power { get; }

        /// <summary>インパクトゾーンの中心からのずれ。±1 がゾーンの端、+ は右（スライス）、- は左（フック）</summary>
        public float ImpactOffset { get; }
    }
}
