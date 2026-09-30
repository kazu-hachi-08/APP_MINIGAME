using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MiniGame.LifeGame
{
    /// <summary>
    /// ルーレットのフリック操作。指を離したときの速さから強さ（0〜1）を出す。
    /// 出目はルールが先に決めるので、強さは回る速さと時間だけに使う（仕様書 §4）。
    /// </summary>
    public class RouletteInput : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [Tooltip("この速さ（画面の高さ／秒）未満のフリックは回さない。誤タップ対策")]
        [SerializeField] private float _minSpeed = 0.6f;
        [Tooltip("この速さ以上は最大の強さとして扱う")]
        [SerializeField] private float _maxSpeed = 4f;
        [Tooltip("押してからこの秒数を過ぎたら、ゆっくり引きずっただけとみなして回さない")]
        [SerializeField] private float _maxDuration = 0.6f;

        private Vector2 _downPosition;
        private float _downTime;
        private bool _pressed;

        /// <summary>フリックされた。引数は強さ（0〜1）</summary>
        public event Action<float> Flicked;

        /// <summary>人間の回す番のときだけ GameManager が true にする</summary>
        public bool Accepting { get; set; }

        public void OnPointerDown(PointerEventData eventData)
        {
            _pressed = Accepting;
            _downPosition = eventData.position;
            _downTime = Time.unscaledTime;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!_pressed || !Accepting) return;

            _pressed = false;
            float duration = Mathf.Max(Time.unscaledTime - _downTime, Mathf.Epsilon);
            if (duration > _maxDuration) return;

            // 画面の高さで割って、解像度が違う端末でも同じ指の動きで同じ強さにする
            float speed = (eventData.position - _downPosition).magnitude / Screen.height / duration;
            if (speed < _minSpeed) return;

            Flicked?.Invoke(Mathf.InverseLerp(_minSpeed, _maxSpeed, speed));
        }
    }
}
