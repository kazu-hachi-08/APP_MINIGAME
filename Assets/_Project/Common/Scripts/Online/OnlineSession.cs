using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;

namespace MiniGame.Common.Online
{
    /// <summary>
    /// オンライン対戦の接続だけを担当する（部屋を作る／参加コードで入る／抜ける）。
    /// 通信経路は Unity Relay に任せ、NAT越えやIP入力をプレイヤーにさせない。
    /// 試合中にやり取りする内容は各ミニゲームの通信クラス（卓球: OnlineMatchLink / サッカー: SoccerOnlineLink）が扱う。
    ///
    /// 3人以上の部屋（HostAsync の maxPlayers が3以上）では、相手が来ても自動では始めず、
    /// ホストが StartMatch を呼んだときに参加順で席番号を配る。
    /// </summary>
    public class OnlineSession : MonoBehaviour
    {
        /// <summary>1対1のミニゲーム（卓球・サッカー）の定員。引数を省略したときはこれまで通り2人部屋</summary>
        public const int DefaultMaxPlayers = 2;

        private const string StartMessage = "session.start";
        private const int StartMessageSize = sizeof(int) * 2;

        /// <summary>接続完了（相手が揃った）。引数は自分がホストかどうか</summary>
        public event Action<bool> OnPeerConnected;

        /// <summary>試合中に相手との接続が切れた</summary>
        public event Action OnPeerDisconnected;

        /// <summary>3人以上の部屋のホストのみ：参加人数（ホスト自身を含む）が変わった</summary>
        public event Action<int> OnMemberCountChanged;

        /// <summary>3人以上の部屋のみ：試合開始。引数は自分の席番号（ホスト=0）と総人数</summary>
        public event Action<int, int> OnMatchStarted;

        public bool IsHost { get; private set; }

        /// <summary>自分が作った部屋の参加コード（ホストのみ）</summary>
        public string JoinCode => _session?.Code;

        /// <summary>ホストから見た参加者（自分以外）。席番号を参加順で配るため順番を保つ</summary>
        private readonly List<ulong> _memberIds = new List<ulong>();

        private ISession _session;
        private bool _peerConnected;
        private int _maxPlayers = DefaultMaxPlayers;
        private bool _matchStarted;

        private bool IsMultiRoom => _maxPlayers > DefaultMaxPlayers;

        private static bool IsBrowser => Application.platform == RuntimePlatform.WebGLPlayer;

        /// <summary>
        /// ブラウザはUDP(dtls)を使えないので、Relayとの通信をWebSocket(wss)にする。
        /// 相手の端末とは独立に選べるので、スマホ版とブラウザ版でも対戦できる
        /// </summary>
        private static NetworkOptions PlatformNetworkOptions => new NetworkOptions
        {
            RelayProtocol = IsBrowser ? RelayProtocol.WSS : RelayProtocol.DTLS
        };

        /// <summary>
        /// 部屋を作り、参加コードを発行する。
        /// 相手の参加は OnPeerConnected（3人以上の部屋では OnMemberCountChanged）で通知する
        /// </summary>
        public async Task<string> HostAsync(int maxPlayers = DefaultMaxPlayers)
        {
            await PrepareAsync();

            // 部屋の作成完了より先に相手が接続してきても取りこぼさないよう、先にホストとして扱う
            IsHost = true;
            _maxPlayers = maxPlayers;
            var options = new SessionOptions { MaxPlayers = maxPlayers }.WithRelayNetwork().WithNetworkOptions(PlatformNetworkOptions);
            _session = await MultiplayerService.Instance.CreateSessionAsync(options);
            return _session.Code;
        }

        /// <summary>参加コードで部屋に入る。接続できたら OnPeerConnected で通知する</summary>
        public async Task JoinAsync(string code)
        {
            await PrepareAsync();

            IsHost = false;
            var options = new JoinSessionOptions().WithNetworkOptions(PlatformNetworkOptions);
            _session = await MultiplayerService.Instance.JoinSessionByCodeAsync(code.Trim().ToUpperInvariant(), options);

            // 1対1の部屋ではホストが開始メッセージを送らないので、登録しても使われないだけで害はない
            NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(StartMessage, ReceiveStart);

            // クライアントは Join が返った時点でホストとつながっている
            NotifyPeerConnected();
        }

        /// <summary>
        /// 3人以上の部屋のホストが、集まったメンバーで試合を始める。席番号は参加順（ホスト=0）。
        /// 以降に入ってきた人は AddMember で切断する
        /// </summary>
        public void StartMatch()
        {
            if (!IsHost || _matchStarted || _memberIds.Count == 0) return;

            _matchStarted = true;
            int count = _memberIds.Count + 1;
            for (int i = 0; i < _memberIds.Count; i++)
            {
                SendStart(_memberIds[i], i + 1, count);
            }

            OnMatchStarted?.Invoke(0, count);
        }

        /// <summary>部屋から抜ける。待機中のキャンセルとシーン離脱の両方で使う</summary>
        public void Leave()
        {
            UnsubscribeNetworkEvents();
            _memberIds.Clear();
            _matchStarted = false;
            _maxPlayers = DefaultMaxPlayers;

            if (_session == null) return;

            // 抜ける処理の完了は待たない（シーン遷移を止めないため）
            _ = _session.LeaveAsync();
            _session = null;
            _peerConnected = false;
        }

