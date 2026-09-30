using UnityEngine;

namespace MiniGame.LifeGame
{
    /// <summary>
    /// 仮図形のスプライトを実行時に作る（フェーズ6でドット絵に置き換えるまでの代用）。
    /// 画像アセットを増やさないので、2人開発でのアセット競合も起きない。どちらも1ワールド単位の大きさで中心がピボット。
    /// </summary>
    public static class LifeShapes
    {
        private const int CircleResolution = 64;

        private static readonly Vector2 CenterPivot = new Vector2(0.5f, 0.5f);

        private static Sprite _square;
        private static Sprite _circle;

        public static Sprite Square
        {
            get
            {
                if (_square == null)
                {
                    Texture2D texture = Texture2D.whiteTexture;
                    _square = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), CenterPivot, texture.width);
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
                        CenterPivot, CircleResolution);
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
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(radius - distance)));
                }
            }

            texture.Apply();
            return texture;
        }
    }
}
