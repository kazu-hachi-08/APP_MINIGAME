using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace MiniGame.TableTennis
{
    /// <summary>
    /// 1回のフリック入力から取り出した情報
    /// </summary>
    public struct FlickData
    {
        /// <summary>画面上のフリック方向（正規化済み）</summary>
        public Vector2 Direction;

        /// <summary>フリック速度（画面高さ/秒。端末の解像度に依存させないための単位）</summary>
        public float Speed;

        /// <summary>フリック速度を 0..1 に正規化した強さ</summary>
        public float Strength;

        /// <summary>フリック検出時の画面座標</summary>
        public Vector2 ScreenPosition;
    }

    /// <summary>
    /// ポインタ（マウス/タッチ共通）からラケット移動用のドラッグ位置とフリックを取り出す。
    /// ゲームロジックから入力方式を切り離すため、ここでは判定だけを行いイベントで通知する。
    /// </summary>
    public class FlickInput : MonoBehaviour
    {
        [Header("Flick Detection")]
        [Tooltip("フリック速度を測る時間窓（秒）")]
        [SerializeField] private float _sampleWindow = 0.08f;

        [Tooltip("フリックとみなす最低速度（画面高さ/秒）")]
        [SerializeField] private float _flickSpeedThreshold = 0.9f;

        [Tooltip("フリックとみなす最低移動距離（画面高さ比）")]
        [SerializeField] private float _minFlickDistance = 0.03f;

        [Tooltip("Strength が 1.0 になる速度（画面高さ/秒）")]
        [SerializeField] private float _maxFlickSpeed = 3.5f;

        /// <summary>押している間の画面座標（ラケット移動に使う）</summary>
        public event Action<Vector2> OnPointerDragged;

        public event Action<FlickData> OnFlicked;

        private readonly List<Sample> _samples = new List<Sample>(16);
        private bool _tracking;
        private bool _flickFired;

        private void Update()
        {
            var pointer = Pointer.current;
            if (pointer == null) return;

            Vector2 position = pointer.position.ReadValue();

            if (pointer.press.wasPressedThisFrame)
            {
                // ポーズボタン等のUI操作をフリックとして拾わない
                _tracking = !IsPointerOverUI();
                _flickFired = false;
                _samples.Clear();
            }

            if (!_tracking) return;

            if (pointer.press.isPressed)
            {
                OnPointerDragged?.Invoke(position);

                _samples.Add(new Sample(position, Time.unscaledTime));
                TrimOldSamples();

                if (!_flickFired)
                {
                    TryDetectFlick(position);
                }
            }

            if (pointer.press.wasReleasedThisFrame)
            {
                _tracking = false;
            }
        }

        /// <summary>
        /// 指を離すまで待たず、振り抜いた瞬間に打てるようドラッグ中に判定する
        /// </summary>
        private void TryDetectFlick(Vector2 current)
        {
            if (_samples.Count < 2) return;

            Sample oldest = _samples[0];
            float elapsed = Time.unscaledTime - oldest.Time;
            if (elapsed <= Mathf.Epsilon) return;

            Vector2 delta = current - oldest.Position;
            float screenHeight = Mathf.Max(1f, Screen.height);
            float distance = delta.magnitude / screenHeight;
            float speed = distance / elapsed;

            if (distance < _minFlickDistance || speed < _flickSpeedThreshold) return;

            _flickFired = true;
            OnFlicked?.Invoke(new FlickData
            {
                Direction = delta.normalized,
                Speed = speed,
                Strength = Mathf.Clamp01(speed / _maxFlickSpeed),
                ScreenPosition = current
            });
        }

        private void TrimOldSamples()
        {
            float limit = Time.unscaledTime - _sampleWindow;
            while (_samples.Count > 2 && _samples[0].Time < limit)
            {
                _samples.RemoveAt(0);
            }
        }

        private static bool IsPointerOverUI()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
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
