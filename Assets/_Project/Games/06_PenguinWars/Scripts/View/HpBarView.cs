using UnityEngine;

namespace MiniGame.PenguinWars
{
    /// <summary>城とユニットの頭上のHPバー。中心ピボットの四角の左端を固定したまま横幅を縮めて割合を表す</summary>
    public class HpBarView : MonoBehaviour
    {
        [SerializeField] private Transform _fill;

        private bool _initialized;
        private float _fullWidth;
        private float _leftEdge;

        /// <summary>非アクティブのまま呼ばれても Awake を待たずに使えるよう、初回に元の大きさを覚える</summary>
        private void EnsureInitialized()
        {
            if (_initialized) return;

            _initialized = true;
            _fullWidth = _fill.localScale.x;
            _leftEdge = _fill.localPosition.x - _fullWidth * 0.5f;
        }

        public void SetRatio(float ratio)
        {
            EnsureInitialized();
            float width = _fullWidth * Mathf.Clamp01(ratio);

            Vector3 scale = _fill.localScale;
            scale.x = width;
            _fill.localScale = scale;

            Vector3 position = _fill.localPosition;
            position.x = _leftEdge + width * 0.5f;
            _fill.localPosition = position;
        }
    }
}
