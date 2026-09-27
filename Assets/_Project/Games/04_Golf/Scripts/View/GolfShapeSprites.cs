using UnityEngine;

namespace MiniGame.Golf
{
    /// <summary>
    /// 単純な図形スプライトを実行時に作る（§5：MVPは単純な図形で表現）。
    /// 画像アセットを増やさずに済むので、2人開発でのアセット競合も起きない。
    /// モルックの ShapeSprites と同じ作りだが、ミニゲーム同士を依存させないためゴルフ側にも持つ。
    /// 1ワールド単位の大きさで、中心がピボット。
    /// </summary>
    public static class GolfShapeSprites
    {
        private const int CircleResolution = 64;

        private static Sprite _circle;
        private static Sprite _square;

        public static Sprite Square
        {
            get
            {
                if (_square == null)
                {
                    // 1ピクセルの白を1ユニットに引き伸ばす
                    var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                    texture.SetPixel(0, 0, Color.white);
                    texture.Apply();
                    _square = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
                }

                return _square;
            }
        }

        public static Sprite Circle
        {
            get
            {
                if (_circle == null)
                {
                    _circle = Sprite.Create(CreateCircleTexture(), new Rect(0, 0, CircleResolution, CircleResolution),
                        new Vector2(0.5f, 0.5f), CircleResolution);
                }

                return _circle;
            }
        }

        private static Texture2D CreateCircleTexture()
        {
            var texture = new Texture2D(CircleResolution, CircleResolution, TextureFormat.RGBA32, false);
            float radius = CircleResolution * 0.5f;

            for (int y = 0; y < CircleResolution; y++)
            {
                for (int x = 0; x < CircleResolution; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(radius, radius));
                    // 縁を1ピクセルぼかしてギザギザを目立たなくする
                    float alpha = Mathf.Clamp01(radius - distance);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            return texture;
        }
    }
}
