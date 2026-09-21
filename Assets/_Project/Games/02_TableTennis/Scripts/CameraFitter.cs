using UnityEngine;

namespace MiniGame.TableTennis
{
    /// <summary>
    /// 端末のアスペクト比が変わってもプレイに必要な横幅が画面に収まるよう orthographicSize を調整する。
    /// 縦長のスマートフォンでは横が狭くなり、ラケットの可動範囲が画面外に出てしまうため。
    /// </summary>
    public class CameraFitter : MonoBehaviour
    {
        [SerializeField] private Camera _camera;

        [Tooltip("必ず画面内に収めたい横幅（ワールド単位）。ラケットの可動範囲＋余白")]
        [SerializeField] private float _requiredWidth = 7.2f;

        [Tooltip("横に余裕がある画面でこれ以上は寄らない、という最小の縦幅の半分")]
        [SerializeField] private float _minOrthographicSize = 4.5f;

        private int _lastWidth;
        private int _lastHeight;

        private void Start()
        {
            Apply();
        }

        private void Update()
        {
            // 画面回転や解像度変更に追従する（毎フレーム計算するほどの処理ではないため差分だけ見る）
            if (Screen.width == _lastWidth && Screen.height == _lastHeight) return;

            Apply();
        }

        private void Apply()
        {
            _lastWidth = Screen.width;
            _lastHeight = Screen.height;

            float aspect = (float)Mathf.Max(1, Screen.width) / Mathf.Max(1, Screen.height);
            _camera.orthographicSize = Mathf.Max(_minOrthographicSize, _requiredWidth * 0.5f / aspect);
        }
    }
}
