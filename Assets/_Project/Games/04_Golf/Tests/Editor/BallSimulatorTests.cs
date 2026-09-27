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
            var config = new BallPhysicsConfig { MaxSimulationTime = 3f };
            var terrain = new TerrainPhysicsConfig { Fairway = new TerrainPhysics(0f, 0.35f, 0.7f) };
            var simulator = new BallSimulator(config, terrain);
            simulator.Place(Vector2.Zero);
            simulator.Launch(Forward, 1f);

            simulator.AdvanceToRest();

            Assert.IsFalse(simulator.IsMoving);
            Assert.LessOrEqual(simulator.ElapsedTime, config.MaxSimulationTime + config.SimulationStep);
        }

        // --- 地面の種類（Phase 2） ---

        private sealed class UniformGround : IGroundMap
        {
            private readonly GroundType _ground;

            public UniformGround(GroundType ground) => _ground = ground;

            public GroundType GetGround(Vector2 position) => _ground;
        }

        private static float RestDistanceOn(GroundType ground, bool putt = false)
        {
            var simulator = new BallSimulator(new BallPhysicsConfig(), new TerrainPhysicsConfig(), new UniformGround(ground));
            simulator.Place(Vector2.Zero);
            if (putt)
            {
                simulator.LaunchPutt(Forward, 1f);
            }
            else
            {
                simulator.Launch(Forward, 1f);
            }

            simulator.AdvanceToRest();
            return simulator.Position.Length();
        }

        [Test]
        public void ラフはフェアウェイより手前で止まる()
        {
            Assert.Less(RestDistanceOn(GroundType.Rough), RestDistanceOn(GroundType.Fairway));
        }

        [Test]
        public void バンカーはラフより手前で止まる()
        {
            Assert.Less(RestDistanceOn(GroundType.Bunker), RestDistanceOn(GroundType.Rough));
        }

        [Test]
        public void グリーンはフェアウェイよりよく転がる()
        {
            // 打ち上げると跳ね返りの小ささで差が出るので、転がりだけを比べる
            Assert.Greater(RestDistanceOn(GroundType.Green, true), RestDistanceOn(GroundType.Fairway, true));
        }

        [Test]
        public void パットは高さを持たずに転がる()
        {
            var simulator = new BallSimulator(new BallPhysicsConfig());
            simulator.Place(Vector2.Zero);
            simulator.LaunchPutt(Forward, 0.5f);

            while (simulator.IsMoving)
            {
                simulator.Advance();
                Assert.AreEqual(0f, simulator.Height);
            }

            Assert.Greater(simulator.Position.Y, 0f);
        }

        // --- カップイン（Phase 2） ---

        private static BallSimulator PuttToward(Vector2 cup, float power)
        {
            var simulator = new BallSimulator(new BallPhysicsConfig(), new TerrainPhysicsConfig(), new UniformGround(GroundType.Green));
            simulator.SetCup(cup);
            simulator.Place(Vector2.Zero);
            simulator.LaunchPutt(cup, power);
            simulator.AdvanceToRest();
            return simulator;
        }

        [Test]
        public void ちょうどよい強さで転がるとカップインする()
        {
            // 強さ0.5（初速2.5）ならグリーンで約1.25ユニット転がるので、1ユニット先のカップをゆっくり通る
            BallSimulator simulator = PuttToward(new Vector2(0f, 1f), 0.5f);

            Assert.IsTrue(simulator.IsInCup);
            Assert.AreEqual(new Vector2(0f, 1f), simulator.Position);
        }

        [Test]
        public void 速すぎるとカップを通り過ぎる()
        {
            BallSimulator simulator = PuttToward(new Vector2(0f, 0.5f), 1f);

            Assert.IsFalse(simulator.IsInCup);
            Assert.Greater(simulator.Position.Y, 0.5f);
        }

        [Test]
        public void 届かなければカップインしない()
        {
            BallSimulator simulator = PuttToward(new Vector2(0f, 4f), 0.3f);

            Assert.IsFalse(simulator.IsInCup);
        }

        [Test]
        public void 空中から直接カップに落ちるとカップインする()
        {
            // 同じ入力なら同じ場所に落ちるので、1回目の最初の着地点にカップを置く
            BallSimulator probe = Shoot(1f);
            do
            {
                probe.Advance();
            } while (probe.Height > 0f);

            Vector2 landing = probe.Position;

            var simulator = new BallSimulator(new BallPhysicsConfig());
            simulator.SetCup(landing);
            simulator.Place(Vector2.Zero);
            simulator.Launch(Forward, 1f);
            simulator.AdvanceToRest();

            Assert.IsTrue(simulator.IsInCup);
        }
    }
}
