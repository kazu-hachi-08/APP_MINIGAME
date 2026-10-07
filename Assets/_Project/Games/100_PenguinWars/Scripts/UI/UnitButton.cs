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
        [Tooltip("じぶんペンギンのボタンの色（どれが自分で作った子か一目で分かるように）")]
        [SerializeField] private Color _customColor = new Color(0.42f, 0.24f, 0.52f, 0.92f);

        private Color _normalColor;

        public void Bind(Action onClick)
        {
            // 通常の色は Scene 生成時の色をそのまま使う（SceneBuilder と二重に持たないため）。
            // Awake だと非アクティブのまま SetEmpty が先に呼ばれ得るので、必ず最初に呼ばれる Bind で覚える
            _normalColor = _button.image.color;
            _button.onClick.AddListener(() => onClick());
        }

        /// <param name="icon">絵が未生成なら null（名前とコストだけ出す）</param>
        /// <param name="isCustom">じぶんペンギンならボタンの色を変える</param>
        public void SetUnit(Sprite icon, string displayName, int cost, bool isCustom)
        {
            _button.image.color = isCustom ? _customColor : _normalColor;
            _icon.sprite = icon;
            _icon.enabled = icon != null;
            _nameLabel.text = displayName;
            _costLabel.text = cost.ToString();
        }

        /// <summary>編成が10体に満たないときの空き枠</summary>
        public void SetEmpty()
        {
            _button.image.color = _normalColor;
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
