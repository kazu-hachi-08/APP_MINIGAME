using UnityEngine;

namespace MiniGame.Golf
{
    /// <summary>
    /// 地面の種類ごとの転がり・跳ね返りの調整値（§8.4）。
    /// 計算に使う値はテストしやすいようエンジン非依存の TerrainPhysicsConfig にまとめて持つ。
    /// </summary>
    [CreateAssetMenu(fileName = "GolfTerrainSettings", menuName = "MiniGame/Golf/Terrain Settings")]
    public class GolfTerrainSettings : ScriptableObject
    {
        [SerializeField] private TerrainPhysicsConfig _terrain = new TerrainPhysicsConfig();

        public TerrainPhysicsConfig Terrain => _terrain;
    }
}
