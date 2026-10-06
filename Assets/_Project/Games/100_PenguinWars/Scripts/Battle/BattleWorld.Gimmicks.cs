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
            if (!IsValidSlot(side, slotIndex)) return false;
            // 制限はステージの自分（左）だけ。敵は定義表どおりに湧かせる
            if (side != Side.Left) return true;

            return DeckRules.IsAllowed(_settings.DeckMaxUnitCost, _settings.DeckBannedRoles, _decks[(int)side][slotIndex]);
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

        /// <summary>自城から砲が届く距離</summary>
        internal float CannonReach(Side side)
        {
            EnemyCannonSettings enemy = CannonOf(side);
            return _settings.FieldLength * (enemy != null ? enemy.RangeRatio : _settings.CannonRangeRatio);
        }

        /// <summary>side の砲が unit に届くか（陣営・生死は見ない）。実際の発射と検証用の bot（SimpleBot）の撃つ判断で同じ範囲を使うため</summary>
        internal bool IsInCannonReach(Side side, UnitState unit)
        {
            return (unit.X - GetCastle(side).X) * side.Forward() <= CannonReach(side);
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
