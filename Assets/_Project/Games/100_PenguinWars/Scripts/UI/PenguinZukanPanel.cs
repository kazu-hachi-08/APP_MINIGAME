using System.Collections.Generic;
using MiniGame.PenguinWars.Battle;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.PenguinWars
{
    /// <summary>
    /// タイトルから開くキャラずかん（仕様書 §2.0）。マスはカタログから実行時に作るので、
    /// キャラを足しても Rebuild PenguinWars でカタログを作り直すだけで並ぶ（Scene に50マス焼き込むと、マスの数を Scene 側でも合わせる必要が出るため）
    /// </summary>
    public class PenguinZukanPanel : MonoBehaviour
    {
        private const string AscendingLabel = "小さい順";
        private const string DescendingLabel = "大きい順";
        private const string RoleLabelPrefix = "役割: ";
        private const string AbilityLabelPrefix = "能力: ";

        [SerializeField] private PenguinUnitCatalog _catalog;
        [Tooltip("非表示のひな形。キャラの数だけ複製する")]
        [SerializeField] private ZukanCell _cellTemplate;
        [SerializeField] private ScrollRect _scroll;
        [SerializeField] private ZukanDetailPanel _detailPanel;
        [SerializeField] private Button _closeButton;
        [Tooltip("左上に出す数値（＝並べ替えの基準）を一覧から選ぶ")]
        [SerializeField] private Dropdown _columnDropdown;
        [SerializeField] private Button _orderButton;
        [Tooltip("しぼりこむ役割を一覧から選ぶ。能力とは別に選べるようにし、「遠距離で範囲」のような組み合わせで探せるようにする")]
        [SerializeField] private Dropdown _roleDropdown;
        [Tooltip("しぼりこむ能力を一覧から選ぶ。10個あるので、押して順に回すより一覧のほうが早い")]
        [SerializeField] private Dropdown _abilityDropdown;
        [Tooltip("隣のマスと歩くタイミングをずらすコマ数")]
        [SerializeField] private float _walkPhaseStep = 0.37f;

        private readonly List<ZukanCell> _cells = new List<ZukanCell>();
        private int _columnIndex;
        private bool _isDescending;
        private int _roleIndex;
        private int _abilityIndex;

        private void Awake()
        {
            _closeButton.onClick.AddListener(Close);
            _orderButton.onClick.AddListener(ToggleOrder);
            SetupDropdown(_columnDropdown, ZukanListOptions.ColumnLabels(), SelectColumn);
            SetupDropdown(_roleDropdown, ZukanListOptions.FilterLabels(ZukanListOptions.Roles), SelectRole);
            SetupDropdown(_abilityDropdown, ZukanListOptions.FilterLabels(ZukanListOptions.Abilities), SelectAbility);
        }

        private static void SetupDropdown(Dropdown dropdown, List<string> labels, UnityEngine.Events.UnityAction<int> onSelect)
        {
            dropdown.ClearOptions();
            dropdown.AddOptions(labels);
            dropdown.onValueChanged.AddListener(onSelect);
        }

        public void Show()
        {
            BuildCellsOnce();
            ApplyUnlocks();
            ResetListState();
            _detailPanel.Hide();
            gameObject.SetActive(true);
        }

        public void Hide() => gameObject.SetActive(false);

        private void BuildCellsOnce()
        {
            if (_cells.Count > 0) return;

            int index = 0;
            foreach (PenguinUnitData unit in _catalog.Units)
            {
                if (unit == null) continue;
                ZukanCell cell = Instantiate(_cellTemplate, _cellTemplate.transform.parent);
                cell.Show(unit, index * _walkPhaseStep, _detailPanel.Show);
                _cells.Add(cell);
                index++;
            }
        }

        /// <summary>
        /// ステージモードでまだ仲間になっていないキャラはシルエットにする。開くたびにセーブを読む
        /// （ステージをクリアしてタイトルに戻ったとき、増えた仲間がすぐ見えるように）
        /// </summary>
        private void ApplyUnlocks()
        {
            CampaignProgress progress = CampaignSave.Load();
            foreach (ZukanCell cell in _cells) cell.SetLocked(!CampaignUnlocks.IsUnlocked(progress, cell.Stats.UnitNo));
        }

        /// <summary>開くたびに「コスト・小さい順・役割すべて・能力すべて」に戻す（前回の絞り込みが残っていると、キャラが減ったように見えるため）</summary>
        private void ResetListState()
        {
            _columnIndex = 0;
            _isDescending = false;
            _roleIndex = 0;
            _abilityIndex = 0;
            // 選び直しの通知は要らない（RefreshList を直接呼ぶ）ので、通知なしで表示だけ戻す
            _columnDropdown.SetValueWithoutNotify(_columnIndex);
            _roleDropdown.SetValueWithoutNotify(_roleIndex);
            _abilityDropdown.SetValueWithoutNotify(_abilityIndex);
            RefreshList();
        }

        private void SelectColumn(int index)
        {
            _columnIndex = index;
            OnListOptionChanged();
        }

        private void ToggleOrder()
        {
            _isDescending = !_isDescending;
            OnListOptionChanged();
        }

        private void SelectRole(int index)
        {
            _roleIndex = index;
            OnListOptionChanged();
        }

        private void SelectAbility(int index)
        {
            _abilityIndex = index;
            OnListOptionChanged();
        }

        private void OnListOptionChanged()
        {
            PenguinUiSound.Click();
            RefreshList();
        }

        /// <summary>マスは作り直さず、並び順（SiblingIndex）と表示/非表示だけ変える。GridLayoutGroup が詰め直してくれる</summary>
        private void RefreshList()
        {
            ZukanStatColumn column = ZukanListOptions.Columns[_columnIndex];
            ZukanFilter role = ZukanListOptions.Roles[_roleIndex];
            ZukanFilter ability = ZukanListOptions.Abilities[_abilityIndex];

            _cells.Sort((a, b) => ZukanListOptions.Compare(a.Stats, b.Stats, column, _isDescending));
            for (int i = 0; i < _cells.Count; i++)
            {
                ZukanCell cell = _cells[i];
                cell.transform.SetSiblingIndex(i);
                cell.ShowValue(column.Format(cell.Stats));
                cell.gameObject.SetActive(role.Matches(cell.Stats) && ability.Matches(cell.Stats));
            }

            _orderButton.GetComponentInChildren<Text>().text = _isDescending ? DescendingLabel : AscendingLabel;
            // 一覧の中は名前だけにし、閉じたときの見出しにだけ「役割: 」「能力: 」を付けてどちらのしぼりこみか分かるようにする
            _roleDropdown.captionText.text = RoleLabelPrefix + role.Label;
            _abilityDropdown.captionText.text = AbilityLabelPrefix + ability.Label;
            _scroll.verticalNormalizedPosition = 1f;
        }

        private void Close()
        {
            PenguinUiSound.Click();
            Hide();
        }
    }
}
