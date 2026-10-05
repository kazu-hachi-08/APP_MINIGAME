using System;
using System.Collections.Generic;
using MiniGame.Common.Audio;
using MiniGame.PenguinWars.Battle;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.PenguinWars
{
    /// <summary>
    /// ステージモードの編成画面。上に10枠（出撃ボタンと同じ5×2・コストの低い順）、下に解放済みキャラの一覧。
    /// 一覧を押すと枠に入る（もう一度押すと外す）、枠を押すと外す。10体そろうまで「けってい」できない。
    /// 「もどる」は変更を捨てる（途中の9体編成が保存されて、次の出撃で勝手に補完されるのを避けるため）
    /// </summary>
    public class DeckEditPanel : MonoBehaviour
    {
        private const string CountFormat = "{0} / {1}";

        [SerializeField] private PenguinUnitCatalog _catalog;
        [Tooltip("非表示のひな形。ずかんと同じマスをキャラの数だけ複製する")]
        [SerializeField] private ZukanCell _cellTemplate;
        [SerializeField] private ScrollRect _scroll;
        [SerializeField] private Dropdown _columnDropdown;
        [Tooltip("並びが出撃ボタンと同じ（左上から右へ、2段目に続く）")]
        [SerializeField] private Button[] _slotButtons;
        [SerializeField] private Image[] _slotIcons;
        [SerializeField] private Text _countLabel;
        [SerializeField] private Button _decideButton;
        [SerializeField] private Button _backButton;
        [Tooltip("隣のマスと歩くタイミングをずらすコマ数")]
        [SerializeField] private float _walkPhaseStep = 0.37f;

        private readonly List<ZukanCell> _cells = new List<ZukanCell>();
        private readonly List<int> _deck = new List<int>();
        private List<int> _unlockedNos = new List<int>();
        private CampaignProgress _progress;
        private Action _onClosed;
        private int _columnIndex;

        private void Awake()
        {
            _decideButton.onClick.AddListener(Decide);
            _backButton.onClick.AddListener(Close);
            for (int i = 0; i < _slotButtons.Length; i++)
            {
                int slot = i;
                _slotButtons[i].onClick.AddListener(() => RemoveAt(slot));
            }
            SetupColumnDropdown();
        }

        private void SetupColumnDropdown()
        {
            var labels = new List<string>();
            foreach (ZukanStatColumn column in ZukanListOptions.Columns) labels.Add(column.Label);
            _columnDropdown.ClearOptions();
            _columnDropdown.AddOptions(labels);
            _columnDropdown.onValueChanged.AddListener(SelectColumn);
        }

        /// <param name="onClosed">「けってい」でも「もどる」でも呼ぶ（詳細パネルの編成アイコンを描き直してもらう）</param>
        public void Show(CampaignProgress progress, Action onClosed)
        {
            _progress = progress;
            _onClosed = onClosed;
            _unlockedNos = CampaignUnlocks.UnlockedNos(progress);
            _deck.Clear();
            _deck.AddRange(DeckRules.CurrentDeck(progress));

            BuildCellsOnce();
            _columnIndex = 0;
            _columnDropdown.SetValueWithoutNotify(_columnIndex);
            RefreshList();
            Refresh();
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
                cell.Show(unit, index * _walkPhaseStep, data => Toggle(data.No));
                _cells.Add(cell);
                index++;
            }
        }

        private void SelectColumn(int index)
        {
            _columnIndex = index;
            PlayClick();
            RefreshList();
        }

        /// <summary>未解放キャラは一覧に出さない。同じ値どうしは No 順（ずかんと同じ）</summary>
        private void RefreshList()
        {
            ZukanStatColumn column = ZukanListOptions.Columns[_columnIndex];
            _cells.Sort((a, b) =>
            {
                int byValue = column.Value(a.Stats).CompareTo(column.Value(b.Stats));
                return byValue != 0 ? byValue : a.Stats.UnitNo.CompareTo(b.Stats.UnitNo);
            });
            for (int i = 0; i < _cells.Count; i++)
            {
                ZukanCell cell = _cells[i];
                cell.transform.SetSiblingIndex(i);
                cell.ShowValue(column.Format(cell.Stats));
                cell.gameObject.SetActive(_unlockedNos.Contains(cell.Stats.UnitNo));
            }
            _scroll.verticalNormalizedPosition = 1f;
        }

        private void Toggle(int unitNo)
        {
            if (_deck.Contains(unitNo)) _deck.Remove(unitNo);
            else if (_deck.Count < DeckRules.DeckSize) _deck.Add(unitNo);
            else return;

            PlayClick();
            SortDeck();
            Refresh();
        }

        private void RemoveAt(int slot)
        {
            if (slot >= _deck.Count) return;

            PlayClick();
            _deck.RemoveAt(slot);
            Refresh();
        }

        /// <summary>枠は出撃ボタンと同じコストの低い順にして、プレイ中の並びを想像しやすくする</summary>
        private void SortDeck()
        {
            List<int> sorted = DeckRules.ByCost(_deck);
            _deck.Clear();
            _deck.AddRange(sorted);
        }

        private void Refresh()
        {
            for (int i = 0; i < _slotIcons.Length; i++)
            {
                Sprite icon = i < _deck.Count ? FindIcon(_deck[i]) : null;
                _slotIcons[i].sprite = icon;
                _slotIcons[i].enabled = icon != null;
            }
            foreach (ZukanCell cell in _cells) cell.SetDimmed(_deck.Contains(cell.Stats.UnitNo));

            _countLabel.text = string.Format(CountFormat, _deck.Count, DeckRules.DeckSize);
            _decideButton.interactable = DeckRules.IsValid(_deck, _unlockedNos);
        }

        private Sprite FindIcon(int unitNo)
        {
            PenguinUnitData data = _catalog.Get(unitNo);
            // 自分の編成なので、出撃ボタンと同じ左陣営（青）の立ち姿
            return data != null ? data.GetSprites(Side.Left).Icon : null;
        }

        private void Decide()
        {
            if (!DeckRules.IsValid(_deck, _unlockedNos)) return;

            _progress.LastDeckNos = new List<int>(_deck);
            CampaignSave.Save(_progress);
            Close();
        }

        private void Close()
        {
            PlayClick();
            Hide();
            _onClosed?.Invoke();
        }

        private static void PlayClick()
        {
            if (AudioManager.HasInstance) AudioManager.Instance.PlaySe(SeId.ButtonClick);
        }
    }
}
