using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace MiniGame.Golf
{
    /// <summary>
    /// オンライン対戦の送受信（§14）。送るのは「試合開始時のホールと風」「打った瞬間の入力」「止まった後の結果」の3つだけ。
    ///
    /// BallSimulator は端末ごとの小数の誤差でずれることがあるため、入力は他の端末で演出を再生するためだけに使い、
    /// 位置と打数は打った人の端末の結果で上書きする。打つ順番はボール位置から全端末で同じ計算になるので送らない。
    /// モルックと同じく NetworkObject を使わず名前付きメッセージだけで済ませ、クライアント同士はホストが中継する。
    /// </summary>
    public class GolfOnlineLink : MonoBehaviour
    {
        private const string SetupMessage = "golf.setup";
        private const string ShotMessage = "golf.shot";
        private const string ResultMessage = "golf.result";

        /// <summary>1メッセージの最大バイト数。一番大きい開始メッセージ（3ホール×16バイト）が十分収まる大きさ</summary>
        private const int MessageBufferSize = 256;

        /// <summary>入力 → 結果の順番が入れ替わると困るので、どれも順序保証ありで送る</summary>
        private const NetworkDelivery Delivery = NetworkDelivery.ReliableSequenced;

        public event Action<GolfMatchSetup> OnSetupReceived;
        public event Action<GolfShotMessage> OnShotReceived;
        public event Action<GolfShotResultMessage> OnResultReceived;

        private readonly List<ulong> _recipients = new List<ulong>();
        private bool _active;

        private static NetworkManager Network => NetworkManager.Singleton;

        /// <summary>
        /// 接続後に呼ぶ。部屋に入り直すと受信の登録が消えるので、何度呼んでも登録し直すだけにする。
        /// クライアントは試合開始の直後に開始メッセージが届くため、参加できた時点で呼んでおく
        /// </summary>
        public void Begin()
        {
            var messaging = Network.CustomMessagingManager;
            messaging.RegisterNamedMessageHandler(SetupMessage, ReceiveSetup);
            messaging.RegisterNamedMessageHandler(ShotMessage, ReceiveShot);
            messaging.RegisterNamedMessageHandler(ResultMessage, ReceiveResult);
            _active = true;
        }

        private void OnDestroy()
        {
            _active = false;

            var messaging = Network != null ? Network.CustomMessagingManager : null;
            if (messaging == null) return;

            messaging.UnregisterNamedMessageHandler(SetupMessage);
            messaging.UnregisterNamedMessageHandler(ShotMessage);
            messaging.UnregisterNamedMessageHandler(ResultMessage);
        }

        // ---- 送信 ----

        /// <summary>ホストだけが呼ぶ</summary>
        public void SendSetup(GolfMatchSetup setup)
        {
            using var writer = new FastBufferWriter(MessageBufferSize, Allocator.Temp);
            writer.WriteValueSafe(setup.HoleIndices.Count);
            for (int i = 0; i < setup.HoleIndices.Count; i++)
            {
                Wind wind = setup.Winds[i];
                writer.WriteValueSafe(setup.HoleIndices[i]);
                writer.WriteValueSafe(wind.Direction.X);
                writer.WriteValueSafe(wind.Direction.Y);
                writer.WriteValueSafe(wind.Strength);
            }

            Send(SetupMessage, writer, Network.LocalClientId);
        }

        public void SendShot(GolfShotMessage shot)
        {
            SendShot(shot, Network.LocalClientId);
        }

        public void SendResult(GolfShotResultMessage result)
        {
            SendResult(result, Network.LocalClientId);
        }

        /// <param name="originId">打った人の端末。ホストが中継するときに送り返さないため</param>
        private void SendShot(GolfShotMessage shot, ulong originId)
        {
            using var writer = new FastBufferWriter(MessageBufferSize, Allocator.Temp);
            writer.WriteValueSafe(shot.Direction);
            writer.WriteValueSafe(shot.ClubIndex);
            writer.WriteValueSafe(shot.Power);
            writer.WriteValueSafe(shot.ImpactOffset);
            Send(ShotMessage, writer, originId);
        }

        private void SendResult(GolfShotResultMessage result, ulong originId)
        {
            using var writer = new FastBufferWriter(MessageBufferSize, Allocator.Temp);
            writer.WriteValueSafe(result.Position);
            writer.WriteValueSafe(result.Strokes);
            writer.WriteValueSafe(result.IsInCup);
            Send(ResultMessage, writer, originId);
        }

        /// <summary>クライアントはホストへ、ホストは送り主以外の全クライアントへ送る</summary>
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

        private void ReceiveSetup(ulong senderId, FastBufferReader reader)
        {
            reader.ReadValueSafe(out int count);
            var holeIndices = new int[count];
            var winds = new Wind[count];
            for (int i = 0; i < count; i++)
            {
                reader.ReadValueSafe(out holeIndices[i]);
                reader.ReadValueSafe(out float directionX);
                reader.ReadValueSafe(out float directionY);
                reader.ReadValueSafe(out float strength);
                winds[i] = new Wind(new System.Numerics.Vector2(directionX, directionY), strength);
            }

            OnSetupReceived?.Invoke(new GolfMatchSetup(holeIndices, winds));
        }

        private void ReceiveShot(ulong senderId, FastBufferReader reader)
        {
            reader.ReadValueSafe(out Vector2 direction);
            reader.ReadValueSafe(out int clubIndex);
            reader.ReadValueSafe(out float power);
            reader.ReadValueSafe(out float impactOffset);
            var shot = new GolfShotMessage(direction, clubIndex, power, impactOffset);

            if (Network.IsServer) SendShot(shot, senderId);
            OnShotReceived?.Invoke(shot);
        }

        private void ReceiveResult(ulong senderId, FastBufferReader reader)
        {
            reader.ReadValueSafe(out Vector2 position);
            reader.ReadValueSafe(out int strokes);
            reader.ReadValueSafe(out bool isInCup);
            var result = new GolfShotResultMessage(position, strokes, isInCup);

            if (Network.IsServer) SendResult(result, senderId);
            OnResultReceived?.Invoke(result);
        }
    }
}
