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

        /// <summary>今の位置から打ち上げる。power は 0〜1</summary>
        public void Launch(Vector2 direction, float power)
        {
            float speed = _config.MaxLaunchSpeed * Math.Clamp(power, 0f, 1f);
            float angle = _config.LaunchAngleDegrees * MathF.PI / 180f;
            StartMoving(direction, speed * MathF.Cos(angle), speed * MathF.Sin(angle));
        }

        /// <summary>高さを持たせず転がして打つ（仮のパット）。power は 0〜1</summary>
        public void LaunchPutt(Vector2 direction, float power)
        {
            StartMoving(direction, _config.PuttMaxSpeed * Math.Clamp(power, 0f, 1f), 0f);
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
            Position += GroundVelocity * dt;
            TryHoleOut();
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
