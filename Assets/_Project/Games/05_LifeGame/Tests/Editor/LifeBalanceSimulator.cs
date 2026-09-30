using System;
using System.Collections.Generic;
using System.Text;

namespace MiniGame.LifeGame.Tests
{
    /// <summary>
    /// 金額バランス調整用に、NPC同士の大量試合の統計を文字列で返す（NUnit に依存しないので dotnet のコンソールからも回せる）。
    /// ルートの有利不利を公平に比べるため、分岐はNPCの判断ではなく等確率で選ばせる
    /// （NPCは「負けている人がギャンブル」を選ぶので、そのままだとギャンブルの勝率が低く見えてしまう）。
    /// </summary>
    public static class LifeBalanceSimulator
    {
        private const int MaxCommands = 2000;

        private static readonly LifeSection[] FirstRoutes = { LifeSection.Job, LifeSection.University, LifeSection.Freeter };
        private static readonly LifeSection[] SecondRoutes = { LifeSection.Safe, LifeSection.Gamble };

        private sealed class PlayerRecord
        {
            public LifeSection First;
            public LifeSection Second;
            public int Spins;
            public int Total;
            public int Rank;
            public int Notes;
        }

        public static string Run(int gameCount, int playerCount, LifeRuleConfig config, LifeAbility[] abilities = null)
        {
            var records = new List<PlayerRecord>();
            var spinsPerGame = new List<int>();
            for (int seed = 0; seed < gameCount; seed++)
            {
                LifeAbility[] seats = RotateAbilities(abilities, playerCount, seed);
                List<PlayerRecord> game = PlayOne(seats, seed, config);
                records.AddRange(game);

                int spins = 0;
                foreach (PlayerRecord record in game) spins += record.Spins;
                spinsPerGame.Add(spins);
            }

            return Format(records, spinsPerGame, playerCount);
        }

        // 能力の有利不利を席順の有利不利と混ぜないよう、試合ごとに座る席をずらす
        private static LifeAbility[] RotateAbilities(LifeAbility[] abilities, int playerCount, int seed)
        {
            var seats = new LifeAbility[playerCount];
            if (abilities == null) return seats;

            for (int i = 0; i < playerCount; i++) seats[i] = abilities[(i + seed) % abilities.Length];
            return seats;
        }

        private static List<PlayerRecord> PlayOne(LifeAbility[] abilities, int seed, LifeRuleConfig config)
        {
            LifeGameState state = LifeGameState.Create(abilities, seed, config);
            var npcRandom = new LifeRandom(seed + 10000);
            var routeRandom = new LifeRandom(seed + 20000);
            var records = new List<PlayerRecord>();
            foreach (LifePlayerState _ in state.Players) records.Add(new PlayerRecord());

            for (int i = 0; i < MaxCommands && state.Pending != LifePending.Finished; i++)
            {
                LifeCommand command = LifeNpcPlanner.Plan(state, npcRandom);
                if (command.Type == LifeCommandType.ChooseBranch)
                {
                    command = new LifeCommand(command.Seat, LifeCommandType.ChooseBranch, routeRandom.Next(state.CurrentCell.Next.Count));
                }

                if (command.Type == LifeCommandType.Spin) records[command.Seat].Spins++;
                LifeRules.Apply(state, command);
                RecordRoute(state, records[command.Seat], command.Seat);
            }

            if (state.Pending != LifePending.Finished) throw new InvalidOperationException($"seed={seed} が終わらない");

            foreach (LifePlayerState player in state.Players) records[player.Seat].Notes = player.Notes;
            foreach (LifeSettlementEntry entry in LifeSettlement.Settle(state))
            {
                records[entry.Seat].Total = entry.Total;
                records[entry.Seat].Rank = entry.Rank;
            }

            return records;
        }

