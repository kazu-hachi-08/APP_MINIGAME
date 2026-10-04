using MiniGame.PenguinWars.Battle;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MiniGame.PenguinWars
{
    /// <summary>PC確認用の数字キー出撃（仕様書 §7.1）。Phase 3 の出撃ボタンと並存させる</summary>
    public class KeyboardCommandInput : MonoBehaviour
    {
        private static readonly Key[] SlotKeys = { Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5 };

        [SerializeField] private BattleRunner _battleRunner;

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;

            for (int slot = 0; slot < SlotKeys.Length; slot++)
            {
                if (keyboard[SlotKeys[slot]].wasPressedThisFrame)
                {
                    _battleRunner.Enqueue(BattleCommand.Spawn(Side.Left, slot));
                }
            }
        }
    }
}
