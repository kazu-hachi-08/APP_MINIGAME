using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace MiniGame.Golf
{
    /// <summary>
    /// Phase 1 の仮のショット操作。押している時間でパワーを溜め、離した位置の方向へ打つ。
    /// 飛び方の手応えを先に確かめるための仮の入力で、Phase 3 の3タップゲージ（ShotInput / ShotGauge）で置き換える。
    /// マウスでもタッチでも同じ操作になるよう、Input System の Pointer で読む。
    /// </summary>
    public class PrototypeShotInput : MonoBehaviour
    {
        [SerializeField] private GolfBall _ball;
        [SerializeField] private Camera _camera;

        [Tooltip("押し始めてからパワー100%になるまでの時間（秒）")]
        [SerializeField] private float _fullPowerHoldTime = 1.2f;

        [Tooltip("ボールのこの距離（ユニット）以内で離したときは打たない。方向が定まらないため")]
        [SerializeField] private float _minAimDistance = 0.3f;

        private bool _charging;
        private float _pressedTime;

        public bool IsCharging => _charging;

        /// <summary>溜めているパワー（0〜1）</summary>
        public float Power => _charging ? Mathf.Clamp01((Time.time - _pressedTime) / _fullPowerHoldTime) : 0f;

        private void Update()
        {
            if (_ball.IsMoving)
            {
                _charging = false;
                return;
            }

            Pointer pointer = ResolvePointer();
            if (pointer == null) return;

            // タイトルへ戻るボタンなどを押したときは溜め始めない
            if (pointer.press.wasPressedThisFrame && !IsPointerOverUI(pointer))
            {
                _charging = true;
                _pressedTime = Time.time;
            }

            if (_charging && pointer.press.wasReleasedThisFrame)
            {
                Shoot(pointer.position.ReadValue());
            }
        }

        private void Shoot(Vector2 screenPosition)
        {
            float power = Power;
            _charging = false;

            Vector2 target = _camera.ScreenToWorldPoint(screenPosition);
            Vector2 direction = target - _ball.GroundPosition;
            if (direction.magnitude < _minAimDistance) return;

            _ball.Hit(direction, power);
        }

        /// <summary>触られている間はタッチを優先し、PCではマウスを使う。タッチは主タッチ（最初の指）だけを見る</summary>
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
