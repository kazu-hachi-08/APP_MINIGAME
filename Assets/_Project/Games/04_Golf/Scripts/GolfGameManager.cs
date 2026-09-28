using System;
using System.Collections;
using System.Collections.Generic;
using MiniGame.Common.Core;
using MiniGame.Common.Online;
using UnityEngine;

namespace MiniGame.Golf
{
    /// <summary>
    /// 試合の進行。設定 → ホール開始 →「○○の番」→ ショット → 結果 を繰り返し、ホールごとにスコアカードを出す。
    /// ボールの実体は1つだけで、手番の人の止まっていた位置に置き直して打たせる（他の人のボールは OtherBallsView が表示だけする）。
    /// オンラインの送受信は GolfOnlineLink に任せ、ここは「いつ送り、届いたものをどう反映するか」だけを持つ。
    /// PAUSE と結果画面は共通基盤（BaseMiniGameManager）の PauseDialog / ResultDialog を使う。
    /// </summary>
    public class GolfGameManager : BaseMiniGameManager
    {
        /// <summary>試合前・試合後など、手番の人がいない状態</summary>
        private const int NoPlayer = -1;

        private const string OneHoleTitle = "1ホール勝負";
        private const string FinalResultTitle = "最終結果";
        private const string ShowResultLabel = "結果を見る";
        private const string NextHoleLabel = "次のホールへ";
        private const string GiveUpResult = "ギブアップ（パー×2）";
        private const string RemoteTurnHint = "打つのを待っています";
        private const string DisconnectedTitle = "他のプレイヤーとの接続が切れました";
        private const string DisconnectedDetail = "試合を終了しました";

        [SerializeField] private GolfBall _ball;
        [SerializeField] private HoleLoader _holeLoader;
        [SerializeField] private ShotInput _input;
        [SerializeField] private BallView _ballView;
        [SerializeField] private GolferView _golferView;
        [SerializeField] private GolfSetupPanel _setupPanel;
        [SerializeField] private GolfTurnBannerView _turnBanner;
        [SerializeField] private ScoreCardView _scoreCard;
        [SerializeField] private NpcGolfer _npcGolfer;
        [SerializeField] private ClubSelector _clubs;
        [SerializeField] private GolfMessageView _message;
        [SerializeField] private GolfAudio _audio;
        [SerializeField] private GolfCameraFollower _cameraFollower;
        [SerializeField] private GolfCharacterCatalog _characters;
        [SerializeField] private GolfCharacterSelectPanel _characterSelectPanel;

        [Tooltip("ボールが止まってから次の人の番を出すまでの時間（秒）。止まった場所を見せる")]
        [SerializeField] private float _shotResultDelay = 1f;

        [Tooltip("NPCの「○○の番」を自動で閉じるまでの時間（秒）。端末の受け渡しが要らないので短くする")]
        [SerializeField] private float _npcBannerSeconds = 1f;

        [Header("Online")]
        [SerializeField] private ModeSelectPanel _modeSelectPanel;
        [SerializeField] private OnlineSession _onlineSession;
        [SerializeField] private GolfOnlineLink _onlineLink;

        [Tooltip("オンラインのホール数。ロビーにモード選択を増やさないよう固定にする（登録ホールが足りなければその数）")]
        [SerializeField] private int _onlineHoleCount = GolfRules.LongModeHoleCount;

        [Tooltip("オンラインの「○○の番」を自動で閉じるまでの時間（秒）。端末の受け渡しが無いのでタップ待ちにしない")]
        [SerializeField] private float _onlineBannerSeconds = 1.5f;

        private readonly List<GolfPlayerSlot> _slots = new List<GolfPlayerSlot>();
        private readonly List<GolfHoleData> _holes = new List<GolfHoleData>();
        private readonly List<Wind> _winds = new List<Wind>();
        private List<int> _teeOrder = new List<int>();
        private bool _shotFinished;
        private bool _shotInCup;

