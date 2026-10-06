using System;

namespace CardGame.Core.State
{
    /// <summary>
    /// 攻撃・効果の対象参照。リーダー(プレイヤー番号)または場のエンティティ(InstanceId)。
    /// </summary>
    public readonly struct TargetRef : IEquatable<TargetRef>
    {
        public bool IsLeader { get; }
        /// <summary>IsLeader のとき: プレイヤー番号。</summary>
        public int PlayerIndex { get; }
        /// <summary>IsLeader でないとき: BoardEntity.InstanceId。</summary>
        public int InstanceId { get; }

        private TargetRef(bool isLeader, int playerIndex, int instanceId)
        {
            IsLeader = isLeader;
            PlayerIndex = playerIndex;
            InstanceId = instanceId;
        }

        public static TargetRef Leader(int playerIndex) => new TargetRef(true, playerIndex, -1);
        public static TargetRef Entity(int instanceId) => new TargetRef(false, -1, instanceId);
        public static TargetRef Entity(BoardEntity entity) => new TargetRef(false, -1, entity.InstanceId);

        public bool Equals(TargetRef other) => IsLeader == other.IsLeader && PlayerIndex == other.PlayerIndex && InstanceId == other.InstanceId;
        public override bool Equals(object? obj) => obj is TargetRef t && Equals(t);
        public override int GetHashCode() => IsLeader ? 1000 + PlayerIndex : InstanceId;
        public static bool operator ==(TargetRef a, TargetRef b) => a.Equals(b);
        public static bool operator !=(TargetRef a, TargetRef b) => !a.Equals(b);

        public override string ToString() => IsLeader ? $"Leader(P{PlayerIndex})" : $"Entity(#{InstanceId})";
    }
}
