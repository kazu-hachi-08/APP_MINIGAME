using System.Collections;
using MiniGame.Common.Audio;
using MiniGame.Common.Core;
using UnityEngine;

namespace MiniGame.Soccer
{
    /// <summary>
    /// サッカーゲームのゲームマネージャー（Phase 1: 最小プロトタイプ）
    /// 選手・ボールの初期化とゴール時の演出/リセットのみを扱う
    /// </summary>
    public class SoccerGameManager : BaseMiniGameManager
    {
        [Header("Soccer References")]
        [SerializeField] private Rigidbody2D _playerRigidbody;
        [SerializeField] private Ball _ball;
        [SerializeField] private Vector2 _playerStartPosition;
        [SerializeField] private Vector2 _ballStartPosition;

        [Header("Goal Banner")]
        [SerializeField] private GameObject _goalBanner;
        [SerializeField] private float _goalBannerDuration = 1.5f;

        private bool _isGoalSequenceRunning;

        protected override void OnGameReady()
        {
            // Phase 1 はキックオフ演出を持たないため、準備完了後すぐにプレイを開始する
            StartGame();
        }

        protected override void OnGameStart()
        {
            ResetPositions();
        }

        /// <summary>
        /// GoalTrigger からゴール検知時に呼び出される
        /// </summary>
        public void OnGoalScored()
        {
            if (!IsPlaying || _isGoalSequenceRunning) return;
            StartCoroutine(GoalSequenceRoutine());
        }

        private IEnumerator GoalSequenceRoutine()
        {
            _isGoalSequenceRunning = true;
            ChangeState(MiniGameState.Event);

            if (AudioManager.HasInstance)
            {
                AudioManager.Instance.PlaySe(SeId.GoalCheer);
            }

            if (_goalBanner != null)
            {
                _goalBanner.SetActive(true);
            }

            yield return new WaitForSeconds(_goalBannerDuration);

            if (_goalBanner != null)
            {
                _goalBanner.SetActive(false);
            }

            ResetPositions();
            ChangeState(MiniGameState.Playing);
            _isGoalSequenceRunning = false;
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
