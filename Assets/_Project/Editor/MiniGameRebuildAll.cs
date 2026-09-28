using MiniGame.Golf.Editor;
using MiniGame.Molkky.Editor;
using MiniGame.Soccer.Editor;
using MiniGame.TableTennis.Editor;
using UnityEditor;

namespace MiniGame.Editor
{
    /// <summary>
    /// 全シーンと生成素材をまとめて作り直すメニュー。
    /// Common は個別ゲームに依存させない決まりなので、ゲーム横断のこのフォルダに置いている。
    /// </summary>
    public static class MiniGameRebuildAll
    {
        // 個別の Rebuild（1〜5）と区切り線で分けるため、優先度を11以上離す
        private const int MenuPriority = 100;

        [MenuItem("Tools/MiniGame/Rebuild All", false, MenuPriority)]
        public static void RebuildAll()
        {
            // 全シーンに大きな差分が出て、相方の作業とコンフリクトしやすいため一度確認する
            bool confirmed = EditorUtility.DisplayDialog("Rebuild All",
                "全ゲームのシーンと素材を作り直します。\n全シーンに大きな差分が出るので、相方と作業が被っていないか確認してください。",
                "実行", "キャンセル");
            if (!confirmed) return;

            TitleSceneBuilder.RebuildTitle();
            SoccerSceneBuilder.RebuildSoccer();
            TableTennisSceneBuilder.RebuildTableTennis();
            MolkkySceneBuilder.RebuildMolkky();
            GolfSceneBuilder.RebuildGolf();
        }
    }
}
