using System.Collections.Generic;
using NUnit.Framework;

namespace MiniGame.LifeGame.Tests
{
    /// <summary>NPC同士で最後まで遊ばせて、ルールが途中で詰まらないことを確かめる</summary>
    public class LifeSimulationTests
    {
        private const int GameCount = 100;

        // 1試合のコマンド数の上限。これを超えたら無限ループとみなす
        private const int MaxCommands = 2000;

        private static readonly LifeAbility[] AllAbilities =
            { LifeAbility.StartMoney, LifeAbility.Salary, LifeAbility.Reroll, LifeAbility.Thrift };

        private static LifeGameState PlayToEnd(int playerCount, int seed, List<LifeCommand> log = null)
        {
            return PlayToEnd(new LifeAbility[playerCount], seed, log);
        }

        private static LifeGameState PlayToEnd(LifeAbility[] abilities, int seed, List<LifeCommand> log = null)
        {
            LifeGameState state = LifeGameState.Create(abilities, seed, new LifeRuleConfig());
            var npcRandom = new LifeRandom(seed + 10000);

            for (int i = 0; i < MaxCommands && state.Pending != LifePending.Finished; i++)
            {
                LifeCommand command = LifeNpcPlanner.Plan(state, npcRandom);
                Assert.IsTrue(LifeRules.IsValid(state, command), $"seed={seed} {command} pending={state.Pending}");

                LifeRules.Apply(state, command);
                log?.Add(command);
            }

            Assert.AreEqual(LifePending.Finished, state.Pending, $"seed={seed} が終わらない");
            return state;
        }

        [Test]
        public void NPC4人で100試合が最後まで終わる()
        {
            for (int seed = 0; seed < GameCount; seed++)
            {
                LifeGameState state = PlayToEnd(4, seed);
                List<LifeSettlementEntry> entries = LifeSettlement.Settle(state);

                var ranks = new HashSet<int>();
                foreach (LifeSettlementEntry entry in entries) ranks.Add(entry.Rank);
                Assert.AreEqual(4, ranks.Count);
            }
        }

        [Test]
        public void 能力持ちのNPC4人でも最後まで終わる()
        {
            for (int seed = 0; seed < GameCount; seed++) PlayToEnd(AllAbilities, seed);
        }

        [Test]
        public void NPC2人でも最後まで終わる()
        {
            for (int seed = 0; seed < GameCount; seed++) PlayToEnd(2, seed);
        }

        [Test]
        public void 同じシードと同じコマンド列なら同じ結果になる()
        {
            var log = new List<LifeCommand>();
            LifeGameState original = PlayToEnd(3, 123, log);

            // オンラインの別端末と同じ状況：コマンドだけを受け取って再生する
            LifeGameState replay = LifeGameState.Create(3, 123, new LifeRuleConfig());
            foreach (LifeCommand command in log) LifeRules.Apply(replay, command);

            for (int seat = 0; seat < 3; seat++)
            {
                Assert.AreEqual(original.Players[seat].Money, replay.Players[seat].Money);
                Assert.AreEqual(original.Players[seat].GoalOrder, replay.Players[seat].GoalOrder);
            }
        }
    }
}
