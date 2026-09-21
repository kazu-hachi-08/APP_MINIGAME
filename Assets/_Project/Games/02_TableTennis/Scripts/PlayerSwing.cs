using System;
using UnityEngine;

namespace MiniGame.TableTennis
{
    /// <summary>
    /// フリック入力・ラケット・ボールをつなぐ打球処理。
    /// 当たり判定とタイミング判定は SwingTimingJudge、打球計算は ShotCalculator に任せ、
    /// ここは「フリックを打球として通すかどうか」だけを決める。
    /// </summary>
    public class PlayerSwing : MonoBehaviour
    {
        [SerializeField] private BallMotion _ball;
        [SerializeField] private RacketController _racket;
        [SerializeField] private FlickInput _flickInput;
        [SerializeField] private SwingTimingJudge _timingJudge;
        [SerializeField] private ShotCalculator _shotCalculator;

        /// <summary>打球が成立したときに通知する（進行・HUD表示・SE用）</summary>
        public event Action<FlickData, ShotResult> OnShot;

        /// <summary>振ったが当たらなかったときに通知する</summary>
        public event Action<SwingJudgement> OnMissed;

        /// <summary>打球を受け付けるかどうか。ラリー外やポーズ中は false にする</summary>
        public bool CanSwing { get; set; } = true;

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

            // 自分から離れていくボール（打ち返した直後など）は打てない。
            // サーブのトス（奥行き速度0）は打てるよう、0は許容する
            if (_ball.Velocity.z > 0f) return;

            SwingJudgement judgement = _timingJudge.Judge(_ball.CourtPosition, _racket.CourtPosition);

            // 打球可能範囲に無いフリックは、仕様通り無視する
            if (judgement.Result == SwingResult.OutOfRange) return;

            if (judgement.Result == SwingResult.Miss)
            {
                OnMissed?.Invoke(judgement);
                return;
            }

            ShotResult shot = _shotCalculator.Calculate(flick, judgement);
            _ball.Launch(_ball.CourtPosition, shot.Velocity, shot.Spin);

            OnShot?.Invoke(flick, shot);
        }
    }
}
