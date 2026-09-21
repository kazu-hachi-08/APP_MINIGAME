using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.TableTennis
{
    /// <summary>
    /// HUDの文字を「出す・少し弾ませる・時間で消す」だけを担当する表示部品。
    /// 進行側（TableTennisGameManager）が演出のタイミング計算を持たずに済むようにしている。
    /// </summary>
    [RequireComponent(typeof(Text))]
    public class HudText : MonoBehaviour
    {
        [SerializeField] private Text _text;

        [Header("Pop")]
        [Tooltip("表示した瞬間の拡大率。1で弾ませない")]
        [SerializeField] private float _popScale = 1.25f;

        [SerializeField] private float _popDuration = 0.16f;

        [Header("Fade")]
        [SerializeField] private float _fadeDuration = 0.25f;

        private Coroutine _routine;
        private Color _baseColor;
        private bool _initialized;

        private void Awake()
        {
            Initialize();
        }

        /// <summary>
        /// 非表示（GameObjectが非アクティブ）の状態から呼ばれると Awake がまだ動いていないため、
        /// 表示・消去の入口でも初期化しておく。
        /// </summary>
        private void Initialize()
        {
            if (_initialized) return;

            if (_text == null)
            {
                _text = GetComponent<Text>();
            }

            _baseColor = _text.color;
            _initialized = true;
        }

        /// <summary>文字を表示する。holdDuration が0より大きければ、その秒数後に自動で消える</summary>
        public void Show(string message, float holdDuration = 0f)
        {
            Initialize();

            if (string.IsNullOrEmpty(message))
            {
                Clear();
                return;
            }

            StopRoutine();

            _text.text = message;
            _text.color = _baseColor;
            gameObject.SetActive(true);

            _routine = StartCoroutine(PlayRoutine(holdDuration));
        }

        public void Clear()
        {
            Initialize();
            StopRoutine();
            _text.text = string.Empty;
            gameObject.SetActive(false);
        }

        private IEnumerator PlayRoutine(float holdDuration)
        {
            yield return Pop();

            if (holdDuration <= 0f) yield break;

            yield return new WaitForSeconds(holdDuration);
            yield return FadeOut();

            gameObject.SetActive(false);
        }

        private IEnumerator Pop()
        {
            Transform target = _text.transform;

            for (float time = 0f; time < _popDuration; time += Time.deltaTime)
            {
                float scale = Mathf.Lerp(_popScale, 1f, time / _popDuration);
                target.localScale = new Vector3(scale, scale, 1f);
                yield return null;
            }

            target.localScale = Vector3.one;
        }

        private IEnumerator FadeOut()
        {
            for (float time = 0f; time < _fadeDuration; time += Time.deltaTime)
            {
                float alpha = 1f - time / _fadeDuration;
                _text.color = new Color(_baseColor.r, _baseColor.g, _baseColor.b, _baseColor.a * alpha);
                yield return null;
            }

            _text.color = new Color(_baseColor.r, _baseColor.g, _baseColor.b, 0f);
        }

        private void StopRoutine()
        {
            if (_routine != null)
            {
                StopCoroutine(_routine);
                _routine = null;
            }

            _text.transform.localScale = Vector3.one;
        }
    }
}
