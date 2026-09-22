using System;
using MiniGame.Common.Audio;
using MiniGame.Common.Input;
using MiniGame.Common.Scene;
using MiniGame.Common.UI;
using UnityEngine;

namespace MiniGame.Common.Core
{
    /// <summary>
    /// 全てのミニゲームマネージャーの親となる基底クラス
    /// ライフサイクル、状態管理、一時停止、リスタート、共通UI連携を提供
    /// </summary>
    public abstract class BaseMiniGameManager : MonoBehaviour
    {
        [Header("Base Settings")]
        [SerializeField] private string _gameTitle = "MiniGame";
        [SerializeField] private BgmId _gameplayBgm = BgmId.GameBgm01;
        [SerializeField] private bool _autoPlayBgm = true;

        [Header("Mobile Virtual Controls (Optional)")]
        [SerializeField] private VirtualJoystick _virtualJoystick;
        [SerializeField] private VirtualButton _actionButton1;
        [SerializeField] private VirtualButton _actionButton2;
        [SerializeField] private VirtualButton _actionButton3;
        [SerializeField] private VirtualButton _actionButton4;

        public string GameTitle => _gameTitle;
        public MiniGameState CurrentState { get; private set; } = MiniGameState.Ready;
        public bool IsPlaying => CurrentState == MiniGameState.Playing;
        public bool IsPaused => CurrentState == MiniGameState.Paused;

        public event GameStateChangedHandler OnGameStateChanged;

        private MiniGameState _stateBeforePause = MiniGameState.Playing;

        protected virtual void Start()
        {
            RegisterVirtualControls();

            if (_autoPlayBgm && AudioManager.HasInstance && _gameplayBgm != BgmId.None)
            {
                AudioManager.Instance.PlayBgm(_gameplayBgm);
            }

            InitializeGame();
        }

        protected virtual void Update()
        {
            // ポーズボタン入力の監視
            if (InputManager.HasInstance && InputManager.Instance.IsPauseDown)
            {
                if (IsPlaying)
                {
                    PauseGame();
                }
                else if (IsPaused)
                {
                    ResumeGame();
                }
            }
        }

        private void RegisterVirtualControls()
        {
            if (InputManager.HasInstance && _virtualJoystick != null)
            {
                InputManager.Instance.RegisterVirtualControls(
                    _virtualJoystick,
                    _actionButton1,
                    _actionButton2,
                    _actionButton3,
                    _actionButton4
                );
            }
        }

        /// <summary>
        /// ゲームの初期化処理（Start時に呼び出し）
        /// </summary>
        public virtual void InitializeGame()
        {
            Time.timeScale = 1.0f;
            ChangeState(MiniGameState.Ready);
            OnGameReady();
        }

        /// <summary>
        /// ゲームプレイの開始
        /// </summary>
        public virtual void StartGame()
        {
            ChangeState(MiniGameState.Playing);
            OnGameStart();
        }

        /// <summary>
        /// ゲーム状態の遷移
        /// </summary>
        public void ChangeState(MiniGameState newState)
        {
            if (CurrentState == newState) return;

            MiniGameState oldState = CurrentState;
            CurrentState = newState;

            OnGameStateChanged?.Invoke(oldState, newState);
        }

        /// <summary>
        /// ゲームの一時停止（ポーズ）
        /// </summary>
        public virtual void PauseGame()
        {
            if (CurrentState == MiniGameState.Paused || CurrentState == MiniGameState.GameOver || CurrentState == MiniGameState.Result)
            {
                return;
            }

            _stateBeforePause = CurrentState;
            ChangeState(MiniGameState.Paused);
            Time.timeScale = 0f;

            if (UIManager.HasInstance)
            {
                UIManager.Instance.ShowPauseDialog(
                    onResume: ResumeGame,
                    onRestart: RestartGame,
                    onTitle: ReturnToTitle
                );
            }

            OnGamePauseStateChanged(true);
        }

        /// <summary>
        /// ゲームの再開（レジューム）
        /// </summary>
        public virtual void ResumeGame()
        {
            if (CurrentState != MiniGameState.Paused) return;

            Time.timeScale = 1.0f;
            ChangeState(_stateBeforePause);

            if (UIManager.HasInstance)
            {
                UIManager.Instance.HidePauseDialog();
            }

            OnGamePauseStateChanged(false);
        }

        /// <summary>
        /// ゲームオーバーまたはクリア終了
        /// </summary>
        public virtual void FinishGame(bool isVictory, string scoreText, string detailText = "")
        {
            ChangeState(MiniGameState.GameOver);
            Time.timeScale = 1.0f;

            if (AudioManager.HasInstance)
            {
                AudioManager.Instance.PlaySe(isVictory ? SeId.GameClear : SeId.GameOver);
            }

            OnGameOver(isVictory);

            // リザルト画面表示
            ChangeState(MiniGameState.Result);
            if (UIManager.HasInstance)
            {
                string title = isVictory ? "VICTORY!" : "GAME OVER";
                UIManager.Instance.ShowResultDialog(
                    title: title,
                    scoreInfo: scoreText,
                    detailInfo: detailText,
                    onRetry: RestartGame,
                    onTitle: ReturnToTitle
                );
            }

            OnGameResult();
        }

        /// <summary>
        /// 現在のミニゲームをリスタート
        /// </summary>
        public virtual void RestartGame()
        {
            Time.timeScale = 1.0f;
            if (SceneLoader.HasInstance)
            {
                SceneLoader.Instance.RestartCurrentScene();
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
                );
            }
        }

        /// <summary>
        /// タイトル画面へ戻る
        /// </summary>
        public virtual void ReturnToTitle()
        {
            Time.timeScale = 1.0f;
            if (SceneLoader.HasInstance)
            {
                SceneLoader.Instance.LoadTitleScene();
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(SceneNames.Title);
            }
        }

        protected virtual void OnDestroy()
        {
            Time.timeScale = 1.0f;
            if (InputManager.HasInstance)
            {
                InputManager.Instance.UnregisterVirtualControls();
            }
        }

        // サブクラス用仮想フック
        protected virtual void OnGameReady() { }
        protected virtual void OnGameStart() { }
        protected virtual void OnGamePauseStateChanged(bool isPaused) { }
        protected virtual void OnGameOver(bool isVictory) { }
        protected virtual void OnGameResult() { }
    }
}
