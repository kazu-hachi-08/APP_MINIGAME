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
        // 各方向 0 = 待機 / 1, 2 = 走り
        [SerializeField] private Sprite[] _downFrames = new Sprite[3];
        [SerializeField] private Sprite[] _upFrames = new Sprite[3];
        [SerializeField] private Sprite[] _sideFrames = new Sprite[3];

        [SerializeField] private float _movingSpeedThreshold = 0.2f;
        [SerializeField] private float _frameInterval = 0.12f;

        private Rigidbody2D _rigidbody;
        private SpriteRenderer _renderer;
        private Sprite[] _currentFrames;
        private float _frameTimer;
        private int _runFrame;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            _renderer = GetComponent<SpriteRenderer>();
            _currentFrames = _downFrames;
        }

        private void Update()
        {
            Vector2 velocity = _rigidbody.linearVelocity;
            bool isMoving = velocity.sqrMagnitude > _movingSpeedThreshold * _movingSpeedThreshold;

            if (isMoving)
            {
                UpdateDirection(velocity);
                AdvanceRunFrame();
            }
            else
            {
                // 止まったら必ず待機コマへ戻す
                _frameTimer = 0f;
                _runFrame = 0;
            }

            Sprite sprite = isMoving ? _currentFrames[1 + _runFrame] : _currentFrames[0];
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
                _runFrame = 1 - _runFrame;
            }
        }
    }
}
