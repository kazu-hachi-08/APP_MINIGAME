using System;
using System.Collections.Generic;
using MiniGame.PenguinWars.Battle;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.PenguinWars
{
    /// <summary>
    /// じぶんペンギンの作成画面の「のうりょく」タブ（役割・コスト・強化レベル・範囲攻撃・能力）。
    /// 画面の状態は持たず、変更のたびに CustomUnitRules.Sanitize を通した定義から描き直す（画面と保存がずれないように）。
    /// どの段階で何が解放されるかは CustomUnitRules に問い合わせ、ここには書かない
    /// </summary>
    public class CustomStatsTab : MonoBehaviour
    {
        private const string NumberFormat = "0.##";
        private const string SecondsSuffix = "秒";
        private const string LockedFormat = "コスト {0} から";
        private const string InfoFormat = "段階 {0}　　強化ポイント のこり {1} / {2}　　能力枠 {3}";
        private const string NoticeFormat = "コストが下がったので {0} をもどしました";
        private const string NoticeSeparator = "・";
        private const string NoticeAbility = "能力";
        private const string NoticeArea = "範囲攻撃";
        private const string NoAbility = "なし";
        private const string AreaOn = "あり";
        private const string AreaOff = "なし";
        private const char TierOn = '●';
        private const char TierOff = '○';

        [SerializeField] private Button _rolePrevButton;
        [SerializeField] private Button _roleNextButton;
        [SerializeField] private Text _roleLabel;
        [Tooltip("値は「役割の下限から何刻み目か」。刻み（壁10・ほか50）でしか止まらないように整数にする")]
        [SerializeField] private Slider _costSlider;
        [SerializeField] private Button _costDownButton;
        [SerializeField] private Button _costUpButton;
        [SerializeField] private Text _costLabel;
        [SerializeField] private Text _infoLabel;
        [SerializeField] private CustomStatRow[] _statRows;
        [SerializeField] private Button _areaButton;
        [SerializeField] private Text _areaLabel;
        [Tooltip("能力の枠（並びが枠の番号）")]
        [SerializeField] private Button[] _abilityPrevButtons;
        [SerializeField] private Button[] _abilityNextButtons;
        [SerializeField] private Text[] _abilityLabels;
        [SerializeField] private Text _noticeLabel;
        [Tooltip("段階が下がって外れたものを知らせる1行を出しておく秒数（黙って消えると驚くため）")]
        [SerializeField] private float _noticeSeconds = 2f;

        private static readonly UnitRole[] Roles = (UnitRole[])Enum.GetValues(typeof(UnitRole));
        private static readonly UnitAbilityType[] AbilityTypes = (UnitAbilityType[])Enum.GetValues(typeof(UnitAbilityType));

        private CustomUnitDefinition _def;
        // 役割を変えるとスライダーの上限が変わり、Slider がその場で値を丸めて onValueChanged を出すので、描き直し中は無視する
        private bool _isRefreshing;

        /// <summary>強さが変わった（見た目は変わらない。役割が変わったときだけ大きさが変わり得る）</summary>
        public event Action Changed;

        private void Awake()
        {
            _rolePrevButton.onClick.AddListener(() => StepRole(-1));
            _roleNextButton.onClick.AddListener(() => StepRole(1));
            _costSlider.wholeNumbers = true;
            _costSlider.onValueChanged.AddListener(OnCostSliderMoved);
            _costDownButton.onClick.AddListener(() => StepCost(-1));
            _costUpButton.onClick.AddListener(() => StepCost(1));
            foreach (CustomStatRow row in _statRows) row.StepRequested += StepLevel;
            _areaButton.onClick.AddListener(ToggleArea);
            for (int i = 0; i < _abilityLabels.Length; i++)
            {
                int slot = i;
                _abilityPrevButtons[i].onClick.AddListener(() => StepAbility(slot, -1));
                _abilityNextButtons[i].onClick.AddListener(() => StepAbility(slot, 1));
            }
        }

        /// <summary>枠を切り替えたとき・保存して作り直したときに呼ぶ。ここでは Changed を出さない</summary>
        public void Bind(CustomUnitDefinition def)
        {
            _def = def;
            HideNotice();
            Refresh();
        }

        // ---- 操作 ----

        /// <summary>新しい役割の帯の同じ位置（t）にコストを寄せ、段階がなるべく変わらないようにする</summary>
        private void StepRole(int direction)
        {
            PenguinUiSound.Click();
            UnitStatFormula.CostRange(_def.Role, out int oldMin, out int oldMax);
            float t = (float)(_def.Cost - oldMin) / (oldMax - oldMin);

            int index = Array.IndexOf(Roles, _def.Role);
            UnitRole role = Roles[(index + direction + Roles.Length) % Roles.Length];
            UnitStatFormula.CostRange(role, out int min, out int max);
            Apply(next =>
            {
                next.Role = role;
                next.Cost = Mathf.RoundToInt(Mathf.Lerp(min, max, t));
            });
        }

        private void OnCostSliderMoved(float steps)
        {
            if (_isRefreshing) return;

            UnitStatFormula.CostRange(_def.Role, out int min, out _);
            int cost = min + Mathf.RoundToInt(steps) * CustomUnitRules.CostStep(_def.Role);
            if (cost != _def.Cost) Apply(next => next.Cost = cost);
        }

        private void StepCost(int direction)
        {
            PenguinUiSound.Click();
            Apply(next => next.Cost += direction * CustomUnitRules.CostStep(next.Role));
        }

        private void StepLevel(CustomStat stat, int direction)
        {
            PenguinUiSound.Click();
            Apply(next => next.Levels.Set(stat, next.Levels.Get(stat) + direction));
        }

        private void ToggleArea()
        {
            PenguinUiSound.Click();
            Apply(next => next.IsAreaAttack = !next.IsAreaAttack);
        }

        /// <summary>「なし」→ 8種 → 「なし」と回す。ほかの枠で選んでいる能力は飛ばす（同じ能力を2つ付けられないように）</summary>
        private void StepAbility(int slot, int direction)
        {
            PenguinUiSound.Click();
            UnitAbilityType? current = AbilityAt(slot);
            // 選択肢の並び: 0 = なし、1〜 = AbilityTypes
            int count = AbilityTypes.Length + 1;
            int index = current.HasValue ? Array.IndexOf(AbilityTypes, current.Value) + 1 : 0;
            for (int i = 0; i < count; i++)
            {
                index = (index + direction + count) % count;
                UnitAbilityType? candidate = index == 0 ? (UnitAbilityType?)null : AbilityTypes[index - 1];
                if (candidate.HasValue && IsUsedByOtherSlot(candidate.Value, slot)) continue;

                Apply(next => SetAbility(next.Abilities, slot, candidate));
                return;
            }
        }

        private UnitAbilityType? AbilityAt(int slot) => slot < _def.Abilities.Count ? _def.Abilities[slot] : (UnitAbilityType?)null;

        private bool IsUsedByOtherSlot(UnitAbilityType type, int slot)
        {
            int index = _def.Abilities.IndexOf(type);
            return index >= 0 && index != slot;
        }

        /// <summary>前の枠が「なし」のまま後ろの枠だけ埋まることはないので、空いたら前に詰める</summary>
        private static void SetAbility(List<UnitAbilityType> abilities, int slot, UnitAbilityType? type)
        {
            if (slot < abilities.Count) abilities.RemoveAt(slot);
            if (!type.HasValue) return;

            abilities.Insert(Mathf.Min(slot, abilities.Count), type.Value);
        }

        /// <summary>コピーを変えて Sanitize を通し、強さの項目だけ書き戻す（名前は入力中に空のこともあるので触らない）</summary>
        private void Apply(Action<CustomUnitDefinition> edit)
        {
            CustomUnitDefinition next = _def.Clone();
            edit(next);
            CustomUnitDefinition fixedDef = CustomUnitRules.Sanitize(next);
            ShowRemovedNotice(next, fixedDef);

            _def.Role = fixedDef.Role;
            _def.Cost = fixedDef.Cost;
            _def.Levels = fixedDef.Levels;
            _def.IsAreaAttack = fixedDef.IsAreaAttack;
            _def.Abilities = fixedDef.Abilities;
            Refresh();
            Changed?.Invoke();
        }

        // ---- 外れたものの知らせ ----

        private void ShowRemovedNotice(CustomUnitDefinition requested, CustomUnitDefinition fixedDef)
        {
            var removed = new List<string>();
            foreach (CustomStat stat in CustomStatLevels.AllStats)
            {
                if (requested.Levels.Get(stat) != fixedDef.Levels.Get(stat)) removed.Add(CustomStatRow.StatName(stat));
            }
            if (requested.Abilities.Count > fixedDef.Abilities.Count) removed.Add(NoticeAbility);
            if (requested.IsAreaAttack && !fixedDef.IsAreaAttack) removed.Add(NoticeArea);
            if (removed.Count == 0) return;

            _noticeLabel.text = string.Format(NoticeFormat, string.Join(NoticeSeparator, removed));
            _noticeLabel.gameObject.SetActive(true);
            CancelInvoke(nameof(HideNotice));
            Invoke(nameof(HideNotice), _noticeSeconds);
        }

        private void HideNotice()
        {
            CancelInvoke(nameof(HideNotice));
            _noticeLabel.gameObject.SetActive(false);
        }

        // ---- 描画 ----

        private void Refresh()
        {
            _isRefreshing = true;
            try
            {
                RefreshAll();
            }
            finally
            {
                _isRefreshing = false;
            }
        }

        private void RefreshAll()
        {
            int tier = CustomUnitRules.Tier(_def.Role, _def.Cost);
            _roleLabel.text = UnitLabels.Role(_def.Role);
            RefreshCost();
            int points = CustomUnitRules.Points(tier);
            int left = points - CustomUnitRules.SpentPoints(_def.Levels);
            _infoLabel.text = string.Format(InfoFormat, TierDots(tier), left, points, CustomUnitRules.AbilitySlots(tier));
            RefreshStatRows(tier, left);
            RefreshArea(tier);
            RefreshAbilities(tier);
        }

        private void RefreshCost()
        {
            UnitStatFormula.CostRange(_def.Role, out int min, out int max);
            int step = CustomUnitRules.CostStep(_def.Role);
            _costSlider.minValue = 0;
            _costSlider.maxValue = (max - min) / step;
            _costSlider.SetValueWithoutNotify((_def.Cost - min) / step);
            _costDownButton.interactable = _def.Cost > min;
            _costUpButton.interactable = _def.Cost < max;
            _costLabel.text = _def.Cost.ToString();
        }

        private static string TierDots(int tier)
        {
            return new string(TierOn, tier) + new string(TierOff, CustomUnitRules.MaxTier - tier);
        }

        /// <summary>右端は「レベル0の値 → 今の値」。能力・範囲攻撃の減額は両方に入るので、付けると数値がその場で下がって見える</summary>
        private void RefreshStatRows(int tier, int pointsLeft)
        {
            UnitStats stats = Calculate(_def);
            CustomUnitDefinition baseDef = _def.Clone();
            baseDef.Levels.Reset();
            UnitStats baseStats = Calculate(baseDef);
            StatTweak tweak = _def.Levels.ToTweak();

            foreach (CustomStatRow row in _statRows)
            {
                CustomStat stat = row.Stat;
                string value = Format(stats, stat);
                if (!CustomUnitRules.IsStatUnlocked(tier, stat))
                {
                    row.ShowLocked(CustomUnitRules.MinCostForTier(_def.Role, CustomUnitRules.StatUnlockTier(stat)), value);
                    continue;
                }

                int level = _def.Levels.Get(stat);
                row.Show(level, Multiplier(tweak, stat),
                    canDown: level > CustomUnitRules.MinLevel,
                    canUp: level < CustomUnitRules.MaxLevel && pointsLeft > 0,
                    Format(baseStats, stat), value);
            }
        }

        private void RefreshArea(int tier)
        {
            bool canUse = CustomUnitRules.CanUseArea(tier);
            _areaButton.interactable = canUse;
            _areaLabel.text = canUse
                ? (_def.IsAreaAttack ? AreaOn : AreaOff)
                : string.Format(LockedFormat, CustomUnitRules.MinCostForTier(_def.Role, CustomUnitRules.AreaUnlockTier));
        }

        private void RefreshAbilities(int tier)
        {
            int slots = CustomUnitRules.AbilitySlots(tier);
            for (int i = 0; i < _abilityLabels.Length; i++)
            {
                // 前の枠が空のうちは後ろの枠を選ばせない（SetAbility で前に詰めるため、選んでも前の枠に入って分かりにくい）
                bool canUse = i < slots && i <= _def.Abilities.Count;
                _abilityPrevButtons[i].interactable = canUse;
                _abilityNextButtons[i].interactable = canUse;
                UnitAbilityType? type = AbilityAt(i);
                _abilityLabels[i].text = i >= slots
                    ? string.Format(LockedFormat, CustomUnitRules.MinCostForTier(_def.Role, CustomUnitRules.AbilitySlotUnlockTier(i)))
                    : type.HasValue ? UnitLabels.Ability(type.Value) : NoAbility;
            }
        }

        private static UnitStats Calculate(CustomUnitDefinition def)
        {
            return UnitStatFormula.Calculate(CustomUnitRules.ToUnitDefinition(def, CustomUnitRules.LeftNo));
        }

        /// <summary>ずかんと同じ単位・桁で出す</summary>
        private static string Format(UnitStats stats, CustomStat stat)
        {
            switch (stat)
            {
                case CustomStat.Hp: return stats.MaxHp.ToString();
                case CustomStat.Attack: return stats.Attack.ToString();
                case CustomStat.Range: return stats.Range.ToString(NumberFormat);
                case CustomStat.Speed: return stats.MoveSpeed.ToString(NumberFormat);
                default: return stats.Cooldown.ToString(NumberFormat) + SecondsSuffix;
            }
        }

        private static float Multiplier(StatTweak tweak, CustomStat stat)
        {
            switch (stat)
            {
                case CustomStat.Hp: return tweak.Hp;
                case CustomStat.Attack: return tweak.Attack;
                case CustomStat.Range: return tweak.Range;
                case CustomStat.Speed: return tweak.Speed;
                default: return tweak.Cooldown;
            }
        }
    }
}
