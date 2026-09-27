using UnityEngine;

namespace MiniGame.Golf
{
    /// <summary>
    /// カメラがボールを追う（§4.3。Phase 1 は追従のみ）。
    /// 高さでずれるボール本体ではなく地面の位置を追うことで、飛んでいる間もカメラが上下に揺れないようにする。
    /// </summary>
    public class GolfCameraFollower : MonoBehaviour
    {
        [SerializeField] private GolfBall _ball;

        [Tooltip("ボールに追いつくまでのおおよその時間（秒）。0だと完全に張り付く")]
        [SerializeField] private float _smoothTime = 0.15f;

        [Tooltip("ボールより奥（画面の上）を見せる量。打つ先が見えるようにする")]
        [SerializeField] private float _lookAhead = 2f;

        private Vector3 _velocity;

        private void Start()
        {
            // 初回はなめらかに寄せず、最初からボールを映す
            transform.position = TargetPosition();
        }

        private void LateUpdate()
        {
            transform.position = Vector3.SmoothDamp(transform.position, TargetPosition(), ref _velocity, _smoothTime);
        }

        private Vector3 TargetPosition()
        {
            Vector2 ground = _ball.GroundPosition;
            return new Vector3(ground.x, ground.y + _lookAhead, transform.position.z);
        }
    }
}
