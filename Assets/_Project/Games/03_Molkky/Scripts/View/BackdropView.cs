using UnityEngine;

namespace MiniGame.Molkky
{
    /// <summary>
    /// フィールドの奥に置く背景（空と木立）。地面の奥行き groundZ の位置に下端を合わせて置き、
    /// それより奥の芝を隠すことで「広場の奥に木が並んでいる」見た目にする。
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class BackdropView : MonoBehaviour
    {
        [SerializeField] private DepthProjector _projector;

        [Tooltip("背景の下端を置く奥行き（地面単位）。ピンが飛んでいく範囲より奥にする")]
        [SerializeField] private float _groundZ = 16f;

        [Tooltip("背景の横幅（ワールド単位）。縦長〜タブレットまで画面の左右を覆える幅にする")]
        [SerializeField] private float _width = 14f;

        [Tooltip("木の根元を芝に少し埋めて、背景と地面の境目を目立たせない")]
        [SerializeField] private float _sinkDepth = 0.1f;

        private void Start()
        {
            var spriteRenderer = GetComponent<SpriteRenderer>();

            // スプライトは下端中央がピボット
            Vector2 bottom = _projector.Project(new Vector2(0f, _groundZ));
            transform.position = new Vector3(bottom.x, bottom.y - _sinkDepth, 0f);
            transform.localScale = Vector3.one * (_width / spriteRenderer.sprite.bounds.size.x);

            // groundZ より奥にある物（遠くへ転がったピンなど）は木立の裏に隠れる
            spriteRenderer.sortingOrder = _projector.SortingOrderAt(_groundZ);
        }
    }
}
