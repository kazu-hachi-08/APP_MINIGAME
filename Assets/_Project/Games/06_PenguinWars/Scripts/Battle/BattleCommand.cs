namespace MiniGame.PenguinWars.Battle
{
    public enum BattleCommandType
    {
        Spawn,
    }

    /// <summary>
    /// 外からの操作。キー・ボタン・CPU・オンラインのゲストのどれから来ても同じ形で BattleWorld に渡す（Phase 10 で通信に載せるため）
    /// </summary>
    public readonly struct BattleCommand
    {
        public BattleCommandType Type { get; }
        public Side Side { get; }
        public int SlotIndex { get; }

        private BattleCommand(BattleCommandType type, Side side, int slotIndex)
        {
            Type = type;
            Side = side;
            SlotIndex = slotIndex;
        }

        public static BattleCommand Spawn(Side side, int slotIndex)
        {
            return new BattleCommand(BattleCommandType.Spawn, side, slotIndex);
        }
    }
}
