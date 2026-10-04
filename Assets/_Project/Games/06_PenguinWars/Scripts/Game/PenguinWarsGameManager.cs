using MiniGame.Common.Core;
using MiniGame.PenguinWars.Battle;
using UnityEngine;

namespace MiniGame.PenguinWars
{
    /// <summary>
    /// ペンギン大戦争の進行役。START! 表示 → プレイ中の経過時間 → 自城が落ちたらリザルト。
    /// 戦闘そのものは BattleRunner に任せ、ここは段階の切り替えだけを持つ
    /// </summary>
    public class PenguinWarsGameManager : BaseMiniGameManager
    {
        private const string StartMessage = "START!";

        [SerializeField] private PenguinWarsBalance _balance;
        [SerializeField] private BattleCamera _battleCamera;
        [SerializeField] private BattleHud _hud;
        [SerializeField] private BattleRunner _battleRunner;
        [SerializeField] private float _startMessageDuration = 1f;

        private float _introTimer;

        public PenguinWarsPhase Phase { get; private set; } = PenguinWarsPhase.Intro;
        public float ElapsedTime { get; private set; }

        protected override void OnGameReady()
        {
            _battleCamera.Initialize(_balance.FieldLength);
            _battleRunner.Initialize();
            _battleRunner.EventRaised += HandleBattleEvent;
            BeginIntro();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (_battleRunner != null) _battleRunner.EventRaised -= HandleBattleEvent;
        }

        private void BeginIntro()
        {
            Phase = PenguinWarsPhase.Intro;
            _introTimer = _startMessageDuration;
            ElapsedTime = 0f;
            _hud.SetElapsed(ElapsedTime);
            _hud.ShowMessage(StartMessage);
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

            _hud.HideMessage();
            Phase = PenguinWarsPhase.Playing;
            _battleRunner.SetRunning(true);
            StartGame();
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

        private void HandleBattleEvent(BattleEvent battleEvent)
        {
            if (battleEvent.Type != BattleEventType.CastleDestroyed || battleEvent.Side != Side.Left) return;

            EndGame();
        }

        /// <summary>城が崩れる演出・撃破数・ベスト記録は Phase 4 で足す。今は生存時間だけの仮のリザルト</summary>
        private void EndGame()
        {
            Phase = PenguinWarsPhase.Finished;
            _battleRunner.SetRunning(false);
            FinishGame(false, $"生存 {BattleHud.FormatTime(Mathf.FloorToInt(ElapsedTime))}");
        }
    }
}
