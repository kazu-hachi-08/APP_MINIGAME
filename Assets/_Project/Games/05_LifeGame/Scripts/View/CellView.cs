using UnityEngine;

namespace MiniGame.LifeGame
{
    /// <summary>1マスの見た目（仮図形：色付きの四角＋文字）。フェーズ6でアイコンに置き換える</summary>
    public class CellView : MonoBehaviour
    {
        // 道（0）の上にマス、その上に文字
        private const int BodySortingOrder = 1;
        private const int LabelSortingOrder = 2;

        public static CellView Create(Transform parent, LifeCell cell, Vector2 position, float size, Color color,
            Font font, int fontSize, float characterSize)
        {
            var obj = new GameObject($"Cell_{cell.Index}_{cell.Type}");
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = position;

            var view = obj.AddComponent<CellView>();
            view.CreateBody(color, size);
            view.CreateLabel(LifeTexts.CellLabel(cell), font, fontSize, characterSize);
            return view;
        }

        // 四角と文字を別の子にして、マスの大きさを変えても文字の大きさが変わらないようにする
        private void CreateBody(Color color, float size)
        {
            var bodyObj = new GameObject("Body");
            bodyObj.transform.SetParent(transform, false);
            bodyObj.transform.localScale = new Vector3(size, size, 1f);

            var body = bodyObj.AddComponent<SpriteRenderer>();
            body.sprite = LifeShapes.Square;
            body.color = color;
            body.sortingOrder = BodySortingOrder;
        }

        private void CreateLabel(string label, Font font, int fontSize, float characterSize)
        {
            var labelObj = new GameObject("Label");
            labelObj.transform.SetParent(transform, false);

            var text = labelObj.AddComponent<TextMesh>();
            text.font = font;
            text.text = label;
            text.fontSize = fontSize;
            text.characterSize = characterSize;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.color = Color.black;

            var renderer = labelObj.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = font.material;
            renderer.sortingOrder = LabelSortingOrder;
        }
    }
}
