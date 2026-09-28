using System;
using System.Numerics;

namespace MiniGame.Golf
{
    /// <summary>
    /// ボールの飛行・風・バウンド・転がり・停止・カップイン・池とOBの計算。
    /// Rigidbody2D を使わず固定の時間刻みで自前計算するので、同じ入力からは必ず同じ結果になり、
    /// 着地予測や NPC の試し打ちにもそのまま使える。MonoBehaviour にしないのは EditModeテストで検証するため。
    /// 座標は地面の平面（X＝左右／Y＝奥行きZ）、Height は地面からの高さH。
    /// </summary>
    public sealed class BallSimulator
    {
        // ImpactOffset の ±1 がインパクトゾーンの端。これを超えたらミスショット
        private const float ImpactZoneEdge = 1f;

        private readonly BallPhysicsConfig _config;
        private readonly TerrainPhysicsConfig _terrain;
        private readonly IGroundMap _ground;
        private readonly ISlopeMap _slope;

        private Vector2 _cupPosition;
        private bool _hasCup;

        // 手番のキャラの能力。既定はバランス型と同じ 1 倍なので、渡さなければキャラ導入前と同じ飛び方になる
        private CharacterAbility _character = CharacterAbility.Default;

        // 空中にいる間だけ足す風の加速度。ホールの間は変わらない
        private Vector2 _windAcceleration;

        // 進行方向の右向きを正とする曲がりの加速度。打ち上げたショットは最初の着地まで、パットは止まるまで効かせる
        private float _curveAcceleration;
        private bool _isPutt;

        // 最初の着地で地面方向の速さに掛ける倍率（スピン）。一度使ったら 1 に戻し、2回目以降のバウンドには効かせない
        private float _spinRollRate = 1f;

        // 打ち直しの位置。OB は打つ前の場所、池は入る直前に通った池・OB以外の地点
        private Vector2 _launchPosition;
        private Vector2 _lastSafePosition;

        /// <summary>terrain・ground を省略するとどこでもフェアウェイ、slope を省略すると平らな地面として計算する</summary>
        public BallSimulator(BallPhysicsConfig config, TerrainPhysicsConfig terrain = null, IGroundMap ground = null,
            ISlopeMap slope = null)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _terrain = terrain ?? new TerrainPhysicsConfig();
            _ground = ground;
            _slope = slope;
        }

        public Vector2 Position { get; private set; }
        public float Height { get; private set; }
        public Vector2 GroundVelocity { get; private set; }
        public float VerticalVelocity { get; private set; }
        public bool IsMoving { get; private set; }
        public bool IsInCup { get; private set; }
        public float ElapsedTime { get; private set; }

        /// <summary>池・OBに入って止まった。次は DropPosition から1打罰で打つ</summary>
        public bool IsInHazard { get; private set; }

        /// <summary>池・OBに入ったときに次に打つ場所</summary>
        public Vector2 DropPosition { get; private set; }

        /// <summary>高さがあるか、打ち出し直後・バウンド直後で上向きに動いている</summary>
        public bool IsAirborne => Height > 0f || VerticalVelocity > 0f;

        public float TimeStep => _config.SimulationStep;

        /// <summary>今いる位置の地面の種類</summary>
        public GroundType Ground => _ground?.GetGround(Position) ?? GroundType.Fairway;

        /// <summary>今いる位置のライ。次のショットの飛距離・インパクトゾーンの幅に効く</summary>
        public TerrainPhysics Lie => _terrain.Get(Ground);

        public void SetCup(Vector2 position)
        {
            _cupPosition = position;
            _hasCup = true;
        }

        public void SetWind(Wind wind)
        {
            _windAcceleration = wind.Velocity * _config.WindAccelerationScale;
        }

        /// <summary>
        /// 手番のキャラの能力を設定する。着地予測・NPC の試し打ちにも引き継ぐので、
        /// 予測線・NPC の狙い・相手端末の再生がすべてこのキャラの能力どおりになる
        /// </summary>
        public void SetCharacter(CharacterAbility character)
        {
            _character = character;
        }

        /// <summary>ボールを止めた状態で置く（ティーや打ち直しの位置）</summary>
        public void Place(Vector2 position)
        {
            Position = position;
            Height = 0f;
            IsInCup = false;
            IsInHazard = false;
            Stop();
        }

        /// <summary>今の位置から打つ。パターは転がし、それ以外はクラブの打ち出し角で打ち上げる</summary>
        public void Launch(ShotRequest shot)
        {
            ClubConfig club = shot.Club;
            float power = Math.Clamp(shot.Power, 0f, 1f);
            float curve = shot.ImpactOffset;
            ApplyMissShot(ref curve, ref power);

            float speed = LaunchSpeed(club, power);
            _launchPosition = Position;
            _lastSafePosition = Position;
            StartMoving(shot.Direction, club, speed);

            _curveAcceleration = curve * club.CurveFactor / _character.StraightnessRate * _config.CurveAccelerationScale;
            _isPutt = club.IsPutter;
            _spinRollRate = club.IsPutter ? 1f : SpinRollRate(shot.Spin);
        }

