using System;

namespace MiniGame.PenguinWars.Battle
{
    /// <summary>
    /// 攻撃が当たったときの特殊能力の判定（仕様書 §5.3）。
    /// 乱数は BattleWorld の System.Random を受け取る（シードを固定すればオンライン・テストで結果を再現できるように）
    /// </summary>
    public static class AbilityResolver
    {
        /// <summary>chance 0 なら必ず外れ、1 なら必ず当たり（NextDouble は 0 以上 1 未満のため）</summary>
        public static bool Roll(Random random, float chance)
        {
            return random.NextDouble() < chance;
        }

        /// <summary>城キラーなら城へのダメージに倍率をかける。ユニットへのダメージには使わない</summary>
        public static int CastleDamage(UnitStats attacker, float castleKillerMultiplier)
        {
            if (!attacker.HasAbility(UnitAbilityType.CastleKiller)) return attacker.Attack;

            return (int)Math.Round(attacker.Attack * castleKillerMultiplier);
        }

        /// <summary>役割キラーが狙いの役割を殴ったときだけ倍率をかける。強いキャラにも「合わせれば勝てる」相手を作り、対戦の硬直を崩すため</summary>
        public static int UnitDamage(UnitStats attacker, UnitRole targetRole, float roleKillerMultiplier)
        {
            UnitAbilityType? killer = KillerFor(targetRole);
            if (killer == null || !attacker.HasAbility(killer.Value)) return attacker.Attack;

            return (int)Math.Round(attacker.Attack * roleKillerMultiplier);
        }

        /// <summary>その役割を狙うキラー能力。壁・アタッカーは狙う意味が薄いのでキラーなし（null）</summary>
        private static UnitAbilityType? KillerFor(UnitRole role)
        {
            switch (role)
            {
                case UnitRole.Large: return UnitAbilityType.LargeKiller;
                case UnitRole.Ranged: return UnitAbilityType.RangedKiller;
                case UnitRole.Disruptor: return UnitAbilityType.DisruptorKiller;
                default: return null;
            }
        }
    }
}
