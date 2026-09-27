using System;
using MiniGame.Common.Online;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace MiniGame.Molkky
{
    /// <summary>
    /// オンライン対戦の送受信（§19.2）。送るのは「投げた瞬間の ThrowRequest」と「静止後の全ピンの状態」の2つだけ。
    ///
    /// 2D物理は端末ごとに結果がずれるため、ThrowRequest は相手端末で演出を再生するためだけに使い、
    /// 得点は投げた側の端末のピン状態で決める。得点ルールは MolkkyRules が純粋関数なので、
    /// 同じピン状態を渡せば両端末で同じ点数になり、点数そのものは送らない。
    /// 卓球と同じく NetworkObject を使わず名前付きメッセージだけで済ませる。
    /// </summary>
    public class MolkkyOnlineLink : MonoBehaviour
    {
        private const string ThrowMessage = "molkky.throw";
        private const string ResultMessage = "molkky.result";

        /// <summary>1メッセージの最大バイト数。結果メッセージ（ピン12本×約17バイト）が収まる大きさ</summary>
        private const int MessageBufferSize = 512;

        public event Action<ThrowRequest> OnThrowReceived;
        public event Action<PinState[]> OnResultReceived;

        private bool _active;

        private static NetworkManager Network => NetworkManager.Singleton;

        /// <summary>接続完了後に呼ぶ。以降、送受信を行う</summary>
        public void Begin()
        {
            var messaging = Network.CustomMessagingManager;
            messaging.RegisterNamedMessageHandler(ThrowMessage, ReceiveThrow);
            messaging.RegisterNamedMessageHandler(ResultMessage, ReceiveResult);
            _active = true;
        }

        private void OnDestroy()
        {
            _active = false;

            var messaging = Network != null ? Network.CustomMessagingManager : null;
            if (messaging == null) return;

            messaging.UnregisterNamedMessageHandler(ThrowMessage);
            messaging.UnregisterNamedMessageHandler(ResultMessage);
        }

        // ---- 送信 ----

        public void SendThrow(ThrowRequest request)
        {
            using var writer = new FastBufferWriter(MessageBufferSize, Allocator.Temp);
            writer.WriteValueSafe(request.PositionX);
            writer.WriteValueSafe(request.AngleDegrees);
            writer.WriteValueSafe(request.Speed);
            writer.WriteValueSafe((int)request.Style);
            Send(ThrowMessage, writer);
        }

        public void SendResult(PinState[] states)
        {
            using var writer = new FastBufferWriter(MessageBufferSize, Allocator.Temp);
            writer.WriteValueSafe(states.Length);
            foreach (PinState state in states)
            {
                writer.WriteValueSafe(state.Position);
                writer.WriteValueSafe(state.IsFallen);
                writer.WriteValueSafe(state.FallDirection);
            }

            Send(ResultMessage, writer);
        }

        /// <summary>投擲 → 結果の順番が入れ替わると困るので、どちらも順序保証ありで送る</summary>
        private void Send(string messageName, FastBufferWriter writer)
        {
            if (!_active || !OnlineSession.TryGetPeerId(out ulong peerId)) return;

            Network.CustomMessagingManager.SendNamedMessage(messageName, peerId, writer, NetworkDelivery.ReliableSequenced);
        }

        // ---- 受信 ----

        private void ReceiveThrow(ulong senderId, FastBufferReader reader)
        {
            reader.ReadValueSafe(out float positionX);
            reader.ReadValueSafe(out float angle);
            reader.ReadValueSafe(out float speed);
            reader.ReadValueSafe(out int style);
            OnThrowReceived?.Invoke(new ThrowRequest(positionX, angle, speed, (ThrowStyle)style));
        }

        private void ReceiveResult(ulong senderId, FastBufferReader reader)
        {
            reader.ReadValueSafe(out int count);
            var states = new PinState[count];
            for (int i = 0; i < count; i++)
            {
                reader.ReadValueSafe(out Vector2 position);
                reader.ReadValueSafe(out bool isFallen);
                reader.ReadValueSafe(out Vector2 fallDirection);
                states[i] = new PinState(position, isFallen, fallDirection);
            }

            OnResultReceived?.Invoke(states);
        }
    }
}
