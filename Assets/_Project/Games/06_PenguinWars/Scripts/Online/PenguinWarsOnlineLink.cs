using System;
using System.Collections.Generic;
using MiniGame.Common.Online;
using MiniGame.PenguinWars.Battle;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace MiniGame.PenguinWars
{
    /// <summary>
    /// ペンギン大戦争のオンライン対戦で試合中のやり取りを送受信する（仕様書 §10.2）。
    /// ホストだけが BattleWorld を計算し、ゲストは操作を送って届いた状態を表示するだけにする（サッカーと同じ）。
    ///
    /// ゲスト → ホスト：準備完了、操作（出撃・働きペンギン・ペンギン砲）
    /// ホスト → ゲスト：編成、試合の状態（約15回/秒）、イベント（出撃・ヒット・撃破・砲・城崩壊・時間切れ）
    ///
    /// NetworkObject は使わず名前付きメッセージだけで送る（Prefab 登録などの準備を不要にするため）。
    /// 中身のバイト列は Battle 側（BattleSnapshot / BattleEventCodec）が作るので、ここは運ぶだけ
    /// </summary>
    public class PenguinWarsOnlineLink : MonoBehaviour, ICommandSink
    {
        private const string ReadyMessage = "pw.ready";
        private const string DeckMessage = "pw.deck";
        private const string SnapshotMessage = "pw.snap";
        private const string EventMessage = "pw.evt";
        private const string CommandMessage = "pw.cmd";

        // バイト列の前に付く長さ（int）の分
        private const int LengthHeaderSize = sizeof(int);
        private const int SmallBufferSize = 64;
        private const int DeckBufferSize = 256;

        /// <summary>状態は最新だけ届けばよいので再送せず、古いものが後から届いたら捨てる</summary>
        private const NetworkDelivery StreamDelivery = NetworkDelivery.UnreliableSequenced;
        /// <summary>
        /// イベント・編成・操作は取りこぼすと演出や操作が抜けるので再送ありで送る。
        /// ヒットが重なるとイベントは1パケットを超えることがあるので、分割できる配信にする
        /// </summary>
        private const NetworkDelivery ReliableDelivery = NetworkDelivery.ReliableFragmentedSequenced;

        [SerializeField] private BattleRunner _battleRunner;
        [Tooltip("ホストが状態を送る間隔（秒）。ゲストは補間して表示するので毎フレーム送らなくてよい（BattleRunner の補間時間と同じにする）")]
        [SerializeField] private float _snapshotInterval = 1f / 15f;

        /// <summary>ホスト: ゲストがメッセージを受け取れるようになった。ここで編成を送る</summary>
        public event Action GuestReady;
        /// <summary>ゲスト: ホストが決めた編成（ホスト基準の Left, Right の順）</summary>
        public event Action<int[], int[]> DecksReceived;
        /// <summary>ゲスト: BattleSnapshot のバイト列</summary>
        public event Action<byte[]> SnapshotReceived;
        /// <summary>ゲスト: BattleEventCodec のバイト列</summary>
        public event Action<byte[]> EventsReceived;

        private readonly BattleSnapshot _snapshot = new BattleSnapshot();
        // 1フレーム分のイベントをまとめて1メッセージで送る
        private readonly List<BattleEvent> _pendingEvents = new List<BattleEvent>();
        private bool _active;
        private bool _isHost;
        private float _sendTimer;

        private static NetworkManager Network => NetworkManager.Singleton;

        /// <summary>接続完了後に呼ぶ。以降、役割に応じた送受信を行う</summary>
        public void Begin(bool isHost)
        {
            _isHost = isHost;
            CustomMessagingManager messaging = Network.CustomMessagingManager;
            if (isHost)
            {
                messaging.RegisterNamedMessageHandler(ReadyMessage, ReceiveReady);
                messaging.RegisterNamedMessageHandler(CommandMessage, ReceiveCommand);
                _battleRunner.EventRaised += CollectEvent;
            }
            else
            {
                messaging.RegisterNamedMessageHandler(DeckMessage, ReceiveDeck);
                messaging.RegisterNamedMessageHandler(SnapshotMessage, ReceiveSnapshot);
                messaging.RegisterNamedMessageHandler(EventMessage, ReceiveEvents);
            }
            _active = true;

            // ホストから先に編成を送ると、ゲストがハンドラを登録する前に届いて捨てられることがある。
            // そこでゲストが登録を終えてから「準備できた」と知らせ、ホストはそれを受けて送る
            if (!isHost) SendReady();
        }

        /// <summary>試合終了・切断後は送らない（リザルト表示中に相手へ状態を流し続けないため）</summary>
        public void Stop()
        {
            _active = false;
        }

        private void OnDestroy()
        {
            _active = false;
            if (_battleRunner != null) _battleRunner.EventRaised -= CollectEvent;

            CustomMessagingManager messaging = Network != null ? Network.CustomMessagingManager : null;
            if (messaging == null) return;

            // 役割によって登録していないものもあるが、未登録の解除は何もしないので全部まとめて外す
            messaging.UnregisterNamedMessageHandler(ReadyMessage);
            messaging.UnregisterNamedMessageHandler(DeckMessage);
            messaging.UnregisterNamedMessageHandler(SnapshotMessage);
            messaging.UnregisterNamedMessageHandler(EventMessage);
            messaging.UnregisterNamedMessageHandler(CommandMessage);
        }

        /// <summary>BattleRunner の Update で溜まったイベントを、同じフレームのうちにまとめて送る</summary>
        private void LateUpdate()
        {
            if (!_active || !_isHost) return;

            FlushEvents();
            if (ConsumeSendTimer()) SendSnapshot();
        }

        /// <summary>ポーズ等で timeScale が変わっても通信の頻度は保ちたいので unscaled で数える</summary>
        private bool ConsumeSendTimer()
        {
            _sendTimer -= Time.unscaledDeltaTime;
            if (_sendTimer > 0f) return false;

            _sendTimer = _snapshotInterval;
            return true;
        }

        // ---- 送信：ホスト → ゲスト ----

        /// <summary>編成（キャラNo の並び）。ゲストは同じカタログから数値と絵を引く</summary>
        public void SendDecks(IReadOnlyList<UnitStats> leftDeck, IReadOnlyList<UnitStats> rightDeck)
        {
            using var writer = new FastBufferWriter(DeckBufferSize, Allocator.Temp);
            writer.WriteValueSafe(ToUnitNos(leftDeck));
            writer.WriteValueSafe(ToUnitNos(rightDeck));
            Send(DeckMessage, writer, ReliableDelivery);
        }

        private void CollectEvent(BattleEvent battleEvent)
        {
            if (_active) _pendingEvents.Add(battleEvent);
        }

        private void FlushEvents()
        {
            if (_pendingEvents.Count == 0) return;

            SendBytes(EventMessage, BattleEventCodec.ToBytes(_pendingEvents), ReliableDelivery);
            _pendingEvents.Clear();
        }

        private void SendSnapshot()
        {
            BattleWorld world = _battleRunner.World;
            // 編成を送る前（ゲストの準備待ち）はまだ試合がない
            if (world == null) return;

            _snapshot.Capture(world);
            SendBytes(SnapshotMessage, _snapshot.ToBytes(), StreamDelivery);
        }

        // ---- 送信：ゲスト → ホスト ----

        /// <summary>陣営はホストが Side.Right に付け直すので、種類と枠番号だけ送る</summary>
        public void Submit(BattleCommand command)
        {
            using var writer = new FastBufferWriter(SmallBufferSize, Allocator.Temp);
            writer.WriteValueSafe((byte)command.Type);
            writer.WriteValueSafe((byte)command.SlotIndex);
            Send(CommandMessage, writer, ReliableDelivery);
        }

        private void SendReady()
        {
            using var writer = new FastBufferWriter(SmallBufferSize, Allocator.Temp);
            Send(ReadyMessage, writer, ReliableDelivery);
        }

        private void SendBytes(string messageName, byte[] bytes, NetworkDelivery delivery)
        {
            using var writer = new FastBufferWriter(bytes.Length + LengthHeaderSize, Allocator.Temp);
            writer.WriteValueSafe(bytes);
            Send(messageName, writer, delivery);
        }

        private void Send(string messageName, FastBufferWriter writer, NetworkDelivery delivery)
        {
            if (!_active || !OnlineSession.TryGetPeerId(out ulong peerId)) return;

            Network.CustomMessagingManager.SendNamedMessage(messageName, peerId, writer, delivery);
        }

        // ---- 受信：ホスト ----

        private void ReceiveReady(ulong senderId, FastBufferReader reader)
        {
            GuestReady?.Invoke();
        }

        /// <summary>ゲストの操作はゲスト画面の向き（自分 = Left）で届くので、ホストでは右陣営の操作として入れる</summary>
        private void ReceiveCommand(ulong senderId, FastBufferReader reader)
        {
            reader.ReadValueSafe(out byte type);
            reader.ReadValueSafe(out byte slotIndex);
            switch ((BattleCommandType)type)
            {
                case BattleCommandType.Spawn:
                    _battleRunner.Enqueue(BattleCommand.Spawn(Side.Right, slotIndex));
                    break;
                case BattleCommandType.LevelUpWallet:
                    _battleRunner.Enqueue(BattleCommand.LevelUpWallet(Side.Right));
                    break;
                case BattleCommandType.FireCannon:
                    _battleRunner.Enqueue(BattleCommand.FireCannon(Side.Right));
                    break;
            }
        }

        // ---- 受信：ゲスト ----

        private void ReceiveDeck(ulong senderId, FastBufferReader reader)
        {
            reader.ReadValueSafe(out int[] leftDeck);
            reader.ReadValueSafe(out int[] rightDeck);
            DecksReceived?.Invoke(leftDeck, rightDeck);
        }

        private void ReceiveSnapshot(ulong senderId, FastBufferReader reader)
        {
            reader.ReadValueSafe(out byte[] bytes);
            SnapshotReceived?.Invoke(bytes);
        }

        private void ReceiveEvents(ulong senderId, FastBufferReader reader)
        {
            reader.ReadValueSafe(out byte[] bytes);
            EventsReceived?.Invoke(bytes);
        }

        private static int[] ToUnitNos(IReadOnlyList<UnitStats> deck)
        {
            var unitNos = new int[deck.Count];
            for (int i = 0; i < unitNos.Length; i++) unitNos[i] = deck[i].UnitNo;
            return unitNos;
        }
    }
}
