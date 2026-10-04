using MiniGame.PenguinWars.Battle;
using UnityEngine;

namespace MiniGame.PenguinWars
{
    /// <summary>
    /// ユニット1体の表示。状態は BattleWorld が持つので、ここは UnitState を受けてコマと色を選ぶだけ。
    /// 絵は右向きで作ってあり、右陣営は左右反転して左向きにする
    /// </summary>
    public class UnitView : MonoBehaviour
    {
        // Id ごとに歩きのコマをずらし、同時に出たユニットが揃って足踏みしないようにする
        private const float WalkPhasePerId = 0.37f;

        [SerializeField] private SpriteRenderer _body;
        [SerializeField] private HpBarView _hpBar;
        [Tooltip("歩きの1コマの秒数")]
        [SerializeField] private float _walkFrameSeconds = 0.2f;
        [Tooltip("攻撃が出た後、振り下ろしのコマを見せておく秒数。過ぎたら立ち姿で次の攻撃を待つ")]
        [SerializeField] private float _strikeHoldSeconds = 0.25f;
        [Tooltip("HPバーを頭のてっぺんからどれだけ上に置くか")]
        [SerializeField] private float _hpBarMargin = 0.2f;
        [Tooltip("止められている間の色。状態異常が見て分かるようにする仮の表現（Phase 7 で演出に置き換える）")]
        [SerializeField] private Color _frozenTint = new Color(0.55f, 0.85f, 1f);
        [Tooltip("遅くされている間の色")]
        [SerializeField] private Color _slowedTint = new Color(0.6f, 0.6f, 0.6f);
        [Tooltip("状態異常の色に寄せる割合")]
        [SerializeField, Range(0f, 1f)] private float _statusTintAmount = 0.7f;

        private UnitSpriteSet _sprites;

        /// <summary>UnitViewPool が View を別のユニットに使い回すたびに呼ぶ</summary>
        public void SetSprites(UnitSpriteSet sprites)
        {
            _sprites = sprites;
            if (sprites == null) return;

            // 大型キャラは絵が大きいので、HPバーも頭の上まで上げる
            Vector3 hpBarPosition = _hpBar.transform.localPosition;
            hpBarPosition.y = sprites.HeadHeight + _hpBarMargin;
            _hpBar.transform.localPosition = hpBarPosition;
        }

        public void Apply(UnitState state)
        {
            // ユニットの原点は足元。城と同じく地面の上端 Y=0 に立たせる。
            // ノックバック中は BattleWorld が X を少しずつ戻すので、位置を写すだけで後ろに飛ばされて見える
            transform.localPosition = new Vector3(state.X, 0f, 0f);
            if (_sprites != null) _body.sprite = _sprites.Get(SelectFrame(state));
            _body.flipX = state.Side == Side.Right;
            _body.color = ResolveTint(state);
            _hpBar.SetRatio(state.HpRatio);
        }

        private PenguinFrame SelectFrame(UnitState state)
        {
            switch (state.Action)
            {
                case UnitAction.Windup: return PenguinFrame.AttackWindup;
                case UnitAction.Cooldown: return IsShowingStrike(state) ? PenguinFrame.AttackStrike : PenguinFrame.Walk0;
                case UnitAction.Knockback: return PenguinFrame.Knockback;
            }

            // 止められている間は足踏みもさせない
            if (state.Status.IsFrozen) return PenguinFrame.Walk0;
            int step = Mathf.FloorToInt(Time.time / _walkFrameSeconds + state.Id * WalkPhasePerId);
            return step % 2 == 0 ? PenguinFrame.Walk0 : PenguinFrame.Walk1;
        }

        /// <summary>攻撃後の硬直は ActionTimer が残り時間なので、全体の長さから引いて「攻撃からの経過」に直す</summary>
        private bool IsShowingStrike(UnitState state)
        {
            float cooldownLength = Mathf.Max(0f, state.Stats.AttackInterval - state.Stats.Windup);
            return cooldownLength - state.ActionTimer < _strikeHoldSeconds;
        }

        /// <summary>止まる（青）を遅い（灰）より優先する。止まっている方が戦況への影響が大きく、見落とされたくないため</summary>
        private Color ResolveTint(UnitState state)
        {
            if (state.Status.IsFrozen) return Color.Lerp(Color.white, _frozenTint, _statusTintAmount);
            if (state.Status.IsSlowed) return Color.Lerp(Color.white, _slowedTint, _statusTintAmount);
            return Color.white;
        }
    }
}
