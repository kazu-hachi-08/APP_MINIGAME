using System;
using System.Collections.Generic;

namespace MiniGame.PenguinWars.Battle
{
    /// <summary>
    /// ステージモードの進み具合（クリア状況・★・ベストタイム・最後に遊んだステージ・最後に使った編成）。
    /// 解放キャラはクリア状況から計算する（CampaignUnlocks）ので持たない。
    /// ステージは番号ではなく ID で持つので、定義表を並べ替えてもセーブが壊れない。
    /// 定義表から消えた ID もそのまま残す（戻したときに記録が復活するように）
    /// </summary>
    public partial class CampaignProgress
    {
        private class StageRecord
        {
            public StarFlags Stars;
            /// <summary>クリアしたことがなければ null</summary>
            public float? BestSeconds;
        }

        private readonly Dictionary<string, StageRecord> _records = new Dictionary<string, StageRecord>();
        // 鍵が外れる演出を見せた（または遊び始めた）ステージ。同じ演出を2回出さないため
        private readonly HashSet<string> _revealedIds = new HashSet<string>();

        /// <summary>ステージ選択を開いたときにこのステージの章を見せる。まだ遊んでいなければ null</summary>
        public string LastPlayedId { get; set; }

        private IReadOnlyList<int> _lastDeckNos = Array.Empty<int>();

        /// <summary>最後に「けってい」した編成。まだ決めていなければ空（使う側が DeckRules.FillDefault で補う）</summary>
        public IReadOnlyList<int> LastDeckNos
        {
            get => _lastDeckNos;
            // 保存（ToJson）や補完で毎回 null を気にしなくて済むように、null は空として持つ
            set => _lastDeckNos = value ?? Array.Empty<int>();
        }

        public bool IsCleared(string stageId) => StarRule.Has(GetStars(stageId), StarFlags.Clear);

        /// <summary>最初のステージか、定義表で1つ前のステージをクリア済みなら遊べる（章の解放もこれで済む）。定義表に無い ID は遊べない</summary>
        public bool IsPlayable(string stageId)
        {
            IReadOnlyList<StageDefinition> all = StageDefinitions.All;
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i].Id != stageId) continue;
                return i == 0 || IsCleared(all[i - 1].Id);
            }
            return false;
        }

        /// <summary>
        /// 遊べるようになったのに、まだ鍵が外れる演出を見せていない。最初のステージは最初から遊べる（鍵が無い）ので対象外
        /// </summary>
        public bool NeedsUnlockReveal(string stageId)
        {
            IReadOnlyList<StageDefinition> all = StageDefinitions.All;
            if (all.Count > 0 && all[0].Id == stageId) return false;

            return IsPlayable(stageId) && !_revealedIds.Contains(stageId);
        }

        /// <summary>演出を見せた・リザルトの「つぎのステージ」から直接遊んだときに呼ぶ</summary>
        public void MarkUnlockRevealed(string stageId) => _revealedIds.Add(stageId);

        public StarFlags GetStars(string stageId)
        {
            return _records.TryGetValue(stageId, out StageRecord record) ? record.Stars : StarFlags.None;
        }

        public float? GetBestSeconds(string stageId)
        {
            return _records.TryGetValue(stageId, out StageRecord record) ? record.BestSeconds : null;
        }

        /// <summary>クリアしたときだけ呼ぶ。★は過去の分と OR し、タイムは短いほうを残す</summary>
        public StageRecordChange Record(string stageId, StarFlags stars, float seconds)
        {
            if (!_records.TryGetValue(stageId, out StageRecord record))
            {
                record = new StageRecord();
                _records[stageId] = record;
            }

            bool isFirstClear = !StarRule.Has(record.Stars, StarFlags.Clear) && StarRule.Has(stars, StarFlags.Clear);
            StarFlags newStars = stars & ~record.Stars;
            bool isNewBest = !record.BestSeconds.HasValue || seconds < record.BestSeconds.Value;
            // 記録する前の解放状況と比べる（別のステージで先に解放済みのキャラは「新しい仲間」にしない）
            List<int> newUnlockNos = isFirstClear ? CollectNewUnlocks(stageId) : new List<int>();

            record.Stars |= stars;
            if (isNewBest) record.BestSeconds = seconds;
            return new StageRecordChange(isFirstClear, isNewBest, newStars, newUnlockNos);
        }

        private List<int> CollectNewUnlocks(string stageId)
        {
            var nos = new List<int>();
            StageDefinition stage = StageDefinitions.Find(stageId);
            if (stage == null) return nos;

            foreach (int no in stage.UnlockNos)
            {
                if (!CampaignUnlocks.IsUnlocked(this, no) && !nos.Contains(no)) nos.Add(no);
            }
            return nos;
        }
    }
}
