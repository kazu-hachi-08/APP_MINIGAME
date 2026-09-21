using UnityEngine;

namespace MiniGame.TableTennis
{
    /// <summary>
    /// BallMotion のコート座標を画面へ反映するだけの表示専用コンポーネント。
    /// 奥行きに応じて表示サイズを変え、擬似3Dの「飛んでくる／奥へ飛んでいく」感覚を出す。
    /// </summary>
    public class BallView : MonoBehaviour
    {
        [SerializeField] private TableLayout _table;
        [SerializeField] private BallMotion _ball;
        [SerializeField] private SpriteRenderer _renderer;

        [Tooltip("台に落ちる位置を読み取れるようにする影（高さ0に投影したボール）")]
        [SerializeField] private Transform _shadow;

        [SerializeField] private SpriteRenderer _shadowRenderer;

        [Header("Shadow")]
        [Tooltip("影が最も薄く小さくなる高さ (m)")]
        [SerializeField] private float _shadowFadeHeight = 0.9f;

        [Range(0f, 1f)] [SerializeField] private float _shadowAlphaAtGround = 0.45f;
        [Range(0f, 1f)] [SerializeField] private float _shadowAlphaAtTop = 0.12f;

        [Tooltip("手前端にいるときの表示直径（ワールド単位）")]
        [SerializeField] private float _displaySizeAtNear = 0.34f;

        [Header("Spin")]
        [Tooltip("回転量1のときのボールの回る速さ（度/秒）。回転の強さを見た目で分かるようにする")]
        [SerializeField] private float _spinRotationSpeed = 900f;

        [Tooltip("サイドスピンが見た目の回転へ寄与する割合。2Dでは横回転をそのまま描けないため弱めに混ぜる")]
        [SerializeField] private float _sideSpinVisualRatio = 0.5f;

        private float _spriteUnitSize = 1f;
        private float _spinAngle;

        private void Start()
        {
            // スプライトの実寸に依存せず、指定した表示直径になるよう倍率を求める
            if (_renderer != null && _renderer.sprite != null)
            {
                _spriteUnitSize = Mathf.Max(0.0001f, _renderer.sprite.bounds.size.x);
            }
        }

        private void LateUpdate()
        {
            Vector3 court = _ball.CourtPosition;
            float depthScale = _table.ScaleAt(court.z) / _table.NearScale;
            float size = _displaySizeAtNear * depthScale / _spriteUnitSize;

            transform.position = _table.Project(court);
            transform.localScale = new Vector3(size, size, 1f);
            transform.rotation = Quaternion.Euler(0f, 0f, AdvanceSpinAngle());

            if (_shadow != null)
            {
                _shadow.position = _table.Project(new Vector3(court.x, 0f, court.z));

                // 高いほど影を小さく薄くして、ボールの高さを読み取れるようにする
                float heightRatio = Mathf.Clamp01(court.y / Mathf.Max(0.01f, _shadowFadeHeight));
                float shadowSize = size * Mathf.Lerp(1f, 0.6f, heightRatio);

                // 台に貼り付いた影に見せるため縦につぶす
                _shadow.localScale = new Vector3(shadowSize, shadowSize * 0.35f, 1f);

                if (_shadowRenderer != null)
                {
                    Color color = _shadowRenderer.color;
                    color.a = Mathf.Lerp(_shadowAlphaAtGround, _shadowAlphaAtTop, heightRatio);
                    _shadowRenderer.color = color;
                }
            }
        }

        /// <summary>
        /// 回転量に比例した角速度でボールを回す。
        /// トップスピンで前転・バックスピンで逆転に見えるよう、符号をそのまま角速度に使う。
        /// </summary>
        private float AdvanceSpinAngle()
        {
            if (!_ball.IsFlying) return _spinAngle;

            Vector2 spin = _ball.Spin;
            float spinAmount = spin.y + spin.x * _sideSpinVisualRatio;
            _spinAngle -= spinAmount * _spinRotationSpeed * Time.deltaTime;
            return _spinAngle;
        }
    }
}
