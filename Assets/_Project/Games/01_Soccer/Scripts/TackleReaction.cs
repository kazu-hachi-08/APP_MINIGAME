using UnityEngine;

namespace MiniGame.Soccer
{
    /// <summary>
    /// タックルで倒された際の「よろけ」演出と一時的な行動不能を管理する。
    /// 転倒用のドット絵は用意せず、既存スプライトのTransformを回転・圧縮するだけで
    /// 「倒れた」ことを表現する（4.3節の「選手は回転させない」は向き表現の話であり、
    /// ここではあえて横倒しに見える副作用を演出として利用している）。
    /// </summary>
    public class TackleReaction : MonoBehaviour
    {
        [Header("Stagger")]
        [SerializeField] private float _staggerDuration = 0.5f;
        [SerializeField] private float _fallenRotationZ = 90f;
        [SerializeField] private float _fallenScaleY = 0.5f;

        private PlayerController _playerController;
        private AIPlayerController _aiController;
        private Vector3 _originalScale;
        private float _timer;
        private bool _isStaggered;

        /// <summary>オンライン対戦でゲスト端末にも転倒を見せるため、状態を外から読めるようにする</summary>
        public bool IsStaggered => _isStaggered;

        private void Awake()
        {
            _playerController = GetComponent<PlayerController>();
            _aiController = GetComponent<AIPlayerController>();
            _originalScale = transform.localScale;
        }

        private void Update()
        {
            if (!_isStaggered) return;

            _timer -= Time.deltaTime;
            if (_timer <= 0f)
            {
                EndStagger();
            }
        }

        /// <summary>
        /// タックルを受けた際に外部（SlidingTackle）から呼ぶ。よろけ演出と一時的な行動不能を開始する
        /// </summary>
        public void Stagger()
        {
            if (_isStaggered) return; // タックル中に多重発火させない

            _isStaggered = true;
            _timer = _staggerDuration;

            SetMovementSuppressed(true);

            transform.localRotation = Quaternion.Euler(0f, 0f, _fallenRotationZ);
            transform.localScale = new Vector3(_originalScale.x, _originalScale.y * _fallenScaleY, _originalScale.z);
        }

        private void EndStagger()
        {
            _isStaggered = false;
            SetMovementSuppressed(false);

            transform.localRotation = Quaternion.identity;
            transform.localScale = _originalScale;
        }

        private void SetMovementSuppressed(bool suppressed)
        {
            if (_playerController != null) _playerController.SetMovementSuppressed(suppressed);
            if (_aiController != null) _aiController.SetMovementSuppressed(suppressed);
        }
    }
}
