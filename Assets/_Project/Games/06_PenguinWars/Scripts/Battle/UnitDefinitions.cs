using System.Collections.Generic;

namespace MiniGame.PenguinWars.Battle
{
    /// <summary>
    /// 全50体の定義表（仕様書 §5.5）。バランス調整はここだけで済むよう、コスト・能力・個別倍率を1か所に集める。
    /// 変えたら Tools > MiniGame > Rebuild PenguinWars でアセットに反映する（見た目は Editor の UnitLooks）
    /// </summary>
    public static class UnitDefinitions
    {
        private const bool Single = false;
        private const bool Area = true;

        // 能力の確率・秒数。役割ごとに強さの目安を揃える（妨害は本業なので高め、アタッカー・大型はおまけ程度）
        private const float SideKnockChance = 0.3f;
        private const float MainKnockChance = 0.5f;
        private const float FreezeChance = 0.4f;
        private const float FreezeSeconds = 2f;
        private const float SideFreezeChance = 0.25f;
        private const float SlowChance = 0.5f;
        private const float SlowSeconds = 3f;

        private static UnitAbility Knock(float chance) => new UnitAbility(UnitAbilityType.Knockback, chance);
        private static UnitAbility Freeze(float chance) => new UnitAbility(UnitAbilityType.Freeze, chance, FreezeSeconds);
        private static UnitAbility Slow() => new UnitAbility(UnitAbilityType.Slow, SlowChance, SlowSeconds);
        private static UnitAbility CastleKiller() => new UnitAbility(UnitAbilityType.CastleKiller);
        private static UnitAbility Steadfast() => new UnitAbility(UnitAbilityType.Steadfast);
        // 役割キラー: 大型は前に出てくるので単体攻撃でも殴れるが、遠距離・妨害は壁の後ろにいるので範囲攻撃のキャラに持たせる
        private static UnitAbility LargeKiller() => new UnitAbility(UnitAbilityType.LargeKiller);
        private static UnitAbility RangedKiller() => new UnitAbility(UnitAbilityType.RangedKiller);
        private static UnitAbility DisruptorKiller() => new UnitAbility(UnitAbilityType.DisruptorKiller);

        private static UnitDefinition Wall(int no, string name, int cost, bool area, params UnitAbility[] abilities) =>
            new UnitDefinition(no, name, UnitRole.Wall, cost, area, abilities);
        private static UnitDefinition Attacker(int no, string name, int cost, bool area, params UnitAbility[] abilities) =>
            new UnitDefinition(no, name, UnitRole.Attacker, cost, area, abilities);
        private static UnitDefinition Ranged(int no, string name, int cost, bool area, params UnitAbility[] abilities) =>
            new UnitDefinition(no, name, UnitRole.Ranged, cost, area, abilities);
        private static UnitDefinition Disruptor(int no, string name, int cost, bool area, params UnitAbility[] abilities) =>
            new UnitDefinition(no, name, UnitRole.Disruptor, cost, area, abilities);
        private static UnitDefinition Large(int no, string name, int cost, bool area, params UnitAbility[] abilities) =>
            new UnitDefinition(no, name, UnitRole.Large, cost, area, abilities);

