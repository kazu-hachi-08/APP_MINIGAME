using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityScene = UnityEngine.SceneManagement.Scene;

namespace MiniGame.Common.UI
{
    /// <summary>
    /// ブラウザ版だけ、Unity標準フォントの Text を日本語フォントに差し替える。
    /// 標準フォント(LegacyRuntime)は日本語を持たず、スマホ版はOSのフォントで補われるが、
    /// ブラウザ版はOSのフォントを使えないため日本語が表示されない。
    /// 各シーンのTextを書き換えるとSceneBuilderで作り直したときに戻るので、実行時に差し替える。
    /// </summary>
    public static class WebFontApplier
    {
        private const string FontPath = "Fonts/NotoSansJP-Regular";
        private const string BuiltinFontName = "LegacyRuntime";

        private static Font _font;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            if (Application.platform != RuntimePlatform.WebGLPlayer)
            {
                return;
            }

            _font = Resources.Load<Font>(FontPath);
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;

            Apply();
        }

        private static void OnSceneLoaded(UnityScene scene, LoadSceneMode mode)
        {
            Apply();
        }

        private static void Apply()
        {
            if (_font == null)
            {
                return;
            }

            // 閉じているダイアログも後で開くので、非表示のものも含める
            var texts = Object.FindObjectsByType<Text>(FindObjectsInactive.Include);
            foreach (var text in texts)
            {
                if (text.font != null && text.font.name == BuiltinFontName)
                {
                    text.font = _font;
                }
            }
        }
    }
}
