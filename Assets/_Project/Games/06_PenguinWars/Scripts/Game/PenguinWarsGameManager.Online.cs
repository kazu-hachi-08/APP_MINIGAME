using System.Collections;
using MiniGame.Common.Core;
using MiniGame.Common.Online;
using MiniGame.Common.Profile;
using MiniGame.Common.UI;
using MiniGame.PenguinWars.Battle;
using UnityEngine;

namespace MiniGame.PenguinWars
{
    /// <summary>
    /// オンライン対戦（仕様書 §2.2・§10）の接続後の流れと決着。
    /// ホスト: ゲストの準備完了 → 両者の編成を決めて送る → 編成発表。ゲスト: 編成が届く → 編成発表。
    /// 決着（城崩壊・時間切れ）はホストの BattleWorld で決まり、ゲストにもイベントとして届くので、両者とも同じ HandleBattleEvent で終わる
    /// </summary>
    public partial class PenguinWarsGameManager
    {
        private enum MatchMode
        {
            Endless,
            Host,
            Guest,
        }

        private const string TimeUpMessage = "TIME UP!";
        private const string DrawTitle = "DRAW";
        private const string WinDetail = "勝利！";
        private const string LoseDetail = "敗北...";
        private const string DrawDetail = "引き分け";
        private const string DisconnectedDetail = "相手が切断しました";
        private const int HostSeat = 0;
        private const int GuestSeat = 1;
        private const float PercentScale = 100f;

        [Header("オンライン対戦")]
        [Tooltip("未設定ならモード選択を出さずエンドレスで始める")]
        [SerializeField] private ModeSelectPanel _modeSelectPanel;
        [SerializeField] private OnlineSession _onlineSession;
        [SerializeField] private PenguinWarsOnlineLink _onlineLink;
        [Tooltip("お互いの編成を見せる時間（仕様書 §2.2）")]
        [SerializeField] private float _versusDeckIntroDuration = 3f;
        [Tooltip("TIME UP! を見せてからリザルトを出すまでの秒数")]
        [SerializeField] private float _timeUpDuration = 1.5f;

        private MatchMode _mode = MatchMode.Endless;
        // オンラインでは相手の端末が止まらないので、ポーズ画面を出しても試合は止めない（サッカーと同じ）
        private bool _isOnlinePauseOpen;

        private bool IsOnline => _mode != MatchMode.Endless;
        private int MySeat => _mode == MatchMode.Guest ? GuestSeat : HostSeat;
        private int OpponentSeat => _mode == MatchMode.Guest ? HostSeat : GuestSeat;

        private void SubscribeOnline()
        {
            if (_onlineSession != null) _onlineSession.OnPeerDisconnected += HandlePeerDisconnected;
            if (_onlineLink == null) return;

            _onlineLink.GuestReady += HandleGuestReady;
            _onlineLink.DecksReceived += HandleDecksReceived;
        }

        private void UnsubscribeOnline()
        {
            if (_onlineSession != null) _onlineSession.OnPeerDisconnected -= HandlePeerDisconnected;
            if (_onlineLink == null) return;

            _onlineLink.GuestReady -= HandleGuestReady;
            _onlineLink.DecksReceived -= HandleDecksReceived;
        }

        /// <summary>相手と接続できた。編成はホストが決めるので、ゲストは届くまで待つ</summary>
        private void StartOnline(bool isHost)
        {
            _mode = isHost ? MatchMode.Host : MatchMode.Guest;
            _onlineLink.Begin(isHost);
        }

        /// <summary>ホストのみ。ゲストが受け取れるようになってから編成を決めて送る</summary>
        private void HandleGuestReady()
        {
            if (_mode != MatchMode.Host || Phase != PenguinWarsPhase.ModeSelect) return;

            _battleRunner.InitializeVersusHost();
            BattleWorld world = _battleRunner.World;
            _onlineLink.SendDecks(world.GetDeck(Side.Left), world.GetDeck(Side.Right));
            BeginIntro(_versusDeckIntroDuration);
        }

