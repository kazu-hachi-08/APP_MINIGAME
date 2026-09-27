using UnityEngine;

namespace MiniGame.Golf
{
    /// <summary>
    /// カップ（黒い円）と旗（赤い四角）の表示（§5）。
    /// スプライトは実行時に作るので、プレハブには色と大きさだけを持たせる。
    /// </summary>
    public class CupView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _hole;
        [SerializeField] private SpriteRenderer _flag;

        private void Awake()
        {
            _hole.sprite = GolfShapeSprites.Circle;
            _flag.sprite = GolfShapeSprites.Square;
        }
    }
}
