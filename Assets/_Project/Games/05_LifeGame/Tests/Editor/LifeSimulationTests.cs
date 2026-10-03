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

        // 再生の一致を確かめる試合数。全フィールドを比べるので100試合より少なくしている
        private const int ReplaySeedCount = 20;

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

        [Test]
        public void 能力持ち4人でも同じコマンド列の再生で状態が完全に一致する()
        {
            for (int seed = 0; seed < ReplaySeedCount; seed++)
            {
                var log = new List<LifeCommand>();
                LifeGameState original = PlayToEnd(AllAbilities, seed, log);

                LifeGameState replay = LifeGameState.Create(AllAbilities, seed, new LifeRuleConfig());
                foreach (LifeCommand command in log) LifeRules.Apply(replay, command);

                AssertSameState(original, replay, seed);
            }
        }

        /// <summary>オンラインで手番以外の人・範囲外の値のコマンドが届いても、無視されて結果が変わらないこと</summary>
        [Test]
        public void 不正なコマンドが混ざっても再生結果は変わらない()
        {
            const int seed = 7;
            var log = new List<LifeCommand>();
            LifeGameState original = PlayToEnd(AllAbilities, seed, log);

            LifeGameState replay = LifeGameState.Create(AllAbilities, seed, new LifeRuleConfig());
            foreach (LifeCommand command in log)
            {
                int otherSeat = (replay.CurrentSeat + 1) % AllAbilities.Length;
                Assert.IsEmpty(LifeRules.Apply(replay, new LifeCommand(otherSeat, LifeCommandType.Spin)));
                Assert.IsEmpty(LifeRules.Apply(replay, new LifeCommand(replay.CurrentSeat, LifeCommandType.ChooseStock, 99)));
                Assert.IsEmpty(LifeRules.Apply(replay, new LifeCommand(replay.CurrentSeat, (LifeCommandType)99)));
                LifeRules.Apply(replay, command);
            }

            AssertSameState(original, replay, seed);
        }

        /// <summary>金額を調整したときに手動で回す（数秒かかるので普段のテストからは外す）。結果はテストの出力に出る</summary>
        [Test, Explicit]
        public void バランス集計()
        {
            const int balanceGameCount = 3000;
            var config = new LifeRuleConfig();
            TestContext.WriteLine(LifeBalanceSimulator.Run(balanceGameCount, 4, config));
            TestContext.WriteLine(LifeBalanceSimulator.Run(balanceGameCount, 2, config));
            TestContext.WriteLine(LifeBalanceSimulator.RunAbilities(balanceGameCount, config));
        }

        /// <summary>盤面以外のルールの状態をすべて比べる。乱数は次に出る値で、引いた回数が揃っているかを見る</summary>
        private static void AssertSameState(LifeGameState expected, LifeGameState actual, int seed)
        {
            string at = $"seed={seed}";
            Assert.AreEqual(expected.Pending, actual.Pending, at);
            Assert.AreEqual(expected.CurrentSeat, actual.CurrentSeat, at);
            Assert.AreEqual(expected.LastRoll, actual.LastRoll, at);
            Assert.AreEqual(expected.GoalCount, actual.GoalCount, at);
            Assert.AreEqual(expected.IsWarping, actual.IsWarping, at);
            Assert.AreEqual(expected.Random.NextUInt64(), actual.Random.NextUInt64(), at);

            for (int seat = 0; seat < expected.Players.Count; seat++)
            {
                LifePlayerState e = expected.Players[seat];
                LifePlayerState a = actual.Players[seat];
                string who = $"{at} P{seat + 1}";
                Assert.AreEqual(e.Money, a.Money, who);
                Assert.AreEqual(e.Position, a.Position, who);
                Assert.AreEqual(e.JobId, a.JobId, who);
                Assert.AreEqual(e.HouseId, a.HouseId, who);
                Assert.AreEqual(e.Insurances, a.Insurances, who);
                Assert.AreEqual(e.LifeInsurancePaid, a.LifeInsurancePaid, who);
                CollectionAssert.AreEqual(e.Stocks, a.Stocks, who);
                Assert.AreEqual(e.Notes, a.Notes, who);
                Assert.AreEqual(e.RerollUsed, a.RerollUsed, who);
                Assert.AreEqual(e.IsMarried, a.IsMarried, who);
                Assert.AreEqual(e.Children, a.Children, who);
                Assert.AreEqual(e.GoalOrder, a.GoalOrder, who);
                Assert.AreEqual(e.RestTurns, a.RestTurns, who);
            }
        }
    }
}
