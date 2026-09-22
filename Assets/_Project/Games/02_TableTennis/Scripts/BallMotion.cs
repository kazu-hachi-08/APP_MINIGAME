using System;
using UnityEngine;

namespace MiniGame.TableTennis
{
    /// <summary>ラリーが終わった理由。どちらの得点になるかは RallyReferee が決める</summary>
    public enum RallyEndReason
    {
        /// <summary>ネットに当たった</summary>
        Net,

        /// <summary>台の外に着地した</summary>
        OutOfTable,

        /// <summary>自分が打ち返せずに後ろへ抜けた</summary>
        PastPlayer,

        /// <summary>相手側を抜けた</summary>
        PastOpponent
    }

    /// <summary>
    /// ボールの飛行計算のみを担当する（表示は BallView が行う）。
    /// 現実の物理を再現せず、打球・回転が結果に素直に反映されることを優先した簡易モデル。
    /// </summary>
    public class BallMotion : MonoBehaviour
    {
        /// <summary>初速の逆算で0除算にならないようにする最小の飛行時間</summary>
        private const float MinSolveTime = 0.05f;

        /// <summary>
        /// 下向き加速度の下限。強いバックスピン（カット）でも
        /// ボールが浮いたまま飛び続けないようにするための安全値。
        /// </summary>
        private const float MinFallAcceleration = 1.5f;

        [SerializeField] private TableLayout _table;

        [Header("Flight")]
        [SerializeField] private float _gravity = 9.8f;
        [SerializeField] private float _airDrag = 0.15f;

        [Header("Spin Effect")]
        [Tooltip("回転量1のトップスピンが生む下向き加速度")]
        [SerializeField] private float _topSpinAcceleration = 6f;
        [Tooltip("回転量1のサイドスピンが生む横向き加速度")]
        [SerializeField] private float _sideSpinAcceleration = 3f;

        [Header("Bounce")]
        [SerializeField] private float _bounceRestitution = 0.8f;
        [SerializeField] private float _bounceFriction = 0.95f;

        [Header("Bounce Spin Effect")]
        [Tooltip("回転量1のトップスピンがバウンド後の前進速度を増やす割合")]
        [SerializeField] private float _topSpinBounceForward = 0.35f;

        [Tooltip("回転量1のバックスピンがバウンド後の跳ね上がりを増やす割合")]
        [SerializeField] private float _backSpinBounceHeight = 0.3f;

        [Tooltip("回転量1のサイドスピンがバウンド後に加える横方向速度 (m/s)")]
        [SerializeField] private float _sideSpinBounceKick = 0.7f;

        [Tooltip("バウンドで残る回転の割合")]
        [Range(0f, 1f)]
        [SerializeField] private float _bounceSpinRetention = 0.7f;

        [Header("Out Of Play")]
        [Tooltip("この距離だけ台の端を越えたらラリー終了とみなす")]
        [SerializeField] private float _outMargin = 1.2f;

        /// <summary>x:左右 / y:高さ / z:奥行き</summary>
        public Vector3 CourtPosition { get; private set; }

        public Vector3 Velocity { get; private set; }

        /// <summary>x:サイドスピン(+が右) / y:トップスピン(+) ・バックスピン(-)</summary>
        public Vector2 Spin { get; private set; }

        public bool IsFlying { get; private set; }

        /// <summary>台にバウンドした（引数は着地点）</summary>
        public event Action<Vector3> OnBounced;

        public event Action<RallyEndReason> OnRallyEnded;

        /// <summary>サーブの1バウンド目を待っている間、本来の狙い点へ向け直すための情報</summary>
        private bool _awaitingServeBounce;
        private Vector3 _serveTarget;
        private Vector2 _serveSpin;
        private float _serveForwardSpeed;

