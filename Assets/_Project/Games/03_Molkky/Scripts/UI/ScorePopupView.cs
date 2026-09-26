using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.Molkky
{
    /// <summary>
    /// 1投ごとの得点ポップアップ（§12.2）。結果の種類ごとに色を変え、ポンと弾む登場で目を引く。
    /// 勝利のときだけ脈打たせ続けて、特別な瞬間だと分かるようにする。
    /// </summary>
    public class ScorePopupView : MonoBehaviour
    {
        // ①〜⑫ は Unicode で連続しているので、数字から丸数字を作れる
        private const char CircledOne = '①';

        [SerializeField] private Text _text;

        [Header("Color")]
        [SerializeField] private Color _scoredColor = new Color(1f, 0.88f, 0.2f);
        [SerializeField] private Color _winColor = new Color(1f, 0.75f, 0.1f);
        [SerializeField] private Color _overColor = new Color(1f, 0.35f, 0.3f);
        [SerializeField] private Color _missColor = new Color(0.85f, 0.85f, 0.9f);
        [SerializeField] private Color _disqualifiedColor = new Color(0.7f, 0.5f, 0.9f);

        [Header("Pop")]
        [SerializeField] private float _popDuration = 0.3f;
        [SerializeField] private float _startScale = 0.3f;
        [Tooltip("勝利のときの大きさ。通常の得点より派手にする")]
        [SerializeField] private float _winScale = 1.4f;
        [SerializeField] private float _pulseSpeed = 8f;
        [SerializeField] private float _pulseAmount = 0.08f;

        public void ShowResult(ThrowResult result)
        {
            bool isWin = result.Outcome == ThrowOutcome.Win;
            _text.text = FormatResult(result);
            _text.color = ColorOf(result.Outcome);

            gameObject.SetActive(true);
            StopAllCoroutines();
            StartCoroutine(PopRoutine(isWin ? _winScale : 1f, isWin));
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private IEnumerator PopRoutine(float targetScale, bool pulse)
        {
            for (float t = 0f; t < _popDuration; t += Time.deltaTime)
            {
                transform.localScale = Vector3.one * Mathf.LerpUnclamped(_startScale, targetScale, EaseOutBack(t / _popDuration));
                yield return null;
            }

            transform.localScale = Vector3.one * targetScale;
            if (!pulse) yield break;

            for (float t = 0f; ; t += Time.deltaTime)
            {
                transform.localScale = Vector3.one * targetScale * (1f + Mathf.Sin(t * _pulseSpeed) * _pulseAmount);
                yield return null;
            }
        }

        /// <summary>少し行き過ぎてから戻る補間。「ポン」と弾む見た目にする</summary>
        private static float EaseOutBack(float t)
        {
            const float overshoot = 1.70158f;
            float u = t - 1f;
            return 1f + (overshoot + 1f) * u * u * u + overshoot * u * u;
        }

        private Color ColorOf(ThrowOutcome outcome)
        {
            switch (outcome)
            {
                case ThrowOutcome.Win: return _winColor;
                case ThrowOutcome.OverTo25: return _overColor;
                case ThrowOutcome.Disqualified: return _disqualifiedColor;
                case ThrowOutcome.Miss: return _missColor;
                default: return _scoredColor;
            }
        }

        /// <summary>1投の結果を §12.2 の文言にする</summary>
        private static string FormatResult(ThrowResult result)
        {
            switch (result.Outcome)
            {
                case ThrowOutcome.Win:
                    return "50! WIN!";
                case ThrowOutcome.OverTo25:
                    return "オーバー！\n25点に";
                case ThrowOutcome.Disqualified:
                    return "失格…";
                case ThrowOutcome.Miss:
                    return "ミス ×";
                default:
                    return result.SinglePinNumber > 0
                        ? $"{(char)(CircledOne + result.SinglePinNumber - 1)}  +{result.Points}"
                        : $"{result.FallenCount}本  +{result.Points}";
            }
        }
    }
}
