using MiniGame.Editor;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.PenguinWars.Editor
{
    /// <summary>画面下の操作UI（働きペンギン・さかな表示・出撃ボタン5個・ページ切替・ペンギン砲）。仕様書 §7.1</summary>
    public static partial class PenguinWarsSceneBuilder
    {
        // スマホで押しやすいよう、ボタンの高さは画面高さ（1080）の約15%
        private const float ControlHeight = 162f;
        private const float ControlMarginX = 30f;
        private const float ControlMarginY = 20f;
        private const float ControlSpacing = 12f;

        private const float WalletButtonWidth = 240f;
        private const int WalletFontSize = 38;
        private const int FishFontSize = 40;
        private static readonly Vector2 FishLabelSize = new Vector2(500f, 60f);
        private const float FishLabelGap = 6f;

        private const float UnitButtonWidth = 170f;
        private const float UnitButtonGap = 28f;
        private static readonly Vector2 UnitIconSize = new Vector2(76f, 76f);
        private static readonly Vector2 UnitIconPosition = new Vector2(0f, -8f);
        private const int UnitNameFontSize = 22;
        private const int UnitNameMinFontSize = 12;
        private static readonly Vector2 UnitNameSize = new Vector2(166f, 30f);
        private static readonly Vector2 UnitNamePosition = new Vector2(0f, 46f);
        private const int UnitCostFontSize = 34;
        private static readonly Vector2 UnitCostSize = new Vector2(166f, 36f);
        private static readonly Vector2 UnitCostPosition = new Vector2(0f, 10f);
        private const float CooldownGaugeHeight = 10f;

        private const float PageButtonWidth = 120f;
        private const int PageFontSize = 30;

        private const float CannonButtonWidth = 260f;
        private const int CannonFontSize = 36;
        private static readonly Color CannonChargeColor = new Color(0.4f, 0.85f, 1f, 0.45f);

        private static readonly Color ControlButtonColor = new Color(0.18f, 0.26f, 0.4f, 0.92f);
        private static readonly Color CostColor = new Color(1f, 0.88f, 0.3f);
        private static readonly Color CooldownBackColor = new Color(0f, 0f, 0f, 0.6f);
        private static readonly Color CooldownFillColor = new Color(0.4f, 0.85f, 1f);
        // 出せないボタンを暗くする幕
        private static readonly Color DimmerColor = new Color(0f, 0f, 0f, 0.55f);

        private static (UnitButtonBar bar, WalletButton wallet, CannonButton cannon) CreateControls(Transform safeArea,
            BattleRunner battleRunner, PenguinUnitCatalog catalog)
        {
            WalletButton wallet = CreateWalletButton(safeArea, battleRunner);
            UnitButtonBar bar = CreateUnitButtonBar(safeArea, battleRunner, catalog);
            CannonButton cannon = CreateCannonButton(safeArea, battleRunner);
            return (bar, wallet, cannon);
        }

        /// <summary>右下。チャージは文字の後ろで左から伸びる帯で見せる</summary>
        private static CannonButton CreateCannonButton(Transform safeArea, BattleRunner battleRunner)
        {
            GameObject buttonObj = UIDialogBuilder.CreateButton(safeArea, "Btn_Cannon", "ペンギン砲",
                CannonButtonWidth, ControlHeight, ControlButtonColor);
            SetAnchor(buttonObj.GetComponent<RectTransform>(), BottomRightAnchor, new Vector2(-ControlMarginX, ControlMarginY));
            buttonObj.GetComponentInChildren<Text>().fontSize = CannonFontSize;

            Image charge = CreateImage(buttonObj.transform, "Charge", BottomLeftAnchor, Vector2.zero, Vector2.zero, CannonChargeColor);
            UIDialogBuilder.SetStretchAll(charge.rectTransform);
            charge.rectTransform.anchorMax = new Vector2(0f, 1f);
            // 文字より奥に描く
            charge.transform.SetAsFirstSibling();

            var cannon = buttonObj.AddComponent<CannonButton>();
            SetRefs(cannon, ("_battleRunner", battleRunner), ("_button", buttonObj.GetComponent<Button>()),
                ("_background", buttonObj.GetComponent<Image>()), ("_chargeFill", charge.rectTransform));
            return cannon;
        }

        private static WalletButton CreateWalletButton(Transform safeArea, BattleRunner battleRunner)
        {
            GameObject buttonObj = UIDialogBuilder.CreateButton(safeArea, "Btn_Wallet", string.Empty,
                WalletButtonWidth, ControlHeight, ControlButtonColor);
            SetAnchor(buttonObj.GetComponent<RectTransform>(), BottomLeftAnchor, new Vector2(ControlMarginX, ControlMarginY));
            Text levelLabel = buttonObj.GetComponentInChildren<Text>();
            levelLabel.fontSize = WalletFontSize;
            GameObject dimmer = CreateDimmer(buttonObj.transform);

            Vector2 fishPosition = new Vector2(ControlMarginX, ControlMarginY + ControlHeight + FishLabelGap);
            Text fishLabel = CreateText(safeArea, "FishLabel", FishFontSize, BottomLeftAnchor, fishPosition, FishLabelSize, Color.white);
            fishLabel.alignment = TextAnchor.MiddleLeft;

            var wallet = buttonObj.AddComponent<WalletButton>();
            SetRefs(wallet, ("_battleRunner", battleRunner), ("_button", buttonObj.GetComponent<Button>()),
                ("_levelLabel", levelLabel), ("_fishLabel", fishLabel), ("_dimmer", dimmer));
            return wallet;
        }

        /// <summary>5個のボタンとページ切替を横に並べ、画面下の中央に置く</summary>
        private static UnitButtonBar CreateUnitButtonBar(Transform safeArea, BattleRunner battleRunner, PenguinUnitCatalog catalog)
        {
            float totalWidth = UnitButtonBar.SlotsPerPage * (UnitButtonWidth + ControlSpacing) + UnitButtonGap + PageButtonWidth;
            GameObject barObj = UIDialogBuilder.CreateUIObject("UnitButtonBar", safeArea);
            RectTransform barRect = barObj.GetComponent<RectTransform>();
            SetAnchor(barRect, BottomCenterAnchor, new Vector2(0f, ControlMarginY));
            barRect.sizeDelta = new Vector2(totalWidth, ControlHeight);

            var buttons = new UnitButton[UnitButtonBar.SlotsPerPage];
            for (int i = 0; i < buttons.Length; i++)
            {
                buttons[i] = CreateUnitButton(barObj.transform, i, i * (UnitButtonWidth + ControlSpacing));
            }

            GameObject pageObj = UIDialogBuilder.CreateButton(barObj.transform, "Btn_Page", string.Empty,
                PageButtonWidth, ControlHeight, HudButtonColor);
            float pageX = buttons.Length * (UnitButtonWidth + ControlSpacing) + UnitButtonGap;
            SetAnchor(pageObj.GetComponent<RectTransform>(), BottomLeftAnchor, new Vector2(pageX, 0f));
            Text pageLabel = pageObj.GetComponentInChildren<Text>();
            pageLabel.fontSize = PageFontSize;

            var bar = barObj.AddComponent<UnitButtonBar>();
            var so = new UnityEditor.SerializedObject(bar);
            SerializedArray(so, "_buttons", buttons);
            so.ApplyModifiedPropertiesWithoutUndo();
            SetRefs(bar, ("_battleRunner", battleRunner), ("_catalog", catalog),
                ("_pageButton", pageObj.GetComponent<Button>()), ("_pageLabel", pageLabel));
            return bar;
        }

        private static UnitButton CreateUnitButton(Transform bar, int index, float x)
        {
            GameObject buttonObj = UIDialogBuilder.CreateButton(bar, $"Btn_Unit{index + 1}", string.Empty,
                UnitButtonWidth, ControlHeight, ControlButtonColor);
            SetAnchor(buttonObj.GetComponent<RectTransform>(), BottomLeftAnchor, new Vector2(x, 0f));
            // 共通ヘルパーが作る中央ラベルは使わない（名前とコストを別々に置くため）
            Object.DestroyImmediate(buttonObj.GetComponentInChildren<Text>().gameObject);

            Image icon = CreateImage(buttonObj.transform, "Icon", TopCenterAnchor, UnitIconPosition, UnitIconSize, Color.white);
            // Image は枠いっぱいに引き伸ばすので、ドット絵の縦横比を保つ
            icon.preserveAspect = true;
            Text nameLabel = CreateText(buttonObj.transform, "Name", UnitNameFontSize, BottomCenterAnchor, UnitNamePosition, UnitNameSize, Color.white);
            // 長い名前（こおりのじょおうペンギン など）でもボタンからはみ出さないよう縮めて収める
            FitText(nameLabel, UnitNameMinFontSize);
            Text costLabel = CreateText(buttonObj.transform, "Cost", UnitCostFontSize, BottomCenterAnchor, UnitCostPosition, UnitCostSize, CostColor);
            (GameObject gauge, RectTransform fill) = CreateCooldownGauge(buttonObj.transform);
            GameObject dimmer = CreateDimmer(buttonObj.transform);

            var unitButton = buttonObj.AddComponent<UnitButton>();
            SetRefs(unitButton, ("_button", buttonObj.GetComponent<Button>()), ("_icon", icon), ("_nameLabel", nameLabel),
                ("_costLabel", costLabel), ("_cooldownGauge", gauge), ("_cooldownFill", fill), ("_dimmer", dimmer));
            return unitButton;
        }

        /// <summary>ボタン下端の細いバー。中の Fill の右端アンカーを UnitButton が動かす</summary>
        private static (GameObject gauge, RectTransform fill) CreateCooldownGauge(Transform button)
        {
            Image back = CreateImage(button, "CooldownGauge", BottomLeftAnchor, Vector2.zero, Vector2.zero, CooldownBackColor);
            RectTransform backRect = back.rectTransform;
            backRect.anchorMin = Vector2.zero;
            backRect.anchorMax = new Vector2(1f, 0f);
            backRect.sizeDelta = new Vector2(0f, CooldownGaugeHeight);

            Image fill = CreateImage(back.transform, "Fill", BottomLeftAnchor, Vector2.zero, Vector2.zero, CooldownFillColor);
            UIDialogBuilder.SetStretchAll(fill.rectTransform);
            back.gameObject.SetActive(false);
            return (back.gameObject, fill.rectTransform);
        }

        private static GameObject CreateDimmer(Transform button)
        {
            Image dimmer = CreateImage(button, "Dimmer", BottomLeftAnchor, Vector2.zero, Vector2.zero, DimmerColor);
            UIDialogBuilder.SetStretchAll(dimmer.rectTransform);
            return dimmer.gameObject;
        }
    }
}
