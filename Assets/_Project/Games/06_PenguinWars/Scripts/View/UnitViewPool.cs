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
                GetOrCreate(unit.Id).Apply(unit);
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

        private UnitView GetOrCreate(int id)
        {
            if (_active.TryGetValue(id, out UnitView view)) return view;

            view = _free.Count > 0 ? _free.Pop() : Instantiate(_template, transform);
            view.gameObject.SetActive(true);
            _active.Add(id, view);
            return view;
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
