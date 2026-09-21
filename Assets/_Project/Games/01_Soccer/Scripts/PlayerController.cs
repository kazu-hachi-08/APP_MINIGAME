using MiniGame.Common.Input;
using UnityEngine;

namespace MiniGame.Soccer
{
    /// <summary>
    /// 操作対象選手の移動・向き・ドリブル・パス/シュートを制御する（Phase 3: パス・シュート）
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float _moveSpeed = 4f;

        [Header("Dribble")]
        [SerializeField] private float _dribbleRadius = 0.6f;
        [SerializeField] private float _dribbleForce = 10f;
        [SerializeField] private float _dribbleSuppressSpeed = 5f; // ボールがこれ以上の速さで動いている間はドリブルの引き寄せを行わない（キック直後の暴発防止）

        [Header("Kick")]
        [SerializeField] private float _kickRadius = 0.7f;
        [SerializeField] private float _passSpeed = 6f;
        [SerializeField] private float _shootSpeed = 12f;

        [SerializeField] private LayerMask _ballLayerMask = ~0;

        private Rigidbody2D _rigidbody;

        /// <summary>選手が現在向いている方向（キック方向として使用）</summary>
        public Vector2 FacingDirection { get; private set; } = Vector2.up;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
        }

        private void Update()
        {
            if (!InputManager.HasInstance) return;

            // Action1 = パス（弱いキック）、Action2 = シュート（強いキック）
            if (InputManager.Instance.IsAction1Down)
            {
                TryKick(_passSpeed);
            }
            else if (InputManager.Instance.IsAction2Down)
            {
                TryKick(_shootSpeed);
            }
        }

        private void FixedUpdate()
        {
            Vector2 moveInput = InputManager.HasInstance ? InputManager.Instance.MoveVector : Vector2.zero;
            _rigidbody.linearVelocity = moveInput * _moveSpeed;

            if (moveInput.sqrMagnitude > 0.01f)
            {
                FacingDirection = moveInput.normalized;
                DribbleBall();
            }
        }

        private void DribbleBall()
        {
            if (!TryFindNearbyBall(_dribbleRadius, out var ball)) return;
            if (ball.Velocity.sqrMagnitude > _dribbleSuppressSpeed * _dribbleSuppressSpeed) return;

            // 進行方向の少し前にボールを引き寄せ、押し出しながら運ぶ感覚を出す
            Vector2 targetPosition = _rigidbody.position + FacingDirection * (_dribbleRadius * 0.6f);
            Vector2 toTarget = targetPosition - ball.Position;
            ball.ApplyForce(toTarget * _dribbleForce);
        }

        private void TryKick(float speed)
        {
            if (!TryFindNearbyBall(_kickRadius, out var ball)) return;
            ball.Kick(FacingDirection, speed);
        }

        private bool TryFindNearbyBall(float radius, out Ball ball)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(_rigidbody.position, radius, _ballLayerMask);
            foreach (var hit in hits)
            {
                if (hit.TryGetComponent(out ball)) return true;
            }

            ball = null;
            return false;
        }
    }
}
