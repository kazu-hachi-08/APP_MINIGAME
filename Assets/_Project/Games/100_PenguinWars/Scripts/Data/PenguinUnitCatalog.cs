using System.Collections.Generic;
using UnityEngine;

namespace MiniGame.PenguinWars
{
    /// <summary>全キャラの一覧。中身は Tools > MiniGame > Rebuild PenguinWars が Data/Units/ から集めて入れる</summary>
    [CreateAssetMenu(fileName = "PenguinUnitCatalog", menuName = "MiniGame/PenguinWars/Unit Catalog")]
    public class PenguinUnitCatalog : ScriptableObject
    {
        [SerializeField] private List<PenguinUnitData> _units = new List<PenguinUnitData>();

        public IReadOnlyList<PenguinUnitData> Units => _units;

        /// <summary>見つからなければ null（No の打ち間違いを呼び出し側で気づけるように）</summary>
        public PenguinUnitData Get(int no)
        {
            foreach (PenguinUnitData unit in _units)
            {
                if (unit != null && unit.No == no) return unit;
            }
            return null;
        }
    }
}
