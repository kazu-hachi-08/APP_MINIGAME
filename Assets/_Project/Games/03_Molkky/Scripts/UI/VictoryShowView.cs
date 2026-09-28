using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.Molkky
{
    /// <summary>
    /// 勝ったキャラが立ち絵とセリフで喜ぶ演出（キャラクター計画 Phase C4）。結果画面の前に挟む。
    /// 立ち絵が下からスライドインして跳ね、吹き出しでセリフを出す。タップで最後の状態まで飛ばせる。
    /// 終わった後も表示は残し、結果画面の背景として勝者が見えるようにする。
    /// </summary>
    public class VictoryShowView : MonoBehaviour
    {
        [SerializeField] private Image _background;
        [SerializeField] private Button _tapArea;
        [SerializeField] private RectTransform _portraitRect;
        [SerializeField] private Image _portrait;
        [SerializeField] private Text _nameText;
        [SerializeField] private RectTransform _bubbleRect;
        [SerializeField] private Text _lineText;

        [Tooltip("背景を勝者の色にどれだけ寄せるか。立ち絵と文字が見えるよう暗めに留める")]
        [Range(0f, 1f)] [SerializeField] private float _backgroundTint = 0.35f;
        [Range(0f, 1f)] [SerializeField] private float _backgroundAlpha = 0.85f;

        [Header("Timing (sec)")]
        [SerializeField] private float _slideDuration = 0.35f;
        [SerializeField] private float _jumpDuration = 0.6f;
        [SerializeField] private float _bubblePopDuration = 0.15f;
        [Tooltip("演出全体の長さ。これを過ぎたら結果画面へ進む")]
        [SerializeField] private float _totalDuration = 2f;

        [Header("Motion")]
        [Tooltip("スライドイン開始時に立ち絵を下へずらす量（UI単位）")]
        [SerializeField] private float _slideDistance = 900f;
        [SerializeField] private float _jumpHeight = 80f;
        [SerializeField] private int _jumpCount = 2;

        private Vector2 _portraitBase;
        private bool _skipped;

        private void Awake()
        {
            _portraitBase = _portraitRect.anchoredPosition;
            _tapArea.onClick.AddListener(() => _skipped = true);
        }

        public IEnumerator Play(MolkkyCharacterData character, Color color, string playerName, string line)
        {
            Setup(character, color, playerName, line);

            float elapsed = 0f;
            while (elapsed < _totalDuration && !_skipped)
            {
                Animate(elapsed);
                yield return null;
                elapsed += Time.deltaTime;
            }

            Animate(_totalDuration);
            // 結果画面の裏に残すので、以降のタップで何も起きないようにする
            _tapArea.interactable = false;
        }

        private void Setup(MolkkyCharacterData character, Color color, string playerName, string line)
        {
            Color background = Color.Lerp(Color.black, color, _backgroundTint);
            background.a = _backgroundAlpha;
            _background.color = background;

            _portrait.sprite = character.FrontSprite;
            _nameText.text = playerName;
            _nameText.color = color;
            _lineText.text = line;

            _skipped = false;
            _tapArea.interactable = true;
            gameObject.SetActive(true);
        }

        /// <summary>経過時間から、立ち絵の位置と吹き出しの大きさを決める。飛ばしたときも同じ式で最後の状態にする</summary>
        private void Animate(float elapsed)
        {
            _portraitRect.anchoredPosition = _portraitBase + Vector2.up * PortraitOffset(elapsed);

            float bubbleT = Mathf.Clamp01((elapsed - _slideDuration) / _bubblePopDuration);
            _bubbleRect.localScale = Vector3.one * bubbleT;
        }

        private float PortraitOffset(float elapsed)
        {
            if (elapsed < _slideDuration)
            {
                // 最後に減速して止まる動きにし、勢いよく飛び出してきた感じを出す
                float t = 1f - Mathf.Pow(1f - elapsed / _slideDuration, 3f);
                return -_slideDistance * (1f - t);
            }

            float jumpT = (elapsed - _slideDuration) / _jumpDuration;
            if (jumpT >= 1f) return 0f;

            return _jumpHeight * Mathf.Abs(Mathf.Sin(jumpT * Mathf.PI * _jumpCount));
        }
    }
}
