using MiniGame.Common.Core;
using UnityEngine;

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
        public bool IsPauseDown { get; private set; }

        private void Update()
        {
            if (!_inputEnabled)
            {
                ResetAllInputs();
                // ポーズ入力のみ無効化時も受け取れるようにする（ポーズ解除等）
                IsPauseDown = UnityEngine.Input.GetKeyDown(KeyCode.Escape) || UnityEngine.Input.GetKeyDown(KeyCode.P);
                return;
            }

            ProcessMoveInput();
            ProcessActionInputs();
            ProcessSystemInputs();
        }

        private void ProcessMoveInput()
        {
            Vector2 keyboardInput = Vector2.zero;

            if (UnityEngine.Input.GetKey(KeyCode.W) || UnityEngine.Input.GetKey(KeyCode.UpArrow)) keyboardInput.y += 1f;
            if (UnityEngine.Input.GetKey(KeyCode.S) || UnityEngine.Input.GetKey(KeyCode.DownArrow)) keyboardInput.y -= 1f;
            if (UnityEngine.Input.GetKey(KeyCode.A) || UnityEngine.Input.GetKey(KeyCode.LeftArrow)) keyboardInput.x -= 1f;
            if (UnityEngine.Input.GetKey(KeyCode.D) || UnityEngine.Input.GetKey(KeyCode.RightArrow)) keyboardInput.x += 1f;

            if (keyboardInput.sqrMagnitude > 1f)
            {
                keyboardInput.Normalize();
            }

            Vector2 virtualInput = _virtualJoystick != null ? _virtualJoystick.Direction : Vector2.zero;

            // キーボードまたはバーチャルジョイスティックのいずれか大きい方を採用
            Vector2 combined = keyboardInput.sqrMagnitude > virtualInput.sqrMagnitude ? keyboardInput : virtualInput;
            MoveVector = Vector2.ClampMagnitude(combined, 1f);
        }

        private void ProcessActionInputs()
        {
            // Action 1: Jキー / Zキー / Spaceキー / 仮想ボタン1
            bool key1Down = UnityEngine.Input.GetKeyDown(KeyCode.J) || UnityEngine.Input.GetKeyDown(KeyCode.Z) || UnityEngine.Input.GetKeyDown(KeyCode.Space);
            bool key1Held = UnityEngine.Input.GetKey(KeyCode.J) || UnityEngine.Input.GetKey(KeyCode.Z) || UnityEngine.Input.GetKey(KeyCode.Space);
            bool key1Up = UnityEngine.Input.GetKeyUp(KeyCode.J) || UnityEngine.Input.GetKeyUp(KeyCode.Z) || UnityEngine.Input.GetKeyUp(KeyCode.Space);

            bool v1Down = _virtualAction1Button != null && _virtualAction1Button.IsDown;
            bool v1Held = _virtualAction1Button != null && _virtualAction1Button.IsHeld;
            bool v1Up = _virtualAction1Button != null && _virtualAction1Button.IsUp;

            IsAction1Down = key1Down || v1Down;
            IsAction1Held = key1Held || v1Held;
            IsAction1Up = key1Up || v1Up;

            // Action 2: Kキー / Xキー / 仮想ボタン2
            bool key2Down = UnityEngine.Input.GetKeyDown(KeyCode.K) || UnityEngine.Input.GetKeyDown(KeyCode.X);
            bool key2Held = UnityEngine.Input.GetKey(KeyCode.K) || UnityEngine.Input.GetKey(KeyCode.X);
            bool key2Up = UnityEngine.Input.GetKeyUp(KeyCode.K) || UnityEngine.Input.GetKeyUp(KeyCode.X);

            bool v2Down = _virtualAction2Button != null && _virtualAction2Button.IsDown;
            bool v2Held = _virtualAction2Button != null && _virtualAction2Button.IsHeld;
            bool v2Up = _virtualAction2Button != null && _virtualAction2Button.IsUp;

            IsAction2Down = key2Down || v2Down;
            IsAction2Held = key2Held || v2Held;
            IsAction2Up = key2Up || v2Up;

            // Action 3: Lキー / Cキー / 仮想ボタン3
            bool key3Down = UnityEngine.Input.GetKeyDown(KeyCode.L) || UnityEngine.Input.GetKeyDown(KeyCode.C);
            bool v3Down = _virtualAction3Button != null && _virtualAction3Button.IsDown;

            IsAction3Down = key3Down || v3Down;
        }

        private void ProcessSystemInputs()
        {
            IsPauseDown = UnityEngine.Input.GetKeyDown(KeyCode.Escape) || UnityEngine.Input.GetKeyDown(KeyCode.P);
        }

        public void RegisterVirtualControls(
            VirtualJoystick joystick,
            VirtualButton btn1 = null,
            VirtualButton btn2 = null,
            VirtualButton btn3 = null)
        {
            _virtualJoystick = joystick;
            _virtualAction1Button = btn1;
            _virtualAction2Button = btn2;
            _virtualAction3Button = btn3;
        }

        public void UnregisterVirtualControls()
        {
            _virtualJoystick = null;
            _virtualAction1Button = null;
            _virtualAction2Button = null;
            _virtualAction3Button = null;
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
            IsPauseDown = false;

            if (_virtualJoystick != null) _virtualJoystick.ResetInput();
            if (_virtualAction1Button != null) _virtualAction1Button.ResetButton();
            if (_virtualAction2Button != null) _virtualAction2Button.ResetButton();
            if (_virtualAction3Button != null) _virtualAction3Button.ResetButton();
        }
    }
}
