using System;
using UnityEngine;

namespace MiniGame.TableTennis
{
    /// <summary>
    /// フリック入力・ラケット・ボールをつなぐ打球処理。
    /// 当たり判定とタイミング判定は SwingTimingJudge、打球計算は ShotCalculator に任せ、
    /// ここは「フリックを打球として通すか」と「スイングのどの瞬間に当てるか」を決める。
    ///
    /// フリックした瞬間の1フレームだけで判定すると、落ちてくる浮いた球に合わせるのが
    /// ほぼ不可能になるため、フリック後しばらくを「スイング中」として扱い、
    /// その間にボールが打点へ来たら当たるようにしている。
    /// </summary>
    public class PlayerSwing : MonoBehaviour
    {
        [SerializeField] private BallMotion _ball;
        [SerializeField] private RacketController _racket;
        [SerializeField] private FlickInput _flickInput;
        [SerializeField] private SwingTimingJudge _timingJudge;
        [SerializeField] private ShotCalculator _shotCalculator;

        [Header("スイング")]
        [Tooltip("フリック1回のスイングが続く時間（秒）。この間にボールが打点へ来れば当たる")]
        [SerializeField] private float _swingDuration = 0.3f;

        [Header("サーブ")]
        [Tooltip("サーブ（トスを打つとき）の接触範囲の倍率。トスは一瞬しか打てないため広げて当てやすくする")]
        [SerializeField] private float _serveRangeScale = 1.8f;

        /// <summary>打球が成立したときに通知する（進行・HUD表示・SE用）</summary>
        public event Action<FlickData, ShotResult> OnShot;

        /// <summary>振ったが当たらなかったときに通知する</summary>
        public event Action<SwingJudgement> OnMissed;

        /// <summary>打球を受け付けるかどうか。ラリー外やポーズ中は false にする</summary>
        public bool CanSwing { get; set; } = true;

        /// <summary>いまサーブのトスを打つ場面かどうか。true の間だけ接触範囲を広げる</summary>
        public bool IsServing { get; set; }

        private bool _swinging;
        private float _swingEndTime;
        private FlickData _swingFlick;

        /// <summary>このフレームの判定結果。振り終わりに当てるかどうかの判断に使う</summary>
        private SwingJudgement _current;

        /// <summary>スイング中に一度でも振りにいける距離まで来たか。空振りを通知する条件</summary>
        private bool _reachedSwingRange;

        private SwingJudgement _nearestJudgement;

        /// <summary>選手の能力による倍率（接触範囲・スイング時間）</summary>
        private float _reachMultiplier = 1f;
        private float _swingDurationMultiplier = 1f;

        private float SwingDuration => _swingDuration * _swingDurationMultiplier;

        /// <summary>選んだ選手の能力を反映する（試合開始前に呼ぶ）</summary>
        public void SetCharacterMultipliers(float reach, float swingDuration)
        {
            _reachMultiplier = reach;
            _swingDurationMultiplier = swingDuration;
        }

        private void OnEnable()
        {
            _flickInput.OnFlicked += HandleFlick;
        }

        private void OnDisable()
        {
            _flickInput.OnFlicked -= HandleFlick;
        }

        private void HandleFlick(FlickData flick)
        {
            if (!CanSwing || !_ball.IsFlying) return;

            // 打球可能範囲に来る見込みのないフリックは、仕様通り無視する
            if (!IsWorthSwinging()) return;

            _swingFlick = flick;
            _swinging = true;
            _swingEndTime = Time.time + SwingDuration;
            _reachedSwingRange = false;

            // すでに打点へ来ているなら、振り終わりを待たずその場で当てる
            TryContact();
        }

        private void FixedUpdate()
        {
            if (!_swinging) return;

            if (!CanSwing || !_ball.IsFlying)
            {
                EndSwing();
                return;
            }

            if (TryContact()) return;
            if (Time.time < _swingEndTime) return;

            // 振り終わり。まだ接触範囲にボールがいるなら、詰まった打球として当てる
            if (_current.Result == SwingResult.Hit)
            {
                Strike(_current);
                return;
            }

            EndSwing();
        }

        /// <summary>
        /// スイング中の1フレーム分の判定。
        /// Early（まだボールが手前まで来ていない）の間は当てずに待つことで、
        /// 少し早めに振り出しても良いタイミングで当たるようにする。
        /// </summary>
        private bool TryContact()
        {
            _current = default;

            // 自分から離れていくボール（打ち返した直後など）は打てない。
            // サーブのトス（奥行き速度0）は打てるよう、0は許容する
            if (_ball.Velocity.z > 0f) return false;

            _current = Judge(_ball.CourtPosition);

            if (_current.Result != SwingResult.OutOfRange)
            {
                _reachedSwingRange = true;
                _nearestJudgement = _current;
            }

            if (_current.Result != SwingResult.Hit) return false;
            if (_current.Timing == ShotTiming.Early) return false;

            Strike(_current);
            return true;
        }

        /// <summary>
        /// ラケットを運ぶだけのドラッグが打球になってしまわないよう、
        /// スイング中に届く見込みのないボールへのフリックは弾く
        /// </summary>
        private bool IsWorthSwinging()
        {
            if (Judge(_ball.CourtPosition).Result != SwingResult.OutOfRange) return true;

            Vector3 predicted = _ball.CourtPosition + _ball.Velocity * SwingDuration;
            return Judge(predicted).Result != SwingResult.OutOfRange;
        }

        private SwingJudgement Judge(Vector3 ballPosition)
        {
            // 次の打球に乗せた必殺技の強化は、当たる前（判定の時点）から効かせる
            SpecialData special = _shotCalculator.PendingSpecial;
            float specialReach = special != null ? special.ReachMultiplier : 1f;
            float rangeScale = (IsServing ? _serveRangeScale : 1f) * _reachMultiplier * specialReach;
            return _timingJudge.Judge(ballPosition, _racket.CourtPosition, rangeScale);
        }

        private void Strike(SwingJudgement judgement)
        {
            _swinging = false;

            SpecialData special = _shotCalculator.PendingSpecial;
            if (special != null && special.PerfectTiming)
            {
                judgement.Timing = ShotTiming.Good;
                judgement.Quality = 1f;
            }

            ShotResult shot = _shotCalculator.Calculate(_swingFlick, judgement, _ball.CourtPosition, IsServing);

            if (shot.IsServe)
            {
                _ball.LaunchServe(_ball.CourtPosition, shot.ServeBouncePoint, shot.ServeTarget, shot.Spin, shot.ServeForwardSpeed);
            }
            else
            {
                _ball.Launch(_ball.CourtPosition, shot.Velocity, shot.Spin);
            }

            OnShot?.Invoke(_swingFlick, shot);
        }

        private void EndSwing()
        {
            _swinging = false;

            // 振りにいける距離まで来ていたのに当たらなかったときだけ空振りとして扱う
            if (_reachedSwingRange)
            {
                OnMissed?.Invoke(_nearestJudgement);
            }
        }
    }
}
