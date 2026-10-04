using UnityEngine;

namespace MiniGame.PenguinWars
{
    /// <summary>
    /// HPバー用の単色の四角。1ワールド単位の白い四角を貼り、大きさは Transform のスケール、色は SpriteRenderer の色で決める。
    /// 画像アセットを作らずに済むよう実行時に生成する
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class PlaceholderSprite : MonoBehaviour
    {
        private static Sprite _sprite;

        private void Awake()
        {
            GetComponent<SpriteRenderer>().sprite = GetSprite();
        }

        private static Sprite GetSprite()
        {
            if (_sprite != null) return _sprite;

            var texture = new Texture2D(1, 1) { filterMode = FilterMode.Point };
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            _sprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            return _sprite;
        }
    }
}