        private void OnDestroy()
        {
            Leave();
        }

        /// <summary>1対1なので、ホストから見た相手は唯一の接続クライアント、クライアントから見た相手はホスト</summary>
        public static bool TryGetPeerId(out ulong peerId)
        {
            var network = NetworkManager.Singleton;
            peerId = NetworkManager.ServerClientId;
            if (network == null) return false;
            if (!network.IsServer) return true;

            foreach (ulong id in network.ConnectedClientsIds)
            {
                if (id == network.LocalClientId) continue;

                peerId = id;
                return true;
            }

            return false;
        }

        private async Task PrepareAsync()
        {
            EnsureNetworkManager();
            await SignInAsync();
            SubscribeNetworkEvents();
        }

        private static async Task SignInAsync()
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                var options = new InitializationOptions();
#if UNITY_EDITOR
                // エディタを2つ立ち上げて試すと同じ匿名IDになり、自分の部屋に参加できなくなるため
                options.SetProfile("editor" + UnityEngine.Random.Range(0, 100000));
#endif
                await UnityServices.InitializeAsync(options);
            }

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }
        }

        /// <summary>
        /// NetworkManager はシーンに置かず実行時に1つだけ作る。
        /// Scene に置くとオフライン（NPC戦）でも常に存在してしまい、2人開発での Scene 差分も増えるため。
        /// NetworkManager 自身が DontDestroyOnLoad になるので、再戦時は作り直さず使い回す。
        /// </summary>
        private static void EnsureNetworkManager()
        {
            if (NetworkManager.Singleton != null) return;

            var obj = new GameObject("NetworkManager");
            var transport = obj.AddComponent<UnityTransport>();
            transport.UseWebSockets = IsBrowser;
            var manager = obj.AddComponent<NetworkManager>();
            manager.NetworkConfig = new NetworkConfig
            {
                NetworkTransport = transport,

                // 両者とも同じシーンを自分で読み込んでいるので、NGOのシーン同期は使わない
                EnableSceneManagement = false
            };
        }

        private void SubscribeNetworkEvents()
        {
            UnsubscribeNetworkEvents();
            NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnected;
        }

        private void UnsubscribeNetworkEvents()
        {
            if (NetworkManager.Singleton == null) return;

            NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnected;
            NetworkManager.Singleton.CustomMessagingManager?.UnregisterNamedMessageHandler(StartMessage);
        }

        private void HandleClientConnected(ulong clientId)
        {
            // ホスト自身の接続は無視し、相手が入ってきたときだけ試合を始める
            if (!IsHost || clientId == NetworkManager.Singleton.LocalClientId) return;

            if (IsMultiRoom)
            {
                AddMember(clientId);
                return;
            }

            NotifyPeerConnected();
        }

        private void AddMember(ulong clientId)
        {
            // 試合中に途中参加されると手番の数が合わなくなるので断る
            if (_matchStarted)
            {
                NetworkManager.Singleton.DisconnectClient(clientId);
                return;
            }

            _memberIds.Add(clientId);
            _peerConnected = true;
            OnMemberCountChanged?.Invoke(_memberIds.Count + 1);
        }

        private void HandleClientDisconnected(ulong clientId)
        {
            if (IsHost && IsMultiRoom)
            {
                HandleMemberDisconnected(clientId);
                return;
            }

            if (!_peerConnected) return;

            _peerConnected = false;
            OnPeerDisconnected?.Invoke();
        }

        /// <summary>
        /// 開始前に抜けた人は人数を減らすだけ。試合中に1人でも抜けたら全員の試合を終える（途中離脱の継続・再接続はしない）。
        /// ホストが部屋を閉じると残りのクライアントにも切断が届き、それぞれの端末で試合が終わる
        /// </summary>
        private void HandleMemberDisconnected(ulong clientId)
        {
            if (!_memberIds.Remove(clientId)) return;

            if (!_matchStarted)
            {
                _peerConnected = _memberIds.Count > 0;
                OnMemberCountChanged?.Invoke(_memberIds.Count + 1);
                return;
            }

            OnPeerDisconnected?.Invoke();
            Leave();
        }

        private static void SendStart(ulong clientId, int seat, int count)
        {
            using var writer = new FastBufferWriter(StartMessageSize, Allocator.Temp);
            writer.WriteValueSafe(seat);
            writer.WriteValueSafe(count);
            NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(StartMessage, clientId, writer,
                NetworkDelivery.ReliableSequenced);
        }

        private void ReceiveStart(ulong senderId, FastBufferReader reader)
        {
            reader.ReadValueSafe(out int seat);
            reader.ReadValueSafe(out int count);
            _matchStarted = true;
            OnMatchStarted?.Invoke(seat, count);
        }

        private void NotifyPeerConnected()
        {
            if (_peerConnected) return;

            _peerConnected = true;
            OnPeerConnected?.Invoke(IsHost);
        }
    }
}
