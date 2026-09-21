using UnityEngine;

namespace MiniGame.TableTennis
{
    /// <summary>
    /// 台に着地した位置で輪を広げる、バウンドの表示専用コンポーネント。
    /// 擬似3Dでは「どこに落ちたか」が読み取りにくいため、着地点をはっきり見せる。
    /// 連続で鳴るので生成はせず、1枚のスプライトを使い回す。
    /// </summary>
    public class BounceEffect : MonoBehaviour
    {
        [SerializeField] private TableLayout _table;
        [SerializeField] private BallMotion _ball;
        [SerializeField] private SpriteRenderer _renderer;

        [SerializeField] private float _duration = 0.3f;

        [Tooltip("広がり始め／広がり終わりの直径（手前端でのワールド単位）")]
        [SerializeField] private float _startSize = 0.25f;
        [SerializeField] private float _endSize = 1.0f;

        /// <summary>負の値なら再生していない</summary>
        private float _time = -1f;

        private float _spriteUnitSize = 1f;

        /// <summary>着地した奥行きでの表示倍率</summary>
        private float _depthScale = 1f;

        private void Start()
        {
            if (_renderer != null && _renderer.sprite != null)
            {
                _spriteUnitSize = Mathf.Max(0.0001f, _renderer.sprite.bounds.size.x);
            }

            Hide();
        }

        private void OnEnable()
        {
            _ball.OnBounced += Play;
        }

        private void OnDisable()
        {
            _ball.OnBounced -= Play;
        }

        private void Play(Vector3 contact)
        {
            // 着地点は台の上（高さ0）なので、そのまま投影すれば台に貼り付いて見える
            Vector2 position = _table.Project(new Vector3(contact.x, 0f, contact.z));
            transform.position = new Vector3(position.x, position.y, 0f);

            // 奥で落ちたときは小さく見せて、奥行きの手がかりにする
            _depthScale = _table.ScaleAt(contact.z) / _table.NearScale;
            _time = 0f;
            _renderer.enabled = true;
        }

        private void Update()
        {
            if (_time < 0f) return;

            _time += Time.deltaTime;
            float progress = _time / Mathf.Max(0.01f, _duration);

            if (progress >= 1f)
            {
                Hide();
                return;
            }

            float size = Mathf.Lerp(_startSize, _endSize, progress) * _depthScale / _spriteUnitSize;

            // 台に寝ているように見せるため縦につぶす
            transform.localScale = new Vector3(size, size * 0.4f, 1f);

            Color color = _renderer.color;
            _renderer.color = new Color(color.r, color.g, color.b, 1f - progress);
        }

        private void Hide()
        {
            _time = -1f;
            if (_renderer != null)
            {
                _renderer.enabled = false;
            }
        }
    }
}
