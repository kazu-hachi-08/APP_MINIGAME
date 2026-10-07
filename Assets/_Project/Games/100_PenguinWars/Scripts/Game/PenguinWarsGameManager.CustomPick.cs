using System;
using System.Collections.Generic;
using MiniGame.PenguinWars.Battle;
using UnityEngine;

namespace MiniGame.PenguinWars
{
    /// <summary>
    /// ドラフトの後の「じぶんペンギン選択」（3枠から1体）。Phase は Draft のまま扱い、切断などの処理をドラフトと共有する。
    /// ホスト: ドラフトの最終ラウンドの次のラウンドを送って合図 → 自分の選択とゲストの pw.custom がそろうか、制限時間＋猶予で試合へ。
    /// ゲスト: 合図を受けて画面を出し、選んだ定義をホストへ送る → 編成が届いたら編成確認（HandleDecksReceived）
    /// </summary>
    public partial class PenguinWarsGameManager
    {
        // ゲストの定義が届かなかったときに使うお手本（じぶんナイト）
        private const int FallbackSampleSlot = 0;

        [Header("じぶんペンギン選択（オンラインのみ）")]
        [SerializeField] private CustomPickPanel _customPickPanel;
        [Tooltip("ホストがゲストの選択を待つ、制限時間より後の猶予（秒）。時間切れでゲストが自動で選んだ分が通信の遅れで届くのを待つ")]
        [SerializeField] private float _customPickGrace = 3f;

        // ドラフトの続きとしてじぶんペンギンを選んでいる間
        private bool _isCustomPicking;
        // ホストのみ
        private float _customPickRemaining;
        private CustomUnitDefinition _hostCustomUnit;
        private CustomUnitDefinition _guestCustomUnit;

        /// <summary>編成10体のうち、じぶんペンギンの枠を除いた分だけドラフトする</summary>
        private static int DraftRounds => DeckRules.DeckSize - CustomUnitRules.SlotsInDeck;

        private void SubscribeCustomPick()
        {
            if (_customPickPanel != null) _customPickPanel.Picked += HandleCustomPicked;
            if (_onlineLink != null) _onlineLink.CustomUnitReceived += HandleCustomUnitReceived;
        }

        private void UnsubscribeCustomPick()
        {
            if (_customPickPanel != null) _customPickPanel.Picked -= HandleCustomPicked;
            if (_onlineLink != null) _onlineLink.CustomUnitReceived -= HandleCustomUnitReceived;
        }

        private void BeginCustomPick()
        {
            _isCustomPicking = true;
            _draftPanel.Hide();
            _customPickPanel.Show(_balance.DraftPickTime);
        }

        // ---- ホスト ----

        /// <summary>ゲストへの合図は「最終ラウンドの次のラウンド」（候補なし）にして、メッセージの種類を増やさない</summary>
        private void BeginCustomPickAsHost()
        {
            _customPickRemaining = _balance.DraftPickTime + _customPickGrace;
            _onlineLink.SendDraftRound(_draft.Round, Array.Empty<int>(), _draft.GetPicks(Side.Right));
            BeginCustomPick();
        }

        private void TickCustomPickAsHost()
        {
            _customPickRemaining -= Time.deltaTime;
            bool bothPicked = _hostCustomUnit != null && _guestCustomUnit != null;
            if (bothPicked || _customPickRemaining <= 0f) FinishDraftAsHost();
        }

        /// <summary>古いビルド・壊れたデータで片方だけ強くならないよう、ゲストの定義は必ずきまりに合わせて直す</summary>
        private void HandleCustomUnitReceived(string json)
        {
            if (_mode != MatchMode.Host || Phase != PenguinWarsPhase.Draft || !_isCustomPicking) return;

            CustomUnitDefinition def = CustomUnitDefinition.FromJson(json);
            if (def != null) _guestCustomUnit = CustomUnitRules.Sanitize(def);
        }

        /// <summary>自分の画面は時間切れでも自動で選ぶので、ここで代わりを使うのは念のため</summary>
        private CustomUnitDefinition ResolveHostCustomUnit()
        {
            if (_hostCustomUnit != null) return _hostCustomUnit;

            CustomUnitPresets presets = CustomUnitSave.Load();
            return CustomUnitRules.Sanitize(presets.Slots[presets.LastPickedSlot]);
        }

        private CustomUnitDefinition ResolveGuestCustomUnit()
        {
            return _guestCustomUnit ?? CustomUnitRules.Sanitize(CustomUnitPresets.CreateSample(FallbackSampleSlot));
        }

        private static List<int> WithCustomUnit(IReadOnlyList<int> draftPicks, int customNo)
        {
            var unitNos = new List<int>(draftPicks) { customNo };
            return unitNos;
        }

        // ---- ゲスト ----

        /// <summary>ホストから届いた値をそのまま使う（ここで直し直すと、ホストと数値がずれることがあるため）</summary>
        private void RegisterCustomUnitsAsGuest(string hostCustomJson, string guestCustomJson)
        {
            CustomUnitDefinition hostUnit = CustomUnitDefinition.FromJson(hostCustomJson)
                ?? CustomUnitRules.Sanitize(CustomUnitPresets.CreateSample(FallbackSampleSlot));
            CustomUnitDefinition guestUnit = CustomUnitDefinition.FromJson(guestCustomJson)
                ?? CustomUnitRules.Sanitize(CustomUnitPresets.CreateSample(FallbackSampleSlot));
            _battleRunner.RegisterVersusCustomUnits(hostUnit, guestUnit);
        }

        // ---- 両方 ----

        private void HandleCustomPicked(CustomUnitDefinition def)
        {
            if (Phase != PenguinWarsPhase.Draft || !_isCustomPicking) return;

            if (_mode == MatchMode.Host) _hostCustomUnit = CustomUnitRules.Sanitize(def);
            else if (_mode == MatchMode.Guest) _onlineLink.SubmitCustomUnit(def.ToJson());
        }
    }
}
