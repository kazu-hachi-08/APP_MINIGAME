using MiniGame.PenguinWars.Battle;
using UnityEngine;

namespace MiniGame.PenguinWars
{
    /// <summary>
    /// オンライン対戦のステージ1つ分（仕様書 §3.4）。ドラフト後にホストが抽選する。
    /// 1ステージ1アセットにして、2人で別々のステージを調整してもぶつからないようにする
    /// </summary>
    [CreateAssetMenu(fileName = "Stage", menuName = "MiniGame/PenguinWars/Stage")]
    public class PenguinStageData : ScriptableObject
    {
        [SerializeField] private string _displayName = "ステージ";
        [Tooltip("戦場の長さ。地面の絵は Balance の FieldLength（30）＋余白までしか敷いていないので、それより長くしない")]
        [SerializeField] private float _fieldLength = 24f;
        [SerializeField] private int _castleHp = 5000;
        [Tooltip("ペンギン砲が届く範囲（自城から戦場の長さのこの割合まで）")]
        [SerializeField, Range(0f, 1f)] private float _cannonRangeRatio = 0.6f;
        [Tooltip("山と地面にかける色。ステージの違いを一目で分かるようにする")]
        [SerializeField] private Color _tint = Color.white;

        [Header("なだれ（間隔 0 ならなし）")]
        [SerializeField] private float _avalancheInterval;
        [SerializeField] private float _avalancheWarningTime = 5f;
        [SerializeField, Range(0f, 1f)] private float _avalancheStartRatio = 0.4f;
        [SerializeField, Range(0f, 1f)] private float _avalancheEndRatio = 0.6f;
        [SerializeField] private int _avalancheDamage = 150;

        public string DisplayName => _displayName;
        public Color Tint => _tint;

        /// <summary>Balance から作った設定のうち、ステージで変わる分だけ上書きする</summary>
        public void ApplyTo(BattleSettings settings)
        {
            settings.FieldLength = _fieldLength;
            settings.LeftCastleHp = _castleHp;
            settings.RightCastleHp = _castleHp;
            settings.CannonRangeRatio = _cannonRangeRatio;
            settings.AvalancheInterval = _avalancheInterval;
            settings.AvalancheWarningTime = _avalancheWarningTime;
            settings.AvalancheStartRatio = _avalancheStartRatio;
            settings.AvalancheEndRatio = _avalancheEndRatio;
            settings.AvalancheDamage = _avalancheDamage;
        }
    }
}
