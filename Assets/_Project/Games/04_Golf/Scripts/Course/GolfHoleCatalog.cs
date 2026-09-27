using System.Collections.Generic;
using UnityEngine;

namespace MiniGame.Golf
{
    /// <summary>遊べるホールの一覧（§9.3）。ホールを追加したらここに登録する</summary>
    [CreateAssetMenu(fileName = "GolfHoleCatalog", menuName = "MiniGame/Golf/Hole Catalog")]
    public class GolfHoleCatalog : ScriptableObject
    {
        [SerializeField] private List<GolfHoleData> _holes = new List<GolfHoleData>();

        public IReadOnlyList<GolfHoleData> Holes => _holes;
    }
}
