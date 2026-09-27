using UnityEngine;
using UnityEngine.Tilemaps;

namespace MiniGame.Golf
{
    /// <summary>
    /// グリーンの傾斜を持つタイル（§8.5）。
    /// 見た目の回転と合わせるため、基準となる傾斜方向をインスペクターで設定し、
    /// Tilemap 上での回転に応じて傾斜方向を変える。
    /// </summary>
    [CreateAssetMenu(fileName = "GolfSlopeTile", menuName = "MiniGame/Golf/Slope Tile")]
    public class GolfSlopeTile : Tile
    {
        [Tooltip("基準となる下り方向と強さ（回転していない時のベクトル）")]
        [SerializeField] private Vector2 _slope = new Vector2(0f, -1f);

        public Vector2 Slope => _slope;
    }
}
