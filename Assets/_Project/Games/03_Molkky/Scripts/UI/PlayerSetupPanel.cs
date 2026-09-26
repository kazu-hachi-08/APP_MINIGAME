using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.Molkky
{
    /// <summary>
    /// 試合前のプレイヤー設定（§10.1）。人数（2〜4人）と、各プレイヤーの人間/NPC（難易度）を決める。
    /// 種別はボタンをタップするたびに 人間 → よわい → ふつう → つよい と切り替わる。
    /// </summary>
    public class PlayerSetupPanel : MonoBehaviour
    {
        public const int MinPlayers = 2;
        public const int MaxPlayers = 4;

        // PlayerKind の並びと合わせる
        private static readonly string[] KindLabels = { "人間", "NPC よわい", "NPC ふつう", "NPC つよい" };

        [Tooltip("2人・3人・4人 の順")]
        [SerializeField] private Button[] _countButtons;
        [SerializeField] private GameObject[] _playerRows;
        [SerializeField] private Button[] _kindButtons;
        [SerializeField] private Text[] _kindTexts;
        [SerializeField] private Button _startButton;

        [SerializeField] private Color _selectedColor = new Color(0.18f, 0.55f, 0.9f);
        [SerializeField] private Color _unselectedColor = new Color(0.3f, 0.33f, 0.4f);

        // 初回は「人間1人 vs NPC1人」ですぐ遊べるようにしておく
        private readonly PlayerKind[] _kinds =
            { PlayerKind.Human, PlayerKind.NpcNormal, PlayerKind.NpcNormal, PlayerKind.NpcNormal };

        private int _count = MinPlayers;
        private Action<IReadOnlyList<PlayerKind>> _onConfirmed;

        private void Awake()
        {
            for (int i = 0; i < _countButtons.Length; i++)
            {
                int count = MinPlayers + i;
                _countButtons[i].onClick.AddListener(() => SelectCount(count));
            }

            for (int i = 0; i < _kindButtons.Length; i++)
            {
                int index = i;
                _kindButtons[i].onClick.AddListener(() => CycleKind(index));
            }

            _startButton.onClick.AddListener(Confirm);
        }

        public void Show(Action<IReadOnlyList<PlayerKind>> onConfirmed)
        {
            _onConfirmed = onConfirmed;
            gameObject.SetActive(true);
            Refresh();
        }

        private void SelectCount(int count)
        {
            _count = count;
            Refresh();
        }

        private void CycleKind(int index)
        {
            _kinds[index] = (PlayerKind)(((int)_kinds[index] + 1) % KindLabels.Length);
            Refresh();
        }

        private void Refresh()
        {
            for (int i = 0; i < _countButtons.Length; i++)
            {
                _countButtons[i].image.color = MinPlayers + i == _count ? _selectedColor : _unselectedColor;
            }

            for (int i = 0; i < _playerRows.Length; i++)
            {
                _playerRows[i].SetActive(i < _count);
                _kindTexts[i].text = KindLabels[(int)_kinds[i]];
            }

            // 全員NPCは不可（§10.1）
            _startButton.interactable = HasHuman();
        }

        private bool HasHuman()
        {
            for (int i = 0; i < _count; i++)
            {
                if (_kinds[i] == PlayerKind.Human) return true;
            }

            return false;
        }

        private void Confirm()
        {
            if (!HasHuman()) return;

            var kinds = new List<PlayerKind>(_count);
            for (int i = 0; i < _count; i++) kinds.Add(_kinds[i]);

            gameObject.SetActive(false);
            _onConfirmed?.Invoke(kinds);
            _onConfirmed = null;
        }
    }
}
