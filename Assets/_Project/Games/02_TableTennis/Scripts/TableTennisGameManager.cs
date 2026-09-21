using System.Collections;
using MiniGame.Common.Audio;
using MiniGame.Common.Core;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.TableTennis
{
    /// <summary>
    /// 卓球ゲームの進行管理（サーブ → ラリー → 得点 → サーブ交代 → 11点先取）。
    /// ルール判定は RallyReferee、得点とサーブ権は MatchScore、相手の打球は NpcController に任せ、
    /// ここは「今どの状態か」と「次に何をするか」だけを見る。
    /// </summary>
    public class TableTennisGameManager : BaseMiniGameManager
    {
        /// <summary>卓球固有のラリー進行状態（共通の MiniGameState とは別に持つ）</summary>
        private enum RallyPhase
        {
            /// <summary>サーブ待ち・トス中</summary>
            Serving,

            /// <summary>ラリー中</summary>
            Rallying,

            /// <summary>得点表示中（打球を受け付けない）</summary>
            PointBreak
        }

        [Header("References")]
        [SerializeField] private BallMotion _ball;
        [SerializeField] private PlayerSwing _playerSwing;
        [SerializeField] private RallyReferee _referee;
        [SerializeField] private ServeController _serve;
        [SerializeField] private NpcController _npc;

        [Header("UI")]
        [SerializeField] private Text _scoreText;
        [SerializeField] private Text _messageText;

        [Tooltip("フリックから打球・回転・タイミングへの対応を確認するための開発用表示（Phase 8 で整理する）")]
        [SerializeField] private Text _shotInfoText;

        [Header("Rule")]
        [SerializeField] private int _pointsToWin = 11;
        [SerializeField] private int _serveChangeInterval = 2;

        [Header("Timing")]
        [SerializeField] private float _serveDelay = 1.0f;
        [SerializeField] private float _pointDisplayDuration = 1.4f;

        [Tooltip("トスを打ち損ねたときに、やり直すまでの待ち時間")]
        [SerializeField] private float _retossDelay = 0.6f;

        private MatchScore _score;
        private RallyPhase _phase = RallyPhase.PointBreak;

        private void OnEnable()
        {
            _ball.OnRallyEnded += HandleRallyEnded;
            _playerSwing.OnShot += HandleShot;
            _playerSwing.OnMissed += HandleMissed;
            _referee.OnPointDecided += HandlePointDecided;
            _npc.OnReturned += HandleNpcReturned;
        }

        private void OnDisable()
        {
            _ball.OnRallyEnded -= HandleRallyEnded;
            _playerSwing.OnShot -= HandleShot;
            _playerSwing.OnMissed -= HandleMissed;
            _referee.OnPointDecided -= HandlePointDecided;
            _npc.OnReturned -= HandleNpcReturned;
        }

        protected override void OnGameReady()
        {
            _score = new MatchScore(_pointsToWin, _serveChangeInterval, CourtSide.Player);
            UpdateScoreText();
            SetShotInfo("フリックして打つ");
            StartGame();
            StartCoroutine(NextServeRoutine());
        }

        protected override void Update()
        {
            base.Update();

            // ポーズ中や得点表示中にフリックが打球として通らないようにする
            _playerSwing.CanSwing = IsPlaying && _phase != RallyPhase.PointBreak && _ball.IsFlying;

            // NPCが動くのはラリー中だけ（サーブ待ちやトス中は構えに戻す）
            _npc.IsActive = IsPlaying && _phase == RallyPhase.Rallying;
        }

        /// <summary>サーブ権を確認し、プレイヤーならトス、相手なら送り出しでラリーを始める</summary>
        private IEnumerator NextServeRoutine()
        {
            _phase = RallyPhase.PointBreak;
            _ball.Stop();
            _referee.Stop();
            _npc.ResetForRally();

            CourtSide server = _score.CurrentServer;
            SetMessage(server == CourtSide.Player ? "YOUR SERVE" : "NPC SERVE");
            yield return new WaitForSeconds(_serveDelay);
            SetMessage(string.Empty);

            if (server == CourtSide.Player)
            {
                // トスを打った時点でラリー開始とする（HandleShot）
                _phase = RallyPhase.Serving;
                _serve.TossForPlayer();
            }
            else
            {
                _phase = RallyPhase.Rallying;
                _npc.Serve();
                _referee.BeginRally(CourtSide.Opponent);
                PlaySe(SeId.Kick);
            }
        }

        private void HandleRallyEnded(RallyEndReason reason)
        {
            if (!IsPlaying) return;

            // トスを打ち損ねただけなので、失点にせずトスをやり直す。
            // ラリー中の失点判定は RallyReferee が行う
            if (_phase == RallyPhase.Serving)
            {
                StartCoroutine(RetossRoutine());
            }
        }

        private IEnumerator RetossRoutine()
        {
            SetShotInfo("トスをやり直します");
            yield return new WaitForSeconds(_retossDelay);

            if (_phase == RallyPhase.Serving)
            {
                _serve.TossForPlayer();
            }
        }

        private void HandleShot(FlickData flick, ShotResult shot)
        {
            PlaySe(SeId.Kick);

            if (_phase == RallyPhase.Serving)
            {
                _phase = RallyPhase.Rallying;
                _referee.BeginRally(CourtSide.Player);
            }
            else
            {
                _referee.NotifyHit(CourtSide.Player);
            }

            SetShotInfo(BuildShotInfo(flick, shot));
        }

        /// <summary>NPCが返球できたら、打球したものとしてラリー判定を継続する</summary>
        private void HandleNpcReturned()
        {
            PlaySe(SeId.Kick);
            _referee.NotifyHit(CourtSide.Opponent);
        }

        private void HandleMissed(SwingJudgement judgement)
        {
            SetShotInfo($"空振り！ タイミング {TimingLabel(judgement.Timing)}");
        }

        private void HandlePointDecided(CourtSide scorer, PointReason reason)
        {
            _phase = RallyPhase.PointBreak;
            _ball.Stop();

            _score.AddPoint(scorer);
            UpdateScoreText();

            SetMessage($"{ReasonLabel(reason)}\n{scorer.ToLabel()} POINT");
            PlaySe(scorer == CourtSide.Player ? SeId.GoalCheer : SeId.Whistle);

            StartCoroutine(AfterPointRoutine());
        }

        private IEnumerator AfterPointRoutine()
        {
            yield return new WaitForSeconds(_pointDisplayDuration);

            if (_score.IsFinished)
            {
                FinishMatch();
                yield break;
            }

            yield return NextServeRoutine();
        }

        private void FinishMatch()
        {
            SetMessage(string.Empty);
            _referee.Stop();
            _ball.Stop();

            bool isVictory = _score.Winner == CourtSide.Player;
            FinishGame(
                isVictory,
                $"{_score.PlayerPoints} - {_score.OpponentPoints}",
                isVictory ? $"{_pointsToWin}点先取！" : "NPCの勝ち");
        }

        private void UpdateScoreText()
        {
            if (_scoreText == null) return;

            // サーブ権がどちらにあるかを ● で示す
            bool playerServes = _score.CurrentServer == CourtSide.Player;
            string playerMark = playerServes ? "●" : "  ";
            string opponentMark = playerServes ? "  " : "●";
            _scoreText.text = $"{playerMark} YOU {_score.PlayerPoints} - {_score.OpponentPoints} NPC {opponentMark}";
        }

        /// <summary>
        /// フリック方向・速度が打球方向・速度・回転へどう分配されたかを可視化する
        /// </summary>
        private static string BuildShotInfo(FlickData flick, ShotResult shot)
        {
            return $"FLICK 方向({flick.Direction.x:0.00}, {flick.Direction.y:0.00})  速度 {flick.Speed:0.00} (強さ {shot.Strength:0.00})\n"
                 + $"打球  前方 {shot.Velocity.z:0.0} m/s  左右 {shot.Velocity.x:+0.0;-0.0;0.0}  打ち上げ {shot.Velocity.y:0.0}\n"
                 + $"回転  {DescribeSpin(shot.Spin)}  (top {shot.Spin.y:+0.00;-0.00;0.00} / side {shot.Spin.x:+0.00;-0.00;0.00})\n"
                 + $"タイミング  {TimingLabel(shot.Timing)}  (品質 {shot.Quality:0.00})";
        }

        private static string DescribeSpin(Vector2 spin)
        {
            const float deadZone = 0.1f;

            string vertical = spin.y > deadZone ? "トップスピン"
                : spin.y < -deadZone ? "バックスピン"
                : "";
            string horizontal = spin.x > deadZone ? "右サイドスピン"
                : spin.x < -deadZone ? "左サイドスピン"
                : "";

            if (vertical.Length > 0 && horizontal.Length > 0) return vertical + " + " + horizontal;
            if (vertical.Length > 0) return vertical;
            if (horizontal.Length > 0) return horizontal;
            return "ほぼ無回転";
        }

        private static string TimingLabel(ShotTiming timing)
        {
            switch (timing)
            {
                case ShotTiming.Early: return "早すぎ";
                case ShotTiming.Late: return "遅すぎ";
                default: return "ジャスト";
            }
        }

        private static string ReasonLabel(PointReason reason)
        {
            switch (reason)
            {
                case PointReason.Net: return "NET";
                case PointReason.Out: return "OUT";
                default: return "MISS";
            }
        }

        private static void PlaySe(SeId id)
        {
            if (AudioManager.HasInstance)
            {
                AudioManager.Instance.PlaySe(id);
            }
        }

        private void SetMessage(string message)
        {
            if (_messageText == null) return;

            _messageText.text = message;
            _messageText.gameObject.SetActive(!string.IsNullOrEmpty(message));
        }

        private void SetShotInfo(string info)
        {
            if (_shotInfoText != null)
            {
                _shotInfoText.text = info;
            }
        }
    }
}
