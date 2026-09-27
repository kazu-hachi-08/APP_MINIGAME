using UnityEngine;

namespace MiniGame.Golf
{
    /// <summary>
    /// カメラの追従（§4.3）。狙っている間はゴルファーの背中越し（Perspective）に打つ方向を見せ、
    /// 打ってフォロースルーを見せ終えたら真上視点（Orthographic）に戻ってボールを追う。
    /// 高さでずれるボール本体ではなく地面の位置を追うことで、飛んでいる間もカメラが上下に揺れないようにする。
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class GolfCameraFollower : MonoBehaviour
    {
        [SerializeField] private GolfBall _ball;
        [Tooltip("背後視点にするかどうかと、背後から見るボールの位置はゴルファーの演出に合わせる")]
        [SerializeField] private GolferView _golfer;
        [SerializeField] private ShotInput _input;

        [Tooltip("ボールに追いつくまでのおおよその時間（秒）。0だと完全に張り付く")]
        [SerializeField] private float _smoothTime = 0.15f;

        [Tooltip("ボールより奥（画面の上）を見せる量。打つ先が見えるようにする")]
        [SerializeField] private float _lookAhead = 2f;

        [Header("背後視点（狙っている間）")]
        [Tooltip("ボールから打つ方向の反対へ下がる距離（地面のユニット）")]
        [SerializeField] private float _backDistance = 3f;

        [Tooltip("地面からのカメラの高さ")]
        [SerializeField] private float _backHeight = 2f;

        [Tooltip("ボールより前方のどこを画面の中心にするか。大きいほどボールが画面の下へ寄る")]
        [SerializeField] private float _lookAheadDistance = 2.75f;

        [Tooltip("縦の視野角。既定値でボールが下のゲージより上、地平線が上のHUDあたりに来る")]
        [SerializeField] private float _fieldOfView = 60f;

        private Camera _camera;
        private float _baseSize;
        private float _topDownZ;
        private Vector3 _velocity;
        private bool _isBehind;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            _baseSize = _camera.orthographicSize;
            _topDownZ = transform.position.z;
            _camera.fieldOfView = _fieldOfView;
            // Perspective のときも Sprite の重なりを真上視点と同じ規則（sortingOrder → 画面の奥行き無視）で決める
            _camera.transparencySortMode = TransparencySortMode.Orthographic;
        }

        private void Start()
        {
            // 初回はなめらかに寄せず、最初からボールを映す
            SetTopDown(snap: true);
        }

        private void LateUpdate()
        {
            bool wantBehind = _golfer.IsShowing;
            // Perspective ⇔ Orthographic の補間は難しいので、切り替わる瞬間だけは寄せずに飛ばす
            bool snap = wantBehind != _isBehind;
            _isBehind = wantBehind;

            if (wantBehind) SetBehind(snap);
            else SetTopDown(snap);
        }

        private void SetBehind(bool snap)
        {
            _camera.orthographic = false;

            // フォロースルー中はボールが飛び始めているので、追わずに打った位置から見る
            Vector3 ball = _golfer.AddressPosition;
            Vector3 dir = _input.Direction;
            Vector3 targetPosition = ball - dir * _backDistance + Vector3.back * _backHeight;
            Vector3 lookPoint = ball + dir * _lookAheadDistance;
            // 地面は XY 平面でカメラは -Z 側にいるので、-Z を「空」にすると画面の上が打つ先になる
            Quaternion targetRotation = Quaternion.LookRotation(lookPoint - targetPosition, Vector3.back);

            MoveTo(targetPosition, snap);
            transform.rotation = snap ? targetRotation : Quaternion.Slerp(transform.rotation, targetRotation, SmoothRate());
        }

        private void SetTopDown(bool snap)
        {
            _camera.orthographic = true;
            _camera.orthographicSize = _baseSize;
            transform.rotation = Quaternion.identity;

            Vector2 ground = _ball.GroundPosition;
            MoveTo(new Vector3(ground.x, ground.y + _lookAhead, _topDownZ), snap);
        }

        private void MoveTo(Vector3 targetPosition, bool snap)
        {
            if (snap)
            {
                transform.position = targetPosition;
                _velocity = Vector3.zero;
                return;
            }

            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref _velocity, _smoothTime);
        }

        /// <summary>フレームレートに依らず、SmoothDamp とおおよそ同じ速さで回転を寄せる割合</summary>
        private float SmoothRate()
        {
            if (_smoothTime <= 0f) return 1f;
            return 1f - Mathf.Exp(-Time.deltaTime / _smoothTime);
        }
    }
}
