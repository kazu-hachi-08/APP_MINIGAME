using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace MiniGame.Molkky
{
    /// <summary>
    /// オンライン対戦の送受信。送るのは「投げた瞬間の ThrowRequest ＋ 投げる直前の全ピンの状態」と
    /// 「静止後の全ピンの状態」の2つだけ。
    ///
    /// 相手端末は ThrowRequest で物理を再生して見せる。投げる直前のピン状態も一緒に送るのは、
    /// 手番の合間に端末ごとにピン位置が少しずつずれ、再生で倒れるピンが投げた側と食い違うのを防ぐため。
    /// それでも物理は端末ごとにずれうるので、得点は投げた側の端末のピン状態で決める。得点ルールは MolkkyRules が純粋関数なので、
    /// 同じピン状態を渡せば全端末で同じ点数になり、点数そのものは送らない。
    /// キャラ選択では「席番号＋キャラ番号」だけを送る。見た目・能力は各端末のカタログから引く。
    /// 卓球と同じく NetworkObject を使わず名前付きメッセージだけで済ませる。
    ///
    /// NGOではクライアント同士が直接送れないため、2〜4人ともホスト中継にする。
    /// クライアントはホストへ送り、ホストは受け取った内容を送り主以外の全クライアントへ転送する。
    /// </summary>
    public class MolkkyOnlineLink : MonoBehaviour
    {
        private const string ThrowMessage = "molkky.throw";
        private const string ResultMessage = "molkky.result";
        private const string CharacterMessage = "molkky.character";

        /// <summary>1メッセージの最大バイト数。投擲メッセージ（約20バイト＋ピン12本×約17バイト）が収まる大きさ</summary>
        private const int MessageBufferSize = 512;

        /// <summary>投擲 → 結果の順番が入れ替わると困るので、どちらも順序保証ありで送る</summary>
        private const NetworkDelivery Delivery = NetworkDelivery.ReliableSequenced;

        /// <summary>引数は投擲内容と、投げる直前の全ピンの状態</summary>
        public event Action<ThrowRequest, PinState[]> OnThrowReceived;
        public event Action<PinState[]> OnResultReceived;

        /// <summary>引数は席番号とキャラ番号</summary>
        public event Action<int, int> OnCharacterReceived;

        private readonly List<ulong> _recipients = new List<ulong>();
        private bool _active;

        private static NetworkManager Network => NetworkManager.Singleton;

        /// <summary>接続完了後に呼ぶ。以降、送受信を行う</summary>
        public void Begin()
        {
            var messaging = Network.CustomMessagingManager;
            messaging.RegisterNamedMessageHandler(ThrowMessage, ReceiveThrow);
            messaging.RegisterNamedMessageHandler(ResultMessage, ReceiveResult);
            messaging.RegisterNamedMessageHandler(CharacterMessage, ReceiveCharacter);
            _active = true;
        }

        private void OnDestroy()
        {
            _active = false;

            var messaging = Network != null ? Network.CustomMessagingManager : null;
            if (messaging == null) return;

            messaging.UnregisterNamedMessageHandler(ThrowMessage);
            messaging.UnregisterNamedMessageHandler(ResultMessage);
            messaging.UnregisterNamedMessageHandler(CharacterMessage);
        }

        // ---- 送信 ----

        public void SendThrow(ThrowRequest request, PinState[] startStates)
        {
            SendThrow(request, startStates, Network.LocalClientId);
        }

        public void SendResult(PinState[] states)
        {
            SendResult(states, Network.LocalClientId);
        }

        public void SendCharacter(int seat, int characterIndex)
        {
            SendCharacter(seat, characterIndex, Network.LocalClientId);
        }

        /// <param name="originId">投げた人の端末。ホストが中継するときに送り返さないため</param>
        private void SendThrow(ThrowRequest request, PinState[] startStates, ulong originId)
        {
            using var writer = new FastBufferWriter(MessageBufferSize, Allocator.Temp);
            WriteRequest(writer, request);
            WriteStates(writer, startStates);
            Send(ThrowMessage, writer, originId);
        }

        private void SendResult(PinState[] states, ulong originId)
        {
            using var writer = new FastBufferWriter(MessageBufferSize, Allocator.Temp);
            WriteStates(writer, states);
            Send(ResultMessage, writer, originId);
        }

        private void SendCharacter(int seat, int characterIndex, ulong originId)
        {
            using var writer = new FastBufferWriter(MessageBufferSize, Allocator.Temp);
            WriteCharacter(writer, seat, characterIndex);
            Send(CharacterMessage, writer, originId);
        }

        private static void WriteRequest(FastBufferWriter writer, ThrowRequest request)
        {
            writer.WriteValueSafe(request.PositionX);
            writer.WriteValueSafe(request.AngleDegrees);
            writer.WriteValueSafe(request.Speed);
            writer.WriteValueSafe((int)request.Style);
            writer.WriteValueSafe((int)request.Arc);
        }

        private static ThrowRequest ReadRequest(FastBufferReader reader)
        {
            reader.ReadValueSafe(out float positionX);
            reader.ReadValueSafe(out float angle);
            reader.ReadValueSafe(out float speed);
            reader.ReadValueSafe(out int style);
            reader.ReadValueSafe(out int arc);
            return new ThrowRequest(positionX, angle, speed, (ThrowStyle)style, (ThrowArc)arc);
        }

        private static void WriteCharacter(FastBufferWriter writer, int seat, int characterIndex)
        {
            writer.WriteValueSafe(seat);
            writer.WriteValueSafe(characterIndex);
        }

        private static (int Seat, int CharacterIndex) ReadCharacter(FastBufferReader reader)
        {
            reader.ReadValueSafe(out int seat);
            reader.ReadValueSafe(out int characterIndex);
            return (seat, characterIndex);
        }

        private static void WriteStates(FastBufferWriter writer, PinState[] states)
        {
            writer.WriteValueSafe(states.Length);
            foreach (PinState state in states)
            {
                writer.WriteValueSafe(state.Position);
                writer.WriteValueSafe(state.IsFallen);
                writer.WriteValueSafe(state.FallDirection);
            }
        }

        private static PinState[] ReadStates(FastBufferReader reader)
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

            return states;
        }

        /// <summary>クライアントはホストへ、ホストは投げた人以外の全クライアントへ送る</summary>
        private void Send(string messageName, FastBufferWriter writer, ulong originId)
        {
            if (!_active || Network == null) return;

            var messaging = Network.CustomMessagingManager;
            if (!Network.IsServer)
            {
                messaging.SendNamedMessage(messageName, NetworkManager.ServerClientId, writer, Delivery);
                return;
            }

            CollectRecipients(originId);
            if (_recipients.Count == 0) return;

            messaging.SendNamedMessage(messageName, _recipients, writer, Delivery);
        }

        private void CollectRecipients(ulong originId)
        {
            _recipients.Clear();
            foreach (ulong id in Network.ConnectedClientsIds)
            {
                if (id == Network.LocalClientId || id == originId) continue;

                _recipients.Add(id);
            }
        }

        // ---- 受信 ----

        private void ReceiveThrow(ulong senderId, FastBufferReader reader)
        {
            ThrowRequest request = ReadRequest(reader);
            PinState[] startStates = ReadStates(reader);

            if (Network.IsServer) SendThrow(request, startStates, senderId);
            OnThrowReceived?.Invoke(request, startStates);
        }

        private void ReceiveResult(ulong senderId, FastBufferReader reader)
        {
            PinState[] states = ReadStates(reader);

            if (Network.IsServer) SendResult(states, senderId);
            OnResultReceived?.Invoke(states);
        }

        private void ReceiveCharacter(ulong senderId, FastBufferReader reader)
        {
            (int seat, int characterIndex) = ReadCharacter(reader);

            if (Network.IsServer) SendCharacter(seat, characterIndex, senderId);
            OnCharacterReceived?.Invoke(seat, characterIndex);
        }
    }
}
