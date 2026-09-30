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
        private const int NotSelected = -1;

        private const string DisconnectedTitle = "他のプレイヤーとの接続が切れました";
        private const string DisconnectedDetail = "試合を終了しました";

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

        [Header("Online")]
        [SerializeField] private ModeSelectPanel _modeSelectPanel;
        [SerializeField] private OnlineSession _onlineSession;
        [SerializeField] private LifeOnlineLink _onlineLink;
        [Tooltip("相手の手番の回転演出の強さ。フリックの強さは送らない（出目に影響しないため）")]
        [Range(0f, 1f)] [SerializeField] private float _remoteFlickStrength = 0.6f;

        [Header("Timing (sec)")]
        [SerializeField] private float _stepDuration = 0.22f;

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
                CarView car = CarView.Create(_carRoot, seat);
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
            var lines = new List<string>();
            foreach (LifeEvent e in events)
            {
                if (e.Type == LifeEventType.Moved)
                {
                    yield return _cars[e.Seat].StepTo(_boardView.PositionOf(e.Value), _stepDuration);
                    continue;
                }

                if (e.Type == LifeEventType.TurnEnded)
                {
                    yield return ShowLines(lines, autoClose);
                    continue;
                }

                string line = LifeTexts.Describe(_state, e);
                if (line != null) lines.Add(line);
            }

            yield return ShowLines(lines, autoClose);
        }

        private IEnumerator ShowLines(List<string> lines, bool autoClose)
        {
            if (lines.Count == 0) yield break;

            _moneyBar.Refresh(_state);
            yield return _eventPopup.Play(string.Join("\n", lines), autoClose ? _npcAutoClose : EventPopupView.WaitForTap);
            lines.Clear();
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
            var labels = new List<string> { "振り直す", "このまま進む" };
            int choice = 0;
            string title = $"出目は {_state.LastRoll}！\n振り直す？（1試合に1回だけ）";
            yield return _choicePanel.ChooseOne(title, labels, null, index => choice = index);

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
            LifeCell branch = _state.CurrentCell;
            var labels = new List<string>();
            foreach (int next in branch.Next) labels.Add(LifeTexts.RouteName(_state.Board[next].Section));

            int choice = 0;
            yield return _choicePanel.ChooseOne("道を選ぶ", labels, null, index => choice = index);
            yield return PlayEvents(Apply(LifeCommandType.ChooseBranch, choice), false);
        }

        private IEnumerator ChooseJob()
        {
            bool isChangeJob = _state.Pending == LifePending.ChangeJob;
            var labels = new List<string>();
            foreach (int jobId in _state.JobCards) labels.Add(LifeTexts.JobCard(_state, jobId));

            // 転職は今の職業のままでもよい（ChooseJob の -1）。並びの最後に置く
            if (isChangeJob) labels.Add($"今のまま\n{LifeTexts.JobName(_state.Current.JobId)}");

            int cardCount = _state.JobCards.Count;
            int choice = 0;
            yield return _choicePanel.ChooseOne(isChangeJob ? "転職する？" : "職業を選ぶ", labels, null, index => choice = index);
            yield return PlayEvents(Apply(LifeCommandType.ChooseJob, choice < cardCount ? choice : -1), false);
        }

        private IEnumerator ChooseHouse()
        {
            var labels = new List<string>();
            for (int id = 0; id < _state.Config.Houses.Length; id++) labels.Add(LifeTexts.HouseChoice(_state.Config, id));
            labels.Add("買わない");

            int choice = 0;
            yield return _choicePanel.ChooseOne("家を買う？（足りない分は約束手形）", labels, null, index => choice = index);
            int houseId = choice < _state.Config.Houses.Length ? choice : LifeRuleConfig.NoHouse;
            yield return PlayEvents(Apply(LifeCommandType.ChooseHouse, houseId), false);
        }

        private IEnumerator ChooseInsurance()
        {
            LifeInsurance[] kinds = { LifeInsurance.Life, LifeInsurance.Auto, LifeInsurance.Fire };
            LifeInsurance available = LifeRules.AvailableInsurances(_state.Current);
            var labels = new List<string>();
            var enabled = new List<bool>();
            foreach (LifeInsurance kind in kinds)
            {
                labels.Add(LifeTexts.InsuranceChoice(_state.Config, kind));
                enabled.Add((available & kind) != 0);
            }

            bool[] selected = null;
            yield return _choicePanel.ChooseMany("保険に入る？（いくつでも）", labels, enabled, result => selected = result);

            LifeInsurance chosen = LifeInsurance.None;
            for (int i = 0; i < kinds.Length; i++)
            {
                if (selected[i]) chosen |= kinds[i];
            }

            yield return PlayEvents(Apply(LifeCommandType.ChooseInsurance, (int)chosen), false);
        }

        private IEnumerator ChooseStock()
        {
            var labels = new List<string>();
            for (int n = 1; n <= LifeRuleConfig.RouletteMax; n++) labels.Add($"{n}番");
            labels.Add("買わない");

            int choice = 0;
            string title = $"株を買う？（1枚 {LifeTexts.Money(_state.Config.StockPrice)}）\nその番号が出るたびに配当";
            yield return _choicePanel.ChooseOne(title, labels, null, index => choice = index);
            // 選択肢の並び 0〜9 が株の番号 1〜10、最後の「買わない」はルールの 0
            int number = choice < LifeRuleConfig.RouletteMax ? choice + 1 : 0;
            yield return PlayEvents(Apply(LifeCommandType.ChooseStock, number), false);
        }

        // ------------------------------------------------------------------
        // 財布
        // ------------------------------------------------------------------
        private void OpenWallet()
        {
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
        private IEnumerator PlaySettlement()
        {
            Phase = LifePhase.Settlement;
            _boardCamera.Follow(null);
            List<LifeSettlementEntry> entries = LifeSettlement.Settle(_state);
            _moneyBar.Refresh(_state);
            yield return _eventPopup.Play(LifeTexts.Settlement(entries));

            Phase = LifePhase.GameSet;
            LifeSettlementEntry winner = entries.Find(entry => entry.Rank == 1);
            // 1台を回して遊ぶときは人間の誰かが勝てば勝利扱い（NPCが勝ったら負け）。オンラインは自分が1位のときだけ勝利
            bool isVictory = _isOnline ? winner.Seat == _localSeat : !IsNpc(winner.Seat);
            FinishGame(isVictory, $"{DisplayName(winner.Seat)} の勝ち", $"総資産 {LifeTexts.Money(winner.Total)}");
        }
    }
}
