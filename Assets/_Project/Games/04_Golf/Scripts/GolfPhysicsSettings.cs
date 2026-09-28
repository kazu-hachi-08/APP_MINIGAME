using UnityEngine;

namespace MiniGame.Golf
{
    /// <summary>
    /// ゴルフの調整値。飛び方・転がり方の手応えはプレイしながら詰めるため、コードから切り出している。
    /// ボールの計算に使う値は、テストしやすいようエンジン非依存の BallPhysicsConfig にまとめて持つ。
    /// </summary>
    [CreateAssetMenu(fileName = "GolfPhysicsSettings", menuName = "MiniGame/Golf/Physics Settings")]
    public class GolfPhysicsSettings : ScriptableObject
    {
        [SerializeField] private BallPhysicsConfig _ball = new BallPhysicsConfig();

        [Tooltip("画面の距離表示用。1ユニット（1タイル）を何ヤードとして見せるか")]
        [SerializeField] private float _yardsPerUnit = 10f;

        [Header("3タップゲージ。位置は 0＝左端〜1＝右端")]
        [Tooltip("1秒あたりにマーカーが動く量。大きいほど難しい")]
        [SerializeField] private float _gaugeSpeed = 0.8f;

        [Tooltip("インパクトゾーンの中心の位置")]
        [SerializeField] private float _impactZoneCenter = 0.1f;

        [Tooltip("インパクトゾーンの半分の幅")]
        [SerializeField] private float _impactZoneHalfWidth = 0.05f;

        public BallPhysicsConfig Ball => _ball;
        public float YardsPerUnit => _yardsPerUnit;
        public float GaugeSpeed => _gaugeSpeed;
        public float ImpactZoneCenter => _impactZoneCenter;
        public float ImpactZoneHalfWidth => _impactZoneHalfWidth;
    }
}
