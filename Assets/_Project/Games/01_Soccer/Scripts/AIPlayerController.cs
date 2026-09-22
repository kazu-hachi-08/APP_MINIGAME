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
        [SerializeField] private float _moveSpeed = 4.5f;
        [SerializeField] private float _arriveThreshold = 0.2f;

        [Header("Ball Awareness")]
        [SerializeField] private float _ballChaseRadius = 5.5f;

        [Header("Attack / Defense")]
        [SerializeField] private float _attackShiftDistance = 2.3f;

        [Header("Ball Follow Weight (non-chaser players)")]
        [SerializeField] private float _minFollowWeight = 0.15f; // 自陣寄りの選手の追従割合
        [SerializeField] private float _maxFollowWeight = 0.5f;  // 前線の選手の追従割合

        [Header("Goalkeeper")]
        [SerializeField] private float _gkOwnGoalThreshold = 1.5f; // 基準ポジションがこの距離以内なら自陣ゴール前＝GKとみなす
        [SerializeField] private float _gkLateralRange = 1.6f;     // GKがボールに合わせて動ける範囲（基準ポジション中心）

        [Header("Kick")]
        [SerializeField] private float _opponentGoalX;
        [SerializeField] private float _kickRadius = 0.6f;
        [SerializeField] private float _shootRange = 7f;
        [SerializeField] private float _passRange = 9f;
        [SerializeField] private float _passAdvantageMargin = 1.5f;
        [SerializeField] private float _passSpeed = 9f;
        [SerializeField] private float _shootSpeed = 17f;
        [SerializeField] private float _kickCooldown = 1.0f;

        private Rigidbody2D _rigidbody;
        private TeamMember _teamMember;
        private Ball _ball;
        private SoccerGameManager _gameManager;
        private float _kickCooldownTimer;
        private bool _isGoalkeeper;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            _teamMember = GetComponent<TeamMember>();
        }

        private void Start()
        {
            _ball = Object.FindFirstObjectByType<Ball>();
            _gameManager = Object.FindFirstObjectByType<SoccerGameManager>();

            // GK判定用の専用フラグはシーン側に持たせず、基準ポジションと自陣ゴールの距離から都度導出する。
            // こうすることでシーン再生成（Build Soccer Scene）に依存せず、既存シーンのままでもGK専用挙動が有効になる。
            float ownGoalX = -_opponentGoalX;
            _isGoalkeeper = Mathf.Abs(_homePosition.x - ownGoalX) <= _gkOwnGoalThreshold;
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
                _rigidbody.linearVelocity = toTarget.normalized * _moveSpeed;
            }

            TryKickBall();
        }

        private Vector2 ComputeTargetPosition()
        {
            // キックオフ演出中やゴール演出中は誰もボールに寄らず、フォーメーションを維持する
            // （全員がボールへ殺到してスクラム状態になり、プレイヤーが触れなくなるのを防ぐ）
            bool canChaseBall = _ball != null && _gameManager != null && _gameManager.IsPlaying;

            if (_isGoalkeeper)
            {
                return ComputeGoalkeeperTarget(canChaseBall);
            }

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
            if (canChaseBall && _teamMember != null)
            {
                if (_ball.Position.x * _teamMember.AttackDirection > 0f)
                {
                    basePosition += Vector2.right * (_teamMember.AttackDirection * _attackShiftDistance);
                }

                // ボールを追いかける1人以外も、役割（前線ほど強め）に応じてボール方向へ少しずつ引っ張られる。
                // これにより「ボールに一番近い1人以外は棒立ち」に見えていた見た目を解消する
                basePosition += (_ball.Position - basePosition) * ComputeFollowWeight();
            }

            return basePosition;
        }

        /// <summary>
        /// 基準ポジションが自陣ゴールに近いほど大きい重みを返す前提で、
        /// 前線の選手ほどボールへ強く反応し、自陣寄りの選手は大きく崩れないようにする
        /// </summary>
        private float ComputeFollowWeight()
        {
            float fieldHalfWidth = Mathf.Abs(_opponentGoalX);
            if (fieldHalfWidth <= 0f) return _minFollowWeight;

            float attackDirection = _teamMember != null ? _teamMember.AttackDirection : 1f;
            float advancement = _homePosition.x * attackDirection; // 自陣ゴール寄り=負、相手ゴール寄り=正
            float normalizedAdvancement = Mathf.InverseLerp(-fieldHalfWidth, fieldHalfWidth, advancement);

            return Mathf.Lerp(_minFollowWeight, _maxFollowWeight, normalizedAdvancement);
        }

        /// <summary>
        /// GKは自陣ゴールライン上のX座標を維持し、ボールのYに合わせて可動範囲内だけ左右（上下）に動く
        /// </summary>
        private Vector2 ComputeGoalkeeperTarget(bool canChaseBall)
        {
            if (!canChaseBall)
            {
                return _homePosition;
            }

            float clampedY = Mathf.Clamp(_ball.Position.y, _homePosition.y - _gkLateralRange, _homePosition.y + _gkLateralRange);
            return new Vector2(_homePosition.x, clampedY);
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
    }
}
