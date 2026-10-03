using System;
using System.Collections;
using System.Collections.Generic;
using MiniGame.Common.Core;
using MiniGame.Common.Online;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.LifeGame
{
    /// <summary>
    /// 人生ゲームの進行（LifePhase）を回す。ルールは LifeRules、見た目は各 View に任せ、ここは「いつ何を呼ぶか」だけを持つ。
    /// 状態を変えるのは必ず LifeRules.Apply を通す（オンラインで同じコマンド列を再生できるようにするため）。
    /// オンラインは決定論ロックステップ：自分の手番の操作だけを送り、相手の手番は届いたコマンドを同じ順に再生する（仕様書 §10.3）。
    /// </summary>
    public class LifeGameManager : BaseMiniGameManager
    {
        private const int RepayUnit = 1;
        private const int Reroll = 1;
        private const int KeepRoll = 0;
        private const int DoBet = 1;
        private const int SkipBet = 0;
        private const int NotSelected = -1;

        /// <summary>保険の選択肢の並び。選択肢の表示と選ばれた保険の変換で同じ並びを使うため</summary>
        private static readonly LifeInsurance[] InsuranceKinds = { LifeInsurance.Life, LifeInsurance.Auto, LifeInsurance.Fire };

        private const string DisconnectedTitle = "他のプレイヤーとの接続が切れました";
        private const string DisconnectedDetail = "試合を終了しました";
        private const string RankingTitle = "順位発表";

        [Header("Life Game")]
        [Tooltip("テーマ選択の並び（現代・ファンタジー・宇宙）。オンラインではこの番号を送る")]
        [SerializeField] private LifeThemeData[] _themes;
        [SerializeField] private LifeCharacterCatalog _characterCatalog;
        [SerializeField] private BoardView _boardView;
        [SerializeField] private BoardCamera _boardCamera;
        [SerializeField] private Transform _carRoot;
        [SerializeField] private RouletteView _rouletteView;
        [SerializeField] private RouletteInput _rouletteInput;
        [SerializeField] private MoneyBarView _moneyBar;
        [SerializeField] private EventPopupView _eventPopup;
        [SerializeField] private ChoicePanel _choicePanel;
        [SerializeField] private WalletPanel _walletPanel;
        [SerializeField] private Button _walletButton;
        [SerializeField] private Button _overviewButton;
        [SerializeField] private ThemeSelectPanel _themeSelectPanel;
        [SerializeField] private PlayerSetupPanel _setupPanel;
        [SerializeField] private CharacterSelectPanel _characterSelectPanel;
        [SerializeField] private TurnBannerView _turnBanner;

        [Header("Effects")]
        [SerializeField] private BoardEffects _effects;
        [SerializeField] private LifeAudio _audio;
        [SerializeField] private SettlementView _settlementView;
        [SerializeField] private VictoryShowView _victoryShow;
        [Tooltip("コマに乗せる結婚相手・子供の顔")]
        [SerializeField] private Sprite _familyFace;

        [Header("Online")]
        [SerializeField] private ModeSelectPanel _modeSelectPanel;
        [SerializeField] private OnlineSession _onlineSession;
        [SerializeField] private LifeOnlineLink _onlineLink;
        [Tooltip("フリックせずに回すときの回転演出の強さ（相手の手番・賭け・精算の家の売却）。フリックの強さは送らない（出目に影響しないため）")]
        [Range(0f, 1f)] [SerializeField] private float _remoteFlickStrength = 0.6f;
        [Tooltip("相手が選んだ選択肢を色付けして見せる時間")]
        [SerializeField] private float _remoteRevealTime = 0.8f;
        [Tooltip("宝くじの当たり番号を光らせて見せる時間")]
        [SerializeField] private float _lotteryRevealTime = 1.5f;

        [Header("Timing (sec)")]
        [SerializeField] private float _stepDuration = 0.22f;
        [Tooltip("お金の増減や結婚などの演出を続けて出すときの間。文字が重なって読めなくならないようにする")]
        [SerializeField] private float _effectInterval = 0.35f;
        [Tooltip("精算で1行ずつ出す間")]
        [SerializeField] private float _settlementStep = 0.6f;

        [Header("NPC (sec)")]
        [Tooltip("「○○の番」表示とイベント表示を自動で閉じるまでの時間（仕様書 §10.2）")]
        [SerializeField] private float _npcAutoClose = 0.8f;
        [Tooltip("回す・選ぶ前に置く間。何をしているか人間が目で追えるようにするため")]
        [SerializeField] private float _npcThinkTime = 0.8f;
        [Tooltip("NPCが回すときのフリックの強さの範囲（回る速さと時間だけに効く）")]
        [SerializeField] private Vector2 _npcFlickStrength = new Vector2(0.3f, 1f);

        private readonly List<CarView> _cars = new List<CarView>();
        private LifeGameState _state;
        private LifeThemeData _theme;
        private LifePlayerKind[] _kinds;
        private int[] _characters;

        // NPCの選択はルールの乱数と分ける（NPCの考えた回数で出目の列がずれないように）
        private LifeRandom _npcRandom;
        private float? _flickStrength;

        private bool _isOnline;
        private int _localSeat;
        private int _seed;

        // 相手の手番のコマンドは演出中にも届くので、ためておいて順に再生する
        private readonly Queue<LifeCommand> _remoteCommands = new Queue<LifeCommand>();

        public LifePhase Phase { get; private set; }

        private int CurrentSeat => _state.CurrentSeat;

        private bool IsNpcTurn => IsNpc(CurrentSeat);

        private bool IsRemoteTurn => _isOnline && CurrentSeat != _localSeat;

        protected override void OnGameReady()
        {
            // PAUSE中（オンラインは時間を止めない）は自分の操作を受け付けない
            _rouletteInput.Flicked += strength => { if (IsPlaying) _flickStrength = strength; };
            _walletButton.onClick.AddListener(OpenWallet);
            _walletPanel.RepayRequested += HandleRepayRequested;
            _overviewButton.onClick.AddListener(_boardCamera.ToggleOverview);
            _boardView.CellTapped += ShowCellInfo;
            SubscribeOnline();

            if (_modeSelectPanel != null)
            {
                _modeSelectPanel.Show(ShowThemeSelect, HandleOnlineStarted, PlayerSetupPanel.MaxPlayers);
            }
            else
            {
                ShowThemeSelect();
            }
        }

        protected override void OnGameStart()
        {
            StartCoroutine(GameLoop());
        }

        // ------------------------------------------------------------------
        // 試合前の設定
        // ------------------------------------------------------------------
        private void ShowThemeSelect()
        {
            Phase = LifePhase.ThemeSelect;
            _themeSelectPanel.Show(_themes, HandleThemeSelected);
        }

        /// <summary>背景はすぐ変えて、選んだテーマが後ろに見えるようにする</summary>
        private void HandleThemeSelected(int index)
        {
            _theme = _themes[Mathf.Clamp(index, 0, _themes.Length - 1)];
            LifeTexts.SetTheme(_theme);
            _boardCamera.SetBackground(_theme.Background);
            ShowPlayerSetup();
        }

        private void ShowPlayerSetup()
        {
            Phase = LifePhase.PlayerSetup;
            _setupPanel.Show(HandlePlayersConfirmed, ShowThemeSelect);
        }

        private void HandlePlayersConfirmed(IReadOnlyList<LifePlayerKind> kinds)
        {
            _kinds = new List<LifePlayerKind>(kinds).ToArray();
            Phase = LifePhase.CharacterSelect;
            _characterSelectPanel.Show(kinds, HandleCharactersConfirmed, ShowPlayerSetup);
        }

        private void HandleCharactersConfirmed(IReadOnlyList<int> characters)
        {
            _characters = new List<int>(characters).ToArray();
            // シードは試合ごとに変えて毎回違う盤面にする
            CreateMatch(Environment.TickCount);
        }

        private void CreateMatch(int seed)
        {
            var abilities = new LifeAbility[_characters.Length];
            for (int seat = 0; seat < abilities.Length; seat++) abilities[seat] = CharacterOf(seat).Ability;

            _state = LifeGameState.Create(abilities, seed, new LifeRuleConfig());
            _npcRandom = new LifeRandom(seed + 1);
            _walletPanel.SetRepayLabel($"手形を1枚返す（{LifeTexts.Money(_state.Config.NoteUnit)}）");
            BuildBoard();
            StartGame();
        }

        private void BuildBoard()
        {
            _boardView.Build(_state.Board, _theme);
            _boardCamera.SetBoardBounds(_boardView.Bounds);

            for (int seat = 0; seat < _state.Players.Count; seat++)
            {
                CarView car = CarView.Create(_carRoot, seat, _theme, CharacterOf(seat).Face, _familyFace);
                car.PlaceAt(_boardView.PositionOf(_state.Players[seat].Position));
                _cars.Add(car);
            }

            _boardCamera.Follow(_cars[CurrentSeat].transform);
            _boardCamera.SnapToTarget();
            _moneyBar.Refresh(_state);
        }

        private bool IsNpc(int seat) => _kinds[seat] == LifePlayerKind.Npc;

        private LifeCharacterData CharacterOf(int seat) => _characterCatalog.Get(_characters[seat]);

        /// <summary>
        /// 「P1 らっきー」のように席番号＋キャラ名。同じキャラを複数人が選べるので、キャラ名だけだと誰か分からないため。
        /// オンラインでは自分の席に「（あなた）」を付ける（所持金バーやイベント表示の P番号と自分を結びつけるため）
        /// </summary>
        private string DisplayName(int seat)
        {
            string you = _isOnline && seat == _localSeat ? "（あなた）" : "";
            return $"{LifeTexts.PlayerName(seat)} {CharacterOf(seat).DisplayName}{you}";
        }

        // ------------------------------------------------------------------
        // オンラインの試合前（仕様書 §10.3）
        // ------------------------------------------------------------------
        /// <summary>
        /// 部屋に集まった人数で遊ぶので人数設定は出さない（全員人間）。席番号＝手番の順で、先攻はホスト。
        /// ホストがテーマを選んでテーマ番号とシードを配り、届いたら各端末で自分のキャラだけを選ぶ
        /// </summary>
        private void HandleOnlineStarted(int localSeat, int playerCount)
        {
            _isOnline = true;
            _localSeat = localSeat;
            _kinds = new LifePlayerKind[playerCount];
            _characters = new int[playerCount];
            Array.Fill(_characters, NotSelected);
            _onlineLink.Begin();

            Phase = LifePhase.ThemeSelect;
            if (_onlineSession.IsHost)
            {
                _themeSelectPanel.Show(_themes, HandleHostThemeSelected);
            }
            else
            {
                _characterSelectPanel.ShowWaiting("ホストがテーマを選んでいます");
            }
        }

        private void HandleHostThemeSelected(int index)
        {
            int seed = Environment.TickCount;
            _onlineLink.SendSetup(index, seed);
            ApplyOnlineSetup(index, seed);
        }

        private void HandleRemoteSetup(int themeIndex, int seed)
        {
            if (!_isOnline || Phase != LifePhase.ThemeSelect) return;

            ApplyOnlineSetup(themeIndex, seed);
        }

        /// <summary>キャラ番号の受信は Phase が CharacterSelect のときだけ受け付けるので、パネルを出す前に変えておく</summary>
        private void ApplyOnlineSetup(int themeIndex, int seed)
        {
            _seed = seed;
            _theme = _themes[Mathf.Clamp(themeIndex, 0, _themes.Length - 1)];
            LifeTexts.SetTheme(_theme);
            _boardCamera.SetBackground(_theme.Background);

            Phase = LifePhase.CharacterSelect;
            _characterSelectPanel.ShowOnline(_localSeat, _characters.Length, HandleLocalCharacterConfirmed);
        }

        private void HandleLocalCharacterConfirmed(int characterIndex)
        {
            _onlineLink.SendCharacter(_localSeat, characterIndex);
            _characterSelectPanel.ShowWaiting("他のプレイヤーを待っています");
            SetOnlineCharacter(_localSeat, characterIndex);
        }

        private void HandleRemoteCharacter(int seat, int characterIndex)
        {
            if (!_isOnline || Phase != LifePhase.CharacterSelect) return;
            if (seat < 0 || seat >= _characters.Length) return;

            SetOnlineCharacter(seat, characterIndex);
        }

        /// <summary>範囲外の番号はここでクランプする。NotSelected と区別できなくなって待ち続けるのを防ぐため</summary>
        private void SetOnlineCharacter(int seat, int characterIndex)
        {
            _characters[seat] = Mathf.Clamp(characterIndex, 0, _characterCatalog.Count - 1);
            if (Array.IndexOf(_characters, NotSelected) >= 0) return;

            _characterSelectPanel.Hide();
            CreateMatch(_seed);
        }

        /// <summary>演出中にも届くので、ためておいて PlayRemoteCommand で届いた順に再生する</summary>
        private void HandleRemoteCommand(LifeCommand command)
        {
            if (_isOnline) _remoteCommands.Enqueue(command);
        }

        private void SubscribeOnline()
        {
            if (_onlineLink != null)
            {
                _onlineLink.OnSetupReceived += HandleRemoteSetup;
                _onlineLink.OnCharacterReceived += HandleRemoteCharacter;
                _onlineLink.OnCommandReceived += HandleRemoteCommand;
            }

            if (_onlineSession != null) _onlineSession.OnPeerDisconnected += HandlePeerDisconnected;
        }

        private void UnsubscribeOnline()
        {
            if (_onlineLink != null)
            {
                _onlineLink.OnSetupReceived -= HandleRemoteSetup;
                _onlineLink.OnCharacterReceived -= HandleRemoteCharacter;
                _onlineLink.OnCommandReceived -= HandleRemoteCommand;
            }

            if (_onlineSession != null) _onlineSession.OnPeerDisconnected -= HandlePeerDisconnected;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            UnsubscribeOnline();
        }

        /// <summary>
        /// オンラインでは相手の端末は止まらないため、時間は止めずに自分の操作受付だけ止める。
        /// PAUSE中は IsPlaying が false になり、フリックが弾かれる（ボタンは PAUSE ダイアログが覆う）
        /// </summary>
        public override void PauseGame()
        {
            base.PauseGame();

            if (_isOnline) Time.timeScale = 1f;
        }

        /// <summary>
        /// 試合中に誰か1人でも切れたら全員その時点で終了する。再接続はしない（途中から状態を揃え直す仕組みを持たないため）
        /// </summary>
        private void HandlePeerDisconnected()
        {
            if (!_isOnline || Phase == LifePhase.GameSet) return;

            StopAllCoroutines();
            Phase = LifePhase.GameSet;
            _rouletteInput.Accepting = false;
            // テーマ選択・キャラ選択・待機中や選択の途中で切れたときも、パネルを残したまま結果画面を出さないようにする
            _themeSelectPanel.gameObject.SetActive(false);
            _characterSelectPanel.Hide();
            _choicePanel.gameObject.SetActive(false);
            _eventPopup.gameObject.SetActive(false);
            _turnBanner.gameObject.SetActive(false);
            _walletPanel.gameObject.SetActive(false);
            _settlementView.Hide();

            FinishGame(false, DisconnectedTitle, DisconnectedDetail);
        }

        // ------------------------------------------------------------------
        // 進行
        // ------------------------------------------------------------------
        private IEnumerator GameLoop()
        {
            while (_state.Pending != LifePending.Finished)
            {
                yield return PlayPending();
            }

            yield return PlaySettlement();
        }

        /// <summary>ルールが待っている操作を1つ受け付けて適用し、起きたことを演出する</summary>
        private IEnumerator PlayPending()
        {
            _boardCamera.Follow(_cars[CurrentSeat].transform);
            _moneyBar.Refresh(_state);

            if (IsRemoteTurn)
            {
                if (_state.Pending == LifePending.Spin) yield return PlayTurnStart();
                yield return PlayRemoteCommand();
                yield break;
            }

            if (_state.Pending == LifePending.Spin)
            {
                yield return PlayTurnStart();
                yield return IsNpcTurn ? PlayNpcSpin() : PlaySpin();
                yield break;
            }

            Phase = _state.Pending == LifePending.Branch || _state.Pending == LifePending.Reroll
                ? LifePhase.Moving
                : LifePhase.CellEvent;
            yield return IsNpcTurn ? PlayNpcChoice() : PlayHumanChoice();
        }

        private IEnumerator PlayHumanChoice()
        {
            switch (_state.Pending)
            {
                case LifePending.Reroll:
                    yield return ChooseReroll();
                    break;
                case LifePending.Branch:
                    yield return ChooseBranch();
                    break;
                case LifePending.JobCard:
                case LifePending.ChangeJob:
                    yield return ChooseJob();
                    break;
                case LifePending.House:
                    yield return ChooseHouse();
                    break;
                case LifePending.Insurance:
                    yield return ChooseInsurance();
                    break;
                case LifePending.Stock:
                    yield return ChooseStock();
                    break;
                case LifePending.Bet:
                    yield return ChooseBet();
                    break;
                case LifePending.ChooseTarget:
                    yield return ChooseTarget();
                    break;
                case LifePending.Lottery:
                    yield return ChooseLottery();
                    break;
            }
        }

        /// <summary>
        /// 人間の番はタップで開始する（端末を渡された人が自分で始められるように）。
        /// NPCとオンラインは端末を回さないので自動で閉じる
        /// </summary>
        private IEnumerator PlayTurnStart()
        {
            Phase = LifePhase.TurnStart;
            string npc = IsNpcTurn ? "（NPC）" : "";
            bool waitForTap = !IsNpcTurn && !_isOnline;
            yield return _turnBanner.Play($"{DisplayName(CurrentSeat)}{npc} の番", LifeColors.Seat(CurrentSeat),
                waitForTap, _npcAutoClose);
        }

        private IEnumerator PlaySpin()
        {
            Phase = LifePhase.Spinning;
            _rouletteView.SetHint($"{DisplayName(CurrentSeat)} の番\nフリックで回す");
            _flickStrength = null;
            _rouletteInput.Accepting = true;
            _walletButton.interactable = true;

            yield return new WaitUntil(() => _flickStrength.HasValue && !_walletPanel.IsOpen);

            _rouletteInput.Accepting = false;
            _walletButton.interactable = false;
            _rouletteView.SetHint("");

            yield return SpinAndPlay(Apply(LifeCommandType.Spin), _flickStrength.Value, false);
        }

        /// <summary>
        /// 出目はルールが Apply の中で決めるので、回転の演出は Apply の後に出目へ合わせて止める。
        /// Apply で手番が次の人へ移っていることがあるので、自動で閉じるかは呼び出し側が Apply の前の手番で決めて渡す
        /// </summary>
        private IEnumerator SpinAndPlay(List<LifeEvent> events, float strength, bool autoClose)
        {
            yield return _rouletteView.SpinTo(_state.LastRoll, strength);

            Phase = LifePhase.Moving;
            yield return PlayEvents(events, autoClose);
        }

        /// <summary>自分の操作を適用する。オンラインでは同じコマンドを全員へ送る（相手の端末で同じ順に再生するため）</summary>
        private List<LifeEvent> Apply(LifeCommandType type, int value = 0)
        {
            var command = new LifeCommand(CurrentSeat, type, value);
            if (_isOnline) _onlineLink.SendCommand(command);
            return LifeRules.Apply(_state, command);
        }

        /// <summary>
        /// ルールが返した出来事を順に見せる。移動は1マスずつ動かし、それ以外は文面にためて最後にまとめて出す。
        /// 手番が変わる前に出すので、次の人に端末を渡す前に結果を読める。NPCの操作の結果は自動で閉じる
        /// </summary>
        private IEnumerator PlayEvents(List<LifeEvent> events, bool autoClose)
        {
            var content = new EventPopupContent();
            var lotteryTickets = new List<LifeEvent>();
            foreach (LifeEvent e in events)
            {
                if (e.Type == LifeEventType.Moved)
                {
                    _audio.PlayStep();
                    yield return _cars[e.Seat].StepTo(_boardView.PositionOf(e.Value), _stepDuration);
                    continue;
                }

                if (e.Type == LifeEventType.TurnEnded)
                {
                    yield return ShowPopup(content, autoClose);
                    content = new EventPopupContent();
                    continue;
                }

                if (e.Type == LifeEventType.Rested)
                {
                    yield return _turnBanner.Play($"{DisplayName(e.Seat)} は1回休み", LifeColors.Seat(e.Seat), false, _npcAutoClose);
                    continue;
                }

                // 宝くじの番号は出目の前に全員分まとめて届くので、ためておいて抽選のときに並べて見せる
                if (e.Type == LifeEventType.LotteryTicket)
                {
                    lotteryTickets.Add(e);
                    continue;
                }

                if (e.Type == LifeEventType.LotteryDrawn) yield return SpinLottery(lotteryTickets, e);

                // 賭けの出目はルールが Apply で決めているので、人間・NPC・相手の手番どれでも、ここで出目に合わせてルーレットを止める
                if (e.Type == LifeEventType.BetResult) yield return SpinBetRoulette(e.Value);

                if (PlayEffect(e)) yield return new WaitForSeconds(_effectInterval);

                AddToPopup(content, e);

                // 進む・戻るは、止まったマスの表示を読んでからコマを動かす（いきなり動いて何が起きたか分からなくならないように）
                if (e.Type == LifeEventType.Warped)
                {
                    yield return ShowPopup(content, autoClose);
                    content = new EventPopupContent();
                }
            }

            yield return ShowPopup(content, autoClose);
        }

        private IEnumerator SpinBetRoulette(int roll)
        {
            _rouletteView.SetHint($"賭けのルーレット\n{_state.Config.BetWinMin}以上で勝ち");
            yield return _rouletteView.SpinTo(roll, _remoteFlickStrength);
            _rouletteView.SetHint("");
        }

        /// <summary>全員の番号を並べてからルーレットを回し、当たった人の番号を光らせる。出目はルールが決めた値で止める</summary>
        private IEnumerator SpinLottery(List<LifeEvent> tickets, LifeEvent drawn)
        {
            var labels = new List<string>();
            var winner = new bool[tickets.Count];
            for (int i = 0; i < tickets.Count; i++)
            {
                labels.Add($"{DisplayName(tickets[i].Seat)}\n{tickets[i].Value}番");
                winner[i] = tickets[i].Seat == drawn.OtherSeat;
            }

            _choicePanel.ShowWatching($"宝くじの抽選！\n当たったら {LifeTexts.Money(_state.Config.LotteryPrize)}", labels, null);
            _rouletteView.SetHint("宝くじの抽選");
            yield return _rouletteView.SpinTo(drawn.Value, _remoteFlickStrength);
            _rouletteView.SetHint("");
            yield return _choicePanel.RevealAndClose(winner, _lotteryRevealTime);
        }

        private void AddToPopup(EventPopupContent content, LifeEvent e)
        {
            // 配当は出目を出した人以外にも入るので、名札（誰の操作の結果か）の決め手にしない
            if (!content.HasNameplate && e.Type != LifeEventType.Dividend) SetNameplate(content, e.Seat);

            if (e.Type == LifeEventType.Landed)
            {
                SetLandedCell(content, _state.Board[e.Value]);
                return;
            }

            string line = LifeTexts.Describe(_state, e, content.Seat);
            if (line != null) content.Lines.Add((line, LifeColors.ForEvent(e.Type)));
        }

        private void SetNameplate(EventPopupContent content, int seat)
        {
            content.Seat = seat;
            content.PlayerName = DisplayName(seat);
            content.Face = CharacterOf(seat).Face;
            content.SeatColor = LifeColors.Seat(seat);
        }

        private void SetLandedCell(EventPopupContent content, LifeCell cell)
        {
            content.Title = LifeTexts.CellName(cell.Type);
            content.Icon = _boardView.IconOf(cell.Type);
            content.IconColor = _theme.CellColor(cell.Type);
            content.Flavor = LifeTexts.CellFlavor(cell);
        }

        /// <summary>
        /// 押したマスの効果を見せる。自分がフリックする前だけ受け付ける（振り直しのフリック待ちも含む）。
        /// 移動などの演出中はイベント表示を取り合うので受け付けない
        /// </summary>
        private void ShowCellInfo(LifeCell cell)
        {
            if (!_rouletteInput.Accepting || _walletPanel.IsOpen || _eventPopup.gameObject.activeSelf) return;

            var content = new EventPopupContent();
            SetLandedCell(content, cell);
            // テーマの一言は「止まったとき」の文なので、見るだけのときは出さない
            content.Flavor = null;
            content.Lines.Add((LifeTexts.CellEffect(_state, cell), LifeColors.Info));
            StartCoroutine(_eventPopup.Play(content));
        }

        private IEnumerator ShowPopup(EventPopupContent content, bool autoClose)
        {
            if (content.IsEmpty) yield break;

            _moneyBar.Refresh(_state);
            yield return _eventPopup.Play(content, autoClose ? _npcAutoClose : EventPopupView.WaitForTap);
        }

        /// <summary>
        /// お金の増減・給料日・結婚などをコマの上の文字と音で見せる。所持金バーもここで数え始める。
        /// 演出したら true（続けて出すときに間を空けるため）
        /// </summary>
        private bool PlayEffect(LifeEvent e)
        {
            switch (e.Type)
            {
                case LifeEventType.Salary:
                    Popup(e.Seat, $"給料日 {LifeTexts.SignedMoney(e.Amount)}", LifeColors.Celebration);
                    _audio.PlayPayday();
                    _cars[e.Seat].Celebrate();
                    break;
                case LifeEventType.Dividend:
                case LifeEventType.Income:
                    Popup(e.Seat, LifeTexts.SignedMoney(e.Amount), LifeColors.Gain);
                    _audio.PlayGain();
                    break;
                case LifeEventType.Payment:
                    Popup(e.Seat, LifeTexts.SignedMoney(-e.Amount), LifeColors.Loss);
                    // 係やご祝儀で受け取った人のコマにも出し、誰にお金が渡ったか分かるようにする
                    if (e.OtherSeat != LifeEvent.Bank) Popup(e.OtherSeat, LifeTexts.SignedMoney(e.Amount), LifeColors.Gain);
                    _audio.PlayLoss();
                    break;
                case LifeEventType.InsuranceCovered:
                    Popup(e.Seat, "保険でセーフ！", LifeColors.Info);
                    _audio.PlayGain();
                    break;
                case LifeEventType.NoteIssued:
                    Popup(e.Seat, $"約束手形 +{e.Value}枚", LifeColors.Loss);
                    break;
                case LifeEventType.Married:
                case LifeEventType.ChildBorn:
                    Popup(e.Seat, e.Type == LifeEventType.Married ? "結婚！" : "誕生！", LifeColors.Family);
                    LifePlayerState player = _state.Players[e.Seat];
                    _cars[e.Seat].SetFamily(player.IsMarried, player.Children);
                    _cars[e.Seat].Celebrate();
                    _audio.PlayFamily();
                    break;
                case LifeEventType.Goal:
                    Popup(e.Seat, $"ゴール！ {e.Value + 1}着", LifeColors.Celebration);
                    _cars[e.Seat].Celebrate();
                    _audio.PlayGoal();
                    break;
                case LifeEventType.BetResult:
                    PlayBetResult(e);
                    break;
                case LifeEventType.JobSwapped:
                    // 奪われた側のコマにも出し、誰と入れ替わったか盤面で分かるようにする
                    Popup(e.Seat, "職業交換！", LifeColors.Celebration);
                    Popup(e.OtherSeat, "職業交換！", LifeColors.Celebration);
                    _audio.PlayGain();
                    break;
                case LifeEventType.Warped:
                    bool forward = e.Amount > 0;
                    Popup(e.Seat, forward ? $"{e.Amount}マス進む！" : $"{-e.Amount}マス戻る…", forward ? LifeColors.Gain : LifeColors.Loss);
                    if (forward) _audio.PlayGain();
                    else _audio.PlayLoss();
                    break;
                case LifeEventType.LotteryDrawn:
                    PlayLotteryResult(e);
                    break;
                default:
                    return false;
            }

            _moneyBar.Refresh(_state);
            return true;
        }

        private void PlayBetResult(LifeEvent e)
        {
            if (e.Amount > 0)
            {
                Popup(e.Seat, "勝ち！", LifeColors.Celebration);
                _cars[e.Seat].Celebrate();
                _audio.PlayPayday();
                return;
            }

            Popup(e.Seat, "負け…", LifeColors.Loss);
            _audio.PlayLoss();
        }

        private void PlayLotteryResult(LifeEvent e)
        {
            if (e.OtherSeat == LifeEvent.Bank)
            {
                Popup(e.Seat, "はずれ…", LifeColors.Loss);
                _audio.PlayLoss();
                return;
            }

            // 当たりは1試合に何度もないので、ゴールと同じ一番派手な音で盛り上げる
            Popup(e.OtherSeat, "大当たり！", LifeColors.Celebration);
            _cars[e.OtherSeat].Celebrate();
            _audio.PlayGoal();
        }

        private void Popup(int seat, string text, Color color)
        {
            _effects.Popup(_cars[seat].transform.position, text, color);
        }

        // ------------------------------------------------------------------
        // NPC
        // ------------------------------------------------------------------
        /// <summary>手形の返済は回す前だけ（人間と同じ）なので、返済を先に済ませてから回す</summary>
        private IEnumerator PlayNpcSpin()
        {
            Phase = LifePhase.Spinning;
            _rouletteView.SetHint($"{DisplayName(CurrentSeat)}（NPC）の番");
            yield return new WaitForSeconds(_npcThinkTime);

            LifeCommand command = LifeNpcPlanner.Plan(_state, _npcRandom);
            while (command.Type == LifeCommandType.Repay)
            {
                LifeRules.Apply(_state, command);
                _moneyBar.Refresh(_state);
                command = LifeNpcPlanner.Plan(_state, _npcRandom);
            }

            _rouletteView.SetHint("");
            float strength = UnityEngine.Random.Range(_npcFlickStrength.x, _npcFlickStrength.y);
            yield return SpinAndPlay(LifeRules.Apply(_state, command), strength, true);
        }

        private IEnumerator PlayNpcChoice()
        {
            yield return new WaitForSeconds(_npcThinkTime);

            LifeCommand command = LifeNpcPlanner.Plan(_state, _npcRandom);
            List<LifeEvent> events = LifeRules.Apply(_state, command);
            if (command.Type == LifeCommandType.ChooseReroll && command.Value == Reroll)
            {
                float strength = UnityEngine.Random.Range(_npcFlickStrength.x, _npcFlickStrength.y);
                yield return SpinAndPlay(events, strength, true);
                yield break;
            }

            yield return PlayEvents(events, true);
        }

        // ------------------------------------------------------------------
        // オンラインの相手の手番
        // ------------------------------------------------------------------
        /// <summary>
        /// 届いたコマンドを1つ再生する。手番以外・範囲外のコマンドは無視する（仕様書 §10.3）。
        /// 返済は回す前に何回でも来るので、回す／選ぶコマンドが来るまで続けて処理する（「○○の番」を出し直さないため）
        /// </summary>
        private IEnumerator PlayRemoteCommand()
        {
            Phase = _state.Pending == LifePending.Spin ? LifePhase.Spinning : LifePhase.CellEvent;
            _rouletteView.SetHint($"{DisplayName(CurrentSeat)} の番");

            // 選択待ちのときは相手の画面と同じ選択肢を出しておく（何を選んでいるところか見えるように）。
            // Apply の後だと _state が進んで選択肢が変わるので、コマンドを待つ前に作る
            bool watching = _state.Pending != LifePending.Spin;
            int optionCount = 0;
            if (watching)
            {
                BuildChoice(out string title, out List<string> labels, out List<bool> enabled);
                optionCount = labels.Count;
                _choicePanel.ShowWatching($"{DisplayName(CurrentSeat)} が選んでいます\n{title}", labels, enabled);
            }

            LifeCommand command = default;
            while (true)
            {
                yield return new WaitUntil(() => _remoteCommands.Count > 0);
                command = _remoteCommands.Dequeue();
                if (!LifeRules.IsValid(_state, command)) continue;
                if (command.Type != LifeCommandType.Repay) break;

                LifeRules.Apply(_state, command);
                _moneyBar.Refresh(_state);
            }

            _rouletteView.SetHint("");
            if (watching) yield return _choicePanel.RevealAndClose(SelectedOptions(command, optionCount), _remoteRevealTime);

            bool spins = command.Type == LifeCommandType.Spin
                || (command.Type == LifeCommandType.ChooseReroll && command.Value == Reroll);
            List<LifeEvent> events = LifeRules.Apply(_state, command);
            if (spins)
            {
                yield return SpinAndPlay(events, _remoteFlickStrength, true);
                yield break;
            }

            Phase = LifePhase.Moving;
            yield return PlayEvents(events, true);
        }

        // ------------------------------------------------------------------
        // 選択
        // ------------------------------------------------------------------
        /// <summary>振り直すときはもう一度フリックしてもらう（振り直しも自分で回した感覚にするため）</summary>
        private IEnumerator ChooseReroll()
        {
            BuildChoice(out string title, out List<string> labels, out List<bool> enabled);
            int choice = 0;
            yield return _choicePanel.ChooseOne(title, labels, enabled, index => choice = index);

            if (choice == 0)
            {
                yield return WaitForFlick("振り直し！\nフリックで回す");
                yield return SpinAndPlay(Apply(LifeCommandType.ChooseReroll, Reroll), _flickStrength.Value, false);
                yield break;
            }

            yield return PlayEvents(Apply(LifeCommandType.ChooseReroll, KeepRoll), false);
        }

        private IEnumerator WaitForFlick(string hint)
        {
            _rouletteView.SetHint(hint);
            _flickStrength = null;
            _rouletteInput.Accepting = true;

            yield return new WaitUntil(() => _flickStrength.HasValue);

            _rouletteInput.Accepting = false;
            _rouletteView.SetHint("");
        }

        private IEnumerator ChooseBranch()
        {
            BuildChoice(out string title, out List<string> labels, out List<bool> enabled);
            int choice = 0;
            yield return _choicePanel.ChooseOne(title, labels, enabled, index => choice = index);
            yield return PlayEvents(Apply(LifeCommandType.ChooseBranch, choice), false);
        }

        private IEnumerator ChooseJob()
        {
            BuildChoice(out string title, out List<string> labels, out List<bool> enabled);
            int cardCount = _state.JobCards.Count;
            int choice = 0;
            yield return _choicePanel.ChooseOne(title, labels, enabled, index => choice = index);
            yield return PlayEvents(Apply(LifeCommandType.ChooseJob, choice < cardCount ? choice : -1), false);
        }

        private IEnumerator ChooseHouse()
        {
            BuildChoice(out string title, out List<string> labels, out List<bool> enabled);
            int choice = 0;
            yield return _choicePanel.ChooseOne(title, labels, enabled, index => choice = index);
            int houseId = choice < _state.Config.Houses.Length ? choice : LifeRuleConfig.NoHouse;
            yield return PlayEvents(Apply(LifeCommandType.ChooseHouse, houseId), false);
        }

        private IEnumerator ChooseInsurance()
        {
            BuildChoice(out string title, out List<string> labels, out List<bool> enabled);
            bool[] selected = null;
            yield return _choicePanel.ChooseMany(title, labels, enabled, result => selected = result);

            LifeInsurance chosen = LifeInsurance.None;
            for (int i = 0; i < InsuranceKinds.Length; i++)
            {
                if (selected[i]) chosen |= InsuranceKinds[i];
            }

            yield return PlayEvents(Apply(LifeCommandType.ChooseInsurance, (int)chosen), false);
        }

        private IEnumerator ChooseStock()
        {
            BuildChoice(out string title, out List<string> labels, out List<bool> enabled);
            int choice = 0;
            yield return _choicePanel.ChooseOne(title, labels, enabled, index => choice = index);
            // 選択肢の並び 0〜9 が株の番号 1〜10、最後の「買わない」はルールの 0
            int number = choice < LifeRuleConfig.RouletteMax ? choice + 1 : 0;
            yield return PlayEvents(Apply(LifeCommandType.ChooseStock, number), false);
        }

        private IEnumerator ChooseBet()
        {
            BuildChoice(out string title, out List<string> labels, out List<bool> enabled);
            int choice = 0;
            yield return _choicePanel.ChooseOne(title, labels, enabled, index => choice = index);
            yield return PlayEvents(Apply(LifeCommandType.ChooseBet, choice == 0 ? DoBet : SkipBet), false);
        }

        private IEnumerator ChooseTarget()
        {
            BuildChoice(out string title, out List<string> labels, out List<bool> enabled);
            List<int> seats = TargetSeats();
            int choice = 0;
            yield return _choicePanel.ChooseOne(title, labels, enabled, index => choice = index);
            // 入れ替えの最後の「交換しない」は並びの外なので NoTarget
            int target = choice < seats.Count ? seats[choice] : LifeRules.NoTarget;
            yield return PlayEvents(Apply(LifeCommandType.ChooseTarget, target), false);
        }

        private IEnumerator ChooseLottery()
        {
            BuildChoice(out string title, out List<string> labels, out List<bool> enabled);
            int choice = 0;
            yield return _choicePanel.ChooseOne(title, labels, enabled, index => choice = index);
            // 選択肢の並び 0〜9 が番号 1〜10
            yield return PlayEvents(Apply(LifeCommandType.ChooseLottery, choice + 1), false);
        }

        /// <summary>指名・入れ替えの選択肢に並べる席（自分以外を席順に）。選べない人も灰色で出す（なぜ選べないか見せるため）</summary>
        private List<int> TargetSeats()
        {
            var seats = new List<int>();
            for (int seat = 0; seat < _state.Players.Count; seat++)
            {
                if (seat != CurrentSeat) seats.Add(seat);
            }

            return seats;
        }

        /// <summary>
        /// 今の Pending の選択肢を作る。自分の手番と、オンラインで相手の手番を見せる表示とで同じものを出すため1か所にまとめる。
        /// 並びを変えるときは各 ChooseXxx の値の変換と SelectedOptions も合わせて変えること
        /// </summary>
        private void BuildChoice(out string title, out List<string> labels, out List<bool> enabled)
        {
            labels = new List<string>();
            enabled = null;
            switch (_state.Pending)
            {
                case LifePending.Reroll:
                    // このまま進んだら止まるマスを見せ、振り直すかをマスの効果で決められるようにする
                    LifeCell stop = _state.Board[LifeRules.PreviewStop(_state, _state.LastRoll)];
                    title = $"出目は {_state.LastRoll}！ このままだと「{LifeTexts.CellName(stop.Type)}」\n"
                        + $"{LifeTexts.CellEffect(_state, stop)}\n振り直す？（1試合に1回だけ）";
                    labels.Add("振り直す");
                    labels.Add("このまま進む");
                    break;
                case LifePending.Branch:
                    title = "道を選ぶ";
                    foreach (int next in _state.CurrentCell.Next) labels.Add(LifeTexts.RouteName(_state.Board[next].Section));
                    break;
                case LifePending.JobCard:
                case LifePending.ChangeJob:
                    bool isChangeJob = _state.Pending == LifePending.ChangeJob;
                    title = isChangeJob ? "転職する？" : "職業を選ぶ";
                    foreach (int jobId in _state.JobCards) labels.Add(LifeTexts.JobCard(_state, jobId));
                    // 転職は今の職業のままでもよい（ChooseJob の -1）。並びの最後に置く
                    if (isChangeJob) labels.Add($"今のまま\n{LifeTexts.JobName(_state.Current.JobId)}");
                    break;
                case LifePending.House:
                    title = "家を買う？（足りない分は約束手形）";
                    for (int id = 0; id < _state.Config.Houses.Length; id++) labels.Add(LifeTexts.HouseChoice(_state.Config, id));
                    labels.Add("買わない");
                    break;
                case LifePending.Insurance:
                    title = "保険に入る？（いくつでも）";
                    enabled = new List<bool>();
                    LifeInsurance available = LifeRules.AvailableInsurances(_state.Current);
                    foreach (LifeInsurance kind in InsuranceKinds)
                    {
                        labels.Add(LifeTexts.InsuranceChoice(_state.Config, kind));
                        enabled.Add((available & kind) != 0);
                    }
                    break;
                case LifePending.Bet:
                    int stake = _state.CurrentCell.Amount;
                    title = $"賭ける？\n{_state.Config.BetWinMin}以上で {LifeTexts.SignedMoney(stake)}、外れたら {LifeTexts.SignedMoney(-stake)}";
                    labels.Add($"賭ける\n{LifeTexts.Money(stake)}");
                    labels.Add("やめる");
                    break;
                case LifePending.ChooseTarget:
                    BuildTargetChoice(out title, labels, out enabled);
                    break;
                case LifePending.Lottery:
                    title = $"宝くじの番号を選ぶ\n当たったら {LifeTexts.Money(_state.Config.LotteryPrize)}（他の人の番号は自動で決まる）";
                    for (int n = 1; n <= LifeRuleConfig.RouletteMax; n++) labels.Add($"{n}番");
                    break;
                default:
                    title = $"株を買う？（1枚 {LifeTexts.Money(_state.Config.StockPrice)}）\nその番号が出るたびに配当";
                    for (int n = 1; n <= LifeRuleConfig.RouletteMax; n++) labels.Add($"{n}番");
                    labels.Add("買わない");
                    break;
            }
        }

        private void BuildTargetChoice(out string title, List<string> labels, out List<bool> enabled)
        {
            bool isSwap = _state.CurrentCell.Type == LifeCellType.SwapJob;
            title = isSwap
                ? $"誰と職業を交換する？\n今の職業：{LifeTexts.JobName(_state.Current.JobId)}"
                : $"誰から {LifeTexts.Money(_state.CurrentCell.Amount)} もらう？";

            enabled = new List<bool>();
            foreach (int seat in TargetSeats())
            {
                labels.Add(LifeTexts.TargetChoice(_state, seat, DisplayName(seat)));
                enabled.Add(LifeRules.IsValidTarget(_state, seat));
            }

            if (!isSwap) return;

            labels.Add("交換しない");
            enabled.Add(true);
        }

        /// <summary>
        /// 各 ChooseXxx の「選択肢の並び → コマンドの値」の逆。相手が何を選んだかを観戦表示で光らせるために使う。
        /// 最後の「買わない／今のまま」は値が特別なので個別に戻す
        /// </summary>
        private bool[] SelectedOptions(LifeCommand command, int count)
        {
            var selected = new bool[count];
            int index;
            switch (command.Type)
            {
                case LifeCommandType.ChooseInsurance:
                    for (int i = 0; i < InsuranceKinds.Length && i < count; i++)
                    {
                        selected[i] = (command.Value & (int)InsuranceKinds[i]) != 0;
                    }
                    return selected;
                case LifeCommandType.ChooseReroll:
                    index = command.Value == Reroll ? 0 : 1;
                    break;
                case LifeCommandType.ChooseStock:
                    index = command.Value == 0 ? count - 1 : command.Value - 1;
                    break;
                case LifeCommandType.ChooseJob:
                    index = command.Value < 0 ? count - 1 : command.Value;
                    break;
                case LifeCommandType.ChooseHouse:
                    index = command.Value == LifeRuleConfig.NoHouse ? count - 1 : command.Value;
                    break;
                case LifeCommandType.ChooseBet:
                    index = command.Value == DoBet ? 0 : 1;
                    break;
                case LifeCommandType.ChooseTarget:
                    index = command.Value == LifeRules.NoTarget ? count - 1 : TargetSeats().IndexOf(command.Value);
                    break;
                case LifeCommandType.ChooseLottery:
                    index = command.Value - 1;
                    break;
                default:
                    index = command.Value;
                    break;
            }

            if (index >= 0 && index < count) selected[index] = true;
            return selected;
        }

        // ------------------------------------------------------------------
        // 財布
        // ------------------------------------------------------------------
        private void OpenWallet()
        {
            // 財布ボタンは試合前の設定画面の裏にも見えているため、試合を作る前に押されることがある
            if (_state == null) return;

            _walletPanel.Show(WalletText(), CanRepay());
        }

        private void HandleRepayRequested()
        {
            if (!CanRepay()) return;

            Apply(LifeCommandType.Repay, RepayUnit);
            _walletPanel.Refresh(WalletText(), CanRepay());
            _moneyBar.Refresh(_state);
        }

        private string WalletText()
        {
            return LifeTexts.Wallet(_state, CurrentSeat, DisplayName(CurrentSeat), CharacterOf(CurrentSeat).AbilityText);
        }

        /// <summary>返済はルーレットを回す前だけにする（移動や選択の途中で状態が変わると表示とずれるため）</summary>
        private bool CanRepay()
        {
            return Phase == LifePhase.Spinning
                && LifeRules.IsValid(_state, new LifeCommand(CurrentSeat, LifeCommandType.Repay, RepayUnit));
        }

        // ------------------------------------------------------------------
        // 精算
        // ------------------------------------------------------------------
        /// <summary>1人ずつ精算を見せ（仕様書 §8）、順位発表 → 1位の勝利演出 → ResultDialog の順に進める</summary>
        private IEnumerator PlaySettlement()
        {
            Phase = LifePhase.Settlement;
            _boardCamera.Follow(null);
            List<LifeSettlementEntry> entries = LifeSettlement.Settle(_state);
            foreach (LifeSettlementEntry entry in entries) yield return PlaySettlementOf(entry);

            _settlementView.Hide();
            _moneyBar.Refresh(_state);
            var ranking = new EventPopupContent { Title = RankingTitle };
            ranking.Lines.Add((LifeTexts.Ranking(entries, DisplayName), Color.white));
            yield return _eventPopup.Play(ranking);

            Phase = LifePhase.GameSet;
            LifeSettlementEntry winner = entries.Find(entry => entry.Rank == 1);
            LifeCharacterData character = CharacterOf(winner.Seat);
            _audio.PlayVictory();
            yield return _victoryShow.Play(character.Portrait, LifeColors.Seat(winner.Seat), DisplayName(winner.Seat),
                character.VictoryLine);

            // 1台を回して遊ぶときは人間の誰かが勝てば勝利扱い（NPCが勝ったら負け）。オンラインは自分が1位のときだけ勝利
            bool isVictory = _isOnline ? winner.Seat == _localSeat : !IsNpc(winner.Seat);
            FinishGame(isVictory, $"{DisplayName(winner.Seat)} の勝ち", $"総資産 {LifeTexts.Money(winner.Total)}");
        }

        /// <summary>
        /// 家の売却 → 株 → 保険 → 手形 の順に1行ずつ足していく。ルールは精算を一度に済ませているので、
        /// 精算前の所持金は内訳から逆算する。家の売却のルーレットはルールが決めた出目で止める
        /// </summary>
        private IEnumerator PlaySettlementOf(LifeSettlementEntry entry)
        {
            int money = entry.Total - entry.HouseSale - entry.StockSale - entry.InsuranceRefund + entry.NoteRepayment;
            _settlementView.Begin($"{DisplayName(entry.Seat)} の精算", LifeColors.Seat(entry.Seat), LifeTexts.SettlementMoney(money));
            yield return new WaitForSeconds(_settlementStep);

            if (entry.HouseRoll > 0)
            {
                _rouletteView.SetHint("家の売却ルーレット");
                yield return _rouletteView.SpinTo(entry.HouseRoll, _remoteFlickStrength);
                _rouletteView.SetHint("");
            }

            money += entry.HouseSale;
            yield return ShowSettlementLine(LifeTexts.HouseSale(entry), money, entry.HouseSale);
            money += entry.StockSale;
            yield return ShowSettlementLine(LifeTexts.StockSale(entry), money, entry.StockSale);
            money += entry.InsuranceRefund;
            yield return ShowSettlementLine(LifeTexts.InsuranceRefund(entry), money, entry.InsuranceRefund);
            money -= entry.NoteRepayment;
            yield return ShowSettlementLine(LifeTexts.NoteRepayment(entry), money, -entry.NoteRepayment);

            _settlementView.SetMoney(LifeTexts.SettlementTotal(entry.Total));
            yield return _settlementView.WaitForTap();
        }

        private IEnumerator ShowSettlementLine(string line, int money, int change)
        {
            _settlementView.AddLine(line, LifeTexts.SettlementMoney(money));
            if (change > 0) _audio.PlayGain();
            if (change < 0) _audio.PlayLoss();
            yield return new WaitForSeconds(_settlementStep);
        }
    }
}
