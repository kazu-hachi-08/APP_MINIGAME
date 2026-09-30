using System.Collections.Generic;

namespace MiniGame.LifeGame
{
    public sealed class LifeCell
    {
        public int Index;
        public LifeCellType Type;
        public LifeSection Section;

        /// <summary>お金のマスの金額（シャッフル時に決まる）。お金が動かないマスは 0</summary>
        public int Amount;

        /// <summary>文面の選び番号。テーマの文面リストの数で割った余りで使う（テーマが決まる前に盤面を作れるようにするため）</summary>
        public int TextVariant;

        /// <summary>次のマス。2つ以上あれば分岐</summary>
        public List<int> Next = new List<int>();

        public bool IsBranch => Next.Count > 1;
    }

    /// <summary>生成済みの盤面。スタートは常に 0 番</summary>
    public sealed class LifeBoard
    {
        public const int StartIndex = 0;

        public readonly List<LifeCell> Cells = new List<LifeCell>();

        public int GoalIndex { get; internal set; }

        public LifeCell this[int index] => Cells[index];

        internal LifeCell Add(LifeCellType type, LifeSection section)
        {
            var cell = new LifeCell { Index = Cells.Count, Type = type, Section = section };
            Cells.Add(cell);
            return cell;
        }

        /// <summary>分岐のどの道が指定の区間へ向かうか。無ければ -1</summary>
        public int BranchChoiceOf(LifeCell branch, LifeSection section)
        {
            for (int i = 0; i < branch.Next.Count; i++)
            {
                if (Cells[branch.Next[i]].Section == section) return i;
            }

            return -1;
        }
    }
}
