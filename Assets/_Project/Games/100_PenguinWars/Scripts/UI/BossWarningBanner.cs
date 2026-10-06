using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.PenguinWars
{
    /// <summary>
    /// ボス登場のとき画面上部に出る赤い帯（仕様書 §9）。「WARNING!」の文字が右から左へ流れ、帯が点滅する。
    /// 画面の中央を隠さないよう上部だけに出し、出撃ボタンも押せるよう当たり判定は持たない
    /// </summary>
    public class BossWarningBanner : MonoBehaviour
    {
        [Tooltip("表示・非表示を切り替える子。自分自身を消すと Update が止まるため")]
        [SerializeField] private GameObject _root;
        [SerializeField] private Image _band;
        [SerializeField] private RectTransform _scrollText;
        [SerializeField] private float _duration = 1.5f;
        [Tooltip("文字が流れる距離。帯の横幅より長くして、画面の外から入って外へ抜けるようにする")]
        [SerializeField] private float _scrollDistance = 2600f;
        [SerializeField] private float _blinkCycles = 3f;
        [SerializeField, Range(0f, 1f)] private float _blinkMinAlpha = 0.55f;

        private float _timer;
        private float _bandAlpha;

        public float Duration => _duration;

        private void Awake()
        {
            _bandAlpha = _band.color.a;
            _root.SetActive(false);
        }

        public void Play()
        {
            _timer = _duration;
            _root.SetActive(true);
            Apply(0f);
        }

        public void Hide()
        {
            _timer = 0f;
            _root.SetActive(false);
        }

        private void Update()
        {
            if (_timer <= 0f) return;

            _timer -= Time.deltaTime;
            Apply(Mathf.Clamp01(1f - _timer / _duration));
            if (_timer <= 0f) _root.SetActive(false);
        }

        private void Apply(float rate)
        {
            Vector2 position = _scrollText.anchoredPosition;
            position.x = Mathf.Lerp(_scrollDistance * 0.5f, -_scrollDistance * 0.5f, rate);
            _scrollText.anchoredPosition = position;

            float blink = Mathf.Abs(Mathf.Cos(rate * _blinkCycles * Mathf.PI));
            Color color = _band.color;
            color.a = _bandAlpha * Mathf.Lerp(_blinkMinAlpha, 1f, blink);
            _band.color = color;
        }
    }
}