        /// <summary>ゾーンの外はミスショット：大きく曲がり、飛距離も落ちる</summary>
        private void ApplyMissShot(ref float curve, ref float power)
        {
            if (Math.Abs(curve) > ImpactZoneEdge)
            {
                curve = Math.Sign(curve) * _config.MissShotCurve;
                power *= _config.MissShotPowerRate;
            }
        }

        private float LaunchSpeed(ClubConfig club, float power)
        {
            // 飛距離はおおよそ初速の2乗に比例するので、平方根を掛けて飛距離の割合がライの ShotDistanceRate どおりになるようにする
            // パターは強さのさじ加減だけを競うので、キャラの飛距離は効かせない
            float distanceRate = club.IsPutter ? 1f : _character.DistanceRate;
            return club.MaxLaunchSpeed * distanceRate * power * MathF.Sqrt(Lie.ShotDistanceRate);
        }

        /// <summary>パターは高さを持たせず転がし、それ以外は打ち出し角で水平・垂直の速さに分ける</summary>
        private void StartMoving(Vector2 direction, ClubConfig club, float speed)
        {
            if (club.IsPutter)
            {
                StartMoving(direction, speed, 0f);
                return;
            }

            float angle = club.LaunchAngleDegrees * MathF.PI / 180f;
            StartMoving(direction, speed * MathF.Cos(angle), speed * MathF.Sin(angle));
        }

        private float SpinRollRate(ShotSpin spin)
        {
            switch (spin)
            {
                case ShotSpin.Back: return _config.BackSpinRollRate;
                case ShotSpin.Top: return _config.TopSpinRollRate;
                default: return 1f;
            }
        }

        /// <summary>
        /// 今の位置からクラブをフルパワー・まっすぐで打ったときの着地点。パターは止まる位置。
        /// 別の計算機で試し打ちするので、このボールの状態は変わらない。風と傾斜は読むのがプレイヤーの仕事なので含めない。
        /// </summary>
        public Vector2 PredictFullPower(ClubConfig club, Vector2 direction)
        {
            return Predict(club, direction, 1f);
        }

        /// <summary>PredictFullPower のパワー指定版。パターの距離の目盛りに使う</summary>
        public Vector2 Predict(ClubConfig club, Vector2 direction, float power)
        {
            // 傾斜を渡さないことで、傾斜なしのまっすぐな予測にする
            BallSimulator probe = CreateProbe(null);
            probe.Launch(new ShotRequest(direction, club, power, 0f));

            if (club.IsPutter)
            {
                probe.AdvanceToRest();
            }
            else
            {
                probe.AdvanceToFirstLanding();
            }

            return probe.Position;
        }

        /// <summary>
        /// 今の位置から shot を打って止まるまで計算した結果（NPC の試し打ち）。別の計算機で打つので、このボールの状態は変わらない。
        /// 風と傾斜を入れるかは、NPC の難易度で「読むかどうか」を変えられるように選べる。
        /// </summary>
        public ShotOutcome TrySimulate(ShotRequest shot, bool withWind, bool withSlope)
        {
            BallSimulator probe = CreateProbe(withSlope ? _slope : null);
            if (_hasCup) probe.SetCup(_cupPosition);
            if (withWind) probe._windAcceleration = _windAcceleration;

            probe.Launch(shot);
            probe.AdvanceToRest();
            return new ShotOutcome(probe.Position, probe.IsInHazard, probe.IsInCup);
        }

        /// <summary>今の位置・キャラを引き継いだ試し打ち用の計算機。カップと風は呼び出し側が必要なときだけ渡す</summary>
        private BallSimulator CreateProbe(ISlopeMap slope)
        {
            var probe = new BallSimulator(_config, _terrain, _ground, slope);
            probe.SetCharacter(_character);
            probe.Place(Position);
            return probe;
        }

        /// <summary>最初に地面に着いた瞬間で止める。打ち出し直後は高さ0なので、先に1回進める</summary>
        private void AdvanceToFirstLanding()
        {
            do
            {
                Advance();
            } while (IsMoving && Height > 0f);
        }

        /// <summary>SimulationStep 秒だけ進める。表示側は経過時間ぶんこれを繰り返し呼ぶ</summary>
        public void Advance()
        {
            if (!IsMoving) return;

            float dt = _config.SimulationStep;
            ElapsedTime += dt;
            RememberSafePosition();

            if (IsAirborne)
            {
                AdvanceFlight(dt);
            }
            else
            {
                AdvanceRoll(dt);
            }

            if (ElapsedTime >= _config.MaxSimulationTime)
            {
                Stop();
            }
        }

