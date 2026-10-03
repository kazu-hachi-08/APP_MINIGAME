using System.Collections.Generic;

namespace MiniGame.LifeGame
{
    /// <summary>
    /// 1区間ぶんの形：長さ・固定マスの位置・シャッフル枠に配るマスの表（LifeSectionDeck）。
    /// </summary>
    public sealed class LifeSectionShape
    {
        public LifeSection Section;
        public int Length;

        // ギャンブルルートは金額を倍にする（仕様書 §5.3）
        public bool DoubleAmounts;

        /// <summary>区間内の位置 → 固定マス</summary>
        public Dictionary<int, LifeCellType> FixedCells = new Dictionary<int, LifeCellType>();

        /// <summary>シャッフル枠に配るマスの種類と数。合計は「長さ − 固定マス数」と一致させる</summary>
        public Dictionary<LifeCellType, int> Deck = new Dictionary<LifeCellType, int>();

        public LifeSectionShape(LifeSection section, int length)
        {
            Section = section;
            Length = length;
        }

        public LifeSectionShape Fixed(int position, LifeCellType type)
        {
            FixedCells[position] = type;
            return this;
        }

        public LifeSectionShape Cards(LifeCellType type, int count)
        {
            Deck[type] = count;
            return this;
        }
    }

    /// <summary>
    /// 道の形（仕様書 §5.1）。3テーマ共通。
    /// 座標だけ LifeBoardLayout（ScriptableObject）に置き、形はコードに置く（ルールは Unity に依存しない asmdef で、テストもこれを使うため）。
    /// </summary>
    public sealed class LifeBoardShape
    {
        /// <summary>分岐①の行き先（就職・大学・フリーターの順）</summary>
        public LifeSectionShape[] FirstRoutes;

        /// <summary>区間B。最後のマスが分岐②</summary>
        public LifeSectionShape Middle;

        /// <summary>分岐②の行き先（安全・ギャンブルの順）</summary>
        public LifeSectionShape[] SecondRoutes;

        /// <summary>区間C。この後にゴールが付く</summary>
        public LifeSectionShape Final;

        public static LifeBoardShape CreateDefault()
        {
            return new LifeBoardShape
            {
                FirstRoutes = new[] { JobRoute(), UniversityRoute(), FreeterRoute() },
                Middle = MiddleSection(),
                SecondRoutes = new[] { SafeRoute(), GambleRoute() },
                Final = FinalSection(),
            };
        }

        private static LifeSectionShape JobRoute()
        {
            return new LifeSectionShape(LifeSection.Job, 8)
                .Fixed(0, LifeCellType.JobOffer)
                .Cards(LifeCellType.Income, 2)
                .Cards(LifeCellType.Expense, 1)
                .Cards(LifeCellType.Accident, 1)
                .Cards(LifeCellType.Insurance, 1)
                .Cards(LifeCellType.Stock, 1)
                .Cards(LifeCellType.Nominate, 1);
        }

        private static LifeSectionShape UniversityRoute()
        {
            return new LifeSectionShape(LifeSection.University, 12)
                .Fixed(2, LifeCellType.Tuition)
                .Fixed(7, LifeCellType.Tuition)
                .Fixed(11, LifeCellType.Graduation)
                .Cards(LifeCellType.Income, 1)
                .Cards(LifeCellType.Expense, 1)
                .Cards(LifeCellType.Sickness, 1)
                .Cards(LifeCellType.Accident, 1)
                .Cards(LifeCellType.Insurance, 1)
                .Cards(LifeCellType.Stock, 1)
                .Cards(LifeCellType.Birth, 1)
                .Cards(LifeCellType.Lottery, 1)
                .Cards(LifeCellType.Forward, 1);
        }

        private static LifeSectionShape FreeterRoute()
        {
            // 給料日は、給料の低いフリーターが他のルートに勝てる見込みを残すため（無いと勝率が約11%まで落ちた）
            return new LifeSectionShape(LifeSection.Freeter, 6)
                .Fixed(3, LifeCellType.Payday)
                .Cards(LifeCellType.Income, 1)
                .Cards(LifeCellType.Accident, 1)
                .Cards(LifeCellType.ChangeJob, 2)
                .Cards(LifeCellType.Bet, 1);
        }

        private static LifeSectionShape MiddleSection()
        {
            return new LifeSectionShape(LifeSection.Middle, 16)
                .Fixed(5, LifeCellType.Payday)
                .Fixed(10, LifeCellType.Marriage)
                .Fixed(15, LifeCellType.Branch)
                .Cards(LifeCellType.Income, 2)
                .Cards(LifeCellType.Expense, 1)
                .Cards(LifeCellType.House, 2)
                .Cards(LifeCellType.Insurance, 1)
                .Cards(LifeCellType.Stock, 1)
                .Cards(LifeCellType.Birth, 1)
                .Cards(LifeCellType.Sickness, 1)
                .Cards(LifeCellType.Fire, 1)
                .Cards(LifeCellType.ChangeJob, 1)
                .Cards(LifeCellType.Nominate, 1)
                .Cards(LifeCellType.SwapJob, 1);
        }

        private static LifeSectionShape SafeRoute()
        {
            return new LifeSectionShape(LifeSection.Safe, 10)
                .Fixed(5, LifeCellType.Payday)
                .Cards(LifeCellType.Income, 2)
                .Cards(LifeCellType.Expense, 1)
                .Cards(LifeCellType.House, 1)
                .Cards(LifeCellType.Birth, 1)
                .Cards(LifeCellType.Stock, 1)
                .Cards(LifeCellType.Sickness, 1)
                .Cards(LifeCellType.Present, 1)
                .Cards(LifeCellType.Forward, 1);
        }

        private static LifeSectionShape GambleRoute()
        {
            var shape = new LifeSectionShape(LifeSection.Gamble, 10)
                .Fixed(5, LifeCellType.Payday)
                .Cards(LifeCellType.Income, 2)
                .Cards(LifeCellType.Expense, 1)
                .Cards(LifeCellType.Sickness, 1)
                .Cards(LifeCellType.Accident, 1)
                .Cards(LifeCellType.Fire, 1)
                .Cards(LifeCellType.Stock, 1)
                .Cards(LifeCellType.Bet, 1)
                .Cards(LifeCellType.Back, 1);
            shape.DoubleAmounts = true;
            return shape;
        }

        private static LifeSectionShape FinalSection()
        {
            return new LifeSectionShape(LifeSection.Final, 14)
                .Fixed(3, LifeCellType.Payday)
                .Fixed(10, LifeCellType.Payday)
                .Cards(LifeCellType.Income, 2)
                .Cards(LifeCellType.Expense, 1)
                .Cards(LifeCellType.Birth, 2)
                .Cards(LifeCellType.House, 1)
                .Cards(LifeCellType.Insurance, 1)
                .Cards(LifeCellType.Fire, 1)
                .Cards(LifeCellType.Accident, 1)
                .Cards(LifeCellType.ChangeJob, 1)
                .Cards(LifeCellType.Lottery, 1)
                .Cards(LifeCellType.Rest, 1);
        }
    }
}
