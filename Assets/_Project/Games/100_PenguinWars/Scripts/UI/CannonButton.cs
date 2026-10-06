using MiniGame.PenguinWars.Battle;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.PenguinWars
{
    /// <summary>
    /// 右下のペンギン砲ボタン（仕様書 §4.4・§7.1）。チャージゲージを描き、満タンでボタンの色を変えて知らせる。
    /// </summary>
    public class CannonButton : MonoBehaviour
    {
        [SerializeField] private BattleRunner _battleRunner;
        [SerializeField] private Button _button;
        [SerializeField] private Image _background;
        [Tooltip("チャージに合わせて横幅を伸ばす（右端のアンカーを動かす）")]
        [SerializeField] private RectTransform _chargeFill;
        [SerializeField] private Color _chargingColor = new Color(0.18f, 0.26f, 0.4f, 0.92f);
        [SerializeField] private Color _readyColor = new Color(1f, 0.75f, 0.15f, 1f);

        private void Awake()
        {
            _button.onClick.AddListener(Fire);
        }

        /// <summary>Space キーもここを通す</summary>
        public void Fire()
        {
            _battleRunner.Enqueue(BattleCommand.FireCannon(Side.Left));
        }

        private void Update()
        {
            BattleWorld world = _battleRunner.World;
            if (world == null) return;

            CannonState cannon = world.GetCannon(Side.Left);
            _chargeFill.anchorMax = new Vector2(cannon.ChargeRatio, 1f);
            _background.color = cannon.IsReady ? _readyColor : _chargingColor;
        }
    }
}
