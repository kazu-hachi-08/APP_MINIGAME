using MiniGame.PenguinWars.Battle;
using UnityEngine;

namespace MiniGame.PenguinWars
{
    /// <summary>
    /// 1キャラ分のデータ（仕様書 §5.1）。1体1アセットにして、2人で別々のキャラを足してもコンフリクトしないようにする
    /// </summary>
    [CreateAssetMenu(fileName = "Unit_000", menuName = "MiniGame/PenguinWars/Unit Data")]
    public class PenguinUnitData : ScriptableObject
    {
        [Tooltip("仕様書 §5.5 のキャラNo。編成やオンライン同期はこの番号で指す")]
        [SerializeField] private int _no;
        [SerializeField] private string _displayName;
        [SerializeField] private int _cost = 100;
        [Tooltip("再生産時間（秒）")]
        [SerializeField] private float _cooldown = 2f;
        [SerializeField] private int _maxHp = 100;
        [SerializeField] private int _attack = 10;
        [Tooltip("自分の位置から前方への射程")]
        [SerializeField] private float _range = 1.4f;
        [SerializeField] private float _attackInterval = 1.2f;
        [Tooltip("攻撃発生までの時間（秒）。AttackInterval より短くする")]
        [SerializeField] private float _windup = 0.3f;
        [SerializeField] private float _moveSpeed = 1f;
        [SerializeField] private bool _isAreaAttack;

        public int No => _no;
        public string DisplayName => _displayName;

        /// <summary>戦闘ロジックは ScriptableObject を知らないので、純C#の数値に詰め替えて渡す</summary>
        public UnitStats ToStats()
        {
            return new UnitStats
            {
                UnitNo = _no,
                Cost = _cost,
                Cooldown = _cooldown,
                MaxHp = _maxHp,
                Attack = _attack,
                Range = _range,
                AttackInterval = _attackInterval,
                Windup = _windup,
                MoveSpeed = _moveSpeed,
                IsAreaAttack = _isAreaAttack,
            };
        }
    }
}
