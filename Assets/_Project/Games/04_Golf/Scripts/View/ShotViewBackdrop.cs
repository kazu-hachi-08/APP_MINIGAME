using UnityEngine;

namespace MiniGame.Golf
{
    /// <summary>
    /// 背後視点の遠景。背景色（林の色）のままだと地平線より上まで林に見えるので、
    /// 背後視点の間だけ背景を空の色にし、地面は林の色の大きな板で地平線まで埋める。
    /// 真上視点では板と背景が同じ林の色になり、これまでと見た目は変わらない。
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class ShotViewBackdrop : MonoBehaviour
    {
        [SerializeField] private GolfCameraFollower _follower;

        [Tooltip("コースの外まで地面を続ける板。Tilemap より下に描く")]
        [SerializeField] private SpriteRenderer _farGround;

        [SerializeField] private Color _skyColor = new Color(0.55f, 0.78f, 0.95f);

        [Tooltip("遠くの地面の板の一辺（ユニット）。カメラの Far（1000）の手前で地平線が途切れて見えない大きさ")]
        [SerializeField] private float _farGroundSize = 1000f;

        private Camera _camera;
        private Color _groundColor;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            _groundColor = _camera.backgroundColor;
            _farGround.sprite = GolfShapeSprites.Square;
            _farGround.color = _groundColor;
            _farGround.transform.localScale = Vector3.one * _farGroundSize;
        }

        private void LateUpdate()
        {
            _camera.backgroundColor = _follower.IsBehind ? _skyColor : _groundColor;
            // 板は地面（Z=0）に置いたまま、カメラの真下についていく
            Vector3 cameraPosition = transform.position;
            _farGround.transform.position = new Vector3(cameraPosition.x, cameraPosition.y, 0f);
        }
    }
}
