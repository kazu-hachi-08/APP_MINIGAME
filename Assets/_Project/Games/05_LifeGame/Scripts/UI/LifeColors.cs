using UnityEngine;

namespace MiniGame.LifeGame
{
    /// <summary>席の色とマスの色。フェーズ4でマスの色はテーマ（LifeThemeData）へ移す</summary>
    public static class LifeColors
    {
        private static readonly Color[] SeatColors =
        {
            new Color(0.95f, 0.3f, 0.3f),
            new Color(0.3f, 0.55f, 1f),
            new Color(0.3f, 0.8f, 0.35f),
            new Color(1f, 0.8f, 0.2f),
        };

        private static readonly Color MoneyGood = new Color(0.55f, 0.85f, 0.45f);
        private static readonly Color MoneyBad = new Color(0.9f, 0.5f, 0.45f);
        private static readonly Color Mishap = new Color(0.7f, 0.45f, 0.75f);
        private static readonly Color Family = new Color(1f, 0.65f, 0.8f);
        private static readonly Color Purchase = new Color(0.45f, 0.75f, 0.9f);
        private static readonly Color Job = new Color(1f, 0.75f, 0.3f);
        private static readonly Color Milestone = new Color(1f, 0.92f, 0.45f);
        private static readonly Color Plain = new Color(0.85f, 0.85f, 0.85f);

        public static Color Seat(int seat) => SeatColors[seat % SeatColors.Length];

        /// <summary>種類をまとめて色分けする（お金が増える／減る／災難／家族／買い物／職業／節目）。マスが多いので種類ごとに全部違う色にすると逆に見分けにくい</summary>
        public static Color Cell(LifeCellType type)
        {
            switch (type)
            {
                case LifeCellType.Income:
                case LifeCellType.Payday:
                    return MoneyGood;
                case LifeCellType.Expense:
                case LifeCellType.Tuition:
                    return MoneyBad;
                case LifeCellType.Sickness:
                case LifeCellType.Accident:
                case LifeCellType.Fire:
                    return Mishap;
                case LifeCellType.Birth:
                case LifeCellType.Marriage:
                    return Family;
                case LifeCellType.House:
                case LifeCellType.Insurance:
                case LifeCellType.Stock:
                    return Purchase;
                case LifeCellType.JobOffer:
                case LifeCellType.Graduation:
                case LifeCellType.ChangeJob:
                    return Job;
                case LifeCellType.Start:
                case LifeCellType.Goal:
                    return Milestone;
                default:
                    return Plain;
            }
        }
    }
}
