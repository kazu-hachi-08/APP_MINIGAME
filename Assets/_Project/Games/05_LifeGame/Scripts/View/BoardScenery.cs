using System.Collections.Generic;
using UnityEngine;

namespace MiniGame.LifeGame
{
    /// <summary>
    /// 盤面の飾り：節目のマス（スタート・就職・卒業・結婚・ゴール）の横に建てる建物と、道の外に散らす木や岩。
    /// 道とマスだけだと「線の上を進むだけ」に見えるので、人生の場面が変わっていく感じを出すために置く。
    /// 置き場所はマスと道の座標から自動で決め、レイアウトを作り直しても手で直さなくて済むようにする。
    /// </summary>
    public class BoardScenery : MonoBehaviour
    {
        // 道とマスより奥に描き、少し重なっても道が隠れないようにする
        private const int DecorationSortingOrder = -6;
        private const int LandmarkSortingOrder = -5;

        // 飾りは試合の乱数（LifeGameState.Random）を使わず固定のシードで置く。出目や盤面の並びをずらさないため
        private const int DecorationSeed = 5;

        [Tooltip("建物をマスの中心からどれだけ離して建てるか")]
        [SerializeField] private float _landmarkDistance = 1.6f;
        [Tooltip("建物の置き場所を何方向から選ぶか。道やマスから一番離れた方向に建てる")]
        [SerializeField] private int _landmarkDirections = 8;

        [Header("Decoration")]
        [Tooltip("飾りを置く候補の間隔")]
        [SerializeField] private float _decorationStep = 1.1f;
        [Range(0f, 1f)]
        [SerializeField] private float _decorationDensity = 0.35f;
        [Tooltip("マスと道からこれ以上離れた所にだけ置く（マスの文字や道を隠さないため）")]
        [SerializeField] private float _decorationClearance = 0.95f;
        [SerializeField] private float _landmarkClearance = 1.3f;
        [Tooltip("盤面の外側にも置く範囲。横長の画面（PC）や全体表示で背景だけの所が見えないようにする")]
        [SerializeField] private Vector2 _decorationMargin = new Vector2(8f, 3f);

        private readonly List<Vector2> _cells = new List<Vector2>();
        private readonly List<(Vector2 From, Vector2 To)> _roads = new List<(Vector2 From, Vector2 To)>();
        private readonly List<(Vector2 Position, Vector2 HalfSize)> _landmarks = new List<(Vector2 Position, Vector2 HalfSize)>();

        /// <summary>建てた建物の位置と大きさの半分（盤面ローカル座標）。カメラの範囲に含めるため</summary>
        public IReadOnlyList<(Vector2 Position, Vector2 HalfSize)> Landmarks => _landmarks;

        /// <summary>盤面の座標と道を覚えてから建物、飾りの順に置く（飾りは建物も避ける）</summary>
        public void Build(LifeBoard board, LifeBoardLayout layout, LifeThemeData theme)
        {
            CollectObstacles(board, layout);

            foreach (LifeCell cell in board.Cells)
            {
                Sprite sprite = theme.Landmark(cell.Type);
                if (sprite != null) CreateLandmark(sprite, layout.PositionOf(cell.Index));
            }

            if (theme.Decorations != null && theme.Decorations.Length > 0) CreateDecorations(theme.Decorations);
        }

        private void CollectObstacles(LifeBoard board, LifeBoardLayout layout)
        {
            _cells.Clear();
            _roads.Clear();
            _landmarks.Clear();
            foreach (LifeCell cell in board.Cells)
            {
                Vector2 position = layout.PositionOf(cell.Index);
                _cells.Add(position);
                foreach (int next in cell.Next) _roads.Add((position, layout.PositionOf(next)));
            }
        }

        private void CreateLandmark(Sprite sprite, Vector2 cellPosition)
        {
            Vector2 position = BestSpotAround(cellPosition);
            CreateSprite("Landmark", sprite, position, LandmarkSortingOrder, false);
            _landmarks.Add((position, sprite.bounds.extents));
        }

