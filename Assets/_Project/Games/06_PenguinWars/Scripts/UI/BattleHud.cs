using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.PenguinWars
{
    /// <summary>画面上部の時間表示と、中央のメッセージ（START! など）</summary>
    public class BattleHud : MonoBehaviour
    {
        private const int SecondsPerMinute = 60;

        [SerializeField] private Text _timeLabel;
        [SerializeField] private Text _messageLabel;

        public void SetElapsed(float seconds)
        {
            _timeLabel.text = $"生存 {FormatTime(Mathf.FloorToInt(seconds))}";
        }

        /// <summary>オンライン対戦（Phase 10）の残り時間用</summary>
        public void SetRemaining(float seconds)
        {
            // 切り捨てだと 0:00 と出たまま1秒近く試合が続くので、0:00 になった瞬間に TIME UP になるよう切り上げる
            _timeLabel.text = $"残り {FormatTime(Mathf.CeilToInt(seconds))}";
        }

        public void ShowMessage(string message)
        {
            _messageLabel.text = message;
            _messageLabel.gameObject.SetActive(true);
        }

        public void HideMessage()
        {
            _messageLabel.gameObject.SetActive(false);
        }

        private static string FormatTime(int totalSeconds)
        {
            int total = Mathf.Max(0, totalSeconds);
            return $"{total / SecondsPerMinute:00}:{total % SecondsPerMinute:00}";
        }
    }
}
