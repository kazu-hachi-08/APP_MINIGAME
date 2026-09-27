using System;
using System.Collections;
using UnityEngine;

namespace MiniGame.Golf
{
    /// <summary>
    /// カメラの追従（§4.3）。狙っている間はゴルファーの背中越し（Perspective）に打つ方向を見せ、
    /// 打ってフォロースルーを見せ終えたら真上視点（Orthographic）に戻ってボールを追う。
    /// 高さでずれるボール本体ではなく地面の位置を追うことで、飛んでいる間もカメラが上下に揺れないようにする。
    /// 「全体」ボタンを押している間だけ、ホール全体を真上から見せる。
    /// ホール開始時はグリーンからティーまでを真上から流して見せ、これから攻めるコースの形を伝える。
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class GolfCameraFollower : MonoBehaviour
    {
        private enum ViewMode { TopDown, Behind, Overview, Flyover }

        /// <summary>背後視点のカメラ位置。ショットとパターで切り替える</summary>
        [Serializable]
        private struct BehindView
        {
            [Tooltip("ボールから打つ方向の反対へ下がる距離（地面のユニット）")]
            public float BackDistance;

            [Tooltip("地面からのカメラの高さ")]
            public float Height;

            [Tooltip("ボールより前方のどこを画面の中心にするか。大きいほどボールが画面の下へ寄る")]
            public float LookAheadDistance;
        }

        [SerializeField] private GolfBall _ball;
        [Tooltip("背後視点にするかどうかと、背後から見るボールの位置はゴルファーの演出に合わせる")]
        [SerializeField] private GolferView _golfer;
        [SerializeField] private ShotInput _input;
        [SerializeField] private ClubSelector _clubs;
        [SerializeField] private HoleLoader _holeLoader;
        [SerializeField] private HoldButton _overviewButton;

        [Tooltip("ボールに追いつくまでのおおよその時間（秒）。0だと完全に張り付く")]
        [SerializeField] private float _smoothTime = 0.15f;

        [Tooltip("ボールより奥（画面の上）を見せる量。打つ先が見えるようにする")]
        [SerializeField] private float _lookAhead = 2f;

        [Header("背後視点（狙っている間）")]
        [SerializeField] private BehindView _shotView = new BehindView { BackDistance = 3f, Height = 2f, LookAheadDistance = 2.75f };

        [Tooltip("パターのときは低く・近くして、グリーンの傾斜の矢印を読みやすくする")]
        [SerializeField] private BehindView _puttView = new BehindView { BackDistance = 2f, Height = 1.4f, LookAheadDistance = 2f };

        [Tooltip("縦の視野角。既定値でボールが下のゲージより上、地平線が上のHUDあたりに来る")]
        [SerializeField] private float _fieldOfView = 60f;

        [Header("全体表示")]
        [Tooltip("ホールの外側に残す余白（ユニット）。コースの端が画面の端に貼り付かないようにする")]
        [SerializeField] private float _overviewMargin = 2f;

        [Header("ホール開始の流し見せ")]
        [Tooltip("グリーンで止めて見せる時間（秒）。カップの位置を先に覚えてもらう")]
        [SerializeField] private float _flyoverHoldSeconds = 0.8f;

        [Tooltip("グリーンからティーまで流す時間（秒）")]
        [SerializeField] private float _flyoverMoveSeconds = 1.8f;

        private Camera _camera;
        private float _baseSize;
        private float _topDownZ;
        private Vector3 _velocity;
        private ViewMode _mode;
        private bool _isFlyingOver;
        private float _flyoverProgress;

        /// <summary>背中越しに見ている間 true。寝かせて描いている飾り（木）を立てる判定に使う</summary>
        public bool IsBehind => _mode == ViewMode.Behind;

        public float FlyoverSeconds => _flyoverHoldSeconds + _flyoverMoveSeconds;

        /// <summary>
        /// グリーンからティー（ボールの位置）までカメラを流す。終わりの位置を真上視点の位置に揃えて、
        /// 通常の追従へ切り替わるときにカメラが跳ばないようにする。
        /// </summary>
        public IEnumerator PlayFlyover()
        {
            _isFlyingOver = true;
            _flyoverProgress = 0f;
            yield return new WaitForSeconds(_flyoverHoldSeconds);

            for (float t = 0f; t < _flyoverMoveSeconds; t += Time.deltaTime)
            {
                _flyoverProgress = t / _flyoverMoveSeconds;
                yield return null;
            }

            _flyoverProgress = 1f;
            _isFlyingOver = false;
        }

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
            ViewMode wanted = WantedMode();
            // Perspective ⇔ Orthographic や全体表示の広さの補間は難しいので、切り替わる瞬間だけは寄せずに飛ばす
            bool snap = wanted != _mode;
            _mode = wanted;

            switch (_mode)
            {
                case ViewMode.Behind: SetBehind(snap); break;
                case ViewMode.Overview: SetOverview(); break;
                case ViewMode.Flyover: SetFlyover(); break;
                default: SetTopDown(snap); break;
            }
        }

        private ViewMode WantedMode()
        {
            if (_isFlyingOver && _holeLoader.CurrentCourse != null) return ViewMode.Flyover;
            if (_overviewButton.IsHeld && _holeLoader.CurrentCourse != null) return ViewMode.Overview;
            return _golfer.IsShowing ? ViewMode.Behind : ViewMode.TopDown;
        }

        private void SetBehind(bool snap)
        {
            _camera.orthographic = false;
            BehindView view = _clubs.Current.IsPutter ? _puttView : _shotView;

            // フォロースルー中はボールが飛び始めているので、追わずに打った位置から見る
            Vector3 ball = _golfer.AddressPosition;
            Vector3 dir = _input.Direction;
            Vector3 targetPosition = ball - dir * view.BackDistance + Vector3.back * view.Height;
            Vector3 lookPoint = ball + dir * view.LookAheadDistance;
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

        private void SetFlyover()
        {
            _camera.orthographic = true;
            _camera.orthographicSize = _baseSize;
            transform.rotation = Quaternion.identity;

            Vector2 cup = _holeLoader.CurrentCourse.CupPosition;
            Vector2 ball = _ball.GroundPosition;
            Vector2 ground = Vector2.Lerp(cup, ball, Mathf.SmoothStep(0f, 1f, _flyoverProgress));
            MoveTo(new Vector3(ground.x, ground.y + _lookAhead, _topDownZ), snap: true);
        }

        /// <summary>押している間は見ているだけなので、なめらかさより即座に全体が見えることを優先する</summary>
        private void SetOverview()
        {
            Bounds bounds = _holeLoader.CurrentCourse.TerrainBounds;
            float halfHeight = bounds.extents.y + _overviewMargin;
            float halfWidth = bounds.extents.x + _overviewMargin;

            _camera.orthographic = true;
            _camera.orthographicSize = Mathf.Max(halfHeight, halfWidth / _camera.aspect);
            transform.rotation = Quaternion.identity;
            MoveTo(new Vector3(bounds.center.x, bounds.center.y, _topDownZ), snap: true);
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
