using MiniGame.Common.Audio;
using MiniGame.Common.UI;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.Common.Title
{
    /// <summary>
    /// タイトル画面の制御コントローラー
    /// </summary>
    public class TitleController : MonoBehaviour
    {
        [Header("Audio")]
        [SerializeField] private BgmId _titleBgm = BgmId.TitleBgm;
        [SerializeField] private bool _autoPlayBgm = true;

        [Header("UI Elements")]
        [SerializeField] private Text _versionText;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Button _quitButton;

        private void Start()
        {
            if (_versionText != null)
            {
                _versionText.text = $"v{Application.version}";
            }

            if (_settingsButton != null)
            {
                _settingsButton.onClick.AddListener(OnSettingsClicked);
            }

            if (_quitButton != null)
            {
#if UNITY_IOS
                // iOSはアプリ自身で終了するボタンがガイドライン上非推奨のため表示しない
                _quitButton.gameObject.SetActive(false);
#else
                _quitButton.onClick.AddListener(OnQuitClicked);
#endif
            }

            if (_autoPlayBgm && AudioManager.HasInstance)
            {
                AudioManager.Instance.PlayBgm(_titleBgm);
            }
        }

        private void OnSettingsClicked()
        {
            if (AudioManager.HasInstance)
            {
                AudioManager.Instance.PlaySe(SeId.ButtonClick);
            }

            if (UIManager.HasInstance)
            {
                // 設定ダイアログとしてポーズダイアログを再利用または確認ダイアログ表示
                UIManager.Instance.ShowPauseDialog(
                    onResume: null,
                    onRestart: null,
                    onTitle: null
                );
            }
        }

        private void OnQuitClicked()
        {
            if (AudioManager.HasInstance)
            {
                AudioManager.Instance.PlaySe(SeId.ButtonClick);
            }

            if (UIManager.HasInstance)
            {
                UIManager.Instance.ShowConfirmDialog(
                    title: "終了確認",
                    message: "ゲームを終了しますか？",
                    onConfirm: () =>
                    {
#if UNITY_EDITOR
                        UnityEditor.EditorApplication.isPlaying = false;
#else
                        Application.Quit();
#endif
                    }
                );
            }
        }
    }
}
