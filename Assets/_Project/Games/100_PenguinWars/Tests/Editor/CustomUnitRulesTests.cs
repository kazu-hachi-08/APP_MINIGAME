using System;
using System.Collections.Generic;
using MiniGame.PenguinWars.Battle;
using NUnit.Framework;

namespace MiniGame.PenguinWars.Battle.Tests
{
    /// <summary>じぶんペンギンのきまり（段階・ポイント・Sanitize・数値への変換）</summary>
    public class CustomUnitRulesTests
    {
        private const int No = CustomUnitRules.LeftNo;
        private const float Tolerance = 0.06f;

        private static CustomUnitDefinition Make(UnitRole role, int cost) => new CustomUnitDefinition { Role = role, Cost = cost };

        private static UnitStats Stats(CustomUnitDefinition def) =>
            UnitStatFormula.Calculate(CustomUnitRules.ToUnitDefinition(def, No));

        // ---- 段階 ----

        [TestCase(UnitRole.Wall, 50, 1)]
        [TestCase(UnitRole.Wall, 80, 1)]
        [TestCase(UnitRole.Wall, 90, 2)]
        [TestCase(UnitRole.Wall, 110, 2)]
        [TestCase(UnitRole.Wall, 120, 3)]
        [TestCase(UnitRole.Wall, 150, 3)]
        [TestCase(UnitRole.Attacker, 200, 1)]
        [TestCase(UnitRole.Attacker, 300, 1)]
        [TestCase(UnitRole.Attacker, 350, 2)]
        [TestCase(UnitRole.Attacker, 450, 2)]
        [TestCase(UnitRole.Attacker, 500, 3)]
        [TestCase(UnitRole.Attacker, 600, 3)]
        [TestCase(UnitRole.Ranged, 400, 1)]
        [TestCase(UnitRole.Ranged, 650, 1)]
        [TestCase(UnitRole.Ranged, 700, 2)]
        [TestCase(UnitRole.Ranged, 900, 2)]
        [TestCase(UnitRole.Ranged, 950, 3)]
        [TestCase(UnitRole.Ranged, 1200, 3)]
        [TestCase(UnitRole.Disruptor, 300, 1)]
        [TestCase(UnitRole.Disruptor, 450, 1)]
        [TestCase(UnitRole.Disruptor, 500, 2)] // ちょうど 1/3 は段階2
        [TestCase(UnitRole.Disruptor, 650, 2)]
        [TestCase(UnitRole.Disruptor, 700, 3)] // ちょうど 2/3 は段階3
        [TestCase(UnitRole.Disruptor, 900, 3)]
        [TestCase(UnitRole.Large, 1500, 1)]
        [TestCase(UnitRole.Large, 2150, 1)]
        [TestCase(UnitRole.Large, 2200, 2)]
        [TestCase(UnitRole.Large, 2800, 2)]
        [TestCase(UnitRole.Large, 2850, 3)]
        [TestCase(UnitRole.Large, 3500, 3)]
        public void Tier_SplitsRoleCostRangeInThirds(UnitRole role, int cost, int expected)
        {
            Assert.AreEqual(expected, CustomUnitRules.Tier(role, cost));
        }

        [Test]
        public void TierTable_PointsSlotsAreaAndUnlocks()
        {
            Assert.AreEqual(new[] { 2, 3, 4 }, new[] { CustomUnitRules.Points(1), CustomUnitRules.Points(2), CustomUnitRules.Points(3) });
            Assert.AreEqual(new[] { 0, 1, 2 }, new[] { CustomUnitRules.AbilitySlots(1), CustomUnitRules.AbilitySlots(2), CustomUnitRules.AbilitySlots(3) });
            Assert.IsFalse(CustomUnitRules.CanUseArea(2));
            Assert.IsTrue(CustomUnitRules.CanUseArea(3));

            Assert.IsTrue(CustomUnitRules.IsStatUnlocked(1, CustomStat.Hp));
            Assert.IsTrue(CustomUnitRules.IsStatUnlocked(1, CustomStat.Attack));
            Assert.IsFalse(CustomUnitRules.IsStatUnlocked(1, CustomStat.Speed));
            Assert.IsTrue(CustomUnitRules.IsStatUnlocked(2, CustomStat.Speed));
            Assert.IsTrue(CustomUnitRules.IsStatUnlocked(2, CustomStat.Cooldown));
            Assert.IsFalse(CustomUnitRules.IsStatUnlocked(2, CustomStat.Range));
            Assert.IsTrue(CustomUnitRules.IsStatUnlocked(3, CustomStat.Range));
        }

