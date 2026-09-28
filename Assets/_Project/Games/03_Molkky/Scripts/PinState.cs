using UnityEngine;

namespace MiniGame.Molkky
{
    /// <summary>
    /// ピン1本の投擲後の状態。オンライン対戦で、投げた側の端末の結果を相手端末へそのまま写すために使う。
    /// </summary>
    public readonly struct PinState
    {
        public readonly Vector2 Position;
        public readonly bool IsFallen;
        public readonly Vector2 FallDirection;

        public PinState(Vector2 position, bool isFallen, Vector2 fallDirection)
        {
            Position = position;
            IsFallen = isFallen;
            FallDirection = fallDirection;
        }
    }
}
