using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.LifeGame
{
    /// <summary>
    /// コマの上に「+180」「結婚！」などの文字を浮かべて消す（所持金の増減や給料日・結婚などの演出）。
    /// 待たずに進められるよう、出した後はここで勝手に動かして消す。
    /// </summary>
    public class BoardEffects : MonoBehaviour
    {
        // コマ（10〜）より手前
        private const int ShadowSortingOrder = 30;
        private const int TextSortingOrder = 31;

        [Tooltip("文字のフォントをこの Text から借りる（ブラウザ版で日本語フォントに差し替わった後のものを使うため）")]
        [SerializeField] private Text _fontSource;
        [SerializeField] private int _fontSize = 64;
        [SerializeField] private float _characterSize = 0.045f;
        [Tooltip("コマの中心からどれだけ上に出すか")]
        [SerializeField] private float _startHeight = 0.45f;
        [SerializeField] private float _riseDistance = 0.8f;
        [SerializeField] private float _duration = 1.3f;
        [Tooltip("影をずらす量。どのテーマの背景の上でも文字が読めるようにする")]
        [SerializeField] private Vector2 _shadowOffset = new Vector2(0.03f, -0.03f);

        public void Popup(Vector2 position, string text, Color color)
        {
            var root = new GameObject("Popup").transform;
            root.SetParent(transform, false);
            root.position = position + Vector2.up * _startHeight;

            TextMesh shadow = CreateText(root, text, Color.black, _shadowOffset, ShadowSortingOrder);
            TextMesh main = CreateText(root, text, color, Vector2.zero, TextSortingOrder);
            StartCoroutine(Rise(root, main, shadow));
        }

        private TextMesh CreateText(Transform parent, string text, Color color, Vector2 offset, int sortingOrder)
        {
            var obj = new GameObject("Text");
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = offset;

            var mesh = obj.AddComponent<TextMesh>();
            mesh.font = _fontSource.font;
            mesh.text = text;
            mesh.fontSize = _fontSize;
            mesh.characterSize = _characterSize;
            mesh.fontStyle = FontStyle.Bold;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.color = color;

            var renderer = obj.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = _fontSource.font.material;
            renderer.sortingOrder = sortingOrder;
            return mesh;
        }

        /// <summary>減速しながら上がり、後半で薄くして消える</summary>
        private IEnumerator Rise(Transform root, TextMesh main, TextMesh shadow)
        {
            const float fadeStart = 0.6f;

            Vector3 from = root.position;
            Color mainColor = main.color;
            Color shadowColor = shadow.color;
            for (float t = 0f; t < _duration; t += Time.deltaTime)
            {
                float rate = t / _duration;
                root.position = from + Vector3.up * (_riseDistance * (1f - (1f - rate) * (1f - rate)));

                float alpha = 1f - Mathf.Clamp01((rate - fadeStart) / (1f - fadeStart));
                main.color = new Color(mainColor.r, mainColor.g, mainColor.b, alpha);
                shadow.color = new Color(shadowColor.r, shadowColor.g, shadowColor.b, alpha);
                yield return null;
            }

            Destroy(root.gameObject);
        }
    }
}
