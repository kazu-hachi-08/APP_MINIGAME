using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MiniGame.Common.Input
{
    /// <summary>
    /// モバイル・タッチ操作向けのUGUI仮想アクションボタン
    /// </summary>
    public class VirtualButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public bool IsDown { get; private set; }
        public bool IsHeld { get; private set; }
        public bool IsUp { get; private set; }

        public event Action OnPressed;
        public event Action OnReleased;

        private bool _wasPressedThisFrame = false;
        private bool _wasReleasedThisFrame = false;

        public void OnPointerDown(PointerEventData eventData)
        {
            _wasPressedThisFrame = true;
            IsHeld = true;
            OnPressed?.Invoke();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _wasReleasedThisFrame = true;
            IsHeld = false;
            OnReleased?.Invoke();
        }

        private void LateUpdate()
        {
            // フレーム単位のDown / Upのフラグ更新
            IsDown = _wasPressedThisFrame;
            IsUp = _wasReleasedThisFrame;

            _wasPressedThisFrame = false;
            _wasReleasedThisFrame = false;
        }

        public void ResetButton()
        {
            IsDown = false;
            IsHeld = false;
            IsUp = false;
            _wasPressedThisFrame = false;
            _wasReleasedThisFrame = false;
        }
    }
}
