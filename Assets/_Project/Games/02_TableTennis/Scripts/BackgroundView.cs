using UnityEngine;

namespace MiniGame.TableTennis
{
    /// <summary>
    /// 背景スプライトをカメラの表示範囲いっぱいに引き伸ばす。
    /// CameraFitterが端末ごとにorthographicSizeを変えるため、固定サイズの背景だと余白ができてしまう。
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class BackgroundView : MonoBehaviour
    {
        [SerializeField] private Camera _camera;

        private SpriteRenderer _renderer;
        private float _lastOrthoSize;
        private float _lastAspect;

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
        }

        private void Update()
        {
            if (_camera == null) return;

            // 毎フレーム計算するほどの処理ではないため、画面回転などで値が変わったときだけ再計算する
            if (Mathf.Approximately(_camera.orthographicSize, _lastOrthoSize) &&
                Mathf.Approximately(_camera.aspect, _lastAspect))
            {
                return;
            }

            _lastOrthoSize = _camera.orthographicSize;
            _lastAspect = _camera.aspect;
            Fit();
        }

        private void Fit()
        {
            float worldHeight = _camera.orthographicSize * 2f;
            float worldWidth = worldHeight * _camera.aspect;

            Vector2 spriteSize = _renderer.sprite.bounds.size;
            transform.position = new Vector3(_camera.transform.position.x, _camera.transform.position.y, transform.position.z);
            transform.localScale = new Vector3(worldWidth / spriteSize.x, worldHeight / spriteSize.y, 1f);
        }
    }
}