        [Test]
        public void SpentPoints_CountsLoweredLevelsAsRefund()
        {
            var levels = new CustomStatLevels { Hp = -1, Speed = 2 };

            Assert.AreEqual(1, CustomUnitRules.SpentPoints(levels));
        }

        // ---- Sanitize ----

        [Test]
        public void Sanitize_TierDrop_ResetsLockedStatsAbilitiesAndArea()
        {
            CustomUnitDefinition def = Make(UnitRole.Attacker, 600);
            def.Levels = new CustomStatLevels { Hp = 1, Range = 1, Speed = 1, Cooldown = 1 };
            def.Abilities.AddRange(new[] { UnitAbilityType.Knockback, UnitAbilityType.Steadfast });
            def.IsAreaAttack = true;
            Assert.IsTrue(CustomUnitRules.IsValid(def));

            def.Cost = 200;
            CustomUnitDefinition fixedDef = CustomUnitRules.Sanitize(def);

            Assert.AreEqual(1, fixedDef.Levels.Hp);
            Assert.AreEqual(0, fixedDef.Levels.Range);
            Assert.AreEqual(0, fixedDef.Levels.Speed);
            Assert.AreEqual(0, fixedDef.Levels.Cooldown);
            Assert.IsEmpty(fixedDef.Abilities);
            Assert.IsFalse(fixedDef.IsAreaAttack);
            Assert.IsTrue(CustomUnitRules.IsValid(fixedDef));
        }

        [Test]
        public void Sanitize_TierDrop_RemovesAbilitiesFromTheBack()
        {
            CustomUnitDefinition def = Make(UnitRole.Attacker, 400);
            def.Abilities.AddRange(new[] { UnitAbilityType.Knockback, UnitAbilityType.Steadfast });

            CustomUnitDefinition fixedDef = CustomUnitRules.Sanitize(def);

            Assert.AreEqual(new[] { UnitAbilityType.Knockback }, fixedDef.Abilities);
        }

        [Test]
        public void Sanitize_OverPoints_ResetsAllLevels()
        {
            CustomUnitDefinition def = Make(UnitRole.Attacker, 200);
            def.Levels = new CustomStatLevels { Hp = 2, Attack = 1 };

            CustomUnitDefinition fixedDef = CustomUnitRules.Sanitize(def);

            Assert.AreEqual(0, CustomUnitRules.SpentPoints(fixedDef.Levels));
            Assert.AreEqual(0, fixedDef.Levels.Hp);
        }

        [Test]
        public void Sanitize_DoesNotModifySource()
        {
            CustomUnitDefinition def = Make(UnitRole.Attacker, 200);
            def.Levels = new CustomStatLevels { Speed = 1 };
            def.Abilities.Add(UnitAbilityType.Knockback);

            CustomUnitRules.Sanitize(def);

            Assert.AreEqual(1, def.Levels.Speed);
            Assert.AreEqual(1, def.Abilities.Count);
        }

        [TestCase(UnitRole.Wall, 10, 50)]
        [TestCase(UnitRole.Wall, 999, 150)]
        [TestCase(UnitRole.Wall, 84, 80)]
        [TestCase(UnitRole.Wall, 85, 90)]
        [TestCase(UnitRole.Attacker, 420, 400)]
        [TestCase(UnitRole.Attacker, 430, 450)]
        [TestCase(UnitRole.Large, 0, 1500)]
        [TestCase(UnitRole.Large, 99999, 3500)]
        public void Sanitize_RoundsCostIntoRangeAndStep(UnitRole role, int cost, int expected)
        {
            Assert.AreEqual(expected, CustomUnitRules.Sanitize(Make(role, cost)).Cost);
        }

        [TestCase(null, CustomUnitRules.DefaultName)]
        [TestCase("   ", CustomUnitRules.DefaultName)]
        [TestCase(" ぺんた ", "ぺんた")]
        [TestCase("あいうえおかきくけこ", "あいうえおかきく")]
        public void Sanitize_FixesName(string name, string expected)
        {
            CustomUnitDefinition def = Make(UnitRole.Wall, 50);
            def.Name = name;

            Assert.AreEqual(expected, CustomUnitRules.Sanitize(def).Name);
        }

        [Test]
        public void Sanitize_AlwaysProducesValidDefinition()
        {
            foreach (CustomUnitDefinition broken in BrokenDefinitions())
            {
                Assert.IsTrue(CustomUnitRules.IsValid(CustomUnitRules.Sanitize(broken)), broken?.ToJson());
            }
        }

