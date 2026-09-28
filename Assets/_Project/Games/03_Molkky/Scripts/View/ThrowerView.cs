using UnityEngine;

namespace MiniGame.Molkky
{
    /// <summary>
    /// 投擲ラインの手前に、手番のキャラの背中を表示する。
    /// 棒の左右位置に追従するが、投げた後は投げた場所に残す（棒を追いかけてピンの方へ歩いていかないように）。
    /// </summary>
    public class ThrowerView : MonoBehaviour
    {
        [SerializeField] private StickThrower _stick;
        [SerializeField] private DepthProjector _projector;

        [Tooltip("投擲ラインからどれだけ手前に立つか（地面単位・マイナスが手前）。棒の飛ぶ先と重ならないよう手前に置く")]
        [SerializeField] private float _standDepth = -0.6f;

        [Tooltip("棒からの左右のずれ（地面単位）。左に立って右手に棒を持っているように見せる")]
        [SerializeField] private float _sideOffset = -0.35f;

        [Tooltip("立つ位置の左右の限界（地面単位）。投擲ラインの端で体が画面外に出ないようにする")]
        [SerializeField] private float _maxStandX = 1.3f;

        [Tooltip("キャラの背の高さ（地面単位）。頭がピンまで届かない高さに留める")]
        [SerializeField] private float _height = 1.3f;

        [Tooltip("地面（-30000）のすぐ上。棒・影・ピンより必ず奥に描き、キャラで棒が隠れて見失わないようにする。" +
                 "頭はピンの手前の列より下に収まるので、ピンと重なって前後が破綻することはない")]
        [SerializeField] private int _sortingOrder = -20000;

        private SpriteRenderer _renderer;
        private float _standX;

        private void Awake()
        {
            var obj = new GameObject("Body");
            obj.transform.SetParent(transform, false);
            _renderer = obj.AddComponent<SpriteRenderer>();
            _renderer.sortingOrder = _sortingOrder;
            _renderer.enabled = false;
        }

        public void SetCharacter(MolkkyCharacterData character)
        {
            _renderer.sprite = character != null ? character.BackSprite : null;
            _renderer.enabled = _renderer.sprite != null;
        }

        private void LateUpdate()
        {
            if (!_renderer.enabled) return;

            if (!_stick.IsThrown)
            {
                _standX = Mathf.Clamp(_stick.GroundPosition.x + _sideOffset, -_maxStandX, _maxStandX);
            }

            Vector2 ground = new Vector2(_standX, _standDepth);
            _renderer.transform.position = _projector.Project(ground);
            SetHeight(_height * _projector.ScaleAt(_standDepth));
        }

        /// <summary>スプライトの縦横比を保ったまま、指定したワールド単位の高さで表示する</summary>
        private void SetHeight(float height)
        {
            float scale = height / _renderer.sprite.bounds.size.y;
            _renderer.transform.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
