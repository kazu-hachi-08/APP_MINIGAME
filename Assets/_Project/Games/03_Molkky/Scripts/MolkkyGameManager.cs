using System.Collections;
using System.Collections.Generic;
using MiniGame.Common.Core;
using UnityEngine;

namespace MiniGame.Molkky
{
    /// <summary>
    /// 手番と進行（MolkkyPhase）を回す。得点ルールは MolkkyRules、物理は PinRack / StickThrower、
    /// NPCの狙いは NpcThrower に任せ、ここは「いつ何を呼ぶか」だけを持つ。
    /// </summary>
    public class MolkkyGameManager : BaseMiniGameManager
    {
        [Header("Molkky")]
        [SerializeField] private PinRack _pinRack;
        [SerializeField] private StickThrower _stick;
        [SerializeField] private ThrowInput _input;
        [SerializeField] private NpcThrower _npc;
        [SerializeField] private ThrowSettleWatcher _settleWatcher;
        [SerializeField] private MolkkyHud _hud;
        [SerializeField] private TurnBannerView _turnBanner;
        [SerializeField] private PlayerSetupPanel _setupPanel;

        [Header("Timing (sec)")]
        [SerializeField] private float _npcBannerDuration = 0.8f;
        [Tooltip("NPCが投げる前の間。考えている感じを出す（§9.5）")]
        [SerializeField] private float _npcThinkTime = 0.8f;
        [SerializeField] private float _scoreDisplayDuration = 1.2f;
        [SerializeField] private float _pinResetDuration = 0.5f;

        private readonly List<PlayerSlot> _players = new List<PlayerSlot>();
        private int _currentIndex;

        public MolkkyPhase Phase { get; private set; }

        private PlayerSlot CurrentPlayer => _players[_currentIndex];

        protected override void OnGameReady()
        {
            _input.ThrowRequested += HandleThrowRequested;
            _input.PositionChanged += HandlePositionChanged;
            _settleWatcher.Settled += HandleSettled;

            Phase = MolkkyPhase.PlayerSetup;
            _setupPanel.Show(HandlePlayersConfirmed);
        }

        private void HandlePlayersConfirmed(IReadOnlyList<PlayerKind> kinds)
        {
            _players.Clear();
            for (int i = 0; i < kinds.Count; i++)
            {
                _players.Add(new PlayerSlot($"P{i + 1}", kinds[i]));
            }

            StartGame();
        }

        protected override void OnGameStart()
        {
            _currentIndex = 0;
            StartCoroutine(TurnStartRoutine());
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (_input != null)
            {
                _input.ThrowRequested -= HandleThrowRequested;
                _input.PositionChanged -= HandlePositionChanged;
            }

            if (_settleWatcher != null) _settleWatcher.Settled -= HandleSettled;
        }

        private IEnumerator TurnStartRoutine()
        {
            Phase = MolkkyPhase.TurnStart;
            _hud.ShowScores(_players, _currentIndex);

            bool isNpc = CurrentPlayer.IsNpc;
            yield return _turnBanner.Play($"{CurrentPlayer.Name} の番", MolkkyPlayerColors.Get(_currentIndex),
                !isNpc, _npcBannerDuration);

            Phase = MolkkyPhase.Aiming;
            if (isNpc)
            {
                yield return NpcThrowRoutine();
            }
            else
            {
                _input.IsAccepting = true;
            }
        }

        private IEnumerator NpcThrowRoutine()
        {
            ThrowRequest request = _npc.CreateRequest(CurrentPlayer);
            _stick.PlaceOnLine(request.PositionX);

            yield return new WaitForSeconds(_npcThinkTime);

            ExecuteThrow(request);
        }

        private void HandlePositionChanged(float x)
        {
            _stick.PlaceOnLine(x);
        }

        private void HandleThrowRequested(ThrowRequest request)
        {
            if (Phase != MolkkyPhase.Aiming || !IsPlaying || CurrentPlayer.IsNpc) return;

            _input.IsAccepting = false;
            ExecuteThrow(request);
        }

        private void ExecuteThrow(ThrowRequest request)
        {
            Phase = MolkkyPhase.Throwing;

            _pinRack.ArmAll();
            _stick.Throw(request);
            _settleWatcher.Begin();
        }

        private void HandleSettled()
        {
            StartCoroutine(ScoringRoutine());
        }

        private IEnumerator ScoringRoutine()
        {
            Phase = MolkkyPhase.Scoring;
            _pinRack.DisarmAll();

            List<int> fallen = _pinRack.CollectFallenNumbers();
            ThrowResult result = MolkkyRules.ApplyThrow(CurrentPlayer, fallen);
            Debug.Log($"[Molkky] {CurrentPlayer.Name}: 倒れたピン [{string.Join(", ", fallen)}] → {result.Outcome} +{result.Points} (合計 {CurrentPlayer.Score})");

            _hud.ShowScores(_players, _currentIndex);
            _hud.ShowMessage(MolkkyHud.FormatResult(result));

            yield return new WaitForSeconds(_scoreDisplayDuration);

            if (TryFinish(result)) yield break;

            Phase = MolkkyPhase.PinReset;
            _hud.ClearMessage();
            _pinRack.StandUpFallen();
            // 棒がピンの間に残っていると立て直したピンを押してしまうので、先に投擲ラインへ戻す
            _input.ResetPosition(0f);

            yield return new WaitForSeconds(_pinResetDuration);

            _currentIndex = MolkkyRules.NextPlayerIndex(_players, _currentIndex);
            yield return TurnStartRoutine();
        }

        /// <summary>50点ちょうど、または残り1人なら試合を終える</summary>
        private bool TryFinish(ThrowResult result)
        {
            PlayerSlot winner = result.Outcome == ThrowOutcome.Win
                ? CurrentPlayer
                : MolkkyRules.FindSoleSurvivor(_players);
            if (winner == null) return false;

            Phase = MolkkyPhase.GameSet;
            _hud.ClearMessage();

            string detail = result.Outcome == ThrowOutcome.Win ? "50点ちょうど！" : "他のプレイヤーが失格";
            // NPCが勝ったときは人間側の負けとして GAME OVER を出す
            FinishGame(!winner.IsNpc, $"{winner.Name} の勝ち", detail);
            return true;
        }
    }
}
