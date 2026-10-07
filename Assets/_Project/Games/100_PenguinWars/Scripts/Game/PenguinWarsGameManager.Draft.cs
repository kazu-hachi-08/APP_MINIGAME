using System;
using MiniGame.Common.Profile;
using MiniGame.PenguinWars.Battle;
using UnityEngine;

namespace MiniGame.PenguinWars
{
    /// <summary>
    /// オンライン対戦前のドラフト（仕様書 §6）と編成確認（§2.2）。
    /// ホスト: ゲストの準備完了 → DraftSession で抽選・時間計測 → ラウンドごとにゲストへ候補を送る → 9体揃ったらじぶんペンギン選択（.CustomPick.cs）
    /// → 編成を送って編成確認。
    /// ゲスト: 候補が届くたびに見せ、選んだらホストへ送る → じぶんペンギン選択 → 編成が届いたら編成確認（HandleDecksReceived）
    /// </summary>
    public partial class PenguinWarsGameManager
    {
        [Header("ドラフト（オンラインのみ）")]
        [SerializeField] private DraftPanel _draftPanel;
        [SerializeField] private DeckRevealPanel _deckRevealPanel;

        // ホストのみ
        private DraftSession _draft;
        // ゲストのみ。今見せているラウンド（再送で同じラウンドが2回届いても見せ直さないため）
        private int _guestDraftRound = -1;

        private void SubscribeDraft()
        {
            if (_draftPanel != null) _draftPanel.Picked += HandleDraftPanelPicked;
            if (_onlineLink == null) return;

            _onlineLink.DraftPickReceived += HandleDraftPickReceived;
            _onlineLink.DraftRoundReceived += HandleDraftRoundReceived;
        }

        private void UnsubscribeDraft()
        {
            if (_draftPanel != null) _draftPanel.Picked -= HandleDraftPanelPicked;
            if (_onlineLink == null) return;

            _onlineLink.DraftPickReceived -= HandleDraftPickReceived;
            _onlineLink.DraftRoundReceived -= HandleDraftRoundReceived;
        }

        // ---- ホスト ----

        private void BeginDraftAsHost()
        {
            var random = new System.Random(Environment.TickCount);
            _draft = new DraftSession(_battleRunner.CollectAllUnitNos(), DraftRounds, _balance.DraftOfferCount,
                _balance.DraftPickTime, random);
            Phase = PenguinWarsPhase.Draft;
            ShowDraftRoundAsHost();
        }

        /// <summary>ホストの候補は自分の画面へ、ゲストの候補はゲストへ送る（相手の候補・選択はお互い見せない）</summary>
        private void ShowDraftRoundAsHost()
        {
            _draftPanel.ShowRound(_draft.Round, DraftRounds, _draft.GetOffer(Side.Left), _draft.GetPicks(Side.Left),
                _balance.DraftPickTime);
            _onlineLink.SendDraftRound(_draft.Round, _draft.GetOffer(Side.Right), _draft.GetPicks(Side.Right));
        }

        /// <summary>
        /// 時間切れの判定はホストだけ。オンラインのポーズは timeScale を止めないので deltaTime のままでよい
        /// </summary>
        private void TickDraft()
        {
            if (_mode != MatchMode.Host || _draft == null) return;
            if (_isCustomPicking)
            {
                TickCustomPickAsHost();
                return;
            }
            if (!_draft.Tick(Time.deltaTime)) return;

            if (_draft.IsComplete) BeginCustomPickAsHost();
            else ShowDraftRoundAsHost();
        }

        /// <summary>ドラフトの9体にじぶんペンギンを足して10体で始める（並びはコスト順に BattleRunner が直す）</summary>
        private void FinishDraftAsHost()
        {
            _isCustomPicking = false;
            HideDraftPanels();
            CustomUnitDefinition hostUnit = ResolveHostCustomUnit();
            CustomUnitDefinition guestUnit = ResolveGuestCustomUnit();
            _battleRunner.RegisterVersusCustomUnits(hostUnit, guestUnit);
            // ステージの抽選もホストだけが行い、編成と一緒に送る（仕様書 §3.4）
            int stageIndex = UnityEngine.Random.Range(0, _battleRunner.VersusStageCount);
            _battleRunner.InitializeVersusHost(WithCustomUnit(_draft.GetPicks(Side.Left), CustomUnitRules.LeftNo),
                WithCustomUnit(_draft.GetPicks(Side.Right), CustomUnitRules.RightNo), stageIndex);
            BattleWorld world = _battleRunner.World;
            // 編成の送信がドラフト完了の合図を兼ねる（ゲストはこれで編成確認へ進む）
            _onlineLink.SendDecks(world.GetDeck(Side.Left), world.GetDeck(Side.Right), stageIndex, hostUnit.ToJson(), guestUnit.ToJson());
            BeginIntro(_versusDeckIntroDuration);
        }

        /// <summary>ラウンドが違う選択は、時間切れで先に進めた後に届いた古いものなので捨てる</summary>
        private void HandleDraftPickReceived(int round, int offerIndex)
        {
            if (_mode != MatchMode.Host || Phase != PenguinWarsPhase.Draft || _draft == null || _isCustomPicking || round != _draft.Round) return;

            _draft.Pick(Side.Right, offerIndex);
        }

        // ---- ゲスト ----

        private void HandleDraftRoundReceived(int round, int[] offer, int[] picks)
        {
            if (_mode != MatchMode.Guest || round <= _guestDraftRound) return;
            if (Phase != PenguinWarsPhase.ModeSelect && Phase != PenguinWarsPhase.Draft) return;

            Phase = PenguinWarsPhase.Draft;
            _guestDraftRound = round;
            // 最終ラウンドの次のラウンドは、じぶんペンギン選択の合図（BeginCustomPickAsHost）
            if (round >= DraftRounds) BeginCustomPick();
            else _draftPanel.ShowRound(round, DraftRounds, offer, picks, _balance.DraftPickTime);
        }

        // ---- 両方 ----

        private void HandleDraftPanelPicked(int offerIndex)
        {
            if (Phase != PenguinWarsPhase.Draft) return;

            if (_mode == MatchMode.Host) _draft.Pick(Side.Left, offerIndex);
            else if (_mode == MatchMode.Guest) _onlineLink.SubmitDraftPick(_guestDraftRound, offerIndex);
        }

        /// <summary>お互いの10体とステージを見せる。どちらの端末でも World の Left が自分（ゲストは反転済み）</summary>
        private void ShowDeckReveal()
        {
            BattleWorld world = _battleRunner.World;
            PenguinStageData stage = _battleRunner.CurrentVersusStage;
            _deckRevealPanel.Show(world.GetDeck(Side.Left), world.GetDeck(Side.Right),
                SeatNames.Get(MySeat), SeatNames.Get(OpponentSeat), stage != null ? stage.DisplayName : string.Empty);
        }

        private void HideDraftPanels()
        {
            if (_draftPanel != null) _draftPanel.Hide();
            if (_customPickPanel != null) _customPickPanel.Hide();
            if (_deckRevealPanel != null) _deckRevealPanel.Hide();
        }
    }
}
