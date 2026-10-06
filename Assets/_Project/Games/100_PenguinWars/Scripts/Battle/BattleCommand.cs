namespace MiniGame.PenguinWars.Battle
{
    public enum BattleCommandType
    {
        Spawn,
        LevelUpWallet,
        FireCannon,
    }

    /// <summary>
    /// 外からの操作。キー・ボタン・CPU・オンラインのゲストのどれから来ても同じ形で BattleWorld に渡す（オンラインで通信に載せるため。§10.2）
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

        public static BattleCommand LevelUpWallet(Side side)
        {
            return new BattleCommand(BattleCommandType.LevelUpWallet, side, 0);
        }

        public static BattleCommand FireCannon(Side side)
        {
            return new BattleCommand(BattleCommandType.FireCannon, side, 0);
        }
    }
}
