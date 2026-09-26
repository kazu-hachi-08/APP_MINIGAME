using System;
using UnityEngine;

namespace MiniGame.TableTennis
{
    /// <summary>
    /// ラリー中にときどき台の上へ現れ、ボールが当たると弱必殺技を渡すキャラクター。
    /// 当たり判定はボールのコート座標との距離だけで行い、物理演算は使わない（ボール自体も物理を使っていないため）。
    /// 打球の軌道は変えず、当たったら消えるだけにして、ラリーの判定に影響させない。
    /// </summary>
    public class TableMascot : MonoBehaviour
    {
        [SerializeField] private TableLayout _table;
        [SerializeField] private BallMotion _ball;
        [SerializeField] private SpriteRenderer _renderer;

        [Tooltip("出現するたびにランダムで選ぶ見た目")]
        [SerializeField] private Sprite[] _sprites;

        [Header("出現")]
        [Tooltip("前回消えてから次に出るまでの時間（秒）。ラリー中だけ数える")]
        [SerializeField] private float _minInterval = 5f;
        [SerializeField] private float _maxInterval = 10f;

        [Tooltip("台の上にいる時間（秒）")]
        [SerializeField] private float _lifetime = 4f;

        [Tooltip("台の縁やネット際に出ると狙いようがないため、そこから離す距離 (m)")]
        [SerializeField] private float _edgeMargin = 0.15f;
        [SerializeField] private float _netMargin = 0.35f;

        [Header("当たり判定 (m)")]
        [Tooltip("台上での左右・奥行きの当たり半径。ボールは狙い点へ落ちるので、狙って当てられる広さにする")]
        [SerializeField] private float _hitRadius = 0.22f;

        [Tooltip("この高さより下を通ったボールだけ当たる（キャラの背の高さ）")]
        [SerializeField] private float _hitHeight = 0.3f;

        [Header("表示")]
        [Tooltip("手前端にいるときの表示身長（ワールド単位）")]
        [SerializeField] private float _displayHeightAtNear = 0.9f;

        /// <summary>当てた側（打った側）を通知する</summary>
        public event Action<CourtSide> OnHit;

        /// <summary>出現してよいか。ラリー中だけ true にする</summary>
        public bool CanAppear { get; set; }

        private float _spawnTimer;
        private float _lifeTimer;
        private bool _visible;
        private Vector3 _courtPosition;

        private void Awake()
        {
            ResetSpawnTimer();
            Hide();
        }

        private void Update()
        {
            if (!CanAppear)
            {
                if (_visible) Hide();
                return;
            }

            if (_visible)
            {
                UpdateVisible();
                return;
            }

            _spawnTimer -= Time.deltaTime;
            if (_spawnTimer <= 0f)
            {
                Appear();
            }
        }

        private void UpdateVisible()
        {
            _lifeTimer -= Time.deltaTime;
            if (_lifeTimer <= 0f)
            {
                Hide();
                return;
            }

            if (_ball.IsFlying && IsBallTouching())
            {
                // 相手コート側へ向かう球はプレイヤーが打った球
                CourtSide hitter = _ball.Velocity.z > 0f ? CourtSide.Player : CourtSide.Opponent;
                Hide();
                OnHit?.Invoke(hitter);
            }
        }

        private bool IsBallTouching()
        {
            Vector3 ball = _ball.CourtPosition;
            if (ball.y > _hitHeight) return false;

            var offset = new Vector2(ball.x - _courtPosition.x, ball.z - _courtPosition.z);
            return offset.sqrMagnitude <= _hitRadius * _hitRadius;
        }

        /// <summary>両コートのどちらかにランダムで出す。どちらの側も当てるチャンスがあるようにする</summary>
        private void Appear()
        {
            float halfX = _table.HalfWidth - _edgeMargin;
            float depth = UnityEngine.Random.Range(_netMargin, _table.HalfLength - _edgeMargin);
            float side = UnityEngine.Random.value < 0.5f ? 1f : -1f;
            _courtPosition = new Vector3(UnityEngine.Random.Range(-halfX, halfX), 0f, depth * side);

            if (_sprites != null && _sprites.Length > 0)
            {
                _renderer.sprite = _sprites[UnityEngine.Random.Range(0, _sprites.Length)];
            }

            ApplyView();
            _renderer.enabled = true;
            _visible = true;
            _lifeTimer = _lifetime;
        }

        private void Hide()
        {
            _renderer.enabled = false;
            _visible = false;
            ResetSpawnTimer();
        }

        private void ResetSpawnTimer()
        {
            _spawnTimer = UnityEngine.Random.Range(_minInterval, _maxInterval);
        }

        /// <summary>足元を台上の位置に合わせ、奥ほど小さく見せる</summary>
        private void ApplyView()
        {
            if (_renderer.sprite == null) return;

            float depthScale = _table.ScaleAt(_courtPosition.z) / _table.NearScale;
            float spriteHeight = Mathf.Max(0.0001f, _renderer.sprite.bounds.size.y);
            float scale = _displayHeightAtNear * depthScale / spriteHeight;

            transform.position = _table.Project(_courtPosition);
            transform.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
