using System;
using System.Threading.Tasks;
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
    /// </summary>
    public class OnlineSession : MonoBehaviour
    {
        /// <summary>どのミニゲームも1対1なので部屋の定員は2人で固定</summary>
        private const int MaxPlayers = 2;

        /// <summary>接続完了（相手が揃った）。引数は自分がホストかどうか</summary>
        public event Action<bool> OnPeerConnected;

        /// <summary>試合中に相手との接続が切れた</summary>
        public event Action OnPeerDisconnected;

        public bool IsHost { get; private set; }

        /// <summary>自分が作った部屋の参加コード（ホストのみ）</summary>
        public string JoinCode => _session?.Code;

        private ISession _session;
        private bool _peerConnected;

        /// <summary>部屋を作り、参加コードを発行する。相手の参加は OnPeerConnected で通知する</summary>
        public async Task<string> HostAsync()
        {
            await PrepareAsync();

            // 部屋の作成完了より先に相手が接続してきても取りこぼさないよう、先にホストとして扱う
            IsHost = true;
            var options = new SessionOptions { MaxPlayers = MaxPlayers }.WithRelayNetwork();
            _session = await MultiplayerService.Instance.CreateSessionAsync(options);
            return _session.Code;
        }

        /// <summary>参加コードで部屋に入る。接続できたら OnPeerConnected で通知する</summary>
        public async Task JoinAsync(string code)
        {
            await PrepareAsync();

            IsHost = false;
            _session = await MultiplayerService.Instance.JoinSessionByCodeAsync(code.Trim().ToUpperInvariant());

            // クライアントは Join が返った時点でホストとつながっている
            NotifyPeerConnected();
        }

        /// <summary>部屋から抜ける。待機中のキャンセルとシーン離脱の両方で使う</summary>
        public void Leave()
        {
            UnsubscribeNetworkEvents();

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
        }

        private void HandleClientConnected(ulong clientId)
        {
            // ホスト自身の接続は無視し、相手が入ってきたときだけ試合を始める
            if (!IsHost || clientId == NetworkManager.Singleton.LocalClientId) return;

            NotifyPeerConnected();
        }

        private void HandleClientDisconnected(ulong clientId)
        {
            if (!_peerConnected) return;

            _peerConnected = false;
            OnPeerDisconnected?.Invoke();
        }

        private void NotifyPeerConnected()
        {
            if (_peerConnected) return;

            _peerConnected = true;
            OnPeerConnected?.Invoke(IsHost);
        }
    }
}
