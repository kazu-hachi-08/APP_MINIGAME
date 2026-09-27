using System.Numerics;

namespace MiniGame.Golf
{
    /// <summary>
    /// 地面の平面座標から地面の種類を引く窓口。
    /// BallSimulator を Tilemap（UnityEngine）に依存させず、テストでは一様な地面に差し替えられるようにする。
    /// </summary>
    public interface IGroundMap
    {
        GroundType GetGround(Vector2 position);
    }
}
