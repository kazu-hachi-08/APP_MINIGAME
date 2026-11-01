using UnityEngine;

namespace MiniGame.Molkky
{
    /// <summary>
    /// PinRack が作ったロジック用のピンそれぞれに、見た目用の PinView を1つずつ付ける。
    /// ピンの見た目の設定（大きさ・色）もここにまとめる。
    /// </summary>
    public class PinRackView : MonoBehaviour
    {
        [SerializeField] private PinRack _pinRack;
        [SerializeField] private DepthProjector _projector;
        [SerializeField] private MolkkyPhysicsSettings _settings;

        [Header("Size (ground units)")]
        [Tooltip("立っているピンの見た目の高さ。前の列に隠れすぎないよう実物より低めにしている")]
        [SerializeField] private float _pinHeight = 0.55f;

        [Header("Sprite（未設定なら単純な四角で代用する）")]
        [SerializeField] private Sprite _standingSprite;
        [SerializeField] private Sprite _fallenSprite;

        [Header("Color（スプライトへの乗算色）")]
        [SerializeField] private Color _standingColor = Color.white;
        // 倒れたピンが一目で分かるよう、赤みを強く乗せる
        [SerializeField] private Color _fallenColor = new Color(1f, 0.6f, 0.5f);
        [SerializeField] private Color _labelColor = new Color(0.2f, 0.12f, 0.05f);

        [Header("Special Pin（ゴールデン / 爆弾モード）")]
        [SerializeField] private Color _goldenColor = new Color(1f, 0.82f, 0.15f);
        // 爆弾は黒っぽい本体に赤い数字にして、遠目にも「触れてはいけない」と分かるようにする
        [SerializeField] private Color _bombColor = new Color(0.3f, 0.3f, 0.35f);
        [SerializeField] private Color _bombLabelColor = new Color(1f, 0.3f, 0.25f);

        [Header("Stand Up")]
        [Tooltip("立て直すときに、倒れた姿勢から起き上がるアニメーションの時間")]
        [SerializeField] private float _standUpDuration = 0.3f;

        [Header("Label")]
        [SerializeField] private int _labelFontSize = 64;
        [SerializeField] private float _labelCharacterSize = 0.035f;

        public Sprite StandingSprite => _standingSprite != null ? _standingSprite : ShapeSprites.Square;
        public Sprite FallenSprite => _fallenSprite != null ? _fallenSprite : ShapeSprites.Square;
        public float StandUpDuration => _standUpDuration;
        public float PinHeight => _pinHeight;
        public float PinWidth => _settings.PinRadius * 2f;
        public Color LabelColor => _labelColor;
        public int LabelFontSize => _labelFontSize;
        public float LabelCharacterSize => _labelCharacterSize;

        /// <summary>
        /// 本体の色。特殊ピンは倒れても特殊色のままにして、倒したのが金 / 爆弾だったと見て分かるようにする
        /// </summary>
        public Color BodyColorOf(Pin pin)
        {
            switch (SpecialKindOf(pin))
            {
                case MolkkyRuleMode.Golden: return _goldenColor;
                case MolkkyRuleMode.Bomb: return _bombColor;
                default: return pin.IsFallen ? _fallenColor : _standingColor;
            }
        }

        public Color LabelColorOf(Pin pin)
        {
            return SpecialKindOf(pin) == MolkkyRuleMode.Bomb ? _bombLabelColor : _labelColor;
        }

        private MolkkyRuleMode SpecialKindOf(Pin pin)
        {
            return pin.Number == _pinRack.SpecialPinNumber ? _pinRack.SpecialRule : MolkkyRuleMode.Normal;
        }

        private void Start()
        {
            // PinRack.Awake でピンが作られた後に見た目を付ける
            foreach (Pin pin in _pinRack.Pins)
            {
                var obj = new GameObject($"PinView_{pin.Number}");
                obj.transform.SetParent(transform, false);
                obj.AddComponent<PinView>().Bind(pin, _projector, this);
            }
        }
    }
}