        /// <summary>指定の位置・速度・回転でボールを発射する</summary>
        public void Launch(Vector3 courtPosition, Vector3 velocity, Vector2 spin)
        {
            CourtPosition = courtPosition;
            Velocity = velocity;
            Spin = spin;
            IsFlying = true;
        }

        /// <summary>
        /// サーブ専用の発射。実際の卓球と同じく、自分のコートに1回バウンドしてから
        /// 相手コートへ向かうよう、まず自陣の着地点だけを狙って飛ばし、
        /// その1バウンド目の直後（CheckBounce）に本来の狙い点へ向け直す。
        /// </summary>
        public void LaunchServe(Vector3 from, Vector3 ownBouncePoint, Vector3 finalTarget, Vector2 spin, float forwardSpeed)
        {
            float speed = Mathf.Max(0.1f, forwardSpeed);
            float firstLegTime = Mathf.Abs(ownBouncePoint.z - from.z) / speed;
            Launch(from, SolveLaunchVelocity(from, ownBouncePoint, firstLegTime, spin), spin);

            _awaitingServeBounce = true;
            _serveTarget = finalTarget;
            _serveSpin = spin;
            _serveForwardSpeed = speed;
        }

        /// <summary>
        /// 奥行き・高さ・速度はそのままに、左右位置だけ上書きする。
        /// サーブのトス中、ラケットの動きにボールを追従させて空振りを防ぐために使う。
        /// </summary>
        public void FollowX(float x)
        {
            CourtPosition = new Vector3(x, CourtPosition.y, CourtPosition.z);
        }

        /// <summary>その場で止める（ラリー間の待機用）</summary>
        public void Stop()
        {
            Velocity = Vector3.zero;
            Spin = Vector2.zero;
            IsFlying = false;
            _awaitingServeBounce = false;
        }

        private void FixedUpdate()
        {
            if (!IsFlying) return;

            float deltaTime = Time.fixedDeltaTime;
            Vector3 previous = CourtPosition;

            Velocity += AccelerationFor(Spin) * deltaTime;
            Velocity *= Mathf.Max(0f, 1f - _airDrag * deltaTime);
            CourtPosition += Velocity * deltaTime;

            if (CheckNet(previous)) return;
            if (CheckBounce(previous)) return;
            CheckOutOfPlay();
        }

        /// <summary>
        /// ネット面（z=0）をまたいだ瞬間の高さを見て、ネットに当たったか判定する
        /// </summary>
        private bool CheckNet(Vector3 previous)
        {
            bool wasPlayerSide = previous.z <= 0f;
            bool isPlayerSide = CourtPosition.z <= 0f;
            if (wasPlayerSide == isPlayerSide) return false;

            float t = Mathf.InverseLerp(previous.z, CourtPosition.z, 0f);
            float heightAtNet = Mathf.Lerp(previous.y, CourtPosition.y, t);
            float xAtNet = Mathf.Lerp(previous.x, CourtPosition.x, t);

            if (heightAtNet > _table.NetHeight || Mathf.Abs(xAtNet) > _table.NetHalfWidth) return false;

            CourtPosition = new Vector3(xAtNet, heightAtNet, 0f);
            EndRally(RallyEndReason.Net);
            return true;
        }

