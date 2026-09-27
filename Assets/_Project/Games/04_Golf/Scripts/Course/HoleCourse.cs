using UnityEngine;
using UnityEngine.Tilemaps;

namespace MiniGame.Golf
{
    /// <summary>
    /// 読み込んだホールの地面の種類・ティー・カップを問い合わせる窓口（§15）。ホールのプレハブのルートに付ける。
    /// Tilemap の外側はすべて OB（§9.2）。
    /// </summary>
    public class HoleCourse : MonoBehaviour, IGroundMap
    {
        [SerializeField] private Tilemap _terrain;
        [SerializeField] private Transform _tee;
        [SerializeField] private Transform _cup;

        public Vector2 TeePosition => _tee.position;
        public Vector2 CupPosition => _cup.position;

        public GroundType GetGround(Vector2 position)
        {
            Vector3Int cell = _terrain.WorldToCell(position);
            var tile = _terrain.GetTile<GolfTerrainTile>(cell);
            return tile != null ? tile.GroundType : GroundType.OutOfBounds;
        }

        GroundType IGroundMap.GetGround(System.Numerics.Vector2 position)
        {
            return GetGround(new Vector2(position.X, position.Y));
        }
    }
}
