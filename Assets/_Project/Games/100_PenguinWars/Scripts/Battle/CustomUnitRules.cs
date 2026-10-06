using System;
using System.Collections.Generic;

namespace MiniGame.PenguinWars.Battle
{
    /// <summary>
    /// じぶんペンギンのきまり（コストの段階・強化ポイント・触れる数値・能力枠・範囲攻撃）。
    /// ホストとゲストで同じ値でないと Sanitize の結果がずれるので、アセット（端末ごとに違い得る）ではなくここの定数に置く
    /// </summary>
    public static class CustomUnitRules
    {
        /// <summary>対戦でのキャラ No。スナップショットは No を byte で送るので 255 以下にし、既存の 1〜50 と離す</summary>
        public const int LeftNo = 91;
        public const int RightNo = 92;

        public const string DefaultName = "じぶんペンギン";
        /// <summary>編成確認・出撃ボタンで名前が切れない長さ</summary>
        public const int NameMaxLength = 8;
        // PenguinLook.DefaultBody / DefaultBodyColor と同じ値（Battle は Data を参照できない）
        public const string DefaultBody = "basic";
        public const string DefaultBodyColor = "standard";

        public const int MinLevel = -2;
        // +3 まで許すと、ほかを下げて体力・攻撃に回したときに素の値の約1.7倍になって既存キャラより明らかに強くなるため +2 で止める
        public const int MaxLevel = 2;
        /// <summary>強化レベル1つで数値が何割良くなるか</summary>
        public const float LevelStep = 0.1f;

        public const int MinTier = 1;
        public const int MaxTier = 3;
        private const int WallCostStep = 10;
        private const int DefaultCostStep = 50;

        // 段階ごとの値（添字 = 段階 - 1）。コストが高いほど自由度を上げ、安いキャラは役割どおりの素直な性能にとどめる
        private static readonly int[] PointsByTier = { 2, 3, 4 };
        private static readonly int[] AbilitySlotsByTier = { 0, 1, 2 };
        private const int AreaTier = 3;
        private const int SpeedCooldownTier = 2;
        private const int RangeTier = 3;

        public static int CostStep(UnitRole role) => role == UnitRole.Wall ? WallCostStep : DefaultCostStep;

        /// <summary>
        /// 役割のコスト帯の中の位置 t（0〜1）で 1/3 ごとに段階を分ける（役割ごとに帯の幅が違うため、コストの値そのものでは決めない）。
        /// 境目ちょうどで浮動小数の誤差が出ないよう整数で比べる
        /// </summary>
        public static int Tier(UnitRole role, int cost)
        {
            UnitStatFormula.CostRange(role, out int min, out int max);
            int offset = Math.Max(0, Math.Min(max - min, cost - min)) * MaxTier;
            int width = max - min;
            if (offset < width) return MinTier;
            if (offset < width * 2) return MinTier + 1;
            return MaxTier;
        }

        public static int Points(int tier) => PointsByTier[ClampTier(tier) - 1];

        public static int AbilitySlots(int tier) => AbilitySlotsByTier[ClampTier(tier) - 1];

        public static bool CanUseArea(int tier) => tier >= AreaTier;

        public static bool IsStatUnlocked(int tier, CustomStat stat)
        {
            switch (stat)
            {
                case CustomStat.Speed:
                case CustomStat.Cooldown:
                    return tier >= SpeedCooldownTier;
                case CustomStat.Range:
                    return tier >= RangeTier;
                default:
                    return true;
            }
        }

        /// <summary>下げたレベルはマイナスとして数える（「体力を下げて速度に回す」ができるように）</summary>
        public static int SpentPoints(CustomStatLevels levels)
        {
            int total = 0;
            foreach (CustomStat stat in CustomStatLevels.AllStats) total += levels.Get(stat);
            return total;
        }

        private static int ClampTier(int tier) => Math.Max(MinTier, Math.Min(MaxTier, tier));

        // ---- 検証・直す ----

        public static bool IsValid(CustomUnitDefinition def)
        {
            if (def == null || def.Levels == null || def.Abilities == null) return false;
            if (!Enum.IsDefined(typeof(UnitRole), def.Role)) return false;
            if (string.IsNullOrWhiteSpace(def.Name) || def.Name.Length > NameMaxLength) return false;
            if (!IsCostValid(def.Role, def.Cost)) return false;

            int tier = Tier(def.Role, def.Cost);
            return AreLevelsValid(tier, def.Levels) && AreAbilitiesValid(tier, def.Abilities)
                && (!def.IsAreaAttack || CanUseArea(tier));
        }

        private static bool IsCostValid(UnitRole role, int cost)
        {
            UnitStatFormula.CostRange(role, out int min, out _);
            return UnitStatFormula.IsCostInRoleRange(role, cost) && (cost - min) % CostStep(role) == 0;
        }

