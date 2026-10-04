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

        public void Apply(UnitState state)
        {
            // ユニットの原点は足元。城と同じく地面の上端 Y=0 に立たせる
            transform.localPosition = new Vector3(state.X, 0f, 0f);

            Color baseColor = state.Side == Side.Left ? _leftColor : _rightColor;
            _body.color = state.Action == UnitAction.Windup ? Color.Lerp(baseColor, Color.white, _windupWhiten) : baseColor;
            _hpBar.SetRatio(state.HpRatio);
        }
    }
}
