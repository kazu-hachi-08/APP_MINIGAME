using MiniGame.Common.Core;
using UnityEngine;

namespace MiniGame.PenguinWars
{
    /// <summary>
    /// ペンギン大戦争の進行役。Phase 1 では START! 表示 → プレイ中の経過時間だけを扱う
    /// </summary>
    public class PenguinWarsGameManager : BaseMiniGameManager
    {
        private const string StartMessage = "START!";

        [SerializeField] private PenguinWarsBalance _balance;
        [SerializeField] private BattleCamera _battleCamera;
        [SerializeField] private BattleHud _hud;
        [SerializeField] private CastleView _leftCastle;
        [SerializeField] private CastleView _rightCastle;
        [SerializeField] private float _startMessageDuration = 1f;

        private float _introTimer;

        public PenguinWarsPhase Phase { get; private set; } = PenguinWarsPhase.Intro;
        public float ElapsedTime { get; private set; }

        protected override void OnGameReady()
        {
            _battleCamera.Initialize(_balance.FieldLength);
            _leftCastle.SetHp(_balance.CastleHpEndless, _balance.CastleHpEndless);
            // エンドレスの右端は無敵の出現ゲートなので HP を出さない（仕様書 §2.1）
            _rightCastle.HideHp();
            BeginIntro();
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
            StartGame();
        }

        private void TickPlaying()
        {
            ElapsedTime += Time.deltaTime;
            _hud.SetElapsed(ElapsedTime);
        }
    }
}
