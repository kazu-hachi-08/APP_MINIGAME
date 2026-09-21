using UnityEngine;

namespace MiniGame.Soccer
{
    /// <summary>
    /// コート全体ではなく、操作対象選手の周辺を映すプレイヤー追従カメラ
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform _target;
        [SerializeField] private float _followSpeed = 8f;

        [Header("Field Clamp")]
        [SerializeField] private Vector2 _fieldHalfExtents = new Vector2(11f, 6f);
        [SerializeField] private float _margin = 1.5f;

        private Camera _camera;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
        }

        public void SetTarget(Transform target)
        {
            _target = target;
        }

        private void LateUpdate()
        {
            if (_target == null) return;

            Vector3 desired = new Vector3(_target.position.x, _target.position.y, transform.position.z);
            Vector3 clamped = ClampToField(desired);
            transform.position = Vector3.Lerp(transform.position, clamped, _followSpeed * Time.deltaTime);
        }

        private Vector3 ClampToField(Vector3 position)
        {
            float halfHeight = _camera != null ? _camera.orthographicSize : 4f;
            float halfWidth = halfHeight * Screen.width / Mathf.Max(Screen.height, 1);

            float minX = -_fieldHalfExtents.x - _margin + halfWidth;
            float maxX = _fieldHalfExtents.x + _margin - halfWidth;
            float minY = -_fieldHalfExtents.y - _margin + halfHeight;
            float maxY = _fieldHalfExtents.y + _margin - halfHeight;

            float clampedX = minX <= maxX ? Mathf.Clamp(position.x, minX, maxX) : 0f;
            float clampedY = minY <= maxY ? Mathf.Clamp(position.y, minY, maxY) : 0f;

            return new Vector3(clampedX, clampedY, position.z);
        }
    }
}
