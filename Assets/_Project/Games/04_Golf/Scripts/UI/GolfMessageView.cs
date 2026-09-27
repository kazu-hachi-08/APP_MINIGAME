using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.Golf
{
    /// <summary>
    /// 画面中央に大きく出す演出メッセージ（§13.3）。「ポン」と弾んで出て、少し見せてから消える。
    /// バーディー以上は脈打たせて、特別な結果だと分かるようにする。
    /// </summary>
    public class GolfMessageView : MonoBehaviour
    {
        [SerializeField] private Text _text;

        [Header("Color")]
        [SerializeField] private Color _greatColor = new Color(1f, 0.82f, 0.2f);
        [SerializeField] private Color _normalColor = Color.white;
        [SerializeField] private Color _badColor = new Color(0.7f, 0.8f, 1f);
        [SerializeField] private Color _penaltyColor = new Color(1f, 0.4f, 0.35f);

        [Header("Pop")]
        [SerializeField] private float _popDuration = 0.3f;
        [SerializeField] private float _startScale = 0.3f;
        [Tooltip("バーディー以上のときの大きさ。パーやボギーより派手にする")]
        [SerializeField] private float _greatScale = 1.4f;
        [SerializeField] private float _pulseSpeed = 8f;
        [SerializeField] private float _pulseAmount = 0.08f;

        [Tooltip("出してから消えるまでの時間（秒）。GolfGameManager はこの間、次の人の番を出さずに待つ")]
        [SerializeField] private float _showSeconds = 1.8f;

        public float ShowSeconds => _showSeconds;

        /// <summary>カップインの結果。パーとの差で色と大きさを変える</summary>
        public void ShowHoleOut(int strokes, int par)
        {
            bool great = strokes < par || strokes == 1;
            Color color = great ? _greatColor : strokes == par ? _normalColor : _badColor;
            Show(GolfRules.ScoreName(strokes, par), color, great);
        }

        public void ShowPenalty(GroundType ground)
        {
            int penalty = GolfRules.PenaltyStrokes;
            string text = ground == GroundType.Water ? $"池ポチャ…\n+{penalty}打" : $"OB\n+{penalty}打";
            Show(text, _penaltyColor, false);
        }

        public void ShowGiveUp()
        {
            Show("ギブアップ\n（パー×2）", _badColor, false);
        }

        /// <summary>ホール開始のコース名。カメラがコースを流している間ずっと出すので、長さは呼ぶ側が決める</summary>
        public void ShowHoleName(string text, float seconds)
        {
            Show(text, _normalColor, false, seconds);
        }

        private void Show(string text, Color color, bool emphasize)
        {
            Show(text, color, emphasize, _showSeconds);
        }

        private void Show(string text, Color color, bool emphasize, float seconds)
        {
            _text.text = text;
            _text.color = color;

            gameObject.SetActive(true);
            StopAllCoroutines();
            StartCoroutine(PopRoutine(emphasize ? _greatScale : 1f, emphasize, seconds));
        }

        private IEnumerator PopRoutine(float targetScale, bool pulse, float seconds)
        {
            for (float t = 0f; t < _popDuration; t += Time.deltaTime)
            {
                transform.localScale = Vector3.one * Mathf.LerpUnclamped(_startScale, targetScale, EaseOutBack(t / _popDuration));
                yield return null;
            }

            for (float t = _popDuration; t < seconds; t += Time.deltaTime)
            {
                float wave = pulse ? Mathf.Sin(t * _pulseSpeed) * _pulseAmount : 0f;
                transform.localScale = Vector3.one * targetScale * (1f + wave);
                yield return null;
            }

            gameObject.SetActive(false);
        }

        /// <summary>少し行き過ぎてから戻る補間。「ポン」と弾む見た目にする</summary>
        private static float EaseOutBack(float t)
        {
            const float overshoot = 1.70158f;
            float u = t - 1f;
            return 1f + (overshoot + 1f) * u * u * u + overshoot * u * u;
        }
    }
}
