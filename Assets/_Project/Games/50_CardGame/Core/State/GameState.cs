using System;
using System.Collections.Generic;
using CardGame.Core.Definitions;

namespace CardGame.Core.State
{
    /// <summary>ゲーム全体の定数(docs/spec/01-rules.md「基本パラメータ」)。</summary>
    public static class GameRules
    {
        public const int LeaderMaxHp = 20;
        public const int HandMax = 9;
        public const int BoardMax = 5;
        public const int PpMax = 10;
        public const int FirstPlayerInitialHand = 3;
        public const int SecondPlayerInitialHand = 4;
        /// <summary>後攻に与える追加 PP(覚醒の刻)の回数。01-rules.md「後攻の追加 PP」。</summary>
        public const int SecondPlayerBonusPp = 1;
        /// <summary>追加 PP を使えるようになる最大 PP(それ未満のターンでは発動しない)。序盤に出ると効きすぎるため。</summary>
        public static int SecondPlayerBonusPpFromMaxPp = 4;
    }

    public enum GamePhase
    {
        /// <summary>両者のマリガン待ち。</summary>
        Mulligan,
        /// <summary>手番プレイヤーのメインフェーズ。</summary>
        Main,
        /// <summary>決着済み。</summary>
        Finished,
    }

    /// <summary>
    /// 対戦の全状態。GameEngine が Command を適用して書き換える。
    /// 決定論性のため、状態はこのオブジェクト木と <see cref="Rng"/> だけで完結する。
    /// </summary>
    public sealed class GameState
    {
        public PlayerState[] Players { get; }
        public GamePhase Phase { get; set; }
        /// <summary>手番プレイヤー(0 or 1)。</summary>
        public int CurrentPlayer { get; set; }
        /// <summary>何ターン目か。先攻の最初のターンが 1。手番が変わるごとに +1。</summary>
        public int TurnNumber { get; set; }
        /// <summary>勝者。引き分けなら null(Phase==Finished かつ Winner==null)。</summary>
        public int? Winner { get; set; }
        public DeterministicRandom Rng { get; }

        private int _nextInstanceId;

        public GameState(CardClass class0, CardClass class1, ulong seed)
        {
            Players = new[] { new PlayerState(0, class0), new PlayerState(1, class1) };
            Phase = GamePhase.Mulligan;
            CurrentPlayer = 0;
            TurnNumber = 0;
            Rng = new DeterministicRandom(seed);
            _nextInstanceId = 1;
        }

        private GameState(GameState src)
        {
            Players = new[] { src.Players[0].Clone(), src.Players[1].Clone() };
            Phase = src.Phase;
            CurrentPlayer = src.CurrentPlayer;
            TurnNumber = src.TurnNumber;
            Winner = src.Winner;
            Rng = src.Rng.Clone();
            _nextInstanceId = src._nextInstanceId;
        }

        /// <summary>AI の先読みなどに使う完全な複製。</summary>
        public GameState Clone() => new GameState(this);

        public PlayerState Current => Players[CurrentPlayer];
        public PlayerState Opponent => Players[1 - CurrentPlayer];
        public PlayerState PlayerOf(int index) => Players[index];
        public PlayerState OpponentOf(int index) => Players[1 - index];
        public bool IsFinished => Phase == GamePhase.Finished;

        public int AllocateInstanceId() => _nextInstanceId++;

        public CardInstance CreateCard(CardDefinition def) => new CardInstance(AllocateInstanceId(), def);

        /// <summary>両プレイヤーの場から InstanceId で検索。</summary>
        public BoardEntity? FindEntity(int instanceId)
            => Players[0].FindEntity(instanceId) ?? Players[1].FindEntity(instanceId);

        /// <summary>TargetRef が指すエンティティ。リーダーや存在しない場合は null。</summary>
        public BoardEntity? Resolve(TargetRef target) => target.IsLeader ? null : FindEntity(target.InstanceId);
    }
}
