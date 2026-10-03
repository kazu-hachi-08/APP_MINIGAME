using System;
using System.Collections.Generic;

namespace MiniGame.LifeGame
{
    /// <summary>
    /// シードから盤面を作る（仕様書 §5.3）。区間ごとにシャッフルし、同じ種類が3つ以上続かないように並べ直す。
    /// </summary>
    public static class LifeBoardGenerator
    {
        private const int MaxRunLength = 2;

        // 表の組み方が悪いと永遠に並べ直すので、上限を超えたら設定ミスとして例外にする
        private const int MaxShuffleAttempts = 1000;

        private const int TextVariantCount = 100;

        public static LifeBoard Generate(LifeBoardShape shape, LifeRuleConfig config, LifeRandom random)
        {
            var board = new LifeBoard();
            LifeCell start = board.Add(LifeCellType.Start, LifeSection.Start);
            LifeCell firstBranch = board.Add(LifeCellType.Branch, LifeSection.Start);
            start.Next.Add(firstBranch.Index);

            List<LifeCell> firstEnds = AddRoutes(board, firstBranch, shape.FirstRoutes, config, random);

            LifeCell middleFirst = AddSection(board, shape.Middle, config, random, out LifeCell secondBranch);
            foreach (LifeCell end in firstEnds) end.Next.Add(middleFirst.Index);

            List<LifeCell> secondEnds = AddRoutes(board, secondBranch, shape.SecondRoutes, config, random);

            LifeCell finalFirst = AddSection(board, shape.Final, config, random, out LifeCell finalLast);
            foreach (LifeCell end in secondEnds) end.Next.Add(finalFirst.Index);

            LifeCell goal = board.Add(LifeCellType.Goal, LifeSection.Final);
            finalLast.Next.Add(goal.Index);
            goal.Prev = finalLast.Index;
            board.GoalIndex = goal.Index;
            return board;
        }

        /// <summary>分岐の先に各ルートを並べ、各ルートの最後のマスを返す</summary>
        private static List<LifeCell> AddRoutes(LifeBoard board, LifeCell branch, LifeSectionShape[] routes, LifeRuleConfig config, LifeRandom random)
        {
            var ends = new List<LifeCell>();
            foreach (LifeSectionShape route in routes)
            {
                LifeCell first = AddSection(board, route, config, random, out LifeCell last);
                branch.Next.Add(first.Index);
                ends.Add(last);
            }

            return ends;
        }

        private static LifeCell AddSection(LifeBoard board, LifeSectionShape shape, LifeRuleConfig config, LifeRandom random, out LifeCell last)
        {
            LifeCellType[] types = ArrangeSection(shape, random);
            LifeCell first = null;
            last = null;
            foreach (LifeCellType type in types)
            {
                LifeCell cell = board.Add(type, shape.Section);
                cell.Amount = RollAmount(type, shape.DoubleAmounts, config, random);
                cell.TextVariant = random.Next(TextVariantCount);

                if (last != null)
                {
                    last.Next.Add(cell.Index);
                    cell.Prev = last.Index;
                }

                if (first == null) first = cell;
                last = cell;
            }

            return first;
        }

        /// <summary>固定マスを置いた残りの枠に、表のマスをシャッフルして配る</summary>
        public static LifeCellType[] ArrangeSection(LifeSectionShape shape, LifeRandom random)
        {
            List<LifeCellType> deck = ExpandDeck(shape);
            if (deck.Count + shape.FixedCells.Count != shape.Length)
            {
                throw new InvalidOperationException($"{shape.Section}: 表のマス数 {deck.Count} と固定マス {shape.FixedCells.Count} の合計が区間の長さ {shape.Length} と合わない");
            }

            var types = new LifeCellType[shape.Length];
            for (int attempt = 0; attempt < MaxShuffleAttempts; attempt++)
            {
                random.Shuffle(deck);
                Fill(shape, deck, types);
                if (!HasLongRun(types)) return types;
            }

            throw new InvalidOperationException($"{shape.Section}: 同じ種類が3連続しない並びを作れない");
        }

        private static List<LifeCellType> ExpandDeck(LifeSectionShape shape)
        {
            var deck = new List<LifeCellType>();
            foreach (KeyValuePair<LifeCellType, int> entry in shape.Deck)
            {
                for (int i = 0; i < entry.Value; i++) deck.Add(entry.Key);
            }

            // Dictionary の列挙順に依存すると端末ごとに並びが変わり得るので、シャッフル前に並びを固定する
            deck.Sort();
            return deck;
        }

        private static void Fill(LifeSectionShape shape, List<LifeCellType> deck, LifeCellType[] types)
        {
            int next = 0;
            for (int i = 0; i < types.Length; i++)
            {
                types[i] = shape.FixedCells.TryGetValue(i, out LifeCellType fixedType) ? fixedType : deck[next++];
            }
        }

        public static bool HasLongRun(IReadOnlyList<LifeCellType> types)
        {
            int run = 1;
            for (int i = 1; i < types.Count; i++)
            {
                run = types[i] == types[i - 1] ? run + 1 : 1;
                if (run > MaxRunLength) return true;
            }

            return false;
        }

        private static int RollAmount(LifeCellType type, bool doubled, LifeRuleConfig config, LifeRandom random)
        {
            int multiplier = doubled ? config.GambleMultiplier : 1;
            switch (type)
            {
                case LifeCellType.Income:
                case LifeCellType.Expense:
                    return RollStep(config.MoneyCellMin, config.MoneyCellMax, config.AmountStep, random) * multiplier;
                case LifeCellType.Sickness:
                case LifeCellType.Accident:
                    return RollStep(config.MishapMin, config.MishapMax, config.AmountStep, random) * multiplier;
                case LifeCellType.Nominate:
                case LifeCellType.Present:
                    return RollStep(config.TransferMin, config.TransferMax, config.AmountStep, random) * multiplier;
                case LifeCellType.Bet:
                    return config.BetStake * multiplier;
                case LifeCellType.Forward:
                case LifeCellType.Back:
                    // 歩数はギャンブルルートでも倍にしない（お金ではないので）
                    return random.Range(config.WarpMin, config.WarpMax);
                case LifeCellType.Tuition:
                    return config.Tuition;
                default:
                    return 0;
            }
        }

        // 金額はキリのいい数字にしたいので、刻み単位で乱数を取る
        private static int RollStep(int min, int max, int step, LifeRandom random)
        {
            return random.Range(min / step, max / step) * step;
        }
    }
}
