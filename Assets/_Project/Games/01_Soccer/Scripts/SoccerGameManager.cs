using System.Collections;
using MiniGame.Common.Audio;
using MiniGame.Common.Core;
using MiniGame.Common.Input;
using MiniGame.Common.Online;
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
            /// <summary>CPU戦（従来どおり）</summary>
            Offline,

            /// <summary>オンラインのホスト。HOMEを操作し、試合全体を計算する</summary>
            Host,

            /// <summary>オンラインのゲスト。AWAYを操作し、表示だけ行う</summary>
            Guest
        }

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
        private bool IsGuest => _role == MatchRole.Guest;

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

        protected override void OnGameReady()
        {
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
                // AWAYの操作選手はCPUではなく、ゲストから届いた入力で動かす
                _awaySwitcher.SetInput(_remoteInput);
                _awaySwitcher.enabled = true;

                ResetPositions();
                StartCoroutine(KickOffRoutine());
            }
            else
            {
                // キックオフ等の進行はホストから届くので、ここでは表示の準備だけして待つ
                _guestView.Begin();
                StartGame();
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

        private IEnumerator TimeUpRoutine()
        {
            _isSequenceRunning = true;
            ChangeState(MiniGameState.Event);

            PlaySe(SeId.Whistle);

            yield return StartCoroutine(ShowMessageRoutine("TIME UP", _timeUpMessageDuration));

            if (_role == MatchRole.Host)
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

            FinishGame(myScore > opponentScore, $"HOME {homeScore} - {awayScore} AWAY", BuildResultDetail(myScore, opponentScore));
        }

        private static string BuildResultDetail(int myScore, int opponentScore)
        {
            if (myScore > opponentScore) return "勝利！";
            if (myScore < opponentScore) return "敗北...";
            return "引き分け";
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

            if (InputManager.HasInstance)
            {
                InputManager.Instance.InputEnabled = false;
            }
        }

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

        /// <summary>
        /// バグ等で選手・ボールが動かなくなり試合が続行不能になった場合の救済措置。
        /// ボールの速度がほぼ0の状態が一定時間続いたら、ゴール時と同じリセットで復帰する
        /// </summary>
        private void MonitorStuckBall()
        {
            if (!IsPlaying || _ball == null || _isSequenceRunning)
            {
                _stuckTimer = 0f;
                return;
            }

            if (_ball.Velocity.sqrMagnitude <= _stuckSpeedThreshold * _stuckSpeedThreshold)
            {
                _stuckTimer += Time.deltaTime;
                if (_stuckTimer >= _stuckTimeLimit)
                {
                    _stuckTimer = 0f;
                    StartCoroutine(StuckRecoveryRoutine());
                }
            }
            else
            {
                _stuckTimer = 0f;
            }
        }

        private IEnumerator StuckRecoveryRoutine()
        {
            _isSequenceRunning = true;
            ChangeState(MiniGameState.Event);

            yield return StartCoroutine(ShowMessageRoutine("RESET", _stuckMessageDuration));

            ResetPositions();
            yield return StartCoroutine(KickOffRoutine());

            _isSequenceRunning = false;
        }

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

            if (scoringTeam == TeamSide.Home)
            {
                _homeScore++;
            }
            else
            {
                _awayScore++;
            }
            UpdateScoreText();

            // ゴールの揺れ演出は GoalTrigger が鳴らし済みなので、ここでは歓声と画面演出だけ
            PlaySeLocal(SeId.GoalCheer);
            if (_goalEffect != null)
            {
                _goalEffect.Play();
            }

            if (_role == MatchRole.Host)
            {
                _onlineLink.SendGoal(scoringTeam);
            }

            string scorerLabel = scoringTeam == TeamSide.Home ? "HOME" : "AWAY";
            yield return StartCoroutine(ShowMessageRoutine($"GOAL! ({scorerLabel})", _goalMessageDuration));

            ResetPositions();
            yield return StartCoroutine(KickOffRoutine());

            _isSequenceRunning = false;
        }

        private IEnumerator KickOffRoutine()
        {
            ChangeState(MiniGameState.Countdown);

            PlaySe(SeId.Whistle);

            yield return StartCoroutine(ShowMessageRoutine("KICK OFF!", _kickOffMessageDuration));
            StartGame();
        }

        private IEnumerator ShowMessageRoutine(string message, float duration)
        {
            ShowMessage(message);
            yield return new WaitForSeconds(duration);
            ShowMessage(string.Empty);
        }

        /// <summary>ホストでは同じメッセージをゲストの画面にも出す</summary>
        private void ShowMessage(string message)
        {
            SetMessage(message);

            if (_role == MatchRole.Host)
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

        /// <summary>ホストでは同じSEをゲストでも鳴らす</summary>
        private void PlaySe(SeId id)
        {
            PlaySeLocal(id);

            if (_role == MatchRole.Host)
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
            if (_role == MatchRole.Host)
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
            PlaySeLocal(SeId.GoalCheer);

            if (_goalEffect != null)
            {
                _goalEffect.Play();
            }

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

        /// <summary>試合中に相手との接続が切れたら、そこで試合を打ち切る</summary>
        private void HandlePeerDisconnected()
        {
            if (!IsOnline || CurrentState == MiniGameState.Result) return;

            StopAllCoroutines();
            _isSequenceRunning = false;
            SetMessage(string.Empty);

            FinishGame(false, $"HOME {_homeScore} - {_awayScore} AWAY", "相手との接続が切れました");
        }

        // ---- 表示 ----

        private void UpdateScoreText()
        {
            if (_scoreText == null) return;

            // オンラインではどちらのチームを操作しているか分かるよう、自分側に YOU を付ける
            string homeLabel = _role == MatchRole.Host ? "HOME(YOU)" : "HOME";
            string awayLabel = IsGuest ? "AWAY(YOU)" : "AWAY";
            _scoreText.text = $"{homeLabel} {_homeScore} - {_awayScore} {awayLabel}";
        }

        private void UpdateTimerText()
        {
            if (_timerText == null) return;

            // 残り0.1秒でも「1」と見せたいので切り上げる
            int totalSeconds = Mathf.CeilToInt(_remainingSeconds);
            _timerText.text = $"{totalSeconds / 60}:{totalSeconds % 60:00}";
        }

        private void ResetPositions()
        {
            if (_ball != null)
            {
                _ball.ResetBall(_ballStartPosition);
            }

            // 22人全員をフォーメーションの基準ポジションへ戻す（Phase 5: 11 vs 11）
            var aiPlayers = Object.FindObjectsByType<AIPlayerController>(FindObjectsSortMode.None);
            foreach (var ai in aiPlayers)
            {
                ai.ResetToHomePosition();
            }

            // 操作対象をキックオフ時の選手へ戻す（Phase 6: 選手切り替え）
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
    }
}
