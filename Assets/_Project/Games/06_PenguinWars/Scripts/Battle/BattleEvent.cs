namespace MiniGame.PenguinWars.Battle
{
    public enum BattleEventType
    {
        Spawned,
        Hit,
        Died,
        CastleDestroyed,
        /// <summary>エンドレスの敵レベルが上がった。Amount は新しいレベル</summary>
        EnemyLevelUp,
        /// <summary>ペンギン砲。Side は撃った側、X は届いた先端</summary>
        CannonFired,
        /// <summary>ノックバックした（Side はやられた側）</summary>
        Knockback,
        /// <summary>状態異常がかかった。Amount は UnitStatusType</summary>
        StatusApplied,
        /// <summary>対戦の時間切れ（仕様書 §2.2）。Side は城の残りHP割合が少なかった側、Amount は引き分けなら 1</summary>
        TimeUp,
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
        /// <summary>Hit はダメージ、Died は倒した側に入った撃破報酬（Phase 7 で獲得さかなを表示するため）、EnemyLevelUp は新しいレベル、StatusApplied は UnitStatusType</summary>
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
