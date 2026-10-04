using MiniGame.Common.Core;
using MiniGame.PenguinWars.Battle;
using UnityEngine;

namespace MiniGame.PenguinWars
{
    /// <summary>
    /// ペンギン大戦争の進行役。編成発表 → START! → プレイ中の経過時間 → 自城が落ちたらリザルト（仕様書 §2.1）。
    /// 戦闘そのものは BattleRunner に任せ、ここは段階の切り替えだけを持つ
    /// </summary>
    public class PenguinWarsGameManager : BaseMiniGameManager
    {
        private const string StartMessage = "START!";
        private const string NewRecordText = "NEW RECORD!";

        [SerializeField] private PenguinWarsBalance _balance;
        [SerializeField] private BattleCamera _battleCamera;
        [SerializeField] private BattleHud _hud;
        [SerializeField] private BattleRunner _battleRunner;
        [SerializeField] private DeckIntroPanel _deckIntroPanel;
        [SerializeField] private BattleEventPresenter _presenter;
        [SerializeField] private PenguinWarsAudio _audio;
        [SerializeField] private float _deckIntroDuration = 2f;
        [SerializeField] private float _startMessageDuration = 1f;

        private float _introTimer;
        // Intro の前半（編成発表）か後半（START!）か
        private bool _showingDeck;

        public PenguinWarsPhase Phase { get; private set; } = PenguinWarsPhase.Intro;
        public float ElapsedTime { get; private set; }

        protected override void OnGameReady()
        {
            _battleCamera.Initialize(_balance.FieldLength);
            _battleRunner.Initialize();
            _battleRunner.EventRaised += HandleBattleEvent;
            _presenter.CastleCollapsed += HandleCastleCollapsed;
            BeginIntro();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (_battleRunner != null) _battleRunner.EventRaised -= HandleBattleEvent;
            if (_presenter != null) _presenter.CastleCollapsed -= HandleCastleCollapsed;
        }

        private void BeginIntro()
        {
            Phase = PenguinWarsPhase.Intro;
            ElapsedTime = 0f;
            _hud.SetElapsed(ElapsedTime);
            _hud.HideMessage();
            _showingDeck = true;
            _introTimer = _deckIntroDuration;
            _deckIntroPanel.Show(_battleRunner.World.GetDeck(Side.Left));
        }

        protected override void Update()
        {
            base.Update();
            if (IsPaused) return;

            switch (Phase)
            {
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
            _audio.PlayBgm();
            StartGame();
        }

        private void ShowStartMessage()
        {
            _showingDeck = false;
            _introTimer = _startMessageDuration;
            _deckIntroPanel.Hide();
            _hud.ShowMessage(StartMessage);
        }

        private void TickPlaying()
        {
            ElapsedTime += Time.deltaTime;
            _hud.SetElapsed(ElapsedTime);
        }

        protected override void OnGamePauseStateChanged(bool isPaused)
        {
            if (Phase == PenguinWarsPhase.Playing) _battleRunner.SetRunning(!isPaused);
        }

        /// <summary>演出・音は BattleEventPresenter が受け持つので、ここは進行に関わる出来事だけ見る</summary>
        private void HandleBattleEvent(BattleEvent battleEvent)
        {
            if (battleEvent.Type == BattleEventType.CastleDestroyed && battleEvent.Side == Side.Left) BeginFinish();
        }

        /// <summary>城が崩れた瞬間に生存時間を止める。リザルトは崩れる演出（1.5秒）が終わってから出す</summary>
        private void BeginFinish()
        {
            Phase = PenguinWarsPhase.Finished;
            _battleRunner.SetRunning(false);
            _audio.StopBgm();
        }

        private void HandleCastleCollapsed(Side side)
        {
            if (side == Side.Left) EndGame();
        }

        private void EndGame()
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
