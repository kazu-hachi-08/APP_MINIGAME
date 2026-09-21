using System;
using UnityEngine;

namespace MiniGame.TableTennis
{
    /// <summary>
    /// NPCの思考担当。仕様通り「内部で返球結果を決め、ラケットの動きは見た目として表現する」ため、
    /// ここではコート座標と返球内容だけを扱い、表示・スイング演出は NpcRacketView に任せる。
    /// 難易度に関わる値はこのコンポーネントにまとめてあり、ここだけで強さを調整できる。
    /// </summary>
    public class NpcController : MonoBehaviour, ICourtActor
    {
        [Header("References")]
        [SerializeField] private TableLayout _table;
        [SerializeField] private BallMotion _ball;

        [Tooltip("空いているコースを狙うために参照するプレイヤーのラケット")]
        [SerializeField] private RacketController _playerRacket;

        [Header("Position (m)")]
        [Tooltip("NPCが打つ奥行き。台の奥端よりさらに奥に構える")]
        [SerializeField] private float _hitZ = 1.7f;

        [SerializeField] private float _homeX = 0f;
        [SerializeField] private float _rangeX = 1.1f;
        [SerializeField] private float _readyHeight = 0.25f;
        [SerializeField] private float _minHeight = 0f;
        [SerializeField] private float _maxHeight = 0.8f;

        [Header("難易度パラメータ")]
        [Tooltip("ラケットの移動速度 (m/s)。小さいほど左右に振られると追いつけない")]
        [SerializeField] private float _moveSpeed = 2.2f;

        [Tooltip("打点で当てられる左右のズレ (m)。小さいほどシビア")]
        [SerializeField] private float _reachX = 0.35f;

        [Tooltip("追い始めるまでの反応の遅れ (秒)")]
        [SerializeField] private float _reactionDelay = 0.12f;

        [Tooltip("追いつけていても返球をミスする確率")]
        [Range(0f, 1f)]
        [SerializeField] private float _missChance = 0.08f;

        [Tooltip("狙い点に乗る左右の誤差 (m)。大きいほどコースが甘くなる")]
        [SerializeField] private float _aimError = 0.18f;

        [Tooltip("プレイヤーがいない側を狙う度合い。0でランダム、1で常に逆サイド")]
        [Range(0f, 1f)]
        [SerializeField] private float _openCourtAim = 0.5f;

        [Header("返球")]
        [Tooltip("狙い点までの飛行時間 (秒)。短いほど速い球になる")]
        [SerializeField] private float _minFlightTime = 0.7f;
        [SerializeField] private float _maxFlightTime = 1.0f;

        [Tooltip("狙う奥行きの範囲。台の手前側（マイナス）で指定する")]
        [SerializeField] private float _targetDepthNear = -1.15f;
        [SerializeField] private float _targetDepthFar = -0.45f;

        [Tooltip("ランダムに散らす左右の幅 (m)")]
        [SerializeField] private float _targetSpreadX = 0.55f;

        [SerializeField] private float _minTopSpin = -0.3f;
        [SerializeField] private float _maxTopSpin = 0.7f;
        [SerializeField] private float _maxSideSpin = 0.4f;

        [Header("サーブ")]
        [SerializeField] private float _serveHeight = 0.35f;
        [SerializeField] private float _serveSpreadX = 0.5f;

        [Tooltip("サーブの回転を抑える倍率。ラリー中より素直な球にする")]
        [Range(0f, 1f)]
        [SerializeField] private float _serveSpinScale = 0.5f;

        [Header("軌道")]
        [Tooltip("ネット上端からどれだけ余裕を持って越えさせるか (m)")]
        [SerializeField] private float _netClearance = 0.08f;

        [Tooltip("ネットに掛かる軌道だったときに飛行時間を伸ばす倍率（山なりにする）")]
        [SerializeField] private float _netRetryTimeScale = 1.2f;

        [SerializeField] private int _netRetryCount = 3;

        [Tooltip("2バウンド目に入りそうな高さ。ここまで落ちたら打点を待たずに踏み込んで打つ")]
        [SerializeField] private float _stepInHeight = 0.12f;

        /// <summary>打球・移動の受け付け。ラリー外やポーズ中は false にする</summary>
        public bool IsActive { get; set; }

        public Vector3 CourtPosition => new Vector3(_x, _y, _hitZ);

        /// <summary>ラケットを振った（空振り含む）。スイング演出用</summary>
        public event Action OnSwing;

        /// <summary>実際に返球できた。ラリー判定とSE用</summary>
        public event Action OnReturned;

        private float _x;
        private float _y;

        /// <summary>自分のコートにバウンドし、返球するかどうかを判断済み</summary>
        private bool _incoming;

        private bool _willReturn;
        private bool _swung;
        private float _reactionTimer;

        private void Awake()
        {
            _x = _homeX;
            _y = _readyHeight;
        }

        private void OnEnable()
        {
            _ball.OnBounced += HandleBounced;
        }

        private void OnDisable()
        {
            _ball.OnBounced -= HandleBounced;
        }

        /// <summary>ラリーの区切りで状態を戻す</summary>
        public void ResetForRally()
        {
            _incoming = false;
            _willReturn = false;
            _swung = false;
            _reactionTimer = 0f;
        }

