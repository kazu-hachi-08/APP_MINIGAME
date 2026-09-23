#nullable enable
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CardGame.Core.Commands;
using Newtonsoft.Json;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

namespace CardGame.Unity.Net
{
    /// <summary>ホストがゲストに送る対戦開始情報。</summary>
    public sealed class MatchStartInfo
    {
        public ulong seed;
        public int firstPlayer;
        public string hostDeck = "";
        public string guestDeck = "";
    }

    /// <summary>
    /// オンライン対戦の通信層(05-online.md)。Unity Relay + Netcode for GameObjects の NamedMessage だけを使う。
    /// - ホスト = プレイヤー 0、ゲスト = プレイヤー 1
    /// - ゲストの操作は "req" でホストへ送り、ホストが検証・適用してから "cmd" で配信する(ホストが順序を決める)
    /// - ゲームロジックは持たない。BattleScreen が Core を進める
    /// </summary>
    public sealed class OnlineSession : MonoBehaviour
    {
        public static OnlineSession? Instance { get; private set; }

        public bool IsHost { get; private set; }
        public bool IsConnected => NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
        public string JoinCode { get; private set; } = "";
        public string LocalDeckName { get; private set; } = "";
        public string RemoteDeckName { get; private set; } = "";

        /// <summary>ドラフトの部屋の印(hello でデッキの代わりに送る。07-draft.md 案 2)。</summary>
        public const string DraftMarker = "@draft";
        /// <summary>ドラフトの部屋でドラフトが始まった(両者に同じシード)。</summary>
        public event Action<ulong>? DraftStarted;
        /// <summary>相手が何回選んだか(ドラフト中)。</summary>
        public event Action<int>? RemoteDraftProgress;
        /// <summary>ホストとゲストで部屋の形式(ドラフト / 通常)が違った。</summary>
        public event Action? FormatMismatch;

        /// <summary>ゲスト側: ホストから開始情報が届いた。ホスト側: ゲストが接続しデッキ名が届いた。</summary>
        public event Action<MatchStartInfo>? MatchStarted;
        /// <summary>ホスト側: ゲストからの操作要求。</summary>
        public event Action<GameCommand>? RequestFromGuest;
        /// <summary>ゲスト側: ホストが確定したコマンド。</summary>
        public event Action<GameCommand>? CommandFromHost;
        /// <summary>ゲスト側: ホストに拒否された。</summary>
        public event Action<string>? Rejected;
        /// <summary>相手が切断した(または自分の接続が切れた)。</summary>
        public event Action? Disconnected;
        /// <summary>ロビー用の状態メッセージ。</summary>
        public event Action<string>? Status;

        private NetworkManager? _nm;
        private UnityTransport? _transport;
        private ulong _guestClientId;
        private bool _matchStarted;
        private MatchStartInfo? _pendingStart;
        private bool _draftStarted;

        public static OnlineSession GetOrCreate()
        {
            if (Instance != null) return Instance;
            var go = new GameObject("OnlineSession");
            DontDestroyOnLoad(go);
            Instance = go.AddComponent<OnlineSession>();
            return Instance;
        }

        // ------------------------------------------------------------------
        // 接続
        // ------------------------------------------------------------------

