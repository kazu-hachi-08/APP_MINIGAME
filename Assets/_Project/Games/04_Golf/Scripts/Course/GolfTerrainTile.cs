using UnityEngine;
using UnityEngine.Tilemaps;

namespace MiniGame.Golf
{
    /// <summary>
    /// 地面の種類を持つタイル（§9.2）。種類ごとに1アセット用意し、タイルの色がそのまま見た目になる。
    /// Tilemap に塗るだけでホールの形とライの両方が決まるので、ホールの形をコードに書かずに済む。
    /// </summary>
    [CreateAssetMenu(fileName = "GolfTerrainTile", menuName = "MiniGame/Golf/Terrain Tile")]
    public class GolfTerrainTile : Tile
    {
        [SerializeField] private GroundType _groundType = GroundType.Fairway;

        public GroundType GroundType => _groundType;
    }
}
