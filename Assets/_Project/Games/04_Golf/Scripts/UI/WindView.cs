using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.Golf
{
    /// <summary>
    /// 風の向き（矢印）と強さ（m）の表示（§9.4）。カメラは回さないので、画面上の向きがそのままコース上の向きになる。
    /// </summary>
    public class WindView : MonoBehaviour
    {
        [SerializeField] private HoleLoader _holeLoader;

        [Tooltip("上向きの矢印。風が吹いていく向きへ回す")]
        [SerializeField] private RectTransform _arrow;

        [SerializeField] private Text _strengthLabel;

        // 矢印は上（+Y）向きに作っているが、Wind の角度は右（+X）が0度
        private const float ArrowBaseDegrees = 90f;

        // ホールは HoleLoader の Awake で読み込まれ、Phase 4 では途中で変わらない
        private void Start()
        {
            Wind wind = _holeLoader.CurrentWind;
            float degrees = Mathf.Atan2(wind.Direction.Y, wind.Direction.X) * Mathf.Rad2Deg;
            _arrow.localRotation = Quaternion.Euler(0f, 0f, degrees - ArrowBaseDegrees);
            _strengthLabel.text = $"風 {Mathf.RoundToInt(wind.Strength)}m";
        }
    }
}
