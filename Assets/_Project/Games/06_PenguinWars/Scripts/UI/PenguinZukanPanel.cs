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
        [Tooltip("押すたびに 左上の数値（＝並べ替えの基準）を切り替える")]
        [SerializeField] private Button _columnButton;
        [SerializeField] private Button _orderButton;
        [Tooltip("押すたびに しぼりこむ特性を切り替える")]
        [SerializeField] private Button _traitButton;
        [Tooltip("隣のマスと歩くタイミングをずらすコマ数")]
        [SerializeField] private float _walkPhaseStep = 0.37f;

        private readonly List<ZukanCell> _cells = new List<ZukanCell>();
        private int _columnIndex;
        private bool _isDescending;
        private int _traitIndex;

        private void Awake()
        {
            _closeButton.onClick.AddListener(Close);
            _columnButton.onClick.AddListener(NextColumn);
            _orderButton.onClick.AddListener(ToggleOrder);
            _traitButton.onClick.AddListener(NextTrait);
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
            RefreshList();
        }

        private void NextColumn()
        {
            _columnIndex = (_columnIndex + 1) % ZukanListOptions.Columns.Length;
            OnListOptionChanged();
        }

        private void ToggleOrder()
        {
            _isDescending = !_isDescending;
            OnListOptionChanged();
        }

        private void NextTrait()
        {
            _traitIndex = (_traitIndex + 1) % ZukanListOptions.Traits.Length;
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

            SetButtonLabel(_columnButton, column.Label);
            SetButtonLabel(_orderButton, _isDescending ? DescendingLabel : AscendingLabel);
            SetButtonLabel(_traitButton, TraitLabelPrefix + trait.Label);
            _scroll.verticalNormalizedPosition = 1f;
        }

        /// <summary>同じ値どうしは No 順にして、押すたびに並びが入れ替わって見えないようにする</summary>
        private int Compare(ZukanCell a, ZukanCell b, ZukanStatColumn column)
        {
            int byValue = column.Value(a.Stats).CompareTo(column.Value(b.Stats));
            if (_isDescending) byValue = -byValue;
            return byValue != 0 ? byValue : a.Stats.UnitNo.CompareTo(b.Stats.UnitNo);
        }

        private static void SetButtonLabel(Button button, string label)
        {
            button.GetComponentInChildren<Text>().text = label;
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