        // 手番の人間がこの端末で打てる間 true。PAUSE 中は入力を止め、再開したらこれに戻す
        private bool _acceptingShot;

        private bool _isOnline;
        private int _localSeat;
        private GolfMatchSetup _receivedSetup;

        // 席番号 → キャラ番号。クライアントは参加した時点で受信を始めるので、試合開始の処理より先に届くこともある。
        // そのため人数が決まる前から溜めておき、揃ったかどうかは開始後に人数ぶん数えて判断する
        private readonly Dictionary<int, int> _onlineCharacters = new Dictionary<int, int>();
        private readonly Queue<GolfShotMessage> _remoteShots = new Queue<GolfShotMessage>();
        private readonly Queue<GolfShotResultMessage> _remoteResults = new Queue<GolfShotResultMessage>();

        public GolfPhase Phase { get; private set; } = GolfPhase.Setup;
        public IReadOnlyList<GolfPlayerSlot> Slots => _slots;

        /// <summary>手番の人の添字。試合前は -1</summary>
        public int CurrentPlayer { get; private set; } = NoPlayer;

        /// <summary>何ホール目か（0始まり）と全ホール数。HUD の「H1/3」表示に使う</summary>
        public int HoleNumber { get; private set; }
        public int HoleCount => _holes.Count;

        public event Action TurnStarted;

        private GolfPlayerSlot Current => _slots[CurrentPlayer];

        /// <summary>オンラインで、今の手番がこの端末の人</summary>
        private bool IsLocalOnlineTurn => _isOnline && CurrentPlayer == _localSeat;

        /// <summary>オンラインで、今の手番が他の端末の人</summary>
        private bool IsRemoteTurn => _isOnline && CurrentPlayer != _localSeat;

        private void Awake()
        {
            // 「○○の番」をタップするまで打てないようにする
            SetAcceptingShot(false);
        }

        private void OnEnable()
        {
            _ball.Launched += OnBallLaunched;
            _ball.Penalized += OnBallPenalized;
            _ball.Stopped += OnBallStopped;
            SubscribeOnline();
        }

        private void OnDisable()
        {
            _ball.Launched -= OnBallLaunched;
            _ball.Penalized -= OnBallPenalized;
            _ball.Stopped -= OnBallStopped;
            UnsubscribeOnline();
        }

        private void SubscribeOnline()
        {
            if (_onlineLink != null)
            {
                _onlineLink.OnSetupReceived += HandleSetupReceived;
                _onlineLink.OnShotReceived += HandleShotReceived;
                _onlineLink.OnResultReceived += HandleResultReceived;
                _onlineLink.OnCharacterReceived += HandleCharacterReceived;
            }

            if (_onlineSession != null)
            {
                _onlineSession.OnPeerConnected += HandlePeerConnected;
                _onlineSession.OnPeerDisconnected += HandlePeerDisconnected;
            }
        }

        private void UnsubscribeOnline()
        {
            if (_onlineLink != null)
            {
                _onlineLink.OnSetupReceived -= HandleSetupReceived;
                _onlineLink.OnShotReceived -= HandleShotReceived;
                _onlineLink.OnResultReceived -= HandleResultReceived;
                _onlineLink.OnCharacterReceived -= HandleCharacterReceived;
            }

            if (_onlineSession != null)
            {
                _onlineSession.OnPeerConnected -= HandlePeerConnected;
                _onlineSession.OnPeerDisconnected -= HandlePeerDisconnected;
            }
        }

        protected override void OnGameReady()
        {
            if (_modeSelectPanel != null)
            {
                _modeSelectPanel.Show(ShowSetupPanel, HandleOnlineStarted, GolfSetupPanel.MaxPlayers);
            }
            else
            {
                ShowSetupPanel();
            }
        }

        private void ShowSetupPanel()
        {
            Phase = GolfPhase.Setup;
            _setupPanel.Show(_holeLoader.Holes.Count, ShowCharacterSelect);
        }

