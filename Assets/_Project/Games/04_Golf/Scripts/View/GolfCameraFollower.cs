using UnityEngine;

namespace MiniGame.Golf
{
    /// <summary>
    /// カメラの追従（§4.3）。狙っている間はボールと着地予測の両方が入るように引き、飛んでいる間はボールを追う。
    /// 高さでずれるボール本体ではなく地面の位置を追うことで、飛んでいる間もカメラが上下に揺れないようにする。
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class GolfCameraFollower : MonoBehaviour
    {
        [SerializeField] private GolfBall _ball;
        [SerializeField] private AimGuideView _aimGuide;

        [Tooltip("ボールに追いつくまでのおおよその時間（秒）。0だと完全に張り付く")]
        [SerializeField] private float _smoothTime = 0.15f;

        [Tooltip("ボールより奥（画面の上）を見せる量。打つ先が見えるようにする")]
        [SerializeField] private float _lookAhead = 2f;

        [Tooltip("狙っている間、ボールと着地予測の外側に空ける余白（ユニット）")]
        [SerializeField] private float _aimMargin = 1.5f;

        [Tooltip("画面の下からこの割合はゲージなどのUIに隠れる。狙いの表示はその上に収める")]
        [SerializeField] private float _bottomUiRatio = 0.18f;

        [Tooltip("画面の上からこの割合はHUDに隠れる")]
        [SerializeField] private float _topUiRatio = 0.2f;

        private Camera _camera;
        private float _baseSize;
        private Vector3 _velocity;
        private float _sizeVelocity;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            _baseSize = _camera.orthographicSize;
        }

        private void Start()
        {
            // 初回はなめらかに寄せず、最初からボールを映す
            transform.position = FollowPosition();
        }

        private void LateUpdate()
        {
            float targetSize = _baseSize;
            Vector3 targetPosition;

            if (_aimGuide.IsShowing)
            {
                targetSize = Mathf.Max(_baseSize, AimSize());
                targetPosition = AimPosition(targetSize);
            }
            else
            {
                targetPosition = FollowPosition();
            }

            _camera.orthographicSize = Mathf.SmoothDamp(_camera.orthographicSize, targetSize, ref _sizeVelocity, _smoothTime);
            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref _velocity, _smoothTime);
        }

        private Vector3 FollowPosition()
        {
            Vector2 ground = _ball.GroundPosition;
            return new Vector3(ground.x, ground.y + _lookAhead, transform.position.z);
        }

        /// <summary>ボールと着地予測が、上下のUIに隠れない帯と画面の横幅に収まる大きさ</summary>
        private float AimSize()
        {
            Vector2 span = _aimGuide.TargetPoint - _ball.GroundPosition;
            float visibleRatio = 1f - _bottomUiRatio - _topUiRatio;
            float sizeForHeight = (Mathf.Abs(span.y) + _aimMargin * 2f) / (2f * visibleRatio);
            float sizeForWidth = (Mathf.Abs(span.x) + _aimMargin * 2f) / (2f * _camera.aspect);
            return Mathf.Max(sizeForHeight, sizeForWidth);
        }

        /// <summary>ボールと着地予測の中点が、UIに隠れない帯の中央に来る位置</summary>
        private Vector3 AimPosition(float size)
        {
            Vector2 center = (_ball.GroundPosition + _aimGuide.TargetPoint) * 0.5f;
            float bandCenter = (_bottomUiRatio + (1f - _topUiRatio)) * 0.5f;
            float offsetY = (0.5f - bandCenter) * 2f * size;
            return new Vector3(center.x, center.y + offsetY, transform.position.z);
        }
    }
}
