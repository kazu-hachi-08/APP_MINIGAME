#nullable enable
using CardGame.Unity.Battle;
using CardGame.Unity.UI;
using UnityEngine;
using UnityEngine.UI;

namespace CardGame.Unity.App
{
    /// <summary>
    /// タイトル / メインメニュー(04-screens.md)。モードを選ぶだけで、デッキは次のデッキ選択画面で選ぶ。
    /// 机の背景の上に金文字のタイトルと羊皮紙のボタン。
    /// </summary>
    public sealed class MainMenuScreen : MonoBehaviour
    {
        private void Start()
        {
            var app = AppRoot.Instance;

            // 中央の紋章(うっすら)とタイトル
            var emblem = Ui.Rect(transform, "Emblem", Ui.RefWidth / 2 - 260, 560, 520, 520).gameObject.AddComponent<Image>();
            emblem.sprite = Icons.Glyph(Icons.Kind.Shield); emblem.color = new Color(1f, 0.9f, 0.6f, 0.06f); emblem.raycastTarget = false;
            FantasyUi.GoldTitle(transform, "Title", 0, 840, Ui.RefWidth, 180, "THE CHAOS Ⅱ", 128);
            FantasyUi.GoldLine(transform, "TitleLineL", Ui.RefWidth / 2 - 560, 838, 360);
            FantasyUi.GoldLine(transform, "TitleLineR", Ui.RefWidth / 2 + 200, 838, 360);
            Ui.Label(transform, "Sub", 0, 790, Ui.RefWidth, 40, $"開発ビルド v{Application.version}", 22, TextAnchor.MiddleCenter, new Color(0.85f, 0.78f, 0.62f));

            // モード(羊皮紙のボタンを縦に)
            const float w = 620, h = 88, gap = 20, x = (Ui.RefWidth - w) / 2;
            float y = 660;
            FantasyUi.ParchmentButton(transform, "VsAi", x, y, w, h, "AI 対戦", () => app.ShowDeckSelect(BattleMode.VersusAi), 34, emphasized: true); y -= h + gap;
            FantasyUi.ParchmentButton(transform, "Local", x, y, w, h, "ローカル対戦", () => app.ShowDeckSelect(BattleMode.LocalTwoPlayer), 28); y -= h + gap;
            FantasyUi.ParchmentButton(transform, "Online", x, y, w, h, "オンライン対戦", () => app.ShowDeckSelect(BattleMode.Online), 28); y -= h + gap;
            FantasyUi.ParchmentButton(transform, "AiVsAi", x, y, w, h, "AI 同士の対戦を観戦", () => app.ShowDeckSelect(BattleMode.AiVersusAi), 28); y -= h + gap;
            FantasyUi.ParchmentButton(transform, "CardList", x, y, w, h, "カード一覧", () => app.ShowCardList(), 28); y -= h + gap;
            FantasyUi.ParchmentButton(transform, "Settings", x, y, w, h, "設定", () => app.ShowSettings(), 28);
            Ui.Label(transform, "Version", 30, 30, 600, 40, $"cards={app.Db.Count}  decks={app.Decks.Count}", 18, TextAnchor.MiddleLeft, new Color(0.7f, 0.62f, 0.48f));

            // ミニゲーム集のタイトルへ戻る(左上。モードボタンの列とは別扱い)
            const float backX = 30, backY = 980, backW = 320, backH = 70;
            FantasyUi.ParchmentButton(transform, "BackToTitle", backX, backY, backW, backH, "◀ タイトルへ戻る", () => app.ReturnToTitle(), 26);
        }
    }
}
