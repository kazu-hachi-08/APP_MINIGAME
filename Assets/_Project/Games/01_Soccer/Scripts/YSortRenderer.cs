using UnityEngine;

namespace MiniGame.Soccer
{
    /// <summary>
    /// Y座標が小さい（画面下＝手前）ものほど前面に描画する。
    /// 見下ろし視点で選手同士やボールの重なりを自然に見せるために使う
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class YSortRenderer : MonoBehaviour
    {
        /// <summary>同じY座標で前後関係を付けたいとき用の調整値（例: マーカーは選手より奥）</summary>
        [SerializeField] private int _orderOffset;

        private const float OrderPerUnit = 100f;

        private SpriteRenderer _renderer;

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
        }

        private void LateUpdate()
        {
            _renderer.sortingOrder = Mathf.RoundToInt(-transform.position.y * OrderPerUnit) + _orderOffset;
        }
    }
}