        /// <summary>P1で「戻る」を押したら設定画面へ戻す。設定画面は前回の選択を覚えているので選び直しは要らない</summary>
        private void ShowCharacterSelect(int holeCount, IReadOnlyList<GolfPlayerType> types)
        {
            Phase = GolfPhase.CharacterSelect;
            _characterSelectPanel.Show(types, characters =>
            {
                ApplySetup(CreateSetup(holeCount));
                StartCoroutine(PlayMatch(types, characters));
            }, ShowSetupPanel);
        }

        /// <summary>
        /// オンラインは部屋に集まった人数で、全員人間。ホストがホールと風を決めて配り、クライアントは届くのを待つ。
        /// 各端末で自分のキャラだけを選び、ホールと風・全席のキャラ番号が揃ったら始める。
        /// 席番号＝1ホール目のティーの順番なので、ホストから打つ
        /// </summary>
        private void HandleOnlineStarted(int localSeat, int playerCount)
        {
            _isOnline = true;
            _localSeat = localSeat;
            Phase = GolfPhase.CharacterSelect;
            _onlineLink.Begin();

            if (_onlineSession.IsHost) CreateAndSendOnlineSetup();

            _characterSelectPanel.ShowOnline(localSeat, playerCount, HandleLocalCharacterConfirmed);
            StartCoroutine(PlayOnlineMatch(playerCount));
        }

        /// <summary>ホストも自分で決めた設定を「届いたもの」として持ち、クライアントと同じ待ち方で始める</summary>
        private void CreateAndSendOnlineSetup()
        {
            _receivedSetup = CreateSetup(Mathf.Min(_onlineHoleCount, _holeLoader.Holes.Count));
            _onlineLink.SendSetup(_receivedSetup);
        }

        private void HandleLocalCharacterConfirmed(int characterIndex)
        {
            _onlineLink.SendCharacter(_localSeat, characterIndex);
            SetOnlineCharacter(_localSeat, characterIndex);
        }

        private IEnumerator PlayOnlineMatch(int playerCount)
        {
            yield return new WaitUntil(() => _receivedSetup != null && HasAllOnlineCharacters(playerCount));
            _characterSelectPanel.Hide();
            ApplySetup(_receivedSetup);

            // 既定値が Human なので、人数ぶん作るだけで全員人間になる
            var types = new GolfPlayerType[playerCount];
            var characters = new int[playerCount];
            for (int i = 0; i < playerCount; i++) characters[i] = _onlineCharacters[i];
            yield return PlayMatch(types, characters);
        }

        /// <summary>範囲外の席番号は人数ぶんしか数えないので、ここで弾かなくても試合には使われない</summary>
        private bool HasAllOnlineCharacters(int playerCount)
        {
            for (int i = 0; i < playerCount; i++)
            {
                if (!_onlineCharacters.ContainsKey(i)) return false;
            }

            return true;
        }

        /// <summary>範囲外のキャラ番号はここでクランプする。全端末で同じキャラ・同じ能力にそろえるため</summary>
        private void SetOnlineCharacter(int seat, int characterIndex)
        {
            if (seat < 0) return;

            _onlineCharacters[seat] = Mathf.Clamp(characterIndex, 0, _characters.Count - 1);
        }

        private IEnumerator PlayMatch(IReadOnlyList<GolfPlayerType> types, IReadOnlyList<int> characters)
        {
            StartGame();
            SetUpPlayers(types, characters);

            for (HoleNumber = 0; HoleNumber < _holes.Count; HoleNumber++)
            {
                yield return PlayHole(_holes[HoleNumber], _winds[HoleNumber]);
                _teeOrder = GolfRules.NextTeeOrder(_teeOrder, _slots);
            }

            ShowGameSet();
        }

        /// <summary>1ホール目のティーは席順</summary>
        private void SetUpPlayers(IReadOnlyList<GolfPlayerType> types, IReadOnlyList<int> characters)
        {
            _slots.Clear();
            _teeOrder.Clear();
            for (int i = 0; i < types.Count; i++)
            {
                string characterName = _characters.Get(characters[i]).DisplayName;
                _slots.Add(new GolfPlayerSlot(i, types[i], characters[i], characterName));
                _teeOrder.Add(i);
            }
        }

