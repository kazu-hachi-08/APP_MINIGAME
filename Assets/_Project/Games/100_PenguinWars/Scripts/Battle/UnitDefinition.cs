using System;
using System.Collections.Generic;

namespace MiniGame.PenguinWars.Battle
{
    /// <summary>
    /// 1キャラ分の定義（UnitDefinitions の1行）。細かい数値は持たず、役割とコストから UnitStatFormula が出す
    /// </summary>
    public class UnitDefinition
    {
        public UnitDefinition(int no, string name, UnitRole role, int cost, bool isAreaAttack, params UnitAbility[] abilities)
        {
            No = no;
            Name = name;
            Role = role;
            Cost = cost;
            IsAreaAttack = isAreaAttack;
            Abilities = abilities ?? Array.Empty<UnitAbility>();
        }

        public int No { get; }
        public string Name { get; }
        public UnitRole Role { get; }
        public int Cost { get; }
        public bool IsAreaAttack { get; }
        public IReadOnlyList<UnitAbility> Abilities { get; }
        public StatTweak Tweak { get; private set; } = new StatTweak();

        /// <summary>定義表を1行1体で書けるよう、自分を返す</summary>
        public UnitDefinition With(StatTweak tweak)
        {
            Tweak = tweak;
            return this;
        }
    }
}
