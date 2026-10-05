namespace MiniGame.PenguinWars.Battle
{
    public enum UnitAction
    {
        Walk,
        /// <summary>攻撃発生待ち。本家と同じく、発生の瞬間に射程内にいる相手にだけ当たる</summary>
        Windup,
        /// <summary>攻撃後の硬直。終わったら Walk に戻って射程を見直す</summary>
        Cooldown,
        /// <summary>後ろに飛ばされている間。動けず攻撃もしない（仕様書 §5.4）</summary>
        Knockback,
        Dead,
    }

    /// <summary>戦場にいる1体の実行時の状態。View とオンライン同期はこれを読むだけにする</summary>
    public class UnitState
    {
        public int Id { get; }
        public Side Side { get; }
        public UnitStats Stats { get; }
        public float X { get; internal set; }
        public int Hp { get; internal set; }
        public UnitAction Action { get; internal set; }
        /// <summary>Windup / Cooldown / Knockback の残り秒数</summary>
        public float ActionTimer { get; internal set; }
        public UnitStatusEffects Status { get; } = new UnitStatusEffects();
        /// <summary>ステージの IsBoss の行で出た敵。表示（拡大・画面上部のHPバー）だけに使い、強さは StatMultiplier で決める</summary>
        public bool IsBoss { get; internal set; }

        public int UnitNo => Stats.UnitNo;
        public bool IsDead => Action == UnitAction.Dead;
        public float HpRatio => Stats.MaxHp > 0 ? (float)Hp / Stats.MaxHp : 0f;

        public UnitState(int id, Side side, UnitStats stats, float x)
        {
            Id = id;
            Side = side;
            Stats = stats;
            X = x;
            Hp = stats.MaxHp;
            Action = UnitAction.Walk;
        }
    }
}
