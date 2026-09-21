using UnityEngine;

namespace MiniGame.TableTennis
{
    /// <summary>フリックした瞬間のボールとラケットの関係</summary>
    public enum SwingResult
    {
        /// <summary>ボールが遠すぎる。フリック入力そのものを無視する</summary>
        OutOfRange,

        /// <summary>振ったが当たらなかった（空振り）</summary>
        Miss,

        /// <summary>打球成立</summary>
        Hit
    }

    /// <summary>打球タイミング</summary>
    public enum ShotTiming
    {
        /// <summary>ボールがまだ遠い＝振るのが早すぎる</summary>
        Early,

        /// <summary>意図した打球が最大限反映される</summary>
        Good,

        /// <summary>ボールが通り過ぎている＝振るのが遅すぎる</summary>
        Late
    }

    public struct SwingJudgement
    {
        public SwingResult Result;

        public ShotTiming Timing;

        /// <summary>打球品質 0（最悪）〜1（ど真ん中）。速度・回転量・方向誤差の補正に使う</summary>
        public float Quality;
    }

    /// <summary>
    /// フリックした瞬間に「当たったか」「タイミングが良かったか」だけを判定する。
    /// 打球計算（ShotCalculator）と分けることで、当たり判定の広さと打球の気持ちよさを別々に調整できる。
    /// </summary>
    public class SwingTimingJudge : MonoBehaviour
    {
        [Header("接触範囲 (m)")]
        [Tooltip("ラケットより手前側で当たる範囲")]
        [SerializeField] private float _contactRangeNear = 0.2f;

        [Tooltip("ラケットより奥側で当たる範囲")]
        [SerializeField] private float _contactRangeFar = 0.55f;

        [SerializeField] private float _contactRadiusX = 0.32f;
        [SerializeField] private float _contactRadiusY = 0.32f;

        [Header("空振り範囲")]
        [Tooltip("接触範囲の何倍まで振りにいくか。この外はフリックを無視し、内側なら空振りになる")]
        [SerializeField] private float _swingRangeScale = 2.2f;

        [Header("タイミング")]
        [Tooltip("最も良いタイミングになる、ラケットからの奥行き距離")]
        [SerializeField] private float _sweetSpotDepth = 0.2f;

        [Tooltip("このズレまでは Good 扱いにする")]
        [SerializeField] private float _goodDepthTolerance = 0.15f;

        [Tooltip("接触位置がラケット中心からズレたときの品質低下の強さ")]
        [Range(0f, 1f)]
        [SerializeField] private float _offsetPenalty = 0.5f;

        /// <summary>ボールとラケットのコート座標から、打球の成否とタイミングを求める</summary>
        public SwingJudgement Judge(Vector3 ball, Vector3 racket)
        {
            float depth = ball.z - racket.z;
            float offsetX = Mathf.Abs(ball.x - racket.x);
            float offsetY = Mathf.Abs(ball.y - racket.y);

            var judgement = new SwingJudgement { Timing = JudgeTiming(depth) };

            if (!IsInside(depth, offsetX, offsetY, _swingRangeScale))
            {
                judgement.Result = SwingResult.OutOfRange;
                return judgement;
            }

            if (!IsInside(depth, offsetX, offsetY, 1f))
            {
                judgement.Result = SwingResult.Miss;
                return judgement;
            }

            judgement.Result = SwingResult.Hit;
            judgement.Quality = CalculateQuality(depth, offsetX, offsetY);
            return judgement;
        }

        private bool IsInside(float depth, float offsetX, float offsetY, float scale)
        {
            return depth >= -_contactRangeNear * scale
                && depth <= _contactRangeFar * scale
                && offsetX <= _contactRadiusX * scale
                && offsetY <= _contactRadiusY * scale;
        }

        private ShotTiming JudgeTiming(float depth)
        {
            float error = depth - _sweetSpotDepth;
            if (error > _goodDepthTolerance) return ShotTiming.Early;
            if (error < -_goodDepthTolerance) return ShotTiming.Late;
            return ShotTiming.Good;
        }

        /// <summary>スイートスポットで1、接触範囲の端で0になる打球品質</summary>
        private float CalculateQuality(float depth, float offsetX, float offsetY)
        {
            float error = depth - _sweetSpotDepth;
            float tolerance = error >= 0f
                ? _contactRangeFar - _sweetSpotDepth
                : _sweetSpotDepth + _contactRangeNear;

            float depthQuality = 1f - Mathf.Clamp01(Mathf.Abs(error) / Mathf.Max(0.0001f, tolerance));

            // 芯を外すほど品質を下げる。奥行きのズレほどは効かせない
            float offsetRatio = Mathf.Clamp01(Mathf.Max(offsetX / _contactRadiusX, offsetY / _contactRadiusY));
            return Mathf.Clamp01(depthQuality * (1f - _offsetPenalty * offsetRatio));
        }
    }
}
