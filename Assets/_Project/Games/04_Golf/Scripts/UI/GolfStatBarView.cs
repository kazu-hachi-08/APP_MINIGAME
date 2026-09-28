using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.Golf
{
    /// <summary>
    /// 能力1行ぶんのマス表示（■■■□□）。倍率の数値そのものは見せず、マスの数で大まかな差だけ伝える。
    /// モルックの StatBarView のコピー。Common へ移すとモルックのシーンも作り直しになるので、ゴルフ側に持つ。
    /// </summary>
    public class GolfStatBarView : MonoBehaviour
    {
        [SerializeField] private Image[] _cells;

        [Tooltip("この倍率で1マス")]
        [SerializeField] private float _minMultiplier = 0.8f;

        [Tooltip("この倍率で全マス")]
        [SerializeField] private float _maxMultiplier = 1.3f;

        [SerializeField] private Color _filledColor = new Color(1f, 0.8f, 0.25f);
        [SerializeField] private Color _emptyColor = new Color(0.25f, 0.27f, 0.32f);

        public void Show(float multiplier)
        {
            int filled = ToCellCount(multiplier);
            for (int i = 0; i < _cells.Length; i++)
            {
                _cells[i].color = i < filled ? _filledColor : _emptyColor;
            }
        }

        /// <summary>最低でも1マスは塗る。0マスだと「その能力が無い」ように見えてしまうため</summary>
        private int ToCellCount(float multiplier)
        {
            float t = Mathf.InverseLerp(_minMultiplier, _maxMultiplier, multiplier);
            return 1 + Mathf.RoundToInt(t * (_cells.Length - 1));
        }
    }
}
