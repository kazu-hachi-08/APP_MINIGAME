using UnityEngine;

namespace MiniGame.Molkky
{
    /// <summary>
    /// 仮素材用の単純な図形スプライトを実行時に作る（Phase 8 でビジュアルを置き換えるまでの間）。
    /// 画像アセットを増やさずに済むので、2人開発でのアセット競合も起きない。
    /// どちらも1ワールド単位の大きさで、中心がピボット。
    /// </summary>
    public static class ShapeSprites
    {
        private const int CircleResolution = 64;

        private static Sprite _square;
        private static Sprite _circle;

        public static Sprite Square
        {
            get
            {
                if (_square == null)
                {
                    Texture2D texture = Texture2D.whiteTexture;
                    _square = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
                        new Vector2(0.5f, 0.5f), texture.width);
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
