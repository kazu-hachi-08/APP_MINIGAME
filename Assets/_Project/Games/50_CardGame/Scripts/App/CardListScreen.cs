#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using CardGame.Core.Definitions;
using CardGame.Unity.Battle;
using CardGame.Unity.UI;
using UnityEngine;
using UnityEngine.UI;

namespace CardGame.Unity.App
{
    /// <summary>
    /// カード一覧(図鑑)。04-screens.md「カード一覧」。
    /// 上 2 段の絞り込みバー + 1 行 6 枚の縦スクロールグリッド。タップで拡大(バトル画面と同じポップアップ)。
    /// 絞り込み・グリッド・拡大表示はデッキ編集でも流用する前提で、状態は全てこのクラスのフィールドに持つ。
    /// </summary>
    public sealed class CardListScreen : MonoBehaviour
    {
        // ---- 絞り込みの状態 ----
        private CardClass? _class;      // null = 全部
        private CardType? _type;        // null = 全
        private int? _cost;             // null = 全、7 = 7 以上
        private bool _sortByName;
        private bool _debug;            // ID と内訳を表示

        // ---- UI ----
        private const int Columns = 6;
        private const float CellW = 252, CellH = 360, GapX = 40, GapY = 28;   // 拡大サイズ(504×720)の 1/2
        private const float CardScale = 0.5f;
        private const float BarH = 150;                                       // 上の絞り込みバー
        private const float DebugLabelH = 28;

        private RectTransform _content = null!;
        private ScrollRect _scroll = null!;
        private Text _countText = null!;
        private RectTransform _overlayLayer = null!;
        private GameObject? _overlay;
        private readonly List<(Image img, Func<bool> selected)> _toggles = new();

        private static readonly Color BarColor = new Color(0.05f, 0.04f, 0.06f, 0.80f);
        private static readonly Color ToggleOff = new Color(0.20f, 0.22f, 0.27f, 0.95f);

        public IReadOnlyList<CardDefinition> Visible { get; private set; } = Array.Empty<CardDefinition>();

        private void Start()
        {
            BuildBar();
            BuildGrid();
            _overlayLayer = Ui.Fill(transform, "OverlayLayer");
            Refresh();
        }

        // ------------------------------------------------------------------
        // 絞り込みバー
        // ------------------------------------------------------------------