        /// <summary>止まるまで一気に計算する（テスト・着地予測・NPCの試し打ち用）</summary>
        public void AdvanceToRest()
        {
            while (IsMoving)
            {
                Advance();
            }
        }

        private void StartMoving(Vector2 direction, float groundSpeed, float verticalSpeed)
        {
            if (direction.LengthSquared() <= 0f) return;

            GroundVelocity = Vector2.Normalize(direction) * groundSpeed;
            VerticalVelocity = verticalSpeed;
            Height = 0f;
            ElapsedTime = 0f;
            IsInCup = false;
            IsInHazard = false;
            IsMoving = true;
        }

        private void AdvanceFlight(float dt)
        {
            ApplyCurve(dt);
            GroundVelocity += _windAcceleration * dt;
            Position += GroundVelocity * dt;
            VerticalVelocity -= _config.Gravity * dt;
            Height += VerticalVelocity * dt;

            if (Height <= 0f)
            {
                Land();
            }
        }

        private void Land()
        {
            Height = 0f;
            // 着地で回転が落ちるとみなし、バウンド以降は曲げない
            _curveAcceleration = 0f;

            // ダイレクトイン：着地した瞬間にカップの上なら速さに関係なく入れる
            if (IsOverCup())
            {
                HoleOut();
                return;
            }

            if (TryEnterHazard()) return;

            Bounce();
        }

        /// <summary>地面に応じて跳ね返す。スピンは最初の着地でだけ効かせる</summary>
        private void Bounce()
        {
            TerrainPhysics terrain = _terrain.Get(Ground);
            float bounceSpeed = -VerticalVelocity * terrain.BounceRestitution;
            GroundVelocity *= terrain.BounceSpeedRetention * _spinRollRate;
            _spinRollRate = 1f;
            VerticalVelocity = bounceSpeed >= _config.MinBounceSpeed ? bounceSpeed : 0f;
        }

        private void AdvanceRoll(float dt)
        {
            ApplySlope(dt);
            float speed = GroundVelocity.Length();
            float nextSpeed = speed - _terrain.Get(Ground).RollDeceleration * dt;
            if (nextSpeed <= _config.StopSpeed)
            {
                Stop();
                TryHoleOut();
                return;
            }

            GroundVelocity *= nextSpeed / speed;
            if (_isPutt) ApplyCurve(dt);
            Position += GroundVelocity * dt;
            if (TryEnterHazard()) return;

            TryHoleOut();
        }

        /// <summary>
        /// 転がっている間だけ、いるマスの下り方向へ加速させる。
        /// 減速より弱い傾斜なら、上りで止まったボールはそこで止まったままにする（止まった後は計算しない）
        /// </summary>
        private void ApplySlope(float dt)
        {
            if (_slope == null) return;

            GroundVelocity += _slope.GetSlope(Position) * (_config.SlopeAccelerationScale * dt);
        }

        /// <summary>空中で池の上を通った地点は覚えず、池に落ちたら岸まで戻せるようにする</summary>
        private void RememberSafePosition()
        {
            if (!IsHazard(Ground)) _lastSafePosition = Position;
        }

        /// <summary>池・OBは着地した瞬間か転がって入った瞬間にボールを止め、打ち直しの位置を決める</summary>
        private bool TryEnterHazard()
        {
            GroundType ground = Ground;
            if (!IsHazard(ground)) return false;

            DropPosition = ground == GroundType.Water ? _lastSafePosition : _launchPosition;
            IsInHazard = true;
            Stop();
            return true;
        }

        private static bool IsHazard(GroundType ground)
        {
            return ground == GroundType.Water || ground == GroundType.OutOfBounds;
        }

        /// <summary>進行方向の横向きに加速度を足す。速さは変えず向きだけ変えて、曲がりで飛距離が伸び縮みしないようにする</summary>
        private void ApplyCurve(float dt)
        {
            float speed = GroundVelocity.Length();
            if (_curveAcceleration == 0f || speed <= 0f) return;

            var right = new Vector2(GroundVelocity.Y, -GroundVelocity.X) / speed;
            Vector2 curved = GroundVelocity + right * (_curveAcceleration * dt);
            GroundVelocity = Vector2.Normalize(curved) * speed;
        }

        /// <summary>転がってカップの上に来ても、速すぎれば通り過ぎる</summary>
        private void TryHoleOut()
        {
            if (!IsOverCup() || GroundVelocity.Length() > _config.CupInMaxSpeed) return;

            HoleOut();
        }

        private bool IsOverCup()
        {
            return _hasCup && Vector2.Distance(Position, _cupPosition) <= _config.CupRadius;
        }

        private void HoleOut()
        {
            Position = _cupPosition;
            IsInCup = true;
            Stop();
        }

        private void Stop()
        {
            GroundVelocity = Vector2.Zero;
            VerticalVelocity = 0f;
            IsMoving = false;
        }
    }
}
