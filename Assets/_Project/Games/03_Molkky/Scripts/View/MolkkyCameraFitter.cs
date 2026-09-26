using UnityEngine;

namespace MiniGame.Molkky
{
    /// <summary>
    /// 縦長のスマートフォンでも投擲ラインの左右端が画面に収まるよう orthographicSize を調整する。
    /// </summary>
    public class MolkkyCameraFitter : MonoBehaviour
    {
        [SerializeField] private Camera _camera;

        [Tooltip("必ず画面内に収めたい横幅（ワールド単位）。投擲ラインの幅＋余白")]
        [SerializeField] private float _requiredWidth = 5.4f;

        [Tooltip("横に余裕がある画面でこれ以上は寄らない、という最小の縦幅の半分")]
        [SerializeField] private float _minOrthographicSize = 5f;

        private int _lastWidth;
        private int _lastHeight;

        private void Start()
        {
            Apply();
        }

        private void Update()
        {
            // 解像度が変わったときだけ計算し直す
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