        private static void RecordRoute(LifeGameState state, PlayerRecord record, int seat)
        {
            LifeSection section = state.Board[state.Players[seat].Position].Section;
            if (Array.IndexOf(FirstRoutes, section) >= 0) record.First = section;
            if (Array.IndexOf(SecondRoutes, section) >= 0) record.Second = section;
        }

        private static string Format(List<PlayerRecord> records, List<int> spinsPerGame, int playerCount)
        {
            var text = new StringBuilder();
            double expectedWin = 100.0 / playerCount;
            text.AppendLine($"試合数 {spinsPerGame.Count} / {playerCount}人 / 勝率の基準 {expectedWin:F1}%");

            var spins = new List<int>();
            var totals = new List<int>();
            foreach (PlayerRecord record in records)
            {
                spins.Add(record.Spins);
                totals.Add(record.Total);
            }

            text.AppendLine($"1人のルーレット回数 {Summary(spins)}");
            text.AppendLine($"1試合のルーレット回数 {Summary(spinsPerGame)}");
            text.AppendLine($"総資産 {Summary(totals)}");
            text.AppendLine();

            text.AppendLine("ルート          人数   勝率   平均資産  中央値  回数");
            foreach (LifeSection route in FirstRoutes) AppendRoute(text, records, r => r.First == route, route.ToString());
            foreach (LifeSection route in SecondRoutes) AppendRoute(text, records, r => r.Second == route, route.ToString());
            text.AppendLine();

            foreach (LifeSection first in FirstRoutes)
            {
                foreach (LifeSection second in SecondRoutes)
                {
                    AppendRoute(text, records, r => r.First == first && r.Second == second, $"{first}+{second}");
                }
            }

            return text.ToString();
        }

        public static string RunAbilities(int gameCount, LifeRuleConfig config)
        {
            LifeAbility[] abilities = { LifeAbility.StartMoney, LifeAbility.Salary, LifeAbility.Reroll, LifeAbility.Thrift };
            var wins = new int[abilities.Length];
            for (int seed = 0; seed < gameCount; seed++)
            {
                LifeAbility[] seats = RotateAbilities(abilities, abilities.Length, seed);
                List<PlayerRecord> game = PlayOne(seats, seed, config);
                for (int seat = 0; seat < seats.Length; seat++)
                {
                    if (game[seat].Rank == 1) wins[Array.IndexOf(abilities, seats[seat])]++;
                }
            }

            var text = new StringBuilder($"能力の勝率（{gameCount}試合・4人・基準 25%）\n");
            for (int i = 0; i < abilities.Length; i++) text.AppendLine($"  {abilities[i],-10} {100.0 * wins[i] / gameCount,5:F1}%");
            return text.ToString();
        }

        private static void AppendRoute(StringBuilder text, List<PlayerRecord> records, Predicate<PlayerRecord> match, string label)
        {
            List<PlayerRecord> hit = records.FindAll(match);
            if (hit.Count == 0) return;

            int wins = 0;
            var totals = new List<int>();
            var spins = new List<int>();
            foreach (PlayerRecord record in hit)
            {
                if (record.Rank == 1) wins++;
                totals.Add(record.Total);
                spins.Add(record.Spins);
            }

            totals.Sort();
            text.AppendLine($"{label,-15} {hit.Count,5}  {100.0 * wins / hit.Count,5:F1}%  {Average(totals),7:F0}  {totals[totals.Count / 2],6}  {Average(spins),4:F1}");
        }

        private static string Summary(List<int> values)
        {
            var sorted = new List<int>(values);
            sorted.Sort();
            int P(int percent) => sorted[(sorted.Count - 1) * percent / 100];
            return $"平均 {Average(sorted):F1} / 最小 {sorted[0]} / 10% {P(10)} / 50% {P(50)} / 90% {P(90)} / 最大 {sorted[sorted.Count - 1]}";
        }

        private static double Average(List<int> values)
        {
            long sum = 0;
            foreach (int value in values) sum += value;
            return (double)sum / values.Count;
        }
    }
}
