using System.Collections.Generic;
using MiniGame.PenguinWars.Battle;
using UnityEngine;

namespace MiniGame.PenguinWars
{
    /// <summary>
    /// UnitState.Id ごとに UnitView を出し入れする。出撃・撃破のたびに Instantiate/Destroy しないよう、消えたユニットの View は使い回す
    /// </summary>
    public class UnitViewPool : MonoBehaviour
    {
        [Tooltip("非アクティブの見本。これを複製して使う")]
        [SerializeField] private UnitView _template;
        [Tooltip("キャラNo から絵を引くため")]
        [SerializeField] private PenguinUnitCatalog _catalog;

        private readonly Dictionary<int, UnitView> _active = new Dictionary<int, UnitView>();
        private readonly Stack<UnitView> _free = new Stack<UnitView>();
        private readonly HashSet<int> _aliveIds = new HashSet<int>();
        private readonly List<int> _removedIds = new List<int>();

        /// <summary>今いるユニットに View を合わせ、いなくなったユニットの View を片付ける</summary>
        public void Sync(IReadOnlyList<UnitState> units)
        {
            _aliveIds.Clear();
            foreach (UnitState unit in units)
            {
                _aliveIds.Add(unit.Id);
                GetOrCreate(unit).Apply(unit);
            }

            _removedIds.Clear();
            foreach (int id in _active.Keys)
            {
                if (!_aliveIds.Contains(id)) _removedIds.Add(id);
            }
            foreach (int id in _removedIds)
            {
                Release(id);
            }
        }

        private UnitView GetOrCreate(UnitState unit)
        {
            if (_active.TryGetValue(unit.Id, out UnitView view)) return view;

            view = _free.Count > 0 ? _free.Pop() : Instantiate(_template, transform);
            // 使い回しの View は前のキャラの絵を持っているので、出し入れのたびに差し替える
            view.SetSprites(FindSprites(unit));
            view.gameObject.SetActive(true);
            _active.Add(unit.Id, view);
            return view;
        }

        private UnitSpriteSet FindSprites(UnitState unit)
        {
            PenguinUnitData data = _catalog.Get(unit.UnitNo);
            UnitSpriteSet sprites = data != null ? data.GetSprites(unit.Side) : null;
            if (sprites != null && sprites.IsValid) return sprites;

            Debug.LogWarning($"[UnitViewPool] No.{unit.UnitNo} の絵がありません。Tools > MiniGame > Rebuild PenguinWars を実行してください");
            return null;
        }

        private void Release(int id)
        {
            UnitView view = _active[id];
            _active.Remove(id);
            view.gameObject.SetActive(false);
            _free.Push(view);
        }
    }
}
