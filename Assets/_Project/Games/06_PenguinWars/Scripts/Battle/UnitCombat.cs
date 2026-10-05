using System.Collections.Generic;

namespace MiniGame.PenguinWars.Battle
{
    /// <summary>
    /// 射程判定と攻撃対象の選び方（仕様書 §5.2）。物理演算を使わず X の比較だけで決める（オンラインで結果がずれないように）
    /// </summary>
    public static class UnitCombat
    {
        /// <summary>前方への距離。後ろにいる相手は負になる</summary>
        public static float ForwardDistance(UnitState attacker, float targetX)
        {
            return (targetX - attacker.X) * attacker.Side.Forward();
        }

        public static bool IsInRange(UnitState attacker, float targetX)
        {
            float distance = ForwardDistance(attacker, targetX);
            return distance >= 0f && distance <= attacker.Stats.Range;
        }

        public static bool HasTarget(UnitState attacker, IReadOnlyList<UnitState> units, CastleState enemyCastle)
        {
            if (CanHitCastle(attacker, enemyCastle)) return true;

            foreach (UnitState unit in units)
            {
                if (IsEnemyInRange(attacker, unit)) return true;
            }
            return false;
        }

        /// <summary>
        /// 範囲攻撃は射程内の全員、単体攻撃は一番こちらに近い1体（城を含む）を results に入れる。
        /// 城に当たるなら true を返す（城は UnitState ではないので別にする）
        /// </summary>
        public static bool CollectTargets(UnitState attacker, IReadOnlyList<UnitState> units, CastleState enemyCastle, List<UnitState> results)
        {
            results.Clear();
            bool castleInRange = CanHitCastle(attacker, enemyCastle);

            if (attacker.Stats.IsAreaAttack)
            {
                foreach (UnitState unit in units)
                {
                    if (IsEnemyInRange(attacker, unit)) results.Add(unit);
                }
                return castleInRange;
            }

            UnitState nearest = FindNearestEnemy(attacker, units);
            if (nearest == null) return castleInRange;
            // 同じ距離ならユニットを優先する。城の前で戦っている壁役を無視して城を殴らないように
            if (castleInRange && ForwardDistance(attacker, enemyCastle.X) < ForwardDistance(attacker, nearest.X)) return true;

            results.Add(nearest);
            return false;
        }

        private static UnitState FindNearestEnemy(UnitState attacker, IReadOnlyList<UnitState> units)
        {
            UnitState nearest = null;
            float nearestDistance = float.MaxValue;
            foreach (UnitState unit in units)
            {
                if (!IsEnemyInRange(attacker, unit)) continue;

                float distance = ForwardDistance(attacker, unit.X);
                if (distance >= nearestDistance) continue;

                nearest = unit;
                nearestDistance = distance;
            }
            return nearest;
        }

        private static bool IsEnemyInRange(UnitState attacker, UnitState other)
        {
            return other.Side != attacker.Side && !other.IsDead && IsInRange(attacker, other.X);
        }

        private static bool CanHitCastle(UnitState attacker, CastleState enemyCastle)
        {
            return IsInRange(attacker, enemyCastle.X);
        }
    }
}
