using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.PenguinWars
{
    /// <summary>画面上部の時間表示・敵レベルUP表示と、中央のメッセージ（START! など）</summary>
    public class BattleHud : MonoBehaviour
    {
        private const int SecondsPerMinute = 60;

        [SerializeField] private Text _timeLabel;
        [SerializeField] private Text _messageLabel;
        [SerializeField] private Text _levelUpLabel;
        [SerializeField] private float _levelUpDisplayTime = 2f;

        private float _levelUpTimer;

        private void Update()
        {
            if (_levelUpTimer <= 0f) return;

            _levelUpTimer -= Time.deltaTime;
            if (_levelUpTimer <= 0f) _levelUpLabel.gameObject.SetActive(false);
        }

        /// <summary>エンドレスの敵が強くなったことを知らせる（仕様書 §9）。しばらくしたら自動で消える</summary>
        public void ShowLevelUp(int level)
        {
            _levelUpLabel.text = $"LEVEL {level}!";
            _levelUpLabel.gameObject.SetActive(true);
            _levelUpTimer = _levelUpDisplayTime;
        }

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

        /// <summary>mm:ss。リザルトの生存時間でも同じ書式を使う</summary>
        public static string FormatTime(int totalSeconds)
        {
            int total = Mathf.Max(0, totalSeconds);
            return $"{total / SecondsPerMinute:00}:{total % SecondsPerMinute:00}";
        }
    }
}
