using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.Golf
{
    /// <summary>
    /// 今のスピンを出し、タップで なし → バック → トップ を切り替えるボタン（§7.7 スピン）。
    /// </summary>
    public class SpinButtonView : MonoBehaviour
    {
        [SerializeField] private ShotInput _input;
        [SerializeField] private ClubSelector _clubs;
        [SerializeField] private Button _button;
        [SerializeField] private Text _label;

        private void Awake()
        {
            _button.onClick.AddListener(_input.CycleSpin);
        }

        private void LateUpdate()
        {
            // パターは転がすだけでスピンが効かないので、押せないことが分かるように薄くする
            bool isPutter = _clubs.Current.IsPutter;
            _label.text = $"スピン\n{(isPutter ? "―" : SpinName(_input.Spin))}";
            _button.interactable = _input.CanAim && !isPutter;
        }

        private static string SpinName(ShotSpin spin)
        {
            switch (spin)
            {
                case ShotSpin.Back: return "バック";
                case ShotSpin.Top: return "トップ";
                default: return "なし";
            }
        }
    }
}
