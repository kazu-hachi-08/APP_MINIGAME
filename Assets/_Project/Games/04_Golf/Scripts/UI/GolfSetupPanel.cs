using System;
using System.Collections.Generic;
using MiniGame.Common.Profile;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.Golf
{
    /// <summary>
    /// 試合前の設定。モード（1ホール／3ホール）・人数（2〜4人）・各プレイヤーの人間/NPC を選ぶ。
    /// 登録ホールが3つ未満なら3ホールモードは押せない。全員NPCでは開始できない。
    /// </summary>
    public class GolfSetupPanel : MonoBehaviour
    {
        public const int MinPlayers = 2;
        public const int MaxPlayers = 4;

        [SerializeField] private Button _oneHoleButton;
        [SerializeField] private Button _threeHoleButton;

        [Tooltip("2人・3人・4人 の順")]
        [SerializeField] private Button[] _countButtons;

        [Tooltip("P1〜P4 の順。押すたびに 人間 → NPCよわい → ふつう → つよい と切り替わる")]
        [SerializeField] private Button[] _typeButtons;
        [SerializeField] private Button _startButton;

        [SerializeField] private Color _selectedColor = new Color(0.18f, 0.55f, 0.9f);
        [SerializeField] private Color _unselectedColor = new Color(0.3f, 0.33f, 0.4f);
        [SerializeField] private Color _npcColor = new Color(0.55f, 0.35f, 0.75f);

        private const int OneHole = 1;
        private static readonly int PlayerTypeCount = Enum.GetValues(typeof(GolfPlayerType)).Length;

        private int _holeCount = OneHole;
        private int _playerCount = MinPlayers;
        private readonly GolfPlayerType[] _types = new GolfPlayerType[MaxPlayers];
        private Action<int, IReadOnlyList<GolfPlayerType>> _onConfirmed;

        private void Awake()
        {
            _oneHoleButton.onClick.AddListener(() => SelectHoleCount(OneHole));
            _threeHoleButton.onClick.AddListener(() => SelectHoleCount(GolfRules.LongModeHoleCount));

            for (int i = 0; i < _countButtons.Length; i++)
            {
                int count = MinPlayers + i;
                _countButtons[i].onClick.AddListener(() => SelectPlayerCount(count));
            }

            for (int i = 0; i < _typeButtons.Length; i++)
            {
                int seat = i;
                _typeButtons[i].onClick.AddListener(() => CycleType(seat));
            }

            _startButton.onClick.AddListener(Confirm);
        }

        /// <summary>onConfirmed にはホール数と、人数ぶんの人間/NPC を渡す</summary>
        public void Show(int availableHoles, Action<int, IReadOnlyList<GolfPlayerType>> onConfirmed)
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

        private void CycleType(int seat)
        {
            _types[seat] = (GolfPlayerType)(((int)_types[seat] + 1) % PlayerTypeCount);
            Refresh();
        }

        private void Refresh()
        {
            // この画面はオフライン専用。P1 を NPC にしたらユーザー名を外すので、ラベルを描く前に席名を決め直す
            SeatNames.UseLocal(_types[0] == GolfPlayerType.Human);

            _oneHoleButton.image.color = SelectionColor(_holeCount == OneHole);
            _threeHoleButton.image.color = SelectionColor(_holeCount == GolfRules.LongModeHoleCount);

            for (int i = 0; i < _countButtons.Length; i++)
            {
                _countButtons[i].image.color = SelectionColor(MinPlayers + i == _playerCount);
            }

            for (int i = 0; i < _typeButtons.Length; i++) RefreshTypeButton(i);

            _startButton.interactable = HasHuman();
        }

        /// <summary>人数より後ろの席は隠す。NPC の席は色を変えて、人間の席と見分けやすくする</summary>
        private void RefreshTypeButton(int seat)
        {
            Button button = _typeButtons[seat];
            button.gameObject.SetActive(seat < _playerCount);
            button.image.color = _types[seat] == GolfPlayerType.Human ? _unselectedColor : _npcColor;
            button.GetComponentInChildren<Text>().text =
                $"{GolfPlayerColors.Colored(seat, GolfPlayerColors.Name(seat))}\n{GolfPlayerColors.TypeName(_types[seat])}";
        }

        private Color SelectionColor(bool selected)
        {
            return selected ? _selectedColor : _unselectedColor;
        }

        /// <summary>全員NPCは不可（最低1人は人間）</summary>
        private bool HasHuman()
        {
            for (int i = 0; i < _playerCount; i++)
            {
                if (_types[i] == GolfPlayerType.Human) return true;
            }

            return false;
        }

        private void Confirm()
        {
            if (!HasHuman()) return;

            var types = new GolfPlayerType[_playerCount];
            Array.Copy(_types, types, _playerCount);

            gameObject.SetActive(false);
            _onConfirmed?.Invoke(_holeCount, types);
            _onConfirmed = null;
        }
    }
}
