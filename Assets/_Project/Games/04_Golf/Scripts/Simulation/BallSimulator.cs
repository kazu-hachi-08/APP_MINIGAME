using System;
using System.Numerics;

namespace MiniGame.Golf
{
    /// <summary>
    /// ボールの飛行・バウンド・転がり・停止・カップインの計算（§8）。
    /// Rigidbody2D を使わず固定の時間刻みで自前計算するので、同じ入力からは必ず同じ結果になり、
    /// 着地予測や NPC の試し打ちにもそのまま使える。MonoBehaviour にしないのは EditModeテストで検証するため。
    /// 座標は地面の平面（X＝左右／Y＝奥行きZ）、Height は地面からの高さH。
    /// </summary>
    public sealed class BallSimulator
    {
        private readonly BallPhysicsConfig _config;
        private readonly TerrainPhysicsConfig _terrain;
        private readonly IGroundMap _ground;

        private Vector2 _cupPosition;
        private bool _hasCup;

        // 進行方向の右向きを正とする曲がりの加速度。打ち上げたショットは最初の着地まで、パットは止まるまで効かせる
        private float _curveAcceleration;
        private bool _isPutt;

        /// <summary>terrain・ground を省略すると、どこでもフェアウェイの平らな地面として計算する</summary>
        public BallSimulator(BallPhysicsConfig config, TerrainPhysicsConfig terrain = null, IGroundMap ground = null)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _terrain = terrain ?? new TerrainPhysicsConfig();
            _ground = ground;
        }

        public Vector2 Position { get; private set; }
        public float Height { get; private set; }
        public Vector2 GroundVelocity { get; private set; }
        public float VerticalVelocity { get; private set; }
        public bool IsMoving { get; private set; }
        public bool IsInCup { get; private set; }
        public float ElapsedTime { get; private set; }

        /// <summary>高さがあるか、打ち出し直後・バウンド直後で上向きに動いている</summary>
        public bool IsAirborne => Height > 0f || VerticalVelocity > 0f;

        public float TimeStep => _config.SimulationStep;

        /// <summary>今いる位置の地面の種類</summary>
        public GroundType Ground => _ground?.GetGround(Position) ?? GroundType.Fairway;

        public void SetCup(Vector2 position)
        {
            _cupPosition = position;
            _hasCup = true;
        }

        /// <summary>ボールを止めた状態で置く（ティーや打ち直しの位置）</summary>
        public void Place(Vector2 position)
        {
            Position = position;
            Height = 0f;
            IsInCup = false;
            Stop();
        }

        /// <summary>今の位置から打つ。パターは転がし、それ以外はクラブの打ち出し角で打ち上げる</summary>
        public void Launch(ShotRequest shot)
        {
            ClubConfig club = shot.Club;
            float power = Math.Clamp(shot.Power, 0f, 1f);
            float curve = shot.ImpactOffset;

            // §7.4 ゾーンの外はミスショット：大きく曲がり、飛距離も落ちる
            if (Math.Abs(curve) > 1f)
            {
                curve = Math.Sign(curve) * _config.MissShotCurve;
                power *= _config.MissShotPowerRate;
            }

            float speed = club.MaxLaunchSpeed * power;
            if (club.IsPutter)
            {
                StartMoving(shot.Direction, speed, 0f);
            }
            else
            {
                float angle = club.LaunchAngleDegrees * MathF.PI / 180f;
                StartMoving(shot.Direction, speed * MathF.Cos(angle), speed * MathF.Sin(angle));
            }

            _curveAcceleration = curve * club.CurveFactor * _config.CurveAccelerationScale;
            _isPutt = club.IsPutter;
        }

        /// <summary>
        /// 今の位置からクラブをフルパワー・まっすぐで打ったときの着地点（§7.3）。パターは止まる位置。
        /// 別の計算機で試し打ちするので、このボールの状態は変わらない。
        /// </summary>
        public Vector2 PredictFullPower(ClubConfig club, Vector2 direction)
        {
            var probe = new BallSimulator(_config, _terrain, _ground);
            probe.Place(Position);
            probe.Launch(new ShotRequest(direction, club, 1f, 0f));

            if (club.IsPutter)
            {
                probe.AdvanceToRest();
                return probe.Position;
            }

            // 最初に地面に着いた瞬間で止める。打ち出し直後は高さ0なので、先に1回進める
            do
            {
                probe.Advance();
            } while (probe.IsMoving && probe.Height > 0f);

            return probe.Position;
        }

        /// <summary>SimulationStep 秒だけ進める。表示側は経過時間ぶんこれを繰り返し呼ぶ</summary>
        public void Advance()
        {
            if (!IsMoving) return;

            float dt = _config.SimulationStep;
            ElapsedTime += dt;

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
            IsMoving = true;
        }

        private void AdvanceFlight(float dt)
        {
            ApplyCurve(dt);
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

            // §8.6 ダイレクトイン：着地した瞬間にカップの上なら速さに関係なく入れる
            if (IsOverCup())
            {
                HoleOut();
                return;
            }

            TerrainPhysics terrain = _terrain.Get(Ground);
            float bounceSpeed = -VerticalVelocity * terrain.BounceRestitution;
            GroundVelocity *= terrain.BounceSpeedRetention;
            VerticalVelocity = bounceSpeed >= _config.MinBounceSpeed ? bounceSpeed : 0f;
        }

        private void AdvanceRoll(float dt)
        {
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
            TryHoleOut();
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
