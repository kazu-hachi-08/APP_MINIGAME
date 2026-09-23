using MiniGame.Common.Input;
using UnityEngine;

namespace MiniGame.Soccer
{
    /// <summary>ゲスト端末から届くボタン操作（IInputProvider の Action1〜4 と同じ並び）</summary>
    public enum SoccerButton : byte
    {
        Pass = 1,
        Shoot = 2,
        Switch = 3,
        Tackle = 4
    }

    /// <summary>
    /// オンライン対戦のホスト端末で、ゲストから届いた入力を IInputProvider として見せる。
    /// AWAYの PlayerController / SlidingTackle / PlayerSwitcher はこれを InputManager の代わりに読む。
    /// </summary>
    // 他のコンポーネントが Update で「押した瞬間」を読む前に、届いたボタンをこのフレームの入力として確定させる
    [DefaultExecutionOrder(-100)]
    public class RemoteInputProvider : MonoBehaviour, IInputProvider
    {
        private bool _pendingPass;
        private bool _pendingShoot;
        private bool _pendingSwitch;
        private bool _pendingTackle;

        public Vector2 MoveVector { get; private set; }

        public bool IsAction1Down { get; private set; }
        public bool IsAction2Down { get; private set; }
        public bool IsAction3Down { get; private set; }
        public bool IsAction4Down { get; private set; }

        // サッカーでは長押し・離した瞬間・ポーズは相手の入力として使わない
        public bool IsAction1Held => false;
        public bool IsAction1Up => false;
        public bool IsAction2Held => false;
        public bool IsAction2Up => false;
        public bool IsPauseDown => false;

        public void SetMove(Vector2 move)
        {
            MoveVector = Vector2.ClampMagnitude(move, 1f);
        }

        public void Press(SoccerButton button)
        {
            switch (button)
            {
                case SoccerButton.Pass: _pendingPass = true; break;
                case SoccerButton.Shoot: _pendingShoot = true; break;
                case SoccerButton.Switch: _pendingSwitch = true; break;
                case SoccerButton.Tackle: _pendingTackle = true; break;
            }
        }

        /// <summary>切断時などに、最後に届いた入力のまま走り続けないよう止める</summary>
        public void Clear()
        {
            MoveVector = Vector2.zero;
            _pendingPass = _pendingShoot = _pendingSwitch = _pendingTackle = false;
        }

        private void Update()
        {
            // 押下は1フレームだけ true にする（InputManager の IsActionXDown と同じ扱い）
            IsAction1Down = _pendingPass;
            IsAction2Down = _pendingShoot;
            IsAction3Down = _pendingSwitch;
            IsAction4Down = _pendingTackle;
            _pendingPass = _pendingShoot = _pendingSwitch = _pendingTackle = false;
        }
    }
}
