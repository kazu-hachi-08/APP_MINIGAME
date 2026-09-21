using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.Soccer
{
    /// <summary>
    /// ゴール時の画面演出（画面フラッシュ＋メッセージの拡大）
    /// アニメーションクリップやTweenライブラリを使わず、コルーチンだけで完結させる
    /// </summary>
    public class GoalEffect : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Image _flashImage;
        [SerializeField] private RectTransform _punchTarget;

        [Header("Flash")]
        [SerializeField] private float _flashDuration = 0.45f;
        [SerializeField] private float _flashStartAlpha = 0.55f;

        [Header("Punch")]
        [SerializeField] private float _punchStartScale = 1.7f;
        [SerializeField] private float _punchDuration = 0.25f;

        /// <summary>
        /// ゴール演出を開始する（メッセージ表示の直前に呼ぶ想定）
        /// </summary>
        public void Play()
        {
            // 連続ゴールで演出が二重に走らないよう、毎回リセットしてから開始する
            StopAllCoroutines();
            ResetVisuals();

            StartCoroutine(FlashRoutine());
            StartCoroutine(PunchRoutine());
        }

        private void ResetVisuals()
        {
            SetFlashAlpha(0f);
            if (_punchTarget != null)
            {
                _punchTarget.localScale = Vector3.one;
            }
        }

        private IEnumerator FlashRoutine()
        {
            float elapsed = 0f;
            while (elapsed < _flashDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / _flashDuration);
                SetFlashAlpha(Mathf.Lerp(_flashStartAlpha, 0f, t));
                yield return null;
            }

            SetFlashAlpha(0f);
        }

        private IEnumerator PunchRoutine()
        {
            if (_punchTarget == null) yield break;

            float elapsed = 0f;
            while (elapsed < _punchDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / _punchDuration);
                float scale = Mathf.Lerp(_punchStartScale, 1f, t);
                _punchTarget.localScale = new Vector3(scale, scale, 1f);
                yield return null;
            }

            _punchTarget.localScale = Vector3.one;
        }

        private void SetFlashAlpha(float alpha)
        {
            if (_flashImage == null) return;

            Color color = _flashImage.color;
            color.a = alpha;
            _flashImage.color = color;
        }
    }
}
