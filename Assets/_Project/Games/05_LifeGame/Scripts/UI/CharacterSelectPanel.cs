using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.LifeGame
{
    /// <summary>
    /// P1 → P2 → … の順で1人ずつキャラを選ぶ（仕様書 §9.3）。1台を回して遊ぶので「今選んでいる1人」だけを見せる。
    /// NPCはランダムで選んだ状態から始め、そのまま決定しても人間が代わりに変えてもよい。
    /// </summary>
    public class CharacterSelectPanel : MonoBehaviour
    {
        [SerializeField] private LifeCharacterCatalog _catalog;

        [SerializeField] private Text _titleText;
        [SerializeField] private Text _nameText;
        [SerializeField] private Text _abilityText;
        [SerializeField] private Button _prevButton;
        [SerializeField] private Button _nextButton;
        [SerializeField] private Button _confirmButton;
        [SerializeField] private Button _backButton;

        private IReadOnlyList<LifePlayerKind> _kinds;
        private int[] _selected;
        private int _seat;
        private Action<IReadOnlyList<int>> _onConfirmed;
        private Action _onBack;

        private void Awake()
        {
            _prevButton.onClick.AddListener(() => Cycle(-1));
            _nextButton.onClick.AddListener(() => Cycle(1));
            _confirmButton.onClick.AddListener(Confirm);
            _backButton.onClick.AddListener(Back);
        }

        /// <summary>全員決まったら席順のキャラ番号を返す。P1で「戻る」を押したら onBack を呼ぶ</summary>
        public void Show(IReadOnlyList<LifePlayerKind> kinds, Action<IReadOnlyList<int>> onConfirmed, Action onBack)
        {
            _kinds = kinds;
            _onConfirmed = onConfirmed;
            _onBack = onBack;
            _selected = new int[kinds.Count];
            for (int i = 0; i < kinds.Count; i++)
            {
                // 人間は先頭のキャラから、NPCは毎回違う相手になるようランダムから始める
                _selected[i] = kinds[i] == LifePlayerKind.Human ? 0 : UnityEngine.Random.Range(0, _catalog.Count);
            }

            _seat = 0;
            gameObject.SetActive(true);
            Refresh();
        }

        private void Cycle(int step)
        {
            _selected[_seat] = (_selected[_seat] + step + _catalog.Count) % _catalog.Count;
            Refresh();
        }

        private void Confirm()
        {
            _seat++;
            if (_seat < _selected.Length)
            {
                Refresh();
                return;
            }

            gameObject.SetActive(false);
            _onConfirmed?.Invoke(_selected);
        }

        private void Back()
        {
            _seat--;
            if (_seat >= 0)
            {
                Refresh();
                return;
            }

            gameObject.SetActive(false);
            _onBack?.Invoke();
        }

        private void Refresh()
        {
            string npc = _kinds[_seat] == LifePlayerKind.Npc ? "（NPC）" : "";
            _titleText.text = $"{LifeTexts.PlayerName(_seat)}{npc} のキャラを選んでね";
            // 端末を回したときに誰の番か一目で分かるよう、席の色で出す
            _titleText.color = LifeColors.Seat(_seat);

            LifeCharacterData character = _catalog.Get(_selected[_seat]);
            _nameText.text = character.DisplayName;
            _abilityText.text = character.AbilityText;
        }
    }
}
