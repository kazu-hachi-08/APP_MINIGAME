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

        public ShotTiming Timing;

        /// <summary>打球品質 0〜1（タイミング判定の結果）</summary>
        public float Quality;
    }

    /// <summary>
    /// フリック入力を打球（方向・速度）と回転（方向・量）へ変換する、本ゲームの中核計算。
    /// フリック方向をそのまま回転にせず、「打球方向」と「回転方向」へ分配するのが要点。
    /// さらに打球タイミングの品質で、速度・回転量を落とし方向誤差を乗せる。
    /// 数値は全て SerializeField にして、プレイしながら調整できるようにしている。
    /// </summary>
    public class ShotCalculator : MonoBehaviour
    {
        [Header("打球速度（フリック速度から決まる）")]
        [SerializeField] private float _minForwardSpeed = 4.5f;
        [SerializeField] private float _maxForwardSpeed = 6.0f;

        [Header("フリック方向の分配")]
        [Tooltip("横フリックが打球方向（左右）へ回る割合。前方速度に対する比")]
        [SerializeField] private float _lateralSpeedRatio = 0.12f;

        [Tooltip("常に加える打ち上げ角。前方速度に対する比")]
        [SerializeField] private float _baseElevation = 0.30f;

        [Tooltip("縦フリックが打ち上げ角へ回る割合。残りは回転へ回る")]
        [SerializeField] private float _elevationRatio = 0.16f;

        [Header("回転量（フリック速度から決まる）")]
        [SerializeField] private float _maxSideSpin = 1.0f;
        [SerializeField] private float _maxTopSpin = 1.0f;

        [Header("タイミング補正（品質0のときの値）")]
        [Tooltip("打球速度の倍率")]
        [SerializeField] private float _worstSpeedScale = 0.6f;

        [Tooltip("回転量の倍率")]
        [SerializeField] private float _worstSpinScale = 0.25f;

        [Tooltip("左右の打球方向に乗る最大誤差（度）")]
        [SerializeField] private float _worstDirectionError = 20f;

        [Tooltip("打ち上げ角に乗る最大誤差（比率）")]
        [SerializeField] private float _worstElevationError = 0.35f;

        /// <summary>
        /// フリックと打球タイミングから打球結果を求める。
        /// 上フリック → トップスピン / 下フリック → バックスピン /
        /// 横フリック → その方向への打球＋サイドスピン、となる。
        /// </summary>
        public ShotResult Calculate(FlickData flick, SwingJudgement judgement)
        {
            float strength = flick.Strength;
            float quality = judgement.Quality;

            // タイミングが悪いほど弱く・回転が少なく・狙いからズレる
            float forward = Mathf.Lerp(_minForwardSpeed, _maxForwardSpeed, strength)
                          * Mathf.Lerp(_worstSpeedScale, 1f, quality);
            float spinScale = Mathf.Lerp(_worstSpinScale, 1f, quality);
            float error = 1f - quality;

            float lateral = flick.Direction.x * _lateralSpeedRatio * forward;
            float elevation = (_baseElevation + flick.Direction.y * _elevationRatio) * forward
                            * (1f + Random.Range(-_worstElevationError, _worstElevationError) * error);

            // 左右の誤差は速度の大きさを変えないよう、水平面で打球方向を回して与える
            Vector2 horizontal = Rotate(
                new Vector2(lateral, forward),
                Random.Range(-_worstDirectionError, _worstDirectionError) * error);

            return new ShotResult
            {
                Velocity = new Vector3(horizontal.x, elevation, horizontal.y),
                Spin = new Vector2(
                    flick.Direction.x * _maxSideSpin * strength * spinScale,
                    flick.Direction.y * _maxTopSpin * strength * spinScale),
                Strength = strength,
                Timing = judgement.Timing,
                Quality = quality
            };
        }

        private static Vector2 Rotate(Vector2 value, float degrees)
        {
            float radians = degrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(radians);
            float sin = Mathf.Sin(radians);
            return new Vector2(value.x * cos - value.y * sin, value.x * sin + value.y * cos);
        }
    }
}