        /// <summary>ホールは登録ホールから重複なしでランダム、風はホールごとにランダム</summary>
        private GolfMatchSetup CreateSetup(int holeCount)
        {
            List<int> indices = GolfRules.PickHoles(_holeLoader.Holes.Count, holeCount, new System.Random());
            var winds = new List<Wind>();
            foreach (int index in indices) winds.Add(HoleLoader.RandomWind(_holeLoader.Holes[index]));

            return new GolfMatchSetup(indices, winds);
        }

        private void ApplySetup(GolfMatchSetup setup)
        {
            _holes.Clear();
            _winds.Clear();
            for (int i = 0; i < setup.HoleIndices.Count; i++)
            {
                _holes.Add(_holeLoader.Holes[setup.HoleIndices[i]]);
                _winds.Add(setup.Winds[i]);
            }
        }

        private IEnumerator PlayHole(GolfHoleData hole, Wind wind)
        {
            StartHole(hole, wind);
            yield return PlayHoleIntro(hole);
            yield return PlayHoleBanner(hole);

            string lastResult = string.Empty;
            while (CurrentPlayer >= 0)
            {
                yield return PlayTurn(lastResult);
                lastResult = FinishShot(hole, out bool hasMessage);

                // カップイン・ギブアップの演出は見終わるまで次の人の番を出さない
                Phase = GolfPhase.ShotResult;
                yield return new WaitForSeconds(hasMessage ? _message.ShowSeconds : _shotResultDelay);

                SelectNextPlayer();
            }

            yield return ShowHoleScoreCard();
        }

        private void StartHole(GolfHoleData hole, Wind wind)
        {
            Phase = GolfPhase.HoleStart;
            _holeLoader.Load(hole, wind);
            foreach (GolfPlayerSlot slot in _slots) slot.StartHole(ToNumerics(_ball.GroundPosition));

            SelectNextPlayer();
            PlaceCurrentBall();
        }

        /// <summary>打つ順番はボール位置から決まるので、オンラインでも送らずに全端末で同じ人になる。全員終わったら負の値になる</summary>
        private void SelectNextPlayer()
        {
            CurrentPlayer = GolfRules.NextPlayer(_slots, _teeOrder, ToNumerics(_ball.CupPosition));
        }

        /// <summary>オンラインは端末の受け渡しが無いのでタップ待ちにせず、時間で閉じる</summary>
        private IEnumerator PlayHoleBanner(GolfHoleData hole)
        {
            return _isOnline
                ? _turnBanner.PlayAuto(HoleTitle(), HoleDetail(hole), Color.white, string.Empty, _onlineBannerSeconds)
                : _turnBanner.Play(HoleTitle(), HoleDetail(hole), Color.white);
        }

        /// <summary>
        /// スコアカードを出し、「次へ」が押されるまで待つ。
        /// 最後のホールは順位付きで出し、そのまま全ホールの結果として見せる
        /// </summary>
        private IEnumerator ShowHoleScoreCard()
        {
            Phase = GolfPhase.HoleResult;
            bool next = false;
            bool isLastHole = HoleNumber + 1 >= _holes.Count;
            string title = isLastHole ? FinalResultTitle : $"ホール{HoleNumber + 1} 終了";
            string nextLabel = isLastHole ? ShowResultLabel : NextHoleLabel;
            _scoreCard.Show(title, _holes, _slots, isLastHole, nextLabel, () => next = true, null);
            return new WaitUntil(() => next);
        }

