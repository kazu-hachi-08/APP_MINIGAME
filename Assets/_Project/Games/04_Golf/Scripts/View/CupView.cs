using UnityEngine;

namespace MiniGame.Golf
{
    /// <summary>
    /// カップ（白い縁の穴）と旗（ポール＋三角の旗）の表示（Phase 10）。
    /// スプライトは実行時に作るので、プレハブには並び順だけを持たせる。
    /// Phase 9 までのプレハブは四角い旗用の位置・大きさ・色を持っているため、ここで上書きして揃える。
    /// </summary>
    public class CupView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _hole;
        [SerializeField] private SpriteRenderer _flag;

        [Tooltip("旗の大きさの倍率。1で高さ1.2ユニット")]
        [SerializeField] private float _flagScale = 1f;

        private void Awake()
        {
            _hole.sprite = GolfShapeSprites.Cup;
            _hole.color = Color.white;

            // 旗のピボットはポールの根元なので、カップの中心に置けばポールが穴に立って見える
            _flag.sprite = GolfShapeSprites.Flag;
            _flag.color = Color.white;
            _flag.transform.localPosition = Vector3.zero;
            _flag.transform.localScale = Vector3.one * _flagScale;
        }
    }
}
