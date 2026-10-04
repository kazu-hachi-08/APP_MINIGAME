using MiniGame.Common.Core;
using MiniGame.PenguinWars.Battle;
using UnityEngine;

namespace MiniGame.PenguinWars
{
    /// <summary>
    /// ペンギン大戦争の進行役。タイトル → モード選択 →（対戦はドラフト →）編成発表 → START! → プレイ → 城が落ちたらリザルト（仕様書 §2）。
    /// 戦闘そのものは BattleRunner に任せ、ここは段階の切り替えだけを持つ。
    /// このファイルは共通の流れとエンドレス。オンライン対戦の接続・決着は .Online.cs、ドラフト・編成確認は .Draft.cs
    /// </summary>
    public partial class PenguinWarsGameManager : BaseMiniGameManager
    {
        private const string StartMessage = "START!";
        private const string NewRecordText = "NEW RECORD!";

        // リトライでシーンを読み直したとき、エンドレスならモード選択を飛ばしてすぐ始める（仕様書 §2.3）。
        // シーンを読み直すとインスタンスの値は消えるので static に置く
        private static bool s_restartEndless;

        [SerializeField] private PenguinWarsBalance _balance;
        [SerializeField] private BattleCamera _battleCamera;
        [SerializeField] private BattleHud _hud;
        [SerializeField] private BattleRunner _battleRunner;
        [SerializeField] private DeckIntroPanel _deckIntroPanel;
        [SerializeField] private BattleEventPresenter _presenter;
        [SerializeField] private PenguinWarsAudio _audio;
        [Tooltip("未設定ならタイトルを出さずモード選択から始める")]
        [SerializeField] private PenguinWarsTitlePanel _titlePanel;
        [SerializeField] private float _deckIntroDuration = 2f;
        [SerializeField] private float _startMessageDuration = 1f;

        private float _introTimer;
        // Intro の前半（編成発表）か後半（START!）か
        private bool _showingDeck;

        public PenguinWarsPhase Phase { get; private set; } = PenguinWarsPhase.Title;
        public float ElapsedTime { get; private set; }

        protected override void OnGameReady()
        {
            _battleCamera.Initialize(_balance.FieldLength);
            _battleRunner.EventRaised += HandleBattleEvent;
            _presenter.CastleCollapsed += HandleCastleCollapsed;
            SubscribeOnline();
            SubscribeDraft();

            bool restartEndless = s_restartEndless;
            s_restartEndless = false;
            if (restartEndless) StartEndless();
            else ShowTitle();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (_battleRunner != null) _battleRunner.EventRaised -= HandleBattleEvent;
            if (_presenter != null) _presenter.CastleCollapsed -= HandleCastleCollapsed;
            UnsubscribeOnline();
            UnsubscribeDraft();
        }

        /// <summary>エンドレスはシーンを読み直してすぐ始める。対戦はモード選択からやり直す（相手を選び直せるように）</summary>
        public override void RestartGame()
        {
            s_restartEndless = !IsOnline;
            base.RestartGame();
        }

        private void ShowTitle()
        {
            if (_titlePanel == null)
            {
                ShowModeSelect();
                return;
            }

            Phase = PenguinWarsPhase.Title;
            _audio.PlayTitleBgm();
            _titlePanel.Show(ShowModeSelect, ReturnToTitle);
        }

        private void ShowModeSelect()
        {
            Phase = PenguinWarsPhase.ModeSelect;
            if (_modeSelectPanel == null) StartEndless();
            else _modeSelectPanel.Show(StartEndless, StartOnline);
        }

        private void StartEndless()
        {
            _battleRunner.InitializeEndless();
            BeginIntro(_deckIntroDuration);
        }

        private void BeginIntro(float deckDuration)
        {
            Phase = PenguinWarsPhase.Intro;
            ElapsedTime = 0f;
            RefreshTime();
            _hud.HideMessage();
            _showingDeck = true;
            _introTimer = deckDuration;
            // 対戦はお互いの10体（ドラフト中は相手の分を隠していたため）、エンドレスは自分の10体だけ
            if (IsOnline) ShowDeckReveal();
            else _deckIntroPanel.Show(_battleRunner.World.GetDeck(Side.Left));
        }

