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
                float t = Mathf.Clamp01(elapsed / _squashDuration);

                // 減衰する正弦波を混ぜることで、単純な潰れ戻りではなく揺れ返しに見せる
                float wobble = Mathf.Sin(t * Mathf.PI * 3f) * (1f - t);
                float scaleX = Mathf.Lerp(_squashScaleX, 1f, t) + wobble * 0.05f;
                float scaleY = Mathf.Lerp(_squashScaleY, 1f, t) - wobble * 0.05f;

                transform.localScale = new Vector3(_baseScale.x * scaleX, _baseScale.y * scaleY, _baseScale.z);
                yield return null;
            }

            transform.localScale = _baseScale;
        }
    }
}
