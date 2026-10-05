using System;
using System.Collections.Generic;
using System.Text;

namespace MiniGame.PenguinWars.Battle
{
    /// <summary>
    /// 全ステージ × bot の強さ × シードを回して Markdown の表にする（Simulate Stages メニューの中身）。
    /// 表の作り方を純C#に置くのは、Unity を開かずに同じ表を出して確かめられるようにするため
    /// </summary>
    public static class StageSimReport
    {
        /// <summary>★3 の目安 = うまい bot の平均秒 × この値（少し工夫が要るくらい。計画書 Phase 5）</summary>
        private const float TargetSecondsHint = 0.9f;

        private static readonly BotSkill[] Skills = { BotSkill.Normal, BotSkill.Skilled };

        /// <param name="onProgress">（今の行の説明, 0〜1）。エディタの進捗バー用。null でよい</param>
        public static string Build(StageSimulator simulator, IReadOnlyList<StageDefinition> stages, int runsPerSkill, float maxSeconds,
            Action<string, float> onProgress = null)
        {
            var text = new StringBuilder();
            text.AppendLine("| Stage | 名前 | ふつう 勝率 | ふつう 平均秒 | ふつう 自城HP | うまい 勝率 | うまい 平均秒 | うまい 自城HP | ★3目標 | ★3目安 |");
            text.AppendLine("| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |");

            int total = stages.Count * Skills.Length;
            for (int i = 0; i < stages.Count; i++)
            {
                StageDefinition stage = stages[i];
                List<int> deck = SimDeckPicker.Pick(stage);
                var summaries = new Summary[Skills.Length];
                for (int s = 0; s < Skills.Length; s++)
                {
                    onProgress?.Invoke($"{stage.Id} {Skills[s].Name}", (float)(i * Skills.Length + s) / total);
                    summaries[s] = RunMany(simulator, stage, deck, Skills[s], runsPerSkill, maxSeconds);
                }
                text.AppendLine(Row(stage, summaries));
            }
            return text.ToString();
        }

        private static Summary RunMany(StageSimulator simulator, StageDefinition stage, List<int> deck, BotSkill skill, int runs, float maxSeconds)
        {
            var summary = new Summary { Runs = runs };
            for (int seed = 1; seed <= runs; seed++)
            {
                StageResult result = simulator.Run(stage, deck, skill, seed, maxSeconds);
                if (result.Cleared)
                {
                    summary.Wins++;
                    summary.TotalWinSeconds += result.ElapsedSeconds;
                    summary.TotalWinHpRatio += result.PlayerCastleHpRatio;
                }
                // 自城が残ったまま終わらなかった = 時間切れ（攻めきれない）。負けとは直し方が違うので分けて数える
                else if (result.PlayerCastleHpRatio > 0f) summary.TimeOuts++;
            }
            return summary;
        }

        private static string Row(StageDefinition stage, Summary[] summaries)
        {
            var cells = new List<string> { stage.Id, stage.Name };
            foreach (Summary summary in summaries)
            {
                cells.Add(summary.WinRateText());
                cells.Add(summary.Wins > 0 ? $"{summary.TotalWinSeconds / summary.Wins:0}" : "-");
                cells.Add(summary.Wins > 0 ? $"{summary.TotalWinHpRatio / summary.Wins:0%}" : "-");
            }
            Summary skilled = summaries[summaries.Length - 1];
            cells.Add($"{stage.TargetSeconds:0}");
            cells.Add(skilled.Wins > 0 ? $"{skilled.TotalWinSeconds / skilled.Wins * TargetSecondsHint:0}" : "-");
            return "| " + string.Join(" | ", cells) + " |";
        }

        /// <summary>1ステージ × 1段階の集計。秒と自城HPは勝った回だけで平均する（負けの秒数は難しさの目安にならないため）</summary>
        private class Summary
        {
            public int Runs;
            public int Wins;
            public int TimeOuts;
            public float TotalWinSeconds;
            public float TotalWinHpRatio;

            public string WinRateText()
            {
                string rate = $"{Wins}/{Runs}";
                return TimeOuts > 0 ? $"{rate}（時間切れ{TimeOuts}）" : rate;
            }
        }
    }
}
