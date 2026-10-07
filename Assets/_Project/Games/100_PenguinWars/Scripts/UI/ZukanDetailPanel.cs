using MiniGame.PenguinWars.Battle;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.PenguinWars
{
    /// <summary>
    /// ずかんの詳細（仕様書 §2.0）。数値はカタログのアセットから毎回読むので、定義表を変えて生成し直せばそのまま反映される
    /// </summary>
    public class ZukanDetailPanel : MonoBehaviour
    {
        private const string NumberFormat = "0.##";

        [SerializeField] private UnitSpriteAnimator _icon;
        [SerializeField] private Text _noLabel;
        [SerializeField] private Text _nameLabel;
        [SerializeField] private Text _statsLabel;
        [SerializeField] private Button _closeButton;

        private void Awake()
        {
            _closeButton.onClick.AddListener(Close);
        }

        public void Show(PenguinUnitData data)
        {
            _icon.Play(data.GetSprites(Side.Left), UnitSpriteAnimator.WalkAndAttackFrames);
            _noLabel.text = $"No.{data.No}";
            _nameLabel.text = data.DisplayName;
            _statsLabel.text = FormatStats(data.ToStats());
            gameObject.SetActive(true);
        }

        public void Hide() => gameObject.SetActive(false);

        /// <summary>じぶんペンギンの作成画面でも同じ書き方で数値を出す</summary>
        public static string FormatStats(UnitStats stats)
        {
            return $"役割　{UnitLabels.Role(stats.Role)}\n"
                + $"コスト　{stats.Cost}　　再生産　{stats.Cooldown.ToString(NumberFormat)}秒\n"
                + $"体力　{stats.MaxHp}　　攻撃力　{stats.Attack}\n"
                + $"射程　{stats.Range.ToString(NumberFormat)}　　速度　{stats.MoveSpeed.ToString(NumberFormat)}\n"
                + $"能力　{UnitLabels.AbilitiesWithArea(stats)}";
        }

        private void Close()
        {
            PenguinUiSound.Click();
            Hide();
        }
    }
}
