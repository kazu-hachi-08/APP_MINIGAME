using System;
using System.Numerics;

namespace MiniGame.Golf
{
    /// <summary>
    /// ボールの飛行・バウンド・転がり・停止の計算（§8）。
    /// Rigidbody2D を使わず固定の時間刻みで自前計算するので、同じ入力からは必ず同じ結果になり、
    /// 着地予測や NPC の試し打ちにもそのまま使える。MonoBehaviour にしないのは EditModeテストで検証するため。
    /// 座標は地面の平面（X＝左右／Y＝奥行きZ）、Height は地面からの高さH。
    /// </summary>
    public sealed class BallSimulator
    {
        private readonly BallPhysicsConfig _config;

        public BallSimulator(BallPhysicsConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public Vector2 Position { get; private set; }
        public float Height { get; private set; }
        public Vector2 GroundVelocity { get; private set; }
        public float VerticalVelocity { get; private set; }
        public bool IsMoving { get; private set; }
        public float ElapsedTime { get; private set; }

        /// <summary>高さがあるか、打ち出し直後・バウンド直後で上向きに動いている</summary>
        public bool IsAirborne => Height > 0f || VerticalVelocity > 0f;

        public float TimeStep => _config.SimulationStep;

        /// <summary>ボールを止めた状態で置く（ティーや打ち直しの位置）</summary>
        public void Place(Vector2 position)
        {
            Position = position;
            Height = 0f;
            Stop();
        }

        /// <summary>今の位置から打つ。power は 0〜1</summary>
        public void Launch(Vector2 direction, float power)
        {
            if (direction.LengthSquared() <= 0f) return;

            float speed = _config.MaxLaunchSpeed * Math.Clamp(power, 0f, 1f);
            float angle = _config.LaunchAngleDegrees * MathF.PI / 180f;

            GroundVelocity = Vector2.Normalize(direction) * (speed * MathF.Cos(angle));
            VerticalVelocity = speed * MathF.Sin(angle);
            Height = 0f;
            ElapsedTime = 0f;
            IsMoving = true;
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
            float bounceSpeed = -VerticalVelocity * _config.BounceRestitution;
            GroundVelocity *= _config.BounceSpeedRetention;
            VerticalVelocity = bounceSpeed >= _config.MinBounceSpeed ? bounceSpeed : 0f;
        }

        private void AdvanceRoll(float dt)
        {
            float speed = GroundVelocity.Length();
            float nextSpeed = speed - _config.RollDeceleration * dt;
            if (nextSpeed <= _config.StopSpeed)
            {
                Stop();
                return;
            }

            GroundVelocity *= nextSpeed / speed;
            Position += GroundVelocity * dt;
        }

        private void Stop()
        {
            GroundVelocity = Vector2.Zero;
            VerticalVelocity = 0f;
            IsMoving = false;
        }
    }
}
