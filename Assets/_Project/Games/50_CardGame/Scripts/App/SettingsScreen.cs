#nullable enable
using CardGame.Unity.UI;
using UnityEngine;

namespace CardGame.Unity.App
{
    /// <summary>設定画面(04-screens.md「設定」)。メインメニューから開く。</summary>
    public sealed class SettingsScreen : MonoBehaviour
    {
        private void Start()
        {
            FantasyUi.GoldTitle(transform, "Title", 0, 900, Ui.RefWidth, 110, "設 定", 72);
            SettingsPanel.Build(transform, (Ui.RefWidth - 900) / 2, 330, 900, 460, showTitle: false);
            FantasyUi.ParchmentButton(transform, "Back", 40, 60, 280, 80, "← 戻る", () => AppRoot.Instance.ShowMainMenu(), 28);
        }
    }
}
