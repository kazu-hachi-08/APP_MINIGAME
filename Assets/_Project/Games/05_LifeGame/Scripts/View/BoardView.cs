using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.LifeGame
{
    /// <summary>
    /// 生成済みの盤面（LifeBoard）からマスと道を並べる。盤面は試合ごとにシードで変わるので、Scene には置かず実行時に作る。
    /// 座標は LifeBoardLayout、色はテーマから引く。
    /// </summary>
    public class BoardView : MonoBehaviour
    {
        private const int RoadSortingOrder = 0;

        [SerializeField] private LifeBoardLayout _layout;
        [SerializeField] private float _cellSize = 1f;
        [SerializeField] private float _roadWidth = 0.18f;
        [Tooltip("区間をまたぐ道の曲がり角を、行き先のマスのどれだけ手前に置くか。区間の間の空き行に曲がり角を置き、道が他のルートのマスの下を通らないようにするため")]
        [SerializeField] private float _cornerOffset = 0.7f;

        [Header("Label")]
        [Tooltip("マスの文字のフォントをこの Text から借りる。ブラウザ版は WebFontApplier が日本語フォントに差し替えた後の Text を使うため")]
        [SerializeField] private Text _fontSource;
        [SerializeField] private int _labelFontSize = 48;
        [Tooltip("4文字（スタートなど）が1マスの幅に収まる大きさ")]
        [SerializeField] private float _labelCharacterSize = 0.042f;

        /// <summary>盤面全体を囲む範囲（マスの大きさ込み）</summary>
        public Rect Bounds { get; private set; }

        public Vector2 PositionOf(int cellIndex) => (Vector2)transform.position + _layout.PositionOf(cellIndex);

        public void Build(LifeBoard board, LifeThemeData theme)
        {
            if (_layout.Count != board.Cells.Count)
            {
                Debug.LogError($"[BoardView] レイアウトのマス数 {_layout.Count} と盤面のマス数 {board.Cells.Count} が違う。Tools > MiniGame > LifeGame > Regenerate Board Layout で作り直す");
                return;
            }

            foreach (LifeCell cell in board.Cells)
            {
                Vector2 position = _layout.PositionOf(cell.Index);
                foreach (int next in cell.Next) CreateRoad(position, _layout.PositionOf(next), theme.Road);

                CellView.Create(transform, cell, position, _cellSize, theme.CellColor(cell.Type), _fontSource.font,
                    _labelFontSize, _labelCharacterSize);
            }

            Bounds = ComputeBounds(board.Cells.Count);
        }

        /// <summary>縦か横に並ぶマスはまっすぐ、区間をまたぐ（斜めになる）ところは「上 → 横 → 上」の鉤形でつなぐ</summary>
        private void CreateRoad(Vector2 from, Vector2 to, Color color)
        {
            if (Mathf.Approximately(from.x, to.x) || Mathf.Approximately(from.y, to.y))
            {
                CreateSegment(from, to, color);
                return;
            }

            float cornerY = to.y - _cornerOffset;
            var corner1 = new Vector2(from.x, cornerY);
            var corner2 = new Vector2(to.x, cornerY);
            CreateSegment(from, corner1, color);
            CreateSegment(corner1, corner2, color);
            CreateSegment(corner2, to, color);
        }

        /// <summary>2点の間を細長い四角でつなぐ。角が欠けないよう道幅ぶん長くする</summary>
        private void CreateSegment(Vector2 from, Vector2 to, Color color)
        {
            var roadObj = new GameObject("Road");
            roadObj.transform.SetParent(transform, false);
            Vector2 delta = to - from;
            roadObj.transform.localPosition = (from + to) * 0.5f;
            roadObj.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            roadObj.transform.localScale = new Vector3(delta.magnitude + _roadWidth, _roadWidth, 1f);

            var road = roadObj.AddComponent<SpriteRenderer>();
            road.sprite = LifeShapes.Square;
            road.color = color;
            road.sortingOrder = RoadSortingOrder;
        }

        private Rect ComputeBounds(int cellCount)
        {
            Vector2 min = _layout.PositionOf(0);
            Vector2 max = min;
            for (int i = 1; i < cellCount; i++)
            {
                min = Vector2.Min(min, _layout.PositionOf(i));
                max = Vector2.Max(max, _layout.PositionOf(i));
            }

            Vector2 half = Vector2.one * (_cellSize * 0.5f);
            Vector2 origin = transform.position;
            return Rect.MinMaxRect(origin.x + min.x - half.x, origin.y + min.y - half.y,
                origin.x + max.x + half.x, origin.y + max.y + half.y);
        }
    }
}