        /// <summary>
        /// 台の高さ（y=0）を割り込んだらバウンド、台の外なら台外エラーとして扱う
        /// </summary>
        private bool CheckBounce(Vector3 previous)
        {
            if (CourtPosition.y > 0f || Velocity.y >= 0f) return false;

            float t = Mathf.InverseLerp(previous.y, CourtPosition.y, 0f);
            Vector3 contact = Vector3.Lerp(previous, CourtPosition, t);

            if (!_table.IsOnTable(contact.x, contact.z))
            {
                CourtPosition = contact;
                // 自分の後ろに落ちたのは打ち損ない、それ以外は台外エラーとして区別する
                EndRally(contact.z < _table.PlayerEndZ ? RallyEndReason.PastPlayer : RallyEndReason.OutOfTable);
                return true;
            }

            CourtPosition = new Vector3(contact.x, 0f, contact.z);

            // 回転の効果はここで一度に反映する。
            // トップスピンは低く伸び、バックスピンは高く跳ねて失速し、サイドスピンは横へ跳ねる
            float forwardFactor = 1f + Spin.y * _topSpinBounceForward;
            float heightFactor = 1f - Spin.y * _backSpinBounceHeight;

            Velocity = new Vector3(
                Velocity.x * _bounceFriction + Spin.x * _sideSpinBounceKick,
                -Velocity.y * _bounceRestitution * heightFactor,
                Velocity.z * _bounceFriction * forwardFactor);

            // 台とこすれた分だけ回転は落ちる
            Spin *= _bounceSpinRetention;

            OnBounced?.Invoke(CourtPosition);

            // サーブの1バウンド目。ここから本来の狙い点（相手コート）へ向け直す
            if (_awaitingServeBounce)
            {
                _awaitingServeBounce = false;
                float secondLegTime = Mathf.Abs(_serveTarget.z - CourtPosition.z) / _serveForwardSpeed;
                Velocity = SolveLaunchVelocity(CourtPosition, _serveTarget, secondLegTime, _serveSpin);
                Spin = _serveSpin;
            }

            return false;
        }

        private void CheckOutOfPlay()
        {
            if (CourtPosition.z < _table.PlayerEndZ - _outMargin)
            {
                EndRally(RallyEndReason.PastPlayer);
            }
            else if (CourtPosition.z > _table.OpponentEndZ + _outMargin)
            {
                EndRally(RallyEndReason.PastOpponent);
            }
        }

        private void EndRally(RallyEndReason reason)
        {
            IsFlying = false;
            Velocity = Vector3.zero;
            OnRallyEnded?.Invoke(reason);
        }

        /// <summary>
        /// 指定の飛行時間で target（狙い点）へ届く初速を求める。
        /// NPCが狙い通りの返球をできるよう、飛行モデルを持つこのクラス自身が逆算を提供する。
        /// 空気抵抗は厳密に解かず、水平方向の失速分を見込む近似にとどめている。
        /// </summary>
        public Vector3 SolveLaunchVelocity(Vector3 from, Vector3 target, float flightTime, Vector2 spin)
        {
            float time = Mathf.Max(MinSolveTime, flightTime);
            float dragCompensation = 1f + _airDrag * time * 0.5f;
            Vector3 acceleration = AccelerationFor(spin);

            return new Vector3(
                ((target.x - from.x) / time - 0.5f * acceleration.x * time) * dragCompensation,
                (target.y - from.y) / time - 0.5f * acceleration.y * time,
                ((target.z - from.z) / time) * dragCompensation);
        }

        /// <summary>
        /// その初速で飛ばしたとき、奥行き z を通過する瞬間の高さ。ネットを越えるかの確認に使う。
        /// z へ向かっていない場合は判定できないため float.MaxValue を返す。
        /// </summary>
        public float PredictHeightAt(Vector3 from, Vector3 velocity, Vector2 spin, float z)
        {
            if (Mathf.Approximately(velocity.z, 0f)) return float.MaxValue;

            float time = (z - from.z) / velocity.z;
            if (time <= 0f) return float.MaxValue;

            Vector3 acceleration = AccelerationFor(spin);
            return from.y + velocity.y * time + 0.5f * acceleration.y * time * time;
        }

        /// <summary>
        /// 回転から決まる加速度。飛行中は一定として扱うことで、
        /// プレイヤーから見て回転の強さと曲がり方の対応が分かりやすくなる。
        /// </summary>
        private Vector3 AccelerationFor(Vector2 spin)
        {
            return new Vector3(
                spin.x * _sideSpinAcceleration,
                Mathf.Min(-MinFallAcceleration, -_gravity - spin.y * _topSpinAcceleration),
                0f);
        }
    }
}
