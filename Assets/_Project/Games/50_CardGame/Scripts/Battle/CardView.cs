#nullable enable
using System.Linq;
using CardGame.Core.Definitions;
using CardGame.Core.State;
using CardGame.Unity.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CardGame.Unity.Battle
{
    public enum CardViewMode
    {
        /// <summary>手札(効果テキストあり、ドラッグでプレイ)。</summary>
        Hand,
        /// <summary>場(小さい、ドラッグで攻撃)。</summary>
        Board,
        /// <summary>拡大表示(操作なし)。</summary>
        Detail,
    }

    /// <summary>
    /// カード 1 枚の見た目。フェーズ3は色枠 + テキストのみ(04-screens.md)。
    /// ドラッグ・クリックは BattleScreen に委譲する。
    /// </summary>
    public sealed class CardView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        public CardDefinition Definition { get; private set; } = null!;
        /// <summary>場のカードのみ。</summary>
        public BoardEntity? Entity { get; private set; }
        /// <summary>手札のみ。</summary>
        public int HandIndex { get; private set; } = -1;
        /// <summary>手札のみ: CardInstance.InstanceId(再生時にプレイされたカードを逆引きする)。</summary>
        public int HandInstanceId { get; private set; } = -1;
        public CardViewMode Mode { get; private set; }

        public BattleScreen? Screen;
        public bool Draggable;

        private Image _frame = null!;
        private Image? _glow;   // ハースストーン風の強調表示の光
        private Text _attackText = null!;
        private Text _healthText = null!;
        private CanvasGroup _group = null!;
        private RectTransform _rt = null!;
        private Transform? _originalParent;
        private Vector2 _originalPos;
        private int _originalSibling;

        public RectTransform Rect => _rt;
        public CanvasGroup Group => _group;

        // カードの縦横比は実物のハースストーン(約 0.77)に合わせる
        public static readonly Vector2 HandSize = new Vector2(230, 300);
        public static readonly Vector2 BoardSize = new Vector2(190, 260);
        public static readonly Vector2 DetailSize = new Vector2(552, 720);

        /// <summary>カードビューを生成する。位置は呼び出し側が設定する。</summary>
        public static CardView Create(Transform parent, CardDefinition def, CardViewMode mode, BoardEntity? entity = null, int handIndex = -1, int handInstanceId = -1)
        {
            var size = mode switch { CardViewMode.Hand => HandSize, CardViewMode.Board => BoardSize, _ => DetailSize };
            var go = new GameObject($"Card {def.Id}", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;

            var view = go.AddComponent<CardView>();
            view._rt = rt;
            view._group = go.GetComponent<CanvasGroup>();
            view._frame = go.GetComponent<Image>();
            view.Definition = def;
            view.Entity = entity;
            view.HandIndex = handIndex;
            view.HandInstanceId = handInstanceId;
            view.Mode = mode;
            view.Build(size);
            return view;
        }

        // ------------------------------------------------------------------
        // ハースストーン風(2026-09-23 オーナー要望。部品は Resources/HsParts、Tools/ArtGen の assets.json で生成)
        // 比率は実物のハースストーンのカード(ミニオン)を測った値。x・y はカードの矩形に対する比(y は下から)
        // ------------------------------------------------------------------

        /// <summary>ハースストーン風の比率(実測)。</summary>
        private static class Hs
        {
            // 台紙(カードの本体)。周りの余白は、上にはみ出す楕円と角の宝石のため
            public const float BodyX = 0.148f, BodyW = 0.738f, BodyY = 0.059f, BodyH = 0.805f;
            // ミニオンの楕円の絵の窓
            // 実物は額が細いので窓 45.5% × 40.7%。こちらの額は太めなので、額の外周が実物と同じ大きさ(横 50% × 縦 45%)になるよう窓を小さくする
            // 絵・リボン・文章欄は台紙の中心線(BodyX + BodyW / 2 = 0.517)にそろえる(ずれるとリボンの飾りが額の真下に来ない。2026-09-23)
            public const float CenterX = BodyX + BodyW / 2;
            public const float OvalX = CenterX - 0.200f, OvalW = 0.400f, OvalY = 0.5155f, OvalH = 0.360f;
            // スペル・アミュレットの角丸の絵の窓
            public const float RectX = CenterX - 0.270f, RectW = 0.540f, RectY = 0.530f, RectH = 0.300f;
            // 名前のリボン
            public const float RibbonX = CenterX - 0.350f, RibbonW = 0.700f, RibbonY = 0.385f, RibbonH = 0.130f;
            // 文章欄
            public const float TextX = CenterX - 0.3125f, TextW = 0.625f, TextY = 0.095f, TextH = 0.285f;
            // 宝石(中心と直径。直径はカードの横幅に対する比)
            public const float CostCx = 0.207f, CostCy = 0.832f, CostD = 0.215f;
            public const float AtkCx = 0.213f, AtkCy = 0.103f, HpCx = 0.830f, HpCy = 0.103f, StatD = 0.200f;
        }

        /// <summary>
        /// ハースストーン風の組み立て。手札・拡大: クラス別の台紙 + 楕円(スペルは角丸)の絵 + 名前のリボン + 文章欄 + 宝石。
        /// 場: 楕円の肖像 + 攻撃・体力の宝石だけ(ハースストーンの場のミニオン)。
        /// </summary>
        private void Build(Vector2 size)
        {
            float s = size.x / HandSize.x;
            var def = Definition;
            var art = CardArt.Get(def.Id);
            float w = size.x, h = size.y;
            _frame.color = Color.clear;
            // 強調表示の光(SetFrame で色を付ける)。手札は台紙の外周、場は楕円の額の外周に沿わせる
            if (Mode == CardViewMode.Board)
            {
                float gw = w * 0.74f / OvalHoleW, gh = h * 0.76f / OvalHoleH;   // 額の外周(下の ArtWindow と同じ計算)
                float gcx = w / 2, gcy = h * 0.14f + h * 0.76f / 2;
                _glow = GlowImage(transform, true, gcx, gcy, gw / 0.84f, gh / 0.84f);
            }
            else
            {
                float gw = w * (Hs.BodyW + 0.03f), gh = h * (Hs.BodyH + 0.02f);
                _glow = GlowImage(transform, false, w * (Hs.BodyX + Hs.BodyW / 2), h * (Hs.BodyY + Hs.BodyH / 2), gw / 0.84f, gh / 0.84f);
            }
            var inner = Ui.FillPanel(transform, "Inner", Color.clear, 0);
            inner.raycastTarget = false;
            var t = inner.transform;
            int statFont = Mathf.RoundToInt(22 * s);

            if (Mode == CardViewMode.Board)
            {
                // 場: 枠の色(選択・攻撃可能の強調)は額の外側に楕円の光として見せる
                _frame.sprite = Icons.Ellipse();
                float ow = w * 0.74f, oh = h * 0.76f;
                ArtWindow(t, art, def, (w - ow) / 2, h * 0.14f, ow, oh, s, oval: true);
                float iy = h * 0.74f;
                foreach (var kw in new[] { Keyword.Ward, Keyword.Storm, Keyword.Rush, Keyword.Bane, Keyword.Drain })
                {
                    if (!def.HasKeyword(kw)) continue;
                    float sz = 30 * s;
                    var icon = Ui.Rect(t, "Kw" + kw, w * 0.86f - sz / 2, iy, sz, sz).gameObject.AddComponent<Image>();
                    var sprite = CardArt.Keyword(kw);
                    if (sprite != null) icon.sprite = sprite;
                    else { icon.sprite = Icons.Glyph(Icons.Kind.Shield); icon.color = new Color(0.80f, 0.85f, 1f); }
                    icon.raycastTarget = false;
                    iy -= sz + 3 * s;
                }
                float bd = w * 0.30f;
                if (def.IsFollower)
                {
                    _attackText = HsGem(t, "Atk", w * 0.20f - bd / 2, h * 0.15f - bd / 2, bd, "gem_attack", statFont);
                    _healthText = HsGem(t, "Hp", w * 0.80f - bd / 2, h * 0.15f - bd / 2, bd, "gem_health", statFont);
                }
                else if (def.IsAmulet)
                {
                    _healthText = HsGem(t, "Cd", w * 0.80f - bd / 2, h * 0.15f - bd / 2, bd, "gem_cost", statFont);
                    _attackText = _healthText;
                }
                else _attackText = _healthText = Ui.Label(t, "None", 0, 0, 1, 1, "", 1);
                RefreshStats();
                return;
            }

            // 台紙
            var body = Ui.Panel(t, "Body", w * Hs.BodyX, h * Hs.BodyY, w * Hs.BodyW, h * Hs.BodyH, Color.white);
            body.sprite = CardArt.HsBody(def.Class); body.type = Image.Type.Simple; body.raycastTarget = false;

            // 絵: ミニオンは楕円、スペル・アミュレットは角丸の四角
            if (def.IsFollower)
                ArtWindow(t, art, def, w * Hs.OvalX, h * Hs.OvalY, w * Hs.OvalW, h * Hs.OvalH, s, oval: true);
            else
                ArtWindow(t, art, def, w * Hs.RectX, h * Hs.RectY, w * Hs.RectW, h * Hs.RectH, s, oval: false);

            // 文章欄
            float tbx = w * Hs.TextX, tbw = w * Hs.TextW, tby = h * Hs.TextY, tbh = h * Hs.TextH;
            var tb = Ui.Panel(t, "TextBox", tbx, tby, tbw, tbh, Color.white);
            tb.sprite = CardArt.Hs("textbox"); tb.type = Image.Type.Simple; tb.raycastTarget = false;
            string typeLabel = def.Type switch { CardType.Spell => "スペル", CardType.Amulet => "アミュレット", _ => "" };
            string text = (string.IsNullOrEmpty(typeLabel) ? "" : $"<color=#7A1A12>{typeLabel}</color>\n") + def.Text.Replace("\n", " ");
            bool flavor = Mode == CardViewMode.Detail && !string.IsNullOrEmpty(def.Flavor);
            float padX = tbw * 0.12f, padTop = tbh * 0.10f, padBottom = tbh * (flavor ? 0.26f : 0.17f);   // 下の角は宝石が重なるので空ける
            var bodyText = Ui.Label(tb.transform, "Text", padX, padBottom, tbw - padX * 2, tbh - padTop - padBottom, text,
                Mathf.RoundToInt(12 * s), TextAnchor.MiddleCenter, FantasyUi.Ink);
            bodyText.resizeTextForBestFit = true;
            bodyText.resizeTextMaxSize = bodyText.fontSize;
            bodyText.resizeTextMinSize = Mathf.RoundToInt(8 * s);
            if (flavor)
            {
                // フレーバーは下の角の宝石に掛からない幅に(2026-09-23)
                var fl = Ui.Label(tb.transform, "Flavor", tbw * 0.22f, tbh * 0.05f, tbw * 0.56f, tbh * 0.20f, def.Flavor, Mathf.RoundToInt(8 * s), TextAnchor.MiddleCenter, FantasyUi.InkDim, FontStyle.Italic);
                fl.resizeTextForBestFit = true; fl.resizeTextMaxSize = fl.fontSize; fl.resizeTextMinSize = Mathf.RoundToInt(6 * s);
            }

            // 名前のリボン(絵の下端に重ねる)
            float bw = w * Hs.RibbonW, bh = h * Hs.RibbonH;
            var banner = Ui.Panel(t, "Banner", w * Hs.RibbonX, h * Hs.RibbonY, bw, bh, Color.white);
            banner.sprite = CardArt.Hs("ribbon"); banner.type = Image.Type.Simple; banner.raycastTarget = false;
            // 書体はもともと太字(NotoSerifJP-Bold)なので FontStyle.Bold を重ねない(擬似太字で字が潰れる)
            // 飾り(上下の宝石)に字が掛からない高さに収め、実物のハースストーンと同じく控えめな大きさに(2026-09-23 オーナー指摘)
            var nameLabel = Ui.Label(banner.transform, "Name", bw * 0.16f, bh * 0.30f, bw * 0.68f, bh * 0.42f, def.Name,
                Mathf.RoundToInt(12 * s), TextAnchor.MiddleCenter, NameInk, FontStyle.Normal);
            nameLabel.font = Ui.NameFont;
            nameLabel.resizeTextForBestFit = true;
            nameLabel.resizeTextMaxSize = nameLabel.fontSize;
            nameLabel.resizeTextMinSize = Mathf.RoundToInt(8 * s);
            nameLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
            nameLabel.verticalOverflow = VerticalWrapMode.Truncate;
            // 羊皮紙に刷った墨のように: 濃い茶の字 + 下に明るい縁を 1 本だけ(黒い縁取りは字の隙間が埋まって「貼り付けた」ように見えた。2026-09-23 オーナー指摘)
            var hl = nameLabel.gameObject.AddComponent<Shadow>();
            hl.effectColor = new Color(1f, 0.95f, 0.82f, 0.55f);
            hl.effectDistance = new Vector2(0f, -Mathf.Max(0.6f, 0.8f * s));

            // 宝石: コスト(左上の青い結晶)・攻撃(左下の赤)・体力(右下の緑)・カウントダウン(右下)
            float cd = w * Hs.CostD, sd = w * Hs.StatD;
            HsGem(t, "Cost", w * Hs.CostCx - cd / 2, h * Hs.CostCy - cd / 2, cd, "gem_cost", Mathf.RoundToInt(24 * s)).text = def.Cost.ToString();
            if (def.IsFollower)
            {
                _attackText = HsGem(t, "Atk", w * Hs.AtkCx - sd / 2, h * Hs.AtkCy - sd / 2, sd, "gem_attack", statFont);
                _healthText = HsGem(t, "Hp", w * Hs.HpCx - sd / 2, h * Hs.HpCy - sd / 2, sd, "gem_health", statFont);
            }
            else if (def.IsAmulet)
            {
                _healthText = HsGem(t, "Cd", w * Hs.HpCx - sd / 2, h * Hs.HpCy - sd / 2, sd, "gem_cost", statFont);
                _attackText = _healthText;
            }
            else
            {
                _attackText = _healthText = Ui.Label(t, "None", 0, 0, 1, 1, "", 1);
            }
            RefreshStats();
        }

        // 額の画像の窓の大きさ(画像に対する比)。Tools/ArtGen の cutout が表示する値
        private const float OvalHoleW = 0.751f, OvalHoleH = 0.803f;     // oval_thin
        private const float OvalThickHoleW = 0.76f, OvalThickHoleH = 0.82f; // oval_ring(太い額、リーダー用)
        private const float RectHoleW = 0.767f, RectHoleH = 0.732f;      // spell_frame

        /// <summary>強調表示の光(加算合成)。中心 (cx, cy)、大きさ w × h。はじめは透明。</summary>
        private static Image GlowImage(Transform parent, bool ellipse, float cx, float cy, float w, float h)
        {
            var img = Ui.Rect(parent, "Glow", cx - w / 2, cy - h / 2, w, h).gameObject.AddComponent<Image>();
            img.sprite = Icons.GlowOutline(ellipse);
            var mat = Fx.Additive;
            if (mat != null) img.material = mat;
            img.color = Color.clear;
            img.raycastTarget = false;
            img.transform.SetAsFirstSibling();
            return img;
        }

        /// <summary>楕円(または角丸の四角)に切り抜いた絵 + 額。絵が無いときは名前を置く。</summary>
        private static void ArtWindow(Transform parent, Sprite? art, CardDefinition def, float x, float y, float w, float h, float s, bool oval)
        {
            var maskRt = Ui.Rect(parent, "ArtWindow", x, y, w, h);
            var maskImg = maskRt.gameObject.AddComponent<Image>();
            maskImg.sprite = oval ? Icons.Ellipse() : Icons.RoundedRect(); maskImg.color = new Color(0.10f, 0.09f, 0.12f); maskImg.raycastTarget = false;
            maskRt.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            if (art != null)
            {
                float aspect = art.rect.width / art.rect.height;
                float iw = w, ih = w / aspect;
                if (ih < h) { ih = h; iw = h * aspect; }
                // 主役は画像の上半分にいるので、上端より少し下(4%)を窓の上端に合わせる
                var img = Ui.Rect(maskRt, "Art", (w - iw) / 2, h - ih + ih * 0.04f, iw, ih).gameObject.AddComponent<Image>();
                img.sprite = art; img.raycastTarget = false;
            }
            else
            {
                var nm = Ui.Label(maskRt, "Name", 6 * s, h * 0.35f, w - 12 * s, h * 0.3f, def.Name, Mathf.RoundToInt(18 * s), TextAnchor.MiddleCenter, Ui.TextMain, FontStyle.Bold);
                nm.resizeTextForBestFit = true; nm.resizeTextMaxSize = nm.fontSize; nm.resizeTextMinSize = Mathf.RoundToInt(9 * s);
            }
            Sprite? ringSprite; float hw, hh;
            if (oval)
            {
                ringSprite = CardArt.Hs("oval_thin"); hw = OvalHoleW; hh = OvalHoleH;
                if (ringSprite == null) { ringSprite = CardArt.Hs("oval_ring"); hw = OvalThickHoleW; hh = OvalThickHoleH; }
            }
            else { ringSprite = CardArt.Hs("spell_frame"); hw = RectHoleW; hh = RectHoleH; }
            if (ringSprite == null) return;
            // 窓の外周に額の内縁が来るように広げる
            float rw = w / hw, rh = h / hh;
            var ring = Ui.Rect(parent, "Ring", x - (rw - w) / 2, y - (rh - h) / 2, rw, rh).gameObject.AddComponent<Image>();
            ring.sprite = ringSprite; ring.raycastTarget = false;
        }

        /// <summary>
        /// リーダーの肖像(ハースストーンのヒーロー枠風): 太い金の楕円の額 + クラスの肖像。BattleScreen の左の欄で使う。
        /// </summary>
        public static void LeaderPortrait(Transform parent, CardClass cls, float x, float y, float w, float h)
        {
            var maskRt = Ui.Rect(parent, "Portrait", x, y, w, h);
            var maskImg = maskRt.gameObject.AddComponent<Image>();
            maskImg.sprite = Icons.Ellipse(); maskImg.color = new Color(0.10f, 0.09f, 0.12f); maskImg.raycastTarget = false;
            maskRt.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            var art = CardArt.Leader(cls);
            if (art != null)
            {
                float aspect = art.rect.width / art.rect.height;
                float iw = w, ih = w / aspect;
                if (ih < h) { ih = h; iw = h * aspect; }
                var img = Ui.Rect(maskRt, "Art", (w - iw) / 2, (h - ih) / 2, iw, ih).gameObject.AddComponent<Image>();
                img.sprite = art; img.raycastTarget = false;
            }
            var ringSprite = CardArt.Hs("oval_ring");
            if (ringSprite == null) return;
            float rw = w / OvalThickHoleW, rh = h / OvalThickHoleH;
            var ring = Ui.Rect(parent, "PortraitRing", x - (rw - w) / 2, y - (rh - h) / 2, rw, rh).gameObject.AddComponent<Image>();
            ring.sprite = ringSprite; ring.raycastTarget = false;
        }

        /// <summary>リボンのカード名の色(羊皮紙に刷った濃い茶の墨)。</summary>
        private static readonly Color NameInk = new Color(0.22f, 0.13f, 0.06f);

        /// <summary>宝石の画像 + 数値(Cinzel、暗い縁取り)。戻り値は数値の Text。</summary>
        internal static Text HsGem(Transform parent, string name, float x, float y, float size, string part, int fontSize)
        {
            var root = Ui.Rect(parent, name, x, y, size, size);
            var img = root.gameObject.AddComponent<Image>();
            img.sprite = CardArt.Hs(part); img.raycastTarget = false;
            // 数値は宝石の内側(中央 6 割)に必ず収める。2 桁は自動で縮む
            float m = size * 0.20f;
            var label = Ui.Label(root, "Value", m, m, size - m * 2, size - m * 2, "", Mathf.Min(fontSize, Mathf.RoundToInt(size * 0.50f)), TextAnchor.MiddleCenter, Color.white, FontStyle.Bold);
            label.font = Ui.DigitFont; label.fontStyle = FontStyle.Normal;
            label.resizeTextForBestFit = true;
            label.resizeTextMaxSize = label.fontSize;
            label.resizeTextMinSize = 6;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            label.raycastTarget = false;
            var outline = label.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.08f, 0.05f, 0.03f, 0.95f);
            outline.effectDistance = new Vector2(size * 0.022f, -size * 0.022f);
            var shadow = label.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.6f);
            shadow.effectDistance = new Vector2(size * 0.02f, -size * 0.035f);
            return label;
        }

        public void RefreshStats()
        {
            var def = Definition;
            if (def.IsFollower)
            {
                int atk = Entity?.Attack ?? def.Attack;
                int hp = Entity?.Health ?? def.Health;
                _attackText.text = atk.ToString();
                _healthText.text = hp.ToString();
                _attackText.color = atk > def.Attack ? Ui.Good : Color.white;
                _healthText.color = Entity != null && hp < Entity.MaxHealth ? new Color(1f, 0.6f, 0.6f) : (hp > def.Health ? Ui.Good : Color.white);
            }
            else if (def.IsAmulet)
            {
                int? cd = Entity?.Countdown ?? def.Countdown;
                _healthText.text = cd.HasValue ? cd.Value.ToString() : "";
                _healthText.transform.parent.gameObject.SetActive(cd.HasValue);
            }
        }

        /// <summary>強調表示の色(輪郭の光)。暗色 = 強調なし。</summary>
        public void SetFrame(Color color)
        {
            if (_glow != null) { _glow.color = color == Ui.PanelDarkColor ? Color.clear : new Color(color.r, color.g, color.b, 1f); return; }
            _frame.color = color == Ui.PanelDarkColor ? Color.clear : color;
        }

        public static string KeywordLabel(Keyword k) => k switch
        {
            Keyword.Ward => "守護",
            Keyword.Storm => "疾走",
            Keyword.Rush => "突進",
            Keyword.Bane => "必殺",
            Keyword.Drain => "ドレイン",
            _ => k.ToString(),
        };

        /// <summary>クラスの表示名(03-cards.md。ハースストーン日本語版にならったカタカナの職業名)。</summary>
        public static string ClassLabel(CardClass c) => c switch
        {
            CardClass.Neutral => "中立",
            CardClass.Knight => "パラディン",
            CardClass.Mage => "メイジ",
            CardClass.Necromancer => "ネクロマンサー",
            CardClass.Druid => "ドルイド",
            CardClass.Dragon => "ドラゴンナイト",
            CardClass.Chaos => "カオス",
            _ => c.ToString(),
        };

        public static string TriggerLabel(Trigger t) => t switch
        {
            Trigger.Fanfare => "FF",
            Trigger.LastWords => "LW",
            Trigger.TurnStart => "TS",
            _ => "",
        };

        // ---- ドラッグ ----

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!Draggable || Screen == null) { eventData.pointerDrag = null; return; }
            _originalParent = transform.parent;
            _originalPos = _rt.anchoredPosition;
            _originalSibling = transform.GetSiblingIndex();
            transform.SetParent(Screen.DragLayer, true);
            _group.blocksRaycasts = false;
            Screen.OnCardHover(this, false);
            Screen.OnCardDragBegin(this, eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!Draggable || Screen == null) return;
            _rt.position = eventData.position;
            Screen.OnCardDragging(this, eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (Screen == null) return;
            _group.blocksRaycasts = true;
            // 元の位置に戻す(再描画で作り直されるので厳密でなくてよい)
            if (_originalParent != null)
            {
                transform.SetParent(_originalParent, false);
                transform.SetSiblingIndex(_originalSibling);
                _rt.anchoredPosition = _originalPos;
            }
            Screen.OnCardDragEnd(this, eventData);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.dragging || Screen == null) return;
            Screen.OnCardHover(this, false);
            Screen.OnCardClicked(this, eventData);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (Screen == null || eventData.dragging) return;
            Screen.OnCardHover(this, true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (Screen == null) return;
            Screen.OnCardHover(this, false);
        }
    }
}
