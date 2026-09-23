using UnityEngine;

namespace MiniGame.TableTennis
{
    /// <summary>
    /// NpcController のコート座標を画面へ反映し、スイングを見た目として表現する表示専用コンポーネント。
    /// 返球結果は NPC 側で確定済みなので、ここでの演出は当たり判定に一切影響しない。
    /// </summary>
    public class NpcRacketView : MonoBehaviour
    {
        [SerializeField] private TableLayout _table;
        [SerializeField] private NpcController _npc;
        [SerializeField] private SpriteRenderer _renderer;

        [Tooltip("手前端にいるときの表示直径（ワールド単位）")]
        [SerializeField] private float _displaySizeAtNear = 0.85f;

        [Header("Swing Animation")]
        [SerializeField] private float _swingDuration = 0.22f;

        [Tooltip("振り抜きで手前（画面下）へ動かす距離（ワールド単位）")]
        [SerializeField] private float _swingForward = 0.35f;

        [SerializeField] private float _swingAngle = 45f;

        private float _spriteUnitSize = 1f;

        /// <summary>負の値なら再生していない</summary>
        private float _swingTime = -1f;

        /// <summary>表示する相手。既定はNPCで、オンライン対戦時は SetSource で差し替える</summary>
        private IOpponentRacket _source;

        private void Awake()
        {
            _source = _npc;
        }

        private void Start()
        {
            UpdateSpriteUnitSize();
        }

        /// <summary>選んだラケットの見た目に差し替える</summary>
        public void SetSprite(Sprite sprite)
        {
            if (_renderer == null || sprite == null) return;

            _renderer.sprite = sprite;
            UpdateSpriteUnitSize();
        }

        private void UpdateSpriteUnitSize()
        {
            if (_renderer != null && _renderer.sprite != null)
            {
                _spriteUnitSize = Mathf.Max(0.0001f, _renderer.sprite.bounds.size.x);
            }
        }

        private void OnEnable()
        {
            _source.OnSwing += PlaySwing;
        }

        private void OnDisable()
        {
            _source.OnSwing -= PlaySwing;
        }

        /// <summary>表示する相手を差し替える（オンライン対戦の開始時に呼ぶ）</summary>
        public void SetSource(IOpponentRacket source)
        {
            _source.OnSwing -= PlaySwing;
            _source = source;
            _source.OnSwing += PlaySwing;
        }

        private void PlaySwing()
        {
            _swingTime = 0f;
        }

        private void LateUpdate()
        {
            Vector3 court = _source.CourtPosition;
            float depthScale = _table.ScaleAt(court.z) / _table.NearScale;
            float size = _displaySizeAtNear * depthScale / _spriteUnitSize;

            Vector2 position = _table.Project(court);
            float swing = AdvanceSwing();

            transform.position = new Vector3(position.x, position.y - _swingForward * swing * depthScale, 0f);
            transform.localScale = new Vector3(size, size, 1f);
            transform.rotation = Quaternion.Euler(0f, 0f, -_swingAngle * swing);
        }

        /// <summary>スイングの進み具合を 0→1→0 の山で返す</summary>
        private float AdvanceSwing()
        {
            if (_swingTime < 0f) return 0f;

            float progress = _swingTime / Mathf.Max(0.01f, _swingDuration);
            if (progress >= 1f)
            {
                _swingTime = -1f;
                return 0f;
            }

            _swingTime += Time.deltaTime;
            return Mathf.Sin(progress * Mathf.PI);
        }
    }
}
