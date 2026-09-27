using UnityEngine;
using UnityEngine.Tilemaps;

namespace MiniGame.Golf
{
    /// <summary>
    /// 地面の種類を持つタイル（§9.2）。種類ごとに1アセット用意し、タイルの画像がそのまま見た目になる。
    /// Tilemap に塗るだけでホールの形とライの両方が決まるので、ホールの形をコードに書かずに済む。
    /// </summary>
    [CreateAssetMenu(fileName = "GolfTerrainTile", menuName = "MiniGame/Golf/Terrain Tile")]
    public class GolfTerrainTile : Tile
    {
        [SerializeField] private GroundType _groundType = GroundType.Fairway;

        [Tooltip("池の波のように動かしたいときだけ設定する。空なら静止画")]
        [SerializeField] private Sprite[] _animationFrames = new Sprite[0];

        [Tooltip("1秒あたりのコマ数")]
        [SerializeField] private float _animationSpeed = 2f;

        public GroundType GroundType => _groundType;

        public override bool GetTileAnimationData(Vector3Int position, ITilemap tilemap, ref TileAnimationData tileAnimationData)
        {
            if (_animationFrames == null || _animationFrames.Length == 0) return false;

            tileAnimationData.animatedSprites = _animationFrames;
            tileAnimationData.animationSpeed = _animationSpeed;
            // 全タイルの開始を揃えて、池全体が同じ波として動くようにする
            tileAnimationData.animationStartTime = 0f;
            return true;
        }
    }
}
