#nullable enable
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace CardGame.Unity.UI
{
    /// <summary>
    /// フィールド案 A(酒場のテーブル)の世界観に合わせた部品。羊皮紙のパネル・ボタン、金文字のタイトル。
    /// バトル画面・メインメニュー・デッキ選択で共用する。
    /// </summary>
    public static class FantasyUi
    {
        public static readonly Color Ink = new Color(0.24f, 0.15f, 0.07f);          // 羊皮紙上の文字
        public static readonly Color InkDim = new Color(0.42f, 0.32f, 0.20f);
        public static readonly Color InkRed = new Color(0.60f, 0.12f, 0.10f);
        public static readonly Color FrameColor = new Color(0.32f, 0.20f, 0.09f);    // 羊皮紙パネルの縁
        public static readonly Color Gold = new Color(0.90f, 0.76f, 0.40f);          // 金文字
        public static readonly Color GoldTrim = new Color(0.80f, 0.66f, 0.30f);      // 金の縁・刺繍
        public static readonly Color MatColor = new Color(0.36f, 0.12f, 0.11f);      // 革マット(深い赤)

        /// <summary>羊皮紙のパネル(縁 = インク色、下に柔らかい影)。戻り値は外枠の Image(縁の色替えに使う)。</summary>
        public static Image ParchmentPanel(Transform parent, string name, float x, float y, float w, float h)
        {
            var shadow = Ui.Panel(parent, name + "Shadow", x - 10, y - 12, w + 20, h + 20, Color.white);
            shadow.sprite = Materials.SoftShadow();
            shadow.type = Image.Type.Simple;
            shadow.raycastTarget = false;
            var outer = Ui.Panel(parent, name, x, y, w, h, FrameColor);
            var inner = UiTex.Fill(outer.transform, "Inner", Materials.Parchment(), Color.white, tiled: true, tilePx: 256, margin: 4);
            inner.raycastTarget = false;
            return outer;
        }

        /// <summary>羊皮紙のボタン(インク色の文字)。</summary>
        public static Button ParchmentButton(Transform parent, string name, float x, float y, float w, float h, string label, UnityAction onClick, int fontSize = 30, bool emphasized = false)
        {
            var outer = ParchmentPanel(parent, name, x, y, w, h);
            var btn = outer.gameObject.AddComponent<Button>();
            btn.targetGraphic = outer.transform.Find("Inner")!.GetComponent<Image>();
            var colors = btn.colors;
            colors.highlightedColor = new Color(1.05f, 1.02f, 0.95f);
            colors.pressedColor = new Color(0.85f, 0.80f, 0.70f);
            colors.disabledColor = new Color(0.6f, 0.6f, 0.6f, 0.8f);
            btn.colors = colors;
            btn.onClick.AddListener(() => Audio.Play(Audio.Button, 0.6f));
            btn.onClick.AddListener(onClick);
            var text = Ui.FillLabel(outer.transform, "Label", label, fontSize, TextAnchor.MiddleCenter, emphasized ? InkRed : Ink, FontStyle.Bold, 6);
            text.raycastTarget = false;
            if (emphasized)
            {
                // 強調ボタンは金の細い縁を足す
                foreach (var (n, lx, ly, lw, lh) in new[] { ("T", 8f, h - 10f, w - 16f, 2f), ("B", 8f, 8f, w - 16f, 2f), ("L", 8f, 8f, 2f, h - 16f), ("R", w - 10f, 8f, 2f, h - 16f) })
                    Ui.Panel(outer.transform, "Trim" + n, lx, ly, lw, lh, GoldTrim).raycastTarget = false;
            }
            return btn;
        }

        /// <summary>金文字のタイトル(暗い縁取り + 落ち影)。</summary>
        public static Text GoldTitle(Transform parent, string name, float x, float y, float w, float h, string text, int fontSize)
        {
            var label = Ui.Label(parent, name, x, y, w, h, text, fontSize, TextAnchor.MiddleCenter, Gold, FontStyle.Bold);
            var outline = label.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.12f, 0.06f, 0.02f, 0.95f);
            outline.effectDistance = new Vector2(fontSize * 0.03f, -fontSize * 0.03f);
            var shadow = label.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.7f);
            shadow.effectDistance = new Vector2(0, -fontSize * 0.08f);
            return label;
        }

        /// <summary>金の細線(飾り罫)。</summary>
        public static Image GoldLine(Transform parent, string name, float x, float y, float w, float h = 2)
        {
            var img = Ui.Panel(parent, name, x, y, w, h, GoldTrim);
            img.raycastTarget = false;
            return img;
        }
    }
}
