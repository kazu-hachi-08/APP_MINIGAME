using System;
using MiniGame.PenguinWars.Battle;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.PenguinWars
{
    /// <summary>ずかんの一覧の1マス（仕様書 §2.0）。歩くペンギンと名前・左上の数値を出し、押されたら詳細を開いてもらう</summary>
    public class ZukanCell : MonoBehaviour
    {
        private static readonly PenguinFrame[] WalkSequence = { PenguinFrame.Walk0, PenguinFrame.Walk1 };

        [SerializeField] private Button _button;
        [SerializeField] private UnitSpriteAnimator _icon;
        [SerializeField] private Text _nameLabel;
        [SerializeField] private Text _valueLabel;

        /// <summary>並べ替えのたびに ToStats() し直さないよう、作ったときに1回だけ詰め替えて持つ</summary>
        public UnitStats Stats { get; private set; }

        public void Show(PenguinUnitData data, float phaseSteps, Action<PenguinUnitData> onClick)
        {
            Stats = data.ToStats();
            // 自分が使うキャラなので、出撃ボタンと同じ左陣営（青）の絵を見せる
            _icon.Play(data.GetSprites(Side.Left), WalkSequence, phaseSteps);
            _nameLabel.text = data.DisplayName;
            _button.onClick.AddListener(() => onClick(data));
            gameObject.SetActive(true);
        }

        public void ShowValue(string value) => _valueLabel.text = value;
    }
}
