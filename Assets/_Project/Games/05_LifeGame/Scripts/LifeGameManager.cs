using System;
using System.Collections;
using System.Collections.Generic;
using MiniGame.Common.Core;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.LifeGame
{
    /// <summary>
    /// 人生ゲームの進行（LifePhase）を回す。ルールは LifeRules、見た目は各 View に任せ、ここは「いつ何を呼ぶか」だけを持つ。
    /// 状態を変えるのは必ず LifeRules.Apply を通す（フェーズ5のオンラインで同じコマンド列を再生できるようにするため）。
    /// </summary>
    public class LifeGameManager : BaseMiniGameManager
    {
        private const int RepayUnit = 1;
        private const int Reroll = 1;
        private const int KeepRoll = 0;

        [Header("Life Game")]
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
        [SerializeField] private PlayerSetupPanel _setupPanel;
        [SerializeField] private CharacterSelectPanel _characterSelectPanel;
        [SerializeField] private TurnBannerView _turnBanner;

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
        private LifePlayerKind[] _kinds;
        private int[] _characters;

        // NPCの選択はルールの乱数と分ける（NPCの考えた回数で出目の列がずれないように）
        private LifeRandom _npcRandom;
        private float? _flickStrength;

        public LifePhase Phase { get; private set; }

        private int CurrentSeat => _state.CurrentSeat;

        private bool IsNpcTurn => IsNpc(CurrentSeat);

        protected override void OnGameReady()
        {
            _rouletteInput.Flicked += strength => _flickStrength = strength;
            _walletButton.onClick.AddListener(OpenWallet);
            _walletPanel.RepayRequested += HandleRepayRequested;
            _overviewButton.onClick.AddListener(_boardCamera.ToggleOverview);

            ShowPlayerSetup();
        }

        protected override void OnGameStart()
        {
            StartCoroutine(GameLoop());
        }

        // ------------------------------------------------------------------
        // 試合前の設定
        // ------------------------------------------------------------------
        private void ShowPlayerSetup()
        {
            Phase = LifePhase.PlayerSetup;
            _setupPanel.Show(HandlePlayersConfirmed);
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
            var abilities = new LifeAbility[_characters.Length];
            for (int seat = 0; seat < abilities.Length; seat++) abilities[seat] = CharacterOf(seat).Ability;

            // シードは試合ごとに変えて毎回違う盤面にする（フェーズ5でホストが配る値に置き換える）
            int seed = Environment.TickCount;
            _state = LifeGameState.Create(abilities, seed, new LifeRuleConfig());
            _npcRandom = new LifeRandom(seed + 1);
            BuildBoard();
            StartGame();
        }

        private void BuildBoard()
        {
            _boardView.Build(_state.Board);
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

        /// <summary>「P1 らっきー」のように席番号＋キャラ名。同じキャラを複数人が選べるので、キャラ名だけだと誰か分からないため</summary>
        private string DisplayName(int seat) => $"{LifeTexts.PlayerName(seat)} {CharacterOf(seat).DisplayName}";

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

        /// <summary>人間の番はタップで開始する（端末を渡された人が自分で始められるように）。NPCは自動で閉じる</summary>
        private IEnumerator PlayTurnStart()
        {
            Phase = LifePhase.TurnStart;
            string npc = IsNpcTurn ? "（NPC）" : "";
            yield return _turnBanner.Play($"{DisplayName(CurrentSeat)}{npc} の番", LifeColors.Seat(CurrentSeat),
                !IsNpcTurn, _npcAutoClose);
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

            yield return SpinAndPlay(Apply(LifeCommandType.Spin), _flickStrength.Value);
        }

        /// <summary>出目はルールが Apply の中で決めるので、回転の演出は Apply の後に出目へ合わせて止める</summary>
        private IEnumerator SpinAndPlay(List<LifeEvent> events, float strength)
        {
            bool isNpc = IsNpcTurn;
            yield return _rouletteView.SpinTo(_state.LastRoll, strength);

            Phase = LifePhase.Moving;
            yield return PlayEvents(events, isNpc);
        }

        private List<LifeEvent> Apply(LifeCommandType type, int value = 0)
        {
            return LifeRules.Apply(_state, new LifeCommand(CurrentSeat, type, value));
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
            yield return SpinAndPlay(LifeRules.Apply(_state, command), strength);
        }

        private IEnumerator PlayNpcChoice()
        {
            yield return new WaitForSeconds(_npcThinkTime);

            LifeCommand command = LifeNpcPlanner.Plan(_state, _npcRandom);
            List<LifeEvent> events = LifeRules.Apply(_state, command);
            if (command.Type == LifeCommandType.ChooseReroll && command.Value == Reroll)
            {
                float strength = UnityEngine.Random.Range(_npcFlickStrength.x, _npcFlickStrength.y);
                yield return SpinAndPlay(events, strength);
                yield break;
            }

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
                yield return SpinAndPlay(Apply(LifeCommandType.ChooseReroll, Reroll), _flickStrength.Value);
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
            foreach (int next in branch.Next) labels.Add($"{LifeTexts.SectionName(_state.Board[next].Section)}ルート");

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
            // 1台を回して遊ぶので、人間の誰かが勝てば勝利扱い。NPCが勝ったら負け扱いにする
            FinishGame(!IsNpc(winner.Seat), $"{DisplayName(winner.Seat)} の勝ち", $"総資産 {LifeTexts.Money(winner.Total)}");
        }
    }
}
