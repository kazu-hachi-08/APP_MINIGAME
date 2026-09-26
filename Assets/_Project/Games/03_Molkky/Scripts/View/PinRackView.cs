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

        [Header("Color")]
        [SerializeField] private Color _standingColor = new Color(0.9f, 0.76f, 0.52f);
        // 倒れたピンが一目で分かるよう、色を大きく変える
        [SerializeField] private Color _fallenColor = new Color(0.85f, 0.45f, 0.3f);
        [SerializeField] private Color _labelColor = new Color(0.2f, 0.12f, 0.05f);

        [Header("Label")]
        [SerializeField] private int _labelFontSize = 64;
        [SerializeField] private float _labelCharacterSize = 0.035f;

        public float PinHeight => _pinHeight;
        public float PinWidth => _settings.PinRadius * 2f;
        public Color StandingColor => _standingColor;
        public Color FallenColor => _fallenColor;
        public Color LabelColor => _labelColor;
        public int LabelFontSize => _labelFontSize;
        public float LabelCharacterSize => _labelCharacterSize;

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
