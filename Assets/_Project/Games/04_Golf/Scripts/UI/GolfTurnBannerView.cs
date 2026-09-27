using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.Golf
{
    /// <summary>
    /// 「○○の番」やホール開始の全画面表示（§11.2）。1台を回して遊ぶので、端末を受け取った人がタップするまで打てないようにする。
    /// 前の人のショット結果（カップイン・ギブアップ）も一緒に出す。
    /// </summary>
    public class GolfTurnBannerView : MonoBehaviour
    {
        [SerializeField] private Text _titleText;
        [SerializeField] private Text _detailText;
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

        public IEnumerator Play(string title, string detail, Color color)
        {
            _titleText.text = title;
            _titleText.color = color;
            _detailText.text = detail;

            Color background = Color.Lerp(Color.black, color, _backgroundTint);
            background.a = _backgroundAlpha;
            _background.color = background;

            _tapped = false;
            gameObject.SetActive(true);
            yield return new WaitUntil(() => _tapped);
            gameObject.SetActive(false);
        }
    }
}
