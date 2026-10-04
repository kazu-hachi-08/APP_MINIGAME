using System.Collections;
using MiniGame.Common.Audio;
using MiniGame.Common.Core;
using MiniGame.Common.Input;
using MiniGame.Common.Online;
using MiniGame.Common.Profile;
using MiniGame.Common.UI;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.Soccer
{
    /// <summary>
    /// サッカーゲームのゲームマネージャー
    /// キックオフ → プレイ → ゴール → リセット → キックオフ、のサイクルとHOME/AWAY両チームの得点表示を管理する。
    ///
    /// オンライン対戦ではホスト（HOME）だけがこの進行を計算し、ゲスト（AWAY）はホストから届く状態・演出を表示するだけにする。
    /// </summary>
    public class SoccerGameManager : BaseMiniGameManager
    {
        /// <summary>この端末が試合でどの立場か</summary>
        private enum MatchRole
        {
            /// <summary>CPU戦</summary>
            Offline,

            /// <summary>オンラインのホスト。HOMEを操作し、試合全体を計算する</summary>
            Host,

            /// <summary>オンラインのゲスト。AWAYを操作し、表示だけ行う</summary>
            Guest
        }

        private const string HomeLabel = "HOME";
        private const string AwayLabel = "AWAY";
        private const string YouSuffix = "(YOU)";

        private const string KickOffMessage = "KICK OFF!";
        private const string TimeUpMessage = "TIME UP";
        private const string StuckResetMessage = "RESET";

        private const string WinDetail = "勝利！";
        private const string LoseDetail = "敗北...";
        private const string DrawDetail = "引き分け";
        private const string DisconnectedDetail = "相手との接続が切れました";

        private const int SecondsPerMinute = 60;

        [Header("Soccer References")]
        [SerializeField] private Ball _ball;
        [SerializeField] private PlayerSwitcher _playerSwitcher;
        [SerializeField] private Vector2 _ballStartPosition;

        [Header("Online（未設定ならCPU戦のみ）")]
        [SerializeField] private ModeSelectPanel _modeSelectPanel;
        [SerializeField] private OnlineSession _onlineSession;
        [SerializeField] private SoccerOnlineLink _onlineLink;
        [SerializeField] private SoccerGuestView _guestView;
        [SerializeField] private PlayerSwitcher _awaySwitcher;
        [SerializeField] private RemoteInputProvider _remoteInput;

        [Header("UI")]
        [SerializeField] private Text _messageText;
        [SerializeField] private Text _scoreText;
        [SerializeField] private Text _timerText;
        [SerializeField] private GoalEffect _goalEffect;

        [Header("Match Time")]
        [SerializeField] private float _matchDurationSeconds = 120f;

        [Header("Timing")]
        [SerializeField] private float _kickOffMessageDuration = 1.0f;
        [SerializeField] private float _goalMessageDuration = 1.5f;
        [SerializeField] private float _timeUpMessageDuration = 1.5f;

        [Header("Stuck Ball Recovery")]
        [SerializeField] private float _stuckSpeedThreshold = 0.15f;
        [SerializeField] private float _stuckTimeLimit = 4f;
        [SerializeField] private float _stuckMessageDuration = 1.0f;

        private int _homeScore;
        private int _awayScore;
        private bool _isSequenceRunning;
        private float _stuckTimer;
        private float _remainingSeconds;
        private MatchRole _role = MatchRole.Offline;
        private bool _isOnlinePauseOpen;

        public int HomeScore => _homeScore;
        public int AwayScore => _awayScore;
        public float RemainingSeconds => _remainingSeconds;

        private bool IsOnline => _role != MatchRole.Offline;
        private bool IsHost => _role == MatchRole.Host;
        private bool IsGuest => _role == MatchRole.Guest;

        /// <summary>CPU戦は自分が操作する HOME をユーザー名にする。オンラインは名前交換（Phase 3）までは従来の HOME</summary>
        private string HomeName => IsOnline ? HomeLabel : SeatNames.Get(0);

        // ---- Unity ライフサイクル ----

        private void OnEnable()
        {
            if (_onlineSession != null)
            {
                _onlineSession.OnPeerDisconnected += HandlePeerDisconnected;
            }

            if (_onlineLink != null)
            {
                _onlineLink.OnSnapshotReceived += HandleSnapshot;
                _onlineLink.OnTextReceived += SetMessage;
                _onlineLink.OnSeReceived += PlaySeLocal;
                _onlineLink.OnKickReceived += HandleRemoteKick;
                _onlineLink.OnGoalReceived += PlayGoalEffects;
                _onlineLink.OnMatchEndReceived += HandleRemoteMatchEnd;
            }

            if (_ball != null)
            {
                _ball.OnKicked += HandleBallKicked;
            }
        }

        private void OnDisable()
        {
            if (_onlineSession != null)
            {
                _onlineSession.OnPeerDisconnected -= HandlePeerDisconnected;
            }

            if (_onlineLink != null)
            {
                _onlineLink.OnSnapshotReceived -= HandleSnapshot;
                _onlineLink.OnTextReceived -= SetMessage;
                _onlineLink.OnSeReceived -= PlaySeLocal;
                _onlineLink.OnKickReceived -= HandleRemoteKick;
                _onlineLink.OnGoalReceived -= PlayGoalEffects;
                _onlineLink.OnMatchEndReceived -= HandleRemoteMatchEnd;
            }

            if (_ball != null)
            {
                _ball.OnKicked -= HandleBallKicked;
            }
        }

        protected override void Update()
        {
            base.Update();

            // ゲストの時間・停滞監視はホストが行い、結果だけが届く
            if (IsGuest) return;

            TickMatchTimer();
            MonitorStuckBall();
        }

        // ---- 試合開始 ----

        protected override void OnGameReady()
        {
            // 既定はオフライン。オンラインを選んだら OnlineSession が席名を差し替える
            SeatNames.UseLocal();
            _remainingSeconds = _matchDurationSeconds;
            ResetPositions();
            UpdateScoreText();
            UpdateTimerText();

            if (_modeSelectPanel != null)
            {
                _modeSelectPanel.Show(StartOfflineMatch, StartOnlineMatch);
            }
            else
            {
                StartOfflineMatch();
            }
        }

        private void StartOfflineMatch()
        {
            StartCoroutine(KickOffRoutine());
        }

        /// <summary>
        /// 相手と接続できたらオンライン対戦を始める。ホストはHOME、ゲストはAWAYを操作する
        /// </summary>
        private void StartOnlineMatch(bool isHost)
        {
            _role = isHost ? MatchRole.Host : MatchRole.Guest;
            _onlineLink.Begin(isHost);
            UpdateScoreText();

            if (isHost)
            {
                StartAsHost();
            }
            else
            {
                StartAsGuest();
            }
        }

        private void StartAsHost()
        {
            // AWAYの操作選手はCPUではなく、ゲストから届いた入力で動かす
            _awaySwitcher.SetInput(_remoteInput);
            _awaySwitcher.enabled = true;

            ResetPositions();
            StartCoroutine(KickOffRoutine());
        }

        private void StartAsGuest()
        {
            // キックオフ等の進行はホストから届くので、ここでは表示の準備だけして待つ
            _guestView.Begin();
            StartGame();
        }

        // ---- 試合進行（時間・停滞監視） ----

        /// <summary>
        /// 試合時間のカウントダウン（プレイ中のみ進み、ゴール演出中などは止まる）
        /// </summary>
        private void TickMatchTimer()
        {
            if (!IsPlaying || _isSequenceRunning) return;

            _remainingSeconds -= Time.deltaTime;
            if (_remainingSeconds <= 0f)
            {
                _remainingSeconds = 0f;
                StartCoroutine(TimeUpRoutine());
            }

            UpdateTimerText();
        }

        /// <summary>
        /// バグ等で選手・ボールが動かなくなり試合が続行不能になった場合の救済措置。
        /// ボールの速度がほぼ0の状態が一定時間続いたら、ゴール時と同じリセットで復帰する
        /// </summary>
        private void MonitorStuckBall()
        {
            if (!IsPlaying || _ball == null || _isSequenceRunning || !IsBallAlmostStopped())
            {
                _stuckTimer = 0f;
                return;
            }

            _stuckTimer += Time.deltaTime;
            if (_stuckTimer >= _stuckTimeLimit)
            {
                _stuckTimer = 0f;
                StartCoroutine(StuckRecoveryRoutine());
            }
        }

        private bool IsBallAlmostStopped()
        {
            return _ball.Velocity.sqrMagnitude <= _stuckSpeedThreshold * _stuckSpeedThreshold;
        }

        private IEnumerator StuckRecoveryRoutine()
        {
            _isSequenceRunning = true;
            ChangeState(MiniGameState.Event);

            yield return StartCoroutine(ShowMessageRoutine(StuckResetMessage, _stuckMessageDuration));

            ResetPositions();
            yield return StartCoroutine(KickOffRoutine());

            _isSequenceRunning = false;
        }

        // ---- ゴール・キックオフ ----

        /// <summary>
        /// GoalTrigger からゴール検知時に呼び出される
        /// </summary>
        public void OnGoalScored(TeamSide scoringTeam)
        {
            if (!IsPlaying || _isSequenceRunning || IsGuest) return;
            StartCoroutine(GoalRoutine(scoringTeam));
        }

        private IEnumerator GoalRoutine(TeamSide scoringTeam)
        {
            _isSequenceRunning = true;
            ChangeState(MiniGameState.Event);

            AddScore(scoringTeam);
            UpdateScoreText();

            // ゴールの揺れ演出は GoalTrigger が鳴らし済みなので、ここでは歓声と画面演出だけ
            PlayGoalCheerAndEffect();

            if (IsHost)
            {
                _onlineLink.SendGoal(scoringTeam);
            }

            string scorerLabel = scoringTeam == TeamSide.Home ? HomeName : AwayLabel;
            yield return StartCoroutine(ShowMessageRoutine($"GOAL! ({scorerLabel})", _goalMessageDuration));

            ResetPositions();
            yield return StartCoroutine(KickOffRoutine());

            _isSequenceRunning = false;
        }

        private void AddScore(TeamSide scoringTeam)
        {
            if (scoringTeam == TeamSide.Home)
            {
                _homeScore++;
            }
            else
            {
                _awayScore++;
            }
        }

        private void PlayGoalCheerAndEffect()
        {
            PlaySeLocal(SeId.GoalCheer);

            if (_goalEffect != null)
            {
                _goalEffect.Play();
            }
        }

        private IEnumerator KickOffRoutine()
        {
            ChangeState(MiniGameState.Countdown);

            PlaySe(SeId.Whistle);

            yield return StartCoroutine(ShowMessageRoutine(KickOffMessage, _kickOffMessageDuration));
            StartGame();
        }

        private void ResetPositions()
        {
            if (_ball != null)
            {
                _ball.ResetBall(_ballStartPosition);
            }

            // キックオフを毎回同じ陣形から始めるため、両チーム全員を基準ポジションへ戻す
            var aiPlayers = Object.FindObjectsByType<AIPlayerController>(FindObjectsSortMode.None);
            foreach (var ai in aiPlayers)
            {
                ai.ResetToHomePosition();
            }

            // キックオフ時に操作する選手を毎回同じにし、直前の操作対象を引きずらないようにする
            if (_playerSwitcher != null)
            {
                _playerSwitcher.ResetPlayers();
            }

            // AWAYの切り替えはオンラインのホストでだけ動いている
            if (_awaySwitcher != null && _awaySwitcher.enabled)
            {
                _awaySwitcher.ResetPlayers();
            }
        }

        // ---- 試合終了 ----

        private IEnumerator TimeUpRoutine()
        {
            _isSequenceRunning = true;
            ChangeState(MiniGameState.Event);

            PlaySe(SeId.Whistle);

            yield return StartCoroutine(ShowMessageRoutine(TimeUpMessage, _timeUpMessageDuration));

            if (IsHost)
            {
                _onlineLink.SendMatchEnd(_homeScore, _awayScore);
            }

            FinishMatch(_homeScore, _awayScore);
        }

        /// <summary>
        /// 勝敗表示とリザルト画面の表示は BaseMiniGameManager に任せる。
        /// ゲストはAWAYなので、自分と相手の得点を入れ替えて判定する
        /// </summary>
        private void FinishMatch(int homeScore, int awayScore)
        {
            int myScore = IsGuest ? awayScore : homeScore;
            int opponentScore = IsGuest ? homeScore : awayScore;

            FinishGame(myScore > opponentScore, BuildScoreSummary(homeScore, awayScore), BuildResultDetail(myScore, opponentScore));
        }

        private string BuildScoreSummary(int homeScore, int awayScore)
        {
            return $"{HomeName} {homeScore} - {awayScore} {AwayLabel}";
        }

        private static string BuildResultDetail(int myScore, int opponentScore)
        {
            if (myScore > opponentScore) return WinDetail;
            if (myScore < opponentScore) return LoseDetail;
            return DrawDetail;
        }

        protected override void OnGameOver(bool isVictory)
        {
            if (IsOnline)
            {
                _onlineLink.Stop();
                ClosePauseDialogIfOpen();
            }

            // 試合終了後もボールと選手が動き続けないよう入力とボールを止める
            // （ゲストはボールの物理を止めているので触らない）
            if (_ball != null && !IsGuest)
            {
                _ball.ResetBall(_ball.Position);
            }

            if (_remoteInput != null)
            {
                _remoteInput.Clear();
            }

            SetLocalInputEnabled(false);
        }

        // ---- ポーズ ----

        /// <summary>
        /// オンラインでは相手の端末は止まらないため、試合は止めずに自分の操作だけ止めてPAUSE画面を出す
        /// </summary>
        public override void PauseGame()
        {
            if (!IsOnline)
            {
                base.PauseGame();
                return;
            }

            // ポーズキーをもう一度押したら閉じる（オンラインでは状態が Paused にならず ResumeGame が呼ばれないため）
            if (_isOnlinePauseOpen)
            {
                ClosePauseDialogIfOpen();
                return;
            }

            if (!UIManager.HasInstance) return;

            _isOnlinePauseOpen = true;
            SetLocalInputEnabled(false);
            UIManager.Instance.ShowPauseDialog(
                onResume: ClosePauseDialogIfOpen,
                onRestart: RestartGame,
                onTitle: ReturnToTitle
            );
        }

        private void ClosePauseDialogIfOpen()
        {
            if (!_isOnlinePauseOpen) return;

            _isOnlinePauseOpen = false;
            SetLocalInputEnabled(true);

            if (UIManager.HasInstance)
            {
                UIManager.Instance.HidePauseDialog();
            }
        }

        private static void SetLocalInputEnabled(bool enabled)
        {
            if (InputManager.HasInstance)
            {
                InputManager.Instance.InputEnabled = enabled;
            }
        }

        // ---- メッセージ・SE（ホストはゲストにも同じものを出す） ----

        private IEnumerator ShowMessageRoutine(string message, float duration)
        {
            ShowMessage(message);
            yield return new WaitForSeconds(duration);
            ShowMessage(string.Empty);
        }

        private void ShowMessage(string message)
        {
            SetMessage(message);

            if (IsHost)
            {
                _onlineLink.SendText(message);
            }
        }

        private void SetMessage(string message)
        {
            if (_messageText == null) return;

            bool visible = !string.IsNullOrEmpty(message);
            _messageText.text = visible ? message : string.Empty;
            _messageText.gameObject.SetActive(visible);
        }

        private void PlaySe(SeId id)
        {
            PlaySeLocal(id);

            if (IsHost)
            {
                _onlineLink.SendSe(id);
            }
        }

        private static void PlaySeLocal(SeId id)
        {
            if (AudioManager.HasInstance)
            {
                AudioManager.Instance.PlaySe(id);
            }
        }

        // ---- オンライン（ホスト） ----

        /// <summary>キック音は Ball がホストで鳴らすので、ゲストにも同じ音を鳴らしてもらう</summary>
        private void HandleBallKicked(float speed)
        {
            if (IsHost)
            {
                _onlineLink.SendKick(speed);
            }
        }

        // ---- オンライン（ゲスト） ----

        private void HandleSnapshot(SoccerSnapshot snapshot)
        {
            bool scoreChanged = snapshot.HomeScore != _homeScore || snapshot.AwayScore != _awayScore;
            _homeScore = snapshot.HomeScore;
            _awayScore = snapshot.AwayScore;
            _remainingSeconds = snapshot.RemainingSeconds;

            if (scoreChanged) UpdateScoreText();
            UpdateTimerText();
        }

        private void HandleRemoteKick(float speed)
        {
            if (_ball != null)
            {
                _ball.PlayKickSe(speed);
            }
        }

        /// <summary>ゴール演出（ゲストはゴール判定をしないので、ゴールの揺れもここで出す）</summary>
        private void PlayGoalEffects(TeamSide scoringTeam)
        {
            PlayGoalCheerAndEffect();

            foreach (var goal in Object.FindObjectsByType<GoalTrigger>(FindObjectsSortMode.None))
            {
                if (goal.DefendingTeam != scoringTeam) goal.PlayReaction();
            }
        }

        private void HandleRemoteMatchEnd(int homeScore, int awayScore)
        {
            if (!IsGuest || CurrentState == MiniGameState.Result) return;

            _homeScore = homeScore;
            _awayScore = awayScore;
            UpdateScoreText();
            FinishMatch(homeScore, awayScore);
        }

        // ---- オンライン（共通） ----

        /// <summary>試合中に相手との接続が切れたら、そこで試合を打ち切る</summary>
        private void HandlePeerDisconnected()
        {
            if (!IsOnline || CurrentState == MiniGameState.Result) return;

            StopAllCoroutines();
            _isSequenceRunning = false;
            SetMessage(string.Empty);

            FinishGame(false, BuildScoreSummary(_homeScore, _awayScore), DisconnectedDetail);
        }

        // ---- 表示 ----

        private void UpdateScoreText()
        {
            if (_scoreText == null) return;

            // オンラインではどちらのチームを操作しているか分かるよう、自分側に YOU を付ける
            string homeLabel = IsHost ? HomeLabel + YouSuffix : HomeName;
            string awayLabel = IsGuest ? AwayLabel + YouSuffix : AwayLabel;
            _scoreText.text = $"{homeLabel} {_homeScore} - {_awayScore} {awayLabel}";
        }

        private void UpdateTimerText()
        {
            if (_timerText == null) return;

            // 残り0.1秒でも「1」と見せたいので切り上げる
            int totalSeconds = Mathf.CeilToInt(_remainingSeconds);
            _timerText.text = $"{totalSeconds / SecondsPerMinute}:{totalSeconds % SecondsPerMinute:00}";
        }
    }
}
