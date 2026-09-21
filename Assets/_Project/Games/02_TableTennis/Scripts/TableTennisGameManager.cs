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
        [SerializeField] private TableTennisAudio _audio;

        [Header("UI")]
        [SerializeField] private Text _scoreText;
        [SerializeField] private HudText _messageHud;

        [Tooltip("打球結果（タイミングと回転）をプレイヤーへ返すHUD")]
        [SerializeField] private HudText _shotInfoHud;

        [Header("Rule")]
        [SerializeField] private int _pointsToWin = 11;
        [SerializeField] private int _serveChangeInterval = 2;

        [Header("Timing")]
        [SerializeField] private float _serveDelay = 1.0f;
        [SerializeField] private float _pointDisplayDuration = 1.4f;

        [Tooltip("トスを打ち損ねたときに、やり直すまでの待ち時間")]
        [SerializeField] private float _retossDelay = 0.6f;

        [Tooltip("打球結果のHUDを出しておく時間")]
        [SerializeField] private float _shotInfoDuration = 1.2f;

        [Tooltip("GAME SET を見せてからリザルトを出すまでの時間")]
        [SerializeField] private float _gameSetDuration = 1.6f;

        private MatchScore _score;
        private RallyPhase _phase = RallyPhase.PointBreak;

        private void Awake()
        {
            // Scene を作り直す前でも遊べるよう、SE が未設定なら自前で用意する
            if (_audio == null)
            {
                _audio = gameObject.AddComponent<TableTennisAudio>();
            }
        }

        private void OnEnable()
        {
            _ball.OnRallyEnded += HandleRallyEnded;
            _ball.OnBounced += HandleBounced;
            _playerSwing.OnShot += HandleShot;
            _playerSwing.OnMissed += HandleMissed;
            _referee.OnPointDecided += HandlePointDecided;
            _npc.OnReturned += HandleNpcReturned;
        }

        private void OnDisable()
        {
            _ball.OnRallyEnded -= HandleRallyEnded;
            _ball.OnBounced -= HandleBounced;
            _playerSwing.OnShot -= HandleShot;
            _playerSwing.OnMissed -= HandleMissed;
            _referee.OnPointDecided -= HandlePointDecided;
            _npc.OnReturned -= HandleNpcReturned;
        }

        protected override void OnGameReady()
        {
            _score = new MatchScore(_pointsToWin, _serveChangeInterval, CourtSide.Player);
            UpdateScoreText();
            SetShotInfo("フリックして打つ", 0f);
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
            SetShotInfo(server == CourtSide.Player ? "トスを打つ" : string.Empty, _shotInfoDuration);

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
                _audio.PlayHit(1f);
            }
        }

        /// <summary>台に落ちた音は、奥行きが読み取りにくい擬似3Dで距離感の手がかりにもなる</summary>
        private void HandleBounced(Vector3 contact)
        {
            _audio.PlayBounce();
        }

        private void HandleRallyEnded(RallyEndReason reason)
        {
            if (reason == RallyEndReason.Net)
            {
                _audio.PlayNet();
            }

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
            SetShotInfo("トスをやり直します", _shotInfoDuration);
            yield return new WaitForSeconds(_retossDelay);

            if (_phase == RallyPhase.Serving)
            {
                _serve.TossForPlayer();
            }
        }

        private void HandleShot(FlickData flick, ShotResult shot)
        {
            _audio.PlayHit(shot.Strength);

            if (_phase == RallyPhase.Serving)
            {
                _phase = RallyPhase.Rallying;
                _referee.BeginRally(CourtSide.Player);
            }
            else
            {
                _referee.NotifyHit(CourtSide.Player);
            }

            SetShotInfo(BuildShotInfo(shot), _shotInfoDuration);
        }

        /// <summary>NPCが返球できたら、打球したものとしてラリー判定を継続する</summary>
        private void HandleNpcReturned()
        {
            _audio.PlayHit(0.8f);
            _referee.NotifyHit(CourtSide.Opponent);
        }

        private void HandleMissed(SwingJudgement judgement)
        {
            SetShotInfo($"空振り！　{TimingLabel(judgement.Timing)}", _shotInfoDuration);
        }

        private void HandlePointDecided(CourtSide scorer, PointReason reason)
        {
            _phase = RallyPhase.PointBreak;
            _ball.Stop();

            _score.AddPoint(scorer);
            UpdateScoreText();

            SetShotInfo(string.Empty, 0f);
            SetMessage($"{ReasonLabel(reason)}\n{scorer.ToLabel()} POINT");
            PlaySe(scorer == CourtSide.Player ? SeId.GoalCheer : SeId.Whistle);

            StartCoroutine(AfterPointRoutine());
        }

        private IEnumerator AfterPointRoutine()
        {
            yield return new WaitForSeconds(_pointDisplayDuration);

            if (_score.IsFinished)
            {
                yield return GameSetRoutine();
                yield break;
            }

            yield return NextServeRoutine();
        }

        /// <summary>リザルトの前に勝敗を一拍見せる（リザルトダイアログ自体は共通UIに任せる）</summary>
        private IEnumerator GameSetRoutine()
        {
            _referee.Stop();
            _ball.Stop();

            bool isVictory = _score.Winner == CourtSide.Player;
            SetMessage(isVictory ? "GAME SET\nYOU WIN" : "GAME SET\nYOU LOSE");
            PlaySe(isVictory ? SeId.GoalCheer : SeId.Whistle);

            yield return new WaitForSeconds(_gameSetDuration);
            SetMessage(string.Empty);

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
        /// 打球の手応え（タイミングと回転）を、プレイ中に読み取れる短さで返す
        /// </summary>
        private static string BuildShotInfo(ShotResult shot)
        {
            return $"{TimingLabel(shot.Timing)}　強さ {shot.Strength * 100f:0}%\n{DescribeSpin(shot.Spin)}";
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
                case ShotTiming.Early: return "早すぎ！";
                case ShotTiming.Late: return "遅すぎ！";
                default: return "ジャスト！";
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
            if (_messageHud == null) return;

            if (string.IsNullOrEmpty(message))
            {
                _messageHud.Clear();
                return;
            }

            _messageHud.Show(message);
        }

        /// <summary>holdDuration が0なら、消さずに出しっぱなしにする</summary>
        private void SetShotInfo(string info, float holdDuration)
        {
            if (_shotInfoHud == null) return;

            if (string.IsNullOrEmpty(info))
            {
                _shotInfoHud.Clear();
                return;
            }

            _shotInfoHud.Show(info, holdDuration);
        }
    }
}
