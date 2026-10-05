using System.Collections.Generic;
using MiniGame.PenguinWars.Battle;
using UnityEngine;

namespace MiniGame.PenguinWars
{
    /// <summary>ステージ選択・詳細・リザルトで同じ言い回しを使うための文字の組み立て</summary>
    public static class StageLabels
    {
        private const string EarnedStar = "★";
        private const string MissingStar = "☆";
        private const string NoRecord = "--:--";
        private const float PercentScale = 100f;

        /// <summary>★の並び順は詳細パネルの条件3行と同じ（クリア・城HP・タイム）</summary>
        public static readonly StarFlags[] StarOrder = { StarFlags.Clear, StarFlags.Safe, StarFlags.Fast };

        public static string Stars(StarFlags stars)
        {
            string text = string.Empty;
            foreach (StarFlags star in StarOrder) text += Star(StarRule.Has(stars, star));
            return text;
        }

        public static string Star(bool earned) => earned ? EarnedStar : MissingStar;

        public static string Condition(StageDefinition stage, StarFlags star)
        {
            switch (star)
            {
                case StarFlags.Safe:
                    return $"城のHPを{Mathf.RoundToInt(stage.SafeHpRatio * PercentScale)}%以上のこしてクリア";
                case StarFlags.Fast:
                    return $"{TimeText(stage.TargetSeconds)} 以内にクリア";
                default:
                    return "クリアする";
            }
        }

        /// <summary>リザルトの「新しく取った★」用。条件の文をそのまま並べる</summary>
        public static string NewStars(StageDefinition stage, StarFlags stars)
        {
            var lines = new List<string>();
            foreach (StarFlags star in StarOrder)
            {
                if (StarRule.Has(stars, star)) lines.Add($"{Star(true)} {Condition(stage, star)}");
            }
            return string.Join("\n", lines);
        }

        public static string TimeText(float seconds) => BattleHud.FormatTime(Mathf.FloorToInt(seconds));

        public static string BestTime(float? seconds) => seconds.HasValue ? TimeText(seconds.Value) : NoRecord;

        public static string Title(StageDefinition stage) => $"{stage.Id}  {stage.Name}";
    }
}
