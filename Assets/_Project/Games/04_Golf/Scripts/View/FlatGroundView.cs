using UnityEngine;

namespace MiniGame.Golf
{
    /// <summary>
    /// Phase 1 用の平らな地面。コース（Tilemap）ができるまでの仮の地面で、Phase 2 で置き換える。
    /// 一色だとカメラがボールを追ったときに動いているのが分からないため、1タイル単位の市松模様にする。
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class FlatGroundView : MonoBehaviour
    {
        [Tooltip("地面の大きさ（タイル数）。x:左右 / y:奥行き")]
        [SerializeField] private Vector2Int _sizeInTiles = new Vector2Int(40, 120);

        [Tooltip("ボールを置く位置が地面の下端からどれだけ奥か（タイル数）")]
        [SerializeField] private int _originFromBottom = 10;

        // §5 のフェアウェイ色を明暗2色にした市松模様
        [SerializeField] private Color _lightColor = new Color(0.47f, 0.78f, 0.37f);
        [SerializeField] private Color _darkColor = new Color(0.41f, 0.71f, 0.32f);

        private void Awake()
        {
            GetComponent<SpriteRenderer>().sprite = CreateCheckerSprite();
            // 原点（ボールの初期位置）が左右中央・下端から _originFromBottom の位置に来るようにずらす
            transform.position = new Vector3(0f, _sizeInTiles.y * 0.5f - _originFromBottom, 0f);
        }

        /// <summary>1ピクセル＝1タイルのテクスチャを作り、Point フィルタで拡大してくっきりした市松模様にする</summary>
        private Sprite CreateCheckerSprite()
        {
            var texture = new Texture2D(_sizeInTiles.x, _sizeInTiles.y, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };

            for (int y = 0; y < _sizeInTiles.y; y++)
            {
                for (int x = 0; x < _sizeInTiles.x; x++)
                {
                    texture.SetPixel(x, y, (x + y) % 2 == 0 ? _lightColor : _darkColor);
                }
            }

            texture.Apply();
            const float pixelsPerUnit = 1f;
            return Sprite.Create(texture, new Rect(0, 0, _sizeInTiles.x, _sizeInTiles.y),
                new Vector2(0.5f, 0.5f), pixelsPerUnit);
        }
    }
}
