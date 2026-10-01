using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MiniGame.LifeGame.Editor
{
    /// <summary>
    /// 盤面の座標（LifeBoardLayout）を、区間ごとの通過点をなめらかにつないだ曲がりくねった道で生成する（仕様書 §3.1・§5.1）。
    /// スタートが下、ゴールが上。マスは区間の道の上に等間隔で並べる。
    /// 既にあるアセットはマス数が合っていれば上書きしない（Inspector で手直しした座標を消さないため）。
    /// </summary>
    public static class LifeBoardLayoutGenerator
    {
        private const string LayoutPath = "Assets/_Project/Games/05_LifeGame/Data/LifeBoardLayout.asset";

        // 曲線を折れ線に近似する細かさ（通過点の間1区切りあたり）。マスの間隔より十分細かければよい
        private const int CurveSteps = 32;

        /// <summary>
        /// 区間ごとの通過点（x は ±3 以内にして縦画面の横幅に収める）。分岐の各ルートは左・中・右に分けて交差させない。
        /// 大学は遠回りなので大きく蛇行、ギャンブルは角ばったジグザグ（Sharp）にしてルートの性格を見た目でも分かるようにする。
        /// 形を変えたら、マス同士が 1.2 以上離れているか Scene で確かめる
        /// </summary>
        private static readonly Dictionary<LifeSection, (Vector2[] Points, bool Sharp)> Paths =
            new Dictionary<LifeSection, (Vector2[] Points, bool Sharp)>
            {
                { LifeSection.Start, (Points((0f, 0f), (0f, 1.4f)), false) },
                { LifeSection.Job, (Points((-1.7f, 2.7f), (-2.8f, 4.4f), (-2.3f, 7.4f), (-2.8f, 10.4f), (-2.2f, 12.8f)), false) },
                { LifeSection.University, (Points((0f, 2.9f), (1.1f, 4.7f), (-1.1f, 7.2f), (1.1f, 9.7f), (-0.9f, 12f), (0f, 13.9f)), false) },
                { LifeSection.Freeter, (Points((1.7f, 2.7f), (2.8f, 4.8f), (2.1f, 7.6f), (2.8f, 10.4f), (2.2f, 12.8f)), false) },
                {
                    LifeSection.Middle, (Points((0f, 15.4f), (2.6f, 16.4f), (2.2f, 18.1f), (-0.6f, 18.7f), (-2.7f, 19.8f),
                        (-2.2f, 21.6f), (0.3f, 22.2f), (2.4f, 23.2f), (2f, 24.7f), (0f, 25.5f)), false)
                },
                { LifeSection.Safe, (Points((-1.3f, 26.7f), (-3f, 28.4f), (-1.1f, 30.6f), (-3f, 32.9f), (-1.6f, 35.4f)), false) },
                {
                    LifeSection.Gamble, (Points((1.3f, 26.7f), (2.9f, 27.7f), (1.3f, 28.7f), (2.9f, 29.7f), (1.3f, 30.7f),
                        (2.9f, 31.7f), (1.3f, 32.7f), (2.9f, 33.7f), (1.3f, 34.7f), (1.3f, 36.1f)), true)
                },
                {
                    LifeSection.Final, (Points((0f, 37.1f), (2.4f, 38.1f), (2.1f, 39.7f), (-0.4f, 40.3f), (-2.6f, 41.3f),
                        (-2.2f, 42.9f), (-0.2f, 43.7f), (1.8f, 44.7f), (1.4f, 46.1f), (0f, 47.1f)), false)
                },
            };

        [MenuItem("Tools/MiniGame/LifeGame/Regenerate Board Layout")]
        public static void Regenerate()
        {
            Save(AssetDatabase.LoadAssetAtPath<LifeBoardLayout>(LayoutPath), Compute(SampleBoard()));
            Debug.Log($"[LifeBoardLayoutGenerator] 盤面レイアウトを作り直しました: {LayoutPath}");
        }

        /// <summary>無いか、マス数が盤面と合わなければ作る（LifeGameSceneBuilder から呼ばれる）</summary>
        public static LifeBoardLayout EnsureLayout()
        {
            LifeBoard board = SampleBoard();
            var layout = AssetDatabase.LoadAssetAtPath<LifeBoardLayout>(LayoutPath);
            if (layout != null && layout.Count == board.Cells.Count) return layout;

            return Save(layout, Compute(board));
        }

        /// <summary>マスの並び（区間・分岐）はシードで変わらないので、どのシードの盤面でも同じ座標になる</summary>
        private static LifeBoard SampleBoard()
        {
            return LifeBoardGenerator.Generate(LifeBoardShape.CreateDefault(), new LifeRuleConfig(), new LifeRandom(0));
        }

        private static LifeBoardLayout Save(LifeBoardLayout layout, Vector2[] positions)
        {
            if (layout == null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(LayoutPath));
                layout = ScriptableObject.CreateInstance<LifeBoardLayout>();
                AssetDatabase.CreateAsset(layout, LayoutPath);
            }

            var so = new SerializedObject(layout);
            SerializedProperty list = so.FindProperty("_positions");
            list.arraySize = positions.Length;
            for (int i = 0; i < positions.Length; i++) list.GetArrayElementAtIndex(i).vector2Value = positions[i];
            so.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.SaveAssets();
            return layout;
        }

        private static Vector2[] Compute(LifeBoard board)
        {
            Dictionary<LifeSection, int> lengths = CountSections(board);
            var spots = new Dictionary<LifeSection, Vector2[]>();
            foreach (KeyValuePair<LifeSection, int> section in lengths)
            {
                (Vector2[] points, bool sharp) = Paths[section.Key];
                spots[section.Key] = PlaceEvenly(Polyline(points, sharp), section.Value);
            }

            var positions = new Vector2[board.Cells.Count];
            var placed = new Dictionary<LifeSection, int>();
            // 盤面は区間ごとに続けて Add されているので、区間内の何番目かでそのまま並べられる
            foreach (LifeCell cell in board.Cells)
            {
                placed.TryGetValue(cell.Section, out int indexInSection);
                positions[cell.Index] = spots[cell.Section][indexInSection];
                placed[cell.Section] = indexInSection + 1;
            }

            return positions;
        }

        /// <summary>通過点を細かい折れ線にする。Sharp なら直線でつなぎ、そうでなければ Catmull-Rom 曲線でなめらかにつなぐ</summary>
        private static List<Vector2> Polyline(Vector2[] points, bool sharp)
        {
            var line = new List<Vector2>();
            for (int i = 0; i < points.Length - 1; i++)
            {
                Vector2 p0 = points[Mathf.Max(i - 1, 0)];
                Vector2 p1 = points[i];
                Vector2 p2 = points[i + 1];
                Vector2 p3 = points[Mathf.Min(i + 2, points.Length - 1)];
                for (int step = 0; step < CurveSteps; step++)
                {
                    float t = step / (float)CurveSteps;
                    line.Add(sharp ? Vector2.Lerp(p1, p2, t) : CatmullRom(p0, p1, p2, p3, t));
                }
            }

            line.Add(points[points.Length - 1]);
            return line;
        }

        private static Vector2 CatmullRom(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
        {
            float t2 = t * t;
            float t3 = t2 * t;
            return 0.5f * (2f * p1 + (p2 - p0) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (3f * p1 - p0 - 3f * p2 + p3) * t3);
        }

        /// <summary>折れ線の長さを count-1 等分した位置にマスを置く（両端にもマスが来る）</summary>
        private static Vector2[] PlaceEvenly(List<Vector2> line, int count)
        {
            var spots = new Vector2[count];
            if (count == 1)
            {
                spots[0] = line[0];
                return spots;
            }

            var distances = new float[line.Count];
            for (int i = 1; i < line.Count; i++) distances[i] = distances[i - 1] + Vector2.Distance(line[i - 1], line[i]);

            float total = distances[line.Count - 1];
            int segment = 0;
            for (int c = 0; c < count; c++)
            {
                float target = total * c / (count - 1);
                while (segment < line.Count - 2 && distances[segment + 1] < target) segment++;

                float length = distances[segment + 1] - distances[segment];
                float t = length <= 0f ? 0f : (target - distances[segment]) / length;
                spots[c] = Vector2.Lerp(line[segment], line[segment + 1], t);
            }

            return spots;
        }

        private static Dictionary<LifeSection, int> CountSections(LifeBoard board)
        {
            var lengths = new Dictionary<LifeSection, int>();
            foreach (LifeCell cell in board.Cells)
            {
                lengths.TryGetValue(cell.Section, out int count);
                lengths[cell.Section] = count + 1;
            }

            return lengths;
        }

        private static Vector2[] Points(params (float X, float Y)[] points)
        {
            var result = new Vector2[points.Length];
            for (int i = 0; i < points.Length; i++) result[i] = new Vector2(points[i].X, points[i].Y);
            return result;
        }
    }
}
