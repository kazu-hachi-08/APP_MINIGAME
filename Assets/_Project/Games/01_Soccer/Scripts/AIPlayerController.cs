using UnityEngine;

namespace MiniGame.Soccer
{
    /// <summary>
    /// NPC選手の簡易AI（ポジション維持・ボール追従・簡易攻守シフト・パス/シュート）
    /// 高度な戦術AI（マークやフォーメーション連携等）は、週末開発で作り切れる範囲を優先して入れていない
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class AIPlayerController : MonoBehaviour
    {
        // シュートが毎回ゴール中央へ飛ぶと単調なので、ゴール幅の内側で上下にばらつかせる
        private const float ShotSpreadY = 0.8f;

        [Header("Formation")]
        [SerializeField] private Vector2 _homePosition;

        [Header("Movement")]
        [SerializeField] private float _moveSpeed = 3.8f;
        [SerializeField] private float _arriveThreshold = 0.2f;

        [Header("Ball Awareness")]
        [SerializeField] private float _ballChaseRadius = 8f;

        [Header("Attack / Defense")]
        [SerializeField] private float _attackShiftDistance = 3.5f;

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
        [SerializeField] private float _passSpeed = 7f;
        [SerializeField] private float _shootSpeed = 12f;
        [SerializeField] private float _kickCooldown = 1.0f;

        private Rigidbody2D _rigidbody;
        private TeamMember _teamMember;
        private Ball _ball;
        private SoccerGameManager _gameManager;
        private float _kickCooldownTimer;
        private bool _isGoalkeeper;
        // タックルでよろけている間はTackleReactionが移動・キックを止める
        private bool _movementSuppressed;

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
            // こうすることでシーン再生成（Rebuild Soccer）に依存せず、既存シーンのままでもGK専用挙動が有効になる。
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
        /// タックルでよろけている間など、外部コンポーネントが移動を止めたい間だけ抑制する
        /// </summary>
        public void SetMovementSuppressed(bool suppressed)
        {
            _movementSuppressed = suppressed;
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
            TickKickCooldown();

            if (_movementSuppressed)
            {
                _rigidbody.linearVelocity = Vector2.zero;
                return;
            }

            MoveToward(ComputeTargetPosition());
            TryKickBall();
        }

        private void TickKickCooldown()
        {
            if (_kickCooldownTimer > 0f)
            {
                _kickCooldownTimer -= Time.fixedDeltaTime;
            }
        }

        private void MoveToward(Vector2 targetPosition)
        {
            Vector2 toTarget = targetPosition - _rigidbody.position;

            if (toTarget.magnitude <= _arriveThreshold)
            {
                _rigidbody.linearVelocity = Vector2.zero;
            }
            else
            {
                _rigidbody.linearVelocity = toTarget.normalized * _moveSpeed;
            }
        }

        /// <summary>
        /// キックオフ演出中やゴール演出中は誰もボールに寄らず、フォーメーションを維持させるための判定
        /// （全員がボールへ殺到してスクラム状態になり、プレイヤーが触れなくなるのを防ぐ）
        /// </summary>
        private bool IsBallInPlay()
        {
            return _ball != null && _gameManager != null && _gameManager.IsPlaying;
        }

        private Vector2 ComputeTargetPosition()
        {
            bool canChaseBall = IsBallInPlay();

            if (_isGoalkeeper)
            {
                return ComputeGoalkeeperTarget(canChaseBall);
            }

            if (canChaseBall && ShouldChaseBall())
            {
                return _ball.Position;
            }

            return ComputeFormationPosition(canChaseBall);
        }

        /// <summary>
        /// 追いかけ圏内にいて、かつ味方の中で最もボールに近い1人だけがボールへ向かう
        /// </summary>
        private bool ShouldChaseBall()
        {
            float myDistance = Vector2.Distance(_rigidbody.position, _ball.Position);
            return myDistance <= _ballChaseRadius && IsClosestTeammateToBall(myDistance);
        }

        private Vector2 ComputeFormationPosition(bool canChaseBall)
        {
            Vector2 basePosition = _homePosition;
            if (!canChaseBall || _teamMember == null) return basePosition;

            // 自チームが攻めている（ボールが攻撃方向側にある）間は基準位置を少し前へシフトする
            if (_ball.Position.x * _teamMember.AttackDirection > 0f)
            {
                basePosition += Vector2.right * (_teamMember.AttackDirection * _attackShiftDistance);
            }

            // ボールを追いかける1人以外も、役割（前線ほど強め）に応じてボール方向へ少しずつ引っ張る。
            // こうしないと「ボールに一番近い1人以外は棒立ち」に見えてしまう
            basePosition += (_ball.Position - basePosition) * ComputeFollowWeight();
            return basePosition;
        }

        /// <summary>
        /// 前線の選手ほどボールへ強く反応させ、自陣寄りの選手は陣形を大きく崩さないようにするため、
        /// 基準ポジションが相手ゴール寄りなほど大きい重みを返す
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
                if (!IsTeammate(member)) continue;

                float otherDistance = Vector2.Distance(member.transform.position, _ball.Position);
                if (otherDistance < myDistance) return false;
            }

            return true;
        }

        /// <summary>
        /// 自チームの他の選手か（自分自身は含まない）
        /// </summary>
        private bool IsTeammate(TeamMember member)
        {
            return member != _teamMember && member.Team == _teamMember.Team;
        }

        /// <summary>
        /// ボールを保持している（最も近く、キック圏内にいる）間、適切な場面でパス・シュートを行う。
        /// 選択肢が無ければキックせず、そのままドリブル（追従）を続ける
        /// </summary>
        private void TryKickBall()
        {
            if (!IsBallInPlay()) return;
            if (_kickCooldownTimer > 0f) return;
            // 保持中のボールを蹴れると近づいただけで奪えてしまうため、奪取はPlayerController側の接触時間判定に任せる
            if (_ball.IsHeld) return;

            float myDistance = Vector2.Distance(_rigidbody.position, _ball.Position);
            if (myDistance > _kickRadius) return;
            if (!IsClosestTeammateToBall(myDistance)) return;
            if (!TryChooseKick(out Vector2 direction, out float speed)) return;

            _ball.Kick(direction, speed);
            _kickCooldownTimer = _kickCooldown;
        }

        /// <summary>
        /// シュートを優先し、打てなければ前方の味方へのパスを選ぶ
        /// </summary>
        private bool TryChooseKick(out Vector2 direction, out float speed)
        {
            if (TryFindShotOpportunity(out Vector2 shotTarget))
            {
                direction = shotTarget - _ball.Position;
                speed = _shootSpeed;
                return true;
            }

            if (TryFindPassTarget(out Vector2 passTarget))
            {
                direction = passTarget - _ball.Position;
                speed = _passSpeed;
                return true;
            }

            direction = Vector2.zero;
            speed = 0f;
            return false;
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
                shotTarget = goalCenter + new Vector2(0f, Random.Range(-ShotSpreadY, ShotSpreadY));
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
                if (!IsTeammate(member)) continue;

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
