using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.LifeGame
{
    /// <summary>
    /// 生成済みの盤面（LifeBoard）からマスと道を並べる。盤面は試合ごとにシードで変わるので、Scene には置かず実行時に作る。
    /// </summary>
    public class BoardView : MonoBehaviour
    {
        private const int RoadSortingOrder = 0;

        [SerializeField] private float _cellSize = 1f;
        [SerializeField] private float _columnSpacing = 2.4f;
        [SerializeField] private float _rowSpacing = 1.3f;
        [SerializeField] private float _roadWidth = 0.18f;
        [SerializeField] private Color _roadColor = new Color(0.45f, 0.4f, 0.35f);

        [Header("Label")]
        [Tooltip("マスの文字のフォントをこの Text から借りる。ブラウザ版は WebFontApplier が日本語フォントに差し替えた後の Text を使うため")]
        [SerializeField] private Text _fontSource;
        [SerializeField] private int _labelFontSize = 48;
        [Tooltip("4文字（スタートなど）が1マスの幅に収まる大きさ")]
        [SerializeField] private float _labelCharacterSize = 0.042f;

        private Vector2[] _positions;

        /// <summary>盤面全体を囲む範囲（マスの大きさ込み）</summary>
        public Rect Bounds { get; private set; }

        public Vector2 PositionOf(int cellIndex) => (Vector2)transform.position + _positions[cellIndex];

        public void Build(LifeBoard board)
        {
            _positions = BoardPositions.Compute(board, _columnSpacing, _rowSpacing);

            foreach (LifeCell cell in board.Cells)
            {
                foreach (int next in cell.Next) CreateRoad(_positions[cell.Index], _positions[next]);

                CellView.Create(transform, cell, _positions[cell.Index], _cellSize, _fontSource.font,
                    _labelFontSize, _labelCharacterSize);
            }

            Bounds = ComputeBounds();
        }

        /// <summary>2マスの間を細長い四角でつなぐ</summary>
        private void CreateRoad(Vector2 from, Vector2 to)
        {
            var roadObj = new GameObject("Road");
            roadObj.transform.SetParent(transform, false);
            Vector2 delta = to - from;
            roadObj.transform.localPosition = (from + to) * 0.5f;
            roadObj.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            roadObj.transform.localScale = new Vector3(delta.magnitude, _roadWidth, 1f);

            var road = roadObj.AddComponent<SpriteRenderer>();
            road.sprite = LifeShapes.Square;
            road.color = _roadColor;
            road.sortingOrder = RoadSortingOrder;
        }

        private Rect ComputeBounds()
        {
            Vector2 min = _positions[0];
            Vector2 max = _positions[0];
            foreach (Vector2 position in _positions)
            {
                min = Vector2.Min(min, position);
                max = Vector2.Max(max, position);
            }

            Vector2 half = Vector2.one * (_cellSize * 0.5f);
            Vector2 origin = transform.position;
            return Rect.MinMaxRect(origin.x + min.x - half.x, origin.y + min.y - half.y,
                origin.x + max.x + half.x, origin.y + max.y + half.y);
        }
    }
}
