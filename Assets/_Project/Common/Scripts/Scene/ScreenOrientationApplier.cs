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
                // カードゲームは 1920×1080 の横画面前提でUIを組んでいる
                // ペンギン大戦争は横スクロールの1本レーン
                case SceneNames.Soccer:
                case SceneNames.CardGame:
                case SceneNames.PenguinWars:
                    SetLandscape();
                    break;

                // タイトルと卓球・モルック（奥行き方向を見る画）・ゴルフ（ティーが下、グリーンが上）・
                // 人生ゲーム（スタートが下、ゴールが上の盤面）は縦画面
                case SceneNames.Title:
                case SceneNames.TableTennis:
                case SceneNames.Molkky:
                case SceneNames.Golf:
                case SceneNames.LifeGame:
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
            WebScreen.SetPortrait();
            StandaloneScreen.SetPortrait();
        }

        private static void SetLandscape()
        {
            // 左右どちらの横持ちでも遊べるように両方許可する
            Screen.autorotateToLandscapeLeft = true;
            Screen.autorotateToLandscapeRight = true;
            Screen.autorotateToPortrait = false;
            Screen.autorotateToPortraitUpsideDown = false;

            // AutoRotationだけだと縦持ちのまま入った端末がセンサー検知まで縦に残るため、
            // まず横に固定して強制的に回転させる
            Screen.orientation = ScreenOrientation.LandscapeLeft;
            EnableLandscapeAutoRotationNextFrame();

            WebScreen.SetLandscape();
            StandaloneScreen.SetLandscape();
        }

        /// <summary>
        /// 同じフレームで orientation を上書きすると固定指定が反映されないことがあるため、
        /// 1フレーム待ってから左右どちらの横持ちにも追従させる
        /// </summary>
        private static async void EnableLandscapeAutoRotationNextFrame()
        {
            await Awaitable.NextFrameAsync();

            // 待っている間に縦画面のシーンへ切り替わっていたら、その指定を上書きしない
            if (Screen.autorotateToPortrait)
            {
                return;
            }

            Screen.orientation = ScreenOrientation.AutoRotation;
        }
    }
}
