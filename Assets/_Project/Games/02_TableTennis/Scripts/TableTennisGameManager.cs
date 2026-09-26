using System.Collections;
using MiniGame.Common.Audio;
using MiniGame.Common.Core;
using MiniGame.Common.Online;
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
        [SerializeField] private DifficultySelectPanel _difficultyPanel;

        [Header("選手・ラケット選択（未設定なら全てスタンダード）")]
        [SerializeField] private LoadoutSelectPanel _loadoutPanel;
        [SerializeField] private LoadoutApplier _loadoutApplier;
        [SerializeField] private LoadoutCatalog _loadoutCatalog;

        [Header("必殺技（未設定なら必殺技なし）")]
        [SerializeField] private SpecialController _special;

        [Header("Online（未設定ならNPC戦のみ）")]
        [SerializeField] private ModeSelectPanel _modeSelectPanel;
        [SerializeField] private OnlineSession _onlineSession;
        [SerializeField] private OnlineMatchLink _onlineLink;
        [SerializeField] private RemoteOpponent _remoteOpponent;

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

        [Tooltip("難易度選択パネルが無いとき（テストシーンなど）のデフォルト難易度")]
        [Range(NpcDifficultyTable.MinLevel, NpcDifficultyTable.MaxLevel)]
        [SerializeField] private int _fallbackDifficulty = 3;

        private MatchScore _score;
        private RallyPhase _phase = RallyPhase.PointBreak;
        private bool _isOnline;
        private bool _isHost;

        /// <summary>オンラインで、自分の選択が済んだか / 相手の選択が届いたか。両方そろってから試合を始める</summary>
        private bool _localLoadoutReady;
        private bool _peerLoadoutReady;

        private string OpponentLabel => _isOnline ? "RIVAL" : "NPC";

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
            _ball.OnEdgeBounced += HandleEdgeBounced;
            _playerSwing.OnShot += HandleShot;
            _playerSwing.OnMissed += HandleMissed;
            _referee.OnPointDecided += HandlePointDecided;
            _npc.OnReturned += HandleNpcReturned;

            if (_special != null)
            {
                _special.OnGranted += HandleSpecialGranted;
                _special.OnNpcUsed += HandleNpcSpecialUsed;
            }

            if (_onlineLink != null)
            {
                _onlineLink.OnShotReceived += HandleRemoteShot;
                _onlineLink.OnPointReceived += HandleRemotePoint;
                _onlineLink.OnLoadoutReceived += HandleRemoteLoadout;
            }

            if (_onlineSession != null)
            {
                _onlineSession.OnPeerDisconnected += HandlePeerDisconnected;
            }
        }

        private void OnDisable()
        {
            _ball.OnRallyEnded -= HandleRallyEnded;
            _ball.OnBounced -= HandleBounced;
            _ball.OnEdgeBounced -= HandleEdgeBounced;
            _playerSwing.OnShot -= HandleShot;
            _playerSwing.OnMissed -= HandleMissed;
            _referee.OnPointDecided -= HandlePointDecided;
            _npc.OnReturned -= HandleNpcReturned;

            if (_special != null)
            {
                _special.OnGranted -= HandleSpecialGranted;
                _special.OnNpcUsed -= HandleNpcSpecialUsed;
            }

            if (_onlineLink != null)
            {
                _onlineLink.OnShotReceived -= HandleRemoteShot;
                _onlineLink.OnPointReceived -= HandleRemotePoint;
                _onlineLink.OnLoadoutReceived -= HandleRemoteLoadout;
            }

            if (_onlineSession != null)
            {
                _onlineSession.OnPeerDisconnected -= HandlePeerDisconnected;
            }
        }

        protected override void OnGameReady()
        {
            _score = new MatchScore(_pointsToWin, _serveChangeInterval, CourtSide.Player);
            UpdateScoreText();
            SetShotInfo("フリックして打つ（上:ドライブ 下:カット）", 0f);

            if (_modeSelectPanel != null)
            {
                _modeSelectPanel.Show(ShowNpcLoadoutSelect, HandleOnlineConnected);
            }
            else
            {
                ShowNpcLoadoutSelect();
            }
        }

        /// <summary>NPC戦は自分とNPCの両方の選手・ラケットを選んでから難易度選択へ進む</summary>
        private void ShowNpcLoadoutSelect()
        {
            if (_loadoutPanel == null)
            {
                ShowDifficultySelect();
                return;
            }

            _loadoutPanel.Show(true, (player, opponent) =>
            {
                _loadoutApplier.ApplyPlayer(player);
                _loadoutApplier.ApplyNpc(opponent);

                if (_special != null)
                {
                    _special.SetSpecials(player.Character, opponent.Character);
                }

                ShowDifficultySelect();
            });
        }

        private void ShowDifficultySelect()
        {
            if (_difficultyPanel != null)
            {
                _difficultyPanel.Show(HandleDifficultySelected);
            }
            else
            {
                HandleDifficultySelected(_fallbackDifficulty);
            }
        }

        /// <summary>難易度選択後に試合を始める。選択自体は Ready 状態のうちに行う</summary>
        private void HandleDifficultySelected(int level)
        {
            _npc.SetDifficulty(level);
            StartGame();
            StartCoroutine(NextServeRoutine());
        }

        /// <summary>
        /// 相手と接続できたら、通信を始めてから自分の選手・ラケットを選ばせる。
        /// 選択中にも相手の選択が届くよう、先に通信を開始しておく。
        /// </summary>
        private void HandleOnlineConnected(bool isHost)
        {
            _isOnline = true;
            _isHost = isHost;

            // 相手の打球は相手端末から届くので、NPCの思考は止めて表示だけリモートへ渡す
            _npc.enabled = false;
            _remoteOpponent.TakeOverViews();
            _referee.IgnoreOwnShotOutcome = true;
            _onlineLink.Begin();

            // 台上キャラの出現・獲得を両端末で揃える仕組みがまだ無いため、オンラインでは必殺技を使わない
            if (_special != null)
            {
                _special.enabled = false;
            }

            if (_loadoutPanel == null)
            {
                _localLoadoutReady = true;
                _peerLoadoutReady = true;
                TryStartOnlineMatch();
                return;
            }

            _loadoutPanel.Show(false, (player, _) =>
            {
                _loadoutApplier.ApplyPlayer(player);
                _onlineLink.SendLoadout(_loadoutCatalog.IndexOf(player.Character), _loadoutCatalog.IndexOf(player.Racket));
                _localLoadoutReady = true;

                if (!_peerLoadoutReady)
                {
                    SetMessage("相手の選択を待っています");
                }

                TryStartOnlineMatch();
            });
        }

        /// <summary>相手の選択は見た目にだけ反映する（能力は相手端末で打球に反映済みで届く）</summary>
        private void HandleRemoteLoadout(int characterIndex, int racketIndex)
        {
            if (!_isOnline || _peerLoadoutReady) return;

            _loadoutApplier.ApplyOpponentLook(_loadoutCatalog.Get(characterIndex, racketIndex));
            _peerLoadoutReady = true;
            TryStartOnlineMatch();
        }

        /// <summary>
        /// 両端末の選択がそろってから試合を始める。
        /// 片方だけ先に始めると、選択中の相手にサーブが飛んでいってしまうため。
        /// 両端末とも自分を手前（Player）として扱い、最初のサーブはホストにする。
        /// </summary>
        private void TryStartOnlineMatch()
        {
            if (!_localLoadoutReady || !_peerLoadoutReady) return;
            if (CurrentState != MiniGameState.Ready) return;

            SetMessage(string.Empty);
            _score = new MatchScore(_pointsToWin, _serveChangeInterval, _isHost ? CourtSide.Player : CourtSide.Opponent);
            UpdateScoreText();

            StartGame();
            StartCoroutine(NextServeRoutine());
        }

        /// <summary>
        /// オンラインでは相手の端末は止まらないため、時間は止めずに打球の受け付けだけ止める
        /// （PAUSE中は IsPlaying が false になり、Update で CanSwing が落ちる）
        /// </summary>
        public override void PauseGame()
        {
            base.PauseGame();

            if (_isOnline)
            {
                Time.timeScale = 1f;
            }
        }

        protected override void Update()
        {
            base.Update();

            // ポーズ中や得点表示中にフリックが打球として通らないようにする
            _playerSwing.CanSwing = IsPlaying && _phase != RallyPhase.PointBreak && _ball.IsFlying;
            _playerSwing.IsServing = _phase == RallyPhase.Serving;

            // NPCが動くのはラリー中だけ（サーブ待ちやトス中は構えに戻す）
            _npc.IsActive = IsPlaying && _phase == RallyPhase.Rallying;

            if (_special != null)
            {
                _special.CanAppear = IsPlaying && _phase == RallyPhase.Rallying;
            }
        }

        /// <summary>サーブ権を確認し、プレイヤーならトス、相手なら送り出しでラリーを始める</summary>
        private IEnumerator NextServeRoutine()
        {
            _phase = RallyPhase.PointBreak;
            _ball.Stop();
            _referee.Stop();
            _npc.ResetForRally();

            CourtSide server = _score.CurrentServer;
            string serveLabel = server == CourtSide.Player ? "YOUR SERVE" : $"{OpponentLabel} SERVE";

            // 黄色ボールのラリーは、取れば強必殺技がもらえることをサーブ前に知らせる
            bool isChance = _special != null && _special.PrepareRally(_score.TotalPoints);
            SetMessage(isChance ? $"CHANCE BALL!\n{serveLabel}" : serveLabel);
            yield return new WaitForSeconds(_serveDelay);
            SetMessage(string.Empty);
            SetShotInfo(server == CourtSide.Player ? "トスを打つ" : string.Empty, _shotInfoDuration);

            if (server == CourtSide.Player)
            {
                // トスを打った時点でラリー開始とする（HandleShot）
                _phase = RallyPhase.Serving;
                _serve.TossForPlayer();
            }
            else if (_isOnline)
            {
                // 相手のサーブは、相手端末から打球として届く（HandleRemoteShot）
                _phase = RallyPhase.Rallying;
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

        /// <summary>縁に当たって跳ね方が変わった理由が分かるよう、打球結果と同じHUDで知らせる</summary>
        private void HandleEdgeBounced(Vector3 contact)
        {
            SetShotInfo("EDGE!", _shotInfoDuration);
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

            if (_isOnline)
            {
                // 打球直後のボール位置 ＝ 発射位置
                _onlineLink.SendShot(_ball.CourtPosition, shot);
            }

            SetShotInfo(BuildShotInfo(shot), _shotInfoDuration);
        }

        /// <summary>相手端末で打たれた球を、こちらの BallMotion で同じように飛ばす</summary>
        private void HandleRemoteShot(RemoteShot shot)
        {
            if (!_isOnline || CurrentState == MiniGameState.Result) return;

            // 得点表示中に届くのはサーブだけのはず。それ以外は判定済みのラリーの残りなので捨てる
            if (_phase == RallyPhase.PointBreak && !shot.IsServe) return;

            // 早送り中のバウンドも判定させるため、発射より先に審判へ知らせる
            if (shot.IsServe)
            {
                _phase = RallyPhase.Rallying;
                _referee.BeginRally(CourtSide.Opponent);
            }
            else
            {
                _referee.NotifyHit(CourtSide.Opponent);
            }

            if (shot.IsServe)
            {
                _ball.LaunchServe(shot.From, shot.ServeBouncePoint, shot.ServeTarget, shot.Spin, shot.ServeForwardSpeed);
            }
            else
            {
                _ball.Launch(shot.From, shot.Velocity, shot.Spin);
            }

            // 通信にかかった時間ぶん進めて、相手の画面とボールの位置を揃える
            _ball.FastForward(shot.Elapsed);

            _remoteOpponent.PlaySwing(shot.From);
            _audio.PlayHit(1f);
        }

        /// <summary>相手端末が判定した得点（こちらの審判は自分の打球の結果を判定しない）</summary>
        private void HandleRemotePoint(CourtSide scorer, PointReason reason)
        {
            if (!_isOnline || CurrentState == MiniGameState.Result) return;

            ApplyPoint(scorer, reason);
        }

        /// <summary>試合中に相手との接続が切れたら、そこで試合を打ち切る</summary>
        private void HandlePeerDisconnected()
        {
            if (!_isOnline || CurrentState == MiniGameState.Result) return;

            StopAllCoroutines();

            // 選択中に切れた場合、最前面の選択パネルがリザルトを隠さないよう閉じる
            if (_loadoutPanel != null)
            {
                _loadoutPanel.Hide();
            }

            _phase = RallyPhase.PointBreak;
            _ball.Stop();
            _referee.Stop();
            SetMessage(string.Empty);

            FinishGame(false, $"{_score.PlayerPoints} - {_score.OpponentPoints}", "相手との接続が切れました");
        }

        /// <summary>NPCが返球できたら、打球したものとしてラリー判定を継続する</summary>
        private void HandleNpcReturned()
        {
            _audio.PlayHit(0.8f);
            _referee.NotifyHit(CourtSide.Opponent);
        }

        private void HandleSpecialGranted(CourtSide side, SpecialData special)
        {
            string label = side == CourtSide.Player ? "SPボタンで次の打球に" : $"{OpponentLabel}が次の返球で使う";
            SetShotInfo($"{special.DisplayName} 獲得！\n{label}", _shotInfoDuration);
        }

        private void HandleNpcSpecialUsed(SpecialData special)
        {
            SetShotInfo($"{OpponentLabel}の{special.DisplayName}！", _shotInfoDuration);
        }

        private void HandleMissed(SwingJudgement judgement)
        {
            SetShotInfo($"空振り！　{TimingLabel(judgement.Timing)}", _shotInfoDuration);
        }

        private void HandlePointDecided(CourtSide scorer, PointReason reason)
        {
            // オンラインではこちらで決まった得点を相手にも伝え、両端末のスコアを揃える
            if (_isOnline)
            {
                _onlineLink.SendPoint(scorer, reason);
            }

            ApplyPoint(scorer, reason);
        }

        private void ApplyPoint(CourtSide scorer, PointReason reason)
        {
            _phase = RallyPhase.PointBreak;
            _ball.Stop();
            _referee.Stop();

            _score.AddPoint(scorer);
            UpdateScoreText();

            SetShotInfo(string.Empty, 0f);
            SetMessage($"{ReasonLabel(reason)}\n{SideLabel(scorer)} POINT");

            // 獲得の表示が打球欄に出るよう、打球欄を消した後に呼ぶ
            if (_special != null && _special.enabled)
            {
                _special.EndRally(scorer);
            }
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
                isVictory ? $"{_pointsToWin}点先取！" : (_isOnline ? "相手の勝ち" : "NPCの勝ち"));
        }

        private string SideLabel(CourtSide side)
        {
            return side == CourtSide.Player ? "YOU" : OpponentLabel;
        }

        private void UpdateScoreText()
        {
            if (_scoreText == null) return;

            // サーブ権がどちらにあるかを ● で示す
            bool playerServes = _score.CurrentServer == CourtSide.Player;
            string playerMark = playerServes ? "●" : "  ";
            string opponentMark = playerServes ? "  " : "●";
            _scoreText.text = $"{playerMark} YOU {_score.PlayerPoints} - {_score.OpponentPoints} {OpponentLabel} {opponentMark}";
        }

        /// <summary>
        /// 打球の手応え（タイミングと回転）を、プレイ中に読み取れる短さで返す
        /// </summary>
        private static string BuildShotInfo(ShotResult shot)
        {
            // 必殺技を乗せた打球は、種別の代わりに技名を出して発動したことを伝える
            string name = shot.Special != null ? shot.Special.DisplayName + "！" : ShotTypeLabel(shot.Type);
            return $"{name}　{TimingLabel(shot.Timing)}　{shot.Strength * 100f:0}%\n{DescribeSpin(shot.Spin)}";
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

        /// <summary>
        /// どの種別で打てたかを毎打球返す。
        /// 種別は打点の高さとフリック方向から自動で決まるため、
        /// これを見せないとプレイヤーが打ち分けを覚えられない。
        /// </summary>
        private static string ShotTypeLabel(ShotType type)
        {
            switch (type)
            {
                case ShotType.Smash: return "スマッシュ";
                case ShotType.Chop: return "カット";
                case ShotType.Lob: return "ロブ";
                default: return "ドライブ";
            }
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
