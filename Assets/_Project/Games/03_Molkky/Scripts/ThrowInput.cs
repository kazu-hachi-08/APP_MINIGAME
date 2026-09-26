using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace MiniGame.Molkky
{
    /// <summary>
    /// 投擲入力（§7）。横ドラッグで投擲ライン上の位置を動かし、上フリックで投げる。
    /// 卓球の FlickInput を参考にしているが、モルックは「離した瞬間の勢い」で投げたいので、判定は指を離したときに行う。
    /// 位置は指の絶対位置ではなく移動量で動かす。棒の真上を触らなくてよいので、指で棒が隠れない（§14 Phase 6）。
    /// </summary>
    public class ThrowInput : MonoBehaviour
    {
        [SerializeField] private MolkkyPhysicsSettings _settings;
        [SerializeField] private DepthProjector _projector;
        [SerializeField] private Camera _camera;

        [Header("Move")]
        [Tooltip("指の横移動に対する棒の移動量の倍率。1で指と棒が画面上で同じだけ動く")]
        [SerializeField] private float _moveSensitivity = 1f;

        [Header("Flick")]
        [Tooltip("離す直前のこの時間（秒）の動きでフリックの方向と速度を測る")]
        [SerializeField] private float _sampleWindow = 0.1f;

        [Tooltip("フリックとみなす最低速度（画面高さ/秒）。これより遅い離し方は投げない")]
        [SerializeField] private float _minFlickSpeed = 0.8f;

        [Tooltip("フリックとみなす最低移動距離（画面高さ比）。指のわずかなブレで投げないようにする")]
        [SerializeField] private float _minFlickDistance = 0.04f;

        [Tooltip("最大の強さになるフリック速度（画面高さ/秒）")]
        [SerializeField] private float _maxFlickSpeed = 4f;

        [Tooltip("最大角度のこの倍率を超える横向きのフリックは投擲としない（§7.3）")]
        [SerializeField] private float _cancelAngleRatio = 2f;

        private readonly List<Sample> _samples = new List<Sample>(16);
        private bool _tracking;
        private Vector2 _lastPosition;

        public bool IsAccepting { get; set; }

        /// <summary>投擲ライン上の現在位置（地面座標のX）</summary>
        public float PositionX { get; private set; }

        public ThrowStyle Style { get; private set; } = ThrowStyle.Horizontal;

        public event Action<float> PositionChanged;
        public event Action<ThrowStyle> StyleChanged;
        public event Action<ThrowRequest> ThrowRequested;

        public void ResetPosition(float x)
        {
            SetPosition(x);
        }

        public void SetStyle(ThrowStyle style)
        {
            Style = style;
            StyleChanged?.Invoke(Style);
        }

        /// <summary>切り替えボタンから呼ぶ。構えている間だけ受け付け、投げた後や相手の番には変えられないようにする</summary>
        public void ToggleStyle()
        {
            if (!IsAccepting) return;

            SetStyle(Style == ThrowStyle.Vertical ? ThrowStyle.Horizontal : ThrowStyle.Vertical);
        }

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

            // 手番開始のタップのように、受付前から押していた指は追わない
            if (pointer.press.wasPressedThisFrame)
            {
                _tracking = !IsPointerOverUI(pointer);
                _lastPosition = position;
                _samples.Clear();
            }

            if (!_tracking) return;

            AddSample(position);

            if (pointer.press.isPressed)
            {
                Move(position - _lastPosition);
                _lastPosition = position;
            }

            if (pointer.press.wasReleasedThisFrame)
            {
                _tracking = false;
                TryThrow();
            }
        }

        /// <summary>
        /// 横向きの動きだけで位置を動かす。上フリックの途中で狙いがずれないよう、縦向きが勝るフレームは無視する
        /// </summary>
        private void Move(Vector2 deltaPixels)
        {
            if (Mathf.Abs(deltaPixels.x) <= Mathf.Abs(deltaPixels.y)) return;

            float worldPerPixel = 2f * _camera.orthographicSize / Mathf.Max(1, Screen.height);
            float groundPerWorld = 1f / _projector.ScaleAt(0f);
            SetPosition(PositionX + deltaPixels.x * worldPerPixel * groundPerWorld * _moveSensitivity);
        }

        private void SetPosition(float x)
        {
            PositionX = Mathf.Clamp(x, -_settings.ThrowLineHalfWidth, _settings.ThrowLineHalfWidth);
            PositionChanged?.Invoke(PositionX);
        }

        private void TryThrow()
        {
            if (_samples.Count < 2) return;

            Sample oldest = _samples[0];
            Sample newest = _samples[_samples.Count - 1];
            float elapsed = newest.Time - oldest.Time;
            if (elapsed <= Mathf.Epsilon) return;

            // 解像度に依存しないよう画面の高さで割る
            Vector2 delta = (newest.Position - oldest.Position) / Mathf.Max(1, Screen.height);
            float flickSpeed = delta.magnitude / elapsed;
            if (delta.y <= 0f || delta.magnitude < _minFlickDistance || flickSpeed < _minFlickSpeed) return;

            float angle = Mathf.Atan2(delta.x, delta.y) * Mathf.Rad2Deg;
            if (Mathf.Abs(angle) > _settings.MaxThrowAngle * _cancelAngleRatio) return;

            angle = Mathf.Clamp(angle, -_settings.MaxThrowAngle, _settings.MaxThrowAngle);
            float power = Mathf.InverseLerp(_minFlickSpeed, _maxFlickSpeed, flickSpeed);
            float speed = Mathf.Lerp(_settings.MinThrowSpeed, _settings.MaxThrowSpeed, power);

            ThrowRequested?.Invoke(new ThrowRequest(PositionX, angle, speed, Style));
        }

        private void AddSample(Vector2 position)
        {
            float now = Time.unscaledTime;
            _samples.Add(new Sample(position, now));

            // 指を止めてから離した場合は古いサンプルが消えて速度が0近くになり、投擲がキャンセルされる
            float limit = now - _sampleWindow;
            while (_samples.Count > 2 && _samples[0].Time < limit)
            {
                _samples.RemoveAt(0);
            }
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

        private readonly struct Sample
        {
            public readonly Vector2 Position;
            public readonly float Time;

            public Sample(Vector2 position, float time)
            {
                Position = position;
                Time = time;
            }
        }
    }
}
