using MiniGame.Common.Audio;
using MiniGame.Common.Core;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.Common.UI
{
    /// <summary>
    /// 画面上のポーズボタン
    /// スマートフォンにはEscキーが無いため、タッチでポーズできる導線を用意する
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class PauseButton : MonoBehaviour
    {
        [SerializeField] private BaseMiniGameManager _gameManager;

        private void Awake()
        {
            GetComponent<Button>().onClick.AddListener(OnClicked);
        }

        private void OnClicked()
        {
            if (_gameManager == null) return;

            if (AudioManager.HasInstance)
            {
                AudioManager.Instance.PlaySe(SeId.ButtonClick);
            }

            // ポーズ可否（リザルト中など）の判定は BaseMiniGameManager 側に任せる
            _gameManager.PauseGame();
        }
    }
}
