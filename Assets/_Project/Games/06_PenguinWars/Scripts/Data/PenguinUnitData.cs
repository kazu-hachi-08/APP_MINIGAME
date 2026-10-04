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
        [Tooltip("役割（仕様書 §5.5）。ランダム編成の「壁2体以上」や大型の確定出現で使う")]
        [SerializeField] private UnitRole _role;
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
        [Tooltip("倒れるまでにノックバックする回数（仕様書 §5.4）。1 ならノックバックせずに倒れる")]
        [SerializeField, Min(1)] private int _knockbackCount = 3;
        [Tooltip("特殊能力（仕様書 §5.3）。複数可")]
        [SerializeField] private PenguinAbilityEntry[] _abilities = new PenguinAbilityEntry[0];
        [Tooltip("見た目のパーツ指定（仕様書 §7.2）。変えたら Rebuild PenguinWars で絵を作り直す")]
        [SerializeField] private PenguinLook _look = new PenguinLook();
        [Header("生成メニューが書き込む（手で触らない）")]
        [SerializeField] private UnitSpriteSet _leftSprites = new UnitSpriteSet();
        [SerializeField] private UnitSpriteSet _rightSprites = new UnitSpriteSet();

        public int No => _no;
        public string DisplayName => _displayName;
        public UnitRole Role => _role;
        public PenguinLook Look => _look;

        public UnitSpriteSet GetSprites(Side side)
        {
            return side == Side.Left ? _leftSprites : _rightSprites;
        }

        /// <summary>戦闘ロジックは ScriptableObject を知らないので、純C#の数値に詰め替えて渡す</summary>
        public UnitStats ToStats()
        {
            return new UnitStats
            {
                UnitNo = _no,
                Role = _role,
                Cost = _cost,
                Cooldown = _cooldown,
                MaxHp = _maxHp,
                Attack = _attack,
                Range = _range,
                AttackInterval = _attackInterval,
                Windup = _windup,
                MoveSpeed = _moveSpeed,
                IsAreaAttack = _isAreaAttack,
                KnockbackCount = _knockbackCount,
                Abilities = ToAbilities(),
            };
        }

        private UnitAbility[] ToAbilities()
        {
            if (_abilities == null) return new UnitAbility[0];

            var abilities = new UnitAbility[_abilities.Length];
            for (int i = 0; i < abilities.Length; i++) abilities[i] = _abilities[i].ToAbility();
            return abilities;
        }
    }
}
