using System;
using System.Collections.Generic;
using System.Text;

namespace MiniGame.PenguinWars.Battle
{
    /// <summary>Record の戻り値。リザルトに「NEW RECORD!」「新しく取った★」「なかまになった！」を出すために使う</summary>
    public readonly struct StageRecordChange
    {
        public bool IsFirstClear { get; }
        public bool IsNewBest { get; }
        public StarFlags NewStars { get; }
        /// <summary>このクリアで新しく使えるようになったキャラの No。初クリアでなければ空</summary>
        public IReadOnlyList<int> NewUnlockNos { get; }

        public StageRecordChange(bool isFirstClear, bool isNewBest, StarFlags newStars, IReadOnlyList<int> newUnlockNos)
        {
            IsFirstClear = isFirstClear;
            IsNewBest = isNewBest;
            NewStars = newStars;
            NewUnlockNos = newUnlockNos ?? Array.Empty<int>();
        }
    }

    /// <summary>
    /// ステージモードの進み具合（クリア状況・★・ベストタイム・最後に遊んだステージ・最後に使った編成）。
    /// 解放キャラはクリア状況から計算する（CampaignUnlocks）ので持たない。
    /// ステージは番号ではなく ID で持つので、定義表を並べ替えてもセーブが壊れない。
    /// 定義表から消えた ID もそのまま残す（戻したときに記録が復活するように）
    /// </summary>
    public class CampaignProgress
    {
        private const int SaveVersion = 1;
        private const string VersionKey = "version";
        private const string LastPlayedKey = "lastPlayed";
        private const string StagesKey = "stages";
        private const string StarsKey = "stars";
        private const string BestKey = "best";
        private const string DeckKey = "deck";
        private const string RevealedKey = "revealed";

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

        /// <summary>最後に「けってい」した編成。まだ決めていなければ空（使う側が DeckRules.FillDefault で補う）</summary>
        public IReadOnlyList<int> LastDeckNos { get; set; } = Array.Empty<int>();

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

        // ---- 保存形式 ----

        public string ToJson()
        {
            var builder = new StringBuilder("{");
            builder.Append(MiniJson.Quote(VersionKey)).Append(':').Append(SaveVersion);
            if (LastPlayedId != null) builder.Append(',').Append(MiniJson.Quote(LastPlayedKey)).Append(':').Append(MiniJson.Quote(LastPlayedId));
            AppendDeck(builder);
            AppendRevealed(builder);
            builder.Append(',').Append(MiniJson.Quote(StagesKey)).Append(":{");
            bool first = true;
            foreach (KeyValuePair<string, StageRecord> pair in _records)
            {
                if (!first) builder.Append(',');
                first = false;
                AppendRecord(builder, pair.Key, pair.Value);
            }
            return builder.Append("}}").ToString();
        }

        private void AppendDeck(StringBuilder builder)
        {
            builder.Append(',').Append(MiniJson.Quote(DeckKey)).Append(":[");
            for (int i = 0; i < LastDeckNos.Count; i++)
            {
                if (i > 0) builder.Append(',');
                builder.Append(LastDeckNos[i]);
            }
            builder.Append(']');
        }

        private void AppendRevealed(StringBuilder builder)
        {
            builder.Append(',').Append(MiniJson.Quote(RevealedKey)).Append(":[");
            bool first = true;
            foreach (string id in _revealedIds)
            {
                if (!first) builder.Append(',');
                first = false;
                builder.Append(MiniJson.Quote(id));
            }
            builder.Append(']');
        }

        private static void AppendRecord(StringBuilder builder, string stageId, StageRecord record)
        {
            builder.Append(MiniJson.Quote(stageId)).Append(":{");
            builder.Append(MiniJson.Quote(StarsKey)).Append(':').Append((int)record.Stars);
            if (record.BestSeconds.HasValue) builder.Append(',').Append(MiniJson.Quote(BestKey)).Append(':').Append(MiniJson.Number(record.BestSeconds.Value));
            builder.Append('}');
        }

        /// <summary>空・壊れた文字列なら空の進み具合を返す（セーブが読めなくてもゲームは始められるように）</summary>
        public static CampaignProgress FromJson(string json)
        {
            var progress = new CampaignProgress();
            try
            {
                if (MiniJson.Parse(json) is Dictionary<string, object> root) progress.Load(root);
            }
            catch (FormatException)
            {
                return new CampaignProgress();
            }
            return progress;
        }

        private static List<int> ReadNos(List<object> items)
        {
            var nos = new List<int>();
            foreach (object item in items)
            {
                if (item is double value) nos.Add((int)value);
            }
            return nos;
        }

        private void ReadRevealed(List<object> items)
        {
            foreach (object item in items)
            {
                if (item is string id) _revealedIds.Add(id);
            }
        }

        /// <summary>型が違う項目は読み飛ばす（手で書き換えられた・古い形式のセーブでも落ちないように）</summary>
        private void Load(Dictionary<string, object> root)
        {
            if (root.TryGetValue(LastPlayedKey, out object last)) LastPlayedId = last as string;
            if (root.TryGetValue(DeckKey, out object deck) && deck is List<object> deckItems) LastDeckNos = ReadNos(deckItems);
            if (root.TryGetValue(RevealedKey, out object revealed) && revealed is List<object> revealedItems) ReadRevealed(revealedItems);
            if (!root.TryGetValue(StagesKey, out object stagesValue) || !(stagesValue is Dictionary<string, object> stages)) return;

            foreach (KeyValuePair<string, object> pair in stages)
            {
                if (!(pair.Value is Dictionary<string, object> fields)) continue;

                var record = new StageRecord();
                if (fields.TryGetValue(StarsKey, out object stars) && stars is double starsValue)
                {
                    record.Stars = (StarFlags)(int)starsValue & StarFlags.All;
                }
                if (fields.TryGetValue(BestKey, out object best) && best is double bestValue) record.BestSeconds = (float)bestValue;
                _records[pair.Key] = record;
            }
        }
    }
}
