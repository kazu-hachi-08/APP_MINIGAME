using UnityEngine;

namespace MiniGame.PenguinWars
{
    /// <summary>
    /// 城の見た目と頭上のHPバー。HP の計算は戦闘ロジック側が持ち、ここは渡された値を描くだけ
    /// </summary>
    public class CastleView : MonoBehaviour
    {
        [SerializeField] private GameObject _hpBarRoot;
        [Tooltip("中心ピボットの四角。左端を固定したまま横幅を縮めてHP割合を表す")]
        [SerializeField] private Transform _hpFill;

        private float _fullWidth;
        private float _leftEdge;

        private void Awake()
        {
            _fullWidth = _hpFill.localScale.x;
            _leftEdge = _hpFill.localPosition.x - _fullWidth * 0.5f;
        }

        public void SetHp(int current, int max)
        {
            _hpBarRoot.SetActive(true);
            float ratio = max > 0 ? Mathf.Clamp01((float)current / max) : 0f;
            float width = _fullWidth * ratio;

            Vector3 scale = _hpFill.localScale;
            scale.x = width;
            _hpFill.localScale = scale;

            Vector3 position = _hpFill.localPosition;
            position.x = _leftEdge + width * 0.5f;
            _hpFill.localPosition = position;
        }

        public void HideHp()
        {
            _hpBarRoot.SetActive(false);
        }
    }
}
