using System;
using MiniGame.Common.Online;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace MiniGame.TableTennis
{
    /// <summary>相手から届いた打球（自分視点の座標へ反転済み）</summary>
    public struct RemoteShot
    {
        public Vector3 From;
        public Vector3 Velocity;
        public Vector2 Spin;
        public bool IsServe;
        public Vector3 ServeBouncePoint;
        public Vector3 ServeTarget;
        public float ServeForwardSpeed;

        /// <summary>相手が打ってから届くまでにかかった時間（秒）</summary>
        public float Elapsed;
    }

    /// <summary>
    /// 試合中のやり取り（打球・得点・ラケット位置）を送受信する。
    ///
    /// ボールを毎フレーム同期せず「打った瞬間の発射条件」だけを送り、軌道は各端末の BallMotion で再計算する。
    /// 通信量が少なく、遅延があってもボールがカクつかないため。
    ///
    /// 両端末とも自分が手前（z&lt;0）に見えるよう、送受信の境目で左右(x)と奥行き(z)を反転する。
    /// NetworkObject を使わず名前付きメッセージだけで済ませ、Prefab登録などの準備を不要にしている。
    /// </summary>
    public class OnlineMatchLink : MonoBehaviour
    {
        private const string ShotMessage = "tt.shot";
        private const string PointMessage = "tt.point";
        private const string RacketMessage = "tt.racket";
        private const string LoadoutMessage = "tt.loadout";

        /// <summary>1メッセージの最大バイト数。打球メッセージ（約80バイト）が収まる大きさ</summary>
        private const int MessageBufferSize = 128;

        [SerializeField] private RacketController _racket;

        [Tooltip("自分のラケット位置を送る間隔（秒）。表示用なので粗くてよい")]
        [SerializeField] private float _racketSendInterval = 0.05f;

        [Tooltip("遅延補正で早送りする上限（秒）。通信が詰まったときにボールが瞬間移動しすぎないようにする")]
        [SerializeField] private float _maxLatencyCompensation = 0.25f;

        public event Action<RemoteShot> OnShotReceived;

        /// <summary>相手端末が決めた得点（得点者は自分視点へ反転済み）</summary>
        public event Action<CourtSide, PointReason> OnPointReceived;

        /// <summary>相手のラケット位置（自分視点へ反転済み）</summary>
        public event Action<Vector3> OnRacketReceived;

        /// <summary>相手が選んだ選手・ラケット（LoadoutCatalog の番号）</summary>
        public event Action<int, int> OnLoadoutReceived;

        private bool _active;
        private float _racketSendTimer;

        private static NetworkManager Network => NetworkManager.Singleton;

        /// <summary>接続完了後に呼ぶ。以降、送受信を行う</summary>
        public void Begin()
        {
            var messaging = Network.CustomMessagingManager;
            messaging.RegisterNamedMessageHandler(ShotMessage, ReceiveShot);
            messaging.RegisterNamedMessageHandler(PointMessage, ReceivePoint);
            messaging.RegisterNamedMessageHandler(RacketMessage, ReceiveRacket);
            messaging.RegisterNamedMessageHandler(LoadoutMessage, ReceiveLoadout);
            _active = true;
        }

        private void OnDestroy()
        {
            _active = false;

            var messaging = Network != null ? Network.CustomMessagingManager : null;
            if (messaging == null) return;

            messaging.UnregisterNamedMessageHandler(ShotMessage);
            messaging.UnregisterNamedMessageHandler(PointMessage);
            messaging.UnregisterNamedMessageHandler(RacketMessage);
            messaging.UnregisterNamedMessageHandler(LoadoutMessage);
        }

        private void Update()
        {
            if (!_active) return;

            _racketSendTimer -= Time.unscaledDeltaTime;
            if (_racketSendTimer > 0f) return;

            _racketSendTimer = _racketSendInterval;
            SendRacket(_racket.CourtPosition);
        }

        // ---- 送信 ----

        /// <summary>自分の打球を送る。引数は自分視点の座標のまま渡してよい</summary>
        public void SendShot(Vector3 from, ShotResult shot)
        {
            using var writer = new FastBufferWriter(MessageBufferSize, Allocator.Temp);
            writer.WriteValueSafe(Network.ServerTime.Time);
            writer.WriteValueSafe(Mirror(from));
            writer.WriteValueSafe(Mirror(shot.Velocity));
            writer.WriteValueSafe(MirrorSpin(shot.Spin));
            writer.WriteValueSafe(shot.IsServe);
            writer.WriteValueSafe(Mirror(shot.ServeBouncePoint));
            writer.WriteValueSafe(Mirror(shot.ServeTarget));
            writer.WriteValueSafe(shot.ServeForwardSpeed);
            Send(ShotMessage, writer, NetworkDelivery.ReliableSequenced);
        }

        /// <summary>自分の端末で決まった得点を送る。得点者は自分視点のまま渡してよい</summary>
        public void SendPoint(CourtSide scorer, PointReason reason)
        {
            using var writer = new FastBufferWriter(MessageBufferSize, Allocator.Temp);
            writer.WriteValueSafe((int)scorer.Opposite());
            writer.WriteValueSafe((int)reason);
            Send(PointMessage, writer, NetworkDelivery.ReliableSequenced);
        }

        /// <summary>
        /// 自分が選んだ選手・ラケットを送る。相手端末では見た目にだけ使う
        /// （能力は打った側の端末で発射条件に反映済みのため）
        /// </summary>
        public void SendLoadout(int characterIndex, int racketIndex)
        {
            using var writer = new FastBufferWriter(MessageBufferSize, Allocator.Temp);
            writer.WriteValueSafe(characterIndex);
            writer.WriteValueSafe(racketIndex);
            Send(LoadoutMessage, writer, NetworkDelivery.Reliable);
        }

        private void SendRacket(Vector3 courtPosition)
        {
            using var writer = new FastBufferWriter(MessageBufferSize, Allocator.Temp);
            writer.WriteValueSafe(Mirror(courtPosition));

            // 表示用で最新の値だけ届けばよいので、再送しない配信にする
            Send(RacketMessage, writer, NetworkDelivery.Unreliable);
        }

        private void Send(string messageName, FastBufferWriter writer, NetworkDelivery delivery)
        {
            if (!_active || !OnlineSession.TryGetPeerId(out ulong peerId)) return;

            Network.CustomMessagingManager.SendNamedMessage(messageName, peerId, writer, delivery);
        }

        // ---- 受信 ----

        private void ReceiveShot(ulong senderId, FastBufferReader reader)
        {
            reader.ReadValueSafe(out double sentTime);

            var shot = new RemoteShot();
            reader.ReadValueSafe(out shot.From);
            reader.ReadValueSafe(out shot.Velocity);
            reader.ReadValueSafe(out shot.Spin);
            reader.ReadValueSafe(out shot.IsServe);
            reader.ReadValueSafe(out shot.ServeBouncePoint);
            reader.ReadValueSafe(out shot.ServeTarget);
            reader.ReadValueSafe(out shot.ServeForwardSpeed);

            float elapsed = (float)(Network.ServerTime.Time - sentTime);
            shot.Elapsed = Mathf.Clamp(elapsed, 0f, _maxLatencyCompensation);

            OnShotReceived?.Invoke(shot);
        }

        private void ReceivePoint(ulong senderId, FastBufferReader reader)
        {
            reader.ReadValueSafe(out int scorer);
            reader.ReadValueSafe(out int reason);
            OnPointReceived?.Invoke((CourtSide)scorer, (PointReason)reason);
        }

        private void ReceiveRacket(ulong senderId, FastBufferReader reader)
        {
            reader.ReadValueSafe(out Vector3 position);
            OnRacketReceived?.Invoke(position);
        }

        private void ReceiveLoadout(ulong senderId, FastBufferReader reader)
        {
            reader.ReadValueSafe(out int characterIndex);
            reader.ReadValueSafe(out int racketIndex);
            OnLoadoutReceived?.Invoke(characterIndex, racketIndex);
        }

        // ---- 座標の反転 ----

        /// <summary>相手視点のコート座標を自分視点へ（ネットを挟んで点対称）</summary>
        private static Vector3 Mirror(Vector3 v)
        {
            return new Vector3(-v.x, v.y, -v.z);
        }

        /// <summary>左右を反転するとサイドスピンの向きも逆になる。上下の回転はそのまま</summary>
        private static Vector2 MirrorSpin(Vector2 spin)
        {
            return new Vector2(-spin.x, spin.y);
        }
    }
}
