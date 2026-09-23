using System.Collections.Generic;
using System.Linq;
using CardGame.Core.State;

namespace CardGame.Core.Commands
{
    /// <summary>
    /// プレイヤーの操作。ローカル / AI / オンラインのいずれもこの型で GameEngine に渡す(Docs/50_CardGame/spec/05-online.md)。
    /// </summary>
    public abstract class GameCommand
    {
        /// <summary>操作するプレイヤー。</summary>
        public int Player { get; }

        protected GameCommand(int player) { Player = player; }
    }

    /// <summary>マリガン: 指定した手札(インデックス)をデッキに戻して同数引き直す。</summary>
    public sealed class MulliganCommand : GameCommand
    {
        public IReadOnlyList<int> HandIndices { get; }

        public MulliganCommand(int player, IEnumerable<int>? handIndices = null) : base(player)
        {
            HandIndices = (handIndices ?? Enumerable.Empty<int>()).Distinct().OrderBy(i => i).ToList();
        }

        public override string ToString() => $"P{Player} Mulligan[{string.Join(",", HandIndices)}]";
    }

    /// <summary>手札のカードをプレイする。</summary>
    public sealed class PlayCardCommand : GameCommand
    {
        public int HandIndex { get; }
        /// <summary>フォロワー/アミュレットを場に置く位置(0..Board.Count)。スペルでは無視。</summary>
        public int BoardPosition { get; }
        /// <summary>Select* 対象。不要なら null。</summary>
        public TargetRef? Target { get; }

        public PlayCardCommand(int player, int handIndex, int boardPosition = int.MaxValue, TargetRef? target = null) : base(player)
        {
            HandIndex = handIndex;
            BoardPosition = boardPosition;
            Target = target;
        }

        public override string ToString() => $"P{Player} Play hand[{HandIndex}] -> {Target?.ToString() ?? "-"}";
    }

    /// <summary>フォロワーで攻撃する。</summary>
    public sealed class AttackCommand : GameCommand
    {
        public int AttackerInstanceId { get; }
        public TargetRef Target { get; }

        public AttackCommand(int player, int attackerInstanceId, TargetRef target) : base(player)
        {
            AttackerInstanceId = attackerInstanceId;
            Target = target;
        }

        public override string ToString() => $"P{Player} Attack #{AttackerInstanceId} -> {Target}";
    }

    public sealed class EndTurnCommand : GameCommand
    {
        public EndTurnCommand(int player) : base(player) { }
        public override string ToString() => $"P{Player} EndTurn";
    }

    public sealed class SurrenderCommand : GameCommand
    {
        public SurrenderCommand(int player) : base(player) { }
        public override string ToString() => $"P{Player} Surrender";
    }
}
