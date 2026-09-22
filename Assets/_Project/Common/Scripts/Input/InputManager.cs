using MiniGame.Common.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MiniGame.Common.Input
{
    /// <summary>
    /// キーボード入力と仮想UI入力を統合し、統一された入力を提供するマネージャー
    /// </summary>
    public class InputManager : SingletonMonoBehaviour<InputManager>, IInputProvider
    {
        [Header("Virtual Controls (Optional)")]
        [SerializeField] private VirtualJoystick _virtualJoystick;
        [SerializeField] private VirtualButton _virtualAction1Button;
        [SerializeField] private VirtualButton _virtualAction2Button;
        [SerializeField] private VirtualButton _virtualAction3Button;
        [SerializeField] private VirtualButton _virtualAction4Button;

        [Header("Settings")]
        [SerializeField] private bool _inputEnabled = true;

        public bool InputEnabled
        {
            get => _inputEnabled;
            set
            {
                _inputEnabled = value;
                if (!value)
                {
                    ResetAllInputs();
                }
            }
        }

        // IInputProvider 実装
        public Vector2 MoveVector { get; private set; }
        public bool IsAction1Down { get; private set; }
        public bool IsAction1Held { get; private set; }
        public bool IsAction1Up { get; private set; }

        public bool IsAction2Down { get; private set; }
        public bool IsAction2Held { get; private set; }
        public bool IsAction2Up { get; private set; }

        public bool IsAction3Down { get; private set; }
        public bool IsAction4Down { get; private set; }
        public bool IsPauseDown { get; private set; }

        private void Update()
        {
            var keyboard = Keyboard.current;

            if (!_inputEnabled)
            {
                ResetAllInputs();
                // ポーズ入力のみ無効化時も受け取れるようにする（ポーズ解除等）
                IsPauseDown = keyboard != null && (keyboard.escapeKey.wasPressedThisFrame || keyboard.pKey.wasPressedThisFrame);
                return;
            }

            ProcessMoveInput(keyboard);
            ProcessActionInputs(keyboard);
            ProcessSystemInputs(keyboard);
        }

        private void ProcessMoveInput(Keyboard keyboard)
        {
            Vector2 keyboardInput = Vector2.zero;

            if (keyboard != null)
            {
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) keyboardInput.y += 1f;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) keyboardInput.y -= 1f;
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) keyboardInput.x -= 1f;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) keyboardInput.x += 1f;
            }

            if (keyboardInput.sqrMagnitude > 1f)
            {
                keyboardInput.Normalize();
            }

            Vector2 virtualInput = _virtualJoystick != null ? _virtualJoystick.Direction : Vector2.zero;

            // キーボードまたはバーチャルジョイスティックのいずれか大きい方を採用
            Vector2 combined = keyboardInput.sqrMagnitude > virtualInput.sqrMagnitude ? keyboardInput : virtualInput;
            MoveVector = Vector2.ClampMagnitude(combined, 1f);
        }

        private void ProcessActionInputs(Keyboard keyboard)
        {
            // Action 1: Jキー / Zキー / Spaceキー / 仮想ボタン1
            bool key1Down = keyboard != null && (keyboard.jKey.wasPressedThisFrame || keyboard.zKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame);
            bool key1Held = keyboard != null && (keyboard.jKey.isPressed || keyboard.zKey.isPressed || keyboard.spaceKey.isPressed);
            bool key1Up = keyboard != null && (keyboard.jKey.wasReleasedThisFrame || keyboard.zKey.wasReleasedThisFrame || keyboard.spaceKey.wasReleasedThisFrame);

            bool v1Down = _virtualAction1Button != null && _virtualAction1Button.IsDown;
            bool v1Held = _virtualAction1Button != null && _virtualAction1Button.IsHeld;
            bool v1Up = _virtualAction1Button != null && _virtualAction1Button.IsUp;

            IsAction1Down = key1Down || v1Down;
            IsAction1Held = key1Held || v1Held;
            IsAction1Up = key1Up || v1Up;

            // Action 2: Kキー / Xキー / 仮想ボタン2
            bool key2Down = keyboard != null && (keyboard.kKey.wasPressedThisFrame || keyboard.xKey.wasPressedThisFrame);
            bool key2Held = keyboard != null && (keyboard.kKey.isPressed || keyboard.xKey.isPressed);
            bool key2Up = keyboard != null && (keyboard.kKey.wasReleasedThisFrame || keyboard.xKey.wasReleasedThisFrame);

            bool v2Down = _virtualAction2Button != null && _virtualAction2Button.IsDown;
            bool v2Held = _virtualAction2Button != null && _virtualAction2Button.IsHeld;
            bool v2Up = _virtualAction2Button != null && _virtualAction2Button.IsUp;

            IsAction2Down = key2Down || v2Down;
            IsAction2Held = key2Held || v2Held;
            IsAction2Up = key2Up || v2Up;

            // Action 3: Lキー / Cキー / 仮想ボタン3
            bool key3Down = keyboard != null && (keyboard.lKey.wasPressedThisFrame || keyboard.cKey.wasPressedThisFrame);
            bool v3Down = _virtualAction3Button != null && _virtualAction3Button.IsDown;

            IsAction3Down = key3Down || v3Down;

            // Action 4: 左Shiftキー / 仮想ボタン4（スライディングタックル用）
            bool key4Down = keyboard != null && keyboard.leftShiftKey.wasPressedThisFrame;
            bool v4Down = _virtualAction4Button != null && _virtualAction4Button.IsDown;

            IsAction4Down = key4Down || v4Down;
        }

        private void ProcessSystemInputs(Keyboard keyboard)
        {
            IsPauseDown = keyboard != null && (keyboard.escapeKey.wasPressedThisFrame || keyboard.pKey.wasPressedThisFrame);
        }

        public void RegisterVirtualControls(
            VirtualJoystick joystick,
            VirtualButton btn1 = null,
            VirtualButton btn2 = null,
            VirtualButton btn3 = null,
            VirtualButton btn4 = null)
        {
            _virtualJoystick = joystick;
            _virtualAction1Button = btn1;
            _virtualAction2Button = btn2;
            _virtualAction3Button = btn3;
            _virtualAction4Button = btn4;
        }

        public void UnregisterVirtualControls()
        {
            _virtualJoystick = null;
            _virtualAction1Button = null;
            _virtualAction2Button = null;
            _virtualAction3Button = null;
            _virtualAction4Button = null;
        }

        private void ResetAllInputs()
        {
            MoveVector = Vector2.zero;
            IsAction1Down = false;
            IsAction1Held = false;
            IsAction1Up = false;
            IsAction2Down = false;
            IsAction2Held = false;
            IsAction2Up = false;
            IsAction3Down = false;
            IsAction4Down = false;
            IsPauseDown = false;

            if (_virtualJoystick != null) _virtualJoystick.ResetInput();
            if (_virtualAction1Button != null) _virtualAction1Button.ResetButton();
            if (_virtualAction2Button != null) _virtualAction2Button.ResetButton();
            if (_virtualAction3Button != null) _virtualAction3Button.ResetButton();
            if (_virtualAction4Button != null) _virtualAction4Button.ResetButton();
        }
    }
}
