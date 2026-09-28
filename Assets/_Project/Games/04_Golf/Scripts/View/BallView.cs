using UnityEngine;

namespace MiniGame.Golf
{
    /// <summary>
    /// ボール本体の表示。真上視点では高さが見えないので、高いほど画面の上へずらして少し大きく描き、
    /// 地面に残る影（ShadowView）との距離で高さを分からせる。
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class BallView : MonoBehaviour
    {
        [SerializeField] private GolfBall _ball;

        [Tooltip("ボールの下に重ねるプレイヤー色の円。少し大きくして縁取りに見せる")]
        [SerializeField] private SpriteRenderer _ring;

        [Tooltip("縁取りの大きさ（ボールに対する倍率）")]
        [SerializeField] private float _ringScale = 1.35f;

        [Tooltip("ボールの直径（ユニット）")]
        [SerializeField] private float _diameter = 0.35f;

        [Tooltip("高さ1あたり、影から画面の上へずらす量")]
        [SerializeField] private float _liftPerHeight = 0.5f;

        [Tooltip("高さ1あたりの拡大率。近づいて見える感じを出す")]
        [SerializeField] private float _scalePerHeight = 0.25f;

        private void Awake()
        {
            GetComponent<SpriteRenderer>().sprite = GolfShapeSprites.Ball;
            _ring.sprite = GolfShapeSprites.Circle;
            _ring.transform.localScale = Vector3.one * _ringScale;
        }

        /// <summary>手番の人の色にする。ボールは1つを使い回すので、手番が替わるたびに呼ぶ</summary>
        public void SetPlayerColor(Color color)
        {
            _ring.color = color;
        }

        private void LateUpdate()
        {
            float height = _ball.Height;
            transform.position = _ball.GroundPosition + Vector2.up * (height * _liftPerHeight);
            transform.localScale = Vector3.one * (_diameter * (1f + height * _scalePerHeight));
        }
    }
}
