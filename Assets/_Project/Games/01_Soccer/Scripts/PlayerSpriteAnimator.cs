using UnityEngine;

namespace MiniGame.Soccer
{
    /// <summary>
    /// 人型スプライトの向きと走りアニメーションを制御する。
    /// 1枚絵を回転させると人が横倒しになってしまうため、向きは方向別スプライトで表現する
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(SpriteRenderer))]
    public class PlayerSpriteAnimator : MonoBehaviour
    {
        // 各方向 0 = 待機 / 1, 2 = 走り（走りは2コマを交互に出す）
        [SerializeField] private Sprite[] _downFrames = new Sprite[3];
        [SerializeField] private Sprite[] _upFrames = new Sprite[3];
        [SerializeField] private Sprite[] _sideFrames = new Sprite[3];

        [SerializeField] private float _movingSpeedThreshold = 0.2f;
        [SerializeField] private float _frameInterval = 0.12f;

        private const int IdleFrameIndex = 0;
        private const int FirstRunFrameIndex = 1;

        private Rigidbody2D _rigidbody;
        private SpriteRenderer _renderer;
        private Sprite[] _currentFrames;
        private float _frameTimer;
        private int _runFrameIndex;  // 走り2コマのどちらを出しているか（0 / 1）

        // オンライン対戦のゲスト端末は物理を動かさないため、Rigidbodyの速度の代わりに届いた速度で向きとコマを決める
        private bool _useExternalVelocity;
        private Vector2 _externalVelocity;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            _renderer = GetComponent<SpriteRenderer>();
            _currentFrames = _downFrames;
        }

        public void SetExternalVelocity(Vector2 velocity)
        {
            _useExternalVelocity = true;
            _externalVelocity = velocity;
        }

        private void Update()
        {
            Vector2 velocity = _useExternalVelocity ? _externalVelocity : _rigidbody.linearVelocity;
            bool isMoving = velocity.sqrMagnitude > _movingSpeedThreshold * _movingSpeedThreshold;

            if (isMoving)
            {
                UpdateDirection(velocity);
                AdvanceRunFrame();
            }
            else
            {
                ResetRunFrame();
            }

            ApplySprite(isMoving);
        }

        /// <summary>止まったら必ず待機コマへ戻し、次に走り出したときも1コマ目から始める</summary>
        private void ResetRunFrame()
        {
            _frameTimer = 0f;
            _runFrameIndex = 0;
        }

        private void ApplySprite(bool isMoving)
        {
            Sprite sprite = isMoving ? _currentFrames[FirstRunFrameIndex + _runFrameIndex] : _currentFrames[IdleFrameIndex];
            if (sprite != null)
            {
                _renderer.sprite = sprite;
            }
        }

        private void UpdateDirection(Vector2 velocity)
        {
            if (Mathf.Abs(velocity.x) > Mathf.Abs(velocity.y))
            {
                // 横向きは右向きで用意しているため、左向きは反転で済ませる
                _currentFrames = _sideFrames;
                _renderer.flipX = velocity.x < 0f;
            }
            else
            {
                _currentFrames = velocity.y > 0f ? _upFrames : _downFrames;
                _renderer.flipX = false;
            }
        }

        private void AdvanceRunFrame()
        {
            _frameTimer += Time.deltaTime;
            while (_frameTimer >= _frameInterval)
            {
                _frameTimer -= _frameInterval;
                _runFrameIndex = 1 - _runFrameIndex;
            }
        }
    }
}
