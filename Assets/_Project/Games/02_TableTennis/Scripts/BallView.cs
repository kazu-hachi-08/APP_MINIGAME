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

        [Tooltip("手前端にいるときの表示直径（ワールド単位）")]
        [SerializeField] private float _displaySizeAtNear = 0.34f;

        private float _spriteUnitSize = 1f;

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

            if (_shadow != null)
            {
                _shadow.position = _table.Project(new Vector3(court.x, 0f, court.z));
                // 台に貼り付いた影に見せるため縦につぶす
                _shadow.localScale = new Vector3(size, size * 0.35f, 1f);
            }
        }
    }
}
