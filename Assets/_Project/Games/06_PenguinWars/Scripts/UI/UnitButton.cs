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

        /// <param name="icon">絵が未生成なら null（名前とコストだけ出す）</param>
        public void SetUnit(Sprite icon, string displayName, int cost)
        {
            _icon.sprite = icon;
            _icon.enabled = icon != null;
            _nameLabel.text = displayName;
            _costLabel.text = cost.ToString();
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
