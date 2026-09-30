using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.LifeGame
{
    /// <summary>止まったマスの結果などの文面を出し、タップで閉じる。NPCの手番は自動で閉じる（仕様書 §3.3）</summary>
    public class EventPopupView : MonoBehaviour
    {
        public const float WaitForTap = 0f;

        [SerializeField] private Text _bodyText;
        [SerializeField] private Button _tapArea;
        [SerializeField] private Text _hintText;

        private bool _tapped;

        private void Awake()
        {
            _tapArea.onClick.AddListener(() => _tapped = true);
        }

        /// <param name="autoCloseSeconds">WaitForTap ならタップまで待つ。それ以外はその秒数で閉じる（タップでも閉じられる）</param>
        public IEnumerator Play(string body, float autoCloseSeconds = WaitForTap)
        {
            bool autoClose = autoCloseSeconds > WaitForTap;
            _bodyText.text = body;
            _hintText.gameObject.SetActive(!autoClose);
            _tapped = false;
            gameObject.SetActive(true);

            float closeAt = Time.time + autoCloseSeconds;
            yield return new WaitUntil(() => _tapped || (autoClose && Time.time >= closeAt));

            gameObject.SetActive(false);
        }
    }
}
