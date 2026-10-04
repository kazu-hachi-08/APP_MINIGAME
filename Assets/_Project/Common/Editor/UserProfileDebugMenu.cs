using MiniGame.Common.Profile;
using UnityEditor;
using UnityEngine;

namespace MiniGame.Editor
{
    /// <summary>
    /// ユーザー名はゲーム内から変更できない仕様のため、初回入力の動作確認用に消すメニューを用意する
    /// </summary>
    public static class UserProfileDebugMenu
    {
        // Rebuild 系メニュー（1〜100）と区切るため離す
        private const int MenuPriority = 200;

        [MenuItem("Tools/MiniGame/ユーザー名をリセット", false, MenuPriority)]
        public static void ResetUserName()
        {
            UserProfile.ResetForDebug();
            Debug.Log("[UserProfile] ユーザー名をリセットしました。次にタイトルを開くと入力ダイアログが出ます。");
        }
    }
}
