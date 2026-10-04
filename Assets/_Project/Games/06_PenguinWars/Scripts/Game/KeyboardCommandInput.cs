using UnityEngine;
using UnityEngine.InputSystem;

namespace MiniGame.PenguinWars
{
    /// <summary>
    /// PC確認用のキー操作（仕様書 §7.1）。1〜5=表示中ページの出撃、Tab=ページ切替、Q=働きペンギン、Space=ペンギン砲。
    /// ボタンと同じ処理を通すため、BattleRunner ではなく UI 部品を呼ぶ
    /// </summary>
    public class KeyboardCommandInput : MonoBehaviour
    {
        private static readonly Key[] SlotKeys = { Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5 };

        [SerializeField] private UnitButtonBar _buttonBar;
        [SerializeField] private WalletButton _walletButton;
        [SerializeField] private CannonButton _cannonButton;

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;

            for (int i = 0; i < SlotKeys.Length; i++)
            {
                if (keyboard[SlotKeys[i]].wasPressedThisFrame) _buttonBar.SpawnVisibleSlot(i);
            }
            if (keyboard.tabKey.wasPressedThisFrame) _buttonBar.TogglePage();
            if (keyboard.qKey.wasPressedThisFrame) _walletButton.LevelUp();
            if (keyboard.spaceKey.wasPressedThisFrame) _cannonButton.Fire();
        }
    }
}
