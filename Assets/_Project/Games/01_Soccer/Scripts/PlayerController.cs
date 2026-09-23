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
        [SerializeField] private float _moveSpeed = 5.5f;

        [Header("Dribble")]
        [SerializeField] private float _dribbleRadius = 0.6f;     // この距離以内のボールを保持する
        [SerializeField] private float _holdOffset = 0.4f;        // 保持中のボール位置（足元から向いている方向へのずれ）
        [SerializeField] private float _dribbleSuppressSpeed = 5f; // ボールがこれ以上の速さで動いている間は保持しない（パスやシュートを横取りしてしまうのを防ぐ）
        [SerializeField] private float _stealRadius = 0.45f;      // 相手選手がボールにこの距離まで近づいたら奪われる
        [SerializeField] private float _regrabCooldown = 0.5f;    // 手放した直後にすぐ保持し直して、奪われた瞬間に奪い返すのを防ぐ

        [Header("Kick")]
        [SerializeField] private float _kickRadius = 0.7f;
        [SerializeField] private float _passSpeed = 9f;
        [SerializeField] private float _shootSpeed = 17f;

        [SerializeField] private LayerMask _ballLayerMask = ~0;

        private Rigidbody2D _rigidbody;
        private Collider2D _collider;
        private TeamMember _teamMember;
        private Ball _heldBall;
        private float _regrabTimer;

        /// <summary>選手が現在向いている方向（キック方向として使用）</summary>
        public Vector2 FacingDirection { get; private set; } = Vector2.up;

        // スライディングタックル中はSlidingTackle側がRigidbodyの速度を制御するため、
        // 通常の移動・キック処理をここで止める（同一フレームでの上書き合戦を防ぐ）
        private bool _movementSuppressed;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            _collider = GetComponent<Collider2D>();
            _teamMember = GetComponent<TeamMember>();
        }

        private void OnDisable()
        {
            // 選手切り替えで操作が外れたら、ボールを足元に縛ったままにしない
            ReleaseBall();
        }

        /// <summary>
        /// タックル中など、外部コンポーネントが移動を制御する間だけ通常の移動・キックを止める
        /// </summary>
        public void SetMovementSuppressed(bool suppressed)
        {
            _movementSuppressed = suppressed;
            if (suppressed) ReleaseBall();
        }

        private void Update()
        {
            if (_movementSuppressed) return;
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
            if (_movementSuppressed)
            {
                _rigidbody.linearVelocity = Vector2.zero;
                return;
            }

            Vector2 moveInput = InputManager.HasInstance ? InputManager.Instance.MoveVector : Vector2.zero;
            _rigidbody.linearVelocity = moveInput * _moveSpeed;

            if (moveInput.sqrMagnitude > 0.01f)
            {
                FacingDirection = moveInput.normalized;
            }

            UpdateDribble();
        }

        private void UpdateDribble()
        {
            if (_regrabTimer > 0f)
            {
                _regrabTimer -= Time.fixedDeltaTime;
            }

            // 相手のキックやゴール後のリセットなど、ボール側で保持が解除されていたら追従をやめる
            if (_heldBall != null && !_heldBall.IsHeldBy(_collider))
            {
                _heldBall = null;
                _regrabTimer = _regrabCooldown;
            }

            if (_heldBall == null)
            {
                TryStartDribble();
                return;
            }

            if (IsOpponentNearBall(_heldBall.Position))
            {
                ReleaseBall();
                return;
            }

            _heldBall.MoveHeldTo(_rigidbody.position + FacingDirection * _holdOffset);
        }

        private void TryStartDribble()
        {
            if (_regrabTimer > 0f) return;
            if (!TryFindNearbyBall(_dribbleRadius, out var ball)) return;
            if (ball.Velocity.sqrMagnitude > _dribbleSuppressSpeed * _dribbleSuppressSpeed) return;

            ball.Hold(_collider);
            _heldBall = ball;
        }

        private void ReleaseBall()
        {
            if (_heldBall == null) return;

            if (_heldBall.IsHeldBy(_collider)) _heldBall.Release();
            _heldBall = null;
            _regrabTimer = _regrabCooldown;
        }

        /// <summary>
        /// 相手選手がボールに触れる距離まで来たら奪われたとみなす（AIはドリブルしないので、手放した後はAIの通常処理に任せる）
        /// </summary>
        private bool IsOpponentNearBall(Vector2 ballPosition)
        {
            if (_teamMember == null) return false;

            Collider2D[] hits = Physics2D.OverlapCircleAll(ballPosition, _stealRadius);
            foreach (var hit in hits)
            {
                if (hit.TryGetComponent<TeamMember>(out var member) && member.Team != _teamMember.Team) return true;
            }

            return false;
        }

        private void TryKick(float speed)
        {
            if (!TryFindNearbyBall(_kickRadius, out var ball)) return;
            ball.Kick(FacingDirection, speed);
            ReleaseBall();
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
