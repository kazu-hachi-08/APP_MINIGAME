using System.Collections.Generic;
using UnityEngine;

namespace MiniGame.LifeGame
{
    /// <summary>
    /// マスの座標を区間ごとの列に並べて決める仮レイアウト。スタートが下、ゴールが上。
    /// 分岐の各ルートは横に並べた列にし、共通の区間は中央の列にする。
    /// フェーズ4で LifeBoardLayout（ScriptableObject）のつづら折りの座標に置き換える。
    /// </summary>
    public static class BoardPositions
    {
        // 下から順に積む区間の段。同じ段の区間は横に並ぶ
        private static readonly LifeSection[][] Rows =
        {
            new[] { LifeSection.Start },
            new[] { LifeSection.Job, LifeSection.University, LifeSection.Freeter },
            new[] { LifeSection.Middle },
            new[] { LifeSection.Safe, LifeSection.Gamble },
            new[] { LifeSection.Final },
        };

        public static Vector2[] Compute(LifeBoard board, float columnSpacing, float rowSpacing)
        {
            Dictionary<LifeSection, int> lengths = CountSections(board);
            Dictionary<LifeSection, Vector2> origins = ComputeOrigins(lengths, columnSpacing, rowSpacing);

            var positions = new Vector2[board.Cells.Count];
            var placed = new Dictionary<LifeSection, int>();
            // 盤面は区間ごとに続けて Add されているので、区間内の何番目かでそのまま上へ積める
            foreach (LifeCell cell in board.Cells)
            {
                placed.TryGetValue(cell.Section, out int indexInSection);
                positions[cell.Index] = origins[cell.Section] + new Vector2(0f, indexInSection * rowSpacing);
                placed[cell.Section] = indexInSection + 1;
            }

            return positions;
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

        private static Dictionary<LifeSection, Vector2> ComputeOrigins(Dictionary<LifeSection, int> lengths,
            float columnSpacing, float rowSpacing)
        {
            var origins = new Dictionary<LifeSection, Vector2>();
            float y = 0f;
            foreach (LifeSection[] row in Rows)
            {
                int tallest = 0;
                for (int i = 0; i < row.Length; i++)
                {
                    // 列を中央に寄せる（3列なら -1, 0, +1、2列なら -0.5, +0.5）
                    float x = (i - (row.Length - 1) * 0.5f) * columnSpacing;
                    origins[row[i]] = new Vector2(x, y);
                    lengths.TryGetValue(row[i], out int length);
                    tallest = Mathf.Max(tallest, length);
                }

                y += tallest * rowSpacing;
            }

            return origins;
        }
    }
}
