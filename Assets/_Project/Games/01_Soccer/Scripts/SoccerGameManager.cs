using System.Collections;
using MiniGame.Common.Audio;
using MiniGame.Common.Core;
using MiniGame.Common.Input;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.Soccer
{
    /// <summary>
    /// サッカーゲームのゲームマネージャー
    /// キックオフ → プレイ → ゴール → リセット → キックオフ、のサイクルとHOME/AWAY両チームの得点表示を管理する
    /// </summary>
    public class SoccerGameManager : BaseMiniGameManager
    {
        [Header("Soccer References")]
        [SerializeField] private Ball _ball;
        [SerializeField] private PlayerSwitcher _playerSwitcher;
        [SerializeField] private Vector2 _ballStartPosition;

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

        protected override void OnGameReady()
        {
            _remainingSeconds = _matchDurationSeconds;
            ResetPositions();
            UpdateScoreText();
            UpdateTimerText();
            StartCoroutine(KickOffRoutine());
        }

        protected override void Update()
        {
            base.Update();
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

            if (AudioManager.HasInstance)
            {
                AudioManager.Instance.PlaySe(SeId.Whistle);
            }

            yield return StartCoroutine(ShowMessageRoutine("TIME UP", _timeUpMessageDuration));

            // 勝敗表示とリザルト画面の表示は BaseMiniGameManager に任せる
            FinishGame(_homeScore > _awayScore, $"HOME {_homeScore} - {_awayScore} AWAY", BuildResultDetail());
        }

        private string BuildResultDetail()
        {
            if (_homeScore > _awayScore) return "勝利！";
            if (_homeScore < _awayScore) return "敗北...";
            return "引き分け";
        }

        protected override void OnGameOver(bool isVictory)
        {
            // 試合終了後もボールと選手が動き続けないよう入力とボールを止める
            if (_ball != null)
            {
                _ball.ResetBall(_ball.Position);
            }

            if (InputManager.HasInstance)
            {
                InputManager.Instance.InputEnabled = false;
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
            if (!IsPlaying || _isSequenceRunning) return;
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

            if (AudioManager.HasInstance)
            {
                AudioManager.Instance.PlaySe(SeId.GoalCheer);
            }

            if (_goalEffect != null)
            {
                _goalEffect.Play();
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

            if (AudioManager.HasInstance)
            {
                AudioManager.Instance.PlaySe(SeId.Whistle);
            }

            yield return StartCoroutine(ShowMessageRoutine("KICK OFF!", _kickOffMessageDuration));
            StartGame();
        }

        private IEnumerator ShowMessageRoutine(string message, float duration)
        {
            if (_messageText != null)
            {
                _messageText.text = message;
                _messageText.gameObject.SetActive(true);
            }

            yield return new WaitForSeconds(duration);

            if (_messageText != null)
            {
                _messageText.gameObject.SetActive(false);
            }
        }

        private void UpdateScoreText()
        {
            if (_scoreText != null)
            {
                _scoreText.text = $"HOME {_homeScore} - {_awayScore} AWAY";
            }
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
        }
    }
}
