using System;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.Golf
{
    /// <summary>
    /// 試合前の設定（§11.1）。モード（1ホール／3ホール）と人数（2〜4人）を選ぶ。人間/NPCの切り替えは Phase 7 で足す。
    /// 登録ホールが3つ未満なら3ホールモードは押せない（§6.2）。
    /// </summary>
    public class GolfSetupPanel : MonoBehaviour
    {
        public const int MinPlayers = 2;
        public const int MaxPlayers = 4;

        [SerializeField] private Button _oneHoleButton;
        [SerializeField] private Button _threeHoleButton;

        [Tooltip("2人・3人・4人 の順")]
        [SerializeField] private Button[] _countButtons;
        [SerializeField] private Button _startButton;

        [SerializeField] private Color _selectedColor = new Color(0.18f, 0.55f, 0.9f);
        [SerializeField] private Color _unselectedColor = new Color(0.3f, 0.33f, 0.4f);

        private const int OneHole = 1;

        private int _holeCount = OneHole;
        private int _playerCount = MinPlayers;
        private Action<int, int> _onConfirmed;

        private void Awake()
        {
            _oneHoleButton.onClick.AddListener(() => SelectHoleCount(OneHole));
            _threeHoleButton.onClick.AddListener(() => SelectHoleCount(GolfRules.LongModeHoleCount));

            for (int i = 0; i < _countButtons.Length; i++)
            {
                int count = MinPlayers + i;
                _countButtons[i].onClick.AddListener(() => SelectPlayerCount(count));
            }

            _startButton.onClick.AddListener(Confirm);
        }

        /// <summary>onConfirmed にはホール数と人数を渡す</summary>
        public void Show(int availableHoles, Action<int, int> onConfirmed)
        {
            _onConfirmed = onConfirmed;
            _threeHoleButton.interactable = availableHoles >= GolfRules.LongModeHoleCount;
            gameObject.SetActive(true);
            Refresh();
        }

        private void SelectHoleCount(int holeCount)
        {
            _holeCount = holeCount;
            Refresh();
        }

        private void SelectPlayerCount(int count)
        {
            _playerCount = count;
            Refresh();
        }

        private void Refresh()
        {
            _oneHoleButton.image.color = _holeCount == OneHole ? _selectedColor : _unselectedColor;
            _threeHoleButton.image.color = _holeCount == GolfRules.LongModeHoleCount ? _selectedColor : _unselectedColor;

            for (int i = 0; i < _countButtons.Length; i++)
            {
                _countButtons[i].image.color = MinPlayers + i == _playerCount ? _selectedColor : _unselectedColor;
            }
        }

        private void Confirm()
        {
            gameObject.SetActive(false);
            _onConfirmed?.Invoke(_holeCount, _playerCount);
            _onConfirmed = null;
        }
    }
}
