using MiniGame.Common.Audio;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.PenguinWars
{
    /// <summary>
    /// タイトルから開く音の設定（BGM ON/OFF・BGM音量・SE音量）。値は共通の AudioManager が PlayerPrefs に保存するので、
    /// ポーズ画面のスライダーや次回の起動とも同じ値になる
    /// </summary>
    public class PenguinWarsSettingsPanel : MonoBehaviour
    {
        [SerializeField] private Button _bgmToggleButton;
        [SerializeField] private Text _bgmToggleLabel;
        [SerializeField] private Slider _bgmSlider;
        [SerializeField] private Slider _seSlider;
        [SerializeField] private Button _closeButton;

        [SerializeField] private string _bgmOnLabel = "BGM：ON";
        [SerializeField] private string _bgmOffLabel = "BGM：OFF";
        [SerializeField] private Color _bgmOnColor = new Color(0.2f, 0.6f, 0.35f);
        [SerializeField] private Color _bgmOffColor = new Color(0.4f, 0.4f, 0.45f);

        private void Awake()
        {
            _bgmToggleButton.onClick.AddListener(ToggleBgm);
            _bgmSlider.onValueChanged.AddListener(SetBgmVolume);
            _seSlider.onValueChanged.AddListener(SetSeVolume);
            _closeButton.onClick.AddListener(Close);
        }

        /// <summary>他の画面（ポーズ）で変えた値もあるので、開くたびに今の値を読み直す</summary>
        private void OnEnable()
        {
            if (!AudioManager.HasInstance) return;

            AudioManager audio = AudioManager.Instance;
            _bgmSlider.SetValueWithoutNotify(audio.BgmVolume);
            _seSlider.SetValueWithoutNotify(audio.SeVolume);
            RefreshBgmToggle(audio.IsBgmOff);
        }

        private void OnDisable()
        {
            if (AudioManager.HasInstance) AudioManager.Instance.SaveAudioSettings();
        }

        public void Show() => gameObject.SetActive(true);

        public void Hide() => gameObject.SetActive(false);

        private void ToggleBgm()
        {
            if (!AudioManager.HasInstance) return;

            AudioManager audio = AudioManager.Instance;
            audio.SetBgmOff(!audio.IsBgmOff);
            audio.PlaySe(SeId.ButtonClick);
            RefreshBgmToggle(audio.IsBgmOff);
        }

        private void RefreshBgmToggle(bool isOff)
        {
            _bgmToggleLabel.text = isOff ? _bgmOffLabel : _bgmOnLabel;
            _bgmToggleButton.image.color = isOff ? _bgmOffColor : _bgmOnColor;
            // OFF 中に音量だけ動かしても聞こえず紛らわしいので、触れないようにする
            _bgmSlider.interactable = !isOff;
        }

        private static void SetBgmVolume(float value)
        {
            if (AudioManager.HasInstance) AudioManager.Instance.SetBgmVolume(value);
        }

        private static void SetSeVolume(float value)
        {
            if (AudioManager.HasInstance) AudioManager.Instance.SetSeVolume(value);
        }

        private void Close()
        {
            PenguinUiSound.Click();
            Hide();
        }
    }
}
