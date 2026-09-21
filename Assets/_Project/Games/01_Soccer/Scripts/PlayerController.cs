using MiniGame.Common.Input;
using UnityEngine;

namespace MiniGame.Soccer
{
    /// <summary>
    /// 操作対象選手の移動・向き・ドリブルを制御する（Phase 2: プレイヤー操作）
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float _moveSpeed = 4f;
        [SerializeField] private float _rotationSpeed = 720f; // 度/秒。向き変更の追従速度

        [Header("Dribble")]
        [SerializeField] private float _dribbleRadius = 0.6f;
        [SerializeField] private float _dribbleForce = 10f;
        [SerializeField] private LayerMask _ballLayerMask = ~0;

        private Rigidbody2D _rigidbody;

        /// <summary>選手が現在向いている方向（Phase 3のキック方向にも使用予定）</summary>
        public Vector2 FacingDirection { get; private set; } = Vector2.up;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
        }

        private void FixedUpdate()
        {
            Vector2 moveInput = InputManager.HasInstance ? InputManager.Instance.MoveVector : Vector2.zero;
            _rigidbody.linearVelocity = moveInput * _moveSpeed;

            if (moveInput.sqrMagnitude > 0.01f)
            {
                FacingDirection = moveInput.normalized;
                RotateTowardsFacing();
                DribbleBall();
            }
        }

        private void RotateTowardsFacing()
        {
            // スプライトの正面（ローカルY+）を移動方向へ向ける
            float targetAngle = Mathf.Atan2(FacingDirection.y, FacingDirection.x) * Mathf.Rad2Deg - 90f;
            float newAngle = Mathf.MoveTowardsAngle(_rigidbody.rotation, targetAngle, _rotationSpeed * Time.fixedDeltaTime);
            _rigidbody.MoveRotation(newAngle);
        }

        private void DribbleBall()
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(_rigidbody.position, _dribbleRadius, _ballLayerMask);
            foreach (var hit in hits)
            {
                if (!hit.TryGetComponent<Ball>(out var ball)) continue;

                // 進行方向の少し前にボールを引き寄せ、押し出しながら運ぶ感覚を出す
                Vector2 targetPosition = _rigidbody.position + FacingDirection * (_dribbleRadius * 0.6f);
                Vector2 toTarget = targetPosition - ball.Position;
                ball.ApplyForce(toTarget * _dribbleForce);
                break;
            }
        }
    }
}
