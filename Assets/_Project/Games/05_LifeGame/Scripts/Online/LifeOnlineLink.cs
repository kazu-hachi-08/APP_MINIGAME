using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace MiniGame.LifeGame
{
    /// <summary>
    /// オンライン対戦の送受信（仕様書 §10.3）。同期は決定論ロックステップなので、送るのは
    /// 「テーマ番号＋シード（ホストから1回）」「席番号＋キャラ番号」「手番の操作（LifeCommand）」の3つだけ。
    /// 出目・盤面・お金は送らない。全端末が同じシードの LifeRules に同じコマンドを渡せば同じになるため。
    /// 値の検証（手番以外・範囲外）は受け取った側の LifeRules.IsValid に任せ、ここでは中身を見ない。
    ///
    /// NGOではクライアント同士が直接送れないため、モルックと同じくホスト中継にする。
    /// クライアントはホストへ送り、ホストは受け取った内容を送り主以外の全クライアントへ転送する。
    /// </summary>
    public class LifeOnlineLink : MonoBehaviour
    {
        private const string SetupMessage = "life.setup";
        private const string CharacterMessage = "life.character";
        private const string CommandMessage = "life.command";

        /// <summary>1メッセージの最大バイト数。どのメッセージも int 3つ以下なので余裕を持たせた値</summary>
        private const int MessageBufferSize = 64;

        /// <summary>コマンドの順番が入れ替わると端末ごとに状態がずれるので、すべて順序保証ありで送る</summary>
        private const NetworkDelivery Delivery = NetworkDelivery.ReliableSequenced;

        /// <summary>引数はテーマ番号とシード</summary>
        public event Action<int, int> OnSetupReceived;

        /// <summary>引数は席番号とキャラ番号</summary>
        public event Action<int, int> OnCharacterReceived;

        public event Action<LifeCommand> OnCommandReceived;

        private readonly List<ulong> _recipients = new List<ulong>();
        private bool _active;

        private static NetworkManager Network => NetworkManager.Singleton;

        /// <summary>接続完了後に呼ぶ。以降、送受信を行う</summary>
        public void Begin()
        {
            var messaging = Network.CustomMessagingManager;
            messaging.RegisterNamedMessageHandler(SetupMessage, ReceiveSetup);
            messaging.RegisterNamedMessageHandler(CharacterMessage, ReceiveCharacter);
            messaging.RegisterNamedMessageHandler(CommandMessage, ReceiveCommand);
            _active = true;
        }

        private void OnDestroy()
        {
            _active = false;

            var messaging = Network != null ? Network.CustomMessagingManager : null;
            if (messaging == null) return;

            messaging.UnregisterNamedMessageHandler(SetupMessage);
            messaging.UnregisterNamedMessageHandler(CharacterMessage);
            messaging.UnregisterNamedMessageHandler(CommandMessage);
        }

        // ---- 送信 ----

        /// <summary>ホストだけが呼ぶ</summary>
        public void SendSetup(int themeIndex, int seed) => SendInts(SetupMessage, Network.LocalClientId, themeIndex, seed);

        public void SendCharacter(int seat, int characterIndex) =>
            SendInts(CharacterMessage, Network.LocalClientId, seat, characterIndex);

        public void SendCommand(LifeCommand command) => SendCommand(command, Network.LocalClientId);

        private void SendCommand(LifeCommand command, ulong originId) =>
            SendInts(CommandMessage, originId, command.Seat, (int)command.Type, command.Value);

        /// <param name="originId">送り主の端末。ホストが中継するときに送り返さないため</param>
        private void SendInts(string messageName, ulong originId, params int[] values)
        {
            using var writer = new FastBufferWriter(MessageBufferSize, Allocator.Temp);
            foreach (int value in values) writer.WriteValueSafe(value);
            Send(messageName, writer, originId);
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
            // テーマとシードを決めるのはホストだけ。クライアントから届いたものは中継も採用もしない
            if (Network.IsServer) return;

            reader.ReadValueSafe(out int themeIndex);
            reader.ReadValueSafe(out int seed);
            OnSetupReceived?.Invoke(themeIndex, seed);
        }

        private void ReceiveCharacter(ulong senderId, FastBufferReader reader)
        {
            reader.ReadValueSafe(out int seat);
            reader.ReadValueSafe(out int characterIndex);

            if (Network.IsServer) SendInts(CharacterMessage, senderId, seat, characterIndex);
            OnCharacterReceived?.Invoke(seat, characterIndex);
        }

        private void ReceiveCommand(ulong senderId, FastBufferReader reader)
        {
            reader.ReadValueSafe(out int seat);
            reader.ReadValueSafe(out int type);
            reader.ReadValueSafe(out int value);
            var command = new LifeCommand(seat, (LifeCommandType)type, value);

            if (Network.IsServer) SendCommand(command, senderId);
            OnCommandReceived?.Invoke(command);
        }
    }
}
