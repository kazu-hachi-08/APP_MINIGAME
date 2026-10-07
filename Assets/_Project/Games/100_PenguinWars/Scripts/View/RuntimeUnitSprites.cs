using MiniGame.PenguinWars.Art;
using MiniGame.PenguinWars.Battle;
using UnityEngine;

namespace MiniGame.PenguinWars
{
    /// <summary>
    /// 見た目ID から1陣営分の絵（5コマ）を実行時に作る。じぶんペンギンはパーツの組み合わせが多すぎて PNG を用意できないため。
    /// PPU とピボットは PenguinArtGenerator（エディタの PNG 生成）と同じにして、既存キャラと同じ大きさ・立ち位置で描かれるようにする
    /// </summary>
    public static class RuntimeUnitSprites
    {
        // PenguinSpriteWriter.PixelsPerUnit と同じ値（あちらは Editor 専用で参照できない）
        private const float PixelsPerUnit = 16f;
        private static readonly Vector2 FootPivot = new Vector2(0.5f, 0f);

        public static UnitSpriteSet Build(PenguinLook look, Side side)
        {
            float pixelsPerUnit = PixelsPerUnit / look.Scale;
            var frames = new Sprite[UnitSpriteSet.FrameCount];
            int headRow = 0;
            for (int i = 0; i < frames.Length; i++)
            {
                var frame = (PenguinFrame)i;
                Color32[] pixels = PenguinFrameComposer.Compose(look, side, frame);
                frames[i] = CreateSprite(pixels, pixelsPerUnit, $"{side}_{frame}");
                if (frame == PenguinFrame.Walk0) headRow = PenguinFrameComposer.TopOpaqueRow(pixels);
            }
            return new UnitSpriteSet(frames, (headRow + 1) / pixelsPerUnit);
        }

        /// <summary>作り直すたびに捨てないとテクスチャがたまり続ける（作成画面では◀▶のたびに作るため）</summary>
        public static void Destroy(UnitSpriteSet sprites)
        {
            for (int i = 0; i < UnitSpriteSet.FrameCount; i++)
            {
                Sprite sprite = sprites.Get((PenguinFrame)i);
                if (sprite == null) continue;

                Object.Destroy(sprite.texture);
                Object.Destroy(sprite);
            }
        }

        private static Sprite CreateSprite(Color32[] pixels, float pixelsPerUnit, string name)
        {
            int size = PenguinFrameComposer.CanvasSize;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point, // ドット絵をぼかさない
                wrapMode = TextureWrapMode.Clamp,
                name = name,
            };
            texture.SetPixels32(pixels);
            texture.Apply(false, true); // CPU 側のコピーは要らないので捨ててメモリを減らす

            var sprite = Sprite.Create(texture, new Rect(0, 0, size, size), FootPivot, pixelsPerUnit, 0, SpriteMeshType.FullRect);
            sprite.name = name;
            return sprite;
        }
    }
}
