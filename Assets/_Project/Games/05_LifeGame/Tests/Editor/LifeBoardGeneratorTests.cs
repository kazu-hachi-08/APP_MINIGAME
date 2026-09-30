using System.Collections.Generic;
using NUnit.Framework;

namespace MiniGame.LifeGame.Tests
{
    public class LifeBoardGeneratorTests
    {
        private const int SeedCount = 200;

        private static LifeBoard Generate(int seed)
        {
            return LifeBoardGenerator.Generate(LifeBoardShape.CreateDefault(), new LifeRuleConfig(), new LifeRandom(seed));
        }

        [Test]
        public void 同じシードなら同じ盤面()
        {
            LifeBoard a = Generate(42);
            LifeBoard b = Generate(42);

            Assert.AreEqual(a.Cells.Count, b.Cells.Count);
            for (int i = 0; i < a.Cells.Count; i++)
            {
                Assert.AreEqual(a[i].Type, b[i].Type);
                Assert.AreEqual(a[i].Amount, b[i].Amount);
                Assert.AreEqual(a[i].TextVariant, b[i].TextVariant);
                CollectionAssert.AreEqual(a[i].Next, b[i].Next);
            }
        }

        [Test]
        public void シードが違えば並びが変わる()
        {
            LifeBoard a = Generate(1);
            LifeBoard b = Generate(2);

            bool differs = false;
            for (int i = 0; i < a.Cells.Count; i++) differs |= a[i].Type != b[i].Type;

            Assert.IsTrue(differs);
        }

        [Test]
        public void 同じ種類のマスが3つ以上続かない()
        {
            for (int seed = 0; seed < SeedCount; seed++)
            {
                LifeBoard board = Generate(seed);
                foreach (List<LifeCellType> section in SectionTypes(board).Values)
                {
                    Assert.IsFalse(LifeBoardGenerator.HasLongRun(section), $"seed={seed}");
                }
            }
        }

        [Test]
        public void 固定マスが決まった位置にある()
        {
            LifeBoard board = Generate(7);
            LifeCell firstBranch = board[1];

            LifeCell job = board[firstBranch.Next[board.BranchChoiceOf(firstBranch, LifeSection.Job)]];
            Assert.AreEqual(LifeCellType.JobOffer, job.Type);

            List<LifeCellType> university = SectionTypes(board)[LifeSection.University];
            Assert.AreEqual(LifeCellType.Graduation, university[university.Count - 1]);

            Assert.AreEqual(LifeCellType.Goal, board[board.GoalIndex].Type);
            Assert.AreEqual(5, CountType(board, LifeCellType.Payday));
        }

        [Test]
        public void 一人が通るマス数は約46から52()
        {
            LifeBoard board = Generate(3);
            Dictionary<LifeSection, List<LifeCellType>> sections = SectionTypes(board);
            int common = sections[LifeSection.Middle].Count + sections[LifeSection.Final].Count;

            int shortest = common + sections[LifeSection.Freeter].Count + sections[LifeSection.Safe].Count;
            int longest = common + sections[LifeSection.University].Count + sections[LifeSection.Gamble].Count;

            Assert.GreaterOrEqual(shortest, 46);
            Assert.LessOrEqual(longest, 52);
        }

        [Test]
        public void ギャンブルルートの金額は倍()
        {
            var config = new LifeRuleConfig();
            for (int seed = 0; seed < SeedCount; seed++)
            {
                foreach (LifeCell cell in Generate(seed).Cells)
                {
                    if (cell.Type != LifeCellType.Income && cell.Type != LifeCellType.Expense) continue;

                    int multiplier = cell.Section == LifeSection.Gamble ? config.GambleMultiplier : 1;
                    Assert.GreaterOrEqual(cell.Amount, config.MoneyCellMin * multiplier);
                    Assert.LessOrEqual(cell.Amount, config.MoneyCellMax * multiplier);
                }
            }
        }

        private static Dictionary<LifeSection, List<LifeCellType>> SectionTypes(LifeBoard board)
        {
            var sections = new Dictionary<LifeSection, List<LifeCellType>>();
            foreach (LifeCell cell in board.Cells)
            {
                if (cell.Section == LifeSection.Start || cell.Type == LifeCellType.Goal) continue;
                if (!sections.ContainsKey(cell.Section)) sections[cell.Section] = new List<LifeCellType>();

                sections[cell.Section].Add(cell.Type);
            }

            return sections;
        }

        private static int CountType(LifeBoard board, LifeCellType type)
        {
            int count = 0;
            foreach (LifeCell cell in board.Cells)
            {
                if (cell.Type == type) count++;
            }

            return count;
        }
    }
}
