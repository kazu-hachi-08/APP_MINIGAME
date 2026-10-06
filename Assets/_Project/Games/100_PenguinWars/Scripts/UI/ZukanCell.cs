using System;
using MiniGame.PenguinWars.Battle;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.PenguinWars
{
    /// <summary>
    /// ずかん・編成画面の一覧の1マス（仕様書 §2.0）。歩くペンギンと名前・左上の数値を出し、押されたら呼び出し元に知らせる。
    /// 編成画面でも同じ見た目にしたいので使い回す
    /// </summary>
    public class ZukanCell : MonoBehaviour
    {
        private const string LockedName = "？？？";

        [SerializeField] private Button _button;
        [SerializeField] private UnitSpriteAnimator _icon;
        [SerializeField] private Text _nameLabel;
        [SerializeField] private Text _valueLabel;
        [Tooltip("編成画面で、もう枠に入れたキャラを暗くする")]
        [SerializeField] private GameObject _dimmer;

        private string _displayName;

        /// <summary>並べ替えのたびに ToStats() し直さないよう、作ったときに1回だけ詰め替えて持つ</summary>
        public UnitStats Stats { get; private set; }
        public bool IsLocked { get; private set; }

        public void Show(PenguinUnitData data, float phaseSteps, Action<PenguinUnitData> onClick)
        {
            Stats = data.ToStats();
            _displayName = data.DisplayName;
            // 自分が使うキャラなので、出撃ボタンと同じ左陣営（青）の絵を見せる
            _icon.Play(data.GetSprites(Side.Left), UnitSpriteAnimator.WalkFrames, phaseSteps);
            _nameLabel.text = _displayName;
            // Show を呼び直しても、押したときの処理が重ならないようにする
            _button.onClick.RemoveAllListeners();
            _button.onClick.AddListener(() => onClick(data));
            SetDimmed(false);
            gameObject.SetActive(true);
        }

        /// <summary>未解放は黒いシルエット＋「？？？」にして押せなくする（何が仲間になるかの楽しみを残すため）</summary>
        public void SetLocked(bool locked)
        {
            IsLocked = locked;
            _icon.SetTint(locked ? Color.black : Color.white);
            _nameLabel.text = locked ? LockedName : _displayName;
            _button.interactable = !locked;
            if (locked) _valueLabel.text = LockedName;
        }

        public void SetDimmed(bool dimmed)
        {
            if (_dimmer != null) _dimmer.SetActive(dimmed);
        }

        public void ShowValue(string value)
        {
            if (!IsLocked) _valueLabel.text = value;
        }
    }
}
