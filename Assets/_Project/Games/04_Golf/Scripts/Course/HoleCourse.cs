using UnityEngine;
using UnityEngine.Tilemaps;

namespace MiniGame.Golf
{
    /// <summary>
    /// 読み込んだホールの地面の種類・ティー・カップを問い合わせる窓口。ホールのプレハブのルートに付ける。
    /// Tilemap の外側はすべて OB。
    /// </summary>
    public class HoleCourse : MonoBehaviour, IGroundMap, ISlopeMap
    {
        [SerializeField] private Tilemap _terrain;
        [SerializeField] private Tilemap _slope;
        [SerializeField] private Transform _tee;
        [SerializeField] private Transform _cup;

        public Vector2 TeePosition => _tee.position;
        public Vector2 CupPosition => _cup.position;

        /// <summary>地面を塗った範囲（ワールド座標）。コース外の飾りをこの外側に置くために使う</summary>
        public Bounds TerrainBounds
        {
            get
            {
                Bounds bounds = _terrain.localBounds;
                bounds.center = _terrain.transform.TransformPoint(bounds.center);
                return bounds;
            }
        }

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

        public System.Numerics.Vector2 GetSlope(System.Numerics.Vector2 position)
        {
            if (_slope == null) return System.Numerics.Vector2.Zero;

            Vector3 pos = new Vector3(position.X, position.Y, 0f);
            Vector3Int cell = _slope.WorldToCell(pos);
            var tile = _slope.GetTile<GolfSlopeTile>(cell);
            if (tile == null) return System.Numerics.Vector2.Zero;

            Matrix4x4 matrix = _slope.GetTransformMatrix(cell);
            Vector3 rotatedSlope = matrix.MultiplyVector(new Vector3(tile.Slope.x, tile.Slope.y, 0f));

            return new System.Numerics.Vector2(rotatedSlope.x, rotatedSlope.y);
        }
    }
}
