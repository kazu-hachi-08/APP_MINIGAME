using UnityEngine;

namespace MiniGame.Soccer
{
    /// <summary>
    /// NPC選手の簡易AI（ポジション維持・ボール追従・簡易攻守シフト・パス/シュート）
    /// 高度な戦術AI（マークやフォーメーション連携等）はMVP対象外のため実装しない
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class AIPlayerController : MonoBehaviour
    {
        [Header("Formation")]
        [SerializeField] private Vector2 _homePosition;

        [Header("Movement")]
        [SerializeField] private float _moveSpeed = 3f;
        [SerializeField] private float _rotationSpeed = 540f;
        [SerializeField] private float _arriveThreshold = 0.2f;

        [Header("Ball Awareness")]
        [SerializeField] private float _ballChaseRadius = 3.5f;

        [Header("Attack / Defense")]
        [SerializeField] private float _attackShiftDistance = 1.5f;

        [Header("Kick")]
        [SerializeField] private float _opponentGoalX;
        [SerializeField] private float _kickRadius = 0.6f;
        [SerializeField] private float _shootRange = 4.5f;
        [SerializeField] private float _passRange = 6f;
        [SerializeField] private float _passAdvantageMargin = 1.5f;
        [SerializeField] private float _passSpeed = 6f;
        [SerializeField] private float _shootSpeed = 12f;
        [SerializeField] private float _kickCooldown = 1.0f;

        private Rigidbody2D _rigidbody;
        private TeamMember _teamMember;
        private Ball _ball;
        private SoccerGameManager _gameManager;
        private float _kickCooldownTimer;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            _teamMember = GetComponent<TeamMember>();
        }

        private void Start()
        {
            _ball = Object.FindFirstObjectByType<Ball>();
            _gameManager = Object.FindFirstObjectByType<SoccerGameManager>();
        }

        public void SetHomePosition(Vector2 position)
        {
            _homePosition = position;
        }

        /// <summary>
        /// 相手ゴールのX座標を設定する（シュート判断に使用）
        /// </summary>
        public void SetOpponentGoalX(float goalX)
        {
            _opponentGoalX = goalX;
        }

        /// <summary>
        /// ゴール後などに基準ポジションへ戻す
        /// </summary>
        public void ResetToHomePosition()
        {
            _rigidbody.linearVelocity = Vector2.zero;
            _rigidbody.position = _homePosition;
        }

        private void FixedUpdate()
        {
            if (_kickCooldownTimer > 0f)
            {
                _kickCooldownTimer -= Time.fixedDeltaTime;
            }

            Vector2 targetPosition = ComputeTargetPosition();
            Vector2 toTarget = targetPosition - _rigidbody.position;

            if (toTarget.magnitude <= _arriveThreshold)
            {
                _rigidbody.linearVelocity = Vector2.zero;
            }
            else
            {
                Vector2 moveDirection = toTarget.normalized;
                _rigidbody.linearVelocity = moveDirection * _moveSpeed;
                RotateTowards(moveDirection);
            }

            TryKickBall();
        }

        private Vector2 ComputeTargetPosition()
        {
            // キックオフ演出中やゴール演出中は誰もボールに寄らず、フォーメーションを維持する
            // （全員がボールへ殺到してスクラム状態になり、プレイヤーが触れなくなるのを防ぐ）
            bool canChaseBall = _ball != null && _gameManager != null && _gameManager.IsPlaying;

            if (canChaseBall)
            {
                float myDistance = Vector2.Distance(_rigidbody.position, _ball.Position);
                if (myDistance <= _ballChaseRadius && IsClosestTeammateToBall(myDistance))
                {
                    return _ball.Position;
                }
            }

            // 自チームが攻めている（ボールが攻撃方向側にある）間は基準位置を少し前へシフトする
            Vector2 basePosition = _homePosition;
            if (canChaseBall && _teamMember != null && _ball.Position.x * _teamMember.AttackDirection > 0f)
            {
                basePosition += Vector2.right * (_teamMember.AttackDirection * _attackShiftDistance);
            }

            return basePosition;
        }

