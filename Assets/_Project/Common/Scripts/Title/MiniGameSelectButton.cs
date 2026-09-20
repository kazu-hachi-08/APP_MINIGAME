using MiniGame.Common.Audio;
using MiniGame.Common.Scene;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.Common.Title
{
    /// <summary>
    /// タイトル画面でミニゲームを選択・起動するためのUIボタン
    /// </summary>
    public class MiniGameSelectButton : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private string _targetSceneName = SceneNames.Soccer;
        [SerializeField] private string _gameTitle = "2D サッカー";
        [SerializeField] private bool _isPlayable = true;

        [Header("UI Components")]
        [SerializeField] private Button _button;
        [SerializeField] private Text _titleText;
        [SerializeField] private GameObject _lockOverlay;

        private void Awake()
        {
            if (_button == null) _button = GetComponent<Button>();
            if (_button != null)
            {
                _button.onClick.AddListener(OnClickButton);
            }

            UpdateUI();
        }

        private void UpdateUI()
        {
            if (_titleText != null)
            {
                _titleText.text = _gameTitle;
            }

            if (_lockOverlay != null)
            {
                _lockOverlay.SetActive(!_isPlayable);
            }

            if (_button != null)
            {
                _button.interactable = _isPlayable;
            }
        }

        public void Setup(string title, string sceneName, bool isPlayable)
        {
            _gameTitle = title;
            _targetSceneName = sceneName;
            _isPlayable = isPlayable;
            UpdateUI();
        }

        private void OnClickButton()
        {
            if (!_isPlayable) return;

            if (AudioManager.HasInstance)
            {
                AudioManager.Instance.PlaySe(SeId.ButtonClick);
            }

            if (SceneLoader.HasInstance)
            {
                SceneLoader.Instance.LoadScene(_targetSceneName);
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(_targetSceneName);
            }
        }
    }
}
