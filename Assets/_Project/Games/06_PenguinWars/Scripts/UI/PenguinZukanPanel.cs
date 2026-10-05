using System.Collections.Generic;
using MiniGame.Common.Audio;
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
        private const string TraitLabelPrefix = "特性: ";

        [SerializeField] private PenguinUnitCatalog _catalog;
        [Tooltip("非表示のひな形。キャラの数だけ複製する")]
        [SerializeField] private ZukanCell _cellTemplate;
        [SerializeField] private ScrollRect _scroll;
        [SerializeField] private ZukanDetailPanel _detailPanel;
        [SerializeField] private Button _closeButton;
        [Tooltip("左上に出す数値（＝並べ替えの基準）を一覧から選ぶ")]
        [SerializeField] private Dropdown _columnDropdown;
        [SerializeField] private Button _orderButton;
        [Tooltip("しぼりこむ特性を一覧から選ぶ。14個あるので、押して順に回すより一覧のほうが早い")]
        [SerializeField] private Dropdown _traitDropdown;
        [Tooltip("隣のマスと歩くタイミングをずらすコマ数")]
        [SerializeField] private float _walkPhaseStep = 0.37f;

        private readonly List<ZukanCell> _cells = new List<ZukanCell>();
        private int _columnIndex;
        private bool _isDescending;
        private int _traitIndex;

        private void Awake()
        {
            _closeButton.onClick.AddListener(Close);
            _orderButton.onClick.AddListener(ToggleOrder);
            SetupDropdown(_columnDropdown, ColumnLabels(), SelectColumn);
            SetupDropdown(_traitDropdown, TraitLabels(), SelectTrait);
        }

        private static void SetupDropdown(Dropdown dropdown, List<string> labels, UnityEngine.Events.UnityAction<int> onSelect)
        {
            dropdown.ClearOptions();
            dropdown.AddOptions(labels);
            dropdown.onValueChanged.AddListener(onSelect);
        }

        private static List<string> ColumnLabels()
        {
            var labels = new List<string>();
            foreach (ZukanStatColumn column in ZukanListOptions.Columns) labels.Add(column.Label);
            return labels;
        }

        private static List<string> TraitLabels()
        {
            var labels = new List<string>();
            foreach (ZukanTraitFilter trait in ZukanListOptions.Traits) labels.Add(trait.Label);
            return labels;
        }

        public void Show()
        {
            BuildCellsOnce();
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

        /// <summary>開くたびに「コスト・小さい順・すべて」に戻す（前回の絞り込みが残っていると、キャラが減ったように見えるため）</summary>
        private void ResetListState()
        {
            _columnIndex = 0;
            _isDescending = false;
            _traitIndex = 0;
            // 選び直しの通知は要らない（RefreshList を直接呼ぶ）ので、通知なしで表示だけ戻す
            _columnDropdown.SetValueWithoutNotify(_columnIndex);
            _traitDropdown.SetValueWithoutNotify(_traitIndex);
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

        private void SelectTrait(int index)
        {
            _traitIndex = index;
            OnListOptionChanged();
        }

        private void OnListOptionChanged()
        {
            PlayClick();
            RefreshList();
        }

        /// <summary>マスは作り直さず、並び順（SiblingIndex）と表示/非表示だけ変える。GridLayoutGroup が詰め直してくれる</summary>
        private void RefreshList()
        {
            ZukanStatColumn column = ZukanListOptions.Columns[_columnIndex];
            ZukanTraitFilter trait = ZukanListOptions.Traits[_traitIndex];

            _cells.Sort((a, b) => Compare(a, b, column));
            for (int i = 0; i < _cells.Count; i++)
            {
                ZukanCell cell = _cells[i];
                cell.transform.SetSiblingIndex(i);
                cell.ShowValue(column.Format(cell.Stats));
                cell.gameObject.SetActive(trait.Matches(cell.Stats));
            }

            _orderButton.GetComponentInChildren<Text>().text = _isDescending ? DescendingLabel : AscendingLabel;
            // 一覧の中は特性名だけにし、閉じたときの見出しにだけ「特性: 」を付けて何のボタンか分かるようにする
            _traitDropdown.captionText.text = TraitLabelPrefix + trait.Label;
            _scroll.verticalNormalizedPosition = 1f;
        }

        /// <summary>同じ値どうしは No 順にして、押すたびに並びが入れ替わって見えないようにする</summary>
        private int Compare(ZukanCell a, ZukanCell b, ZukanStatColumn column)
        {
            int byValue = column.Value(a.Stats).CompareTo(column.Value(b.Stats));
            if (_isDescending) byValue = -byValue;
            return byValue != 0 ? byValue : a.Stats.UnitNo.CompareTo(b.Stats.UnitNo);
        }

        private void Close()
        {
            PlayClick();
            Hide();
        }

        private static void PlayClick()
        {
            if (AudioManager.HasInstance) AudioManager.Instance.PlaySe(SeId.ButtonClick);
        }
    }
}
