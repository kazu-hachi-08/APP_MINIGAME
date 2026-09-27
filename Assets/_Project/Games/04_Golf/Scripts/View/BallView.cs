using UnityEngine;

namespace MiniGame.Golf
{
    /// <summary>
    /// ボール本体の表示（§4.2）。真上視点では高さが見えないので、高いほど画面の上へずらして少し大きく描き、
    /// 地面に残る影（ShadowView）との距離で高さを分からせる。
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class BallView : MonoBehaviour
    {
        [SerializeField] private GolfBall _ball;

        [Tooltip("ボールの直径（ユニット）")]
        [SerializeField] private float _diameter = 0.35f;

        [Tooltip("高さ1あたり、影から画面の上へずらす量")]
        [SerializeField] private float _liftPerHeight = 0.5f;

        [Tooltip("高さ1あたりの拡大率。近づいて見える感じを出す")]
        [SerializeField] private float _scalePerHeight = 0.25f;

        private void Awake()
        {
            GetComponent<SpriteRenderer>().sprite = GolfShapeSprites.Circle;
        }

        private void LateUpdate()
        {
            float height = _ball.Height;
            transform.position = _ball.GroundPosition + Vector2.up * (height * _liftPerHeight);
            transform.localScale = Vector3.one * (_diameter * (1f + height * _scalePerHeight));
        }
    }
}
