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

        [SerializeField] private Color _stickColor = new Color(0.75f, 0.55f, 0.32f);
        [SerializeField] private Color _shadowColor = new Color(0f, 0f, 0f, 0.35f);

        [Tooltip("この高さで影が最も薄くなる")]
        [SerializeField] private float _shadowFadeHeight = 1.5f;

        [Tooltip("高く飛んでいるときの影の濃さの下限（割合）")]
        [SerializeField] private float _minShadowAlphaRatio = 0.3f;

        private SpriteRenderer _stickRenderer;
        private SpriteRenderer _shadowRenderer;

        private void Awake()
        {
            _shadowRenderer = CreateRenderer("Shadow", ShapeSprites.Circle, _shadowColor);
            _stickRenderer = CreateRenderer("Stick", ShapeSprites.Square, _stickColor);
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

            Transform stick = _stickRenderer.transform;
            // 地面から浮いて見えるよう、棒の太さの半分だけ持ち上げる
            stick.position = _projector.Project(ground, height + _settings.StickThickness * 0.5f);
            stick.rotation = Quaternion.Euler(0f, 0f, _stick.RotationDegrees);
            stick.localScale = new Vector3(_settings.StickLength * scale, _settings.StickThickness * scale, 1f);
            _stickRenderer.sortingOrder = order + 2;

            Transform shadow = _shadowRenderer.transform;
            shadow.position = _projector.Project(ground);
            shadow.localScale = new Vector3(_settings.StickLength * scale, _settings.StickThickness * scale, 1f);
            _shadowRenderer.sortingOrder = order - 1;

            float fade = Mathf.Lerp(1f, _minShadowAlphaRatio, height / _shadowFadeHeight);
            _shadowRenderer.color = new Color(_shadowColor.r, _shadowColor.g, _shadowColor.b, _shadowColor.a * fade);
        }
    }
}