        /// <summary>マスの周りの候補から、マス・道・他の建物から一番離れた所を選ぶ</summary>
        private Vector2 BestSpotAround(Vector2 center)
        {
            Vector2 best = center + Vector2.right * _landmarkDistance;
            float bestClearance = float.MinValue;
            for (int i = 0; i < _landmarkDirections; i++)
            {
                float angle = i * Mathf.PI * 2f / _landmarkDirections;
                Vector2 candidate = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * _landmarkDistance;
                float clearance = Mathf.Min(DistanceToBoard(candidate), DistanceToLandmarks(candidate));
                if (clearance <= bestClearance) continue;

                best = candidate;
                bestClearance = clearance;
            }

            return best;
        }

        private void CreateDecorations(Sprite[] sprites)
        {
            Rect area = AreaOfBoard();
            var random = new System.Random(DecorationSeed);
            float jitter = _decorationStep * 0.35f;
            for (float y = area.yMin; y <= area.yMax; y += _decorationStep)
            {
                for (float x = area.xMin; x <= area.xMax; x += _decorationStep)
                {
                    // 置くかどうかに関係なく同じ回数だけ乱数を引き、どこを直しても他の飾りの位置が変わらないようにする
                    bool place = random.NextDouble() < _decorationDensity;
                    var position = new Vector2(x + Jitter(random, jitter), y + Jitter(random, jitter));
                    Sprite sprite = sprites[random.Next(sprites.Length)];
                    bool flip = random.Next(2) == 0;
                    if (!place || !IsOpen(position)) continue;

                    CreateSprite("Decoration", sprite, position, DecorationSortingOrder, flip);
                }
            }
        }

        private bool IsOpen(Vector2 position)
        {
            return DistanceToBoard(position) >= _decorationClearance && DistanceToLandmarks(position) >= _landmarkClearance;
        }

        private Rect AreaOfBoard()
        {
            Vector2 min = _cells[0];
            Vector2 max = _cells[0];
            foreach (Vector2 cell in _cells)
            {
                min = Vector2.Min(min, cell);
                max = Vector2.Max(max, cell);
            }

            return Rect.MinMaxRect(min.x - _decorationMargin.x, min.y - _decorationMargin.y,
                max.x + _decorationMargin.x, max.y + _decorationMargin.y);
        }

        private static float Jitter(System.Random random, float amount)
        {
            return ((float)random.NextDouble() * 2f - 1f) * amount;
        }

        private float DistanceToBoard(Vector2 point)
        {
            float nearest = float.MaxValue;
            foreach (Vector2 cell in _cells) nearest = Mathf.Min(nearest, Vector2.Distance(point, cell));
            foreach ((Vector2 from, Vector2 to) in _roads) nearest = Mathf.Min(nearest, DistanceToSegment(point, from, to));
            return nearest;
        }

        private float DistanceToLandmarks(Vector2 point)
        {
            float nearest = float.MaxValue;
            foreach (var landmark in _landmarks) nearest = Mathf.Min(nearest, Vector2.Distance(point, landmark.Position));
            return nearest;
        }

        private static float DistanceToSegment(Vector2 point, Vector2 from, Vector2 to)
        {
            Vector2 delta = to - from;
            float lengthSq = delta.sqrMagnitude;
            float t = lengthSq <= 0f ? 0f : Mathf.Clamp01(Vector2.Dot(point - from, delta) / lengthSq);
            return Vector2.Distance(point, from + delta * t);
        }

        private void CreateSprite(string objectName, Sprite sprite, Vector2 position, int sortingOrder, bool flip)
        {
            var obj = new GameObject(objectName);
            obj.transform.SetParent(transform, false);
            obj.transform.localPosition = position;

            var renderer = obj.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.flipX = flip;
            renderer.sortingOrder = sortingOrder;
        }
    }
}
