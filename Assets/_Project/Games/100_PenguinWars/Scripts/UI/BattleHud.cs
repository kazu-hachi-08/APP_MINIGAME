using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.PenguinWars
{
    /// <summary>画面上部の時間表示・お知らせ（なだれ注意）と、中央のメッセージ（START! など）。ボス出現は BossWarningBanner</summary>
    public class BattleHud : MonoBehaviour
    {
        private const int SecondsPerMinute = 60;

        [SerializeField] private Text _timeLabel;
        [SerializeField] private Text _messageLabel;
        [SerializeField] private Text _noticeLabel;

        private float _noticeTimer;

        private void Update()
        {
            if (_noticeTimer <= 0f) return;

            _noticeTimer -= Time.deltaTime;
            if (_noticeTimer <= 0f) _noticeLabel.gameObject.SetActive(false);
        }

        /// <summary>
        /// 上部の小さなお知らせ（なだれ注意）。中央のメッセージ（START!・TIME UP!）とは別の欄にして、
        /// 試合の区切りの表示を消してしまわないようにする
        /// </summary>
        public void ShowNotice(string text, float duration)
        {
            _noticeLabel.text = text;
            _noticeLabel.gameObject.SetActive(true);
            _noticeTimer = duration;
        }

        public void HideNotice()
        {
            _noticeTimer = 0f;
            _noticeLabel.gameObject.SetActive(false);
        }

        public void SetElapsed(float seconds)
        {
            _timeLabel.text = $"経過 {FormatTime(Mathf.FloorToInt(seconds))}";
        }

        /// <summary>オンライン対戦（§2.2）の残り時間用</summary>
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

        /// <summary>mm:ss。リザルトのクリアタイムでも同じ書式を使う</summary>
        public static string FormatTime(int totalSeconds)
        {
            int total = Mathf.Max(0, totalSeconds);
            return $"{total / SecondsPerMinute:00}:{total % SecondsPerMinute:00}";
        }
    }
}
