using MiniGame.PenguinWars.Battle;
using UnityEditor;
using UnityEngine;

namespace MiniGame.PenguinWars.Editor
{
    /// <summary>
    /// 全キャラ×陣営2色×5コマのドット絵と、城・背景・演出の絵をコードから生成する（仕様書 §7.2）。
    /// 外部素材に依存せず同じ絵をいつでも作り直せるようにするため。生成した PNG とキャラデータへの参照はコミットする
    /// </summary>
    public static class PenguinArtGenerator
    {
        private const string UnitSpriteDirectory = PenguinSpriteWriter.SpriteDirectory + "/Units";
        private const string LogPrefix = "[PenguinArtGenerator]";

        // 原点を足元の中央にして、UnitView は X を写すだけで地面に立つようにする
        private static readonly Vector2 FootPivot = new Vector2(0.5f, 0f);

        /// <summary>
        /// SceneBuilder から呼ぶ。見た目の定義を変えたときも Rebuild だけで反映されるよう、毎回すべて描き直す
        /// （同じ定義からは同じ PNG ができるので、見た目を変えていなければ差分は出ない）
        /// </summary>
        public static void Regenerate(PenguinUnitCatalog catalog)
        {
            int count = 0;
            foreach (PenguinUnitData unit in catalog.Units)
            {
                if (unit != null && GenerateUnit(unit)) count++;
            }
            FieldArtGenerator.GenerateAll();
            EffectArtGenerator.GenerateAll();

            AssetDatabase.SaveAssets();
            Debug.Log($"{LogPrefix} ドット絵を生成しました（{count}体＋城・背景・演出）: {PenguinSpriteWriter.SpriteDirectory}");
        }

        private static bool GenerateUnit(PenguinUnitData unit)
        {
            if (!IsValidLook(unit)) return false;

            var so = new SerializedObject(unit);
            WriteSide(so.FindProperty("_leftSprites"), unit, Side.Left);
            WriteSide(so.FindProperty("_rightSprites"), unit, Side.Right);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(unit);
            return true;
        }

        /// <summary>ID の打ち間違いはここで止める（パーツの方は Composer が警告を出して描かずに進む）</summary>
        private static bool IsValidLook(PenguinUnitData unit)
        {
            if (!PenguinBodyPatterns.Shapes.ContainsKey(unit.Look.Body))
            {
                Debug.LogError($"{LogPrefix} No.{unit.No} の体の形 '{unit.Look.Body}' がありません");
                return false;
            }
            if (!PenguinPalette.HasBodyColor(unit.Look.BodyColor))
            {
                Debug.LogError($"{LogPrefix} No.{unit.No} の体色 '{unit.Look.BodyColor}' がありません");
                return false;
            }
            return true;
        }

        private static void WriteSide(SerializedProperty spriteSet, PenguinUnitData unit, Side side)
        {
            float pixelsPerUnit = (float)PenguinSpriteWriter.PixelsPerUnit / unit.Look.Scale;
            SerializedProperty frames = spriteSet.FindPropertyRelative("_frames");
            frames.arraySize = UnitSpriteSet.FrameCount;

            int headRow = 0;
            for (int i = 0; i < UnitSpriteSet.FrameCount; i++)
            {
                var frame = (PenguinFrame)i;
                Color32[] pixels = PenguinFrameComposer.Compose(unit.Look, side, frame);
                string path = $"{UnitSpriteDirectory}/Unit_{unit.No:000}_{side}_{frame}.png";
                int size = PenguinFrameComposer.CanvasSize;
                frames.GetArrayElementAtIndex(i).objectReferenceValue =
                    PenguinSpriteWriter.Save(path, pixels, size, size, pixelsPerUnit, FootPivot);

                if (frame == PenguinFrame.Walk0) headRow = TopOpaqueRow(pixels, size);
            }

            spriteSet.FindPropertyRelative("_headHeight").floatValue = (headRow + 1) / pixelsPerUnit;
        }

        private static int TopOpaqueRow(Color32[] pixels, int size)
        {
            for (int i = pixels.Length - 1; i >= 0; i--)
            {
                if (pixels[i].a > 0) return i / size;
            }
            return 0;
        }
    }
}
