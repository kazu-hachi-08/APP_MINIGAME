using System.Collections.Generic;

namespace MiniGame.LifeGame
{
    /// <summary>時代イベント（B4）。None は最初の時代が来る前</summary>
    public enum LifeEra
    {
        None,
        Boom,
        Recession,
        StockBoom,
        BabyBoom,
        Peace,
    }

    /// <summary>
    /// 時代の引き方と倍率。倍率の計算をここ1か所に集め、支払い（LifeRules・LifeMoney）と
    /// 効果の説明（LifeTexts）が同じ式を通るようにする（表示と実際の金額がずれないように）。
    /// </summary>
    public static class LifeEras
    {
        private const int NoChange = 100;

        private static readonly LifeEra[] Eras = { LifeEra.Boom, LifeEra.Recession, LifeEra.StockBoom, LifeEra.BabyBoom, LifeEra.Peace };

        /// <summary>
        /// 区間の段階（区間B = 1、分岐②の道 = 2、区間C = 3）。時代が変わらない区間は 0。
        /// 段階で持つと、先頭が入った区間より手前の区間に後続が入っても引き直さずに済む
        /// </summary>
        public static int StageOf(LifeSection section)
        {
            switch (section)
            {
                case LifeSection.Middle: return 1;
                case LifeSection.Safe:
                case LifeSection.Gamble: return 2;
                case LifeSection.Final: return 3;
                default: return 0;
            }
        }

        /// <summary>今と違う時代を引く（同じ時代が2回続くと変わった感じがしないため）</summary>
        public static LifeEra Draw(LifeRandom random, LifeEra current)
        {
            var candidates = new List<LifeEra>();
            foreach (LifeEra era in Eras)
            {
                if (era != current) candidates.Add(era);
            }

            return candidates[random.Next(candidates.Count)];
        }

        /// <summary>
        /// 時代の倍率をかけた額。type は効果の出どころ（配当は Stock、ご祝儀は Marriage / Birth）。
        /// float を使わず % の整数で計算する（オンラインで全端末の結果をそろえるため）
        /// </summary>
        public static int Multiply(LifeGameState state, LifeCellType type, int amount)
        {
            return amount * PercentOf(state.Config, state.Era, type) / 100;
        }

        public static int PercentOf(LifeRuleConfig config, LifeEra era, LifeCellType type)
        {
            switch (era)
            {
                case LifeEra.Boom:
                    return type == LifeCellType.Income || type == LifeCellType.Nominate ? config.EraBoomPercent : NoChange;
                case LifeEra.Recession:
                    if (type == LifeCellType.Income) return config.EraRecessionIncomePercent;
                    return type == LifeCellType.Expense ? config.EraRecessionExpensePercent : NoChange;
                case LifeEra.StockBoom:
                    return type == LifeCellType.Stock ? config.EraStockBoomPercent : NoChange;
                case LifeEra.BabyBoom:
                    return type == LifeCellType.Marriage || type == LifeCellType.Birth ? config.EraBabyBoomPercent : NoChange;
                case LifeEra.Peace:
                    bool mishap = type == LifeCellType.Sickness || type == LifeCellType.Accident || type == LifeCellType.Fire;
                    return mishap ? config.EraPeacePercent : NoChange;
                default:
                    return NoChange;
            }
        }

        public static bool Affects(LifeGameState state, LifeCellType type) => PercentOf(state.Config, state.Era, type) != NoChange;
    }
}
