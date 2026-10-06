using System;
using System.Collections.Generic;
using System.Text;

namespace MiniGame.PenguinWars.Battle
{
    /// <summary>
    /// じぶんペンギン1体の定義。保存と通信で同じ JSON を使う。
    /// きまりに合っているかはここでは見ない（CustomUnitRules.Sanitize で直す）。
    /// 見た目は Battle から PenguinLook を参照できないので ID 文字列で持つ（空ならパーツなし）
    /// </summary>
    public class CustomUnitDefinition
    {
        private const string NameKey = "name";
        private const string RoleKey = "role";
        private const string CostKey = "cost";
        private const string LevelsKey = "levels";
        private const string AreaKey = "area";
        private const string AbilitiesKey = "abilities";
        private const string BodyKey = "body";
        private const string BodyColorKey = "bodyColor";
        private const string HeadKey = "head";
        private const string HandKey = "hand";
        private const string BackKey = "back";

        public string Name { get; set; } = CustomUnitRules.DefaultName;
        public UnitRole Role { get; set; } = UnitRole.Attacker;
        public int Cost { get; set; }
        public CustomStatLevels Levels { get; set; } = new CustomStatLevels();
        public bool IsAreaAttack { get; set; }
        public List<UnitAbilityType> Abilities { get; set; } = new List<UnitAbilityType>();

        public string Body { get; set; } = CustomUnitRules.DefaultBody;
        public string BodyColor { get; set; } = CustomUnitRules.DefaultBodyColor;
        public string Head { get; set; } = string.Empty;
        public string Hand { get; set; } = string.Empty;
        public string Back { get; set; } = string.Empty;

        public CustomUnitDefinition Clone()
        {
            var copy = (CustomUnitDefinition)MemberwiseClone();
            copy.Levels = (Levels ?? new CustomStatLevels()).Clone();
            copy.Abilities = new List<UnitAbilityType>(Abilities ?? new List<UnitAbilityType>());
            return copy;
        }

        // ---- JSON ----

        public string ToJson()
        {
            var builder = new StringBuilder();
            AppendJson(builder);
            return builder.ToString();
        }

        /// <summary>プリセットの配列の中にそのまま書けるよう StringBuilder に足す</summary>
        public void AppendJson(StringBuilder builder)
        {
            builder.Append('{');
            AppendString(builder, NameKey, Name).Append(',');
            builder.Append(MiniJson.Quote(RoleKey)).Append(':').Append((int)Role).Append(',');
            builder.Append(MiniJson.Quote(CostKey)).Append(':').Append(Cost).Append(',');
            AppendLevels(builder);
            builder.Append(MiniJson.Quote(AreaKey)).Append(':').Append(IsAreaAttack ? "true" : "false").Append(',');
            AppendAbilities(builder);
            AppendString(builder, BodyKey, Body).Append(',');
            AppendString(builder, BodyColorKey, BodyColor).Append(',');
            AppendString(builder, HeadKey, Head).Append(',');
            AppendString(builder, HandKey, Hand).Append(',');
            AppendString(builder, BackKey, Back);
            builder.Append('}');
        }

        private static StringBuilder AppendString(StringBuilder builder, string key, string value)
        {
            return builder.Append(MiniJson.Quote(key)).Append(':').Append(MiniJson.Quote(value ?? string.Empty));
        }

        /// <summary>レベルは CustomStat の順の配列で書く（項目名を5つ並べるより短く、通信でも軽い）</summary>
        private void AppendLevels(StringBuilder builder)
        {
            builder.Append(MiniJson.Quote(LevelsKey)).Append(":[");
            CustomStatLevels levels = Levels ?? new CustomStatLevels();
            for (int i = 0; i < CustomStatLevels.AllStats.Count; i++)
            {
                if (i > 0) builder.Append(',');
                builder.Append(levels.Get(CustomStatLevels.AllStats[i]));
            }
            builder.Append("],");
        }

        private void AppendAbilities(StringBuilder builder)
        {
            builder.Append(MiniJson.Quote(AbilitiesKey)).Append(":[");
            List<UnitAbilityType> abilities = Abilities ?? new List<UnitAbilityType>();
            for (int i = 0; i < abilities.Count; i++)
            {
                if (i > 0) builder.Append(',');
                builder.Append((int)abilities[i]);
            }
            builder.Append("],");
        }

        /// <summary>壊れた文字列なら null（呼ぶ側でお手本に戻す）</summary>
        public static CustomUnitDefinition FromJson(string json)
        {
            try
            {
                return MiniJson.Parse(json) is Dictionary<string, object> root ? FromJsonObject(root) : null;
            }
            catch (FormatException)
            {
                return null;
            }
        }

        /// <summary>型が違う・知らない値の項目は初期値のまま読み飛ばす（古いビルドや手で書き換えたセーブでも落ちないように）</summary>
        public static CustomUnitDefinition FromJsonObject(Dictionary<string, object> root)
        {
            var def = new CustomUnitDefinition();
            def.Name = ReadString(root, NameKey, def.Name);
            if (root.TryGetValue(RoleKey, out object role) && role is double roleValue
                && Enum.IsDefined(typeof(UnitRole), (int)roleValue))
            {
                def.Role = (UnitRole)(int)roleValue;
            }
            if (root.TryGetValue(CostKey, out object cost) && cost is double costValue) def.Cost = (int)costValue;
            if (root.TryGetValue(LevelsKey, out object levels) && levels is List<object> levelItems) ReadLevels(def.Levels, levelItems);
            if (root.TryGetValue(AreaKey, out object area) && area is bool areaValue) def.IsAreaAttack = areaValue;
            if (root.TryGetValue(AbilitiesKey, out object abilities) && abilities is List<object> abilityItems) ReadAbilities(def.Abilities, abilityItems);
            def.Body = ReadString(root, BodyKey, def.Body);
            def.BodyColor = ReadString(root, BodyColorKey, def.BodyColor);
            def.Head = ReadString(root, HeadKey, def.Head);
            def.Hand = ReadString(root, HandKey, def.Hand);
            def.Back = ReadString(root, BackKey, def.Back);
            return def;
        }

        private static string ReadString(Dictionary<string, object> root, string key, string fallback)
        {
            return root.TryGetValue(key, out object value) && value is string text ? text : fallback;
        }

        private static void ReadLevels(CustomStatLevels levels, List<object> items)
        {
            int count = Math.Min(items.Count, CustomStatLevels.AllStats.Count);
            for (int i = 0; i < count; i++)
            {
                if (items[i] is double value) levels.Set(CustomStatLevels.AllStats[i], (int)value);
            }
        }

        private static void ReadAbilities(List<UnitAbilityType> abilities, List<object> items)
        {
            foreach (object item in items)
            {
                if (item is double value && Enum.IsDefined(typeof(UnitAbilityType), (int)value))
                {
                    abilities.Add((UnitAbilityType)(int)value);
                }
            }
        }
    }
}
