using System;
using MiniGame.PenguinWars.Battle;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.PenguinWars
{
    /// <summary>ドラフトの候補カード1枚（仕様書 §6）。見た目・名前・コスト・役割・能力を描くだけで、選んだ後の進行は DraftPanel が持つ</summary>
    public class DraftCard : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private Image _icon;
        [SerializeField] private Text _nameLabel;
        [SerializeField] private Text _costLabel;
        [SerializeField] private Text _roleLabel;
        [SerializeField] private Text _abilityLabel;
        [Tooltip("自分が選んだカードに出す枠")]
        [SerializeField] private GameObject _selectedFrame;
        [Tooltip("選ばなかったカードを暗くする")]
        [SerializeField] private GameObject _dimmer;

        public void Bind(Action onClick)
        {
            _button.onClick.AddListener(() => onClick());
        }

        /// <param name="data">カタログに無い No（ビルドのバージョン違い）なら null。名前に No だけ出す</param>
        public void Show(int unitNo, PenguinUnitData data)
        {
            // 自分が使うキャラなので、出撃ボタンと同じ左陣営（青）の絵を見せる
            UnitLabels.SetIcon(_icon, data, Side.Left);
            _nameLabel.text = UnitLabels.Name(data, unitNo);

            UnitStats stats = data != null ? data.ToStats() : null;
            _costLabel.text = stats != null ? stats.Cost.ToString() : string.Empty;
            _roleLabel.text = stats != null ? UnitLabels.Role(stats.Role) : string.Empty;
            _abilityLabel.text = stats != null ? UnitLabels.AbilitiesWithArea(stats) : string.Empty;
            SetState(true, false);
        }

        /// <param name="selectable">まだ選べるか</param>
        /// <param name="selected">自分が選んだカードか</param>
        public void SetState(bool selectable, bool selected)
        {
            _button.interactable = selectable;
            _selectedFrame.SetActive(selected);
            _dimmer.SetActive(!selectable && !selected);
        }
    }
}
