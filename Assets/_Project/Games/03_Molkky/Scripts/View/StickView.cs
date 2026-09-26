using UnityEngine;

namespace MiniGame.Molkky
{
    /// <summary>
    /// 棒と影の見た目（§4.2 / §5.1）。
    /// 棒は高さぶん浮かせて描き、影は地面に置く。影との距離で「どれだけ高く飛んでいるか」を読み取れるようにする。
    /// </summary>
    public class StickView : MonoBehaviour
    {
        [SerializeField] private StickThrower _stick;
        [SerializeField] private DepthProjector _projector;
        [SerializeField] private MolkkyPhysicsSettings _settings;

        [Header("Sprite（未設定なら単純な図形で代用する）")]
        [SerializeField] private Sprite _stickSprite;
        [SerializeField] private Sprite _shadowSprite;

        [SerializeField] private Color _stickColor = Color.white;
        [SerializeField] private Color _shadowColor = new Color(0f, 0f, 0f, 0.35f);

        [Tooltip("この高さで影が最も薄くなる")]
        [SerializeField] private float _shadowFadeHeight = 1.5f;

        [Tooltip("高く飛んでいるときの影の濃さの下限（割合）")]
        [SerializeField] private float _minShadowAlphaRatio = 0.3f;

        private SpriteRenderer _stickRenderer;
        private SpriteRenderer _shadowRenderer;

        private void Awake()
        {
            _shadowRenderer = CreateRenderer("Shadow", _shadowSprite != null ? _shadowSprite : ShapeSprites.Circle, _shadowColor);
            _stickRenderer = CreateRenderer("Stick", _stickSprite != null ? _stickSprite : ShapeSprites.Square, _stickColor);
        }

        private SpriteRenderer CreateRenderer(string name, Sprite sprite, Color color)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(transform, false);
            var renderer = obj.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            return renderer;
        }

        private void LateUpdate()
        {
            Vector2 ground = _stick.GroundPosition;
            float height = _stick.Height;
            float scale = _projector.ScaleAt(ground.y);
            int order = _projector.SortingOrderAt(ground.y);

            // 地面から浮いて見えるよう、棒の太さの半分だけ持ち上げる
            PlaceAlongStick(_stickRenderer, ground, height + _settings.StickThickness * 0.5f, scale);
            _stickRenderer.sortingOrder = order + 2;

            PlaceAlongStick(_shadowRenderer, ground, 0f, scale);
            _shadowRenderer.sortingOrder = order - 1;

            float fade = Mathf.Lerp(1f, _minShadowAlphaRatio, height / _shadowFadeHeight);
            _shadowRenderer.color = new Color(_shadowColor.r, _shadowColor.g, _shadowColor.b, _shadowColor.a * fade);
        }

        /// <summary>
        /// 棒の両端を別々に投影して、その間にスプライトを張る。
        /// 棒は奥を向いて飛ぶので、地面の回転角をそのまま画面に使うと縮まずに縦長の柱のように見えてしまう
        /// </summary>
        private void PlaceAlongStick(SpriteRenderer renderer, Vector2 ground, float height, float scale)
        {
            float radians = _stick.RotationDegrees * Mathf.Deg2Rad;
            Vector2 halfAxis = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * (_settings.StickLength * 0.5f);
            Vector2 back = _projector.Project(ground - halfAxis, height);
            Vector2 front = _projector.Project(ground + halfAxis, height);
            Vector2 delta = front - back;

            renderer.transform.position = (back + front) * 0.5f;
            renderer.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            // 奥向きだと画面上で極端に短くなるため、太さぶんは長さを残して「棒」と分かるようにする
            float thickness = _settings.StickThickness * scale;
            SetSize(renderer, Mathf.Max(delta.magnitude, thickness), thickness);
        }

        /// <summary>スプライトの元の大きさに関係なく、指定したワールド単位の大きさで表示する</summary>
        private static void SetSize(SpriteRenderer renderer, float width, float height)
        {
            Vector2 spriteSize = renderer.sprite.bounds.size;
            renderer.transform.localScale = new Vector3(width / spriteSize.x, height / spriteSize.y, 1f);
        }
    }
}
