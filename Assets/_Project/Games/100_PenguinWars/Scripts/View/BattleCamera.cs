using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace MiniGame.PenguinWars
{
    /// <summary>
    /// 戦場を横にスクロールするカメラ（仕様書 §3.2）。ドラッグ・←→キーで X だけ動かし、戦場の端で止める。
    /// 画面に映る幅を端末の縦横比に関係なく揃えるため、orthographicSize は横幅から逆算する
    /// </summary>
    public class BattleCamera : MonoBehaviour
    {
        // EventSystem が無いとき（テスト用の単体シーンなど）のドラッグ開始の距離。EventSystem の既定値と同じにする
        private const float FallbackDragThresholdPixels = 10f;

        [SerializeField] private Camera _camera;
        [Tooltip("画面に映る横幅（ワールド単位）。戦場の約半分")]
        [SerializeField] private float _visibleWidth = 16f;
        [Tooltip("城の外側に見せる余白。城が画面の端で切れないようにする")]
        [SerializeField] private float _edgeMargin = 2f;
        [SerializeField] private float _keyScrollSpeed = 15f;
        [Tooltip("揺れが弱まり始めるまでの割合。最初は強く揺らして、だんだん収める")]
        [SerializeField, Range(0f, 1f)] private float _shakeHoldRate = 0.3f;
        [Tooltip("Peek の行き・帰りそれぞれにかける割合。残りはその場で止まって見せる")]
        [SerializeField, Range(0.05f, 0.5f)] private float _peekMoveRate = 0.3f;

        private float _fieldLength;
        private bool _pressValid;
        private bool _isDragging;
        private Vector2 _pressScreenPosition;
        private float _pressCameraX;
        private readonly List<RaycastResult> _raycastResults = new List<RaycastResult>();
        private float _shakeDuration;
        private float _shakeTimer;
        private float _shakeStrength;
        // スクロール位置（ドラッグ・キー・端で止める計算）に揺れが混ざらないよう、足した分を覚えておいて毎フレーム外す
        private Vector3 _appliedShake;
        private float _peekDuration;
        private float _peekTimer;
        private float _peekFromX;
        private float _peekToX;

        /// <summary>開始時は自城（左）側を映す</summary>
        public void Initialize(float fieldLength)
        {
            _fieldLength = fieldLength;
            // デモ・前の試合の揺れやボスの寄りが残っていると、開始位置からずれて動き出すため
            CancelPeek();
            RemoveShake();
            _shakeTimer = 0f;
            ApplySize();
            SetX(float.MinValue);
        }

        private void Update()
        {
            RemoveShake();
            ApplySize();
            HandleKeys();
            HandlePointer();
            UpdatePeek();
            ApplyShake();
        }

        /// <summary>ペンギン砲・城崩れの画面揺れ（仕様書 §9）。揺れている途中に呼ばれたら新しい方で上書きする</summary>
        public void Shake(float duration, float strength)
        {
            _shakeDuration = duration;
            _shakeTimer = duration;
            _shakeStrength = strength;
        }

        /// <summary>指定した X を画面の中央に映す（戦場の端では止まる）。城が崩れるところを見せるため</summary>
        public void LookAt(float x)
        {
            RemoveShake();
            CancelPeek();
            SetX(x);
        }

        /// <summary>
        /// 指定した X へ寄って、少し見せてから元の位置へ戻る（ボス登場。仕様書 §9）。
        /// 操作は奪わず、途中でスクロールされたらそこで止めてプレイヤーの操作を優先する
        /// </summary>
        public void Peek(float x, float duration)
        {
            if (duration <= 0f) return;

            _peekFromX = _camera.transform.position.x - _appliedShake.x;
            _peekToX = x;
            _peekDuration = duration;
            _peekTimer = duration;
        }

        private void UpdatePeek()
        {
            if (_peekTimer <= 0f) return;

            _peekTimer = Mathf.Max(0f, _peekTimer - Time.deltaTime);
            float rate = 1f - _peekTimer / _peekDuration;
            float weight = rate < _peekMoveRate ? rate / _peekMoveRate
                : rate > 1f - _peekMoveRate ? (1f - rate) / _peekMoveRate
                : 1f;
            SetX(Mathf.Lerp(_peekFromX, _peekToX, Mathf.SmoothStep(0f, 1f, weight)));
        }

        private void CancelPeek() => _peekTimer = 0f;

        private void ApplyShake()
        {
            if (_shakeTimer <= 0f || _shakeDuration <= 0f) return;

            _shakeTimer -= Time.deltaTime;
            float remaining = Mathf.Clamp01(_shakeTimer / (_shakeDuration * (1f - _shakeHoldRate)));
            _appliedShake = (Vector3)(Random.insideUnitCircle * (_shakeStrength * remaining));
            _camera.transform.position += _appliedShake;
        }

        private void RemoveShake()
        {
            _camera.transform.position -= _appliedShake;
            _appliedShake = Vector3.zero;
        }

        /// <summary>スマホを回したときに縦横比が変わるので毎フレーム合わせる</summary>
        private void ApplySize()
        {
            _camera.orthographicSize = _visibleWidth * 0.5f / _camera.aspect;
        }

        private void HandleKeys()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;

            float direction = 0f;
            if (keyboard.leftArrowKey.isPressed) direction -= 1f;
            if (keyboard.rightArrowKey.isPressed) direction += 1f;
            if (direction == 0f) return;

            CancelPeek();
            SetX(_camera.transform.position.x + direction * _keyScrollSpeed * Time.deltaTime);
        }

        private void HandlePointer()
        {
            Pointer pointer = ResolvePointer();
            if (pointer == null) return;

            if (pointer.press.wasPressedThisFrame) BeginPress(pointer);
            else if (pointer.press.isPressed && _pressValid) UpdateDrag(pointer);
            else if (pointer.press.wasReleasedThisFrame) EndPress();
        }

        private void BeginPress(Pointer pointer)
        {
            Vector2 position = pointer.position.ReadValue();
            // 出撃ボタンなどUIの上で始まった操作では戦場を動かさない（ボタン連打で画面がぶれないように）
            _pressValid = !IsOverUI(position);
            _isDragging = false;
            _pressScreenPosition = position;
            _pressCameraX = _camera.transform.position.x;
        }

        private void EndPress()
        {
            _pressValid = false;
            _isDragging = false;
        }

        private void UpdateDrag(Pointer pointer)
        {
            float deltaX = pointer.position.ReadValue().x - _pressScreenPosition.x;
            if (!_isDragging && Mathf.Abs(deltaX) < DragThreshold()) return;

            _isDragging = true;
            CancelPeek();
            float worldPerPixel = _visibleWidth / Screen.width;
            // 指に戦場が付いてくるように、指と逆向きにカメラを動かす
            SetX(_pressCameraX - deltaX * worldPerPixel);
        }

        private void SetX(float x)
        {
            Vector3 position = _camera.transform.position;
            position.x = ClampX(x);
            _camera.transform.position = position;
        }

        private float ClampX(float x)
        {
            float halfView = _visibleWidth * 0.5f;
            float min = -_edgeMargin + halfView;
            float max = _fieldLength + _edgeMargin - halfView;
            // 画面が戦場より広い端末では中央に固定する
            if (min > max) return _fieldLength * 0.5f;

            return Mathf.Clamp(x, min, max);
        }

        private static float DragThreshold()
        {
            return EventSystem.current != null ? EventSystem.current.pixelDragThreshold : FallbackDragThresholdPixels;
        }

        /// <summary>
        /// IsPointerOverGameObject はタッチだと指IDを渡さないと判定できず、押した瞬間のフレームでは不正確なため、
        /// LifeGame の BoardCamera と同じく UI へのレイキャストで判定する
        /// </summary>
        private bool IsOverUI(Vector2 screenPosition)
        {
            if (EventSystem.current == null) return false;

            var eventData = new PointerEventData(EventSystem.current) { position = screenPosition };
            EventSystem.current.RaycastAll(eventData, _raycastResults);
            foreach (RaycastResult result in _raycastResults)
            {
                if (result.module is GraphicRaycaster) return true;
            }
            return false;
        }

        /// <summary>触られている間はタッチを優先する（2本目の指で飛ばないように）</summary>
        private static Pointer ResolvePointer()
        {
            Touchscreen touchscreen = Touchscreen.current;
            bool touching = touchscreen != null
                && (touchscreen.press.isPressed || touchscreen.press.wasReleasedThisFrame);

            return touching ? touchscreen : Pointer.current;
        }
    }
}
