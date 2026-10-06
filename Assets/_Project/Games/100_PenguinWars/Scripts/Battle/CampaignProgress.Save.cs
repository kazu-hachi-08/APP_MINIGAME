using System;
using System.Collections.Generic;
using System.Text;

namespace MiniGame.PenguinWars.Battle
{
    /// <summary>進み具合の保存形式（JSON）。ゲームの規則と分けて、保存形式だけを見て直せるようにする</summary>
    public partial class CampaignProgress
    {
        private const int SaveVersion = 1;
        private const string VersionKey = "version";
        private const string LastPlayedKey = "lastPlayed";
        private const string StagesKey = "stages";
        private const string StarsKey = "stars";
        private const string BestKey = "best";
        private const string DeckKey = "deck";
        private const string RevealedKey = "revealed";

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
