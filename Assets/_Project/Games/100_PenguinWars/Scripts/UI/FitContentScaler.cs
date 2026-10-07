using UnityEngine;

namespace MiniGame.PenguinWars
{
    /// <summary>
    /// 固定サイズで並べた中身（_content）を、この枠（SafeArea）に収まるまで縦横同じ比率で縮める。
    /// 横長のスマホは Canvas の高さが 1080 より低くなり、さらにノッチの分 SafeArea が狭くなるので、
    /// 端に寄せた置き方だとボタン同士が重なってしまうため
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class FitContentScaler : MonoBehaviour
    {
        [SerializeField] private RectTransform _content;

        private void OnEnable() => Apply();

        // SafeAreaFitter が枠を変えたとき（回転・分割表示など）に呼ばれる
        private void OnRectTransformDimensionsChange() => Apply();

        private void Apply()
        {
            if (_content == null) return;

            Vector2 area = ((RectTransform)transform).rect.size;
            Vector2 design = _content.sizeDelta;
            if (design.x <= 0f || design.y <= 0f) return;

            // 広い画面では拡大せず、設計したままの大きさで見せる
            float scale = Mathf.Min(1f, area.x / design.x, area.y / design.y);
            _content.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
