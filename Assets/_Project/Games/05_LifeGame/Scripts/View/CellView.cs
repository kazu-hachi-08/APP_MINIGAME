using UnityEngine;

namespace MiniGame.LifeGame
{
    /// <summary>1マスの見た目（テーマの色に染めたマス＋種類のアイコン＋金額か名前）</summary>
    public class CellView : MonoBehaviour
    {
        // 道（0）の上にマス、その上にアイコンと文字
        private const int BodySortingOrder = 1;
        private const int IconSortingOrder = 2;
        private const int LabelSortingOrder = 3;

        /// <summary>全マス共通の絵と大きさ（BoardView の設定をまとめて渡す）</summary>
        public struct Style
        {
            public float Size;
            public Sprite Tile;
            public float IconSize;
            public float IconY;
            public Font Font;
            public int FontSize;
            public float CharacterSize;
            public float LabelY;
        }

        public static CellView Create(Transform parent, LifeCell cell, Vector2 position, Color color, Sprite icon, Style style)
        {
            var obj = new GameObject($"Cell_{cell.Index}_{cell.Type}");
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = position;

            var view = obj.AddComponent<CellView>();
            view.CreateSprite("Body", style.Tile, color, Vector2.zero, style.Size, BodySortingOrder);
            view.CreateSprite("Icon", icon, Color.white, new Vector2(0f, style.IconY), style.IconSize, IconSortingOrder);
            view.CreateLabel(LifeTexts.CellLabel(cell), style);
            return view;
        }

        /// <summary>絵の解像度に関係なく、横幅が size（ワールド単位）になるよう拡大する（手描きに差し替えても大きさが変わらないように）</summary>
        private void CreateSprite(string name, Sprite sprite, Color color, Vector2 position, float size, int sortingOrder)
        {
            var spriteObj = new GameObject(name);
            spriteObj.transform.SetParent(transform, false);
            spriteObj.transform.localPosition = position;
            if (sprite != null)
            {
                float scale = size / sprite.bounds.size.x;
                spriteObj.transform.localScale = new Vector3(scale, scale, 1f);
            }

            var renderer = spriteObj.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
        }

        private void CreateLabel(string label, Style style)
        {
            var labelObj = new GameObject("Label");
            labelObj.transform.SetParent(transform, false);
            labelObj.transform.localPosition = new Vector2(0f, style.LabelY);

            var text = labelObj.AddComponent<TextMesh>();
            text.font = style.Font;
            text.text = label;
            text.fontSize = style.FontSize;
            text.characterSize = style.CharacterSize;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.color = Color.black;

            var renderer = labelObj.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = style.Font.material;
            renderer.sortingOrder = LabelSortingOrder;
        }
    }
}