        public static readonly IReadOnlyList<UnitDefinition> All = new[]
        {
            // 壁（50〜150）
            Wall(1, "ペンギン", 75, Single),
            Wall(2, "かべペンギン", 150, Single),
            Wall(3, "ゆきだまペンギン", 100, Single),
            Wall(4, "ヘルメットペンギン", 150, Single, Steadfast()),
            Wall(5, "こおりペンギン", 120, Single, Slow()),
            Wall(6, "ひなペンギン", 50, Single).With(new StatTweak { Hp = 0.8f, Speed = 1.3f }),
            Wall(7, "ダンボールペンギン", 75, Single),
            Wall(8, "まるまるペンギン", 125, Area),
            Wall(9, "ふとんペンギン", 150, Single, Steadfast()),
            Wall(10, "かまくらペンギン", 150, Single).With(new StatTweak { Hp = 1.3f, Speed = 0.6f }),

            // アタッカー（200〜600）
            Attacker(11, "おのペンギン", 300, Single, LargeKiller()),
            Attacker(12, "さかなけんペンギン", 250, Single),
            Attacker(13, "ボクサーペンギン", 350, Single, Knock(SideKnockChance)),
            Attacker(14, "すもうペンギン", 450, Area, Knock(SideKnockChance)).With(new StatTweak { Hp = 1.2f, Speed = 0.8f }),
            Attacker(15, "ながあしペンギン", 400, Area, RangedKiller()).With(new StatTweak { Range = 1.3f, Speed = 1.2f }),
            Attacker(16, "ハンマーペンギン", 550, Area, DisruptorKiller()).With(new StatTweak { Attack = 1.2f, Speed = 0.8f }),
            Attacker(17, "にんじゃペンギン", 300, Single).With(new StatTweak { Hp = 0.7f, Speed = 1.8f }),
            Attacker(18, "さむらいペンギン", 450, Single).With(new StatTweak { Attack = 1.2f, Hp = 0.9f }),
            Attacker(19, "バイクペンギン", 500, Single, CastleKiller()).With(new StatTweak { Speed = 1.8f }),
            Attacker(20, "ドリルペンギン", 400, Single, CastleKiller()),
            Attacker(21, "ゆうしゃペンギン", 600, Area).With(new StatTweak { Hp = 1.1f }),
            Attacker(22, "ムキムキペンギン", 550, Area).With(new StatTweak { Attack = 1.15f, Speed = 0.9f }),

            // 遠距離（400〜1200）
            Ranged(23, "ゆみペンギン", 450, Single),
            Ranged(24, "ゆきなげペンギン", 600, Area, RangedKiller()),
            Ranged(25, "つりざおペンギン", 500, Single),
            Ranged(26, "のっぽペンギン", 900, Area).With(new StatTweak { Hp = 1.3f }),
            Ranged(27, "たいほうペンギン", 1000, Area).With(new StatTweak { Attack = 1.2f, Speed = 0.8f }),
            Ranged(28, "まほうつかいペンギン", 1100, Area),
            Ranged(29, "スナイパーペンギン", 1200, Single, LargeKiller()).With(new StatTweak { Range = 1.3f, Hp = 0.8f }),
            Ranged(30, "ブーメランペンギン", 700, Area, DisruptorKiller()),
            Ranged(31, "ロケットペンギン", 800, Single, CastleKiller()),
            Ranged(32, "ペンギンタワー", 1200, Area).With(new StatTweak { Hp = 1.4f, Speed = 0.7f }),

            // 妨害（300〜900）
            Disruptor(33, "ふぶきペンギン", 600, Area, Slow()),
            Disruptor(34, "れいとうペンギン", 600, Single, Freeze(FreezeChance)),
            Disruptor(35, "せんぷうきペンギン", 500, Area, Knock(MainKnockChance)),
            Disruptor(36, "ねむりペンギン", 750, Area, Freeze(FreezeChance)),
            Disruptor(37, "ねばねばペンギン", 400, Area, Slow()),
            Disruptor(38, "ハリセンペンギン", 300, Single, Knock(MainKnockChance)),
            Disruptor(39, "アイドルペンギン", 700, Area, Freeze(FreezeChance)),
            Disruptor(40, "タコあしペンギン", 550, Area, Slow()),
            Disruptor(41, "ふうせんペンギン", 450, Single, Knock(MainKnockChance)),
            Disruptor(42, "こおりのじょおうペンギン", 900, Area, Freeze(FreezeChance), Slow()),

            // 大型（1500〜3500）
            Large(43, "きょだいペンギン", 2500, Area, Steadfast()),
            Large(44, "ペンギンロボ", 3000, Area, Steadfast()).With(new StatTweak { Attack = 1.1f }),
            Large(45, "ペンギンせんしゃ", 2800, Area, CastleKiller()).With(new StatTweak { Range = 1.3f }),
            Large(46, "ひょうざんペンギン", 2000, Area, Steadfast()).With(new StatTweak { Hp = 1.3f, Attack = 0.8f }),
            Large(47, "クジラのりペンギン", 2200, Area, Knock(SideKnockChance)).With(new StatTweak { Speed = 1.3f }),
            Large(48, "ペンギンキング", 3500, Area),
            Large(49, "オーロラペンギン", 2600, Area, Slow()),
            Large(50, "ペンギンかみさま", 3500, Area, Knock(SideKnockChance), Freeze(SideFreezeChance)),
        };
    }
}
