using MiniGame.Common.Input;
using UnityEngine;

namespace MiniGame.Soccer
{
    /// <summary>
    /// 操作対象選手のスライディングタックル（簡易ディフェンス機能）。
    /// ファウル判定や成否判定は持たず、「向いている方向へ突進してボールに触れたら弾き飛ばす」
    /// だけのシンプルな実装。突進後に一定時間だけ移動速度が落ちる硬直をリスクとする。
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(PlayerController))]
    public class SlidingTackle : MonoBehaviour
    {
        private enum State
        {
            Ready,
            Dashing,
            Recovering
        }

        [Header("Dash")]
        [SerializeField] private float _dashSpeed = 7f;
        [SerializeField] private float _dashDuration = 0.25f;

        [Header("Recovery (硬直)")]
        [SerializeField] private float _recoveryDuration = 0.4f;
        [SerializeField] private float _recoverySpeed = 2.5f;

        [Header("Knock")]
        [SerializeField] private float _tackleRadius = 0.55f;
        [SerializeField] private float _knockSpeed = 7f;
        [SerializeField] private LayerMask _ballLayerMask = ~0;

        [Header("Ball Carrier Reaction")]
        [SerializeField] private float _carrierSearchRadius = 1.0f; // ボールからこの距離以内にいる相手を「奪われた選手」とみなす

        private Rigidbody2D _rigidbody;
        private PlayerController _playerController;
        private TeamMember _teamMember;
        private State _state = State.Ready;
        private float _timer;
        private Vector2 _dashDirection;
        private bool _hasKnockedThisTackle;

        // PlayerController と同じく、オンライン対戦ではホストがAWAY選手を相手の入力で動かすため差し替え可能にする
        private IInputProvider _input;

        private IInputProvider Input => _input ?? (InputManager.HasInstance ? InputManager.Instance : null);

        public void SetInput(IInputProvider input)
        {
            _input = input;
        }

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            _playerController = GetComponent<PlayerController>();
            _teamMember = GetComponent<TeamMember>();
        }

        private void OnDisable()
        {
            // 選手切り替え等で無効化された場合、移動抑制を残さず元に戻す
            _state = State.Ready;
            _playerController.SetMovementSuppressed(false);
        }

        private void Update()
        {
            if (_state != State.Ready) return;
            var input = Input;
            if (input == null || !input.IsAction4Down) return;

            StartDash();
        }

        private void FixedUpdate()
        {
            switch (_state)
            {
                case State.Dashing:
                    TickDash();
                    break;
                case State.Recovering:
                    TickRecovery();
                    break;
            }
        }

        private void StartDash()
        {
            _state = State.Dashing;
            _timer = _dashDuration;
            _dashDirection = _playerController.FacingDirection;
            _hasKnockedThisTackle = false;
            _playerController.SetMovementSuppressed(true);
        }

        private void TickDash()
        {
            _rigidbody.linearVelocity = _dashDirection * _dashSpeed;
            TryKnockBall();

            _timer -= Time.fixedDeltaTime;
            if (_timer <= 0f)
            {
                _state = State.Recovering;
                _timer = _recoveryDuration;
            }
        }

        private void TickRecovery()
        {
            var input = Input;
            Vector2 moveInput = input != null ? input.MoveVector : Vector2.zero;
            _rigidbody.linearVelocity = moveInput * _recoverySpeed;

            _timer -= Time.fixedDeltaTime;
            if (_timer <= 0f)
            {
                _state = State.Ready;
                _playerController.SetMovementSuppressed(false);
            }
        }

        /// <summary>
        /// 突進中にボールへ触れたら、その場で一度だけ弾き飛ばす（1タックルにつき1回のみ）
        /// </summary>
        private void TryKnockBall()
        {
            if (_hasKnockedThisTackle) return;

            Collider2D[] hits = Physics2D.OverlapCircleAll(_rigidbody.position, _tackleRadius, _ballLayerMask);
            foreach (var hit in hits)
            {
                if (!hit.TryGetComponent<Ball>(out var ball)) continue;

                StaggerBallCarrier(ball.Position);
                ball.Kick(_dashDirection, _knockSpeed);
                _hasKnockedThisTackle = true;
                break;
            }
        }

        /// <summary>
        /// タックル直前にボールへ最も近かった相手選手を「奪われた側」とみなし、よろけ演出を発生させる
        /// </summary>
        private void StaggerBallCarrier(Vector2 ballPosition)
        {
            if (_teamMember == null) return;

            TeamMember nearestOpponent = null;
            float nearestDistance = _carrierSearchRadius;

            var allMembers = Object.FindObjectsByType<TeamMember>(FindObjectsSortMode.None);
            foreach (var member in allMembers)
            {
                if (member.Team == _teamMember.Team) continue;

                float distance = Vector2.Distance(member.transform.position, ballPosition);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestOpponent = member;
                }
            }

            if (nearestOpponent != null && nearestOpponent.TryGetComponent<TackleReaction>(out var reaction))
            {
                reaction.Stagger();
            }
        }
    }
}
