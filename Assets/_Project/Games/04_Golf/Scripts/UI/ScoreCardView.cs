using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.Golf
{
    /// <summary>
    /// スコアカード（§13.4）。ホール数と人数で表の大きさが変わるので、セルは表示のたびにテンプレートから作る。
    /// 試合の終わりは順位を付けて出す（§6.6）。
    /// </summary>
    public class ScoreCardView : MonoBehaviour
    {
        [SerializeField] private Text _titleText;
        [SerializeField] private GridLayoutGroup _grid;
        [SerializeField] private Text _cellTemplate;
        [SerializeField] private Button _primaryButton;
        [SerializeField] private Text _primaryLabel;
        [SerializeField] private Button _secondaryButton;

        [Tooltip("表の横幅（Canvas 基準の px）。列数で割ってセルの幅にする")]
        [SerializeField] private float _gridWidth = 860f;

        [SerializeField] private float _rowHeight = 90f;

        // 見出し列と合計列ぶん
        private const int ExtraColumns = 2;

        private readonly List<GameObject> _cells = new List<GameObject>();
        private Action _onPrimary;
        private Action _onSecondary;

        private void Awake()
        {
            _cellTemplate.gameObject.SetActive(false);
            _primaryButton.onClick.AddListener(() => Close(_onPrimary));
            _secondaryButton.onClick.AddListener(() => Close(_onSecondary));
        }

        /// <summary>onSecondary が null なら2つ目のボタンは出さない</summary>
        public void Show(string title, IReadOnlyList<GolfHoleData> holes, IReadOnlyList<GolfPlayerSlot> slots,
            bool showRanks, string primaryLabel, Action onPrimary, Action onSecondary)
        {
            _titleText.text = title;
            _primaryLabel.text = primaryLabel;
            _onPrimary = onPrimary;
            _onSecondary = onSecondary;
            _secondaryButton.gameObject.SetActive(onSecondary != null);

            BuildTable(holes, slots, showRanks);
            gameObject.SetActive(true);
        }

        private void BuildTable(IReadOnlyList<GolfHoleData> holes, IReadOnlyList<GolfPlayerSlot> slots, bool showRanks)
        {
            ClearCells();
            int columns = holes.Count + ExtraColumns;
            _grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            _grid.constraintCount = columns;
            _grid.cellSize = new Vector2(_gridWidth / columns, _rowHeight);

            AddHeaderRow(holes.Count);
            AddParRow(holes);

            int[] ranks = showRanks ? GolfRules.Ranks(Totals(slots)) : null;
            for (int i = 0; i < slots.Count; i++)
            {
                AddPlayerRow(holes, slots[i], ranks != null ? $"{ranks[i]}位 " : string.Empty);
            }
        }

        private void AddHeaderRow(int holeCount)
        {
            AddCell(string.Empty);
            for (int h = 0; h < holeCount; h++) AddCell($"H{h + 1}");
            AddCell("合計");
        }

        private void AddParRow(IReadOnlyList<GolfHoleData> holes)
        {
            AddCell("PAR");
            int total = 0;
            foreach (GolfHoleData hole in holes)
            {
                AddCell(hole.Par.ToString());
                total += hole.Par;
            }

            AddCell(total.ToString());
        }

        /// <summary>まだ終わっていないホールは空欄。パーとの差は終わったホールのパーだけで比べる</summary>
        private void AddPlayerRow(IReadOnlyList<GolfHoleData> holes, GolfPlayerSlot slot, string rankPrefix)
        {
            // 見出し列は幅が狭いので「P1」とキャラ名を2行に分ける
            AddCell(rankPrefix + GolfPlayerColors.Colored(slot.Seat, GolfPlayerColors.FullName(slot, "\n")));

            int playedPar = 0;
            for (int h = 0; h < holes.Count; h++)
            {
                bool played = h < slot.HoleScores.Count;
                AddCell(played ? slot.HoleScores[h].ToString() : string.Empty);
                if (played) playedPar += holes[h].Par;
            }

            AddCell($"{slot.Total} ({GolfRules.FormatToPar(slot.Total - playedPar)})");
        }

        private void AddCell(string content)
        {
            Text cell = Instantiate(_cellTemplate, _grid.transform);
            cell.text = content;
            cell.gameObject.SetActive(true);
            _cells.Add(cell.gameObject);
        }

        private void ClearCells()
        {
            foreach (GameObject cell in _cells) Destroy(cell);
            _cells.Clear();
        }

        private void Close(Action callback)
        {
            gameObject.SetActive(false);
            callback?.Invoke();
        }

        private static int[] Totals(IReadOnlyList<GolfPlayerSlot> slots)
        {
            var totals = new int[slots.Count];
            for (int i = 0; i < slots.Count; i++) totals[i] = slots[i].Total;
            return totals;
        }
    }
}