        private void BuildBar()
        {
            var bar = Ui.Panel(transform, "Bar", 0, Ui.RefHeight - BarH, Ui.RefWidth, BarH, BarColor);
            bar.raycastTarget = true; // 下のグリッドにタップを通さない

            // 1 段目: 戻る / クラス / ID トグル
            float y1 = Ui.RefHeight - 80, h1 = 60;
            Ui.Button(transform, "Back", 20, y1, 170, h1, "← 戻る", () => AppRoot.Instance.ShowMainMenu(), Ui.PanelDarkColor, 28);
            Ui.Label(transform, "ClassLabel", 220, y1, 110, h1, "クラス", 26, TextAnchor.MiddleLeft, Ui.TextDim, FontStyle.Bold);
            float x = 330;
            Toggle("ClassAll", x, y1, 110, h1, "全部", () => _class == null, () => _class = null); x += 118;
            foreach (var cls in new[] { CardClass.Neutral, CardClass.Knight, CardClass.Mage, CardClass.Necromancer, CardClass.Druid, CardClass.Dragon })
            {
                var c = cls;
                Toggle("Class" + c, x, y1, 186, h1, CardView.ClassLabel(c), () => _class == c, () => _class = c);
                var em = CardArt.Emblem(c);
                if (em != null)
                {
                    var icon = Ui.Rect(transform, "Emblem" + c, x + 6, y1 + 8, h1 - 16, h1 - 16).gameObject.AddComponent<Image>();
                    icon.sprite = em; icon.raycastTarget = false;
                    // 紋章に文字が隠れないよう、ラベルを紋章の右側の範囲に収める(「死霊術師」の「死」が隠れていた)
                    var label = transform.Find("Class" + c)?.GetComponentInChildren<Text>();
                    if (label != null)
                    {
                        var lrt = label.rectTransform;
                        lrt.offsetMin = new Vector2(h1 - 8, lrt.offsetMin.y);
                        lrt.offsetMax = new Vector2(-4, lrt.offsetMax.y);
                        // 折り返さずに 1 行で、入らなければ字を小さくする(best fit だと 2 行に折り返してしまう)
                        label.resizeTextForBestFit = false; label.fontSize = 24;
                        label.horizontalOverflow = HorizontalWrapMode.Overflow;
                        float room = 186 - (h1 - 8) - 4 - 6;
                        if (label.preferredWidth > room) label.fontSize = Mathf.Max(14, Mathf.FloorToInt(24 * room / label.preferredWidth));
                    }
                }
                x += 192;   // 「ネクロマンサー」「ドラゴンナイト」が 1 行に収まる幅
            }
            Toggle("Debug", Ui.RefWidth - 110, y1, 90, h1, "ID", () => _debug, () => _debug = !_debug);

            // 2 段目: 種類 / コスト / 並び / 件数
            float y2 = Ui.RefHeight - BarH + 14, h2 = 52;
            Ui.Label(transform, "TypeLabel", 220, y2, 110, h2, "種類", 26, TextAnchor.MiddleLeft, Ui.TextDim, FontStyle.Bold);
            x = 330;
            Toggle("TypeAll", x, y2, 90, h2, "全", () => _type == null, () => _type = null); x += 98;
            foreach (var (t, label) in new[] { (CardType.Follower, "フォロワー"), (CardType.Spell, "スペル"), (CardType.Amulet, "アミュレット") })
            {
                var tt = t;
                float w = label.Length > 4 ? 160 : 120;
                Toggle("Type" + tt, x, y2, w, h2, label, () => _type == tt, () => _type = tt);
                x += w + 8;
            }
            x += 30;
            Ui.Label(transform, "CostLabel", x, y2, 90, h2, "コスト", 26, TextAnchor.MiddleLeft, Ui.TextDim, FontStyle.Bold); x += 96;
            Toggle("CostAll", x, y2, 70, h2, "全", () => _cost == null, () => _cost = null); x += 78;
            for (int c = 1; c <= 7; c++)
            {
                int cc = c;
                Toggle("Cost" + cc, x, y2, 62, h2, cc == 7 ? "7+" : cc.ToString(), () => _cost == cc, () => _cost = cc);
                x += 70;
            }
            x += 30;
            Ui.Label(transform, "SortLabel", x, y2, 80, h2, "並び", 26, TextAnchor.MiddleLeft, Ui.TextDim, FontStyle.Bold); x += 84;
            Toggle("SortCost", x, y2, 110, h2, "コスト", () => !_sortByName, () => _sortByName = false); x += 118;
            Toggle("SortName", x, y2, 110, h2, "名前", () => _sortByName, () => _sortByName = true);
            _countText = Ui.Label(transform, "Count", 1600, y1, 190, h1, "", 24, TextAnchor.MiddleRight, Ui.TextDim);
            _countText.resizeTextForBestFit = true; _countText.resizeTextMinSize = 12; _countText.resizeTextMaxSize = 24;
        }

        /// <summary>選択状態を色で示すボタン。押すと apply → 一覧を作り直す。</summary>
        private void Toggle(string name, float x, float y, float w, float h, string label, Func<bool> selected, Action apply)
        {
            var btn = Ui.Button(transform, name, x, y, w, h, label, () => { apply(); Refresh(); }, ToggleOff, 24);
            var img = btn.GetComponent<Image>();
            _toggles.Add((img, selected));
        }

        private void RefreshToggles()
        {
            foreach (var (img, selected) in _toggles)
            {
                bool on = selected();
                img.color = on ? Ui.Accent : ToggleOff;
                img.GetComponentInChildren<Text>().color = on ? Color.black : Ui.TextMain;
            }
        }

        // ------------------------------------------------------------------
        // グリッド(縦スクロール)
        // ------------------------------------------------------------------

        private void BuildGrid()
        {
            float viewH = Ui.RefHeight - BarH;
            var viewport = Ui.Rect(transform, "Viewport", 0, 0, Ui.RefWidth, viewH);
            viewport.gameObject.AddComponent<RectMask2D>();
            var catcher = viewport.gameObject.AddComponent<Image>();   // 空き領域でもドラッグ(スクロール)を受ける
            catcher.color = Color.clear;

            var content = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
            content.SetParent(viewport, false);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(0, viewH);
            _content = content;

            _scroll = viewport.gameObject.AddComponent<ScrollRect>();
            _scroll.content = content;
            _scroll.viewport = viewport;
            _scroll.horizontal = false;
            _scroll.vertical = true;
            _scroll.movementType = ScrollRect.MovementType.Clamped;
            _scroll.scrollSensitivity = 150;  // PC のホイール(1 ノッチで約半行)
            _scroll.inertia = true;
            _scroll.decelerationRate = 0.3f;  // フリック後の滑りを長めに(値が大きいほど減速が緩い)
        }

