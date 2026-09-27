using System.Numerics;

namespace MiniGame.Golf
{
    /// <summary>試し打ち（BallSimulator.TrySimulate）で止まった結果。NPC が狙いを決めるのに使う</summary>
    public readonly struct ShotOutcome
    {
        public ShotOutcome(Vector2 position, bool isInHazard, bool isInCup)
        {
            Position = position;
            IsInHazard = isInHazard;
            IsInCup = isInCup;
        }

        /// <summary>止まった位置。池・OBのときは入った位置</summary>
        public Vector2 Position { get; }

        public bool IsInHazard { get; }
        public bool IsInCup { get; }
    }
}
