#nullable enable
using System;
using System.Collections.Generic;
using CardGame.Core.Ai;
using CardGame.Core.Commands;
using CardGame.Core.Definitions;
using CardGame.Core.Engine;
using CardGame.Core.Events;
using CardGame.Core.State;

namespace CardGame.Unity.Battle
{
    public enum BattleMode
    {
        /// <summary>P0 = 人間、P1 = AI。</summary>
        VersusAi,
        /// <summary>P0/P1 とも人間。1 台を交互に操作。</summary>
        LocalTwoPlayer,
        /// <summary>両方 AI(観戦・デバッグ用)。</summary>
        AiVersusAi,
        /// <summary>オンライン。LocalPlayer が人間、相手はネットワーク越し。</summary>
        Online,
    }

    public sealed class BattleConfig
    {
        public BattleMode Mode;
        public DeckDefinition Deck0 = null!;
        public DeckDefinition Deck1 = null!;
        public ulong Seed;
        /// <summary>先攻。null ならシードから決める。オンラインではホストが決めて共有する。</summary>
        public int? FirstPlayer;
        /// <summary>この端末で操作するプレイヤー(オンライン: ホスト 0 / ゲスト 1)。</summary>
        public int LocalPlayer;
    }

    /// <summary>
    /// 1 対戦のセッション。Core の GameEngine を包み、誰が人間で誰が AI かを管理する。
    /// Unity 非依存(将来オンライン化するときは Submit の先にネットワーク層を挟む)。
    /// </summary>
    public sealed class BattleSession
    {
        public BattleConfig Config { get; }
        public GameEngine Engine { get; }
        public GameState State => Engine.State;

        private readonly IAgent?[] _agents = new IAgent?[2];

        /// <summary>コマンド適用後に発火。引数は発生イベント。</summary>
        public event Action<GameCommand, IReadOnlyList<GameEvent>>? Applied;

        public BattleSession(CardDatabase db, BattleConfig config)
        {
            Config = config;
            Engine = GameEngine.StartNew(db, config.Deck0, config.Deck1, config.Seed, config.FirstPlayer);
            switch (config.Mode)
            {
                case BattleMode.VersusAi:
                    _agents[1] = new GreedyAgent(config.Seed ^ 0xB1);
                    break;
                case BattleMode.AiVersusAi:
                    _agents[0] = new GreedyAgent(config.Seed ^ 0xA1);
                    _agents[1] = new GreedyAgent(config.Seed ^ 0xB1);
                    break;
            }
        }

        /// <summary>この端末で操作する人間か。</summary>
        public bool IsHuman(int player) => _agents[player] == null && !IsRemote(player);
        public bool IsAi(int player) => _agents[player] != null;
        /// <summary>ネットワーク越しの相手か。</summary>
        public bool IsRemote(int player) => Config.Mode == BattleMode.Online && player != Config.LocalPlayer;

        /// <summary>今コマンドを出すべきプレイヤー。決着済みなら null。</summary>
        public int? PlayerToAct
        {
            get
            {
                if (State.IsFinished) return null;
                if (State.Phase == GamePhase.Mulligan)
                {
                    if (!State.Players[0].MulliganDone) return 0;
                    if (!State.Players[1].MulliganDone) return 1;
                    return null;
                }
                return State.CurrentPlayer;
            }
        }

        public bool AiShouldAct => PlayerToAct is int p && IsAi(p);

        public IReadOnlyList<GameEvent> Submit(GameCommand command)
        {
            var events = Engine.Apply(command);
            Applied?.Invoke(command, events);
            return events;
        }

        public GameCommand AiDecide()
        {
            int p = PlayerToAct ?? throw new InvalidOperationException("行動できるプレイヤーがいない");
            var agent = _agents[p] ?? throw new InvalidOperationException($"P{p} は AI ではない");
            return agent.Decide(Engine, p);
        }
    }
}
