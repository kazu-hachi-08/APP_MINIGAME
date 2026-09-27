using System;
using System.Numerics;

namespace MiniGame.Golf
{
    /// <summary>
    /// 1ホールの間変わらない風（§9.4）。向きは地面の平面（X＝左右／Y＝奥行きZ）で、風が吹いていく方向。
    /// </summary>
    public readonly struct Wind
    {
        public static readonly Wind Calm = new Wind(Vector2.UnitY, 0f);

        public Wind(Vector2 direction, float strength)
        {
            Direction = direction.LengthSquared() > 0f ? Vector2.Normalize(direction) : Vector2.UnitY;
            Strength = strength;
        }

        /// <summary>長さ1</summary>
        public Vector2 Direction { get; }

        /// <summary>強さ（m）。画面にもこの値を出す</summary>
        public float Strength { get; }

        public Vector2 Velocity => Direction * Strength;

        /// <summary>X軸（右）から反時計回りの角度（度）で作る</summary>
        public static Wind FromDegrees(float degrees, float strength)
        {
            float radians = degrees * MathF.PI / 180f;
            return new Wind(new Vector2(MathF.Cos(radians), MathF.Sin(radians)), strength);
        }
    }
}
