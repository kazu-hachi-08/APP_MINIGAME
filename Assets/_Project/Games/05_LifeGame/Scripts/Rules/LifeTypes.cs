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

        // 追加マス。アイコン（BoardView._icons）が番号順なので、既存の番号をずらさないよう末尾に足す
        Bet,
        Nominate,
        Present,
        SwapJob,
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

    /// <summary>キャラの能力（仕様書 §9.2）。運のゲームなので差は小さくする</summary>
    public enum LifeAbility
    {
        None,

        /// <summary>初期所持金が増える</summary>
        StartMoney,

        /// <summary>給料が増える</summary>
        Salary,

        /// <summary>移動のルーレットを1試合に1回だけ振り直せる</summary>
        Reroll,

        /// <summary>出費・病気・事故・火事の支払いが減る</summary>
        Thrift,
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

        /// <summary>振り直せる人が出目を見て、振り直すか決める</summary>
        Reroll,
        Branch,
        JobCard,
        ChangeJob,
        House,
        Insurance,
        Stock,

        /// <summary>賭けマスで賭けるか決める</summary>
        Bet,

        /// <summary>指名・入れ替えマスで相手を選ぶ。どちらのマスかは CurrentCell.Type で分かる</summary>
        ChooseTarget,
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
