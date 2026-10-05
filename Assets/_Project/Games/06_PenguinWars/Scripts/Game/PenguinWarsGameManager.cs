using MiniGame.Common.Core;
using MiniGame.Common.UI;
using MiniGame.PenguinWars.Battle;
using UnityEngine;

namespace MiniGame.PenguinWars
{
    /// <summary>
    /// ペンギン大戦争の進行役。タイトル → モード選択 →（対戦はドラフト →）編成発表 → START! → プレイ → 城が落ちたらリザルト（仕様書 §2）。
    /// 戦闘そのものは BattleRunner に任せ、ここは段階の切り替えだけを持つ。
    /// このファイルは共通の流れ。一人用のステージ選択・結果は .Stage.cs、オンライン対戦の接続・決着は .Online.cs、ドラフト・編成確認は .Draft.cs
    /// </summary>
    public partial class PenguinWarsGameManager : BaseMiniGameManager
    {
        private const string StartMessage = "START!";

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

        protected override void OnGameReady()
        {
            _battleCamera.Initialize(_balance.FieldLength);
            _battleRunner.EventRaised += HandleBattleEvent;
            _presenter.CastleCollapsed += HandleCastleCollapsed;
            SubscribeOnline();
            SubscribeDraft();

            _progress = CampaignSave.Load();
            if (!TryResumeStageFlow()) ShowTitle();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (_battleRunner != null) _battleRunner.EventRaised -= HandleBattleEvent;
            if (_presenter != null) _presenter.CastleCollapsed -= HandleCastleCollapsed;
            UnsubscribeOnline();
            UnsubscribeDraft();
        }

        /// <summary>ステージはシーンを読み直して同じステージをすぐ始める。対戦はモード選択からやり直す（相手を選び直せるように）</summary>
        public override void RestartGame()
        {
            if (!IsOnline && _currentStage != null) ReloadInto(_currentStage.Id);
            else base.RestartGame();
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
            if (_modeSelectPanel == null) ShowStageSelect();
            else _modeSelectPanel.Show(ShowStageSelect, StartOnline);
        }

        private void BeginIntro(float deckDuration)
        {
            Phase = PenguinWarsPhase.Intro;
            RefreshTime();
            _hud.HideMessage();
            // 対戦はステージごとに戦場の長さが違うので、試合が決まってからカメラの動ける範囲を合わせる
            _battleCamera.Initialize(_battleRunner.FieldLength);
            _showingDeck = true;
            _introTimer = deckDuration;
            // 対戦はお互いの10体（ドラフト中は相手の分を隠していたため）、ステージは自分の10体だけ
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
            RefreshTime();
        }

        /// <summary>ステージは経過時間（★3 の目標タイムと同じ World の時計）、対戦は残り時間（ゲストはホストから届いた値）を出す</summary>
        private void RefreshTime()
        {
            if (IsOnline) _hud.SetRemaining(_battleRunner.World.RemainingTime);
            else _hud.SetElapsed(_battleRunner.World.ElapsedTime);
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
            else EndStage();
        }

        /// <summary>BaseMiniGameManager.FinishGame はタイトルが VICTORY! / GAME OVER しかないので、別のタイトルを出すときは同じ手順を自前で踏む</summary>
        private void ShowCustomResult(string title, bool isVictory, string score, string detail)
        {
            ChangeState(MiniGameState.GameOver);
            OnGameOver(isVictory);
            ChangeState(MiniGameState.Result);
            if (UIManager.HasInstance) UIManager.Instance.ShowResultDialog(title, score, detail, RestartGame, ReturnToTitle);
        }
    }
}
