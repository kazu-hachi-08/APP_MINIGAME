using System;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.PenguinWars
{
    /// <summary>
    /// 出撃ボタン1つ（仕様書 §7.1）。アイコン・名前・コスト・再生産ゲージを描くだけで、出撃できるかの判定は UnitButtonBar が BattleWorld から読んで渡す
    /// </summary>
    public class UnitButton : MonoBehaviour
    {
        // キャラNoごとに色相をずらして、ドット絵（Phase 6）ができるまで見分けられるようにする
        private const float HueStepPerUnitNo = 0.618f;
        private const float IconSaturation = 0.55f;
        private const float IconBrightness = 0.95f;

        [SerializeField] private Button _button;
        [SerializeField] private Image _icon;
        [SerializeField] private Text _nameLabel;
        [SerializeField] private Text _costLabel;
        [SerializeField] private GameObject _cooldownGauge;
        [Tooltip("再生産の残りに合わせて横幅を縮める（右端のアンカーを動かす）")]
        [SerializeField] private RectTransform _cooldownFill;
        [SerializeField] private GameObject _dimmer;

        public void Bind(Action onClick)
        {
            _button.onClick.AddListener(() => onClick());
        }

        public void SetUnit(int unitNo, string displayName, int cost)
        {
            _icon.enabled = true;
            _icon.color = IconColor(unitNo);
            _nameLabel.text = displayName;
            _costLabel.text = cost.ToString();
        }

        /// <summary>仮アイコンの色。編成発表（DeckIntroPanel）でも同じ色にして、ボタンと見比べられるようにする</summary>
        public static Color IconColor(int unitNo)
        {
            return Color.HSVToRGB(unitNo * HueStepPerUnitNo % 1f, IconSaturation, IconBrightness);
        }

        /// <summary>編成が10体に満たないときの空き枠</summary>
        public void SetEmpty()
        {
            _icon.enabled = false;
            _nameLabel.text = string.Empty;
            _costLabel.text = string.Empty;
            Refresh(false, 0f);
        }

        /// <param name="cooldownRatio">再生産の残り割合（1=出した直後、0=出せる）</param>
        public void Refresh(bool canSpawn, float cooldownRatio)
        {
            _dimmer.SetActive(!canSpawn);
            bool coolingDown = cooldownRatio > 0f;
            _cooldownGauge.SetActive(coolingDown);
            if (coolingDown) _cooldownFill.anchorMax = new Vector2(cooldownRatio, 1f);
        }
    }
}
