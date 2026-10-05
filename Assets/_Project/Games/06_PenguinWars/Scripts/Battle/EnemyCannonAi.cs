using System.Collections.Generic;

namespace MiniGame.PenguinWars.Battle
{
    /// <summary>
    /// 敵の城の砲を撃つかどうかの判断。BattleWorld を持たず、ユニット一覧と城の位置だけで決めてテストしやすくする
    /// </summary>
    public static class EnemyCannonAi
    {
        /// <param name="side">砲を撃つ側（ステージでは Right）</param>
        /// <param name="castleX">撃つ側の城の位置</param>
        public static bool ShouldFire(EnemyCannonSettings cannon, bool isReady, IReadOnlyList<UnitState> units, Side side,
            float castleX, float fieldLength)
        {
            if (cannon == null || !isReady) return false;

            float reach = fieldLength * cannon.RangeRatio;
            float danger = fieldLength * cannon.DangerRatio;
            int inRange = 0;
            foreach (UnitState unit in units)
            {
                if (unit.Side == side || unit.IsDead) continue;

                float distance = (unit.X - castleX) * side.Forward();
                if (distance > reach) continue;
                if (distance <= danger) return true;

                inRange++;
            }
            return inRange >= cannon.MinTargets;
        }
    }
}
