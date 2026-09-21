using System;
using UnityEngine;

namespace MiniGame.TableTennis
{
    /// <summary>得点が入った理由</summary>
    public enum PointReason
    {
        /// <summary>ネットに当たった</summary>
        Net,

        /// <summary>相手コートに入らなかった（台外・自分側バウンド）</summary>
        Out,

        /// <summary>返球できなかった（2バウンド・空振り・見送り）</summary>
        NotReturned
    }

    /// <summary>
    /// ラリー中のボールを見て、どちらに得点が入るかだけを判定する審判役。
    /// 進行管理（TableTennisGameManager）とルール判定を分けることで、
    /// ルールだけを後から差し替え・調整できるようにしている。
    /// </summary>
    public class RallyReferee : MonoBehaviour
    {
        [SerializeField] private BallMotion _ball;

        public event Action<CourtSide, PointReason> OnPointDecided;

        private bool _judging;
        private CourtSide _lastHitter;
        private int _bouncesSinceHit;

        /// <summary>サーブが自分のコートに1回バウンドするのを待っている状態</summary>
        private bool _serveBounceAllowed;

        private void OnEnable()
        {
            _ball.OnBounced += HandleBounced;
            _ball.OnRallyEnded += HandleRallyEnded;
        }

        private void OnDisable()
        {
            _ball.OnBounced -= HandleBounced;
            _ball.OnRallyEnded -= HandleRallyEnded;
        }

        /// <summary>サーブが打たれた時点でラリーの判定を始める</summary>
        public void BeginRally(CourtSide server)
        {
            _judging = true;
            NotifyHit(server);

            // 卓球のルール通り、サーブは自分のコートに1回落ちてよい。
            // 短いサーブがいきなり失点にならないようにするため
            _serveBounceAllowed = true;
        }

        /// <summary>打球が成立したことを伝える（バウンド数がここでリセットされる）</summary>
        public void NotifyHit(CourtSide side)
        {
            _lastHitter = side;
            _bouncesSinceHit = 0;
            _serveBounceAllowed = false;
        }

        /// <summary>ラリー外（サーブ待ち・得点表示中）では判定しない</summary>
        public void Stop()
        {
            _judging = false;
        }

        private void HandleBounced(Vector3 contact)
        {
            if (!_judging) return;

            CourtSide bouncedSide = contact.z < 0f ? CourtSide.Player : CourtSide.Opponent;

            // 打った本人のコートに落ちた ＝ 相手コートへ返せていない
            if (bouncedSide == _lastHitter)
            {
                if (_serveBounceAllowed)
                {
                    _serveBounceAllowed = false;
                    return;
                }

                Award(_lastHitter.Opposite(), PointReason.Out);
                return;
            }

            _serveBounceAllowed = false;
            _bouncesSinceHit++;
            if (_bouncesSinceHit >= 2)
            {
                // 相手コートで2回バウンド ＝ 相手が返球できなかった
                Award(_lastHitter, PointReason.NotReturned);
            }
        }

        private void HandleRallyEnded(RallyEndReason reason)
        {
            if (!_judging) return;

            switch (reason)
            {
                case RallyEndReason.Net:
                    Award(_lastHitter.Opposite(), PointReason.Net);
                    break;
                case RallyEndReason.OutOfTable:
                    Award(_lastHitter.Opposite(), PointReason.Out);
                    break;
                case RallyEndReason.PastPlayer:
                    AwardForPassedSide(CourtSide.Player);
                    break;
                default:
                    AwardForPassedSide(CourtSide.Opponent);
                    break;
            }
        }

        /// <summary>
        /// その側を抜けた場合、いずれも抜かれた側の失点。
        /// 自分で打った球が抜けたなら台外、相手の球なら返球失敗として区別する。
        /// </summary>
        private void AwardForPassedSide(CourtSide passedSide)
        {
            PointReason reason = _lastHitter == passedSide ? PointReason.Out : PointReason.NotReturned;
            Award(passedSide.Opposite(), reason);
        }

        private void Award(CourtSide scorer, PointReason reason)
        {
            _judging = false;
            OnPointDecided?.Invoke(scorer, reason);
        }
    }
}
