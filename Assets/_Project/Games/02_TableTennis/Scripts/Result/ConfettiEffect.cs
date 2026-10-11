using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.TableTennis
{
    /// <summary>
    /// 勝利画面の紙吹雪。素材を用意せずに済むよう、無地の Image を色違いで降らせる。
    /// 画面下へ抜けた紙は上へ戻して使い回し、表示中ずっと降り続けるようにする。
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class ConfettiEffect : MonoBehaviour
    {
        private struct Piece
        {
            public RectTransform Rect;
            public float FallSpeed;
            public float SpinSpeed;
            public float SwayPhase;
        }

        [SerializeField] private int _pieceCount = 60;
        [SerializeField] private Vector2 _pieceSize = new Vector2(16f, 26f);

        [Tooltip("落ちる速さの範囲（UI単位/秒）")]
        [SerializeField] private Vector2 _fallSpeedRange = new Vector2(250f, 450f);

        [Tooltip("左右に揺れる速さ。ひらひら舞って見せる")]
        [SerializeField] private float _swaySpeed = 80f;

        [SerializeField] private float _swayFrequency = 3f;
        [SerializeField] private float _maxSpinSpeed = 360f;

        [SerializeField] private Color[] _colors =
        {
            new Color(1f, 0.3f, 0.3f),
            new Color(1f, 0.85f, 0.2f),
            new Color(0.3f, 0.8f, 1f),
            new Color(0.4f, 0.9f, 0.4f),
            new Color(1f, 0.5f, 0.9f)
        };

        private Piece[] _pieces;
        private RectTransform _area;

        public void Play()
        {
            _area = (RectTransform)transform;
            if (_pieces == null)
            {
                CreatePieces();
            }

            // 最初は画面の上半分に散らして、始まった瞬間から舞っているように見せる
            for (int i = 0; i < _pieces.Length; i++)
            {
                Respawn(ref _pieces[i], Random.Range(0f, _area.rect.height * 0.5f));
            }
        }

        private void Update()
        {
            if (_pieces == null) return;

            float bottom = -_area.rect.height * 0.5f - _pieceSize.y;
            for (int i = 0; i < _pieces.Length; i++)
            {
                Move(ref _pieces[i]);

                if (_pieces[i].Rect.anchoredPosition.y < bottom)
                {
                    Respawn(ref _pieces[i], 0f);
                }
            }
        }

        private void Move(ref Piece piece)
        {
            piece.SwayPhase += _swayFrequency * Time.deltaTime;

            Vector2 position = piece.Rect.anchoredPosition;
            position.y -= piece.FallSpeed * Time.deltaTime;
            position.x += Mathf.Sin(piece.SwayPhase) * _swaySpeed * Time.deltaTime;
            piece.Rect.anchoredPosition = position;
            piece.Rect.Rotate(0f, 0f, piece.SpinSpeed * Time.deltaTime);
        }

        private void CreatePieces()
        {
            _pieces = new Piece[_pieceCount];
            for (int i = 0; i < _pieceCount; i++)
            {
                var obj = new GameObject($"Confetti_{i}", typeof(RectTransform));
                obj.transform.SetParent(transform, false);

                var image = obj.AddComponent<Image>();
                image.color = _colors[i % _colors.Length];
                image.raycastTarget = false;

                _pieces[i].Rect = (RectTransform)obj.transform;
                _pieces[i].Rect.sizeDelta = _pieceSize;
            }
        }

        /// <param name="depthBelowTop">画面のすぐ上からどれだけ下に置くか（0で画面外のすぐ上）</param>
        private void Respawn(ref Piece piece, float depthBelowTop)
        {
            float halfWidth = _area.rect.width * 0.5f;
            float top = _area.rect.height * 0.5f + _pieceSize.y;

            piece.Rect.anchoredPosition = new Vector2(Random.Range(-halfWidth, halfWidth), top - depthBelowTop);
            piece.FallSpeed = Random.Range(_fallSpeedRange.x, _fallSpeedRange.y);
            piece.SpinSpeed = Random.Range(-_maxSpinSpeed, _maxSpinSpeed);
            piece.SwayPhase = Random.Range(0f, Mathf.PI * 2f);
        }
    }
}
