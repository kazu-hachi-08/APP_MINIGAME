using System;
using System.Collections;
using UnityEngine;

namespace MiniGame.PenguinWars
{
    /// <summary>
    /// 城が揺れて崩れる演出（仕様書 §2.1・§9。1.5秒）。前半は揺れるだけ、後半で足元に向かって潰れる。
    /// 終わってからリザルトを出すよう、終了を呼び出し元に知らせる
    /// </summary>
    public class CastleCollapse : MonoBehaviour
    {
        [SerializeField] private Transform _body;
        [SerializeField] private GameObject _hpBar;
        [SerializeField] private float _duration = 1.5f;
        [Tooltip("この割合までは揺れるだけ。いきなり消えず「やられた」と分かる間を作る")]
        [SerializeField, Range(0f, 0.9f)] private float _shakeOnlyRate = 0.4f;
        [SerializeField] private float _shakeWidth = 0.12f;

        public float Duration => _duration;

        public void Play(Action onFinished)
        {
            StartCoroutine(Collapse(onFinished));
        }

        private IEnumerator Collapse(Action onFinished)
        {
            _hpBar.SetActive(false);
            Vector3 basePosition = _body.localPosition;
            Vector3 baseScale = _body.localScale;

            for (float t = 0f; t < _duration; t += Time.deltaTime)
            {
                float rate = t / _duration;
                float fall = Mathf.Clamp01((rate - _shakeOnlyRate) / (1f - _shakeOnlyRate));
                _body.localPosition = basePosition + Vector3.right * (UnityEngine.Random.Range(-1f, 1f) * _shakeWidth);
                // 絵の原点が足元なので、縦に縮めるだけで地面に崩れ落ちるように見える。最初はゆっくり、最後に一気に
                _body.localScale = new Vector3(baseScale.x, baseScale.y * (1f - fall * fall), baseScale.z);
                yield return null;
            }

            _body.gameObject.SetActive(false);
            onFinished?.Invoke();
        }
    }
}
