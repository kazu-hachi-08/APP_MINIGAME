using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MiniGame.LifeGame.Editor
{
    /// <summary>
    /// 盤面の座標（LifeBoardLayout）をつづら折りで生成する（仕様書 §3.1・§5.1）。スタートが下、ゴールが上。
    /// 共通の区間は横5マスで折り返し、分岐の各ルートは5マスの幅を分け合って横に並べる。
    /// 既にあるアセットはマス数が合っていれば上書きしない（Inspector で手直しした座標を消さないため）。
    /// </summary>
    public static class LifeBoardLayoutGenerator
    {
        private const string LayoutPath = "Assets/_Project/Games/05_LifeGame/Data/LifeBoardLayout.asset";

        private const float Spacing = 1.3f;

        // 5列なら縦画面の横幅に収まる（BoardCamera が横に揺れない）
        private const int Columns = 5;
        private const int RouteGapColumns = 1;

        // 区間の間に1行空け、区間をまたぐ道の曲がり角をそこに通す（BoardView の鉤形の道）
        private const int SectionGapRows = 2;

        // 下から順に積む区間の段。同じ段の区間は分岐の各ルートで、横に並ぶ
        private static readonly LifeSection[][] Rows =
        {
            new[] { LifeSection.Start },
            new[] { LifeSection.Job, LifeSection.University, LifeSection.Freeter },
            new[] { LifeSection.Middle },
            new[] { LifeSection.Safe, LifeSection.Gamble },
            new[] { LifeSection.Final },
        };

        private struct Lane
        {
            public float Left;
            public float Bottom;
            public int Columns;
        }

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
            Dictionary<LifeSection, Lane> lanes = ComputeLanes(lengths);

            var positions = new Vector2[board.Cells.Count];
            var placed = new Dictionary<LifeSection, int>();
            // 盤面は区間ごとに続けて Add されているので、区間内の何番目かでそのまま並べられる
            foreach (LifeCell cell in board.Cells)
            {
                placed.TryGetValue(cell.Section, out int indexInSection);
                positions[cell.Index] = Snake(lanes[cell.Section], indexInSection);
                placed[cell.Section] = indexInSection + 1;
            }

            return positions;
        }

        /// <summary>左から右へ並べ、端で1段上がって右から左へ戻る（つづら折り）</summary>
        private static Vector2 Snake(Lane lane, int index)
        {
            int row = index / lane.Columns;
            int column = index % lane.Columns;
            if (row % 2 == 1) column = lane.Columns - 1 - column;

            return new Vector2(lane.Left + column * Spacing, lane.Bottom + row * Spacing);
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

        private static Dictionary<LifeSection, Lane> ComputeLanes(Dictionary<LifeSection, int> lengths)
        {
            var lanes = new Dictionary<LifeSection, Lane>();
            float bottom = 0f;
            foreach (LifeSection[] row in Rows)
            {
                int laneColumns = LaneColumns(row);
                int tallest = 0;
                for (int i = 0; i < row.Length; i++)
                {
                    lengths.TryGetValue(row[i], out int length);
                    int columns = Mathf.Min(laneColumns, length);
                    lanes[row[i]] = new Lane { Left = LaneLeft(row.Length, laneColumns, i, columns), Bottom = bottom, Columns = columns };
                    tallest = Mathf.Max(tallest, Mathf.CeilToInt(length / (float)columns));
                }

                bottom += (tallest - 1 + SectionGapRows) * Spacing;
            }

            return lanes;
        }

        /// <summary>1段の区間で5列を分け合う（3ルートなら1列ずつ、2ルートなら2列ずつ、1区間なら5列）</summary>
        private static int LaneColumns(LifeSection[] row)
        {
            if (row.Length == 1) return row[0] == LifeSection.Start ? 1 : Columns;

            return (Columns - RouteGapColumns * (row.Length - 1)) / row.Length;
        }

        /// <summary>レーンの左端の x。段全体を x=0 に中央寄せする</summary>
        private static float LaneLeft(int laneCount, int laneColumns, int laneIndex, int columns)
        {
            int rowWidth = laneColumns * laneCount + RouteGapColumns * (laneCount - 1);
            float firstColumn = -(rowWidth - 1) * 0.5f;
            // マスが列数より少ない区間（スタート）はレーンの中で中央に寄せる
            float inset = (laneColumns - columns) * 0.5f;
            return (firstColumn + laneIndex * (laneColumns + RouteGapColumns) + inset) * Spacing;
        }
    }
}
