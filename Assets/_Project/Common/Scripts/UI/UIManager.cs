using System;
using MiniGame.Common.Core;
using UnityEngine;

namespace MiniGame.Common.UI
{
    /// <summary>
    /// 共通UI（ダイアログ、ポーズ、リザルト等）の表示と制御を統括するマネージャー
    /// </summary>
    public class UIManager : SingletonMonoBehaviour<UIManager>
    {
        [Header("UI Dialog Prefabs / References")]
        [SerializeField] private CommonDialog _commonDialog;
        [SerializeField] private PauseDialog _pauseDialog;
        [SerializeField] private ResultDialog _resultDialog;

        /// <summary>
        /// 汎用確認ダイアログの表示
        /// </summary>
        public void ShowConfirmDialog(
            string title,
            string message,
            Action onConfirm = null,
            Action onCancel = null,
            string confirmText = "OK",
            string cancelText = "キャンセル")
        {
            if (_commonDialog != null)
            {
                _commonDialog.Show(title, message, onConfirm, onCancel, confirmText, cancelText, showCancelButton: true);
            }
            else
            {
                Debug.LogWarning("[UIManager] CommonDialog参照が設定されていません。");
            }
        }

        /// <summary>
        /// 単一ボタンOKアラートの表示
        /// </summary>
        public void ShowAlertDialog(
            string title,
            string message,
            Action onOk = null,
            string okText = "OK")
        {
            if (_commonDialog != null)
            {
                _commonDialog.Show(title, message, onOk, null, okText, "", showCancelButton: false);
            }
        }

        /// <summary>
        /// 一時停止ダイアログの表示
        /// </summary>
        public void ShowPauseDialog(Action onResume, Action onRestart, Action onTitle)
        {
            if (_pauseDialog != null)
            {
                _pauseDialog.Initialize(onResume, onRestart, onTitle);
                _pauseDialog.Show();
            }
            else
            {
                Debug.LogWarning("[UIManager] PauseDialog参照が設定されていません。");
            }
        }

        /// <summary>
        /// 一時停止ダイアログを閉じる
        /// </summary>
        public void HidePauseDialog()
        {
            if (_pauseDialog != null)
            {
                _pauseDialog.Hide();
            }
        }

        /// <summary>
        /// リザルト画面の表示
        /// </summary>
        public void ShowResultDialog(
            string title,
            string scoreInfo,
            string detailInfo = "",
            Action onRetry = null,
            Action onTitle = null)
        {
            if (_resultDialog != null)
            {
                _resultDialog.Show(title, scoreInfo, detailInfo, onRetry, onTitle);
            }
            else
            {
                Debug.LogWarning("[UIManager] ResultDialog参照が設定されていません。");
            }
        }

        /// <summary>
        /// 各ダイアログのインスタンスをランタイム登録
        /// </summary>
        public void RegisterDialogs(CommonDialog commonDialog = null, PauseDialog pauseDialog = null, ResultDialog resultDialog = null)
        {
            if (commonDialog != null) _commonDialog = commonDialog;
            if (pauseDialog != null) _pauseDialog = pauseDialog;
            if (resultDialog != null) _resultDialog = resultDialog;
        }
    }
}
