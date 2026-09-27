using System;
using System.Numerics;
using NUnit.Framework;

namespace MiniGame.Golf.Tests
{
    public class NpcShotPlannerTests
    {
        // クラブの値はテストが調整値の変更で壊れないよう、テスト側で固定する
        private static readonly ClubConfig Iron = new ClubConfig(12f, 25f, 0.8f);
        private static readonly ClubConfig Putter = new ClubConfig(5f, 0f, 0.1f, true);

        // ブレなしで判断だけを変えた難易度
        private static readonly NpcDifficultyConfig Simple = new NpcDifficultyConfig(0f, 0f, 0f, false, false, false);
        private static readonly NpcDifficultyConfig Smart = new NpcDifficultyConfig(0f, 0f, 0f, true, true, true);

        /// <summary>中心 center・半径 radius の池。それ以外はフェアウェイ</summary>
        private sealed class Pond : IGroundMap
        {
            private readonly Vector2 _center;
            private readonly float _radius;

            public Pond(Vector2 center, float radius)
            {
                _center = center;
                _radius = radius;
            }

            public GroundType GetGround(Vector2 position)
            {
                return Vector2.Distance(position, _center) <= _radius ? GroundType.Water : GroundType.Fairway;
            }
        }

        private sealed class UniformSlope : ISlopeMap
        {
            private readonly Vector2 _slope;

            public UniformSlope(Vector2 slope)
            {
                _slope = slope;
            }

            public Vector2 GetSlope(Vector2 position) => _slope;
        }

        private static BallSimulator CreateBall(Vector2 cup, IGroundMap ground = null, ISlopeMap slope = null,
            Wind? wind = null)
        {
            var ball = new BallSimulator(new BallPhysicsConfig(), null, ground, slope);
            ball.SetCup(cup);
            ball.SetWind(wind ?? Wind.Calm);
            ball.Place(Vector2.Zero);
            return ball;
        }

        private static ShotOutcome AimAndShoot(BallSimulator ball, NpcDifficultyConfig difficulty, ClubConfig club,
            Vector2 cup)
        {
            ShotRequest shot = new NpcShotPlanner(ball, difficulty, new Random(0)).Aim(club, cup);
            return ball.TrySimulate(shot, true, true);
        }

        [Test]
        public void 届く距離ならカップの近くに止まるパワーを選ぶ()
        {
            var cup = new Vector2(0f, 10f);
            ShotOutcome outcome = AimAndShoot(CreateBall(cup), Simple, Iron, cup);

            Assert.Less(Vector2.Distance(outcome.Position, cup), 1f);
        }

        [Test]
        public void 平らなグリーンの短いパットは入る()
        {
            var cup = new Vector2(0.5f, 2.5f);
            ShotOutcome outcome = AimAndShoot(CreateBall(cup), Simple, Putter, cup);

            Assert.IsTrue(outcome.IsInCup);
        }

        [Test]
        public void 届かないときはフルパワーで打つ()
        {
            var cup = new Vector2(0f, 100f);
            ShotRequest shot = new NpcShotPlanner(CreateBall(cup), Simple, new Random(0)).Aim(Iron, cup);

            Assert.AreEqual(1f, shot.Power, 0.01f);
        }

        // カップの手前、アイアンが最初に着地するあたりの池
        private static readonly Pond PondBeforeCup = new Pond(new Vector2(0f, 7f), 1.5f);

        [Test]
        public void 狙いの先に池があっても池には打ち込まない([Values(false, true)] bool smart)
        {
            var cup = new Vector2(0f, 10f);
            BallSimulator ball = CreateBall(cup, PondBeforeCup);

            ShotOutcome outcome = AimAndShoot(ball, smart ? Smart : Simple, Iron, cup);

            Assert.IsFalse(outcome.IsInHazard);
        }

        [Test]
        public void つよいは池を左右に避け手前に刻むより前へ進む()
        {
            var cup = new Vector2(0f, 10f);

            ShotOutcome simple = AimAndShoot(CreateBall(cup, PondBeforeCup), Simple, Iron, cup);
            ShotOutcome smart = AimAndShoot(CreateBall(cup, PondBeforeCup), Smart, Iron, cup);

            Assert.Greater(smart.Position.Y, simple.Position.Y);
        }

        [Test]
        public void 風を読むと横風でもカップの近くに止まる()
        {
            var cup = new Vector2(0f, 12f);
            Wind crossWind = Wind.FromDegrees(0f, 5f);

            ShotOutcome simple = AimAndShoot(CreateBall(cup, wind: crossWind), Simple, Iron, cup);
            ShotOutcome smart = AimAndShoot(CreateBall(cup, wind: crossWind), Smart, Iron, cup);

            Assert.Less(Vector2.Distance(smart.Position, cup), Vector2.Distance(simple.Position, cup));
            Assert.Less(Vector2.Distance(smart.Position, cup), 1f);
        }

        [Test]
        public void 傾斜を読むと曲がるパットでもカップの近くに止まる()
        {
            var cup = new Vector2(0f, 3f);
            var slope = new UniformSlope(new Vector2(1f, 0f));

            ShotOutcome simple = AimAndShoot(CreateBall(cup, slope: slope), Simple, Putter, cup);
            ShotOutcome smart = AimAndShoot(CreateBall(cup, slope: slope), Smart, Putter, cup);

            Assert.Less(Vector2.Distance(smart.Position, cup), Vector2.Distance(simple.Position, cup));
        }

        [Test]
        public void ブレが大きい難易度ほど方向が散らばる()
        {
            var cup = new Vector2(0f, 10f);
            var weak = new NpcDifficultyConfig(10f, 0f, 0f, false, false, false);
            var strong = new NpcDifficultyConfig(1.5f, 0f, 0f, false, false, false);

            Assert.Greater(MeanAngleError(weak, cup), MeanAngleError(strong, cup));
        }

        private static float MeanAngleError(NpcDifficultyConfig difficulty, Vector2 cup)
        {
            const int Trials = 50;
            var planner = new NpcShotPlanner(CreateBall(cup), difficulty, new Random(1));
            float total = 0f;
            for (int i = 0; i < Trials; i++)
            {
                Vector2 direction = planner.Plan(Iron, cup).Direction;
                total += MathF.Abs(MathF.Atan2(direction.X, direction.Y));
            }

            return total / Trials;
        }
    }
}
