using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.Molkky
{
    /// <summary>
    /// 「○○の番」の全画面表示（§10.2）。1台を回して遊ぶとき、前の人の指がそのまま投擲にならないよう
    /// 人間の番はタップするまで待つ。NPCの番は短く出して自動で閉じる（§9.5）。
    /// </summary>
    public class TurnBannerView : MonoBehaviour
    {
        [SerializeField] private Text _titleText;
        [SerializeField] private Text _hintText;
        [SerializeField] private Button _tapArea;
        [SerializeField] private Image _background;

        [Tooltip("背景をプレイヤー色にどれだけ寄せるか。文字が読めるよう暗めに留める")]
        [Range(0f, 1f)] [SerializeField] private float _backgroundTint = 0.35f;
        [Range(0f, 1f)] [SerializeField] private float _backgroundAlpha = 0.8f;

        private bool _tapped;

        private void Awake()
        {
            _tapArea.onClick.AddListener(() => _tapped = true);
        }

        public IEnumerator Play(string title, Color color, bool waitForTap, float autoCloseSeconds)
        {
            _titleText.text = title;
            _titleText.color = color;
            // 画面全体を手番の人の色に寄せ、端末を渡された人が「自分の番だ」と一目で分かるようにする
            Color background = Color.Lerp(Color.black, color, _backgroundTint);
            background.a = _backgroundAlpha;
            _background.color = background;
            _hintText.gameObject.SetActive(waitForTap);
            _tapArea.interactable = waitForTap;
            _tapped = false;
            gameObject.SetActive(true);

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
    }
}
