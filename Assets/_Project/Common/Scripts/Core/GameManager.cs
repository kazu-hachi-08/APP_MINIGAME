using UnityEngine;

namespace MiniGame.Common.Core
{
    /// <summary>
    /// アプリケーション全体のグローバルライフサイクルおよび設定マネージャー
    /// </summary>
    public class GameManager : SingletonMonoBehaviour<GameManager>
    {
        [Header("Application Settings")]
        [SerializeField] private int _targetFrameRate = 60;
        [SerializeField] private bool _neverSleep = true;

        protected override void Awake()
        {
            base.Awake();
            InitializeApplication();
        }

        private void InitializeApplication()
        {
            // フレームレート設定
            Application.targetFrameRate = _targetFrameRate;

            // スリープ抑制（スマホ実機向け）
            if (_neverSleep)
            {
                Screen.sleepTimeout = SleepTimeout.NeverSleep;
            }
        }
    }
}
