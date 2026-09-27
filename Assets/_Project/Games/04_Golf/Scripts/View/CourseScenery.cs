using UnityEngine;

namespace MiniGame.Golf
{
    /// <summary>
    /// コース外の飾り（木）を置く（Phase 10）。当たり判定はなく見た目だけ。
    /// ホールのプレハブに直接置かず、読み込むたびに地面の外側へ並べることで、ホールを追加しても手作業が要らない。
    /// </summary>
    public class CourseScenery : MonoBehaviour
    {
        [SerializeField] private HoleLoader _holeLoader;

        [Tooltip("コースの外側に木を並べる幅（ユニット）。「全体」表示で見える範囲まで埋める")]
        [SerializeField] private float _margin = 8f;

        [Tooltip("木を置く格子の間隔（ユニット）")]
        [SerializeField] private float _spacing = 1.6f;

        [Tooltip("格子からずらす最大量（ユニット）。並びが整いすぎないようにする")]
        [SerializeField] private float _jitter = 0.6f;

        [Range(0f, 1f)]
        [Tooltip("格子の点に木を置く割合")]
        [SerializeField] private float _density = 0.75f;

        [SerializeField] private float _minSize = 1.3f;
        [SerializeField] private float _maxSize = 2.2f;

        [Tooltip("地面（0）より上、カップ・ボールより下に描く。コース外に出たボールが木に隠れないようにする")]
        [SerializeField] private int _sortingOrder = 2;

        private void OnEnable() => _holeLoader.HoleLoaded += Build;

        private void OnDisable() => _holeLoader.HoleLoaded -= Build;

        private void Build()
        {
            HoleCourse course = _holeLoader.CurrentCourse;
            Bounds courseBounds = course.TerrainBounds;
            // 木の葉がコースの縁にかぶらないよう、木の大きさの半分だけ離す
            courseBounds.Expand(_maxSize);

            // ホールごとに同じ並びにする（何度遊んでも同じ景色）
            var random = new System.Random(_holeLoader.CurrentHole.name.GetHashCode());
            Bounds area = course.TerrainBounds;
            area.Expand(_margin * 2f);

            for (float y = area.min.y; y <= area.max.y; y += _spacing)
            {
                for (float x = area.min.x; x <= area.max.x; x += _spacing)
                {
                    if (random.NextDouble() > _density) continue;

                    var position = new Vector2(x + Range(random, -_jitter, _jitter), y + Range(random, -_jitter, _jitter));
                    if (courseBounds.Contains(new Vector3(position.x, position.y, courseBounds.center.z))) continue;

                    CreateTree(course.transform, position, Range(random, _minSize, _maxSize));
                }
            }
        }

        /// <summary>ホールのプレハブの子にして、次のホールを読み込むときに一緒に消えるようにする</summary>
        private void CreateTree(Transform parent, Vector2 position, float size)
        {
            var tree = new GameObject("Tree").AddComponent<SpriteRenderer>();
            tree.transform.SetParent(parent, false);
            tree.transform.position = position;
            tree.transform.localScale = Vector3.one * size;
            tree.sprite = GolfShapeSprites.Tree;
            tree.sortingOrder = _sortingOrder;
        }

        private static float Range(System.Random random, float min, float max)
        {
            return min + (float)random.NextDouble() * (max - min);
        }
    }
}
