using System;
using MiniGame.Common.Profile;
using MiniGame.PenguinWars.Battle;
using UnityEngine;

namespace MiniGame.PenguinWars
{
    /// <summary>
    /// オンライン対戦前のドラフト（仕様書 §6）と編成確認（§2.2）。
    /// ホスト: ゲストの準備完了 → DraftSession で抽選・時間計測 → ラウンドごとにゲストへ候補を送る → 10体揃ったら編成を送って編成確認。
    /// ゲスト: 候補が届くたびに見せ、選んだらホストへ送る → 編成が届いたら編成確認（HandleDecksReceived）
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
            _draft = new DraftSession(_battleRunner.CollectAllUnitNos(), _balance.DeckSize, _balance.DraftOfferCount,
                _balance.DraftPickTime, random);
            Phase = PenguinWarsPhase.Draft;
            ShowDraftRoundAsHost();
        }

        /// <summary>ホストの候補は自分の画面へ、ゲストの候補はゲストへ送る（相手の候補・選択はお互い見せない）</summary>
        private void ShowDraftRoundAsHost()
        {
            _draftPanel.ShowRound(_draft.Round, _draft.TotalRounds, _draft.GetOffer(Side.Left), _draft.GetPicks(Side.Left),
                _balance.DraftPickTime);
            _onlineLink.SendDraftRound(_draft.Round, _draft.GetOffer(Side.Right), _draft.GetPicks(Side.Right));
        }

        /// <summary>
        /// 時間切れの判定はホストだけ。オンラインのポーズは timeScale を止めないので deltaTime のままでよい
        /// </summary>
        private void TickDraft()
        {
            if (_mode != MatchMode.Host || _draft == null || !_draft.Tick(Time.deltaTime)) return;

            if (_draft.IsComplete) FinishDraftAsHost();
            else ShowDraftRoundAsHost();
        }

        private void FinishDraftAsHost()
        {
            _draftPanel.Hide();
            _battleRunner.InitializeVersusHost(_draft.GetPicks(Side.Left), _draft.GetPicks(Side.Right));
            BattleWorld world = _battleRunner.World;
            // 編成の送信がドラフト完了の合図を兼ねる（ゲストはこれで編成確認へ進む）
            _onlineLink.SendDecks(world.GetDeck(Side.Left), world.GetDeck(Side.Right));
            BeginIntro(_versusDeckIntroDuration);
        }

        /// <summary>ラウンドが違う選択は、時間切れで先に進めた後に届いた古いものなので捨てる</summary>
        private void HandleDraftPickReceived(int round, int offerIndex)
        {
            if (_mode != MatchMode.Host || Phase != PenguinWarsPhase.Draft || _draft == null || round != _draft.Round) return;

            _draft.Pick(Side.Right, offerIndex);
        }

        // ---- ゲスト ----

        private void HandleDraftRoundReceived(int round, int[] offer, int[] picks)
        {
            if (_mode != MatchMode.Guest || round <= _guestDraftRound) return;
            if (Phase != PenguinWarsPhase.ModeSelect && Phase != PenguinWarsPhase.Draft) return;

            Phase = PenguinWarsPhase.Draft;
            _guestDraftRound = round;
            _draftPanel.ShowRound(round, _balance.DeckSize, offer, picks, _balance.DraftPickTime);
        }

        // ---- 両方 ----

        private void HandleDraftPanelPicked(int offerIndex)
        {
            if (Phase != PenguinWarsPhase.Draft) return;

            if (_mode == MatchMode.Host) _draft.Pick(Side.Left, offerIndex);
            else if (_mode == MatchMode.Guest) _onlineLink.SubmitDraftPick(_guestDraftRound, offerIndex);
        }

        /// <summary>お互いの10体を見せる。どちらの端末でも World の Left が自分（ゲストは反転済み）</summary>
        private void ShowDeckReveal()
        {
            BattleWorld world = _battleRunner.World;
            _deckRevealPanel.Show(world.GetDeck(Side.Left), world.GetDeck(Side.Right),
                SeatNames.Get(MySeat), SeatNames.Get(OpponentSeat));
        }

        private void HideDraftPanels()
        {
            if (_draftPanel != null) _draftPanel.Hide();
            if (_deckRevealPanel != null) _deckRevealPanel.Hide();
        }
    }
}
