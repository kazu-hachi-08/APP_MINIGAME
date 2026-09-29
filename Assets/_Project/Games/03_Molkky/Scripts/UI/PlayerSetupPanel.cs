using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.Molkky
{
    /// <summary>
    /// 試合前のプレイヤー設定。人数（2〜4人）と、各プレイヤーの人間/NPC（難易度）を決める。
    /// 種別はボタンをタップするたびに 人間 → よわい → ふつう → つよい と切り替わる。
    /// 3人以上のときは「個人戦 / チーム戦」を切り替えられ、チーム戦では各席の A / B をタップで切り替える。
    /// </summary>
    public class PlayerSetupPanel : MonoBehaviour
    {
        public const int MinPlayers = 2;
        public const int MaxPlayers = 4;

        // 2人のチーム戦は個人戦と同じになるので、3人から選べるようにする
        private const int MinTeamPlayers = 3;
        private const int TeamCount = 2;

        // PlayerKind の並びと合わせる
        private static readonly string[] KindLabels = { "人間", "NPC よわい", "NPC ふつう", "NPC つよい" };
        private static readonly string[] TeamLabels = { "A", "B" };

        [Tooltip("2人・3人・4人 の順")]
        [SerializeField] private Button[] _countButtons;
        [SerializeField] private GameObject[] _playerRows;
        [SerializeField] private Button[] _kindButtons;
        [SerializeField] private Text[] _kindTexts;
        [SerializeField] private Button _startButton;

        [Header("Team")]
        [Tooltip("「個人戦 / チーム戦」切り替えボタンを含む行。3人以上のときだけ表示する")]
        [SerializeField] private GameObject _modeRow;
        [SerializeField] private Button _modeButton;
        [SerializeField] private Text _modeText;
        [SerializeField] private Button[] _teamButtons;
        [SerializeField] private Text[] _teamTexts;

        [SerializeField] private Color _selectedColor = new Color(0.18f, 0.55f, 0.9f);
        [SerializeField] private Color _unselectedColor = new Color(0.3f, 0.33f, 0.4f);

        // 初回は「人間1人 vs NPC1人」ですぐ遊べるようにしておく
        private readonly PlayerKind[] _kinds =
            { PlayerKind.Human, PlayerKind.NpcNormal, PlayerKind.NpcNormal, PlayerKind.NpcNormal };

        private readonly int[] _teams = new int[MaxPlayers];

        private int _count = MinPlayers;
        private bool _isTeamMatch;
        private Action<IReadOnlyList<PlayerKind>, IReadOnlyList<int>> _onConfirmed;

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
                _teamButtons[i].onClick.AddListener(() => ToggleTeam(index));
            }

            _modeButton.onClick.AddListener(ToggleMode);
            _startButton.onClick.AddListener(Confirm);
        }

        /// <summary>
        /// 決定したら各席の種別と、チーム戦なら各席のチーム番号（0 = A）を返す。個人戦ならチームは null
        /// </summary>
        public void Show(Action<IReadOnlyList<PlayerKind>, IReadOnlyList<int>> onConfirmed)
        {
            _onConfirmed = onConfirmed;
            gameObject.SetActive(true);
            Refresh();
        }

        private void SelectCount(int count)
        {
            _count = count;
            if (_count < MinTeamPlayers) _isTeamMatch = false;
            Refresh();
        }

        private void CycleKind(int index)
        {
            _kinds[index] = (PlayerKind)(((int)_kinds[index] + 1) % KindLabels.Length);
            Refresh();
        }

        /// <summary>切り替えた直後は奇数席 = A、偶数席 = B にする（P1・P3 対 P2・P4）</summary>
        private void ToggleMode()
        {
            _isTeamMatch = !_isTeamMatch;
            for (int i = 0; i < _teams.Length; i++) _teams[i] = i % TeamCount;
            Refresh();
        }

        private void ToggleTeam(int index)
        {
            _teams[index] = (_teams[index] + 1) % TeamCount;
            Refresh();
        }

        private void Refresh()
        {
            RefreshCountButtons();
            RefreshModeRow();
            RefreshPlayerRows();

            _startButton.interactable = CanStart();
        }

        private void RefreshCountButtons()
        {
            for (int i = 0; i < _countButtons.Length; i++)
            {
                _countButtons[i].image.color = MinPlayers + i == _count ? _selectedColor : _unselectedColor;
            }
        }

        private void RefreshModeRow()
        {
            _modeRow.SetActive(_count >= MinTeamPlayers);
            _modeText.text = _isTeamMatch ? "チーム戦" : "個人戦";
            _modeButton.image.color = _isTeamMatch ? _selectedColor : _unselectedColor;
        }

        private void RefreshPlayerRows()
        {
            for (int i = 0; i < _playerRows.Length; i++)
            {
                _playerRows[i].SetActive(i < _count);
                _kindTexts[i].text = KindLabels[(int)_kinds[i]];

                _teamButtons[i].gameObject.SetActive(_isTeamMatch);
                _teamTexts[i].text = TeamLabels[_teams[i]];
                // チームA = 赤、チームB = 青（P1 / P2 の色）
                _teamButtons[i].image.color = MolkkyPlayerColors.Get(_teams[i]);
            }
        }

        /// <summary>誰も操作しない試合や、相手のいないチーム戦にならないようにする</summary>
        private bool CanStart()
        {
            return HasHuman() && (!_isTeamMatch || EveryTeamHasMember());
        }

        private bool HasHuman()
        {
            for (int i = 0; i < _count; i++)
            {
                if (_kinds[i] == PlayerKind.Human) return true;
            }

            return false;
        }

        private bool EveryTeamHasMember()
        {
            for (int team = 0; team < TeamCount; team++)
            {
                if (Array.IndexOf(_teams, team, 0, _count) < 0) return false;
            }

            return true;
        }

        private void Confirm()
        {
            if (!CanStart()) return;

            var kinds = new List<PlayerKind>(_count);
            for (int i = 0; i < _count; i++) kinds.Add(_kinds[i]);

            int[] teams = _isTeamMatch ? CopyTeams() : null;

            gameObject.SetActive(false);
            _onConfirmed?.Invoke(kinds, teams);
            _onConfirmed = null;
        }

        private int[] CopyTeams()
        {
            var teams = new int[_count];
            Array.Copy(_teams, teams, _count);
            return teams;
        }
    }
}
