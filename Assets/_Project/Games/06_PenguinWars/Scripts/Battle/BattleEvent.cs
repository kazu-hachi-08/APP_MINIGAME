namespace MiniGame.PenguinWars.Battle
{
    public enum BattleEventType
    {
        Spawned,
        Hit,
        Died,
        CastleDestroyed,
    }

    /// <summary>
    /// 1ステップ中に起きた出来事。View の演出・音・オンラインの演出同期はこれを見て鳴らす
    /// </summary>
    public readonly struct BattleEvent
    {
        /// <summary>城が対象のときの UnitId</summary>
        public const int CastleId = -1;

        public BattleEventType Type { get; }
        /// <summary>出来事が起きた側（Hit・Died・CastleDestroyed は「やられた側」）</summary>
        public Side Side { get; }
        public int UnitId { get; }
        public float X { get; }
        public int Amount { get; }

        public BattleEvent(BattleEventType type, Side side, int unitId, float x, int amount = 0)
        {
            Type = type;
            Side = side;
            UnitId = unitId;
            X = x;
            Amount = amount;
        }
    }
}
