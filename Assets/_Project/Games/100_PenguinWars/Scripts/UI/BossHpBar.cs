using MiniGame.PenguinWars.Battle;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.PenguinWars
{
    /// <summary>
    /// ボスが場にいる間だけ、画面上部にボスの名前とHPバーを出す。戦場をスクロールしてボスが画面外にいても減り具合が分かるように
    /// </summary>
    public class BossHpBar : MonoBehaviour
    {
        private const string BossPrefix = "ボス ";

        [SerializeField] private BattleRunner _battleRunner;
        [SerializeField] private PenguinUnitCatalog _catalog;
        [Tooltip("表示・非表示を切り替える親")]
        [SerializeField] private GameObject _root;
        [Tooltip("残りHPに合わせて右端のアンカーを動かす")]
        [SerializeField] private RectTransform _fill;
        [SerializeField] private Text _nameLabel;

        // 名前は出ているボスが変わったときだけ書き直す（毎フレーム文字列を作らないため）
        private int _shownUnitId = -1;

        private void Update()
        {
            UnitState boss = FindBoss(_battleRunner.World);
            _root.SetActive(boss != null);
            if (boss == null) return;

            _fill.anchorMax = new Vector2(boss.HpRatio, 1f);
            if (boss.Id == _shownUnitId) return;

            _shownUnitId = boss.Id;
            PenguinUnitData data = _catalog.Get(boss.UnitNo);
            _nameLabel.text = BossPrefix + UnitLabels.Name(data, boss.UnitNo);
        }

        /// <summary>同時に2体いたら先に出た方（倒せば次のボスに切り替わる）</summary>
        public static UnitState FindBoss(BattleWorld world)
        {
            if (world == null) return null;

            foreach (UnitState unit in world.Units)
            {
                if (unit.IsBoss && !unit.IsDead) return unit;
            }
            return null;
        }
    }
}
