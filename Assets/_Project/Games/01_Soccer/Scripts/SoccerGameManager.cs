using System.Collections;
using MiniGame.Common.Audio;
using MiniGame.Common.Core;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.Soccer
{
    /// <summary>
    /// サッカーゲームのゲームマネージャー（Phase 4: ゴール・試合状態）
    /// キックオフ → プレイ → ゴール → リセット → キックオフ、のサイクルと得点表示を管理する
    /// </summary>
    public class SoccerGameManager : BaseMiniGameManager
    {
        [Header("Soccer References")]
        [SerializeField] private Rigidbody2D _playerRigidbody;
        [SerializeField] private Ball _ball;
        [SerializeField] private Vector2 _playerStartPosition;
        [SerializeField] private Vector2 _ballStartPosition;

        [Header("UI")]
        [SerializeField] private Text _messageText;
        [SerializeField] private Text _scoreText;

        [Header("Timing")]
        [SerializeField] private float _kickOffMessageDuration = 1.0f;
        [SerializeField] private float _goalMessageDuration = 1.5f;

        private int _score;
        private bool _isSequenceRunning;

        protected override void OnGameReady()
        {
            ResetPositions();
            UpdateScoreText();
            StartCoroutine(KickOffRoutine());
        }

        /// <summary>
        /// GoalTrigger からゴール検知時に呼び出される
        /// </summary>
        public void OnGoalScored()
        {
            if (!IsPlaying || _isSequenceRunning) return;
            StartCoroutine(GoalRoutine());
        }

        private IEnumerator GoalRoutine()
        {
            _isSequenceRunning = true;
            ChangeState(MiniGameState.Event);

            _score++;
            UpdateScoreText();

            if (AudioManager.HasInstance)
            {
                AudioManager.Instance.PlaySe(SeId.GoalCheer);
            }

            yield return StartCoroutine(ShowMessageRoutine("GOAL!", _goalMessageDuration));

            ResetPositions();
            yield return StartCoroutine(KickOffRoutine());

            _isSequenceRunning = false;
        }

        private IEnumerator KickOffRoutine()
        {
            ChangeState(MiniGameState.Countdown);
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
                _scoreText.text = $"SCORE: {_score}";
            }
        }

        private void ResetPositions()
        {
            if (_playerRigidbody != null)
            {
                _playerRigidbody.linearVelocity = Vector2.zero;
                _playerRigidbody.position = _playerStartPosition;
            }

            if (_ball != null)
            {
                _ball.ResetBall(_ballStartPosition);
            }
        }
    }
}
