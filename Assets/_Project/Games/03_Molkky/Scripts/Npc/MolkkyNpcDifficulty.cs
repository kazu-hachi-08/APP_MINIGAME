using UnityEngine;

namespace MiniGame.Molkky
{
    /// <summary>
    /// NPC1段階分の強さ（§9.4）。ブレの大きさと、どこまで賢く狙いを決めるかを持つ。
    /// 強さの差はプレイしながら詰めるため、コードから切り出している。
    /// </summary>
    [CreateAssetMenu(fileName = "MolkkyNpcDifficulty", menuName = "MiniGame/Molkky/Npc Difficulty")]
    public class MolkkyNpcDifficulty : ScriptableObject
    {
        [Tooltip("投擲方向のブレ（±度）")]
        [SerializeField] private float _angleNoise = 4f;

        [Tooltip("初速のブレ（±割合）")]
        [SerializeField] private float _speedNoise = 0.15f;

        [Tooltip("残り点数と同じ数字のピンを狙う（§9.2 ②）。オフだと常に密集地を狙う")]
        [SerializeField] private bool _aimExactPin = true;

        [Tooltip("狙うピンの周りが混んでいて複数本倒しそうなら、残り点数より小さい数字の孤立したピンに切り替える（§9.2 ④）")]
        [SerializeField] private bool _avoidOverflow;

        public float AngleNoise => _angleNoise;
        public float SpeedNoise => _speedNoise;
        public bool AimExactPin => _aimExactPin;
        public bool AvoidOverflow => _avoidOverflow;
    }
}
