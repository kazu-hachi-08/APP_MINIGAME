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

        [Header("Point Reaction")]
        [Tooltip("得点・失点のリアクションを続ける時間。得点表示（1.4秒）より短くして次のサーブに持ち越さない")]
        [SerializeField] private float _reactionDuration = 1.1f;

        [Tooltip("得点したときに跳ねる回数")]
        [SerializeField] private int _winHopCount = 2;

        [Tooltip("得点したときに跳ねる高さ（ワールド単位）")]
        [SerializeField] private float _winHopHeight = 0.35f;

        [Tooltip("失点したときに沈み込む深さ（ワールド単位）")]
        [SerializeField] private float _loseSinkDepth = 0.15f;

        [Tooltip("失点したときに縦に縮む割合。うなだれて小さくなったように見せる")]
        [Range(0f, 1f)]
        [SerializeField] private float _loseShrinkRatio = 0.08f;

        [Tooltip("失点したときの色。暗くして落ち込んで見せる")]
        [SerializeField] private Color _loseTint = new Color(0.55f, 0.55f, 0.65f, 1f);

        private ICourtActor _actor;
        private float _x;
        private float _baseScale = 1f;
        private float _reactionTimer;
        private bool _isWinReaction;

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

        /// <summary>選んだ選手の見た目に差し替える。素材ごとに大きさが違っても同じ身長で表示する</summary>
        public void SetSprite(Sprite sprite)
        {
            if (_renderer == null || sprite == null) return;

            _renderer.sprite = sprite;
            ApplyScale();
        }

        /// <summary>追従するラケットを差し替える（オンライン対戦の開始時に呼ぶ）</summary>
        public void SetActor(ICourtActor actor)
        {
            _actor = actor;
        }

        /// <summary>
        /// 得点（ジャンプ）/ 失点（うなだれる）のリアクションを始める。
        /// 専用の絵を用意せずに済むよう、位置・縦の縮み・色だけで表現する
        /// </summary>
        public void PlayPointReaction(bool isWinner)
        {
            _isWinReaction = isWinner;
            _reactionTimer = _reactionDuration;
        }

        private void LateUpdate()
        {
            if (_actor == null) return;

            float targetX = _actor.CourtPosition.x * _followRatio;
            _x = Mathf.Lerp(_x, targetX, 1f - Mathf.Exp(-_followSpeed * Time.deltaTime));

            Vector2 basePosition = _table.Project(new Vector3(_x, 0f, _courtZ));
            float reactionY = UpdateReaction();
            transform.position = new Vector3(basePosition.x, basePosition.y + _baseYOffset + reactionY, 0f);
        }

        /// <summary>リアクションの進み具合から、縦のずれを返す（縮みと色もここで反映する）</summary>
        private float UpdateReaction()
        {
            if (_reactionTimer <= 0f) return 0f;

            _reactionTimer = Mathf.Max(0f, _reactionTimer - Time.deltaTime);
            float progress = 1f - _reactionTimer / Mathf.Max(0.0001f, _reactionDuration);

            if (_isWinReaction)
            {
                ApplyReactionLook(1f, Color.white);
                return Mathf.Abs(Mathf.Sin(progress * Mathf.PI * _winHopCount)) * _winHopHeight;
            }

            // 終わり際に元へ戻るよう、0→1→0 の山なりで沈ませる
            float weight = Mathf.Sin(progress * Mathf.PI);
            ApplyReactionLook(1f - _loseShrinkRatio * weight, Color.Lerp(Color.white, _loseTint, weight));
            return -_loseSinkDepth * weight;
        }

        private void ApplyReactionLook(float verticalScale, Color tint)
        {
            transform.localScale = new Vector3(_baseScale, _baseScale * verticalScale, 1f);
            if (_renderer != null)
            {
                _renderer.color = tint;
            }
        }

        private void ApplyScale()
        {
            if (_renderer == null || _renderer.sprite == null) return;

            // 素材の大きさに依存せず、指定した身長になるよう倍率を求める
            float spriteHeight = Mathf.Max(0.0001f, _renderer.sprite.bounds.size.y);
            _baseScale = _displayHeight / spriteHeight;
            transform.localScale = new Vector3(_baseScale, _baseScale, 1f);
        }
    }
}
