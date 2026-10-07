using MiniGame.PenguinWars.Battle;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.PenguinWars
{
    /// <summary>
    /// 右下のペンギン砲ボタン（仕様書 §4.4・§7.1）。下端のゲージでチャージを見せ、
    /// 満タンになったら文字を「発射！」に変えて、色と文字の大きさを脈打たせて知らせる。
    /// </summary>
    public class CannonButton : MonoBehaviour
    {
        [SerializeField] private BattleRunner _battleRunner;
        [SerializeField] private Button _button;
        [SerializeField] private Image _background;
        [SerializeField] private Text _label;
        [Tooltip("チャージに合わせて横幅を伸ばす（右端のアンカーを動かす）")]
        [SerializeField] private RectTransform _chargeFill;
        [SerializeField] private Color _chargingColor = new Color(0.12f, 0.18f, 0.32f, 0.95f);
        [SerializeField] private Color _readyColor = new Color(1f, 0.55f, 0.1f, 1f);
        [SerializeField] private Color _readyFlashColor = new Color(1f, 0.85f, 0.3f, 1f);
        [SerializeField] private string _chargingText = "ペンギン砲";
        [SerializeField] private string _readyText = "発射！";
        [Tooltip("満タン時の脈打ちの速さ（1秒あたりの回数）")]
        [SerializeField] private float _pulsePerSecond = 1.5f;
        [Tooltip("満タン時に文字を最大何倍まで大きくするか")]
        [SerializeField] private float _pulseMaxScale = 1.12f;

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
            if (cannon.IsReady) ShowReady();
            else ShowCharging();
        }

        private void ShowCharging()
        {
            _background.color = _chargingColor;
            _label.text = _chargingText;
            _label.rectTransform.localScale = Vector3.one;
        }

        private void ShowReady()
        {
            // 0→1→0 を繰り返す波。ポーズ中（timeScale 0）は止まってよいので Time.time を使う
            float wave = 0.5f - 0.5f * Mathf.Cos(Time.time * _pulsePerSecond * 2f * Mathf.PI);
            _background.color = Color.Lerp(_readyColor, _readyFlashColor, wave);
            _label.text = _readyText;
            // ボタン本体は右下基準なので拡大すると左隣に重なる。文字だけ中心から拡大する
            _label.rectTransform.localScale = Vector3.one * Mathf.Lerp(1f, _pulseMaxScale, wave);
        }
    }
}
