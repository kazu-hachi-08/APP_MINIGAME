#nullable enable
using CardGame.Core.Definitions;
using CardGame.Core.State;
using CardGame.Unity.UI;
using UnityEngine;
using UnityEngine.UI;

namespace CardGame.Unity.Battle
{
    /// <summary>
    /// カードの拡大表示 + キーワード説明のポップアップ。バトル画面とカード一覧で共用する。
    /// 呼び出し側がオーバーレイ(画面全体の半透明パネル)を用意し、その上に載せる。
    /// </summary>
    public static class CardDetailPopup
    {
        /// <summary>オーバーレイ ov の上にカードと説明を配置する。タップで閉じる動作は ov 側の Button に任せる。</summary>
        public static CardView Build(RectTransform ov, CardDefinition def, BoardEntity? entity, UnityEngine.Events.UnityAction close)
        {
            var help = KeywordHelp.ForCard(def);
            float cardX = help.Count > 0 ? Ui.RefWidth / 2 - 200 : Ui.RefWidth / 2;
            var view = CardView.Create(ov, def, CardViewMode.Detail, entity);
            view.Rect.anchoredPosition = new Vector2(cardX, Ui.RefHeight / 2);
            view.Draggable = false;
            view.gameObject.AddComponent<Button>().onClick.AddListener(close);

            if (help.Count > 0)
            {
                const float w = 480;
                float h = 40 + 110 * help.Count;
                var panel = Ui.Panel(ov, "Help", cardX + CardView.DetailSize.x / 2 + 30, Ui.RefHeight / 2 - h / 2, w, h, new Color(0.05f, 0.05f, 0.07f, 0.95f));
                panel.raycastTarget = false;
                Ui.FillLabel(panel.transform, "Text", KeywordHelp.ToRichText(def), 26, TextAnchor.UpperLeft, Ui.TextMain, FontStyle.Normal, 20);
            }
            Ui.Label(ov, "Hint", 0, 40, Ui.RefWidth, 40, "タップで閉じる", 24, TextAnchor.MiddleCenter, Ui.TextDim);
            return view;
        }
    }
}
