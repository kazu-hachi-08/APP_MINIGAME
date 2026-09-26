using UnityEngine;

namespace MiniGame.Molkky
{
    /// <summary>
    /// 1回の投擲内容（§7.4）。入力とNPCが同じものを作ることで、投擲処理を共通化する。
    /// </summary>
    public readonly struct ThrowRequest
    {
        /// <summary>投擲ライン上の左右位置</summary>
        public readonly float PositionX;

        /// <summary>真っすぐ奥を0とした左右の角度（度・右が+）</summary>
        public readonly float AngleDegrees;

        public readonly float Speed;

        public readonly ThrowStyle Style;

        public ThrowRequest(float positionX, float angleDegrees, float speed, ThrowStyle style)
        {
            PositionX = positionX;
            AngleDegrees = angleDegrees;
            Speed = speed;
            Style = style;
        }

        /// <summary>地面平面上の進行方向（X＝左右／Y＝奥）</summary>
        public Vector2 Direction
        {
            get
            {
                float radians = AngleDegrees * Mathf.Deg2Rad;
                return new Vector2(Mathf.Sin(radians), Mathf.Cos(radians));
            }
        }
    }
}
