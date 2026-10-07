using System;
using System.Collections.Generic;
using System.Text;

namespace MiniGame.PenguinWars.Battle
{
    /// <summary>
    /// じぶんペンギンの3枠と、前回対戦で選んだ枠。
    /// 空の枠は作らない（初めての人でも対戦でそのまま選べるよう、最初からお手本が入っている）
    /// </summary>
    public class CustomUnitPresets
    {
        public const int SlotCount = 3;

        private const int SaveVersion = 1;
        private const string VersionKey = "version";
        private const string LastPickKey = "lastPick";
        private const string SlotsKey = "slots";

        private readonly CustomUnitDefinition[] _slots = new CustomUnitDefinition[SlotCount];
        private int _lastPickedSlot;

        public IReadOnlyList<CustomUnitDefinition> Slots => _slots;

        /// <summary>時間切れのときに使う枠（0〜2）</summary>
        public int LastPickedSlot
        {
            get => _lastPickedSlot;
            set => _lastPickedSlot = Math.Max(0, Math.Min(SlotCount - 1, value));
        }

        private CustomUnitPresets()
        {
        }

        /// <summary>きまりに合わない定義を保存しないよう、入れるときに直す</summary>
        public void SetSlot(int index, CustomUnitDefinition def)
        {
            _slots[index] = CustomUnitRules.Sanitize(def);
        }

        // ---- お手本 ----

        public static CustomUnitPresets CreateDefault()
        {
            var presets = new CustomUnitPresets();
            for (int i = 0; i < SlotCount; i++) presets._slots[i] = CreateSample(i);
            return presets;
        }

        /// <summary>役割・段階がばらけるように選んだお手本（アタッカー段階2・遠距離段階2・大型段階3）</summary>
        public static CustomUnitDefinition CreateSample(int index)
        {
            switch (index)
            {
                case 0:
                    return Sample("じぶんナイト", UnitRole.Attacker, 400, false,
                        new[] { UnitAbilityType.Knockback }, "basic", "standard", "helmet", "sword_shield", "cape_blue",
                        hp: 1, attack: 1, speed: 1);
                case 1:
                    return Sample("じぶんアーチャー", UnitRole.Ranged, 800, false,
                        new[] { UnitAbilityType.RangedKiller }, "tall", "ice", "hood", "bow", string.Empty,
                        attack: 2, cooldown: 1);
                default:
                    return Sample("じぶんまじん", UnitRole.Large, 3000, true,
                        new[] { UnitAbilityType.Steadfast }, "round", "aurora", "crown", "staff", "aurora",
                        hp: 2, attack: 1, range: 1);
            }
        }

        private static CustomUnitDefinition Sample(string name, UnitRole role, int cost, bool area, UnitAbilityType[] abilities,
            string body, string bodyColor, string head, string hand, string back,
            int hp = 0, int attack = 0, int range = 0, int speed = 0, int cooldown = 0)
        {
            return new CustomUnitDefinition
            {
                Name = name,
                Role = role,
                Cost = cost,
                IsAreaAttack = area,
                Abilities = new List<UnitAbilityType>(abilities),
                Levels = new CustomStatLevels { Hp = hp, Attack = attack, Range = range, Speed = speed, Cooldown = cooldown },
                Body = body,
                BodyColor = bodyColor,
                Head = head,
                Hand = hand,
                Back = back,
            };
        }

        // ---- JSON ----

        public string ToJson()
        {
            var builder = new StringBuilder("{");
            builder.Append(MiniJson.Quote(VersionKey)).Append(':').Append(SaveVersion).Append(',');
            builder.Append(MiniJson.Quote(LastPickKey)).Append(':').Append(LastPickedSlot).Append(',');
            builder.Append(MiniJson.Quote(SlotsKey)).Append(":[");
            for (int i = 0; i < SlotCount; i++)
            {
                if (i > 0) builder.Append(',');
                _slots[i].AppendJson(builder);
            }
            return builder.Append("]}").ToString();
        }

        /// <summary>
        /// 空・壊れた文字列や足りない枠はお手本で埋める。読んだ枠は Sanitize する
        /// （きまりの数値を後のビルドで変えても、古いセーブがそのまま対戦に出せるように）
        /// </summary>
        public static CustomUnitPresets FromJson(string json)
        {
            CustomUnitPresets presets = CreateDefault();
            try
            {
                if (MiniJson.Parse(json) is Dictionary<string, object> root) presets.Load(root);
            }
            catch (FormatException)
            {
                return CreateDefault();
            }
            return presets;
        }

        private void Load(Dictionary<string, object> root)
        {
            if (root.TryGetValue(LastPickKey, out object lastPick) && lastPick is double lastPickValue)
            {
                LastPickedSlot = (int)lastPickValue;
            }
            if (!root.TryGetValue(SlotsKey, out object slots) || !(slots is List<object> items)) return;

            int count = Math.Min(items.Count, SlotCount);
            for (int i = 0; i < count; i++)
            {
                if (items[i] is Dictionary<string, object> fields) SetSlot(i, CustomUnitDefinition.FromJsonObject(fields));
            }
        }
    }
}
