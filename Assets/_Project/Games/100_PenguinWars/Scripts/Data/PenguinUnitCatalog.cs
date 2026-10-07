using System;
using System.Collections.Generic;
using UnityEngine;

namespace MiniGame.PenguinWars
{
    /// <summary>全キャラの一覧。中身は Tools > MiniGame > Rebuild PenguinWars が Data/Units/ から集めて入れる</summary>
    [CreateAssetMenu(fileName = "PenguinUnitCatalog", menuName = "MiniGame/PenguinWars/Unit Catalog")]
    public class PenguinUnitCatalog : ScriptableObject
    {
        [SerializeField] private List<PenguinUnitData> _units = new List<PenguinUnitData>();

        // 実行時に作ったキャラ（じぶんペンギン）。アセットに残すと次の再生や Rebuild に紛れ込むのでシリアライズしない
        [NonSerialized] private Dictionary<int, PenguinUnitData> _runtimeUnits;

        /// <summary>アセットの全キャラ。実行時登録は含めない（ずかん・ドラフトの候補に混ざらないように）</summary>
        public IReadOnlyList<PenguinUnitData> Units => _units;

        /// <summary>実行時に登録したキャラ（対戦のじぶんペンギン）。戦闘の数値を集めるときに Units と合わせて使う</summary>
        public IEnumerable<PenguinUnitData> RuntimeUnits =>
            _runtimeUnits != null ? _runtimeUnits.Values : (IEnumerable<PenguinUnitData>)Array.Empty<PenguinUnitData>();

        /// <summary>同じ No が登録済みなら古い方を捨てて差し替える（作成画面のプレビューで作り直すため）</summary>
        public void RegisterRuntime(PenguinUnitData data)
        {
            _runtimeUnits ??= new Dictionary<int, PenguinUnitData>();
            if (_runtimeUnits.TryGetValue(data.No, out PenguinUnitData old) && old != null && old != data) old.DestroyRuntime();
            _runtimeUnits[data.No] = data;
        }

        public void ClearRuntime()
        {
            if (_runtimeUnits == null) return;

            foreach (PenguinUnitData data in _runtimeUnits.Values)
            {
                if (data != null) data.DestroyRuntime();
            }
            _runtimeUnits.Clear();
        }

        /// <summary>見つからなければ null（No の打ち間違いを呼び出し側で気づけるように）。実行時登録を先に見る</summary>
        public PenguinUnitData Get(int no)
        {
            if (_runtimeUnits != null && _runtimeUnits.TryGetValue(no, out PenguinUnitData runtime) && runtime != null)
            {
                return runtime;
            }
            foreach (PenguinUnitData unit in _units)
            {
                if (unit != null && unit.No == no) return unit;
            }
            return null;
        }
    }
}
