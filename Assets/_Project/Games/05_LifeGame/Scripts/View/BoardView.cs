using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.LifeGame
{
    /// <summary>
    /// 生成済みの盤面（LifeBoard）からマスと道を並べる。盤面は試合ごとにシードで変わるので、Scene には置かず実行時に作る。
    /// 座標は LifeBoardLayout、色と背景はテーマから引く。建物と木などの飾りは BoardScenery が置く。
    /// </summary>
    public class BoardView : MonoBehaviour
    {
        private const int BackgroundSortingOrder = -10;
        private const int RoadSortingOrder = 0;

        [SerializeField] private LifeBoardLayout _layout;
        [SerializeField] private float _cellSize = 1f;
        [SerializeField] private float _roadWidth = 0.24f;
        [SerializeField] private BoardScenery _scenery;

        [Tooltip("背景を盤面の外側へどれだけ広げるか。全体表示や縦長の端末でも背景の端が見えないようにする")]
        [SerializeField] private float _backgroundMargin = 20f;

        [Header("Art")]
        [SerializeField] private Sprite _cellSprite;
        [Tooltip("上下の縁だけ伸ばさない Sliced の道")]
        [SerializeField] private Sprite _roadSprite;
        [Tooltip("マスのアイコン。LifeCellType の番号順")]
        [SerializeField] private Sprite[] _icons;
        [SerializeField] private float _iconSize = 0.55f;
        [Tooltip("アイコンを上に寄せ、下に金額か名前の1行を置く")]
        [SerializeField] private float _iconY = 0.1f;

        [Header("Label")]
        [Tooltip("マスの文字のフォントをこの Text から借りる。ブラウザ版は WebFontApplier が日本語フォントに差し替えた後の Text を使うため")]
        [SerializeField] private Text _fontSource;
        [SerializeField] private int _labelFontSize = 48;
        [Tooltip("4文字（スタートなど）がアイコンの下に1行で収まる大きさ")]
        [SerializeField] private float _labelCharacterSize = 0.032f;
        [SerializeField] private float _labelY = -0.3f;

        /// <summary>盤面全体を囲む範囲（マスの大きさ込み）</summary>
        public Rect Bounds { get; private set; }

        /// <summary>マスが押された（効果の説明を出すため）</summary>
        public event System.Action<LifeCell> CellTapped;

        public Vector2 PositionOf(int cellIndex) => (Vector2)transform.position + _layout.PositionOf(cellIndex);

        /// <summary>イベント表示でも盤面と同じアイコンを出し、どのマスに止まったか見比べられるようにする</summary>
        /// <remarks>マスの種類を足した直後で Rebuild LifeGame 前のシーンでも落ちないよう、アイコンが無ければ null</remarks>
        public Sprite IconOf(LifeCellType type) => (int)type < _icons.Length ? _icons[(int)type] : null;

        public void Build(LifeBoard board, LifeThemeData theme)
        {
            if (_layout.Count != board.Cells.Count)
            {
                Debug.LogError($"[BoardView] レイアウトのマス数 {_layout.Count} と盤面のマス数 {board.Cells.Count} が違う。Tools > MiniGame > Rebuild LifeGame で作り直す");
                return;
            }

            CellView.Style style = CellStyle();
            foreach (LifeCell cell in board.Cells)
            {
                Vector2 position = _layout.PositionOf(cell.Index);
                foreach (int next in cell.Next) CreateSegment(position, _layout.PositionOf(next), theme.Road);

                CellView.Create(transform, cell, position, theme.CellColor(cell.Type), IconOf(cell.Type), style,
                    tapped => CellTapped?.Invoke(tapped));
            }

            _scenery.Build(board, _layout, theme);
            Bounds = ComputeBounds(board.Cells.Count);
            CreateBackground(theme.BackgroundTile);
        }

        private CellView.Style CellStyle()
        {
            return new CellView.Style
            {
                Size = _cellSize,
                Tile = _cellSprite,
                IconSize = _iconSize,
                IconY = _iconY,
                Font = _fontSource.font,
                FontSize = _labelFontSize,
                CharacterSize = _labelCharacterSize,
                LabelY = _labelY,
            };
        }

        /// <summary>盤面の範囲より広く敷き詰める。絵の無いテーマ（手描きに差し替え途中など）はカメラの背景色のままにする</summary>
        private void CreateBackground(Sprite tile)
        {
            if (tile == null) return;

            var backgroundObj = new GameObject("Background");
            backgroundObj.transform.SetParent(transform, false);
            backgroundObj.transform.position = Bounds.center;

            var background = backgroundObj.AddComponent<SpriteRenderer>();
            background.sprite = tile;
            background.drawMode = SpriteDrawMode.Tiled;
            background.size = Bounds.size + Vector2.one * (_backgroundMargin * 2f);
            background.sortingOrder = BackgroundSortingOrder;
        }

        /// <summary>2点の間を道でつなぐ。角が欠けないよう道幅ぶん長くする。縁の太さを変えないよう scale ではなく size で伸ばす</summary>
        private void CreateSegment(Vector2 from, Vector2 to, Color color)
        {
            var roadObj = new GameObject("Road");
            roadObj.transform.SetParent(transform, false);
            Vector2 delta = to - from;
            roadObj.transform.localPosition = (from + to) * 0.5f;
            roadObj.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);

            var road = roadObj.AddComponent<SpriteRenderer>();
            road.sprite = _roadSprite;
            road.drawMode = SpriteDrawMode.Sliced;
            road.size = new Vector2(delta.magnitude + _roadWidth, _roadWidth);
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
            min -= half;
            max += half;
            // 盤面の端の建物もカメラの範囲に入れ、縦長の端末で見切れないようにする
            foreach ((Vector2 position, Vector2 extent) in _scenery.Landmarks)
            {
                min = Vector2.Min(min, position - extent);
                max = Vector2.Max(max, position + extent);
            }

            Vector2 origin = transform.position;
            return Rect.MinMaxRect(origin.x + min.x, origin.y + min.y, origin.x + max.x, origin.y + max.y);
        }
    }
}
