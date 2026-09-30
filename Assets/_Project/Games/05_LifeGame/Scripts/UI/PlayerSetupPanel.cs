using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.LifeGame
{
    /// <summary>
    /// 試合前のプレイヤー設定。人数（2〜4人）と各席の人間/NPCを決める（仕様書 §10.1）。
    /// モルックの同名パネルを元にしているが、チーム戦・難易度が無いぶん小さいので共通化はしない。
    /// </summary>
    public class PlayerSetupPanel : MonoBehaviour
    {
        public const int MinPlayers = 2;
        public const int MaxPlayers = 4;

        [Tooltip("2人・3人・4人 の順")]
        [SerializeField] private Button[] _countButtons;
        [SerializeField] private GameObject[] _playerRows;
        [SerializeField] private Button[] _kindButtons;
        [SerializeField] private Text[] _kindTexts;
        [SerializeField] private Button _startButton;
        [SerializeField] private Button _backButton;

        [SerializeField] private Color _selectedColor = new Color(0.18f, 0.55f, 0.9f);
        [SerializeField] private Color _unselectedColor = new Color(0.3f, 0.33f, 0.4f);

        // 初期状態は「2人・P1人間・P2 NPC」（仕様書 §10.1）。3人目以降も NPC にしておき、人数を増やすだけで遊べるようにする
        private readonly LifePlayerKind[] _kinds =
            { LifePlayerKind.Human, LifePlayerKind.Npc, LifePlayerKind.Npc, LifePlayerKind.Npc };

        private int _count = MinPlayers;
        private Action<IReadOnlyList<LifePlayerKind>> _onConfirmed;
        private Action _onBack;

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
                _kindButtons[i].onClick.AddListener(() => ToggleKind(index));
            }

            _startButton.onClick.AddListener(Confirm);
            _backButton.onClick.AddListener(Back);
        }

        /// <summary>「戻る」を押したら onBack を呼ぶ（テーマ選択へ戻る）</summary>
        public void Show(Action<IReadOnlyList<LifePlayerKind>> onConfirmed, Action onBack)
        {
            _onConfirmed = onConfirmed;
            _onBack = onBack;
            gameObject.SetActive(true);
            Refresh();
        }

        private void SelectCount(int count)
        {
            _count = count;
            Refresh();
        }

        private void ToggleKind(int index)
        {
            _kinds[index] = _kinds[index] == LifePlayerKind.Human ? LifePlayerKind.Npc : LifePlayerKind.Human;
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
                _kindTexts[i].text = _kinds[i] == LifePlayerKind.Human ? "人間" : "NPC";
            }

            _startButton.interactable = HasHuman();
        }

        /// <summary>全員NPCは不可（誰も操作しない試合になるため）</summary>
        private bool HasHuman()
        {
            for (int i = 0; i < _count; i++)
            {
                if (_kinds[i] == LifePlayerKind.Human) return true;
            }

            return false;
        }

        private void Confirm()
        {
            if (!HasHuman()) return;

            var kinds = new List<LifePlayerKind>(_count);
            for (int i = 0; i < _count; i++) kinds.Add(_kinds[i]);

            gameObject.SetActive(false);
            _onConfirmed?.Invoke(kinds);
        }

        private void Back()
        {
            gameObject.SetActive(false);
            _onBack?.Invoke();
        }
    }
}
