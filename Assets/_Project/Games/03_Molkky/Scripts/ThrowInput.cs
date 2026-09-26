using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace MiniGame.Molkky
{
    /// <summary>
    /// 仮の投擲入力（Phase 1〜3）。押した位置から離した位置までのドラッグで方向と強さを決める。
    /// Phase 4 で左右移動とフリック速度による投擲に置き換える。
    /// </summary>
    public class ThrowInput : MonoBehaviour
    {
        [SerializeField] private MolkkyPhysicsSettings _settings;

        [Tooltip("この長さ（画面の高さに対する割合）以上ドラッグすると最大の強さになる")]
        [SerializeField] private float _dragLengthForMaxPower = 0.35f;

        [Tooltip("これより短いドラッグは誤タップとして無視する（画面の高さに対する割合）")]
        [SerializeField] private float _minDragLength = 0.03f;

        [Tooltip("最大角度のこの倍率を超える横向きのドラッグは投擲としない（§7.3）")]
        [SerializeField] private float _cancelAngleRatio = 2f;

        private bool _tracking;
        private Vector2 _startPosition;

        public bool IsAccepting { get; set; }

        public event Action<ThrowRequest> ThrowRequested;

        private void Update()
        {
            if (!IsAccepting)
            {
                _tracking = false;
                return;
            }

            Pointer pointer = ResolvePointer();
            if (pointer == null) return;

            Vector2 position = pointer.position.ReadValue();

            if (pointer.press.wasPressedThisFrame)
            {
                _tracking = !IsPointerOverUI(pointer);
                _startPosition = position;
            }
            else if (_tracking && pointer.press.wasReleasedThisFrame)
            {
                _tracking = false;
                TryThrow(position - _startPosition);
            }
        }

        private void TryThrow(Vector2 dragPixels)
        {
            Vector2 drag = dragPixels / Mathf.Max(1, Screen.height);
            if (drag.y <= 0f || drag.magnitude < _minDragLength) return;

            float angle = Mathf.Atan2(drag.x, drag.y) * Mathf.Rad2Deg;
            if (Mathf.Abs(angle) > _settings.MaxThrowAngle * _cancelAngleRatio) return;

            angle = Mathf.Clamp(angle, -_settings.MaxThrowAngle, _settings.MaxThrowAngle);
            float power = Mathf.Clamp01(drag.magnitude / _dragLengthForMaxPower);
            float speed = Mathf.Lerp(_settings.MinThrowSpeed, _settings.MaxThrowSpeed, power);

            ThrowRequested?.Invoke(new ThrowRequest(0f, angle, speed));
        }

        /// <summary>触られている間はタッチを優先し、PCではマウスを使う</summary>
        private static Pointer ResolvePointer()
        {
            Touchscreen touchscreen = Touchscreen.current;
            bool touching = touchscreen != null
                && (touchscreen.press.isPressed || touchscreen.press.wasReleasedThisFrame);

            return touching ? touchscreen : Pointer.current;
        }

        private static bool IsPointerOverUI(Pointer pointer)
        {
            if (EventSystem.current == null) return false;

            // タッチは指のIDを渡さないと正しく判定できない
            if (pointer is Touchscreen touchscreen)
            {
                return EventSystem.current.IsPointerOverGameObject(touchscreen.primaryTouch.touchId.ReadValue());
            }

            return EventSystem.current.IsPointerOverGameObject();
        }
    }
}
