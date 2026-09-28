using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.Molkky
{
    /// <summary>
    /// 人数設定の後に、P1 → P2 → … の順で1人ずつキャラを選ぶパネル（キャラクター計画 Phase C2 / B2案）。
    /// 1台を回して遊ぶ前提なので、画面は「今選んでいる1人」だけを大きく見せる。
    /// NPCはランダムで選んだ状態から始め、そのまま決定しても人間が代わりに変えてもよい。
    /// </summary>
    public class CharacterSelectPanel : MonoBehaviour
    {
        [SerializeField] private MolkkyCharacterCatalog _catalog;

        [SerializeField] private Text _titleText;
        [SerializeField] private Image _portrait;
        [SerializeField] private Text _nameText;
        [SerializeField] private StatBarView _powerBar;
        [SerializeField] private StatBarView _controlBar;
        [SerializeField] private StatBarView _stickLengthBar;

        [SerializeField] private Button _prevButton;
        [SerializeField] private Button _nextButton;
        [SerializeField] private Button _confirmButton;
        [SerializeField] private Button _backButton;

        private IReadOnlyList<PlayerKind> _kinds;
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

        /// <summary>全員決まったら席順のキャラ番号一覧を返す。P1で「戻る」を押したら onBack を呼ぶ</summary>
        public void Show(IReadOnlyList<PlayerKind> kinds, Action<IReadOnlyList<int>> onConfirmed, Action onBack)
        {
            _kinds = kinds;
            _onConfirmed = onConfirmed;
            _onBack = onBack;
            _selected = CreateInitialSelection(kinds);
            _seat = 0;

            gameObject.SetActive(true);
            Refresh();
        }

        private int[] CreateInitialSelection(IReadOnlyList<PlayerKind> kinds)
        {
            var selected = new int[kinds.Count];
            for (int i = 0; i < kinds.Count; i++)
            {
                // 人間は基準のバランス型から、NPCは毎回違う相手になるようランダムから始める
                selected[i] = kinds[i] == PlayerKind.Human ? 0 : UnityEngine.Random.Range(0, _catalog.Count);
            }

            return selected;
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

            Close();
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

            Close();
            _onBack?.Invoke();
        }

        private void Close()
        {
            gameObject.SetActive(false);
        }

        private void Refresh()
        {
            bool isNpc = _kinds[_seat] != PlayerKind.Human;
            string owner = isNpc ? $"P{_seat + 1}（NPC）" : $"P{_seat + 1}";
            _titleText.text = $"{owner} のキャラを選んでね";
            // 端末を回したときに誰の番か一目で分かるよう、席の色で出す
            _titleText.color = MolkkyPlayerColors.Get(_seat);

            MolkkyCharacterData character = _catalog.Get(_selected[_seat]);
            _portrait.sprite = character.FrontSprite;
            _nameText.text = character.DisplayName;
            _powerBar.Show(character.PowerMultiplier);
            _controlBar.Show(character.ControlMultiplier);
            _stickLengthBar.Show(character.StickLengthMultiplier);
        }
    }
}
