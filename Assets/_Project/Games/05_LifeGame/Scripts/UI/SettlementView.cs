using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.LifeGame
{
    /// <summary>
    /// 1人ずつの精算の内訳（仕様書 §8）。家の売却ルーレットが見えるよう、画面の上寄りに出して下のルーレットは隠さない。
    /// 何をいつ出すかは LifeGameManager が決め、ここは行を足して所持金をカウントアップするだけにする。
    /// 演出の途中でタップすると、その人の残りの行を待たずに出し切る（2回目以降のプレイで待たされないように）。
    /// </summary>
    public class SettlementView : MonoBehaviour
    {
        [SerializeField] private Text _titleText;
        [SerializeField] private Text _bodyText;
        [SerializeField] private Text _moneyText;
        [SerializeField] private Text _hintText;
        [SerializeField] private Button _tapArea;
        [SerializeField] private LifeAudio _audio;

        [Header("Count up")]
        [SerializeField] private float _countDuration = 0.5f;
        [Tooltip("増減を色で見せた後、元の色へ戻るまでの時間")]
        [SerializeField] private float _colorFadeDuration = 0.4f;

        [Header("Wobble")]
        [Tooltip("数字が動いている間に傾ける角度。レイアウトに位置を決められているので、位置ではなく回転で揺らす")]
        [SerializeField] private float _wobbleAngle = 6f;
        [SerializeField] private float _wobbleSpeed = 40f;
        [SerializeField] private float _punchScale = 1.15f;

        private bool _tapped;
        private bool _fastForward;
        private int _shownMoney;
        private Color _moneyBaseColor;

        private void Awake()
        {
            _moneyBaseColor = _moneyText.color;
            _tapArea.onClick.AddListener(() =>
            {
                _tapped = true;
                _fastForward = true;
            });
        }

        /// <summary>タップで早送り中か。家の売却ルーレットなど、ここ以外の演出も飛ばすかどうかに使う</summary>
        public bool IsFastForward => _fastForward;

        public void Begin(string title, Color color, int money)
        {
            // 最初の表示で Awake（元の文字色の記録）が先に走るよう、表示してから中身を入れる
            gameObject.SetActive(true);
            _titleText.text = title;
            _titleText.color = color;
            _bodyText.text = "";
            _shownMoney = money;
            _moneyText.text = LifeTexts.SettlementMoney(money);
            ResetMoneyStyle();
            _hintText.gameObject.SetActive(false);
            _fastForward = false;
        }

        /// <summary>内訳を1行足し、所持金を今の表示から money まで数える</summary>
        public IEnumerator AddLine(string line, int money)
        {
            _bodyText.text = _bodyText.text.Length == 0 ? line : $"{_bodyText.text}\n{line}";
            yield return CountTo(money);
        }

        /// <summary>行と行の間の待ち。早送り中は待たない</summary>
        public IEnumerator Pause(float seconds)
        {
            for (float t = 0f; t < seconds && !_fastForward; t += Time.deltaTime) yield return null;
        }

        public void ShowTotal(int total)
        {
            _moneyText.text = LifeTexts.SettlementTotal(total);
        }

        /// <summary>内訳を読み終えたら次の人へ進めるよう、タップを待つ</summary>
        public IEnumerator WaitForTap()
        {
            _hintText.gameObject.SetActive(true);
            _tapped = false;
            yield return new WaitUntil(() => _tapped);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private IEnumerator CountTo(int money)
        {
            int from = _shownMoney;
            _shownMoney = money;
            if (money == from) yield break;

            bool increasing = money > from;
            Color flash = increasing ? LifeColors.Gain : LifeColors.Loss;
            for (float t = 0f; t < _countDuration && !_fastForward; t += Time.deltaTime)
            {
                float rate = t / _countDuration;
                _moneyText.text = LifeTexts.SettlementMoney(Mathf.RoundToInt(Mathf.Lerp(from, money, rate)));
                _moneyText.color = flash;
                Wobble(1f - rate);
                _audio.PlayCount(increasing);
                yield return null;
            }

            _moneyText.text = LifeTexts.SettlementMoney(money);
            if (increasing) _audio.PlayGain();
            else _audio.PlayLoss();
            yield return FadeColor(flash);
        }

        /// <summary>動いている間は傾けて少し大きくし、数え終わりに近づくほど揺れを小さくする</summary>
        private void Wobble(float strength)
        {
            Transform money = _moneyText.transform;
            money.localEulerAngles = new Vector3(0f, 0f, Mathf.Sin(Time.time * _wobbleSpeed) * _wobbleAngle * strength);
            money.localScale = Vector3.one * Mathf.Lerp(1f, _punchScale, strength);
        }

        private IEnumerator FadeColor(Color flash)
        {
            ResetMoneyTransform();
            for (float t = 0f; t < _colorFadeDuration && !_fastForward; t += Time.deltaTime)
            {
                _moneyText.color = Color.Lerp(flash, _moneyBaseColor, t / _colorFadeDuration);
                yield return null;
            }

            _moneyText.color = _moneyBaseColor;
        }

        private void ResetMoneyStyle()
        {
            _moneyText.color = _moneyBaseColor;
            ResetMoneyTransform();
        }

        private void ResetMoneyTransform()
        {
            _moneyText.transform.localEulerAngles = Vector3.zero;
            _moneyText.transform.localScale = Vector3.one;
        }
    }
}
