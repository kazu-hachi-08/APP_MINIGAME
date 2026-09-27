using UnityEngine;

namespace MiniGame.Golf
{
    /// <summary>
    /// 背後視点で打つ人を見せる（背後視点 Phase B）。表示だけの責任で、ショットの判定には関わらない。
    /// 地面（XY 平面）に寝かさず Z 方向へ立て、打つ方向に正対させる。カメラへ向けるビルボードより、
    /// 背後視点のカメラと同じ向きに揃えた方が背中が歪まず安定して見える。
    /// 真上視点では立てたスプライトが線にしか見えないので、狙っている間（背後視点の間）だけ表示する。
    /// </summary>
    public class GolferView : MonoBehaviour
    {
        [SerializeField] private GolfBall _ball;
        [SerializeField] private ShotInput _input;
        [SerializeField] private AimGuideView _aimGuide;

        [Tooltip("胴と脚。プレイヤー色を掛けて服の色にする")]
        [SerializeField] private SpriteRenderer _body;

        [Tooltip("頭（後ろ姿なので髪の色）")]
        [SerializeField] private SpriteRenderer _head;

        [Tooltip("ボールから打つ方向の左へずらす距離（右打ちの立ち位置）。ボールが体に隠れないだけ離す")]
        [SerializeField] private float _sideOffset = 0.6f;

        [Tooltip("ボールより後ろへ下げる距離。0でボールの真横")]
        [SerializeField] private float _backOffset = 0.1f;

        [Tooltip("立ち絵の高さ（ユニット）。ボールの直径 0.35 に対して人らしく見える大きさ")]
        [SerializeField] private float _height = 1.2f;

        [Tooltip("胴の高さを1としたときの頭の中心の高さ")]
        [SerializeField] private float _headHeight = 1.1f;

        [Tooltip("胴の高さを1としたときの頭の直径")]
        [SerializeField] private float _headDiameter = 0.34f;

        [SerializeField] private Color _hairColor = new Color(0.22f, 0.15f, 0.1f);

        private void Awake()
        {
            _body.sprite = GolfShapeSprites.GolferBody;
            _head.sprite = GolfShapeSprites.Circle;
            _head.color = _hairColor;
            _head.transform.localPosition = Vector3.up * _headHeight;
            _head.transform.localScale = Vector3.one * _headDiameter;
            transform.localScale = Vector3.one * _height;
        }

        /// <summary>手番の人の色にする。ゴルファーは1人を使い回すので、手番が替わるたびに呼ぶ</summary>
        public void SetPlayerColor(Color color)
        {
            _body.color = color;
        }

        private void LateUpdate()
        {
            bool visible = _aimGuide.IsShowing;
            _body.enabled = visible;
            _head.enabled = visible;
            if (!visible) return;

            Vector2 dir = _input.Direction;
            Vector2 left = new Vector2(-dir.y, dir.x);
            transform.position = _ball.GroundPosition + left * _sideOffset - dir * _backOffset;
            // 前＝打つ方向、上＝空（-Z）。カメラと同じ規則なので、スプライトの右が画面の右になる
            transform.rotation = Quaternion.LookRotation(dir, Vector3.back);
        }
    }
}
