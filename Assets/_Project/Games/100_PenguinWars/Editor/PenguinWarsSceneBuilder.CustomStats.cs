using MiniGame.Editor;
using MiniGame.PenguinWars.Battle;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.PenguinWars.Editor
{
    /// <summary>
    /// じぶんペンギンの作成画面の「のうりょく」タブ。みためタブ（.Custom.cs）と同じ領域に、上から
    /// 知らせ → 役割 → コスト → 段階・ポイント → 5つの数値 → 範囲攻撃 → 能力 の11行を並べる
    /// </summary>
    public static partial class PenguinWarsSceneBuilder
    {
        // 11行を CustomTabRootSize（高さ640）に収める
        private const float StatsRowHeight = 54f;
        private const float StatsRowStep = 58f;
        private const int StatsFontSize = 34;
        private const int StatsMinFontSize = 20;
        private const float StatsLabelX = -415f;
        private const float StatsLabelWidth = 170f;
        private const float StatsSmallButtonWidth = 70f;

        // 役割・範囲攻撃の行（◀ 値 ▶）
        private const float StatsCyclePrevX = -290f;
        private const float StatsCycleValueX = -110f;
        private const float StatsCycleValueWidth = 280f;
        private const float StatsCycleNextX = 70f;

        // コストの行（[-] スライダー [+] 値）
        private const float StatsCostDownX = -290f;
        private const float StatsSliderX = -30f;
        private const float StatsSliderWidth = 420f;
        private const float StatsCostUpX = 230f;
        private const float StatsCostValueX = 380f;
        private const float StatsCostValueWidth = 200f;
        private const float StatsSliderTrackHeight = 14f;
        private const float StatsSliderHandleSize = 48f;
        private static readonly Color StatsSliderTrackColor = new Color(0.3f, 0.35f, 0.45f);
        private static readonly Color StatsSliderFillColor = new Color(0.95f, 0.55f, 0.15f);

        // 数値の行（[-] ゲージ [+] 倍率 値）。🔒 の文はゲージ〜倍率の場所に出す
        private const float StatsMinusX = -290f;
        private const float StatsGaugeX = -180f;
        private const float StatsGaugeWidth = 150f;
        private const float StatsPlusX = -70f;
        private const float StatsMultiplierX = 35f;
        private const float StatsMultiplierWidth = 120f;
        private const float StatsValueX = 300f;
        private const float StatsValueWidth = 400f;
        private const float StatsLockedX = -120f;
        private const float StatsLockedWidth = 420f;

        // 能力の行は2枠を横に並べる
        private const float StatsAbilityValueWidth = 200f;
        private static readonly float[] StatsAbilitySlotX = { -150f, 210f };

        private static readonly Color StatsNoticeColor = new Color(1f, 0.6f, 0.45f);
        private static readonly Color StatsLockedColor = new Color(0.65f, 0.7f, 0.8f);

        private static readonly CustomStat[] StatsRowOrder =
        {
            CustomStat.Hp, CustomStat.Attack, CustomStat.Speed, CustomStat.Cooldown, CustomStat.Range,
        };

        private static float StatsRowY(int row) => CustomTabRootSize.y * 0.5f - StatsRowHeight * 0.5f - row * StatsRowStep;

        private static CustomStatsTab CreateCustomStatsTab(Transform panel)
        {
            RectTransform root = CreateCustomTabRoot(panel, "StatsTab");
            int row = 0;

            Text notice = CreateStatsText(root, "Notice", 0f, StatsRowY(row++), CustomTabRootSize.x, StatsNoticeColor);
            notice.gameObject.SetActive(false);

            CreateStatsLabel(root, "RoleLabel", "やくわり", StatsRowY(row));
            (Button rolePrev, Text role, Button roleNext) = CreateStatsCycle(root, "Role", StatsCyclePrevX, StatsCycleValueX,
                StatsCycleValueWidth, StatsCycleNextX, StatsRowY(row++));

            float costY = StatsRowY(row++);
            CreateStatsLabel(root, "CostLabel", "コスト", costY);
            Button costDown = CreateStatsButton(root, "Btn_CostDown", "-", StatsCostDownX, costY, StatsSmallButtonWidth);
            Slider slider = CreateStatsSlider(root, costY);
            Button costUp = CreateStatsButton(root, "Btn_CostUp", "+", StatsCostUpX, costY, StatsSmallButtonWidth);
            Text cost = CreateStatsText(root, "CostValue", StatsCostValueX, costY, StatsCostValueWidth, MessageColor);

            Text info = CreateStatsText(root, "Info", 0f, StatsRowY(row++), CustomTabRootSize.x, TitleSubColor);

            var statRows = new CustomStatRow[StatsRowOrder.Length];
            for (int i = 0; i < statRows.Length; i++) statRows[i] = CreateCustomStatRow(root, StatsRowOrder[i], StatsRowY(row++));

            float areaY = StatsRowY(row++);
            CreateStatsLabel(root, "AreaLabel", "範囲攻撃", areaY);
            Button area = CreateStatsButton(root, "Btn_Area", string.Empty, StatsCycleValueX, areaY, StatsCycleValueWidth);
            Text areaText = area.GetComponentInChildren<Text>();

            float abilityY = StatsRowY(row);
            CreateStatsLabel(root, "AbilityLabel", "能力", abilityY);
            int slotCount = CustomUnitRules.MaxAbilitySlots;
            var abilityPrev = new Button[slotCount];
            var abilityNext = new Button[slotCount];
            var abilityLabels = new Text[slotCount];
            for (int i = 0; i < slotCount; i++)
            {
                float center = StatsAbilitySlotX[i];
                float offset = (StatsAbilityValueWidth + StatsSmallButtonWidth) * 0.5f;
                (abilityPrev[i], abilityLabels[i], abilityNext[i]) = CreateStatsCycle(root, $"Ability{i + 1}",
                    center - offset, center, StatsAbilityValueWidth, center + offset, abilityY);
            }

            var tab = root.gameObject.AddComponent<CustomStatsTab>();
            var so = new SerializedObject(tab);
            SerializedArray(so, "_statRows", statRows);
            SerializedArray(so, "_abilityPrevButtons", abilityPrev);
            SerializedArray(so, "_abilityNextButtons", abilityNext);
            SerializedArray(so, "_abilityLabels", abilityLabels);
            so.ApplyModifiedPropertiesWithoutUndo();
            SetRefs(tab, ("_rolePrevButton", rolePrev), ("_roleNextButton", roleNext), ("_roleLabel", role),
                ("_costSlider", slider), ("_costDownButton", costDown), ("_costUpButton", costUp), ("_costLabel", cost),
                ("_infoLabel", info), ("_areaButton", area), ("_areaLabel", areaText), ("_noticeLabel", notice));
            root.gameObject.SetActive(false);
            return tab;
        }

        private static CustomStatRow CreateCustomStatRow(Transform root, CustomStat stat, float y)
        {
            GameObject rowObj = UIDialogBuilder.CreateUIObject($"Row_{stat}", root);
            RectTransform rect = rowObj.GetComponent<RectTransform>();
            SetAnchor(rect, CenterAnchor, new Vector2(0f, y));
            rect.sizeDelta = new Vector2(CustomTabRootSize.x, StatsRowHeight);

            // 項目名は CustomStatRow が実行時に入れる
            Text name = CreateStatsLabel(rowObj.transform, "Name", string.Empty, 0f);
            Button minus = CreateStatsButton(rowObj.transform, "Btn_Minus", "-", StatsMinusX, 0f, StatsSmallButtonWidth);
            Text gauge = CreateStatsText(rowObj.transform, "Gauge", StatsGaugeX, 0f, StatsGaugeWidth, Color.white);
            Button plus = CreateStatsButton(rowObj.transform, "Btn_Plus", "+", StatsPlusX, 0f, StatsSmallButtonWidth);
            Text multiplier = CreateStatsText(rowObj.transform, "Multiplier", StatsMultiplierX, 0f, StatsMultiplierWidth, TitleSubColor);
            Text value = CreateStatsText(rowObj.transform, "Value", StatsValueX, 0f, StatsValueWidth, Color.white);
            value.alignment = TextAnchor.MiddleRight;
            Text locked = CreateStatsText(rowObj.transform, "Locked", StatsLockedX, 0f, StatsLockedWidth, StatsLockedColor);
            locked.gameObject.SetActive(false);

            var row = rowObj.AddComponent<CustomStatRow>();
            var so = new SerializedObject(row);
            FindPropertyOrLog(so, "_stat").enumValueIndex = (int)stat;
            so.ApplyModifiedPropertiesWithoutUndo();
            SetRefs(row, ("_nameLabel", name), ("_minusButton", minus), ("_plusButton", plus), ("_gaugeLabel", gauge),
                ("_multiplierLabel", multiplier), ("_valueLabel", value), ("_lockedLabel", locked));
            return row;
        }

        private static (Button prev, Text value, Button next) CreateStatsCycle(Transform root, string name,
            float prevX, float valueX, float valueWidth, float nextX, float y)
        {
            Button prev = CreateStatsButton(root, $"Btn_{name}Prev", "◀", prevX, y, StatsSmallButtonWidth);
            Text value = CreateStatsText(root, $"{name}Value", valueX, y, valueWidth, Color.white);
            Button next = CreateStatsButton(root, $"Btn_{name}Next", "▶", nextX, y, StatsSmallButtonWidth);
            return (prev, value, next);
        }

        /// <summary>行の左端に置く項目名（左寄せ）</summary>
        private static Text CreateStatsLabel(Transform parent, string name, string label, float y)
        {
            Text text = CreateStatsText(parent, name, StatsLabelX, y, StatsLabelWidth, Color.white);
            text.alignment = TextAnchor.MiddleLeft;
            text.text = label;
            return text;
        }

        /// <summary>「遠距離キラー」「コスト 3000 から」など長い文字も枠に収まるよう縮める</summary>
        private static Text CreateStatsText(Transform parent, string name, float x, float y, float width, Color color)
        {
            Text text = CreateText(parent, name, StatsFontSize, CenterAnchor, new Vector2(x, y), new Vector2(width, StatsRowHeight), color);
            FitText(text, StatsMinFontSize);
            return text;
        }

        private static Button CreateStatsButton(Transform parent, string name, string label, float x, float y, float width)
        {
            Button button = CreateAnchoredButton(parent, name, label, new Vector2(width, StatsRowHeight), CenterAnchor, new Vector2(x, y));
            Text text = button.GetComponentInChildren<Text>();
            text.fontSize = StatsFontSize;
            FitText(text, StatsMinFontSize);
            return button;
        }

        /// <summary>
        /// 共通の音量スライダーは上にラベル行が付いて高さが足りないので、ここで細いものを作る。
        /// 透明な Image を行の高さいっぱいに敷き、指で掴みやすくする（Slider は自分に当たったレイキャストでしかドラッグを受け取らないため）
        /// </summary>
        private static Slider CreateStatsSlider(Transform parent, float y)
        {
            Image hit = CreateImage(parent, "CostSlider", CenterAnchor, new Vector2(StatsSliderX, y),
                new Vector2(StatsSliderWidth, StatsRowHeight), Color.clear);
            hit.raycastTarget = true;

            // つまみが両端ではみ出さないよう、可動域をつまみの半径ぶん内側に寄せる
            float inset = StatsSliderHandleSize * 0.5f;
            RectTransform track = CreateStatsSliderBand(hit.transform, "Track", inset, StatsSliderTrackHeight);
            var trackImage = track.gameObject.AddComponent<Image>();
            trackImage.color = StatsSliderTrackColor;
            trackImage.raycastTarget = false;
            RectTransform fillArea = CreateStatsSliderBand(hit.transform, "Fill Area", inset, StatsSliderTrackHeight);
            Image fill = CreateImage(fillArea, "Fill", CenterAnchor, Vector2.zero, Vector2.zero, StatsSliderFillColor);
            UIDialogBuilder.SetStretchAll(fill.rectTransform);
            RectTransform handleArea = CreateStatsSliderBand(hit.transform, "Handle Slide Area", inset, StatsSliderHandleSize);
            Image handle = CreateImage(handleArea, "Handle", CenterAnchor, Vector2.zero,
                new Vector2(StatsSliderHandleSize, StatsSliderHandleSize), Color.white);
            handle.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

            var slider = hit.gameObject.AddComponent<Slider>();
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.wholeNumbers = true;
            return slider;
        }

        /// <summary>親の縦中央に、左右を inset ずつ内側へ寄せた高さ height の帯を置く</summary>
        private static RectTransform CreateStatsSliderBand(Transform parent, string name, float inset, float height)
        {
            RectTransform rect = UIDialogBuilder.CreateUIObject(name, parent).GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(inset, -height * 0.5f);
            rect.offsetMax = new Vector2(-inset, height * 0.5f);
            return rect;
        }
    }
}