        private async Task EnsureServicesAsync()
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                Report("Unity Services を初期化中…");
                await UnityServices.InitializeAsync();
            }
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                Report("サインイン中…");
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }
        }

        private void EnsureNetworkManager()
        {
            if (_nm != null) return;
            DestroyForeignNetworkManager();
            var go = new GameObject("NetworkManager");
            DontDestroyOnLoad(go);
            _nm = go.AddComponent<NetworkManager>();
            _transport = go.AddComponent<UnityTransport>();
            _transport.UseWebSockets = Application.platform == RuntimePlatform.WebGLPlayer;
            _nm.NetworkConfig = new NetworkConfig
            {
                NetworkTransport = _transport,
                ConnectionApproval = false,
                EnableSceneManagement = false,
            };
            _nm.OnClientConnectedCallback += OnClientConnected;
            _nm.OnClientDisconnectCallback += OnClientDisconnected;
            _nm.OnTransportFailure += () => { Debug.LogWarning("[Online] transport failure"); HandleDisconnect(); };
        }

        /// <summary>
        /// 他のミニゲームが作った NetworkManager は設定が違う(シーン管理あり等)ので使い回さずに消す。
        /// Destroy だとフレーム末まで残り、直後の AddComponent で二重になるため DestroyImmediate を使う。
        /// </summary>
        private static void DestroyForeignNetworkManager()
        {
            var other = NetworkManager.Singleton;
            if (other == null) return;
            if (other.IsListening) other.Shutdown();
            DestroyImmediate(other.gameObject);
        }

        /// <summary>
        /// カードゲームから出るときに呼ぶ。自分の NetworkManager を残すと、共通側のオンライン処理が
        /// NetworkManager.Singleton として拾ってしまうため、自分ごと破棄する。
        /// </summary>
        public void Teardown()
        {
            Leave();
            if (_nm != null) Destroy(_nm.gameObject);
            Destroy(gameObject);   // OnDestroy で Instance が null に戻る
        }

        private static string ConnectionType => Application.platform == RuntimePlatform.WebGLPlayer ? "wss" : "dtls";

        private static RelayServerData ToServerData(Allocation a)
        {
            var ep = PickEndpoint(a.ServerEndpoints.Select(e => (e.ConnectionType, e.Host, e.Port, e.Secure)));
            return new RelayServerData(ep.Host, (ushort)ep.Port, a.AllocationIdBytes, a.ConnectionData, a.ConnectionData, a.Key, ep.Secure, ep.ConnectionType == "wss");
        }

        private static RelayServerData ToServerData(JoinAllocation a)
        {
            var ep = PickEndpoint(a.ServerEndpoints.Select(e => (e.ConnectionType, e.Host, e.Port, e.Secure)));
            return new RelayServerData(ep.Host, (ushort)ep.Port, a.AllocationIdBytes, a.ConnectionData, a.HostConnectionData, a.Key, ep.Secure, ep.ConnectionType == "wss");
        }

        private static (string ConnectionType, string Host, int Port, bool Secure) PickEndpoint(System.Collections.Generic.IEnumerable<(string ConnectionType, string Host, int Port, bool Secure)> endpoints)
        {
            var list = endpoints.ToList();
            return list.FirstOrDefault(e => e.ConnectionType == ConnectionType) is var ep && ep.Host != null
                ? ep
                : list.First();
        }

        /// <summary>部屋を作る。戻り値は部屋コード。ゲストが来て開始できたら MatchStarted が発火する。</summary>
        public async Task<string> HostAsync(string deckName)
        {
            LocalDeckName = deckName;
            IsHost = true;
            _matchStarted = false;
            _draftStarted = false;
            RemoteDeckName = "";
            await EnsureServicesAsync();
            EnsureNetworkManager();

            Report("部屋を作成中…");
            var allocation = await RelayService.Instance.CreateAllocationAsync(1);
            JoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            _transport!.SetRelayServerData(ToServerData(allocation));

            if (!_nm!.StartHost()) throw new InvalidOperationException("ホストを開始できなかった");
            RegisterHandlers();
            Report($"部屋コード: {JoinCode}");
            return JoinCode;
        }

        /// <summary>部屋コードで参加する。開始情報が届くと MatchStarted が発火する。</summary>
        public async Task JoinAsync(string joinCode, string deckName)
        {
            LocalDeckName = deckName;
            IsHost = false;
            _matchStarted = false;
            _draftStarted = false;
            RemoteDeckName = "";
            await EnsureServicesAsync();
            EnsureNetworkManager();

            Report("部屋を探しています…");
            var allocation = await RelayService.Instance.JoinAllocationAsync(joinCode.Trim().ToUpperInvariant());
            JoinCode = joinCode;
            _transport!.SetRelayServerData(ToServerData(allocation));

            if (!_nm!.StartClient()) throw new InvalidOperationException("接続を開始できなかった");
            Report("ホストに接続中…");
        }

        public void Leave()
        {
            if (_nm != null && _nm.IsListening) _nm.Shutdown();
            _matchStarted = false;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ------------------------------------------------------------------
        // メッセージ
        // ------------------------------------------------------------------

        private const string MsgHello = "hello";   // guest → host: デッキ名
        private const string MsgStart = "start";   // host → guest: MatchStartInfo
        private const string MsgReq = "req";       // guest → host: コマンド
        private const string MsgCmd = "cmd";       // host → guest: 確定コマンド
        private const string MsgRej = "rej";       // host → guest: 拒否理由
        private const string MsgDraft = "draft";   // host → guest: ドラフトのシード
        private const string MsgProg = "prog";     // 双方向: ドラフトで何回選んだか
        private const string MsgFmt = "fmt";       // host → guest: 部屋の形式が違う

        private void RegisterHandlers()
        {
            var cm = _nm!.CustomMessagingManager;
            cm.RegisterNamedMessageHandler(MsgHello, (sender, reader) => { RemoteDeckName = ReadString(reader); TryStartMatch(); });
            cm.RegisterNamedMessageHandler(MsgStart, (sender, reader) =>
            {
                var info = JsonConvert.DeserializeObject<MatchStartInfo>(ReadString(reader))!;
                RemoteDeckName = info.hostDeck;
                _matchStarted = true;
                MatchStarted?.Invoke(info);
            });
            cm.RegisterNamedMessageHandler(MsgReq, (sender, reader) => RequestFromGuest?.Invoke(CommandCodec.Decode(ReadString(reader))));
            cm.RegisterNamedMessageHandler(MsgCmd, (sender, reader) => CommandFromHost?.Invoke(CommandCodec.Decode(ReadString(reader))));
            cm.RegisterNamedMessageHandler(MsgRej, (sender, reader) => Rejected?.Invoke(ReadString(reader)));
            cm.RegisterNamedMessageHandler(MsgDraft, (sender, reader) =>
            {
                _draftStarted = true;
                DraftStarted?.Invoke(ulong.Parse(ReadString(reader)));
            });
            cm.RegisterNamedMessageHandler(MsgProg, (sender, reader) => RemoteDraftProgress?.Invoke(int.Parse(ReadString(reader))));
            cm.RegisterNamedMessageHandler(MsgFmt, (sender, reader) => { ReadString(reader); FormatMismatch?.Invoke(); Leave(); });
        }

        private void OnClientConnected(ulong clientId)
        {
            if (_nm == null) return;
            if (IsHost)
            {
                if (clientId == _nm.LocalClientId) return;
                _guestClientId = clientId;
                Report("相手が接続しました。開始情報を送信中…");
                TryStartMatch();
            }
            else
            {
                RegisterHandlers();
                Report("接続しました。ホストの開始を待っています…");
                Send(MsgHello, LocalDeckName, NetworkManager.ServerClientId);
            }
        }

        /// <summary>
        /// ホスト: ゲストの接続とデッキが揃ったら開始情報を作って送る。
        /// ドラフトの部屋では、まず 2 人に同じシードを配ってドラフトを始め、両者のデッキが届いてから開始する。
        /// </summary>
        private void TryStartMatch()
        {
            if (!IsHost || _matchStarted || string.IsNullOrEmpty(RemoteDeckName) || _guestClientId == 0) return;
            bool localDraft = LocalDeckName == DraftMarker, remoteDraft = RemoteDeckName == DraftMarker;
            if (!_draftStarted && localDraft != remoteDraft)
            {
                Send(MsgFmt, "", _guestClientId);
                Report("部屋の形式(ドラフト / 通常のデッキ)が相手と違います");
                FormatMismatch?.Invoke();
                StartCoroutine(LeaveLater(1f));
                return;
            }
            if (localDraft && remoteDraft && !_draftStarted)
            {
                _draftStarted = true;
                ulong draftSeed = (ulong)DateTime.UtcNow.Ticks;
                Send(MsgDraft, draftSeed.ToString(), _guestClientId);
                DraftStarted?.Invoke(draftSeed);
                return;
            }
            if (localDraft || remoteDraft) return;   // ドラフト中: 両者のデッキが揃うのを待つ
            _matchStarted = true;
            var info = new MatchStartInfo
            {
                seed = (ulong)DateTime.UtcNow.Ticks,
                firstPlayer = UnityEngine.Random.Range(0, 2),
                hostDeck = LocalDeckName,
                guestDeck = RemoteDeckName,
            };
            Send(MsgStart, JsonConvert.SerializeObject(info), _guestClientId);
            MatchStarted?.Invoke(info);
        }

        private void OnClientDisconnected(ulong clientId)
        {
            if (_nm == null) return;
            // ホスト: ゲストが落ちた / ゲスト: 自分が切られた(= ホストが落ちた)
            if (IsHost && clientId == _nm.LocalClientId) return;
            HandleDisconnect();
        }

        private void HandleDisconnect()
        {
            Report("接続が切れました");
            Disconnected?.Invoke();
        }

        /// <summary>ドラフト完了: 自分のデッキ(中身ごとの文字列)を登録し、ゲストならホストへ送る。</summary>
        public void SubmitDraftDeck(string deckWire)
        {
            LocalDeckName = deckWire;
            if (IsHost) TryStartMatch();
            else Send(MsgHello, deckWire, NetworkManager.ServerClientId);
        }

        /// <summary>ドラフトで何回選んだかを相手へ知らせる。</summary>
        public void SendDraftProgress(int picks)
        {
            if (IsHost) { if (_guestClientId != 0) Send(MsgProg, picks.ToString(), _guestClientId); }
            else Send(MsgProg, picks.ToString(), NetworkManager.ServerClientId);
        }

        private System.Collections.IEnumerator LeaveLater(float seconds)
        {
            yield return new WaitForSecondsRealtime(seconds);
            Leave();
        }

        /// <summary>ゲスト: 自分の操作をホストへ要求する。</summary>
        public void SendRequest(GameCommand cmd) => Send(MsgReq, CommandCodec.Encode(cmd), NetworkManager.ServerClientId);

        /// <summary>ホスト: 確定したコマンドをゲストへ配信する。</summary>
        public void Broadcast(GameCommand cmd)
        {
            if (_guestClientId != 0) Send(MsgCmd, CommandCodec.Encode(cmd), _guestClientId);
        }

        /// <summary>ホスト: ゲストの要求を拒否する。</summary>
        public void Reject(string reason)
        {
            if (_guestClientId != 0) Send(MsgRej, reason, _guestClientId);
        }

        private void Send(string name, string payload, ulong to)
        {
            if (_nm == null || !_nm.IsListening) return;
            var bytes = Encoding.UTF8.GetBytes(payload);
            using var writer = new FastBufferWriter(bytes.Length + sizeof(int), Allocator.Temp);
            writer.WriteValueSafe(bytes.Length);
            writer.WriteBytesSafe(bytes, bytes.Length);
            _nm.CustomMessagingManager.SendNamedMessage(name, to, writer, NetworkDelivery.ReliableSequenced);
        }

        private static string ReadString(FastBufferReader reader)
        {
            reader.ReadValueSafe(out int len);
            var bytes = new byte[len];
            reader.ReadBytesSafe(ref bytes, len);
            return Encoding.UTF8.GetString(bytes);
        }

        private void Report(string s)
        {
            Debug.Log($"[Online] {s}");
            Status?.Invoke(s);
        }
    }
}
