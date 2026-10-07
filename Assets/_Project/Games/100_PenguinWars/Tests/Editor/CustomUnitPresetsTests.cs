using MiniGame.PenguinWars.Battle;
using NUnit.Framework;

namespace MiniGame.PenguinWars.Battle.Tests
{
    /// <summary>じぶんペンギンの3枠の保存形式（JSON）</summary>
    public class CustomUnitPresetsTests
    {
        private static void AssertSameDefinition(CustomUnitDefinition expected, CustomUnitDefinition actual)
        {
            Assert.AreEqual(expected.ToJson(), actual.ToJson());
        }

        [Test]
        public void CreateDefault_FillsAllSlotsWithSamples()
        {
            CustomUnitPresets presets = CustomUnitPresets.CreateDefault();

            Assert.AreEqual(CustomUnitPresets.SlotCount, presets.Slots.Count);
            Assert.AreEqual("じぶんナイト", presets.Slots[0].Name);
            Assert.AreEqual("じぶんアーチャー", presets.Slots[1].Name);
            Assert.AreEqual("じぶんまじん", presets.Slots[2].Name);
            Assert.AreEqual(0, presets.LastPickedSlot);
        }

        [Test]
        public void Json_RoundTrip_KeepsContents()
        {
            CustomUnitPresets presets = CustomUnitPresets.CreateDefault();
            CustomUnitDefinition edited = presets.Slots[1].Clone();
            edited.Name = "ぺん\"た\\";
            edited.Head = string.Empty;
            edited.Levels.Speed = 1;
            edited.Levels.Attack = 1;
            presets.SetSlot(1, edited);
            presets.LastPickedSlot = 2;

            CustomUnitPresets loaded = CustomUnitPresets.FromJson(presets.ToJson());

            Assert.AreEqual(2, loaded.LastPickedSlot);
            for (int i = 0; i < CustomUnitPresets.SlotCount; i++) AssertSameDefinition(presets.Slots[i], loaded.Slots[i]);
            Assert.AreEqual("ぺん\"た\\", loaded.Slots[1].Name);
        }

        [Test]
        public void Definition_JsonRoundTrip_KeepsContents()
        {
            CustomUnitDefinition sample = CustomUnitPresets.CreateSample(2);

            CustomUnitDefinition loaded = CustomUnitDefinition.FromJson(sample.ToJson());

            AssertSameDefinition(sample, loaded);
            Assert.AreEqual(UnitRole.Large, loaded.Role);
            Assert.IsTrue(loaded.IsAreaAttack);
            Assert.AreEqual(2, loaded.Levels.Hp);
            Assert.AreEqual(new[] { UnitAbilityType.Steadfast }, loaded.Abilities);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("{broken")]
        [TestCase("[1,2,3]")]
        [TestCase("{\"slots\":\"x\"}")]
        public void FromJson_Broken_ReturnsSamples(string json)
        {
            CustomUnitPresets loaded = CustomUnitPresets.FromJson(json);
            CustomUnitPresets defaults = CustomUnitPresets.CreateDefault();

            for (int i = 0; i < CustomUnitPresets.SlotCount; i++) AssertSameDefinition(defaults.Slots[i], loaded.Slots[i]);
        }

        [Test]
        public void FromJson_TwoSlots_FillsThirdWithSample()
        {
            CustomUnitPresets presets = CustomUnitPresets.CreateDefault();
            CustomUnitDefinition edited = presets.Slots[0].Clone();
            edited.Name = "ぺんた";
            string json = "{\"version\":1,\"lastPick\":1,\"slots\":[" + edited.ToJson() + "," + presets.Slots[1].ToJson() + "]}";

            CustomUnitPresets loaded = CustomUnitPresets.FromJson(json);

            Assert.AreEqual("ぺんた", loaded.Slots[0].Name);
            AssertSameDefinition(CustomUnitPresets.CreateSample(2), loaded.Slots[2]);
            Assert.AreEqual(1, loaded.LastPickedSlot);
        }

        [Test]
        public void FromJson_RuleBreakingSlot_IsSanitized()
        {
            string json = "{\"slots\":[{\"name\":\"\",\"role\":0,\"cost\":51,\"levels\":[9,9,9,9,9],\"area\":true,\"abilities\":[0,1]}]}";

            CustomUnitPresets loaded = CustomUnitPresets.FromJson(json);

            Assert.IsTrue(CustomUnitRules.IsValid(loaded.Slots[0]));
            Assert.AreEqual(CustomUnitRules.DefaultName, loaded.Slots[0].Name);
        }

        [Test]
        public void LastPickedSlot_IsClamped()
        {
            CustomUnitPresets loaded = CustomUnitPresets.FromJson("{\"lastPick\":9}");

            Assert.AreEqual(CustomUnitPresets.SlotCount - 1, loaded.LastPickedSlot);
        }
    }
}