        /// <summary>
        /// 自チームの中で自分が最もボールに近いか判定する（近い1人だけがボールへ向かい、他は定位置を維持する）
        /// </summary>
        private bool IsClosestTeammateToBall(float myDistance)
        {
            if (_teamMember == null) return true;

            var allMembers = Object.FindObjectsByType<TeamMember>(FindObjectsSortMode.None);
            foreach (var member in allMembers)
            {
                if (member == _teamMember || member.Team != _teamMember.Team) continue;

                float otherDistance = Vector2.Distance(member.transform.position, _ball.Position);
                if (otherDistance < myDistance) return false;
            }

            return true;
        }

        /// <summary>
        /// ボールを保持している（最も近く、キック圏内にいる）間、適切な場面でパス・シュートを行う。
        /// 選択肢が無ければキックせず、そのままドリブル（追従）を続ける
        /// </summary>
        private void TryKickBall()
        {
            if (_ball == null || _gameManager == null || !_gameManager.IsPlaying) return;
            if (_kickCooldownTimer > 0f) return;

            float myDistance = Vector2.Distance(_rigidbody.position, _ball.Position);
            if (myDistance > _kickRadius) return;
            if (!IsClosestTeammateToBall(myDistance)) return;

            Vector2 direction;
            float speed;

            if (TryFindShotOpportunity(out Vector2 shotTarget))
            {
                direction = shotTarget - _ball.Position;
                speed = _shootSpeed;
            }
            else if (TryFindPassTarget(out Vector2 passTarget))
            {
                direction = passTarget - _ball.Position;
                speed = _passSpeed;
            }
            else
            {
                return;
            }

            _ball.Kick(direction, speed);
            _kickCooldownTimer = _kickCooldown;
        }

        /// <summary>
        /// 相手ゴールに十分近ければシュートする
        /// </summary>
        private bool TryFindShotOpportunity(out Vector2 shotTarget)
        {
            Vector2 goalCenter = new Vector2(_opponentGoalX, 0f);
            float distanceToGoal = Vector2.Distance(_rigidbody.position, goalCenter);

            if (distanceToGoal <= _shootRange)
            {
                shotTarget = goalCenter + new Vector2(0f, Random.Range(-0.8f, 0.8f));
                return true;
            }

            shotTarget = Vector2.zero;
            return false;
        }

        /// <summary>
        /// パス圏内に、自分より明確に前方（攻撃方向側）にいる味方がいればそこへパスする
        /// </summary>
        private bool TryFindPassTarget(out Vector2 passTarget)
        {
            passTarget = Vector2.zero;
            if (_teamMember == null) return false;

            float bestAdvancement = _rigidbody.position.x * _teamMember.AttackDirection + _passAdvantageMargin;
            bool found = false;

            var allMembers = Object.FindObjectsByType<TeamMember>(FindObjectsSortMode.None);
            foreach (var member in allMembers)
            {
                if (member == _teamMember || member.Team != _teamMember.Team) continue;

                Vector2 teammatePosition = member.transform.position;
                if (Vector2.Distance(_rigidbody.position, teammatePosition) > _passRange) continue;

                float advancement = teammatePosition.x * _teamMember.AttackDirection;
                if (advancement > bestAdvancement)
                {
                    bestAdvancement = advancement;
                    passTarget = teammatePosition;
                    found = true;
                }
            }

            return found;
        }

        private void RotateTowards(Vector2 direction)
        {
            // 矢印スプライトの正面補正（PlayerControllerと同じ値）
            float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + 90f;
            float newAngle = Mathf.MoveTowardsAngle(_rigidbody.rotation, targetAngle, _rotationSpeed * Time.fixedDeltaTime);
            _rigidbody.MoveRotation(newAngle);
        }
    }
}