        private static IEnumerable<CustomUnitDefinition> BrokenDefinitions()
        {
            yield return null;
            yield return new CustomUnitDefinition { Name = null, Levels = null, Abilities = null };
            yield return new CustomUnitDefinition { Role = (UnitRole)99, Cost = -5 };
            yield return new CustomUnitDefinition
            {
                Role = UnitRole.Large,
                Cost = 3500,
                Levels = new CustomStatLevels { Hp = 9, Attack = 9, Range = -9, Speed = 3, Cooldown = 3 },
                Abilities = new List<UnitAbilityType>
                {
                    UnitAbilityType.Slow, UnitAbilityType.Slow, (UnitAbilityType)77, UnitAbilityType.Freeze, UnitAbilityType.Knockback,
                },
                IsAreaAttack = true,
            };
            yield return new CustomUnitDefinition
            {
                Role = UnitRole.Wall,
                Cost = 55,
                Levels = new CustomStatLevels { Hp = 3, Attack = -2, Speed = -2 },
                IsAreaAttack = true,
                Abilities = new List<UnitAbilityType> { UnitAbilityType.Steadfast },
            };
            // すべてのレベルを最大にしたもの（ポイント超過）
            foreach (UnitRole role in (UnitRole[])Enum.GetValues(typeof(UnitRole)))
            {
                UnitStatFormula.CostRange(role, out _, out int max);
                var levels = new CustomStatLevels();
                foreach (CustomStat stat in CustomStatLevels.AllStats) levels.Set(stat, CustomUnitRules.MaxLevel);
                yield return new CustomUnitDefinition { Role = role, Cost = max, Levels = levels };
            }
        }

        [Test]
        public void SamplePresets_AreValid()
        {
            for (int i = 0; i < CustomUnitPresets.SlotCount; i++)
            {
                Assert.IsTrue(CustomUnitRules.IsValid(CustomUnitPresets.CreateSample(i)), $"枠{i + 1}");
            }
        }

        // ---- 数値への変換 ----

        [Test]
        public void ToUnitDefinition_LevelZero_MatchesFormula()
        {
            CustomUnitDefinition def = Make(UnitRole.Ranged, 800);
            def.Abilities.Add(UnitAbilityType.RangedKiller);

            UnitStats custom = Stats(def);
            UnitStats formula = UnitStatFormula.Calculate(new UnitDefinition(No, "x", UnitRole.Ranged, 800, false,
                new UnitAbility(UnitAbilityType.RangedKiller)));

            Assert.AreEqual(formula.MaxHp, custom.MaxHp);
            Assert.AreEqual(formula.Attack, custom.Attack);
            Assert.AreEqual(formula.Range, custom.Range);
            Assert.AreEqual(formula.MoveSpeed, custom.MoveSpeed);
            Assert.AreEqual(formula.Cooldown, custom.Cooldown);
            Assert.AreEqual(No, custom.UnitNo);
        }

        [Test]
        public void ToUnitDefinition_HpPlusOne_IsAboutTenPercentMore()
        {
            CustomUnitDefinition baseDef = Make(UnitRole.Large, 3000);
            CustomUnitDefinition upDef = baseDef.Clone();
            upDef.Levels.Hp = 1;

            Assert.AreEqual(1.1f, (float)Stats(upDef).MaxHp / Stats(baseDef).MaxHp, Tolerance);
        }

        [Test]
        public void ToUnitDefinition_CooldownPlusOne_IsShorter()
        {
            CustomUnitDefinition baseDef = Make(UnitRole.Attacker, 400);
            CustomUnitDefinition upDef = baseDef.Clone();
            upDef.Levels.Cooldown = 1;

            Assert.Less(Stats(upDef).Cooldown, Stats(baseDef).Cooldown);
        }

        // ---- バランスの目安 ----

        /// <summary>
        /// 素の値（レベル0・能力なし・単体）の体力×火力に対して、ほかを下げて体力・攻撃に全部振っても何倍までか。
        /// 既存キャラの個別倍率の最大（ペンギンタワーの体力1.4倍）と同じくらいに収める。
        /// 既存キャラの最大値とは比べない（大型・妨害は全員が範囲・能力持ちでその分減額されており、レベル0の単体でも超えてしまうため）
        /// </summary>
        private const double MaxPowerRatio = 1.5;

