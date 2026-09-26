using System;
using UnityEngine;

namespace MiniGame.TableTennis
{
    /// <summary>
    /// 打球の種別。打点の高さとフリック方向・強さから自動で決まる。
    /// プレイヤーが覚える操作を増やさずに打ち分けを増やすため、専用の入力は用意しない。
    /// </summary>
    public enum ShotType
    {
        /// <summary>標準の返球。前進回転で弧を描いて入る</summary>
        Drive,

        /// <summary>高い打点を叩く。速くて低い弧</summary>
        Smash,

        /// <summary>下フリックの下回転。遅く浮いて飛び、バウンド後に失速して跳ね上がる</summary>
        Chop,

        /// <summary>低い打点からそっと上げる逃げ球。高く浮いて体勢を立て直す時間を作る</summary>
        Lob
    }

    /// <summary>
    /// ショット種別ごとの打球パラメータ。
    /// 本ゲームの飛行モデルでは前方速度がそのまま飛行時間＝弧の高さになるため、
    /// 速度を種別ごとに変えるだけで「速くて低い球」「遅くて山なりの球」が作り分けられる。
    /// </summary>
    [Serializable]
    public class ShotProfile
    {
        [Tooltip("前進速度。小さいほど飛行時間が延びて山なりの軌道になる")]
        public float MinForwardSpeed = 4.5f;

        public float MaxForwardSpeed = 6.5f;

        [Tooltip("弱いフリックで落ちる奥行き")]
        public float LandingDepthNear = 0.5f;

        [Tooltip("強いフリックで落ちる奥行き。台の奥端(1.37)より手前にしておく")]
        public float LandingDepthFar = 1.15f;

        [Tooltip("かかる縦回転。+がトップスピン / -がバックスピン")]
        public float TopSpin = 0.7f;

        [Tooltip("横フリックで乗るサイドスピンの最大値")]
        public float SideSpin = 1f;
    }

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

        public ShotType Type;

        /// <summary>サーブかどうか。true のときは Velocity ではなくサーブ用の2段階発射で飛ばす</summary>
        public bool IsServe;

        /// <summary>サーブで自陣に1バウンドさせる狙い点</summary>
        public Vector3 ServeBouncePoint;

        /// <summary>自陣バウンド後に向け直す、本来の相手コートの狙い点</summary>
        public Vector3 ServeTarget;

        public float ServeForwardSpeed;

