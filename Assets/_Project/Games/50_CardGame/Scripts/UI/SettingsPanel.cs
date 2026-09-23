#nullable enable
using UnityEngine;
using UnityEngine.UI;

namespace CardGame.Unity.UI
{
    /// <summary>
    /// 設定の中身(音量スライダーなど)。04-screens.md「設定」。
    /// メインメニューの設定画面と、対戦中のメニューの両方から同じものを使う。
    /// </summary>
    public static class SettingsPanel
    {
        /// <summary>羊皮紙のパネルを作り、その中に設定項目を並べる。w × h は外枠の大きさ。</summary>
        public static Image Build(Transform parent, float x, float y, float w, float h, bool showTitle = true)
        {
            var panel = FantasyUi.ParchmentPanel(parent, "Settings", x, y, w, h);
            var t = panel.transform;
            FantasyUi.GoldLine(t, "LineT", 16, h - 18, w - 32, 3);
            FantasyUi.GoldLine(t, "LineB", 16, 15, w - 32, 3);
            if (showTitle) Ui.Label(t, "Title", 0, h - 80, w, 50, "設定", 38, TextAnchor.MiddleCenter, FantasyUi.Ink, FontStyle.Bold);

            float rowY = h - (showTitle ? 160 : 100);
            Volume(t, "Bgm", 60, rowY, w - 120, "BGM の音量", Audio.BgmVolume, v => Audio.BgmVolume = v);
            rowY -= 120;
            Volume(t, "Se", 60, rowY, w - 120, "効果音の音量", Audio.SeVolume, v =>
            {
                Audio.SeVolume = v;
                Audio.Play(Audio.Button, 0.8f, 0f);   // 動かすたびに今の音量で鳴らす
            });
            rowY -= 130;

            // 全画面(ブラウザ・PC 用)
            var fsBtn = FantasyUi.ParchmentButton(t, "Fullscreen", 60, rowY, (w - 160) / 2, 76,
                Screen.fullScreen ? "全画面を解除" : "全画面にする", () => { Screen.fullScreen = !Screen.fullScreen; }, 24);
            var fsLabel = fsBtn.GetComponentInChildren<Text>();
            fsBtn.onClick.AddListener(() => fsLabel.text = Screen.fullScreen ? "全画面を解除" : "全画面にする");

            FantasyUi.ParchmentButton(t, "TestSound", 60 + (w - 160) / 2 + 40, rowY, (w - 160) / 2, 76,
                "音を試す", () => Audio.Play(Audio.Awakening, 1f, 0f), 24);

            return panel;
        }

        /// <summary>ラベル + スライダー + 数値(%)の 1 行。</summary>
        private static void Volume(Transform parent, string name, float x, float y, float w, string label, float value, System.Action<float> onChanged)
        {
            Ui.Label(parent, name + "Label", x, y + 52, w, 40, label, 26, TextAnchor.MiddleLeft, FantasyUi.Ink, FontStyle.Bold);
            var valueText = Ui.Label(parent, name + "Value", x, y + 52, w, 40, $"{Mathf.RoundToInt(value * 100)}%", 26, TextAnchor.MiddleRight, FantasyUi.InkDim);
            Ui.Slider(parent, name + "Slider", x, y, w, 44, value, v =>
            {
                valueText.text = $"{Mathf.RoundToInt(v * 100)}%";
                onChanged(v);
            });
        }
    }
}
