using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.Common.Online
{
    /// <summary>
    /// 試合前に「コンピュータと対戦」か「オンライン対戦（部屋を作る／コードで参加）」かを選ばせるパネル。
    /// オンラインを選んだ場合は、相手と接続できるまでここで待たせる。
    /// </summary>
    public class ModeSelectPanel : MonoBehaviour
    {
        [SerializeField] private OnlineSession _session;

        [Header("Menu")]
        [SerializeField] private GameObject _menuGroup;
        [SerializeField] private Button _npcButton;
        [SerializeField] private Button _hostButton;
        [SerializeField] private Button _joinButton;
        [SerializeField] private InputField _codeInput;

        [Header("Waiting")]
        [SerializeField] private GameObject _waitingGroup;
        [SerializeField] private Text _statusText;
        [SerializeField] private Button _cancelButton;
        [SerializeField] private Button _copyButton;
        [SerializeField] private Text _copyButtonLabel;

        [Header("Copy")]
        [SerializeField] private string _copyLabel = "コードをコピー";
        [SerializeField] private string _copiedLabel = "コピーしました";
        [SerializeField] private float _copiedLabelSeconds = 1.5f;

        private Action _onNpcSelected;
        private Action<bool> _onOnlineReady;
        private string _roomCode;

        private void Awake()
        {
            _npcButton.onClick.AddListener(SelectNpc);
            _hostButton.onClick.AddListener(Host);
            _joinButton.onClick.AddListener(Join);
            _cancelButton.onClick.AddListener(CancelWaiting);
            _copyButton.onClick.AddListener(CopyRoomCode);
        }

        private void OnEnable()
        {
            _session.OnPeerConnected += HandlePeerConnected;
        }

        private void OnDisable()
        {
            _session.OnPeerConnected -= HandlePeerConnected;
        }

        /// <param name="onNpcSelected">NPC戦を選んだ</param>
        /// <param name="onOnlineReady">相手と接続できた（引数は自分がホストか）</param>
        public void Show(Action onNpcSelected, Action<bool> onOnlineReady)
        {
            _onNpcSelected = onNpcSelected;
            _onOnlineReady = onOnlineReady;
            gameObject.SetActive(true);
            ShowMenu();
        }

        private void SelectNpc()
        {
            gameObject.SetActive(false);
            _onNpcSelected?.Invoke();
        }

        private async void Host()
        {
            ShowWaiting("部屋を作っています…");
            try
            {
                string code = await _session.HostAsync();
                SetStatus($"参加コード\n<size=64>{code}</size>\n相手の参加を待っています");
                ShowCopyButton(code);
            }
            catch (Exception e)
            {
                ShowError(e);
            }
        }

        private async void Join()
        {
            // チャットから貼り付けると前後に空白・改行が混ざったり小文字になったりするため整える
            string code = _codeInput.text.Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(code)) return;

            ShowWaiting("接続しています…");
            try
            {
                await _session.JoinAsync(code);
            }
            catch (Exception e)
            {
                ShowError(e);
            }
        }

        /// <summary>友達にチャットで送れるよう、コードだけをクリップボードに入れる</summary>
        private void CopyRoomCode()
        {
            GUIUtility.systemCopyBuffer = _roomCode;
            StopAllCoroutines();
            StartCoroutine(ShowCopiedFeedback());
        }

        private IEnumerator ShowCopiedFeedback()
        {
            _copyButtonLabel.text = _copiedLabel;
            yield return new WaitForSecondsRealtime(_copiedLabelSeconds);
            _copyButtonLabel.text = _copyLabel;
        }

        private void ShowCopyButton(string code)
        {
            _roomCode = code;
            _copyButtonLabel.text = _copyLabel;
            _copyButton.gameObject.SetActive(true);
        }

        private void CancelWaiting()
        {
            _session.Leave();
            ShowMenu();
        }

        private void HandlePeerConnected(bool isHost)
        {
            gameObject.SetActive(false);
            _onOnlineReady?.Invoke(isHost);
        }

        /// <summary>
        /// 接続失敗の理由（コード違い・通信なし等）は細かく分けず、ログに残して待機画面で止める。
        /// キャンセルでメニューに戻ればやり直せる
        /// </summary>
        private void ShowError(Exception e)
        {
            Debug.LogWarning($"[ModeSelectPanel] オンライン接続に失敗しました: {e}");
            _session.Leave();
            _copyButton.gameObject.SetActive(false);
            SetStatus("接続できませんでした\nコードや通信状況を確認してください");
        }

        private void ShowMenu()
        {
            _menuGroup.SetActive(true);
            _waitingGroup.SetActive(false);
        }

        private void ShowWaiting(string status)
        {
            _menuGroup.SetActive(false);
            _waitingGroup.SetActive(true);
            // コードが発行されるまで（部屋作成中・参加側の接続中）はコピーするものがないので隠す
            _copyButton.gameObject.SetActive(false);
            SetStatus(status);
        }

        private void SetStatus(string status)
        {
            _statusText.text = status;
        }
    }
}
