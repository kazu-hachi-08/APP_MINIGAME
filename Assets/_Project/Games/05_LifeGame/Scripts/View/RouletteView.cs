using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.LifeGame
{
    /// <summary>
    /// 1〜10のルーレットの見た目。出目はルールが先に決め、ここは回転をその出目で止める演出だけを持つ（仕様書 §4）。
    /// 盤面の絵は実行時に作る（区画の色・縁・中心の軸だけの単純な絵なので、PNG にせずコードで描く）。
    /// </summary>
    public class RouletteView : MonoBehaviour
    {
        private const int Count = LifeRuleConfig.RouletteMax;
        private const float SegmentAngle = 360f / Count;
        private const int TextureSize = 256;

        [SerializeField] private RectTransform _wheel;
        [SerializeField] private Image _wheelImage;
        [Tooltip("数字の見本。実行時に10個複製して円周に並べる")]
        [SerializeField] private Text _labelTemplate;
        [SerializeField] private Text _hintText;
        [SerializeField] private float _labelRadiusRate = 0.36f;

        [Header("Spin")]
        [SerializeField] private float _minDuration = 1.2f;
        [SerializeField] private float _maxDuration = 2.4f;
        [SerializeField] private int _minTurns = 2;
        [SerializeField] private int _maxTurns = 5;

        [Header("Color")]
        [SerializeField] private Color _evenColor = new Color(0.95f, 0.95f, 0.9f);
        [SerializeField] private Color _oddColor = new Color(1f, 0.8f, 0.45f);
        [SerializeField] private Color _rimColor = new Color(0.55f, 0.3f, 0.15f);
        [SerializeField] private Color _hubColor = new Color(0.85f, 0.2f, 0.2f);
        [Tooltip("外周の縁の太さ（テクスチャのピクセル）")]
        [SerializeField] private float _rimWidth = 10f;
        [Tooltip("中心の軸の半径（テクスチャのピクセル）")]
        [SerializeField] private float _hubRadius = 18f;

        /// <summary>区画の境目が針を通るたび（回転音のカチカチ用）</summary>
        public event Action Ticked;

        /// <summary>出目で止まったとき</summary>
        public event Action Stopped;

        private void Awake()
        {
            _wheelImage.sprite = CreateWheelSprite();
            CreateLabels();
        }

        public void SetHint(string hint)
        {
            _hintText.text = hint;
        }

        /// <summary>フリックの強さで回る速さと時間を変え、number が上の針に来るように止める</summary>
        public IEnumerator SpinTo(int number, float strength)
        {
            float duration = Mathf.Lerp(_minDuration, _maxDuration, strength);
            int turns = Mathf.RoundToInt(Mathf.Lerp(_minTurns, _maxTurns, strength));

            float from = _wheel.localEulerAngles.z;
            float to = from + turns * 360f + Mathf.Repeat(AngleOf(number) - from, 360f);
            int segment = SegmentAt(from);
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                // 減速して止まる（ease-out cubic）
                float rate = 1f - Mathf.Pow(1f - t / duration, 3f);
                float angle = Mathf.Lerp(from, to, rate);
                _wheel.localEulerAngles = new Vector3(0f, 0f, angle);

                // 区画の境目を越えるたびに鳴らし、減速していく様子を音でも伝える
                if (SegmentAt(angle) != segment)
                {
                    segment = SegmentAt(angle);
                    Ticked?.Invoke();
                }

                yield return null;
            }

            _wheel.localEulerAngles = new Vector3(0f, 0f, to);
            Stopped?.Invoke();
        }

        /// <summary>針の下にある区画の番号（境目の判定用。区画の中心が数字の位置なので半区画ずらす）</summary>
        private static int SegmentAt(float angle) => Mathf.FloorToInt((angle + SegmentAngle * 0.5f) / SegmentAngle);

        /// <summary>
        /// 数字 n の区画は、上から時計回りに (n-1)×36° の位置にある。
        /// UIの回転は反時計回りが正なので、その角度だけ回すと区画が真上（針の位置）に来る
        /// </summary>
        private static float AngleOf(int number) => (number - 1) * SegmentAngle;

        private void CreateLabels()
        {
            float radius = _wheel.rect.width * _labelRadiusRate;
            for (int number = 1; number <= Count; number++)
            {
                Text label = Instantiate(_labelTemplate, _wheel);
                label.name = $"Label_{number}";
                label.text = number.ToString();
                float radian = AngleOf(number) * Mathf.Deg2Rad;
                label.rectTransform.anchoredPosition = new Vector2(Mathf.Sin(radian), Mathf.Cos(radian)) * radius;
                // 文字も一緒に外向きへ傾けて、回ったときに円周に沿って見えるようにする
                label.rectTransform.localEulerAngles = new Vector3(0f, 0f, -AngleOf(number));
                label.gameObject.SetActive(true);
            }

            _labelTemplate.gameObject.SetActive(false);
        }

        private Sprite CreateWheelSprite()
        {
            var texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false);
            float radius = TextureSize * 0.5f;
            var center = new Vector2(radius, radius);

            for (int y = 0; y < TextureSize; y++)
            {
                for (int x = 0; x < TextureSize; x++)
                {
                    Vector2 offset = new Vector2(x + 0.5f, y + 0.5f) - center;
                    // 上から時計回りの角度。区画の中心が数字の位置に来るよう半区画ずらす
                    float angle = Mathf.Repeat(Mathf.Atan2(offset.x, offset.y) * Mathf.Rad2Deg + SegmentAngle * 0.5f, 360f);
                    int segment = Mathf.FloorToInt(angle / SegmentAngle);
                    float distance = offset.magnitude;
                    Color color = segment % 2 == 0 ? _evenColor : _oddColor;
                    if (distance > radius - _rimWidth) color = _rimColor;
                    if (distance < _hubRadius) color = _hubColor;
                    color.a = Mathf.Clamp01(radius - distance);
                    texture.SetPixel(x, y, color);
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, TextureSize, TextureSize), new Vector2(0.5f, 0.5f));
        }
    }
}
