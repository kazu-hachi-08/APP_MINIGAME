using MiniGame.PenguinWars.Battle;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.PenguinWars
{
    /// <summary>左下の働きペンギンボタンと、さかなの残り表示（仕様書 §4.2・§7.1）</summary>
    public class WalletButton : MonoBehaviour
    {
        [SerializeField] private BattleRunner _battleRunner;
        [SerializeField] private Button _button;
        [SerializeField] private Text _levelLabel;
        [SerializeField] private Text _fishLabel;
        [SerializeField] private GameObject _dimmer;

        // 値が変わったときだけ文字列を作る（毎フレームのGCを避けるため）
        private int _shownLevel = -1;
        private int _shownFish = -1;
        private int _shownCap = -1;

        private void Awake()
        {
            _button.onClick.AddListener(LevelUp);
        }

        /// <summary>Q キーもここを通す</summary>
        public void LevelUp()
        {
            _battleRunner.Enqueue(BattleCommand.LevelUpWallet(Side.Left));
        }

        private void Update()
        {
            BattleWorld world = _battleRunner.World;
            if (world == null) return;

            WalletState wallet = world.GetWallet(Side.Left);
            RefreshLevel(wallet);
            RefreshFish(wallet);
            _dimmer.SetActive(!wallet.CanLevelUp);
        }

        private void RefreshLevel(WalletState wallet)
        {
            if (wallet.Level == _shownLevel) return;

            _shownLevel = wallet.Level;
            string next = wallet.IsMaxLevel ? "MAX" : wallet.LevelUpCost.ToString();
            _levelLabel.text = $"働き Lv{wallet.Level}\n{next}";
        }

        private void RefreshFish(WalletState wallet)
        {
            if (wallet.Fish == _shownFish && wallet.Cap == _shownCap) return;

            _shownFish = wallet.Fish;
            _shownCap = wallet.Cap;
            _fishLabel.text = $"さかな {_shownFish}/{_shownCap}";
        }
    }
}
