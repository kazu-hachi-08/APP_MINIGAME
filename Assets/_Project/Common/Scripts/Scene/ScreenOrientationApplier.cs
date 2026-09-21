using UnityEngine;
using UnityEngine.SceneManagement;
using UnityScene = UnityEngine.SceneManagement.Scene;

namespace MiniGame.Common.Scene
{
    /// <summary>
    /// シーンごとに端末の画面向きを切り替える。
    /// 向き設定用のコンポーネントを各シーンに置くとSceneファイルの競合が増えるため、
    /// 「シーン名 → 画面向き」の対応をこの1箇所に集約している。
    /// </summary>
    public static class ScreenOrientationApplier
    {
        /// <summary>
        /// 起動時に自動で購読する。TitleSceneのように SceneLoader を経由せずに
        /// 読み込まれる最初のシーンにも向きを適用するため、AfterSceneLoad で初期化する。
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;

            Apply(SceneManager.GetActiveScene().name);
        }

        private static void OnSceneLoaded(UnityScene scene, LoadSceneMode mode)
        {
            // 加算ロードは画面全体の切り替えではないので無視する
            if (mode == LoadSceneMode.Additive)
            {
                return;
            }

            Apply(scene.name);
        }

        private static void Apply(string sceneName)
        {
            switch (sceneName)
            {
                // サッカーは横長フィールド＋横画面前提の仮想コントロール配置
                case SceneNames.Soccer:
                    SetLandscape();
                    break;

                // タイトルと卓球（奥行き方向にコートを見る画）は縦画面
                case SceneNames.Title:
                case SceneNames.TableTennis:
                default:
                    SetPortrait();
                    break;
            }
        }

        private static void SetPortrait()
        {
            // 縦しか許可しないならAutoRotationにする意味がなく、
            // AutoRotationだと横向きで起動した端末が次のセンサー検知まで横のまま残るため固定する
            Screen.autorotateToPortrait = true;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = false;
            Screen.autorotateToLandscapeRight = false;
            Screen.orientation = ScreenOrientation.Portrait;
        }

        private static void SetLandscape()
        {
            // 左右どちらの横持ちでも遊べるように両方許可する
            Screen.autorotateToLandscapeLeft = true;
            Screen.autorotateToLandscapeRight = true;
            Screen.autorotateToPortrait = false;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.orientation = ScreenOrientation.AutoRotation;
        }
    }
}
