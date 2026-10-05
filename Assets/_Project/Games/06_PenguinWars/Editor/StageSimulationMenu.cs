using System.Diagnostics;
using System.IO;
using MiniGame.PenguinWars.Battle;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace MiniGame.PenguinWars.Editor
{
    /// <summary>
    /// 全ステージを bot で自動プレイし、勝率・秒数・自城HP を表にする（ステージ計画 Phase 5）。
    /// Phase 6 で数値を変えるたびに手で遊ばず、難しさの並びを確かめるため
    /// </summary>
    public static class StageSimulationMenu
    {
        private const string BalancePath = "Assets/_Project/Games/06_PenguinWars/Data/PenguinWarsBalance.asset";
        // Temp はプロジェクト直下の Git に入らないフォルダ。表は調整中に見返すだけなので残さない
        private const string OutputPath = "Temp/PenguinStageSim.md";
        private const int RunsPerSkill = 5;
        // これを過ぎても決着しなければ時間切れ（攻めきれない）として数える。実際のステージより十分長くする
        private const float MaxSeconds = 600f;
        private const string LogPrefix = "[StageSimulation]";

        [MenuItem("Tools/MiniGame/PenguinWars/Simulate Stages")]
        public static void SimulateStages()
        {
            var balance = AssetDatabase.LoadAssetAtPath<PenguinWarsBalance>(BalancePath);
            if (balance == null)
            {
                Debug.LogWarning($"{LogPrefix} {BalancePath} がありません。Tools > MiniGame > Rebuild PenguinWars を実行してください");
                return;
            }

            var simulator = new StageSimulator(() => balance.CreateBattleSettings(false, 0), StageSimulator.DefinitionStats());
            Stopwatch watch = Stopwatch.StartNew();
            string table;
            try
            {
                table = StageSimReport.Build(simulator, StageDefinitions.All, RunsPerSkill, MaxSeconds,
                    (label, progress) => EditorUtility.DisplayProgressBar("Simulate Stages", label, progress));
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            string report = $"{table}\n各 {RunsPerSkill} 回（シード 1〜{RunsPerSkill}）。秒・自城HP は勝った回の平均。{watch.Elapsed.TotalSeconds:0.0} 秒\n";
            File.WriteAllText(OutputPath, report);
            Debug.Log($"{LogPrefix} {OutputPath} に書き出しました\n{report}");
        }
    }
}
