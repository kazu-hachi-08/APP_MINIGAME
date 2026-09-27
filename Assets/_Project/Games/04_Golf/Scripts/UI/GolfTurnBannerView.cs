using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.Golf
{
    /// <summary>
    /// 「○○の番」やホール開始の全画面表示（§11.2）。1台を回して遊ぶので、端末を受け取った人がタップするまで打てないようにする。
    /// 前の人のショット結果（カップイン・ギブアップ）も一緒に出す。NPC の番は受け渡しが要らないので、短く出して自動で閉じる（§10.4）。
    /// </summary>
    public class GolfTurnBannerView : MonoBehaviour
    {
        [SerializeField] private Text _titleText;
        [SerializeField] private Text _detailText;
        [SerializeField] private Text _hintText;
        [SerializeField] private Button _tapArea;
        [SerializeField] private Image _background;

        [Tooltip("背景をプレイヤー色にどれだけ寄せるか。文字が読めるよう暗めに留める")]
        [Range(0f, 1f)] [SerializeField] private float _backgroundTint = 0.35f;
        [Range(0f, 1f)] [SerializeField] private float _backgroundAlpha = 0.8f;

        private const string TapHint = "タップで開始";

        private bool _tapped;

        private void Awake()
        {
            _tapArea.onClick.AddListener(() => _tapped = true);
        }

        /// <summary>タップされるまで出す</summary>
        public IEnumerator Play(string title, string detail, Color color)
        {
            Show(title, detail, color, TapHint);
            yield return new WaitUntil(() => _tapped);
            gameObject.SetActive(false);
        }

        /// <summary>seconds 秒で自動で閉じる（タップでも閉じる）。hint には「タップで開始」の代わりに出す文字を渡す</summary>
        public IEnumerator PlayAuto(string title, string detail, Color color, string hint, float seconds)
        {
            Show(title, detail, color, hint);
            float closeAt = Time.time + seconds;
            yield return new WaitUntil(() => _tapped || Time.time >= closeAt);
            gameObject.SetActive(false);
        }

        private void Show(string title, string detail, Color color, string hint)
        {
            _titleText.text = title;
            _titleText.color = color;
            _detailText.text = detail;
            _hintText.text = hint;

            Color background = Color.Lerp(Color.black, color, _backgroundTint);
            background.a = _backgroundAlpha;
            _background.color = background;

            _tapped = false;
            gameObject.SetActive(true);
        }
    }
}
