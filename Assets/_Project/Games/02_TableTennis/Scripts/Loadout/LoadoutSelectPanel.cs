using System;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.TableTennis
{
    /// <summary>
    /// 試合前に選手とラケットを1画面でまとめて選ぶパネル。
    /// NPC戦では相手（NPC）の分も選ばせ、オンラインでは自分の分だけを見せる。
    /// 初期選択は全てスタンダードにし、そのまま決定すれば従来と同じ遊び方になるようにしている。
    /// </summary>
    public class LoadoutSelectPanel : MonoBehaviour
    {
        /// <summary>1行ぶんの選択肢ボタン（一覧の並び順と対応する）</summary>
        [Serializable]
        private class ChoiceRow
        {
            public Button[] Buttons;

            [NonSerialized] public int Selected;
        }

        [SerializeField] private LoadoutCatalog _catalog;

        [SerializeField] private ChoiceRow _playerCharacterRow;
        [SerializeField] private ChoiceRow _playerRacketRow;

        [Tooltip("NPC戦のときだけ表示する、相手の選択欄")]
        [SerializeField] private GameObject _opponentGroup;

        [SerializeField] private ChoiceRow _opponentCharacterRow;
        [SerializeField] private ChoiceRow _opponentRacketRow;
        [SerializeField] private Button _decideButton;

        [Header("Colors")]
        [SerializeField] private Color _selectedColor = new Color(0.18f, 0.55f, 0.9f);
        [SerializeField] private Color _normalColor = new Color(0.3f, 0.33f, 0.4f);

        private Action<Loadout, Loadout> _onDecided;

        private void Awake()
        {
            SetupCharacterRow(_playerCharacterRow);
            SetupRacketRow(_playerRacketRow);
            SetupCharacterRow(_opponentCharacterRow);
            SetupRacketRow(_opponentRacketRow);
            _decideButton.onClick.AddListener(Decide);
        }

        /// <param name="withOpponent">相手の選択欄も出すか（NPC戦のみ true）</param>
        /// <param name="onDecided">決定時に（自分, 相手）の組み合わせを返す。相手欄を出していないときの相手は初期値</param>
        public void Show(bool withOpponent, Action<Loadout, Loadout> onDecided)
        {
            _onDecided = onDecided;
            _opponentGroup.SetActive(withOpponent);
            gameObject.SetActive(true);
        }

        /// <summary>決定せずに閉じる（選択中に通信が切れたときなど）</summary>
        public void Hide()
        {
            gameObject.SetActive(false);
            _onDecided = null;
        }

        private void SetupCharacterRow(ChoiceRow row)
        {
            SetupRow(row, i => $"{_catalog.Characters[i].DisplayName}\n({_catalog.Characters[i].Style.Label()})");
        }

        private void SetupRacketRow(ChoiceRow row)
        {
            SetupRow(row, i => _catalog.Rackets[i].Style.Label());
        }

        /// <summary>ラベルは一覧のデータから付けるので、選手名やタイプを変えてもボタンを作り直さなくてよい</summary>
        private void SetupRow(ChoiceRow row, Func<int, string> labelOf)
        {
            for (int i = 0; i < row.Buttons.Length; i++)
            {
                int index = i;
                row.Buttons[i].GetComponentInChildren<Text>().text = labelOf(i);
                row.Buttons[i].onClick.AddListener(() => Select(row, index));
            }

            Select(row, 0);
        }

        private void Select(ChoiceRow row, int index)
        {
            row.Selected = index;

            for (int i = 0; i < row.Buttons.Length; i++)
            {
                row.Buttons[i].GetComponent<Image>().color = i == index ? _selectedColor : _normalColor;
            }
        }

        private void Decide()
        {
            gameObject.SetActive(false);

            Loadout player = _catalog.Get(_playerCharacterRow.Selected, _playerRacketRow.Selected);
            Loadout opponent = _catalog.Get(_opponentCharacterRow.Selected, _opponentRacketRow.Selected);
            _onDecided?.Invoke(player, opponent);
            _onDecided = null;
        }
    }
}
