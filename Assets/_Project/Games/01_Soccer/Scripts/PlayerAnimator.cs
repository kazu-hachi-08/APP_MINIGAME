using UnityEngine;

namespace MiniGame.Soccer
{
    /// <summary>
    /// 選手の簡易アニメーション
    /// 仮素材が1枚スプライトのためアニメーションクリップは用意せず、
    /// 移動中だけ縦横に伸縮させて走っている印象を出す
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerAnimator : MonoBehaviour
    {
        [SerializeField] private float _movingSpeedThreshold = 0.2f;
        [SerializeField] private float _bobFrequency = 12f;
        [SerializeField] private float _bobAmplitude = 0.1f;

        private Rigidbody2D _rigidbody;
        private Vector3 _baseScale;
        private float _bobTime;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            _baseScale = transform.localScale;
        }

        private void Update()
        {
            bool isMoving = _rigidbody.linearVelocity.sqrMagnitude > _movingSpeedThreshold * _movingSpeedThreshold;

            if (isMoving)
            {
                _bobTime += Time.deltaTime * _bobFrequency;
            }
            else
            {
                // 止まったら位相をリセットし、必ず等倍へ戻す
                _bobTime = 0f;
            }

            float bob = Mathf.Sin(_bobTime) * _bobAmplitude;
            transform.localScale = new Vector3(
                _baseScale.x * (1f - bob),
                _baseScale.y * (1f + bob),
                _baseScale.z);
        }
    }
}
