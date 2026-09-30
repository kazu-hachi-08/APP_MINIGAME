using System;

namespace MiniGame.LifeGame
{
    public enum LifeCellType
    {
        // 固定マス
        Start,
        Branch,
        JobOffer,
        Graduation,
        Payday,
        Marriage,
        Tuition,
        Goal,

        // シャッフルマス
        Income,
        Expense,
        Sickness,
        Accident,
        Fire,
        Birth,
        House,
        Insurance,
        Stock,
        ChangeJob,
    }

    /// <summary>盤面の区間。シャッフルは区間ごとに行う</summary>
    public enum LifeSection
    {
        Start,
        Job,
        University,
        Freeter,
        Middle,
        Safe,
        Gamble,
        Final,
    }

    /// <summary>職業の係。該当するマスの支払いを受け取る</summary>
    public enum LifeJobRole
    {
        None,
        Healer,
        Police,
        Repair,
    }

    [Flags]
    public enum LifeInsurance
    {
        None = 0,
        Life = 1,
        Auto = 2,
        Fire = 4,
    }

    /// <summary>ルールが次に待っている操作</summary>
    public enum LifePending
    {
        Spin,
        Branch,
        JobCard,
        ChangeJob,
        House,
        Insurance,
        Stock,
        Finished,
    }

    public static class LifeCellTypes
    {
        /// <summary>通過できず、残りの歩数を捨てて止まるマス</summary>
        public static bool IsStop(LifeCellType type)
        {
            return type == LifeCellType.JobOffer
                || type == LifeCellType.Graduation
                || type == LifeCellType.Marriage
                || type == LifeCellType.Goal;
        }
    }
}
