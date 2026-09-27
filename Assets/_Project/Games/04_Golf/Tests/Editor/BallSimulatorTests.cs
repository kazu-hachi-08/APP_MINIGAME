using System.Numerics;
using NUnit.Framework;

namespace MiniGame.Golf.Tests
{
    public class BallSimulatorTests
    {
        private static readonly Vector2 Forward = new Vector2(0f, 1f);

        // クラブの値はテストが調整値の変更で壊れないよう、テスト側で固定する
        private static readonly ClubConfig Iron = new ClubConfig(12f, 25f, 0.8f);
        private static readonly ClubConfig Driver = new ClubConfig(16f, 15f, 1f);
        private static readonly ClubConfig Wedge = new ClubConfig(7f, 50f, 0.5f);
        private static readonly ClubConfig Putter = new ClubConfig(5f, 0f, 0.1f, true);

        private static void Hit(BallSimulator simulator, Vector2 direction, float power, ClubConfig club = null,
            float impactOffset = 0f)
        {
            simulator.Launch(new ShotRequest(direction, club ?? Iron, power, impactOffset));
        }

        private static void Putt(BallSimulator simulator, Vector2 direction, float power)
        {
            Hit(simulator, direction, power, Putter);
        }

        private static BallSimulator Shoot(float power, ClubConfig club = null, float impactOffset = 0f)
        {
            var simulator = new BallSimulator(new BallPhysicsConfig());
            simulator.Place(Vector2.Zero);
            Hit(simulator, Forward, power, club, impactOffset);
            return simulator;
        }

        private static float MaxHeight(BallSimulator simulator)
        {
            float maxHeight = 0f;
            while (simulator.IsMoving)
            {
                simulator.Advance();
                if (simulator.Height > maxHeight) maxHeight = simulator.Height;
            }

            return maxHeight;
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
            Assert.Greater(MaxHeight(Shoot(1f)), 1f);
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
            Hit(simulator, new Vector2(1f, 0f), 1f);

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
            Hit(simulator, Forward, 1f);

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
                Putt(simulator, Forward, 1f);
            }
            else
            {
                Hit(simulator, Forward, 1f);
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
            Putt(simulator, Forward, 0.5f);

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
            Putt(simulator, cup, power);
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
            Hit(simulator, Forward, 1f);
            simulator.AdvanceToRest();

            Assert.IsTrue(simulator.IsInCup);
        }

        // --- クラブ・インパクト・着地予測（Phase 3） ---

        private static Vector2 FirstLanding(BallSimulator simulator)
        {
            do
            {
                simulator.Advance();
            } while (simulator.IsMoving && simulator.Height > 0f);

            return simulator.Position;
        }

        [Test]
        public void クラブで飛距離が変わる()
        {
            float driver = FirstLanding(Shoot(1f, Driver)).Length();
            float iron = FirstLanding(Shoot(1f, Iron)).Length();
            float wedge = FirstLanding(Shoot(1f, Wedge)).Length();

            Assert.Greater(driver, iron);
            Assert.Greater(iron, wedge);
        }

        [Test]
        public void ウェッジはドライバーより高く上がる()
        {
            Assert.Greater(MaxHeight(Shoot(1f, Wedge)), MaxHeight(Shoot(1f, Driver)));
        }

        [Test]
        public void インパクトがゾーンの中心ならまっすぐ飛ぶ()
        {
            BallSimulator simulator = Shoot(1f);
            simulator.AdvanceToRest();

            Assert.AreEqual(0f, simulator.Position.X, 0.0001f);
        }

        [Test]
        public void インパクトが右にずれるとスライス左にずれるとフックする()
        {
            BallSimulator slice = Shoot(1f, Iron, 0.5f);
            BallSimulator hook = Shoot(1f, Iron, -0.5f);
            slice.AdvanceToRest();
            hook.AdvanceToRest();

            Assert.Greater(slice.Position.X, 0.1f);
            Assert.Less(hook.Position.X, -0.1f);
        }

        [Test]
        public void ずれが大きいほど大きく曲がる()
        {
            BallSimulator small = Shoot(1f, Iron, 0.3f);
            BallSimulator large = Shoot(1f, Iron, 0.9f);
            small.AdvanceToRest();
            large.AdvanceToRest();

            Assert.Greater(large.Position.X, small.Position.X);
        }

        [Test]
        public void ゾーンの外はミスショットで大きく曲がり飛ばない()
        {
            Vector2 edge = FirstLanding(Shoot(1f, Iron, 1f));
            Vector2 miss = FirstLanding(Shoot(1f, Iron, 1.5f));

            Assert.Greater(miss.X, edge.X);
            Assert.Less(miss.Length(), edge.Length());
        }

        [Test]
        public void パットもインパクトがずれると曲がる()
        {
            BallSimulator simulator = Shoot(1f, Putter, 1f);
            simulator.AdvanceToRest();

            Assert.Greater(simulator.Position.X, 0f);
        }

        [Test]
        public void 着地予測はフルパワーでまっすぐ打った最初の着地点になる()
        {
            var simulator = new BallSimulator(new BallPhysicsConfig());
            simulator.Place(new Vector2(1f, 2f));

            Vector2 predicted = simulator.PredictFullPower(Driver, Forward);

            var actual = new BallSimulator(new BallPhysicsConfig());
            actual.Place(new Vector2(1f, 2f));
            Hit(actual, Forward, 1f, Driver);
            Assert.AreEqual(FirstLanding(actual), predicted);
            // 予測しても元のボールは動かない
            Assert.IsFalse(simulator.IsMoving);
            Assert.AreEqual(new Vector2(1f, 2f), simulator.Position);
        }

        [Test]
        public void パターの予測は止まる位置になる()
        {
            var simulator = new BallSimulator(new BallPhysicsConfig());
            simulator.Place(Vector2.Zero);

            Vector2 predicted = simulator.PredictFullPower(Putter, Forward);

            BallSimulator actual = Shoot(1f, Putter);
            actual.AdvanceToRest();
            Assert.AreEqual(actual.Position, predicted);
        }
    }
}
