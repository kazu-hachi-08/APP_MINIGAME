using System.Collections;
using UnityEngine;

namespace MiniGame.Soccer
{
    /// <summary>
    /// ゴール成立時にゴール本体を潰して戻す演出。
    /// ネットだけを揺らす専用素材は用意していないため、ゴール絵全体のスケールを
    /// スカッシュ＆ストレッチさせることで「ネットが揺れた」手応えの代わりにする
    /// </summary>
    public class GoalReaction : MonoBehaviour
    {
        [SerializeField] private float _squashDuration = 0.3f;
        [SerializeField] private float _squashScaleX = 1.25f;
        [SerializeField] private float _squashScaleY = 0.75f;

        /// <summary>演出中に揺れ返す回数（半周期の数）。3回ほどで「ボヨン」と収まって見える</summary>
        private const float WobbleHalfCycles = 3f;

        /// <summary>揺れ返しの強さ。潰れ幅より十分小さくして、潰れ戻りの動きを邪魔しない程度にする</summary>
        private const float WobbleAmplitude = 0.05f;

        private Vector3 _baseScale;

        private void Awake()
        {
            _baseScale = transform.localScale;
        }

        public void Play()
        {
            StopAllCoroutines();
            transform.localScale = _baseScale;
            StartCoroutine(SquashRoutine());
        }

        private IEnumerator SquashRoutine()
        {
            float elapsed = 0f;
            while (elapsed < _squashDuration)
            {
                elapsed += Time.deltaTime;
                ApplySquash(Mathf.Clamp01(elapsed / _squashDuration));
                yield return null;
            }

            transform.localScale = _baseScale;
        }

        /// <param name="t">演出の進み具合（0 = 一番潰れた状態、1 = 元の形）</param>
        private void ApplySquash(float t)
        {
            // 減衰する正弦波を混ぜることで、単純な潰れ戻りではなく揺れ返しに見せる
            float wobble = Mathf.Sin(t * Mathf.PI * WobbleHalfCycles) * (1f - t) * WobbleAmplitude;
            float scaleX = Mathf.Lerp(_squashScaleX, 1f, t) + wobble;
            float scaleY = Mathf.Lerp(_squashScaleY, 1f, t) - wobble;

            transform.localScale = new Vector3(_baseScale.x * scaleX, _baseScale.y * scaleY, _baseScale.z);
        }
    }
}
