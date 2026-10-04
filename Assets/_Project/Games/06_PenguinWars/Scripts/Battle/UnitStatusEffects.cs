using System;

namespace MiniGame.PenguinWars.Battle
{
    /// <summary>状態異常の種類。BattleEvent.StatusApplied の Amount に入れる</summary>
    public enum UnitStatusType
    {
        Freeze,
        Slow,
    }

    /// <summary>
    /// 1体が受けている「止まる」「遅い」の残り時間。
    /// 重ねがけは長い方で上書きする（足し算にすると、妨害役を並べたときに永久に止まってしまうため）
    /// </summary>
    public class UnitStatusEffects
    {
        public float FreezeTime { get; private set; }
        public float SlowTime { get; private set; }

        public bool IsFrozen => FreezeTime > 0f;
        public bool IsSlowed => SlowTime > 0f;

        public void Apply(UnitStatusType type, float duration)
        {
            if (type == UnitStatusType.Freeze) FreezeTime = Math.Max(FreezeTime, duration);
            else SlowTime = Math.Max(SlowTime, duration);
        }

        public void Tick(float deltaTime)
        {
            FreezeTime = Math.Max(0f, FreezeTime - deltaTime);
            SlowTime = Math.Max(0f, SlowTime - deltaTime);
        }
    }
}
