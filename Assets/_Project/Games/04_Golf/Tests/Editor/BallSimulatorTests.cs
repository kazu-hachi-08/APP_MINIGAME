using System;
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

        // --- 池・OB・風・ライ（Phase 4） ---

        /// <summary>奥行き boundaryY から先が指定の地面、手前はフェアウェイ</summary>
        private sealed class BeyondGround : IGroundMap
        {
            private readonly float _boundaryY;
            private readonly GroundType _beyond;

            public BeyondGround(float boundaryY, GroundType beyond)
            {
                _boundaryY = boundaryY;
                _beyond = beyond;
            }

            public GroundType GetGround(Vector2 position) => position.Y >= _boundaryY ? _beyond : GroundType.Fairway;
        }

        private static BallSimulator CreateOn(IGroundMap ground, Vector2 start)
        {
            var simulator = new BallSimulator(new BallPhysicsConfig(), new TerrainPhysicsConfig(), ground);
            simulator.Place(start);
            return simulator;
        }

        private static BallSimulator CreateWithWind(Wind wind)
        {
            var simulator = new BallSimulator(new BallPhysicsConfig());
            simulator.SetWind(wind);
            simulator.Place(Vector2.Zero);
            return simulator;
        }

        [Test]
        public void 池に落ちると止まり池に入る直前の地点から打ち直す()
        {
            // 手前5ユニットから先が池。アイアンのフルショットは池の上を飛んで池に落ちる
            BallSimulator simulator = CreateOn(new BeyondGround(5f, GroundType.Water), Vector2.Zero);
            Hit(simulator, Forward, 1f);

            simulator.AdvanceToRest();

            Assert.IsTrue(simulator.IsInHazard);
            Assert.AreEqual(GroundType.Water, simulator.Ground);
            Assert.Greater(simulator.DropPosition.Y, 4f);
            Assert.Less(simulator.DropPosition.Y, 5f);
        }

        [Test]
        public void 転がって池に入っても止まる()
        {
            BallSimulator simulator = CreateOn(new BeyondGround(1f, GroundType.Water), Vector2.Zero);
            Putt(simulator, Forward, 1f);

            simulator.AdvanceToRest();

            Assert.IsTrue(simulator.IsInHazard);
            // 池に入った次の刻みで止まる（そのまま転がり続けない）
            Assert.Less(simulator.Position.Y, 1.2f);
            Assert.Less(simulator.DropPosition.Y, 1f);
        }

        [Test]
        public void OBになると打つ前の場所から打ち直す()
        {
            var start = new Vector2(1f, 2f);
            BallSimulator simulator = CreateOn(new BeyondGround(7f, GroundType.OutOfBounds), start);
            Hit(simulator, Forward, 1f);

            simulator.AdvanceToRest();

            Assert.IsTrue(simulator.IsInHazard);
            Assert.AreEqual(GroundType.OutOfBounds, simulator.Ground);
            Assert.AreEqual(start, simulator.DropPosition);
        }

        [Test]
        public void 置き直すと池OBの状態は消える()
        {
            BallSimulator simulator = CreateOn(new BeyondGround(5f, GroundType.Water), Vector2.Zero);
            Hit(simulator, Forward, 1f);
            simulator.AdvanceToRest();

            simulator.Place(simulator.DropPosition);

            Assert.IsFalse(simulator.IsInHazard);
            Assert.AreEqual(GroundType.Fairway, simulator.Ground);
        }

        [Test]
        public void 横風で風下へ流される()
        {
            BallSimulator simulator = CreateWithWind(new Wind(new Vector2(1f, 0f), 5f));
            Hit(simulator, Forward, 1f);

            Assert.Greater(FirstLanding(simulator).X, 0.5f);
        }

        [Test]
        public void 向かい風では飛ばない()
        {
            float calm = FirstLanding(Shoot(1f)).Y;

            BallSimulator headwind = CreateWithWind(new Wind(new Vector2(0f, -1f), 5f));
            Hit(headwind, Forward, 1f);

            Assert.Less(FirstLanding(headwind).Y, calm);
        }

        [Test]
        public void パットは風の影響を受けない()
        {
            BallSimulator simulator = CreateWithWind(new Wind(new Vector2(1f, 0f), 5f));
            Putt(simulator, Forward, 1f);

            simulator.AdvanceToRest();

            Assert.AreEqual(0f, simulator.Position.X, 0.0001f);
        }

        [Test]
        public void 着地予測に風は含めない()
        {
            BallSimulator windy = CreateWithWind(new Wind(new Vector2(1f, 0f), 5f));
            var calm = new BallSimulator(new BallPhysicsConfig());
            calm.Place(Vector2.Zero);

            Assert.AreEqual(calm.PredictFullPower(Driver, Forward), windy.PredictFullPower(Driver, Forward));
        }

        private static float CarryFrom(GroundType lie)
        {
            BallSimulator simulator = CreateOn(new UniformGround(lie), Vector2.Zero);
            Hit(simulator, Forward, 1f);
            return FirstLanding(simulator).Length();
        }

        [Test]
        public void ラフから打つとフェアウェイより飛ばない()
        {
            Assert.Less(CarryFrom(GroundType.Rough), CarryFrom(GroundType.Fairway));
        }

        [Test]
        public void バンカーから打つとラフより飛ばない()
        {
            Assert.Less(CarryFrom(GroundType.Bunker), CarryFrom(GroundType.Rough));
        }

        [Test]
        public void ライの飛距離の割合がおおよそそのまま飛距離になる()
        {
            var terrain = new TerrainPhysicsConfig();
            float rate = CarryFrom(GroundType.Bunker) / CarryFrom(GroundType.Fairway);

            Assert.AreEqual(terrain.Bunker.ShotDistanceRate, rate, 0.05f);
        }

        // --- グリーンの傾斜（Phase 5） ---

        /// <summary>どこでも同じ向き・強さの傾斜</summary>
        private sealed class UniformSlope : ISlopeMap
        {
            private readonly Vector2 _slope;

            public UniformSlope(Vector2 slope) => _slope = slope;

            public Vector2 GetSlope(Vector2 position) => _slope;
        }

        private static BallSimulator CreateOnSlopedGreen(Vector2 slope)
        {
            var simulator = new BallSimulator(new BallPhysicsConfig(), new TerrainPhysicsConfig(),
                new UniformGround(GroundType.Green), new UniformSlope(slope));
            simulator.Place(Vector2.Zero);
            return simulator;
        }

        private static Vector2 PuttOnSlope(Vector2 slope, float power = 0.6f)
        {
            BallSimulator simulator = CreateOnSlopedGreen(slope);
            Putt(simulator, Forward, power);
            simulator.AdvanceToRest();
            return simulator.Position;
        }

        [Test]
        public void パットは傾斜の下り方向へ曲がる()
        {
            Assert.Greater(PuttOnSlope(new Vector2(1f, 0f)).X, 0.1f);
            Assert.Less(PuttOnSlope(new Vector2(-1f, 0f)).X, -0.1f);
        }

        [Test]
        public void 傾斜が強いほど大きく曲がる()
        {
            Assert.Greater(PuttOnSlope(new Vector2(3f, 0f)).X, PuttOnSlope(new Vector2(1f, 0f)).X);
        }

        [Test]
        public void 下りはよく転がり上りは手前で止まる()
        {
            float flat = PuttOnSlope(Vector2.Zero).Y;

            Assert.Greater(PuttOnSlope(new Vector2(0f, 1f)).Y, flat);
            Assert.Less(PuttOnSlope(new Vector2(0f, -1f)).Y, flat);
        }

        [Test]
        public void 一番強い傾斜でも転がり続けずに止まる()
        {
            BallSimulator simulator = CreateOnSlopedGreen(new Vector2(0f, 3f));
            Putt(simulator, Forward, 1f);

            simulator.AdvanceToRest();

            Assert.Less(simulator.ElapsedTime, new BallPhysicsConfig().MaxSimulationTime);
        }

        /// <summary>右下りの強い傾斜で、3ユニット先のカップへ degrees（右回りが正）の向きにパットする</summary>
        private static bool HolesOutOnSideSlope(int degrees)
        {
            var simulator = new BallSimulator(new BallPhysicsConfig(), new TerrainPhysicsConfig(),
                new UniformGround(GroundType.Green), new UniformSlope(new Vector2(3f, 0f)));
            simulator.SetCup(new Vector2(0f, 3f));
            simulator.Place(Vector2.Zero);
            float radians = degrees * MathF.PI / 180f;
            Putt(simulator, new Vector2(MathF.Sin(radians), MathF.Cos(radians)), 0.8f);
            simulator.AdvanceToRest();
            return simulator.IsInCup;
        }

        [Test]
        public void カップへまっすぐ打つと傾斜で外れ読んで左へ打つと入る()
        {
            Assert.IsFalse(HolesOutOnSideSlope(0));

            bool holedOut = false;
            for (int degrees = -1; degrees >= -30 && !holedOut; degrees--)
            {
                holedOut = HolesOutOnSideSlope(degrees);
            }

            Assert.IsTrue(holedOut);
        }

        [Test]
        public void 打ち上げたショットは空中で傾斜の影響を受けない()
        {
            BallSimulator simulator = CreateOnSlopedGreen(new Vector2(3f, 0f));
            Hit(simulator, Forward, 1f);

            Assert.AreEqual(0f, FirstLanding(simulator).X, 0.0001f);
        }

        [Test]
        public void パターの予測に傾斜は含めない()
        {
            BallSimulator sloped = CreateOnSlopedGreen(new Vector2(1f, 0f));
            var flat = new BallSimulator(new BallPhysicsConfig(), new TerrainPhysicsConfig(), new UniformGround(GroundType.Green));
            flat.Place(Vector2.Zero);

            Assert.AreEqual(flat.PredictFullPower(Putter, Forward), sloped.PredictFullPower(Putter, Forward));
        }

        private static float RestDistance(ClubConfig club, ShotSpin spin)
        {
            var simulator = new BallSimulator(new BallPhysicsConfig());
            simulator.Place(Vector2.Zero);
            simulator.Launch(new ShotRequest(Forward, club, 1f, 0f, spin));
            simulator.AdvanceToRest();
            return simulator.Position.Y;
        }

        [Test]
        public void バックスピンはスピンなしより手前に止まる()
        {
            Assert.Less(RestDistance(Iron, ShotSpin.Back), RestDistance(Iron, ShotSpin.None));
        }

        [Test]
        public void トップスピンはスピンなしより奥に止まる()
        {
            Assert.Greater(RestDistance(Iron, ShotSpin.Top), RestDistance(Iron, ShotSpin.None));
        }

        [Test]
        public void スピンは着地点を変えない()
        {
            BallSimulator none = Shoot(1f);
            var back = new BallSimulator(new BallPhysicsConfig());
            back.Place(Vector2.Zero);
            back.Launch(new ShotRequest(Forward, Iron, 1f, 0f, ShotSpin.Back));

            Assert.AreEqual(FirstLanding(none), FirstLanding(back));
        }

        [Test]
        public void パターにスピンは効かない()
        {
            Assert.AreEqual(RestDistance(Putter, ShotSpin.None), RestDistance(Putter, ShotSpin.Back));
        }

        // ------------------------------------------------------------------
        // キャラの能力倍率
        // ------------------------------------------------------------------
        private static readonly CharacterAbility Power = new CharacterAbility(1.15f, 0.75f);
        private static readonly CharacterAbility Technique = new CharacterAbility(0.9f, 1.35f);

        private static BallSimulator ShootAs(CharacterAbility character, ClubConfig club, float impactOffset = 0f)
        {
            var simulator = new BallSimulator(new BallPhysicsConfig());
            simulator.SetCharacter(character);
            simulator.Place(Vector2.Zero);
            Hit(simulator, Forward, 1f, club, impactOffset);
            return simulator;
        }

        [Test]
        public void 倍率1のキャラはキャラを渡さないときと同じ位置に止まる()
        {
            BallSimulator plain = Shoot(1f, Iron, 0.5f);
            BallSimulator balance = ShootAs(CharacterAbility.Default, Iron, 0.5f);
            plain.AdvanceToRest();
            balance.AdvanceToRest();

            Assert.AreEqual(plain.Position, balance.Position);
        }

        [Test]
        public void 飛距離の倍率が高いほど遠くへ飛ぶ()
        {
            float balance = FirstLanding(ShootAs(CharacterAbility.Default, Driver)).Length();
            float power = FirstLanding(ShootAs(Power, Driver)).Length();

            Assert.Greater(power, balance);
        }

        [Test]
        public void パターにはキャラの飛距離が効かない()
        {
            BallSimulator balance = ShootAs(CharacterAbility.Default, Putter);
            BallSimulator power = ShootAs(Power, Putter);
            balance.AdvanceToRest();
            power.AdvanceToRest();

            Assert.AreEqual(balance.Position, power.Position);
        }

        [Test]
        public void 曲がりにくさが高いほど同じずれでも横に曲がらない()
        {
            float balance = FirstLanding(ShootAs(CharacterAbility.Default, Iron, 0.5f)).X;
            float technique = FirstLanding(ShootAs(Technique, Iron, 0.5f)).X;
            float power = FirstLanding(ShootAs(Power, Iron, 0.5f)).X;

            Assert.Less(technique, balance);
            Assert.Greater(power, balance);
        }

        [Test]
        public void 着地予測と試し打ちにもキャラの能力が効く()
        {
            var balance = new BallSimulator(new BallPhysicsConfig());
            balance.Place(Vector2.Zero);
            var power = new BallSimulator(new BallPhysicsConfig());
            power.SetCharacter(Power);
            power.Place(Vector2.Zero);

            Assert.Greater(power.PredictFullPower(Driver, Forward).Length(),
                balance.PredictFullPower(Driver, Forward).Length());

            var shot = new ShotRequest(Forward, Driver, 1f, 0f);
            Assert.Greater(power.TrySimulate(shot, false, false).Position.Length(),
                balance.TrySimulate(shot, false, false).Position.Length());
        }
    }
}
