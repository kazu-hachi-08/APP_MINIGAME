using System.Collections.Generic;
using UnityEngine;

namespace MiniGame.PenguinWars
{
    /// <summary>
    /// 非表示になった演出を使い回す。上限を超えたら新しく作らずに null を返す
    /// （敵が大量にいるときに演出の数だけ重くならないよう、出しきれない分は諦める）
    /// </summary>
    public class EffectPool<T> where T : PooledEffect
    {
        private readonly T _template;
        private readonly Transform _parent;
        private readonly int _maxCount;
        private readonly List<T> _items = new List<T>();

        public EffectPool(T template, Transform parent, int maxCount)
        {
            _template = template;
            _parent = parent;
            _maxCount = maxCount;
        }

        public T Get()
        {
            foreach (T item in _items)
            {
                if (!item.gameObject.activeSelf) return item;
            }
            if (_template == null || _items.Count >= _maxCount) return null;

            T created = Object.Instantiate(_template, _parent);
            _items.Add(created);
            return created;
        }
    }
}