        /// <summary>前のショットの結果と「○○の番」を出し、打ってボールが止まり、位置と打数が確定するまで待つ</summary>
        private IEnumerator PlayTurn(string lastResult)
        {
            StartTurn();

            string playerName = GolfPlayerColors.FullName(Current);
            Color color = GolfPlayerColors.Get(Current.Seat);
            if (Current.IsNpc)
            {
                yield return PlayNpcTurnStart(playerName, lastResult, color);
            }
            else if (_isOnline)
            {
                yield return PlayOnlineTurnStart(playerName, lastResult, color);
            }
            else
            {
                yield return PlayLocalTurnStart(playerName, lastResult, color);
            }

            yield return new WaitUntil(() => _shotFinished);
            SetAcceptingShot(false);

            if (IsRemoteTurn)
            {
                yield return ApplyRemoteResult();
            }
            else
            {
                RecordLocalResult();
            }
        }

        private void StartTurn()
        {
            Phase = GolfPhase.TurnStart;
            PlaceCurrentBall();
            ApplyCurrentCharacter();
            TurnStarted?.Invoke();
            _shotFinished = false;
        }

        /// <summary>人間・NPC・相手端末のどの手番でも渡す。予測線・NPC の狙い・相手のショットの再生もこのキャラの能力で計算するため</summary>
        private void ApplyCurrentCharacter()
        {
            GolfCharacterData character = _characters.Get(Current.CharacterIndex);
            _ball.SetCharacter(character);
            _golferView.SetCharacter(character);
        }

        /// <summary>NPC は端末の受け渡しが要らないので、バナーは時間で閉じてそのまま打たせる</summary>
        private IEnumerator PlayNpcTurnStart(string playerName, string lastResult, Color color)
        {
            yield return _turnBanner.PlayAuto($"{playerName} の番", lastResult, color, GolfPlayerColors.TypeName(Current.Type),
                _npcBannerSeconds);

            Phase = GolfPhase.Aiming;
            yield return _npcGolfer.TakeShot(Current.Type);
        }

        /// <summary>1台プレイは端末を次の人へ渡すので、バナーをタップしてから打てるようにする</summary>
        private IEnumerator PlayLocalTurnStart(string playerName, string lastResult, Color color)
        {
            yield return _turnBanner.Play($"{playerName} の番", lastResult, color);

            Phase = GolfPhase.Aiming;
            SetAcceptingShot(true);
        }

        /// <summary>自分の番なら打てるようにし、他の人の番なら届いた入力でボールを飛ばして見せる</summary>
        private IEnumerator PlayOnlineTurnStart(string playerName, string lastResult, Color color)
        {
            bool isLocal = IsLocalOnlineTurn;
            string title = isLocal ? $"{playerName}（あなた）の番" : $"{playerName} の番";
            string hint = isLocal ? string.Empty : RemoteTurnHint;
            yield return _turnBanner.PlayAuto(title, lastResult, color, hint, _onlineBannerSeconds);

            Phase = GolfPhase.Aiming;
            if (isLocal)
            {
                SetAcceptingShot(true);
                yield break;
            }

            yield return new WaitUntil(() => _remoteShots.Count > 0);
            HitRemoteShot(_remoteShots.Dequeue());
        }

        private void HitRemoteShot(GolfShotMessage shot)
        {
            _clubs.Select(shot.ClubIndex);
            _input.SetDirection(shot.Direction);
            _ball.Hit(_input.Direction, _clubs.Current.Config, shot.Power, shot.ImpactOffset, shot.Spin);
        }

        /// <summary>自分の端末で止まった結果で確定する。オンラインなら他の端末へ送る</summary>
        private void RecordLocalResult()
        {
            Current.Position = ToNumerics(_ball.GroundPosition);
            _shotInCup = _ball.IsInCup;

            if (IsLocalOnlineTurn)
            {
                _onlineLink.SendResult(new GolfShotResultMessage(_ball.GroundPosition, Current.Strokes, _shotInCup));
            }
        }

