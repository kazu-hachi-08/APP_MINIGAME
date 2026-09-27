using UnityEngine;

namespace MiniGame.Golf
{
    /// <summary>
    /// ゴルフの調整値（§8.7）。飛び方・転がり方の手応えはプレイしながら詰めるため、コードから切り出している。
    /// ボールの計算に使う値は、テストしやすいようエンジン非依存の BallPhysicsConfig にまとめて持つ。
    /// </summary>
    [CreateAssetMenu(fileName = "GolfPhysicsSettings", menuName = "MiniGame/Golf/Physics Settings")]
    public class GolfPhysicsSettings : ScriptableObject
    {
        [SerializeField] private BallPhysicsConfig _ball = new BallPhysicsConfig();

        [Tooltip("画面の距離表示用。1ユニット（1タイル）を何ヤードとして見せるか")]
        [SerializeField] private float _yardsPerUnit = 10f;

        public BallPhysicsConfig Ball => _ball;
        public float YardsPerUnit => _yardsPerUnit;
    }
}
