using System.Numerics;
using NUnit.Framework;

namespace MiniGame.Golf.Tests
{
    public class BallSimulatorTests
    {
        private static readonly Vector2 Forward = new Vector2(0f, 1f);

        private static BallSimulator Shoot(float power, BallPhysicsConfig config = null)
        {
            var simulator = new BallSimulator(config ?? new BallPhysicsConfig());
            simulator.Place(Vector2.Zero);
            simulator.Launch(Forward, power);
            return simulator;
        }

        [Test]
        public void 同じ入力なら同じ位置に止まる()
        {
            BallSimulator first = Shoot(0.8f);
            BallSimulator second = Shoot(0.8f);

            first.AdvanceToRest();
            second.AdvanceToRest();

            Assert.AreEqual(first.Position, second.Position);
            Assert.AreEqual(first.ElapsedTime, second.ElapsedTime);
        }

        [Test]
        public void 打ったボールは高さを持って飛ぶ()
        {
            BallSimulator simulator = Shoot(1f);
            float maxHeight = 0f;

            while (simulator.IsMoving)
            {
                simulator.Advance();
                if (simulator.Height > maxHeight) maxHeight = simulator.Height;
            }

            Assert.Greater(maxHeight, 1f);
        }

        [Test]
        public void 着地した後に転がってから止まる()
        {
            BallSimulator simulator = Shoot(1f);
            float firstLandingDistance = -1f;

            while (simulator.IsMoving)
            {
                simulator.Advance();
                if (firstLandingDistance < 0f && simulator.Height <= 0f)
                {
                    firstLandingDistance = simulator.Position.Length();
                }
            }

            Assert.Greater(firstLandingDistance, 0f);
            Assert.Greater(simulator.Position.Length(), firstLandingDistance);
            Assert.AreEqual(0f, simulator.Height);
            Assert.IsFalse(simulator.IsMoving);
        }

        [Test]
        public void パワーが大きいほど遠くに止まる()
        {
            BallSimulator weak = Shoot(0.3f);
            BallSimulator strong = Shoot(1f);

            weak.AdvanceToRest();
            strong.AdvanceToRest();

            Assert.Greater(strong.Position.Length(), weak.Position.Length());
        }

        [Test]
        public void 打った方向へ飛ぶ()
        {
            var simulator = new BallSimulator(new BallPhysicsConfig());
            simulator.Place(new Vector2(3f, 5f));
            simulator.Launch(new Vector2(1f, 0f), 1f);

            simulator.AdvanceToRest();

            Assert.Greater(simulator.Position.X, 3f);
            Assert.AreEqual(5f, simulator.Position.Y, 0.0001f);
        }

        [Test]
        public void 減速しない設定でも最大時間で止まる()
        {
            var config = new BallPhysicsConfig { RollDeceleration = 0f, MaxSimulationTime = 3f };
            BallSimulator simulator = Shoot(1f, config);

            simulator.AdvanceToRest();

            Assert.IsFalse(simulator.IsMoving);
            Assert.LessOrEqual(simulator.ElapsedTime, config.MaxSimulationTime + config.SimulationStep);
        }
    }
}
