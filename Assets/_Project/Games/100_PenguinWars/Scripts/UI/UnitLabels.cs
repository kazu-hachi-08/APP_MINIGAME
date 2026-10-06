using System.Collections.Generic;
using System.Text;
using MiniGame.PenguinWars.Battle;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.PenguinWars
{
    /// <summary>
    /// キャラ名・役割・特殊能力の画面に出す名前（仕様書 §5.3・§5.5）と、一覧に出す立ち姿。enum の並びと表示名を1か所で対応させる。
    /// 名前と絵はどの画面でも「カタログに無い No（ビルドのバージョン違い）なら No だけ出す」決まりなので、ここにまとめる
    /// </summary>
    public static class UnitLabels
    {
        private const string NoAbility = "能力なし";
        private const string AbilitySeparator = "・";
        private const string RangeArea = "範囲";
        private const string RangeSingle = "単体";
        private const string RoleRangeSeparator = "・";

        /// <param name="data">カタログに無い No なら null</param>
        public static string Name(PenguinUnitData data, int unitNo) => data != null ? data.DisplayName : $"No.{unitNo}";

        /// <summary>立ち姿を出す。data が null なら絵を消す（前のキャラの絵が残らないように）</summary>
        /// <param name="side">自分のキャラは Left（青）、敵は Right（赤）で戦場と同じ色にする</param>
        public static void SetIcon(Image image, PenguinUnitData data, Side side)
        {
            Sprite icon = data != null ? data.GetSprites(side).Icon : null;
            image.sprite = icon;
            image.enabled = icon != null;
        }

        public static string Role(UnitRole role)
        {
            switch (role)
            {
                case UnitRole.Wall: return "壁";
                case UnitRole.Attacker: return "アタッカー";
                case UnitRole.Ranged: return "遠距離";
                case UnitRole.Disruptor: return "妨害";
                case UnitRole.Large: return "大型";
                default: return role.ToString();
            }
        }

        public static string AttackRange(bool isAreaAttack) => isAreaAttack ? RangeArea : RangeSingle;

        /// <summary>「アタッカー・単体」のように役割と攻撃範囲をつなげる（ドラフトのカード・ずかんの詳細）</summary>
        public static string RoleAndRange(UnitStats stats) => $"{Role(stats.Role)}{RoleRangeSeparator}{AttackRange(stats.IsAreaAttack)}";

        public static string Ability(UnitAbilityType type)
        {
            switch (type)
            {
                case UnitAbilityType.Knockback: return "ふっとばす";
                case UnitAbilityType.Freeze: return "止める";
                case UnitAbilityType.Slow: return "遅くする";
                case UnitAbilityType.CastleKiller: return "城キラー";
                case UnitAbilityType.Steadfast: return "ふんばる";
                case UnitAbilityType.LargeKiller: return "大型キラー";
                case UnitAbilityType.RangedKiller: return "遠距離キラー";
                case UnitAbilityType.DisruptorKiller: return "妨害キラー";
                default: return type.ToString();
            }
        }

        /// <summary>「止める・遅くする」のようにつなげる。能力がなければ「能力なし」</summary>
        public static string Abilities(IReadOnlyList<UnitAbility> abilities)
        {
            if (abilities == null || abilities.Count == 0) return NoAbility;

            var builder = new StringBuilder();
            foreach (UnitAbility ability in abilities)
            {
                if (builder.Length > 0) builder.Append(AbilitySeparator);
                builder.Append(Ability(ability.Type));
            }
            return builder.ToString();
        }
    }
}
