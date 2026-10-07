using System;
using System.Text;
using MiniGame.PenguinWars.Battle;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.PenguinWars
{
    /// <summary>
    /// のうりょくタブの「体力 [-] ゲージ [+] 倍率 数値」1行。数値の種類が違うだけで5行とも同じ動きなので使い回す。
    /// 状態は持たず、CustomStatsTab が毎回描き直す
    /// </summary>
    public class CustomStatRow : MonoBehaviour
    {
        private const string LockedFormat = "コスト {0} から";
        private const string MultiplierFormat = "×{0:0.0}";
        private const string ChangeArrow = " → ";
        private const string GaugeFilled = "■";
        private const string GaugeEmpty = "□";
        private const string GaugeCenter = "|";

        [SerializeField] private CustomStat _stat;
        [SerializeField] private Text _nameLabel;
        [SerializeField] private Button _minusButton;
        [SerializeField] private Button _plusButton;
        [SerializeField] private Text _gaugeLabel;
        [SerializeField] private Text _multiplierLabel;
        [SerializeField] private Text _valueLabel;
        [Tooltip("触れない段階のときだけ出す（[-][+]・ゲージ・倍率の代わり）")]
        [SerializeField] private Text _lockedLabel;
        [Tooltip("下げたレベルは上げたレベルと見分けがつくよう別の色にする")]
        [SerializeField] private Color _plusColor = new Color(1f, 0.85f, 0.25f);
        [SerializeField] private Color _minusColor = new Color(1f, 0.45f, 0.4f);

        /// <summary>[-] なら -1、[+] なら +1</summary>
        public event Action<CustomStat, int> StepRequested;

        public CustomStat Stat => _stat;

        private void Awake()
        {
            _nameLabel.text = StatName(_stat);
            _minusButton.onClick.AddListener(() => StepRequested?.Invoke(_stat, -1));
            _plusButton.onClick.AddListener(() => StepRequested?.Invoke(_stat, 1));
        }

        /// <param name="baseValue">強化レベル0のときの値。レベルが0なら出さない</param>
        public void Show(int level, float multiplier, bool canDown, bool canUp, string baseValue, string value)
        {
            SetLocked(false);
            _minusButton.interactable = canDown;
            _plusButton.interactable = canUp;
            _gaugeLabel.text = Gauge(level);
            _multiplierLabel.text = string.Format(MultiplierFormat, multiplier);
            _valueLabel.text = level == 0 ? value : baseValue + ChangeArrow + value;
        }

        /// <summary>何を上げれば触れるようになるか分かるよう、解放されるコストを出す</summary>
        public void ShowLocked(int unlockCost, string value)
        {
            SetLocked(true);
            _lockedLabel.text = string.Format(LockedFormat, unlockCost);
            _valueLabel.text = value;
        }

        private void SetLocked(bool isLocked)
        {
            _minusButton.gameObject.SetActive(!isLocked);
            _plusButton.gameObject.SetActive(!isLocked);
            _gaugeLabel.gameObject.SetActive(!isLocked);
            _multiplierLabel.gameObject.SetActive(!isLocked);
            _lockedLabel.gameObject.SetActive(isLocked);
        }

        /// <summary>「□■|■□」のように真ん中から左右へ塗る（左が下げた分・右が上げた分）</summary>
        private string Gauge(int level)
        {
            var builder = new StringBuilder();
            for (int i = CustomUnitRules.MinLevel; i < 0; i++) AppendCell(builder, level <= i, _minusColor);
            builder.Append(GaugeCenter);
            for (int i = 1; i <= CustomUnitRules.MaxLevel; i++) AppendCell(builder, level >= i, _plusColor);
            return builder.ToString();
        }

        private static void AppendCell(StringBuilder builder, bool isFilled, Color color)
        {
            if (!isFilled)
            {
                builder.Append(GaugeEmpty);
                return;
            }
            builder.Append("<color=#").Append(ColorUtility.ToHtmlStringRGB(color)).Append('>')
                .Append(GaugeFilled).Append("</color>");
        }

        public static string StatName(CustomStat stat)
        {
            switch (stat)
            {
                case CustomStat.Hp: return "体力";
                case CustomStat.Attack: return "攻撃";
                case CustomStat.Range: return "射程";
                case CustomStat.Speed: return "速度";
                case CustomStat.Cooldown: return "再生産";
                default: return stat.ToString();
            }
        }
    }
}
