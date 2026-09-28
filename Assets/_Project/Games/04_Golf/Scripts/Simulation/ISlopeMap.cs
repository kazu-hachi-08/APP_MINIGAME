using System.Numerics;

namespace MiniGame.Golf
{
    /// <summary>
    /// 地面の平面座標からグリーンの傾斜を引く窓口。IGroundMap と同じく、BallSimulator を Tilemap に依存させないために分ける。
    /// </summary>
    public interface ISlopeMap
    {
        /// <summary>下り方向を向き、長さが傾斜の強さのベクトル。傾斜のないマスはゼロ</summary>
        Vector2 GetSlope(Vector2 position);
    }
}
