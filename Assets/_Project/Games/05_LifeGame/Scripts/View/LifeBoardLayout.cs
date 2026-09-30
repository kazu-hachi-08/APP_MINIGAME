using UnityEngine;

namespace MiniGame.LifeGame
{
    /// <summary>
    /// マス番号 → 盤面上の座標（仕様書 §5.1）。LifeBoardLayoutGenerator がつづら折りで生成し、Inspector で手直しできる。
    /// マスの並び（区間・分岐）はシードが変わっても同じで、変わるのはマスの種類だけなので、座標は番号だけで引ける。
    /// </summary>
    [CreateAssetMenu(fileName = "LifeBoardLayout", menuName = "MiniGame/LifeGame/Board Layout")]
    public class LifeBoardLayout : ScriptableObject
    {
        [SerializeField] private Vector2[] _positions = new Vector2[0];

        public int Count => _positions.Length;

        public Vector2 PositionOf(int cellIndex) => _positions[cellIndex];
    }
}
