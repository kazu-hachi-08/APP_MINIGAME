using MiniGame.PenguinWars.Battle;
using UnityEngine;

namespace MiniGame.PenguinWars
{
    /// <summary>
    /// ユニット1体の表示。ドット絵（Phase 6）ができるまでは陣営色の四角＋HPバー。
    /// 状態は BattleWorld が持つので、ここは UnitState を受けて描くだけ
    /// </summary>
    public class UnitView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _body;
        [SerializeField] private HpBarView _hpBar;
        [SerializeField] private Color _leftColor = new Color(0.25f, 0.45f, 0.95f);
        [SerializeField] private Color _rightColor = new Color(0.9f, 0.25f, 0.25f);
        [Tooltip("攻撃発生待ちの間だけ白に寄せる割合。アニメが入るまで、殴っているのが見えるようにする仮の表現")]
        [SerializeField, Range(0f, 1f)] private float _windupWhiten = 0.6f;
        [Tooltip("止められている間の色。状態異常が見て分かるようにする仮の表現（Phase 7 で演出に置き換える）")]
        [SerializeField] private Color _frozenTint = new Color(0.55f, 0.85f, 1f);
        [Tooltip("遅くされている間の色")]
        [SerializeField] private Color _slowedTint = new Color(0.6f, 0.6f, 0.6f);
        [Tooltip("状態異常の色に寄せる割合")]
        [SerializeField, Range(0f, 1f)] private float _statusTintAmount = 0.7f;

        public void Apply(UnitState state)
        {
            // ユニットの原点は足元。城と同じく地面の上端 Y=0 に立たせる。
            // ノックバック中は BattleWorld が X を少しずつ戻すので、位置を写すだけで後ろに跳ねて見える
            transform.localPosition = new Vector3(state.X, 0f, 0f);
            _body.color = ResolveColor(state);
            _hpBar.SetRatio(state.HpRatio);
        }

        /// <summary>止まる（青）を遅い（灰）より優先する。止まっている方が戦況への影響が大きく、見落とされたくないため</summary>
        private Color ResolveColor(UnitState state)
        {
            Color color = state.Side == Side.Left ? _leftColor : _rightColor;
            if (state.Action == UnitAction.Windup) color = Color.Lerp(color, Color.white, _windupWhiten);

            if (state.Status.IsFrozen) return Color.Lerp(color, _frozenTint, _statusTintAmount);
            if (state.Status.IsSlowed) return Color.Lerp(color, _slowedTint, _statusTintAmount);
            return color;
        }
    }
}