        /// <summary>この打球に乗った必殺技。乗っていなければ null</summary>
        public SpecialData Special;
    }

    /// <summary>
    /// フリック入力を打球（種別・方向・速度）と回転（方向・量）へ変換する、本ゲームの中核計算。
    /// 打球方向を直接決めるのではなく、相手コートの狙い点へ落ちる初速を逆算する方式にしている。
    /// こうすると「当たれば台に入る」が保証され、フリックは速さとコースの指定に専念できる。
    ///
    /// 逆算は回転込みで解くため、回転の違いは飛行の軌道には出ない。
    /// そこで「打点の高さ×フリック方向」でショット種別を選び、種別ごとに前方速度を変えることで、
    /// スマッシュの速い球やカット・ロブの山なりの球を打ち分けられるようにしている。
    /// 数値は全て SerializeField にして、プレイしながら調整できるようにしている。
    /// </summary>
    public class ShotCalculator : MonoBehaviour
    {
        [Tooltip("飛行モデルに合わせて初速を逆算するために参照する")]
        [SerializeField] private BallMotion _ball;

        [Header("ショット種別の判定")]
        [Tooltip("上下フリックとみなす、フリック方向のY成分のしきい値")]
        [SerializeField] private float _verticalFlickThreshold = 0.35f;

        [Tooltip("この高さ以上の打点で上フリックするとスマッシュになる")]
        [SerializeField] private float _smashContactHeight = 0.45f;

        [Tooltip("低い打点で上フリックしたとき、この強さ未満ならロブになる")]
        [Range(0f, 1f)]
        [SerializeField] private float _lobStrengthThreshold = 0.35f;

        [Header("ショット種別ごとの打球")]
        [SerializeField] private ShotProfile _drive = new ShotProfile();

        [SerializeField]
        private ShotProfile _smash = new ShotProfile
        {
            MinForwardSpeed = 7.5f,
            MaxForwardSpeed = 10f,
            LandingDepthNear = 0.55f,
            LandingDepthFar = 1.2f,
            TopSpin = 1.2f,
            SideSpin = 0.6f
        };

        [SerializeField]
        private ShotProfile _chop = new ShotProfile
        {
            MinForwardSpeed = 2.6f,
            MaxForwardSpeed = 3.8f,
            LandingDepthNear = 0.8f,
            LandingDepthFar = 1.25f,
            TopSpin = -1.2f,
            SideSpin = 1f
        };

        [SerializeField]
        private ShotProfile _lob = new ShotProfile
        {
            MinForwardSpeed = 2f,
            MaxForwardSpeed = 2.8f,
            LandingDepthNear = 0.9f,
            LandingDepthFar = 1.25f,
            TopSpin = -0.4f,
            SideSpin = 0.4f
        };

        [Header("狙い点（相手コート）")]
        [Tooltip("ネット際に落ちて引っ掛かるのを防ぐ、狙い点の最短奥行き")]
        [SerializeField] private float _minLandingDepth = 0.25f;

        [Tooltip("横フリックで振れるコースの幅 (m)")]
        [SerializeField] private float _courseSpread = 0.6f;

        [Header("回転量")]
        [Tooltip("フリックが最も弱いときに残る回転量の割合")]
        [Range(0f, 1f)]
        [SerializeField] private float _minSpinScale = 0.3f;

        [Header("タイミング補正（品質0のときの値）")]
        [Tooltip("打球速度の倍率")]
        [SerializeField] private float _worstSpeedScale = 0.75f;

        [Tooltip("回転量の倍率")]
        [SerializeField] private float _worstSpinScale = 0.25f;

        [Tooltip("狙い点に乗る左右の誤差 (m)")]
        [SerializeField] private float _worstCourseError = 0.45f;

        [Tooltip("狙い点に乗る奥行きの誤差 (m)。大きく外すと台外になる")]
        [SerializeField] private float _worstDepthError = 0.35f;

        [Header("サーブ")]
        [Tooltip("実際の卓球と同じく、サーブは自分のコートに1回バウンドさせる。ネットからこの距離だけ自陣側の地点を1バウンド目の狙い点にする")]
        [SerializeField] private float _serveOwnBounceDepth = 0.6f;

        /// <summary>ラケットの能力による倍率（打球速度・回転量・タイミング誤差）</summary>
        private float _speedMultiplier = 1f;
        private float _spinMultiplier = 1f;
        private float _errorMultiplier = 1f;

        /// <summary>
        /// 次の打球1回に乗せる必殺技。打球の計算で消費される。
        /// 初速の逆算より前に速度と回転を変える必要があるため、打ったあとではなくここで反映する
        /// </summary>
        public SpecialData PendingSpecial { get; set; }

        /// <summary>選んだラケットの能力を反映する（試合開始前に呼ぶ）</summary>
        public void SetRacketMultipliers(float speed, float spin, float error)
        {
            _speedMultiplier = speed;
            _spinMultiplier = spin;
            _errorMultiplier = error;
        }

        /// <summary>
        /// フリックと打球タイミングから打球結果を求める。
        /// 縦フリックと打点の高さがショット種別を、横フリックがコースとサイドスピンを決める。
        /// from は打点（ボールの現在位置）。isServe が true のときは、相手コートへ直接ではなく
        /// 自陣への1バウンドを経由してから狙い点へ向かうサーブとして扱う。
        /// </summary>
        public ShotResult Calculate(FlickData flick, SwingJudgement judgement, Vector3 from, bool isServe = false)
        {
            ShotType type = SelectType(flick, from.y);
            ShotProfile profile = ProfileFor(type);

            float strength = flick.Strength;
            float quality = judgement.Quality;
            // ラケットの「ミスしやすさ」は、タイミングが悪いときのズレ幅として効かせる
            float error = (1f - quality) * _errorMultiplier;

            // タイミングが悪いほど弱く・回転が少なく・狙いからズレる
            float forward = Mathf.Lerp(profile.MinForwardSpeed, profile.MaxForwardSpeed, strength)
                          * Mathf.Lerp(_worstSpeedScale, 1f, quality)
                          * _speedMultiplier;

            // 縦回転の向きはショット種別が決め（カットなら必ず下回転）、量だけフリックの強さで変わる。
            // 横回転はフリックの左右がそのまま乗る
            float spinScale = Mathf.Lerp(_minSpinScale, 1f, strength) * Mathf.Lerp(_worstSpinScale, 1f, quality)
                            * _spinMultiplier;
            Vector2 spin = new Vector2(flick.Direction.x * profile.SideSpin, profile.TopSpin) * spinScale;

            SpecialData special = PendingSpecial;
            PendingSpecial = null;
            if (special != null)
            {
                forward *= special.SpeedMultiplier;
                spin = special.ApplySpin(spin);
            }

            var target = new Vector3(
                flick.Direction.x * _courseSpread + UnityEngine.Random.Range(-_worstCourseError, _worstCourseError) * error,
                0f,
                Mathf.Max(
                    _minLandingDepth,
                    Mathf.Lerp(profile.LandingDepthNear, profile.LandingDepthFar, strength)
                        + UnityEngine.Random.Range(-_worstDepthError, _worstDepthError) * error));

            if (isServe)
            {
                // ネットを越える弧は BallMotion が1バウンド目の直後に保証するため、速度はそのまま使う。
                // サーブは相手コートへ直接ではなく、まず自陣への1バウンドを狙う。
                // 実際の向け直しは BallMotion.LaunchServe が1バウンド目の直後に行う
                var ownBounce = new Vector3(target.x, 0f, -_serveOwnBounceDepth);

                return new ShotResult
                {
                    Spin = spin,
                    Strength = strength,
                    Timing = judgement.Timing,
                    Quality = quality,
                    Type = type,
                    IsServe = true,
                    ServeBouncePoint = ownBounce,
                    ServeTarget = target,
                    ServeForwardSpeed = forward,
                    Special = special
                };
            }

            // 前方への速度で飛行時間が決まり、その時間で狙い点へ落ちる初速を逆算する。
            // 種別ごとに前方速度が違うので、カットやロブは自然と山なりの軌道になる
            float flightTime = (target.z - from.z) / Mathf.Max(0.1f, forward);

            return new ShotResult
            {
                Velocity = _ball.SolveLaunchVelocity(from, target, flightTime, spin),
                Spin = spin,
                Strength = strength,
                Timing = judgement.Timing,
                Quality = quality,
                Type = type,
                Special = special
            };
        }

        /// <summary>
        /// 下フリックはカット、上フリックは打点が高ければスマッシュ、
        /// 低い打点でそっと上げたならロブ、それ以外はドライブ。
        /// </summary>
        private ShotType SelectType(FlickData flick, float contactHeight)
        {
            if (flick.Direction.y < -_verticalFlickThreshold) return ShotType.Chop;
            if (flick.Direction.y <= _verticalFlickThreshold) return ShotType.Drive;
            if (contactHeight >= _smashContactHeight) return ShotType.Smash;

            return flick.Strength < _lobStrengthThreshold ? ShotType.Lob : ShotType.Drive;
        }

        private ShotProfile ProfileFor(ShotType type)
        {
            switch (type)
            {
                case ShotType.Smash: return _smash;
                case ShotType.Chop: return _chop;
                case ShotType.Lob: return _lob;
                default: return _drive;
            }
        }
    }
}
