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
    /// 打球方向を直接決めるのではなく、相手コートの狙い点へ落ちる初速を逆算する方式にしている。
    /// こうすると「当たれば台に入る」が保証され、フリックは速さとコースの指定に専念できる。
    /// 数値は全て SerializeField にして、プレイしながら調整できるようにしている。
    /// </summary>
    public class ShotCalculator : MonoBehaviour
    {
        [Tooltip("飛行モデルに合わせて初速を逆算するために参照する")]
        [SerializeField] private BallMotion _ball;

        [Header("打球速度（フリック速度から決まる）")]
        [SerializeField] private float _minForwardSpeed = 4.5f;
        [SerializeField] private float _maxForwardSpeed = 6.5f;

        [Header("狙い点（相手コート）")]
        [Tooltip("弱いフリックで落ちる奥行き")]
        [SerializeField] private float _landingDepthNear = 0.5f;

        [Tooltip("強いフリックで落ちる奥行き。台の奥端(1.37)より手前にしておく")]
        [SerializeField] private float _landingDepthFar = 1.15f;

        [Tooltip("ネット際に落ちて引っ掛かるのを防ぐ、狙い点の最短奥行き")]
        [SerializeField] private float _minLandingDepth = 0.25f;

        [Tooltip("横フリックで振れるコースの幅 (m)")]
        [SerializeField] private float _courseSpread = 0.6f;

        [Header("回転量（フリック速度から決まる）")]
        [SerializeField] private float _maxSideSpin = 1.0f;
        [SerializeField] private float _maxTopSpin = 1.0f;

        [Header("タイミング補正（品質0のときの値）")]
        [Tooltip("打球速度の倍率")]
        [SerializeField] private float _worstSpeedScale = 0.75f;

        [Tooltip("回転量の倍率")]
        [SerializeField] private float _worstSpinScale = 0.25f;

        [Tooltip("狙い点に乗る左右の誤差 (m)")]
        [SerializeField] private float _worstCourseError = 0.45f;

        [Tooltip("狙い点に乗る奥行きの誤差 (m)。大きく外すと台外になる")]
        [SerializeField] private float _worstDepthError = 0.35f;

        /// <summary>
        /// フリックと打球タイミングから打球結果を求める。
        /// 上フリック → トップスピン / 下フリック → バックスピン /
        /// 横フリック → その方向へのコース＋サイドスピン、となる。
        /// from は打点（ボールの現在位置）。
        /// </summary>
        public ShotResult Calculate(FlickData flick, SwingJudgement judgement, Vector3 from)
        {
            float strength = flick.Strength;
            float quality = judgement.Quality;
            float error = 1f - quality;

            // タイミングが悪いほど弱く・回転が少なく・狙いからズレる
            float forward = Mathf.Lerp(_minForwardSpeed, _maxForwardSpeed, strength)
                          * Mathf.Lerp(_worstSpeedScale, 1f, quality);

            Vector2 spin = new Vector2(flick.Direction.x * _maxSideSpin, flick.Direction.y * _maxTopSpin)
                         * strength * Mathf.Lerp(_worstSpinScale, 1f, quality);

            var target = new Vector3(
                flick.Direction.x * _courseSpread + Random.Range(-_worstCourseError, _worstCourseError) * error,
                0f,
                Mathf.Max(
                    _minLandingDepth,
                    Mathf.Lerp(_landingDepthNear, _landingDepthFar, strength)
                        + Random.Range(-_worstDepthError, _worstDepthError) * error));

            // 前方への速度で飛行時間が決まり、その時間で狙い点へ落ちる初速を逆算する。
            // 回転による曲がりも込みで解くため、強い回転をかけても狙い通りに飛ぶ
            float flightTime = (target.z - from.z) / Mathf.Max(0.1f, forward);

            return new ShotResult
            {
                Velocity = _ball.SolveLaunchVelocity(from, target, flightTime, spin),
                Spin = spin,
                Strength = strength,
                Timing = judgement.Timing,
                Quality = quality
            };
        }
    }
}
