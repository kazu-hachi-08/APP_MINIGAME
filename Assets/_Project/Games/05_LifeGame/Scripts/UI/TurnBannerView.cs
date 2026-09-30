using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.LifeGame
{
    /// <summary>
    /// 「○○の番」の全画面表示（仕様書 §10.1）。1台を回して遊ぶので、人間の番はタップするまで待ち、
    /// 端末を受け取った人が自分で始められるようにする。NPCの番は短く出して自動で閉じる。
    /// </summary>
    public class TurnBannerView : MonoBehaviour
    {
        [SerializeField] private Text _titleText;
        [SerializeField] private Text _hintText;
        [SerializeField] private Button _tapArea;
        [SerializeField] private Image _background;

        [Tooltip("背景を席の色にどれだけ寄せるか。文字が読めるよう暗めに留める")]
        [Range(0f, 1f)] [SerializeField] private float _backgroundTint = 0.35f;
        [Range(0f, 1f)] [SerializeField] private float _backgroundAlpha = 0.8f;

        private bool _tapped;

        private void Awake()
        {
            _tapArea.onClick.AddListener(() => _tapped = true);
        }

        public IEnumerator Play(string title, Color color, bool waitForTap, float autoCloseSeconds)
        {
            Setup(title, color, waitForTap);

            if (waitForTap)
            {
                yield return new WaitUntil(() => _tapped);
            }
            else
            {
                yield return new WaitForSeconds(autoCloseSeconds);
            }

            gameObject.SetActive(false);
        }

        private void Setup(string title, Color color, bool waitForTap)
        {
            _titleText.text = title;
            _titleText.color = color;
            Color background = Color.Lerp(Color.black, color, _backgroundTint);
            background.a = _backgroundAlpha;
            _background.color = background;
            _hintText.gameObject.SetActive(waitForTap);
            _tapArea.interactable = waitForTap;
            _tapped = false;
            gameObject.SetActive(true);
        }
    }
}
