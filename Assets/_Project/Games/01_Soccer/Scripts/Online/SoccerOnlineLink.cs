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

        /// <summary>位置や移動入力は最新だけ届けばよいので再送せず、古いものが後から届いたら捨てる</summary>
        private const NetworkDelivery StreamDelivery = NetworkDelivery.UnreliableSequenced;

        /// <summary>メッセージ・SE・ボタンなどは取りこぼすと演出や操作が抜けるので、再送ありで順番どおりに届ける</summary>
        private const NetworkDelivery EventDelivery = NetworkDelivery.ReliableSequenced;

        /// <summary>操作中の選手が見つからないとき。受信側は範囲外の番号として無視する</summary>
        private const int NoPlayerIndex = -1;

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
        private Rigidbody2D[] _playerBodies;
        private TackleReaction[] _playerReactions;
        private bool _active;
        private bool _isHost;
        private float _sendTimer;

        private static NetworkManager Network => NetworkManager.Singleton;

        /// <summary>接続完了後に呼ぶ。以降、役割に応じた送受信を行う</summary>
        public void Begin(bool isHost)
        {
            _isHost = isHost;
            CachePlayerComponents();

            var messaging = Network.CustomMessagingManager;
            if (isHost)
            {
                RegisterHostHandlers(messaging);
            }
            else
            {
                RegisterGuestHandlers(messaging);
            }

            _active = true;
        }

        /// <summary>試合終了・切断後は送らない（リザルト表示中に相手へ入力や状態を流し続けないため）</summary>
        public void Stop()
        {
            _active = false;
        }

        /// <summary>スナップショットは毎秒約30回×22人ぶん読むので、GetComponent を毎回呼ばないよう先に集めておく</summary>
        private void CachePlayerComponents()
        {
            _playerBodies = new Rigidbody2D[_players.Length];
            _playerReactions = new TackleReaction[_players.Length];
            for (int i = 0; i < _players.Length; i++)
            {
                _playerBodies[i] = _players[i].GetComponent<Rigidbody2D>();
                _playerReactions[i] = _players[i].GetComponent<TackleReaction>();
            }
        }

        private void RegisterHostHandlers(CustomMessagingManager messaging)
        {
            messaging.RegisterNamedMessageHandler(MoveMessage, ReceiveMove);
            messaging.RegisterNamedMessageHandler(ButtonMessage, ReceiveButton);
        }

        private void RegisterGuestHandlers(CustomMessagingManager messaging)
        {
            messaging.RegisterNamedMessageHandler(SnapshotMessage, ReceiveSnapshot);
            messaging.RegisterNamedMessageHandler(TextMessage, ReceiveText);
            messaging.RegisterNamedMessageHandler(SeMessage, ReceiveSe);
            messaging.RegisterNamedMessageHandler(KickMessage, ReceiveKick);
            messaging.RegisterNamedMessageHandler(GoalMessage, ReceiveGoal);
            messaging.RegisterNamedMessageHandler(EndMessage, ReceiveEnd);
        }

        private void OnDestroy()
        {
            _active = false;

            var messaging = Network != null ? Network.CustomMessagingManager : null;
            if (messaging == null) return;

            // 役割によって登録していないものもあるが、未登録の解除は何もしないので全部まとめて外す
            messaging.UnregisterNamedMessageHandler(SnapshotMessage);
            messaging.UnregisterNamedMessageHandler(TextMessage);
            messaging.UnregisterNamedMessageHandler(SeMessage);
            messaging.UnregisterNamedMessageHandler(KickMessage);
            messaging.UnregisterNamedMessageHandler(GoalMessage);
            messaging.UnregisterNamedMessageHandler(EndMessage);
            messaging.UnregisterNamedMessageHandler(MoveMessage);
            messaging.UnregisterNamedMessageHandler(ButtonMessage);
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
            if (!ConsumeSendTimer(_snapshotInterval)) return;

            SendSnapshot();
        }

        private void TickGuest()
        {
            SendButtonsPressedThisFrame();

            if (!ConsumeSendTimer(_moveSendInterval)) return;

            SendMove();
        }

        /// <summary>
        /// 送る間隔が来たら true を返してタイマーを戻す。
        /// ポーズ等で timeScale が変わっても通信の頻度は保ちたいので unscaled で数える
        /// </summary>
        private bool ConsumeSendTimer(float interval)
        {
            _sendTimer -= Time.unscaledDeltaTime;
            if (_sendTimer > 0f) return false;

            _sendTimer = interval;
            return true;
        }

        // ---- 送信：ホスト → ゲスト ----
        // スナップショットの並び（受信側 ReceiveSnapshot と必ずそろえる）:
        //   double 送信時刻 / float 残り時間 / byte HOME得点 / byte AWAY得点 /
        //   sbyte HOME操作選手 / sbyte AWAY操作選手 / Vector2 ボール位置 / Vector2 ボール速度 /
        //   byte 選手数 / 選手ごとに（Vector2 位置 / Vector2 速度 / bool 転倒中）

        private void SendSnapshot()
        {
            using var writer = new FastBufferWriter(SnapshotBufferSize, Allocator.Temp);
            WriteMatchState(writer);
            WritePlayers(writer);
            Send(SnapshotMessage, writer, StreamDelivery);
        }

        private void WriteMatchState(FastBufferWriter writer)
        {
            writer.WriteValueSafe(Network.ServerTime.Time);
            writer.WriteValueSafe(_gameManager.RemainingSeconds);
            writer.WriteValueSafe((byte)_gameManager.HomeScore);
            writer.WriteValueSafe((byte)_gameManager.AwayScore);
            writer.WriteValueSafe((sbyte)IndexOf(_homeSwitcher.CurrentPlayer));
            writer.WriteValueSafe((sbyte)IndexOf(_awaySwitcher.CurrentPlayer));
            writer.WriteValueSafe(_ball.Position);
            writer.WriteValueSafe(_ball.Velocity);
        }

        private void WritePlayers(FastBufferWriter writer)
        {
            writer.WriteValueSafe((byte)_players.Length);
            for (int i = 0; i < _players.Length; i++)
            {
                Rigidbody2D body = _playerBodies[i];
                writer.WriteValueSafe(body.position);
                writer.WriteValueSafe(body.linearVelocity);
                writer.WriteValueSafe(_playerReactions[i] != null && _playerReactions[i].IsStaggered);
            }
        }

        /// <summary>画面中央のメッセージ（空文字で非表示）</summary>
        public void SendText(string text)
        {
            using var writer = new FastBufferWriter(SmallBufferSize, Allocator.Temp);
            writer.WriteValueSafe(text ?? string.Empty);
            Send(TextMessage, writer, EventDelivery);
        }

        public void SendSe(SeId id)
        {
            using var writer = new FastBufferWriter(SmallBufferSize, Allocator.Temp);
            writer.WriteValueSafe((int)id);
            Send(SeMessage, writer, EventDelivery);
        }

        /// <summary>キック音はパス/シュートで音程が変わるので速度ごと送る</summary>
        public void SendKick(float speed)
        {
            using var writer = new FastBufferWriter(SmallBufferSize, Allocator.Temp);
            writer.WriteValueSafe(speed);
            Send(KickMessage, writer, EventDelivery);
        }

        public void SendGoal(TeamSide scoringTeam)
        {
            using var writer = new FastBufferWriter(SmallBufferSize, Allocator.Temp);
            writer.WriteValueSafe((int)scoringTeam);
            Send(GoalMessage, writer, EventDelivery);
        }

        public void SendMatchEnd(int homeScore, int awayScore)
        {
            using var writer = new FastBufferWriter(SmallBufferSize, Allocator.Temp);
            writer.WriteValueSafe(homeScore);
            writer.WriteValueSafe(awayScore);
            Send(EndMessage, writer, EventDelivery);
        }

        // ---- 送信：ゲスト → ホスト ----

        private void SendMove()
        {
            Vector2 move = InputManager.HasInstance ? InputManager.Instance.MoveVector : Vector2.zero;

            using var writer = new FastBufferWriter(SmallBufferSize, Allocator.Temp);
            writer.WriteValueSafe(move);
            Send(MoveMessage, writer, StreamDelivery);
        }

        /// <summary>ボタンは取りこぼすと操作が効かないので、移動入力の送信間隔を待たず押した瞬間に送る</summary>
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
            Send(ButtonMessage, writer, EventDelivery);
        }

        private void Send(string messageName, FastBufferWriter writer, NetworkDelivery delivery)
        {
            if (!_active || !OnlineSession.TryGetPeerId(out ulong peerId)) return;

            Network.CustomMessagingManager.SendNamedMessage(messageName, peerId, writer, delivery);
        }

        // ---- 受信：ゲスト ----

        private void ReceiveSnapshot(ulong senderId, FastBufferReader reader)
        {
            double sentTime = ReadMatchState(reader);
            ReadPlayers(reader);

            _received.Age = Mathf.Clamp((float)(Network.ServerTime.Time - sentTime), 0f, _maxLatencyCompensation);
            OnSnapshotReceived?.Invoke(_received);
        }

        /// <returns>ホストが送った時刻（遅延の計算に使う）</returns>
        private double ReadMatchState(FastBufferReader reader)
        {
            reader.ReadValueSafe(out double sentTime);
            reader.ReadValueSafe(out _received.RemainingSeconds);
            reader.ReadValueSafe(out byte homeScore);
            reader.ReadValueSafe(out byte awayScore);
            reader.ReadValueSafe(out sbyte homeControlled);
            reader.ReadValueSafe(out sbyte awayControlled);
            reader.ReadValueSafe(out _received.BallPosition);
            reader.ReadValueSafe(out _received.BallVelocity);

            _received.HomeScore = homeScore;
            _received.AwayScore = awayScore;
            _received.HomeControlledIndex = homeControlled;
            _received.AwayControlledIndex = awayControlled;
            return sentTime;
        }

        private void ReadPlayers(FastBufferReader reader)
        {
            reader.ReadValueSafe(out byte count);

            // 毎回配列を作り直さず、人数が変わったときだけ確保し直す
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

        // ---- 受信：ホスト ----

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
            if (player == null) return NoPlayerIndex;

            for (int i = 0; i < _players.Length; i++)
            {
                if (_players[i].transform == player) return i;
            }

            return NoPlayerIndex;
        }
    }
}