        protected override void Update()
        {
            base.Update();
            if (IsPaused) return;

            switch (Phase)
            {
                case PenguinWarsPhase.Draft:
                    TickDraft();
                    break;
                case PenguinWarsPhase.Intro:
                    TickIntro();
                    break;
                case PenguinWarsPhase.Playing:
                    TickPlaying();
                    break;
            }
        }

        private void TickIntro()
        {
            _introTimer -= Time.deltaTime;
            if (_introTimer > 0f) return;

            if (_showingDeck)
            {
                ShowStartMessage();
                return;
            }

            _hud.HideMessage();
            Phase = PenguinWarsPhase.Playing;
            _battleRunner.SetRunning(true);
            _audio.PlayBattleBgm();
            StartGame();
        }

        private void ShowStartMessage()
        {
            _showingDeck = false;
            _introTimer = _startMessageDuration;
            _deckIntroPanel.Hide();
            HideDraftPanels();
            _hud.ShowMessage(StartMessage);
        }

        private void TickPlaying()
        {
            ElapsedTime += Time.deltaTime;
            RefreshTime();
        }

        /// <summary>エンドレスは生存時間、対戦は残り時間（ゲストはホストから届いた値）を出す</summary>
        private void RefreshTime()
        {
            if (IsOnline) _hud.SetRemaining(_battleRunner.World.RemainingTime);
            else _hud.SetElapsed(ElapsedTime);
        }

        protected override void OnGamePauseStateChanged(bool isPaused)
        {
            if (Phase == PenguinWarsPhase.Playing) _battleRunner.SetRunning(!isPaused);
        }

        /// <summary>演出・音は BattleEventPresenter が受け持つので、ここは進行に関わる出来事だけ見る</summary>
        private void HandleBattleEvent(BattleEvent battleEvent)
        {
            // タイトル中はあそびかたのデモが戦場を動かしているだけなので、試合の進行には使わない
            if (Phase == PenguinWarsPhase.Finished || Phase == PenguinWarsPhase.Title) return;

            // エンドレスの右はゲートで落ちないので、城が落ちるのは自城だけ。対戦はどちらも落ちうる
            if (battleEvent.Type == BattleEventType.CastleDestroyed) BeginFinish();
            else if (battleEvent.Type == BattleEventType.TimeUp) BeginTimeUp(battleEvent.Side, battleEvent.Amount != 0);
        }

        /// <summary>城が崩れた瞬間に時間を止める。リザルトは崩れる演出（1.5秒）が終わってから出す</summary>
        private void BeginFinish()
        {
            Phase = PenguinWarsPhase.Finished;
            _battleRunner.SetRunning(false);
            _audio.StopBgm();
        }

        private void HandleCastleCollapsed(Side side)
        {
            if (Phase == PenguinWarsPhase.Title) return;

            if (IsOnline) EndVersus(side, false);
            else if (side == Side.Left) EndEndless();
        }

        private void EndEndless()
        {
            int seconds = Mathf.FloorToInt(ElapsedTime);
            int previousBest = EndlessRecord.LoadBestSeconds();
            bool isNewRecord = EndlessRecord.TryUpdateBest(seconds);
            int kills = _battleRunner.World.GetKillCount(Side.Left);

            string score = $"生存 {BattleHud.FormatTime(seconds)} / 撃破 {kills}体";
            FinishGame(false, score, BuildRecordText(isNewRecord, previousBest));
        }

        private static string BuildRecordText(bool isNewRecord, int previousBest)
        {
            if (!isNewRecord) return $"ベスト {BattleHud.FormatTime(previousBest)}";
            // 初回プレイは比べる記録がないので、前回ベストは出さない
            return previousBest > 0 ? $"{NewRecordText}（前回ベスト {BattleHud.FormatTime(previousBest)}）" : NewRecordText;
        }
    }
}
