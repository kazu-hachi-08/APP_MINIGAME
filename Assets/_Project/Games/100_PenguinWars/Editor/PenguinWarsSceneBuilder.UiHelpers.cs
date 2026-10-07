using MiniGame.Editor;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.PenguinWars.Editor
{
    /// <summary>どの画面の partial からも使う UI 生成のヘルパー（文字・絵・ボタン・全画面パネル）と、アンカー・共通の色</summary>
    public static partial class PenguinWarsSceneBuilder
    {
        private static readonly Vector2 TopLeftAnchor = new Vector2(0f, 1f);
        private static readonly Vector2 TopCenterAnchor = new Vector2(0.5f, 1f);
        private static readonly Vector2 TopRightAnchor = new Vector2(1f, 1f);
        private static readonly Vector2 CenterAnchor = new Vector2(0.5f, 0.5f);
        private static readonly Vector2 BottomLeftAnchor = new Vector2(0f, 0f);
        private static readonly Vector2 BottomCenterAnchor = new Vector2(0.5f, 0f);
        private static readonly Vector2 BottomRightAnchor = new Vector2(1f, 0f);

        // 雪の白い地面や空の上でも文字が読めるように縁取る
        private static readonly Vector2 TextOutline = new Vector2(3f, -3f);
        private static readonly Color TextOutlineColor = new Color(0f, 0f, 0f, 0.8f);

        // 画面の上に重ねるダイアログ（ずかんの詳細・設定・ステージ詳細・リザルト）で揃える、後ろを暗くする幕と箱の色
        private static readonly Color DialogDimColor = new Color(0f, 0f, 0f, 0.7f);
        private static readonly Color DialogBoxColor = new Color(0.12f, 0.14f, 0.18f);

        private static Text CreateText(Transform parent, string name, int fontSize,
            Vector2 anchor, Vector2 anchoredPosition, Vector2 size, Color color)
        {
            var obj = UIDialogBuilder.CreateUIObject(name, parent);
            RectTransform rect = obj.GetComponent<RectTransform>();
            SetAnchor(rect, anchor, anchoredPosition);
            rect.sizeDelta = size;

            var text = obj.AddComponent<Text>();
            text.fontSize = fontSize;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = color;
            // 既定の Truncate だと、1行の高さが枠を超えた瞬間にその行ごと描画されなくなるため
            text.verticalOverflow = VerticalWrapMode.Overflow;
            // 文字の上でもドラッグで戦場をスクロールできるようにする
            text.raycastTarget = false;

            var outline = obj.AddComponent<Outline>();
            outline.effectDistance = TextOutline;
            outline.effectColor = TextOutlineColor;
            return text;
        }

        /// <summary>
        /// 長い名前（こおりのじょおうペンギン など）や「能力: 遠距離キラー」でも枠からはみ出さないよう、今の大きさから minSize まで縮めて収める。
        /// Overflow のままだと縮まないので Truncate に戻す
        /// </summary>
        private static void FitText(Text text, int minSize)
        {
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = minSize;
            text.resizeTextMaxSize = text.fontSize;
        }

        /// <summary>アンカー・ピボットを同じ点に揃えて、その点からの位置で置く</summary>
        private static void SetAnchor(RectTransform rect, Vector2 anchor, Vector2 anchoredPosition)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = anchoredPosition;
        }

        /// <summary>押せる部分はボタン本体だけにする（上に重ねた絵がタップを奪わないように raycastTarget を切る）</summary>
        private static Image CreateImage(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size, Color color)
        {
            GameObject obj = UIDialogBuilder.CreateUIObject(name, parent);
            RectTransform rect = obj.GetComponent<RectTransform>();
            SetAnchor(rect, anchor, position);
            rect.sizeDelta = size;
            var image = obj.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static UnitSpriteAnimator CreateAnimatedIcon(Transform parent, Vector2 anchor, Vector2 position, Vector2 size)
        {
            Image image = CreateImage(parent, "Icon", anchor, position, size, Color.white);
            // 大型ほど絵が大きいので、枠に合わせて縮めて縦横比だけ保つ
            image.preserveAspect = true;
            var animator = image.gameObject.AddComponent<UnitSpriteAnimator>();
            SetRefs(animator, ("_image", image));
            return animator;
        }

        private static Text CreateDetailText(Transform parent, string name, int fontSize, Vector2 position, Vector2 size, Color color)
        {
            Text text = CreateText(parent, name, fontSize, CenterAnchor, position, size, color);
            text.alignment = TextAnchor.MiddleLeft;
            return text;
        }

        /// <param name="color">省略するとタイトルのサブボタンと同じ色</param>
        private static Button CreateAnchoredButton(Transform parent, string name, string label, Vector2 size, Vector2 anchor, Vector2 position,
            Color? color = null)
        {
            GameObject buttonObj = UIDialogBuilder.CreateButton(parent, name, label, size.x, size.y, color ?? TitleSubButtonColor);
            SetAnchor(buttonObj.GetComponent<RectTransform>(), anchor, position);
            buttonObj.GetComponentInChildren<Text>().fontSize = TitleButtonFontSize;
            return buttonObj.GetComponent<Button>();
        }

        /// <summary>中央基準で置くボタン（タイトル・ステージ詳細・リザルト）</summary>
        private static Button CreateTitleButton(Transform parent, string name, string label, Vector2 size, Vector2 position, Color color)
        {
            return CreateAnchoredButton(parent, name, label, size, CenterAnchor, position, color);
        }

        /// <summary>背景は raycastTarget を残し、表示中に下のボタンや戦場のドラッグが効かないようにする</summary>
        private static GameObject CreateFullScreenPanel(Transform parent, string name, Color color)
        {
            GameObject panelObj = UIDialogBuilder.CreateUIObject(name, parent);
            UIDialogBuilder.SetStretchAll(panelObj.GetComponent<RectTransform>());
            panelObj.AddComponent<Image>().color = color;
            return panelObj;
        }

        private static void SerializedArray(UnityEditor.SerializedObject so, string name, Object[] values)
        {
            UnityEditor.SerializedProperty array = FindPropertyOrLog(so, name);
            if (array == null) return;

            array.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
        }
    }
}