        [Test]
        public void Balance_BestHpAttackBuild_StaysNearFormulaValue()
        {
            foreach (UnitRole role in (UnitRole[])Enum.GetValues(typeof(UnitRole)))
            {
                UnitStatFormula.CostRange(role, out int min, out int max);
                for (int cost = min; cost <= max; cost += CustomUnitRules.CostStep(role))
                {
                    double ratio = Power(BestHpAttackBuild(role, cost)) / Power(Make(role, cost));
                    Assert.LessOrEqual(ratio, MaxPowerRatio, $"{role} コスト{cost}");
                }
            }
        }

        private static double Power(CustomUnitDefinition def)
        {
            UnitStats stats = Stats(def);
            return (double)stats.MaxHp * stats.Attack;
        }

        /// <summary>触れる数値を全部最低にして、浮いたポイントで体力・攻撃を一番高くした組み合わせ</summary>
        private static CustomUnitDefinition BestHpAttackBuild(UnitRole role, int cost)
        {
            int tier = CustomUnitRules.Tier(role, cost);
            CustomUnitDefinition best = Make(role, cost);
            double bestPower = Power(best);
            for (int hp = CustomUnitRules.MinLevel; hp <= CustomUnitRules.MaxLevel; hp++)
            {
                for (int attack = CustomUnitRules.MinLevel; attack <= CustomUnitRules.MaxLevel; attack++)
                {
                    CustomUnitDefinition def = Make(role, cost);
                    foreach (CustomStat stat in new[] { CustomStat.Range, CustomStat.Speed, CustomStat.Cooldown })
                    {
                        if (CustomUnitRules.IsStatUnlocked(tier, stat)) def.Levels.Set(stat, CustomUnitRules.MinLevel);
                    }
                    def.Levels.Hp = hp;
                    def.Levels.Attack = attack;
                    if (!CustomUnitRules.IsValid(def) || Power(def) <= bestPower) continue;

                    best = def;
                    bestPower = Power(def);
                }
            }
            return best;
        }

        [Test]
        public void ToAbility_KnockbackChance_DependsOnRole()
        {
            Assert.AreEqual(0.5f, CustomUnitRules.ToAbility(UnitAbilityType.Knockback, UnitRole.Disruptor).Chance);
            Assert.AreEqual(0.3f, CustomUnitRules.ToAbility(UnitAbilityType.Knockback, UnitRole.Attacker).Chance);
            Assert.AreEqual(2f, CustomUnitRules.ToAbility(UnitAbilityType.Freeze, UnitRole.Wall).Duration);
            Assert.AreEqual(3f, CustomUnitRules.ToAbility(UnitAbilityType.Slow, UnitRole.Wall).Duration);
        }

        // ---- 画面の「コスト N から」 ----

        [TestCase(UnitRole.Wall, 2, 90)]
        [TestCase(UnitRole.Wall, 3, 120)]
        [TestCase(UnitRole.Attacker, 1, 200)]
        [TestCase(UnitRole.Attacker, 2, 350)]
        [TestCase(UnitRole.Attacker, 3, 500)]
        [TestCase(UnitRole.Ranged, 3, 950)]
        public void MinCostForTier_IsFirstCostOfTier(UnitRole role, int tier, int expected)
        {
            int cost = CustomUnitRules.MinCostForTier(role, tier);
            Assert.AreEqual(expected, cost);
            Assert.AreEqual(tier, CustomUnitRules.Tier(role, cost));
        }

        [Test]
        public void UnlockTiers_MatchUnlockChecks()
        {
            foreach (CustomStat stat in CustomStatLevels.AllStats)
            {
                int tier = CustomUnitRules.StatUnlockTier(stat);
                Assert.IsTrue(CustomUnitRules.IsStatUnlocked(tier, stat), stat.ToString());
                Assert.IsFalse(tier > CustomUnitRules.MinTier && CustomUnitRules.IsStatUnlocked(tier - 1, stat), stat.ToString());
            }
            Assert.IsTrue(CustomUnitRules.CanUseArea(CustomUnitRules.AreaUnlockTier));
            Assert.IsFalse(CustomUnitRules.CanUseArea(CustomUnitRules.AreaUnlockTier - 1));
            for (int i = 0; i < CustomUnitRules.MaxAbilitySlots; i++)
            {
                int tier = CustomUnitRules.AbilitySlotUnlockTier(i);
                Assert.Greater(CustomUnitRules.AbilitySlots(tier), i);
                Assert.IsFalse(tier > CustomUnitRules.MinTier && CustomUnitRules.AbilitySlots(tier - 1) > i);
            }
        }
    }
}
