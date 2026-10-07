using System;
using System.Collections.Generic;

namespace MiniGame.PenguinWars.Battle
{
    /// <summary>
    /// 「役割 × コスト」から数値を出す計算式（仕様書 §5.5）。
    /// 50体を1体ずつ手で決めるとコストと強さがずれていくので、「コスト1あたりの体力・火力」を役割ごとに揃え、個性は StatTweak で足す
    /// </summary>
    public static class UnitStatFormula
    {
        /// <summary>範囲攻撃は複数に当たるぶん、1体あたりの火力を下げる</summary>
        private const float AreaDpsMultiplier = 0.75f;
        /// <summary>能力1つごとに体力・火力にかける値（能力のぶん素の強さを削る）</summary>
        private const float AbilityPenalty = 0.9f;
        private const int HpRoundStep = 10;
        private const int MinHp = 10;
        private const int MinAttack = 1;
        private const int Decimals = 2;

        private class RoleProfile
        {
            public int CostMin;
            public int CostMax;
            public float CooldownMin;
            public float CooldownMax;
            public float HpPerCost;
            public float DpsPerCost;
            public float RangeMin;
            public float RangeMax;
            public float MoveSpeed;
            public float AttackInterval;
            public float Windup;
            public int KnockbackCount;
        }

        // 再生産・射程は「役割のコスト帯の中でどの位置か」で Min〜Max を補間する（同じ役割なら高いほど再生産が長く射程が長い）
        private static readonly Dictionary<UnitRole, RoleProfile> Profiles = new Dictionary<UnitRole, RoleProfile>
        {
            [UnitRole.Wall] = new RoleProfile
            {
                CostMin = 50, CostMax = 150, CooldownMin = 2f, CooldownMax = 4f,
                HpPerCost = 2.0f, DpsPerCost = 0.08f, RangeMin = 1.4f, RangeMax = 1.4f,
                MoveSpeed = 1.0f, AttackInterval = 1.2f, Windup = 0.3f, KnockbackCount = 3,
            },
            [UnitRole.Attacker] = new RoleProfile
            {
                CostMin = 200, CostMax = 600, CooldownMin = 6f, CooldownMax = 10f,
                HpPerCost = 0.7f, DpsPerCost = 0.09f, RangeMin = 1.5f, RangeMax = 1.5f,
                MoveSpeed = 1.1f, AttackInterval = 1.3f, Windup = 0.4f, KnockbackCount = 3,
            },
            [UnitRole.Ranged] = new RoleProfile
            {
                CostMin = 400, CostMax = 1200, CooldownMin = 8f, CooldownMax = 20f,
                HpPerCost = 0.3f, DpsPerCost = 0.03f, RangeMin = 3.5f, RangeMax = 5.0f,
                MoveSpeed = 0.8f, AttackInterval = 2.5f, Windup = 0.6f, KnockbackCount = 2,
            },
            [UnitRole.Disruptor] = new RoleProfile
            {
                CostMin = 300, CostMax = 900, CooldownMin = 8f, CooldownMax = 16f,
                HpPerCost = 0.6f, DpsPerCost = 0.03f, RangeMin = 2.2f, RangeMax = 3.0f,
                MoveSpeed = 1.0f, AttackInterval = 2.0f, Windup = 0.4f, KnockbackCount = 3,
            },
            [UnitRole.Large] = new RoleProfile
            {
                CostMin = 1500, CostMax = 3500, CooldownMin = 40f, CooldownMax = 80f,
                HpPerCost = 0.8f, DpsPerCost = 0.04f, RangeMin = 2.0f, RangeMax = 3.0f,
                MoveSpeed = 0.6f, AttackInterval = 3.0f, Windup = 1.0f, KnockbackCount = 2,
            },
        };

        /// <summary>役割のコスト帯（仕様書 §5.5「コスト目安」）。定義表の打ち間違いをテストで見つけるために公開する</summary>
        public static bool IsCostInRoleRange(UnitRole role, int cost)
        {
            RoleProfile profile = Profiles[role];
            return cost >= profile.CostMin && cost <= profile.CostMax;
        }

        /// <summary>役割のコスト帯の下限・上限。じぶんペンギンのコストの段階（CustomUnitRules）も同じ帯で決める</summary>
        public static void CostRange(UnitRole role, out int min, out int max)
        {
            RoleProfile profile = Profiles[role];
            min = profile.CostMin;
            max = profile.CostMax;
        }

        public static UnitStats Calculate(UnitDefinition def)
        {
            RoleProfile profile = Profiles[def.Role];
            StatTweak tweak = def.Tweak;
            float t = CostPosition(profile, def.Cost);
            float abilityFactor = (float)Math.Pow(AbilityPenalty, def.Abilities.Count);
            float areaFactor = def.IsAreaAttack ? AreaDpsMultiplier : 1f;
            float dps = def.Cost * profile.DpsPerCost * abilityFactor * areaFactor * tweak.Attack;

            return new UnitStats
            {
                UnitNo = def.No,
                Role = def.Role,
                Cost = def.Cost,
                Cooldown = Round(Lerp(profile.CooldownMin, profile.CooldownMax, t) * tweak.Cooldown),
                MaxHp = RoundHp(def.Cost * profile.HpPerCost * abilityFactor * tweak.Hp),
                Attack = Math.Max(MinAttack, (int)Math.Round(dps * profile.AttackInterval)),
                Range = Round(Lerp(profile.RangeMin, profile.RangeMax, t) * tweak.Range),
                AttackInterval = profile.AttackInterval,
                Windup = profile.Windup,
                MoveSpeed = Round(profile.MoveSpeed * tweak.Speed),
                IsAreaAttack = def.IsAreaAttack,
                KnockbackCount = profile.KnockbackCount,
                Abilities = def.Abilities,
            };
        }

        /// <summary>コスト帯の下限=0・上限=1。帯の外は端に寄せる</summary>
        private static float CostPosition(RoleProfile profile, int cost)
        {
            if (profile.CostMax <= profile.CostMin) return 0f;

            float t = (float)(cost - profile.CostMin) / (profile.CostMax - profile.CostMin);
            return Math.Max(0f, Math.Min(1f, t));
        }

        private static float Lerp(float a, float b, float t)
        {
            return a + (b - a) * t;
        }

        /// <summary>Inspector で見やすいよう小数2桁にそろえる</summary>
        private static float Round(float value)
        {
            return (float)Math.Round(value, Decimals);
        }

        private static int RoundHp(float value)
        {
            int rounded = (int)Math.Round(value / HpRoundStep) * HpRoundStep;
            return Math.Max(MinHp, rounded);
        }
    }
}