        /// <summary>ゲストのみ。ホスト基準の編成を受け取り、自分（ホストの右）を左に入れ替えて持つ</summary>
        private void HandleDecksReceived(int[] hostLeftDeck, int[] hostRightDeck)
        {
            if (_mode != MatchMode.Guest || Phase != PenguinWarsPhase.ModeSelect) return;

            _battleRunner.InitializeGuest(hostRightDeck, hostLeftDeck, _onlineLink);
            BeginIntro(_versusDeckIntroDuration);
        }

        /// <summary>時間切れは崩れる演出がないので、TIME UP! を少し見せてからリザルトへ</summary>
        private void BeginTimeUp(Side loser, bool isDraw)
        {
            BeginFinish();
            _hud.ShowMessage(TimeUpMessage);
            StartCoroutine(EndAfterTimeUp(loser, isDraw));
        }

        private IEnumerator EndAfterTimeUp(Side loser, bool isDraw)
        {
            yield return new WaitForSeconds(_timeUpDuration);
            _hud.HideMessage();
            EndVersus(loser, isDraw);
        }

        /// <summary>ゲストも左右反転した向きで受け取っているので、どちらの端末でも「自分 = Left」で勝敗を判定できる</summary>
        private void EndVersus(Side loser, bool isDraw)
        {
            string score = BuildCastleSummary();
            if (isDraw)
            {
                FinishAsDraw(score);
                return;
            }

            bool isVictory = loser == Side.Right;
            FinishGame(isVictory, score, isVictory ? WinDetail : LoseDetail);
        }

        /// <summary>BaseMiniGameManager.FinishGame はタイトルが VICTORY! / GAME OVER しかないので、引き分けだけ同じ手順を自前で踏む（仕様書 §2.3）</summary>
        private void FinishAsDraw(string score)
        {
            ChangeState(MiniGameState.GameOver);
            OnGameOver(false);
            ChangeState(MiniGameState.Result);
            if (UIManager.HasInstance)
            {
                UIManager.Instance.ShowResultDialog(DrawTitle, score, DrawDetail, RestartGame, ReturnToTitle);
            }
        }

        /// <summary>「自分 72% - 40% 相手」。時間切れの判定と同じく城の残りHP割合で見せる</summary>
        private string BuildCastleSummary()
        {
            BattleWorld world = _battleRunner.World;
            if (world == null) return string.Empty;

            string mine = FormatCastleHp(world.GetCastle(Side.Left));
            string theirs = FormatCastleHp(world.GetCastle(Side.Right));
            return $"{SeatNames.Get(MySeat)} {mine} - {theirs} {SeatNames.Get(OpponentSeat)}";
        }

        /// <summary>切り上げにして、少しでも残っていれば 0% と出さない（落ちた城と見分けがつくように）</summary>
        private static string FormatCastleHp(CastleState castle)
        {
            float ratio = castle.MaxHp > 0 ? (float)castle.Hp / castle.MaxHp : 0f;
            return $"{Mathf.CeilToInt(ratio * PercentScale)}%";
        }

        /// <summary>
        /// 試合中に相手が抜けたら勝ち扱い（仕様書 §10.4）。
        /// 既に決着して崩れる演出・TIME UP を見せている間なら、そのまま本来の結果を出す
        /// </summary>
        private void HandlePeerDisconnected()
        {
            if (!IsOnline || Phase == PenguinWarsPhase.Finished || CurrentState == MiniGameState.Result) return;

            Phase = PenguinWarsPhase.Finished;
            if (_battleRunner.World != null) _battleRunner.SetRunning(false);
            _deckIntroPanel.Hide();
            _hud.HideMessage();
            _audio.StopBgm();
            FinishGame(true, BuildCastleSummary(), DisconnectedDetail);
        }

        protected override void OnGameOver(bool isVictory)
        {
            if (!IsOnline) return;

            _onlineLink.Stop();
            CloseOnlinePause();
        }

        // ---- ポーズ ----

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
                CloseOnlinePause();
                return;
            }
            if (!UIManager.HasInstance) return;

            _isOnlinePauseOpen = true;
            UIManager.Instance.ShowPauseDialog(onResume: CloseOnlinePause, onRestart: RestartGame, onTitle: ReturnToTitle);
        }

        private void CloseOnlinePause()
        {
            if (!_isOnlinePauseOpen) return;

            _isOnlinePauseOpen = false;
            if (UIManager.HasInstance) UIManager.Instance.HidePauseDialog();
        }
    }
}
