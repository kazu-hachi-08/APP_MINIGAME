using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.Golf
{
    /// <summary>
    /// 3タップゲージの表示。ゲージ自体がボタンで、押すとスイングを始める。
    /// マーカー・インパクトゾーン・決めたパワーの位置を、ゲージの幅に対する割合で置く。
    /// </summary>
    public class ShotGaugeView : MonoBehaviour
    {
        [SerializeField] private ShotInput _input;
        [SerializeField] private Button _button;
        [SerializeField] private RectTransform _zone;
        [SerializeField] private RectTransform _marker;
        [SerializeField] private RectTransform _powerMark;
        [SerializeField] private Text _label;

        private const string IdleLabel = "タップでスイング";

        private void Awake()
        {
            _button.onClick.AddListener(_input.BeginSwing);
            _label.text = IdleLabel;
        }

        private void LateUpdate()
        {
            ShotGauge gauge = _input.Gauge;
            // ゾーンの幅はライで変わるので毎回合わせる
            SetRange(_zone, gauge.ZoneCenter - gauge.ZoneHalfWidth, gauge.ZoneCenter + gauge.ZoneHalfWidth);
            SetPosition(_marker, gauge.Marker);

            bool powerDecided = gauge.State != ShotGauge.GaugeState.Rising;
            _powerMark.gameObject.SetActive(gauge.State != ShotGauge.GaugeState.Idle && powerDecided);
            SetPosition(_powerMark, gauge.Power);

            _label.enabled = gauge.State == ShotGauge.GaugeState.Idle;

            // 飛んでいる間とカップイン後は押せないことが分かるように薄くする
            _button.interactable = _input.CanAim || gauge.IsSwinging;
        }

        private static void SetPosition(RectTransform rect, float ratio)
        {
            rect.anchorMin = new Vector2(ratio, rect.anchorMin.y);
            rect.anchorMax = new Vector2(ratio, rect.anchorMax.y);
        }

        private static void SetRange(RectTransform rect, float from, float to)
        {
            rect.anchorMin = new Vector2(from, rect.anchorMin.y);
            rect.anchorMax = new Vector2(to, rect.anchorMax.y);
        }
    }
}
