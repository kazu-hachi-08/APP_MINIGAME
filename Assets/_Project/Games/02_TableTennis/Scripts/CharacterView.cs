using UnityEngine;

namespace MiniGame.TableTennis
{
    /// <summary>
    /// ラケットの左右位置に合わせてキャラクターを立たせる表示専用コンポーネント。
    /// プレイヤー（手前・背中向き）とNPC（奥・正面）で同じものを使い、
    /// 奥行きは固定なので拡大率は Inspector の表示高さだけで決める。
    /// </summary>
    public class CharacterView : MonoBehaviour
    {
        [SerializeField] private TableLayout _table;

        [Tooltip("追従するラケット（RacketController / NpcController）")]
        [SerializeField] private MonoBehaviour _actorSource;

        [SerializeField] private SpriteRenderer _renderer;

        [Header("Placement")]
        [Tooltip("キャラクターが立つ奥行き。ラケットよりさらに外側に置く")]
        [SerializeField] private float _courtZ = 2.2f;

        [Tooltip("ラケットの左右移動にどれだけ追従するか。1で同じだけ動く")]
        [Range(0f, 1f)]
        [SerializeField] private float _followRatio = 0.6f;

        [Tooltip("表示する身長（ワールド単位）")]
        [SerializeField] private float _displayHeight = 1.7f;

        [Tooltip("足元を画面下へずらす量（ワールド単位）。手前のキャラクターを画面外まで下げるのに使う")]
        [SerializeField] private float _baseYOffset;

        [Tooltip("横移動に追いつく速さ。ラケットより遅らせて体重移動に見せる")]
        [SerializeField] private float _followSpeed = 8f;

        private ICourtActor _actor;
        private float _x;

        private void Awake()
        {
            _actor = _actorSource as ICourtActor;
            if (_actor == null)
            {
                Debug.LogWarning($"[CharacterView] {name}: ICourtActor が設定されていません。");
            }
        }

        private void Start()
        {
            ApplyScale();
        }

        /// <summary>追従するラケットを差し替える（オンライン対戦の開始時に呼ぶ）</summary>
        public void SetActor(ICourtActor actor)
        {
            _actor = actor;
        }

        private void LateUpdate()
        {
            if (_actor == null) return;

            float targetX = _actor.CourtPosition.x * _followRatio;
            _x = Mathf.Lerp(_x, targetX, 1f - Mathf.Exp(-_followSpeed * Time.deltaTime));

            Vector2 basePosition = _table.Project(new Vector3(_x, 0f, _courtZ));
            transform.position = new Vector3(basePosition.x, basePosition.y + _baseYOffset, 0f);
        }

        private void ApplyScale()
        {
            if (_renderer == null || _renderer.sprite == null) return;

            // 素材の大きさに依存せず、指定した身長になるよう倍率を求める
            float spriteHeight = Mathf.Max(0.0001f, _renderer.sprite.bounds.size.y);
            float scale = _displayHeight / spriteHeight;
            transform.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
