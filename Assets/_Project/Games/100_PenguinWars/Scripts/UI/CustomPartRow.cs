using System;
using System.Collections.Generic;
using MiniGame.PenguinWars.Art;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.PenguinWars
{
    /// <summary>
    /// じぶんペンギンの作成画面の「◀ パーツ名 ▶」1行。部位が違うだけで5行とも同じ動きなので使い回す。
    /// 端まで行ったら反対の端へ回る（ボタン2つで全パーツを見られるように）
    /// </summary>
    public class CustomPartRow : MonoBehaviour
    {
        [SerializeField] private PenguinPartSlot _slot;
        [SerializeField] private Text _slotLabel;
        [SerializeField] private Text _valueLabel;
        [SerializeField] private Button _prevButton;
        [SerializeField] private Button _nextButton;

        private int _index;

        /// <summary>◀▶ で値が変わったとき（SetValue では呼ばない）</summary>
        public event Action Changed;

        public PenguinPartSlot Slot => _slot;
        public string Value => Ids[_index];

        private IReadOnlyList<string> Ids => PenguinPartCatalog.Ids(_slot);

        private void Awake()
        {
            _slotLabel.text = PartLabels.Slot(_slot);
            _prevButton.onClick.AddListener(() => Step(-1));
            _nextButton.onClick.AddListener(() => Step(1));
        }

        /// <summary>知らない ID はその部位の既定に直して出す</summary>
        public void SetValue(string id)
        {
            _index = Mathf.Max(0, IndexOf(PenguinPartCatalog.Sanitize(_slot, id)));
            Refresh();
        }

        public void Randomize()
        {
            _index = UnityEngine.Random.Range(0, Ids.Count);
            Refresh();
        }

        private void Step(int direction)
        {
            PenguinUiSound.Click();
            _index = (_index + direction + Ids.Count) % Ids.Count;
            Refresh();
            Changed?.Invoke();
        }

        private int IndexOf(string id)
        {
            for (int i = 0; i < Ids.Count; i++)
            {
                if (Ids[i] == id) return i;
            }
            return -1;
        }

        private void Refresh() => _valueLabel.text = PartLabels.Part(_slot, Value);
    }
}
