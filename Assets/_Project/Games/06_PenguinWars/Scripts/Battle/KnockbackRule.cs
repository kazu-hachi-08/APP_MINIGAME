namespace MiniGame.PenguinWars.Battle
{
    /// <summary>
    /// ノックバックの判定（仕様書 §5.4）。体力が「最大HP ÷ KnockbackCount」減るごとに1回ノックバックする
    /// </summary>
    public static class KnockbackRule
    {
        /// <summary>ふんばるはどの原因（被ダメージ・ふっとばす・ペンギン砲）でも飛ばされない</summary>
        public static bool CanBeKnockedBack(UnitStats stats)
        {
            return !stats.HasAbility(UnitAbilityType.Steadfast);
        }

        /// <summary>
        /// hpBefore → hpAfter の間にしきい値をまたいだか。1回の攻撃で複数またいでもノックバックは1回（本家と同じ）。
        /// 最後のしきい値（HP 0）は撃破なので含めない
        /// </summary>
        public static bool CrossesThreshold(UnitStats stats, int hpBefore, int hpAfter)
        {
            int count = stats.KnockbackCount;
            for (int i = 1; i < count; i++)
            {
                // 整数で比べて、割り切れない最大HPでも端数で判定がぶれないようにする
                long threshold = (long)stats.MaxHp * (count - i);
                if ((long)hpBefore * count > threshold && (long)hpAfter * count <= threshold) return true;
            }
            return false;
        }
    }
}
