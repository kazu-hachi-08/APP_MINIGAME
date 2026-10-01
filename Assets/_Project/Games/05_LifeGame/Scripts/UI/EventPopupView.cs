using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.LifeGame
{
    /// <summary>
    /// 止まったマスの結果などをカード風に出し、タップで閉じる。NPCの手番は自動で閉じる（仕様書 §3.3）。
    /// 上から 名札（席の色・顔・名前）→ マスのアイコンと名前 → テーマの一言 → お金などの行（増減で色分け）の順に並べる
    /// </summary>
    public class EventPopupView : MonoBehaviour
    {
        public const float WaitForTap = 0f;

        // EaseOutBack の標準値。1割ほど大きくなってから戻り、ポンと飛び出して見える
        private const float EaseOvershoot = 1.70158f;

        [Header("Parts")]
        [SerializeField] private RectTransform _box;
        [SerializeField] private Button _tapArea;
        [SerializeField] private Image _nameplate;
        [SerializeField] private Image _faceImage;
        [SerializeField] private Text _nameText;
        [SerializeField] private GameObject _titleRow;
        [SerializeField] private Image _iconFrame;
        [SerializeField] private Image _iconImage;
        [SerializeField] private Text _titleText;
        [SerializeField] private Text _flavorText;
        [SerializeField] private GameObject _divider;
        [SerializeField] private Text _linesText;
        [SerializeField] private Text _hintText;

        [Header("Layout")]
        [SerializeField] private float _titleWidthWithIcon = 620f;
        [Tooltip("アイコンが無いとき（順位発表）はタイトルを箱の幅いっぱいにして中央に置く")]
        [SerializeField] private float _titleWidthAlone = 820f;

        [Header("Motion")]
        [SerializeField] private float _popDuration = 0.25f;
        [SerializeField] private float _popStartScale = 0.6f;
        [SerializeField] private float _hintBlinkSpeed = 1.5f;
        [Range(0f, 1f)] [SerializeField] private float _hintMinAlpha = 0.3f;

        private float _elapsed;
        private bool _tapped;

        private void Awake()
        {
            _tapArea.onClick.AddListener(OnTap);
        }

        /// <param name="autoCloseSeconds">WaitForTap ならタップまで待つ。それ以外はその秒数で閉じる（タップでも閉じられる）</param>
        public IEnumerator Play(EventPopupContent content, float autoCloseSeconds = WaitForTap)
        {
            bool autoClose = autoCloseSeconds > WaitForTap;
            Setup(content);
            _hintText.gameObject.SetActive(!autoClose);
            _tapped = false;
            _elapsed = 0f;
            gameObject.SetActive(true);
            // 中身によって箱の高さが変わるので、最初のフレームに前回の大きさで出ないよう先に並べ直す
            LayoutRebuilder.ForceRebuildLayoutImmediate(_box);

            while (!_tapped && !(autoClose && _elapsed >= autoCloseSeconds))
            {
                Animate(_elapsed);
                yield return null;
                _elapsed += Time.deltaTime;
            }

            gameObject.SetActive(false);
        }

        private void OnTap()
        {
            // 飛び出している途中のタップは数えない（前の操作の連打で、読む前に閉じてしまわないように）
            if (_elapsed >= _popDuration) _tapped = true;
        }

        private void Setup(EventPopupContent content)
        {
            SetupNameplate(content);
            SetupTitle(content);

            bool hasFlavor = !string.IsNullOrEmpty(content.Flavor);
            _flavorText.gameObject.SetActive(hasFlavor);
            _flavorText.text = content.Flavor;

            bool hasLines = content.Lines.Count > 0;
            _linesText.gameObject.SetActive(hasLines);
            _linesText.text = ColoredLines(content.Lines);
            // マスの説明とお金の行の間だけに線を引き、「何が起きたか」と「その結果」を分けて読めるようにする
            _divider.SetActive(hasLines && (content.Title != null || hasFlavor));
        }

        private void SetupNameplate(EventPopupContent content)
        {
            _nameplate.gameObject.SetActive(content.HasNameplate);
            if (!content.HasNameplate) return;

            _nameplate.color = content.SeatColor;
            _nameText.text = content.PlayerName;
            _faceImage.sprite = content.Face;
            _faceImage.gameObject.SetActive(content.Face != null);
        }

        private void SetupTitle(EventPopupContent content)
        {
            _titleRow.SetActive(content.Title != null);
            if (content.Title == null) return;

            bool hasIcon = content.Icon != null;
            _iconFrame.gameObject.SetActive(hasIcon);
            _iconFrame.color = content.IconColor;
            _iconImage.sprite = content.Icon;

            _titleText.text = content.Title;
            _titleText.alignment = hasIcon ? TextAnchor.MiddleLeft : TextAnchor.MiddleCenter;
            RectTransform titleRect = _titleText.rectTransform;
            titleRect.sizeDelta = new Vector2(hasIcon ? _titleWidthWithIcon : _titleWidthAlone, titleRect.sizeDelta.y);
        }

        private static string ColoredLines(List<(string text, Color color)> lines)
        {
            var colored = new List<string>(lines.Count);
            foreach ((string text, Color color) in lines)
            {
                colored.Add($"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{text}</color>");
            }

            return string.Join("\n", colored);
        }

        private void Animate(float elapsed)
        {
            float t = Mathf.Clamp01(elapsed / _popDuration);
            _box.localScale = Vector3.one * Mathf.LerpUnclamped(_popStartScale, 1f, EaseOutBack(t));

            Color hint = _hintText.color;
            hint.a = Mathf.Lerp(_hintMinAlpha, 1f, Mathf.PingPong(elapsed * _hintBlinkSpeed, 1f));
            _hintText.color = hint;
        }

        private static float EaseOutBack(float t)
        {
            float u = t - 1f;
            return 1f + (EaseOvershoot + 1f) * u * u * u + EaseOvershoot * u * u;
        }
    }
}