        /// <summary>
        /// 自分の端末の再生は小数の誤差でずれることがあるので、打った人の端末の結果で上書きする。
        /// 再生が終わるまで待ってから上書きするのは、ボールが途中で瞬間移動して見えないようにするため
        /// </summary>
        private IEnumerator ApplyRemoteResult()
        {
            yield return new WaitUntil(() => _remoteResults.Count > 0);
            GolfShotResultMessage result = _remoteResults.Dequeue();

            _ball.Place(result.Position);
            Current.Position = ToNumerics(result.Position);
            Current.SetStrokes(result.Strokes);
            _shotInCup = result.IsInCup;
        }

        /// <summary>カメラが次の人のボールへ寄れるよう、バナーを出す前に置き直して構えておく</summary>
        private void PlaceCurrentBall()
        {
            if (CurrentPlayer < 0) return;

            _ball.Place(ToUnity(Current.Position));
            Color color = GolfPlayerColors.Get(Current.Seat);
            _ballView.SetPlayerColor(color);
            _golferView.SetPlayerColor(color);
            _input.PrepareShot();
        }

        /// <summary>
        /// 確定した結果からカップイン・打ち切りを決め、演出を出す。次のバナーに出す結果を返す。
        /// オンラインでも打数が確定してから出すので、全端末で同じ呼び名になる
        /// </summary>
        private string FinishShot(GolfHoleData hole, out bool hasMessage)
        {
            string playerName = GolfPlayerColors.Name(Current.Seat);
            hasMessage = true;

            if (_shotInCup)
            {
                Current.HoleOut();
                _message.ShowHoleOut(Current.Strokes, hole.Par);
                _audio.PlayHoleOut(Current.Strokes, hole.Par);
                return $"{playerName} {GolfRules.ScoreName(Current.Strokes, hole.Par)}（{Current.Strokes}打）";
            }

            if (GolfRules.ShouldGiveUp(Current.Strokes, hole.Par))
            {
                Current.GiveUp(GolfRules.StrokeLimit(hole.Par));
                _message.ShowGiveUp();
                _audio.PlayGiveUp();
                return $"{playerName} {GiveUpResult}";
            }

            hasMessage = false;
            return string.Empty;
        }

        /// <summary>
        /// 共通の ResultDialog に優勝者を出す（順位の表は直前の最終スコアカードで見せている）。
        /// 1台プレイは人間が優勝すれば、オンラインは自分が優勝すれば勝ちの演出にする
        /// </summary>
        private void ShowGameSet()
        {
            Phase = GolfPhase.GameSet;
            CurrentPlayer = NoPlayer;

            int[] ranks = RankByTotal();
            var winners = new List<string>();
            bool isVictory = false;
            int winnerTotal = 0;
            for (int i = 0; i < _slots.Count; i++)
            {
                if (ranks[i] != 1) continue;

                winners.Add(GolfPlayerColors.FullName(_slots[i]));
                winnerTotal = _slots[i].Total;
                isVictory |= IsVictoryFor(i);
            }

            string score = winners.Count > 1 ? $"{string.Join("・", winners)} が同率1位" : $"{winners[0]} の優勝！";
            FinishGame(isVictory, score, $"合計 {winnerTotal}打（{GolfRules.FormatToPar(winnerTotal - TotalPar())}）");
        }

        private int[] RankByTotal()
        {
            var totals = new int[_slots.Count];
            for (int i = 0; i < _slots.Count; i++) totals[i] = _slots[i].Total;
            return GolfRules.Ranks(totals);
        }

        /// <summary>その席の優勝を、この端末では「勝ち」として演出するか</summary>
        private bool IsVictoryFor(int seat)
        {
            return _isOnline ? seat == _localSeat : !_slots[seat].IsNpc;
        }

        private int TotalPar()
        {
            int total = 0;
            foreach (GolfHoleData hole in _holes) total += hole.Par;
            return total;
        }

        /// <summary>
        /// オンラインでは他の人の端末は止まらないので、時間は止めずに自分のショット受付だけ止める。
        /// 1台プレイは時間ごと止める（ゲージ・ボール・NPC が止まる）
        /// </summary>
        public override void PauseGame()
        {
            base.PauseGame();
            if (_isOnline && IsPaused) Time.timeScale = 1f;
        }

