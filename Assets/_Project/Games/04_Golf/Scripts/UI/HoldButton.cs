using UnityEngine;
using UnityEngine.EventSystems;

namespace MiniGame.Golf
{
    /// <summary>
    /// 押している間だけ IsHeld が true になるボタン（方向の◀▶）。
    /// Button の onClick は離した瞬間にしか届かないため、押しっぱなしを読めるようにする。
    /// </summary>
    public class HoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public bool IsHeld { get; private set; }

        public void OnPointerDown(PointerEventData eventData) => IsHeld = true;

        public void OnPointerUp(PointerEventData eventData) => IsHeld = false;

        // 押したまま指がボタンの外へずれたら止める
        public void OnPointerExit(PointerEventData eventData) => IsHeld = false;

        private void OnDisable() => IsHeld = false;
    }
}
