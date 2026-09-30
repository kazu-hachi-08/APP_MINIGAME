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
        // フェーズ3でプレイヤー設定パネルから決めるまでの仮の人数（全員人間）
        private const int PlayerCount = 2;
        private const int RepayUnit = 1;

        [Header("Life Game")]
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

        [Header("Timing (sec)")]
        [SerializeField] private float _stepDuration = 0.22f;

        private readonly List<CarView> _cars = new List<CarView>();
        private LifeGameState _state;
        private float? _flickStrength;

        public LifePhase Phase { get; private set; }

        private int CurrentSeat => _state.CurrentSeat;

        protected override void OnGameReady()
        {
            _rouletteInput.Flicked += strength => _flickStrength = strength;
            _walletButton.onClick.AddListener(OpenWallet);
            _walletPanel.RepayRequested += HandleRepayRequested;
            _overviewButton.onClick.AddListener(_boardCamera.ToggleOverview);

            // シードは試合ごとに変えて毎回違う盤面にする（フェーズ5でホストが配る値に置き換える）
            _state = LifeGameState.Create(PlayerCount, Environment.TickCount, new LifeRuleConfig());
            BuildBoard();
            StartGame();
        }

        protected override void OnGameStart()
        {
            StartCoroutine(GameLoop());
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

            switch (_state.Pending)
            {
                case LifePending.Spin:
                    yield return PlaySpin();
                    break;
                case LifePending.Branch:
                    Phase = LifePhase.Moving;
                    yield return ChooseBranch();
                    break;
                case LifePending.JobCard:
                case LifePending.ChangeJob:
                    Phase = LifePhase.CellEvent;
                    yield return ChooseJob();
                    break;
                case LifePending.House:
                    Phase = LifePhase.CellEvent;
                    yield return ChooseHouse();
                    break;
                case LifePending.Insurance:
                    Phase = LifePhase.CellEvent;
                    yield return ChooseInsurance();
                    break;
                case LifePending.Stock:
                    Phase = LifePhase.CellEvent;
                    yield return ChooseStock();
                    break;
            }
        }

        private IEnumerator PlaySpin()
        {
            Phase = LifePhase.Spinning;
            _rouletteView.SetHint($"{LifeTexts.PlayerName(CurrentSeat)} の番\nフリックで回す");
            _flickStrength = null;
            _rouletteInput.Accepting = true;
            _walletButton.interactable = true;

            yield return new WaitUntil(() => _flickStrength.HasValue && !_walletPanel.IsOpen);

            _rouletteInput.Accepting = false;
            _walletButton.interactable = false;
            _rouletteView.SetHint("");

            List<LifeEvent> events = Apply(LifeCommandType.Spin);
            yield return _rouletteView.SpinTo(_state.LastRoll, _flickStrength.Value);

            Phase = LifePhase.Moving;
            yield return PlayEvents(events);
        }

        private List<LifeEvent> Apply(LifeCommandType type, int value = 0)
        {
            return LifeRules.Apply(_state, new LifeCommand(CurrentSeat, type, value));
        }

        /// <summary>
        /// ルールが返した出来事を順に見せる。移動は1マスずつ動かし、それ以外は文面にためて最後にまとめて出す。
        /// 手番が変わる前に出すので、次の人に端末を渡す前に結果を読める
        /// </summary>
        private IEnumerator PlayEvents(List<LifeEvent> events)
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
                    yield return ShowLines(lines);
                    continue;
                }

                string line = LifeTexts.Describe(_state, e);
                if (line != null) lines.Add(line);
            }

            yield return ShowLines(lines);
        }

        private IEnumerator ShowLines(List<string> lines)
        {
            if (lines.Count == 0) yield break;

            _moneyBar.Refresh(_state);
            yield return _eventPopup.Play(string.Join("\n", lines));
            lines.Clear();
        }

        // ------------------------------------------------------------------
        // 選択
        // ------------------------------------------------------------------
        private IEnumerator ChooseBranch()
        {
            LifeCell branch = _state.CurrentCell;
            var labels = new List<string>();
            foreach (int next in branch.Next) labels.Add($"{LifeTexts.SectionName(_state.Board[next].Section)}ルート");

            int choice = 0;
            yield return _choicePanel.ChooseOne("道を選ぶ", labels, null, index => choice = index);
            yield return PlayEvents(Apply(LifeCommandType.ChooseBranch, choice));
        }

        private IEnumerator ChooseJob()
        {
            bool isChangeJob = _state.Pending == LifePending.ChangeJob;
            var labels = new List<string>();
            foreach (int jobId in _state.JobCards) labels.Add(LifeTexts.JobCard(_state.Config, jobId));

            // 転職は今の職業のままでもよい（ChooseJob の -1）。並びの最後に置く
            if (isChangeJob) labels.Add($"今のまま\n{LifeTexts.JobName(_state.Current.JobId)}");

            int cardCount = _state.JobCards.Count;
            int choice = 0;
            yield return _choicePanel.ChooseOne(isChangeJob ? "転職する？" : "職業を選ぶ", labels, null, index => choice = index);
            yield return PlayEvents(Apply(LifeCommandType.ChooseJob, choice < cardCount ? choice : -1));
        }

        private IEnumerator ChooseHouse()
        {
            var labels = new List<string>();
            for (int id = 0; id < _state.Config.Houses.Length; id++) labels.Add(LifeTexts.HouseChoice(_state.Config, id));
            labels.Add("買わない");

            int choice = 0;
            yield return _choicePanel.ChooseOne("家を買う？（足りない分は約束手形）", labels, null, index => choice = index);
            int houseId = choice < _state.Config.Houses.Length ? choice : LifeRuleConfig.NoHouse;
            yield return PlayEvents(Apply(LifeCommandType.ChooseHouse, houseId));
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

            yield return PlayEvents(Apply(LifeCommandType.ChooseInsurance, (int)chosen));
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
            yield return PlayEvents(Apply(LifeCommandType.ChooseStock, number));
        }

        // ------------------------------------------------------------------
        // 財布
        // ------------------------------------------------------------------
        private void OpenWallet()
        {
            _walletPanel.Show(LifeTexts.Wallet(_state, CurrentSeat), CanRepay());
        }

        private void HandleRepayRequested()
        {
            if (!CanRepay()) return;

            Apply(LifeCommandType.Repay, RepayUnit);
            _walletPanel.Refresh(LifeTexts.Wallet(_state, CurrentSeat), CanRepay());
            _moneyBar.Refresh(_state);
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
            // 1台で人間だけが遊ぶので、誰が勝っても勝利扱いにする（フェーズ3でNPCが勝ったときは負け扱いにする）
            FinishGame(true, $"{LifeTexts.PlayerName(winner.Seat)} の勝ち", $"総資産 {LifeTexts.Money(winner.Total)}");
        }
    }
}