        /// <summary>ShotInput はUIを通さず画面のタップを直接読むので、PAUSE のボタンを押したタップでゲージが進まないよう止めておく</summary>
        protected override void OnGamePauseStateChanged(bool isPaused)
        {
            _input.enabled = _acceptingShot && !isPaused;
        }

        private void SetAcceptingShot(bool accepting)
        {
            _acceptingShot = accepting;
            _input.enabled = accepting && !IsPaused;
        }

        /// <summary>コース名を出しながらグリーンからティーまでを見せる。オンラインでも全員同じ長さなので同期は要らない</summary>
        private IEnumerator PlayHoleIntro(GolfHoleData hole)
        {
            _message.ShowHoleName($"{HoleTitle()}\n{hole.DisplayName}", _cameraFollower.FlyoverSeconds);
            yield return _cameraFollower.PlayFlyover();
        }

        private string HoleTitle()
        {
            return _holes.Count > 1 ? $"ホール {HoleNumber + 1}/{_holes.Count}" : OneHoleTitle;
        }

        private string HoleDetail(GolfHoleData hole)
        {
            return $"{hole.DisplayName}  PAR{hole.Par}\n風 {Mathf.RoundToInt(_holeLoader.CurrentWind.Strength)}m";
        }

        private void OnBallLaunched()
        {
            Phase = GolfPhase.BallMoving;
            Current.AddStrokes(1);

            // Launched は Hit の中で呼ばれるので、方向・クラブ・ゲージはまだ打った瞬間の値のまま
            if (IsLocalOnlineTurn)
            {
                _onlineLink.SendShot(new GolfShotMessage(_input.Direction, _clubs.CurrentIndex, _input.Gauge.Power,
                    _input.Gauge.ImpactOffset, _input.Spin));
            }
        }

        private void OnBallPenalized(GroundType ground)
        {
            Current.AddStrokes(GolfRules.PenaltyStrokes);
            _message.ShowPenalty(ground);
        }

        private void OnBallStopped()
        {
            _shotFinished = true;
        }

        /// <summary>クライアントは参加できた時点で受信を始める。ホストの開始メッセージの直後にホールと風が届くため</summary>
        private void HandlePeerConnected(bool isHost)
        {
            _onlineLink.Begin();
        }

        private void HandleSetupReceived(GolfMatchSetup setup)
        {
            _receivedSetup = setup;
        }

        private void HandleShotReceived(GolfShotMessage shot)
        {
            _remoteShots.Enqueue(shot);
        }

        private void HandleResultReceived(GolfShotResultMessage result)
        {
            _remoteResults.Enqueue(result);
        }

        private void HandleCharacterReceived(int seat, int characterIndex)
        {
            SetOnlineCharacter(seat, characterIndex);
        }

        /// <summary>試合中（キャラ選択・待機中も含む）に1人でも切れたら全員その時点で終了する。再接続はしない</summary>
        private void HandlePeerDisconnected()
        {
            if (!_isOnline || Phase == GolfPhase.GameSet) return;

            StopAllCoroutines();
            Phase = GolfPhase.GameSet;
            CurrentPlayer = NoPlayer;
            SetAcceptingShot(false);
            HideMatchPanels();
            FinishGame(false, DisconnectedTitle, DisconnectedDetail);
        }

        /// <summary>止めたコルーチンが閉じるはずだった表示を、結果画面の手前に残さないよう閉じる</summary>
        private void HideMatchPanels()
        {
            _turnBanner.gameObject.SetActive(false);
            _scoreCard.gameObject.SetActive(false);
            // キャラ選択・待機中に切れたときも、選択パネルを残したまま結果画面を出さないようにする
            _characterSelectPanel.Hide();
        }

        private static Vector2 ToUnity(System.Numerics.Vector2 v) => new Vector2(v.X, v.Y);
        private static System.Numerics.Vector2 ToNumerics(Vector2 v) => new System.Numerics.Vector2(v.x, v.y);
    }
}
