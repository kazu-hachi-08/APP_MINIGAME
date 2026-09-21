using System;
using UnityEngine;

namespace MiniGame.TableTennis
{
    /// <summary>
    /// フリック入力・ラケット・ボールをつなぐ打球処理。
    /// ボールが打球可能範囲に無ければ、仕様通りフリックを無視する。
    /// </summary>
    public class PlayerSwing : MonoBehaviour
    {
        [SerializeField] private BallMotion _ball;
        [SerializeField] private RacketController _racket;
        [SerializeField] private FlickInput _flickInput;
        [SerializeField] private ShotCalculator _shotCalculator;

        [Header("打球可能範囲 (m)")]
        [Tooltip("ラケットより手前側で打てる範囲")]
        [SerializeField] private float _hitRangeNear = 0.2f;

        [Tooltip("ラケットより奥側で打てる範囲")]
        [SerializeField] private float _hitRangeFar = 0.55f;

        [SerializeField] private float _hitRadiusX = 0.32f;
        [SerializeField] private float _hitRadiusY = 0.32f;

        /// <summary>打球が成立したときに通知する（HUD表示・SE用）</summary>
        public event Action<FlickData, ShotResult> OnShot;

        /// <summary>打球を受け付けるかどうか。ラリー外やポーズ中は false にする</summary>
        public bool CanSwing { get; set; } = true;

        private void OnEnable()
        {
            _flickInput.OnFlicked += HandleFlick;
        }

        private void OnDisable()
        {
            _flickInput.OnFlicked -= HandleFlick;
        }

        private void HandleFlick(FlickData flick)
        {
            if (!CanSwing || !IsBallHittable()) return;

            ShotResult shot = _shotCalculator.Calculate(flick);
            _ball.Launch(_ball.CourtPosition, shot.Velocity, shot.Spin);

            OnShot?.Invoke(flick, shot);
        }

        /// <summary>
        /// ボールがラケットの打球可能範囲にあるか。
        /// 打球タイミングによる補正（早すぎ/遅すぎ）は Phase 4 で追加する。
        /// </summary>
        private bool IsBallHittable()
        {
            if (!_ball.IsFlying) return false;

            // 自分へ向かってきていないボール（打ち返した直後など）は打てない
            if (_ball.Velocity.z >= 0f) return false;

            Vector3 ball = _ball.CourtPosition;
            Vector3 racket = _racket.CourtPosition;

            float depthDifference = ball.z - racket.z;
            if (depthDifference < -_hitRangeNear || depthDifference > _hitRangeFar) return false;

            return Mathf.Abs(ball.x - racket.x) <= _hitRadiusX
                && Mathf.Abs(ball.y - racket.y) <= _hitRadiusY;
        }
    }
}
