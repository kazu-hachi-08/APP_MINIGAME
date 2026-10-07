using System;
using MiniGame.PenguinWars.Battle;
using UnityEngine;

namespace MiniGame.PenguinWars
{
    /// <summary>
    /// インスペクタで編集する特殊能力1つ分。UnitAbility は readonly struct で Unity がシリアライズできないので、ここで持って詰め替える
    /// </summary>
    [Serializable]
    public class PenguinAbilityEntry
    {
        [SerializeField] private UnitAbilityType _type;
        [Tooltip("発動確率（0〜1）。ふっとばす・止める・遅くするで使う")]
        [SerializeField, Range(0f, 1f)] private float _chance;
        [Tooltip("効果の秒数。止める・遅くするで使う")]
        [SerializeField] private float _duration;

        public PenguinAbilityEntry()
        {
        }

        /// <summary>実行時に作るキャラ（じぶんペンギン）用</summary>
        public PenguinAbilityEntry(UnitAbility ability)
        {
            _type = ability.Type;
            _chance = ability.Chance;
            _duration = ability.Duration;
        }

        public UnitAbility ToAbility()
        {
            return new UnitAbility(_type, _chance, _duration);
        }
    }
}