        private static bool AreLevelsValid(int tier, CustomStatLevels levels)
        {
            foreach (CustomStat stat in CustomStatLevels.AllStats)
            {
                int level = levels.Get(stat);
                if (level < MinLevel || level > MaxLevel) return false;
                if (!IsStatUnlocked(tier, stat) && level != 0) return false;
            }
            return SpentPoints(levels) <= Points(tier);
        }

        private static bool AreAbilitiesValid(int tier, List<UnitAbilityType> abilities)
        {
            if (abilities.Count > AbilitySlots(tier)) return false;

            var seen = new HashSet<UnitAbilityType>();
            foreach (UnitAbilityType type in abilities)
            {
                if (!Enum.IsDefined(typeof(UnitAbilityType), type) || !seen.Add(type)) return false;
            }
            return true;
        }

        /// <summary>
        /// きまりに合わせて直したコピーを返す（元は書き換えない）。コストを下げて段階が下がったときも画面でこれを通す。
        /// 見た目IDは触らない（Battle はパーツの一覧を知らない。知らないパーツは絵の合成側が描かずに進む）
        /// </summary>
        public static CustomUnitDefinition Sanitize(CustomUnitDefinition source)
        {
            CustomUnitDefinition def = source?.Clone() ?? new CustomUnitDefinition();
            def.Name = SanitizeName(def.Name);
            if (!Enum.IsDefined(typeof(UnitRole), def.Role)) def.Role = UnitRole.Attacker;
            def.Cost = SanitizeCost(def.Role, def.Cost);

            int tier = Tier(def.Role, def.Cost);
            SanitizeLevels(tier, def.Levels);
            SanitizeAbilities(tier, def.Abilities);
            if (!CanUseArea(tier)) def.IsAreaAttack = false;
            return def;
        }

        private static string SanitizeName(string name)
        {
            string trimmed = name?.Trim();
            if (string.IsNullOrEmpty(trimmed)) return DefaultName;
            if (trimmed.Length <= NameMaxLength) return trimmed;

            // 絵文字などのサロゲートペアを途中で切ると文字化けするので、その手前で切る
            int length = char.IsHighSurrogate(trimmed[NameMaxLength - 1]) ? NameMaxLength - 1 : NameMaxLength;
            return trimmed.Substring(0, length);
        }

        /// <summary>帯の中に収め、下限から刻みの倍数に丸める</summary>
        private static int SanitizeCost(UnitRole role, int cost)
        {
            UnitStatFormula.CostRange(role, out int min, out int max);
            int step = CostStep(role);
            int clamped = Math.Max(min, Math.Min(max, cost));
            int steps = (int)Math.Round((double)(clamped - min) / step, MidpointRounding.AwayFromZero);
            return Math.Min(max, min + steps * step);
        }

        private static void SanitizeLevels(int tier, CustomStatLevels levels)
        {
            foreach (CustomStat stat in CustomStatLevels.AllStats)
            {
                int level = Math.Max(MinLevel, Math.Min(MaxLevel, levels.Get(stat)));
                levels.Set(stat, IsStatUnlocked(tier, stat) ? level : 0);
            }
            // どれを削るか選ぶと好みが入るので、ポイントを超えたら全部やり直してもらう
            if (SpentPoints(levels) > Points(tier)) levels.Reset();
        }

        /// <summary>知らない能力・重複を外し、枠を超えた分は後ろ（あとから足した方）から外す</summary>
        private static void SanitizeAbilities(int tier, List<UnitAbilityType> abilities)
        {
            var seen = new HashSet<UnitAbilityType>();
            abilities.RemoveAll(type => !Enum.IsDefined(typeof(UnitAbilityType), type) || !seen.Add(type));
            int slots = AbilitySlots(tier);
            if (abilities.Count > slots) abilities.RemoveRange(slots, abilities.Count - slots);
        }

        // ---- 戦闘用への変換 ----

        /// <summary>既存キャラと同じ UnitDefinition にする。UnitStatFormula.Calculate に通せば UnitStats になる</summary>
        public static UnitDefinition ToUnitDefinition(CustomUnitDefinition def, int no)
        {
            var abilities = new UnitAbility[def.Abilities.Count];
            for (int i = 0; i < abilities.Length; i++) abilities[i] = ToAbility(def.Abilities[i], def.Role);

            return new UnitDefinition(no, def.Name, def.Role, def.Cost, def.IsAreaAttack, abilities)
                .With(def.Levels.ToTweak());
        }

        /// <summary>確率・秒数は既存キャラと同じ値。ふっとばすだけは妨害が本業なので確率が上がる</summary>
        public static UnitAbility ToAbility(UnitAbilityType type, UnitRole role)
        {
            switch (type)
            {
                case UnitAbilityType.Knockback:
                    return UnitDefinitions.Knock(role == UnitRole.Disruptor
                        ? UnitDefinitions.MainKnockChance
                        : UnitDefinitions.SideKnockChance);
                case UnitAbilityType.Freeze:
                    return UnitDefinitions.Freeze(UnitDefinitions.FreezeChance);
                case UnitAbilityType.Slow:
                    return UnitDefinitions.Slow();
                default:
                    return new UnitAbility(type);
            }
        }
    }
}