        /// <summary>絞り込みを適用して一覧を作り直す。</summary>
        public void Refresh()
        {
            RefreshToggles();
            var db = AppRoot.Instance.Db;
            IEnumerable<CardDefinition> q = db.All.Where(c => !c.IsToken);
            if (_class.HasValue) q = q.Where(c => c.Class == _class.Value);
            if (_type.HasValue) q = q.Where(c => c.Type == _type.Value);
            if (_cost.HasValue) q = q.Where(c => _cost.Value == 7 ? c.Cost >= 7 : c.Cost == _cost.Value);
            var list = _sortByName
                ? q.OrderBy(c => c.Name, StringComparer.Ordinal).ThenBy(c => c.Id, StringComparer.Ordinal).ToList()
                : q.OrderBy(c => c.Class).ThenBy(c => c.Cost).ThenBy(c => c.Id, StringComparer.Ordinal).ToList();
            Visible = list;

            // 件数(開発表示なら内訳も)
            string count = $"{list.Count} 件";
            if (_debug && list.Count > 0)
            {
                int f = list.Count(c => c.IsFollower), s = list.Count(c => c.IsSpell), a = list.Count(c => c.IsAmulet);
                count = $"F{f} / S{s} / A{a}  平均コスト {list.Average(c => c.Cost):0.0}  |  {count}";
            }
            _countText.text = count;

            // グリッドを作り直す
            Ui.Clear(_content);
            float rowH = CellH + GapY + (_debug ? DebugLabelH : 0);
            int rows = (list.Count + Columns - 1) / Columns;
            float totalH = Mathf.Max(Ui.RefHeight - BarH, GapY + rows * rowH);
            _content.sizeDelta = new Vector2(0, totalH);
            float startX = (Ui.RefWidth - (Columns * CellW + (Columns - 1) * GapX)) / 2;
            for (int i = 0; i < list.Count; i++)
            {
                var def = list[i];
                int col = i % Columns, row = i / Columns;
                float x = startX + col * (CellW + GapX);
                float y = totalH - GapY - (row + 1) * rowH + GapY + (_debug ? DebugLabelH : 0);
                var cell = Ui.Panel(_content, "Cell " + def.Id, x, y, CellW, CellH, Color.clear);
                cell.gameObject.AddComponent<Button>().onClick.AddListener(() => ShowDetail(def));

                var view = CardView.Create(cell.transform, def, CardViewMode.Detail);
                view.Rect.localScale = Vector3.one * CardScale;
                view.Rect.anchoredPosition = new Vector2(CellW / 2, CellH / 2);
                view.Draggable = false;
                view.Group.blocksRaycasts = false;   // タップはセル、ドラッグは ScrollRect が受ける

                if (_debug)
                    Ui.Label(_content, "Id " + def.Id, x, y - DebugLabelH, CellW, DebugLabelH, def.Id, 20, TextAnchor.MiddleCenter, Ui.TextDim);
            }
            _scroll.verticalNormalizedPosition = 1f;
        }

        // ------------------------------------------------------------------
        // 拡大表示
        // ------------------------------------------------------------------

        private void ShowDetail(CardDefinition def)
        {
            CloseOverlay();
            var ov = Ui.Overlay(_overlayLayer, "Overlay", 0.6f);
            _overlay = ov.gameObject;
            ov.gameObject.AddComponent<Button>().onClick.AddListener(CloseOverlay);
            CardDetailPopup.Build(ov.rectTransform, def, null, CloseOverlay);
        }

        private void CloseOverlay()
        {
            if (_overlay != null) Destroy(_overlay);
            _overlay = null;
        }

        // 開発用(DevAutoplay から)
        internal void SetClass(CardClass? cls) { _class = cls; Refresh(); }
        internal void SetDebug(bool on) { _debug = on; Refresh(); }
        internal void Scroll(float normalized) => _scroll.verticalNormalizedPosition = normalized;
    }
}
