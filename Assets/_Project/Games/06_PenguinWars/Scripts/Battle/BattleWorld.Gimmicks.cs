using System.Collections.Generic;

namespace MiniGame.PenguinWars.Battle
{
    /// <summary>ステージのギミックのうち、陣営ごとに変わるもの（敵の城の砲・編成制限）</summary>
    public partial class BattleWorld
    {
        /// <summary>
        /// 編成制限に当たるキャラか。出撃ボタンの暗さ（CanSpawn）と編成発表の暗さで同じ判定を使う。
        /// 編成そのものは書き換えず、そのステージだけ出せなくする
        /// </summary>
        public bool IsAllowedByRules(Side side, int slotIndex)
        {
            IReadOnlyList<UnitStats> deck = _decks[(int)side];
            if (deck == null || slotIndex < 0 || slotIndex >= deck.Count) return false;
            // 制限はステージの自分（左）だけ。敵は定義表どおりに湧かせる
            if (side != Side.Left) return true;

            return DeckRules.IsAllowed(_settings.DeckMaxUnitCost, _settings.DeckBannedRoles, deck[slotIndex]);
        }

        private EnemyCannonSettings CannonOf(Side side)
        {
            return side == Side.Right ? _settings.EnemyCannon : null;
        }

        private float CannonChargeTime(Side side)
        {
            EnemyCannonSettings enemy = CannonOf(side);
            return enemy != null ? enemy.ChargeTime : _settings.CannonChargeTime;
        }

        /// <summary>自城から砲が届く距離。検証用の bot（SimpleBot）も撃つ判断に使う</summary>
        internal float CannonReach(Side side)
        {
            EnemyCannonSettings enemy = CannonOf(side);
            return _settings.FieldLength * (enemy != null ? enemy.RangeRatio : _settings.CannonRangeRatio);
        }

        private int CannonDamage(Side side)
        {
            EnemyCannonSettings enemy = CannonOf(side);
            return enemy != null ? enemy.Damage : _settings.CannonDamage;
        }

        private void TickEnemyCannon()
        {
            CannonState cannon = GetCannon(Side.Right);
            if (!EnemyCannonAi.ShouldFire(_settings.EnemyCannon, cannon.IsReady, _units, Side.Right, _rightCastle.X, _settings.FieldLength)) return;

            if (cannon.TryFire()) FireCannon(Side.Right);
        }
    }
}
