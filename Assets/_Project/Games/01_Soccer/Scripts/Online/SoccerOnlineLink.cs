using System;
using MiniGame.Common.Audio;
using MiniGame.Common.Input;
using MiniGame.Common.Online;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace MiniGame.Soccer
{
    /// <summary>選手1人ぶんの同期内容</summary>
    public struct PlayerSnapshot
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public bool IsStaggered;
    }

    /// <summary>ホストから届く試合の最新状態（受信側で使い回すので class にしている）</summary>
    public class SoccerSnapshot
    {
        public float RemainingSeconds;
        public int HomeScore;
        public int AwayScore;
        public int HomeControlledIndex;
        public int AwayControlledIndex;
        public Vector2 BallPosition;
        public Vector2 BallVelocity;
        public PlayerSnapshot[] Players = Array.Empty<PlayerSnapshot>();

        /// <summary>ホストが送ってから届くまでにかかった時間（秒）</summary>
        public float Age;
    }

    /// <summary>
    /// サッカーのオンライン対戦で試合中のやり取りを送受信する。
    ///
    /// 卓球と違いボールと22人が常に接触し合うため、各端末で物理を計算すると結果がずれる。
    /// そこでホスト（HOME）だけが試合を計算し、ゲスト（AWAY）は入力を送って、届いた状態を表示するだけにする。
    ///
    /// ホスト → ゲスト：選手・ボールの位置（毎秒約30回）、メッセージ・SE・ゴール・試合終了
    /// ゲスト → ホスト：移動入力（毎秒約30回）、ボタンを押した瞬間
    ///
    /// NetworkObject を使わず名前付きメッセージだけで済ませ、Prefab登録などの準備を不要にしている。
    /// </summary>
    public class SoccerOnlineLink : MonoBehaviour
    {
        private const string SnapshotMessage = "sc.snap";
        private const string TextMessage = "sc.msg";
        private const string SeMessage = "sc.se";
        private const string KickMessage = "sc.kick";
        private const string GoalMessage = "sc.goal";
        private const string EndMessage = "sc.end";
        private const string MoveMessage = "sc.move";
        private const string ButtonMessage = "sc.btn";

        /// <summary>
        /// 1メッセージの最大バイト数。スナップショット（22人で約420バイト）が収まり、
        /// かつ再送なし配信で分割されない大きさ（MTU未満）にする
        /// </summary>
        private const int SnapshotBufferSize = 1024;
        private const int SmallBufferSize = 256;

        [Header("Sync Targets（ホスト・ゲストで同じ並び）")]
        [Tooltip("HOME 11人 → AWAY 11人の順。両端末とも同じSceneなので、この並びの番号で選手を対応付ける")]
        [SerializeField] private TeamMember[] _players = Array.Empty<TeamMember>();
        [SerializeField] private Ball _ball;
        [SerializeField] private PlayerSwitcher _homeSwitcher;
        [SerializeField] private PlayerSwitcher _awaySwitcher;
        [SerializeField] private SoccerGameManager _gameManager;
        [SerializeField] private RemoteInputProvider _remoteInput;

        [Header("Send Rate")]
        [Tooltip("ホストが状態を送る間隔（秒）。ゲストは補間して表示するので毎フレーム送らなくてよい")]
        [SerializeField] private float _snapshotInterval = 1f / 30f;

        [Tooltip("ゲストが移動入力を送る間隔（秒）")]
        [SerializeField] private float _moveSendInterval = 1f / 30f;

        [Tooltip("遅延補正で先読みする上限（秒）。通信が詰まったときに選手が飛びすぎないようにする")]
        [SerializeField] private float _maxLatencyCompensation = 0.25f;

        public event Action<SoccerSnapshot> OnSnapshotReceived;
        public event Action<string> OnTextReceived;
        public event Action<SeId> OnSeReceived;
        public event Action<float> OnKickReceived;
        public event Action<TeamSide> OnGoalReceived;
        public event Action<int, int> OnMatchEndReceived;

        public TeamMember[] Players => _players;

        private readonly SoccerSnapshot _received = new SoccerSnapshot();
        private TackleReaction[] _reactions;
        private bool _active;
        private bool _isHost;
        private float _sendTimer;

        private static NetworkManager Network => NetworkManager.Singleton;

        /// <summary>接続完了後に呼ぶ。以降、役割に応じた送受信を行う</summary>
        public void Begin(bool isHost)
        {
            _isHost = isHost;
            _reactions = new TackleReaction[_players.Length];
            for (int i = 0; i < _players.Length; i++)
            {
                _reactions[i] = _players[i].GetComponent<TackleReaction>();
            }

            var messaging = Network.CustomMessagingManager;
            if (isHost)
            {
                messaging.RegisterNamedMessageHandler(MoveMessage, ReceiveMove);
                messaging.RegisterNamedMessageHandler(ButtonMessage, ReceiveButton);
            }
            else
            {
                messaging.RegisterNamedMessageHandler(SnapshotMessage, ReceiveSnapshot);
                messaging.RegisterNamedMessageHandler(TextMessage, ReceiveText);
                messaging.RegisterNamedMessageHandler(SeMessage, ReceiveSe);
                messaging.RegisterNamedMessageHandler(KickMessage, ReceiveKick);
                messaging.RegisterNamedMessageHandler(GoalMessage, ReceiveGoal);
                messaging.RegisterNamedMessageHandler(EndMessage, ReceiveEnd);
            }

            _active = true;
        }

        /// <summary>試合終了・切断後は送らない（リザルト表示中に相手へ入力や状態を流し続けないため）</summary>
        public void Stop()
        {
            _active = false;
        }

        private void OnDestroy()
        {
            _active = false;

            var messaging = Network != null ? Network.CustomMessagingManager : null;
            if (messaging == null) return;

            foreach (string name in new[] { SnapshotMessage, TextMessage, SeMessage, KickMessage, GoalMessage, EndMessage, MoveMessage, ButtonMessage })
            {
                messaging.UnregisterNamedMessageHandler(name);
            }
        }

        private void Update()
        {
            if (!_active) return;

            if (_isHost)
            {
                TickHost();
            }
            else
            {
                TickGuest();
            }
        }

        private void TickHost()
        {
            _sendTimer -= Time.unscaledDeltaTime;
            if (_sendTimer > 0f) return;

            _sendTimer = _snapshotInterval;
            SendSnapshot();
        }

        private void TickGuest()
        {
            SendButtonsPressedThisFrame();

            _sendTimer -= Time.unscaledDeltaTime;
            if (_sendTimer > 0f) return;

            _sendTimer = _moveSendInterval;
            SendMove();
        }

        // ---- ホスト → ゲスト ----

        private void SendSnapshot()
        {
            using var writer = new FastBufferWriter(SnapshotBufferSize, Allocator.Temp);
            writer.WriteValueSafe(Network.ServerTime.Time);
            writer.WriteValueSafe(_gameManager.RemainingSeconds);
            writer.WriteValueSafe((byte)_gameManager.HomeScore);
            writer.WriteValueSafe((byte)_gameManager.AwayScore);
            writer.WriteValueSafe((sbyte)IndexOf(_homeSwitcher.CurrentPlayer));
            writer.WriteValueSafe((sbyte)IndexOf(_awaySwitcher.CurrentPlayer));
            writer.WriteValueSafe(_ball.Position);
            writer.WriteValueSafe(_ball.Velocity);

            writer.WriteValueSafe((byte)_players.Length);
            for (int i = 0; i < _players.Length; i++)
            {
                var body = _players[i].GetComponent<Rigidbody2D>();
                writer.WriteValueSafe(body.position);
                writer.WriteValueSafe(body.linearVelocity);
                writer.WriteValueSafe(_reactions[i] != null && _reactions[i].IsStaggered);
            }

            // 位置は最新だけ届けばよいので再送せず、古いものが後から届いたら捨てる
            Send(SnapshotMessage, writer, NetworkDelivery.UnreliableSequenced);
        }

        /// <summary>画面中央のメッセージ（空文字で非表示）</summary>
        public void SendText(string text)
        {
            using var writer = new FastBufferWriter(SmallBufferSize, Allocator.Temp);
            writer.WriteValueSafe(text ?? string.Empty);
            Send(TextMessage, writer, NetworkDelivery.ReliableSequenced);
        }

        public void SendSe(SeId id)
        {
            using var writer = new FastBufferWriter(SmallBufferSize, Allocator.Temp);
            writer.WriteValueSafe((int)id);
            Send(SeMessage, writer, NetworkDelivery.ReliableSequenced);
        }

        /// <summary>キック音はパス/シュートで音程が変わるので速度ごと送る</summary>
        public void SendKick(float speed)
        {
            using var writer = new FastBufferWriter(SmallBufferSize, Allocator.Temp);
            writer.WriteValueSafe(speed);
            Send(KickMessage, writer, NetworkDelivery.ReliableSequenced);
        }

        public void SendGoal(TeamSide scoringTeam)
        {
            using var writer = new FastBufferWriter(SmallBufferSize, Allocator.Temp);
            writer.WriteValueSafe((int)scoringTeam);
            Send(GoalMessage, writer, NetworkDelivery.ReliableSequenced);
        }

        public void SendMatchEnd(int homeScore, int awayScore)
        {
            using var writer = new FastBufferWriter(SmallBufferSize, Allocator.Temp);
            writer.WriteValueSafe(homeScore);
            writer.WriteValueSafe(awayScore);
            Send(EndMessage, writer, NetworkDelivery.ReliableSequenced);
        }

        // ---- ゲスト → ホスト ----

        private void SendMove()
        {
            Vector2 move = InputManager.HasInstance ? InputManager.Instance.MoveVector : Vector2.zero;

            using var writer = new FastBufferWriter(SmallBufferSize, Allocator.Temp);
            writer.WriteValueSafe(move);
            Send(MoveMessage, writer, NetworkDelivery.UnreliableSequenced);
        }

        /// <summary>ボタンは取りこぼすと操作が効かないので、押した瞬間に再送ありで送る</summary>
        private void SendButtonsPressedThisFrame()
        {
            if (!InputManager.HasInstance) return;

            var input = InputManager.Instance;
            if (input.IsAction1Down) SendButton(SoccerButton.Pass);
            if (input.IsAction2Down) SendButton(SoccerButton.Shoot);
            if (input.IsAction3Down) SendButton(SoccerButton.Switch);
            if (input.IsAction4Down) SendButton(SoccerButton.Tackle);
        }

        private void SendButton(SoccerButton button)
        {
            using var writer = new FastBufferWriter(SmallBufferSize, Allocator.Temp);
            writer.WriteValueSafe((byte)button);
            Send(ButtonMessage, writer, NetworkDelivery.ReliableSequenced);
        }

        private void Send(string messageName, FastBufferWriter writer, NetworkDelivery delivery)
        {
            if (!_active || !OnlineSession.TryGetPeerId(out ulong peerId)) return;

            Network.CustomMessagingManager.SendNamedMessage(messageName, peerId, writer, delivery);
        }

        // ---- 受信（ゲスト） ----

        private void ReceiveSnapshot(ulong senderId, FastBufferReader reader)
        {
            reader.ReadValueSafe(out double sentTime);
            reader.ReadValueSafe(out _received.RemainingSeconds);
            reader.ReadValueSafe(out byte homeScore);
            reader.ReadValueSafe(out byte awayScore);
            reader.ReadValueSafe(out sbyte homeControlled);
            reader.ReadValueSafe(out sbyte awayControlled);
            reader.ReadValueSafe(out _received.BallPosition);
            reader.ReadValueSafe(out _received.BallVelocity);

            reader.ReadValueSafe(out byte count);
            if (_received.Players.Length != count)
            {
                _received.Players = new PlayerSnapshot[count];
            }

            for (int i = 0; i < count; i++)
            {
                reader.ReadValueSafe(out _received.Players[i].Position);
                reader.ReadValueSafe(out _received.Players[i].Velocity);
                reader.ReadValueSafe(out _received.Players[i].IsStaggered);
            }

            _received.HomeScore = homeScore;
            _received.AwayScore = awayScore;
            _received.HomeControlledIndex = homeControlled;
            _received.AwayControlledIndex = awayControlled;
            _received.Age = Mathf.Clamp((float)(Network.ServerTime.Time - sentTime), 0f, _maxLatencyCompensation);

            OnSnapshotReceived?.Invoke(_received);
        }

        private void ReceiveText(ulong senderId, FastBufferReader reader)
        {
            reader.ReadValueSafe(out string text);
            OnTextReceived?.Invoke(text);
        }

        private void ReceiveSe(ulong senderId, FastBufferReader reader)
        {
            reader.ReadValueSafe(out int id);
            OnSeReceived?.Invoke((SeId)id);
        }

        private void ReceiveKick(ulong senderId, FastBufferReader reader)
        {
            reader.ReadValueSafe(out float speed);
            OnKickReceived?.Invoke(speed);
        }

        private void ReceiveGoal(ulong senderId, FastBufferReader reader)
        {
            reader.ReadValueSafe(out int scoringTeam);
            OnGoalReceived?.Invoke((TeamSide)scoringTeam);
        }

        private void ReceiveEnd(ulong senderId, FastBufferReader reader)
        {
            reader.ReadValueSafe(out int homeScore);
            reader.ReadValueSafe(out int awayScore);
            OnMatchEndReceived?.Invoke(homeScore, awayScore);
        }

        // ---- 受信（ホスト） ----

        private void ReceiveMove(ulong senderId, FastBufferReader reader)
        {
            reader.ReadValueSafe(out Vector2 move);
            _remoteInput.SetMove(move);
        }

        private void ReceiveButton(ulong senderId, FastBufferReader reader)
        {
            reader.ReadValueSafe(out byte button);
            _remoteInput.Press((SoccerButton)button);
        }

        private int IndexOf(Transform player)
        {
            if (player == null) return -1;

            for (int i = 0; i < _players.Length; i++)
            {
                if (_players[i].transform == player) return i;
            }

            return -1;
        }
    }
}
