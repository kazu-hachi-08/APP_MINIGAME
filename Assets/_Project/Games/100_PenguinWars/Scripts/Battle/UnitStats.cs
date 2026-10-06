using System;
using System.Collections.Generic;

namespace MiniGame.PenguinWars.Battle
{
    /// <summary>
    /// 1キャラ分の数値（仕様書 §5.1）。ScriptableObject から変換して渡すことで、戦闘ロジックとテストを Unity のアセットから切り離す
    /// </summary>
    public class UnitStats
    {
        public int UnitNo { get; set; }
        public UnitRole Role { get; set; }
        public int Cost { get; set; }
        public float Cooldown { get; set; }
        public int MaxHp { get; set; }
        public int Attack { get; set; }
        public float Range { get; set; }
        public float AttackInterval { get; set; }
        public float Windup { get; set; }
        public float MoveSpeed { get; set; }
        public bool IsAreaAttack { get; set; }
        /// <summary>倒れるまでにノックバックする回数（仕様書 §5.4）。1 ならノックバックせずに倒れる</summary>
        public int KnockbackCount { get; set; } = 1;
        public IReadOnlyList<UnitAbility> Abilities { get; set; } = Array.Empty<UnitAbility>();

        /// <summary>攻撃が当たってから次の行動までの硬直（Cooldown）の長さ。発生待ち（Windup）の分を攻撃間隔から引く</summary>
        public float RecoveryTime => Math.Max(0f, AttackInterval - Windup);

        public bool HasAbility(UnitAbilityType type)
        {
            return TryGetAbility(type, out _);
        }

        public bool TryGetAbility(UnitAbilityType type, out UnitAbility ability)
        {
            foreach (UnitAbility candidate in Abilities)
            {
                if (candidate.Type != type) continue;

                ability = candidate;
                return true;
            }
            ability = default;
            return false;
        }

        /// <summary>
        /// 体力・攻撃に倍率をかけたコピー（ステージの敵の倍率。EnemySpawnEntry.StatMultiplier）。
        /// コストはそのまま残す（撃破報酬は倍率なしのため。§8.5）。能力リストは書き換えないので共有してよい
        /// </summary>
        public UnitStats Scaled(float multiplier)
        {
            var copy = (UnitStats)MemberwiseClone();
            copy.MaxHp = (int)Math.Round(MaxHp * multiplier);
            copy.Attack = (int)Math.Round(Attack * multiplier);
            return copy;
        }
    }
}
