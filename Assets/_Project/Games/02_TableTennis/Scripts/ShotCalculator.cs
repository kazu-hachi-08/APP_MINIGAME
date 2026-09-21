using UnityEngine;

namespace MiniGame.TableTennis
{
    /// <summary>打球計算の結果</summary>
    public struct ShotResult
    {
        public Vector3 Velocity;

        /// <summary>x:サイドスピン(+が右) / y:トップスピン(+) ・バックスピン(-)</summary>
        public Vector2 Spin;

        public float Strength;
    }

    /// <summary>
    /// フリック入力を打球（方向・速度）と回転（方向・量）へ変換する、本ゲームの中核計算。
    /// フリック方向をそのまま回転にせず、「打球方向」と「回転方向」へ分配するのが要点。
    /// 数値は全て SerializeField にして、プレイしながら調整できるようにしている。
    /// </summary>
    public class ShotCalculator : MonoBehaviour
    {
        [Header("打球速度（フリック速度から決まる）")]
        [SerializeField] private float _minForwardSpeed = 4.0f;
        [SerializeField] private float _maxForwardSpeed = 8.5f;

        [Header("フリック方向の分配")]
        [Tooltip("横フリックが打球方向（左右）へ回る割合。前方速度に対する比")]
        [SerializeField] private float _lateralSpeedRatio = 0.25f;

        [Tooltip("常に加える打ち上げ角。前方速度に対する比")]
        [SerializeField] private float _baseElevation = 0.28f;

        [Tooltip("縦フリックが打ち上げ角へ回る割合。残りは回転へ回る")]
        [SerializeField] private float _elevationRatio = 0.12f;

        [Header("回転量（フリック速度から決まる）")]
        [SerializeField] private float _maxSideSpin = 1.0f;
        [SerializeField] private float _maxTopSpin = 1.0f;

        /// <summary>
        /// フリックから打球結果を求める。
        /// 上フリック → トップスピン / 下フリック → バックスピン /
        /// 横フリック → その方向への打球＋サイドスピン、となる。
        /// </summary>
        public ShotResult Calculate(FlickData flick)
        {
            float strength = flick.Strength;
            float forward = Mathf.Lerp(_minForwardSpeed, _maxForwardSpeed, strength);

            float lateral = flick.Direction.x * _lateralSpeedRatio * forward;
            float elevation = (_baseElevation + flick.Direction.y * _elevationRatio) * forward;

            return new ShotResult
            {
                Velocity = new Vector3(lateral, elevation, forward),
                Spin = new Vector2(
                    flick.Direction.x * _maxSideSpin * strength,
                    flick.Direction.y * _maxTopSpin * strength),
                Strength = strength
            };
        }
    }
}