        /// <summary>NPCのサーブ。返球と同じ狙い方でプレイヤーコートへ送り出す</summary>
        public void Serve()
        {
            ResetForRally();

            _x = Mathf.Clamp(UnityEngine.Random.Range(-_serveSpreadX, _serveSpreadX), -_rangeX, _rangeX);
            _y = _readyHeight;

            var from = new Vector3(_x, _serveHeight, _hitZ);
            Vector2 spin = PickSpin() * _serveSpinScale;

            _ball.Launch(from, SolveShot(from, spin), spin);
            OnSwing?.Invoke();
        }

        /// <summary>
        /// 自分のコートにバウンドした時点で「返せるかどうか」を先に決める。
        /// 結果を先に確定させることで、難易度をこのクラスの値だけで調整できる。
        /// </summary>
        private void HandleBounced(Vector3 contact)
        {
            if (!IsActive || _incoming) return;
            if (contact.z <= 0f) return;
            if (_ball.Velocity.z <= 0f) return;

            _incoming = true;
            _swung = false;
            _reactionTimer = _reactionDelay;
            _willReturn = CanReach(contact) && UnityEngine.Random.value >= _missChance;
        }

        /// <summary>バウンド地点から打点までに、横移動が間に合うかどうか</summary>
        private bool CanReach(Vector3 contact)
        {
            float travelTime = (_hitZ - contact.z) / Mathf.Max(0.1f, _ball.Velocity.z);
            float predictedX = contact.x + _ball.Velocity.x * travelTime;
            float movableDistance = _moveSpeed * Mathf.Max(0f, travelTime - _reactionDelay) + _reachX;

            return Mathf.Abs(predictedX - _x) <= movableDistance;
        }

        private void Update()
        {
            if (!IsActive)
            {
                MoveTowards(_homeX, _readyHeight);
                return;
            }

            if (_reactionTimer > 0f)
            {
                _reactionTimer -= Time.deltaTime;
            }

            bool chasing = _incoming && _reactionTimer <= 0f && _ball.IsFlying;
            if (chasing)
            {
                Vector3 ball = _ball.CourtPosition;
                MoveTowards(ball.x, ball.y);
            }
            else
            {
                MoveTowards(_homeX, _readyHeight);
            }

            if (_incoming && _willReturn && !_swung && _ball.IsFlying && ShouldSwingNow())
            {
                Swing();
            }
        }

        private void MoveTowards(float targetX, float targetY)
        {
            float step = _moveSpeed * Time.deltaTime;
            _x = Mathf.MoveTowards(_x, Mathf.Clamp(targetX, -_rangeX, _rangeX), step);
            _y = Mathf.MoveTowards(_y, Mathf.Clamp(targetY, _minHeight, _maxHeight), step);
        }

        /// <summary>打点まで来たとき、または2バウンド目に入りそうなときに振る</summary>
        private bool ShouldSwingNow()
        {
            Vector3 ball = _ball.CourtPosition;
            if (ball.z >= _hitZ) return true;

            return _ball.Velocity.y < 0f && ball.y <= _stepInHeight;
        }

        private void Swing()
        {
            _swung = true;
            OnSwing?.Invoke();

            Vector3 ball = _ball.CourtPosition;

            // 追いつけていなければ空振り。2バウンドや打ち抜けとして RallyReferee が失点にする
            if (Mathf.Abs(ball.x - _x) > _reachX) return;

            Vector2 spin = PickSpin();
            _ball.Launch(ball, SolveShot(ball, spin), spin);
            OnReturned?.Invoke();
        }

        /// <summary>狙い点へ落とす初速を求める。ネットに掛かる軌道なら山なりにして取り直す</summary>
        private Vector3 SolveShot(Vector3 from, Vector2 spin)
        {
            Vector3 target = PickTarget();
            float flightTime = UnityEngine.Random.Range(_minFlightTime, _maxFlightTime);
            Vector3 velocity = _ball.SolveLaunchVelocity(from, target, flightTime, spin);

            for (int i = 0; i < _netRetryCount; i++)
            {
                if (_ball.PredictHeightAt(from, velocity, spin, 0f) >= _table.NetHeight + _netClearance)
                {
                    break;
                }

                flightTime *= _netRetryTimeScale;
                velocity = _ball.SolveLaunchVelocity(from, target, flightTime, spin);
            }

            return velocity;
        }

        /// <summary>プレイヤーコート上の狙い点</summary>
        private Vector3 PickTarget()
        {
            float random = UnityEngine.Random.Range(-_targetSpreadX, _targetSpreadX);
            float open = -Mathf.Sign(_playerRacket.CourtPosition.x) * _table.HalfWidth * 0.6f;

            float x = Mathf.Lerp(random, open, _openCourtAim)
                    + UnityEngine.Random.Range(-_aimError, _aimError);

            // 台外は狙わない（NPCのミスは「追いつけない」「確率ミス」で表現する）
            float limit = _table.HalfWidth * 0.9f;
            return new Vector3(
                Mathf.Clamp(x, -limit, limit),
                0f,
                UnityEngine.Random.Range(_targetDepthNear, _targetDepthFar));
        }

        private Vector2 PickSpin()
        {
            return new Vector2(
                UnityEngine.Random.Range(-_maxSideSpin, _maxSideSpin),
                UnityEngine.Random.Range(_minTopSpin, _maxTopSpin));
        }
    }
}
