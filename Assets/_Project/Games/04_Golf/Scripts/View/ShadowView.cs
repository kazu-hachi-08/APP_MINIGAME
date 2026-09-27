using UnityEngine;

namespace MiniGame.Golf
{
    /// <summary>
    /// ボールの影の表示（§4.2）。常に地面の位置に置き、高いほど小さく薄くして、ボール本体との離れ具合を強調する。
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class ShadowView : MonoBehaviour
    {
        [SerializeField] private GolfBall _ball;

        [Tooltip("地面にあるときの影の大きさ（ユニット）。横長にして地面に落ちた影らしくする")]
        [SerializeField] private Vector2 _size = new Vector2(0.4f, 0.25f);

        [SerializeField] private Color _color = new Color(0f, 0f, 0f, 0.35f);

        [Tooltip("この高さで影が最も小さく薄くなる")]
        [SerializeField] private float _fadeHeight = 3f;

        [Range(0f, 1f)]
        [SerializeField] private float _minScale = 0.6f;

        [Range(0f, 1f)]
        [SerializeField] private float _minAlphaRatio = 0.4f;

        private SpriteRenderer _renderer;

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            _renderer.sprite = GolfShapeSprites.Circle;
        }

        private void LateUpdate()
        {
            float t = Mathf.Clamp01(_ball.Height / _fadeHeight);

            transform.position = _ball.GroundPosition;
            Vector2 size = _size * Mathf.Lerp(1f, _minScale, t);
            transform.localScale = new Vector3(size.x, size.y, 1f);

            Color color = _color;
            color.a *= Mathf.Lerp(1f, _minAlphaRatio, t);
            _renderer.color = color;
        }
    }
}
