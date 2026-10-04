using System;

namespace MiniGame.PenguinWars.Battle
{
    /// <summary>
    /// ペンギン砲のチャージ（仕様書 §4.4）。開始時は 0 から溜まる。
    /// 当てる処理はユニット一覧を持つ BattleWorld が行う
    /// </summary>
    public class CannonState
    {
        public CannonState(float chargeTime)
        {
            ChargeTime = chargeTime;
        }

        public float ChargeTime { get; }
        public float Charge { get; private set; }
        public bool IsReady => Charge >= ChargeTime;
        /// <summary>0=撃った直後、1=撃てる</summary>
        public float ChargeRatio => ChargeTime > 0f ? Charge / ChargeTime : 1f;

        public void Tick(float deltaTime)
        {
            Charge = Math.Min(ChargeTime, Charge + deltaTime);
        }

        public bool TryFire()
        {
            if (!IsReady) return false;

            Charge = 0f;
            return true;
        }
    }
}
