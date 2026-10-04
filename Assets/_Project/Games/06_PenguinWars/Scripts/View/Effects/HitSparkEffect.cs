using UnityEngine;

namespace MiniGame.PenguinWars
{
    /// <summary>攻撃ヒットの小さい白いエフェクト（仕様書 §9）。一瞬大きく出てすぐしぼむ</summary>
    public class HitSparkEffect : PooledEffect
    {
        [SerializeField] private SpriteRenderer _renderer;
        [SerializeField] private float _peakScale = 0.8f;
        [Tooltip("ここまでで最大の大きさになり、残りでしぼむ")]
        [SerializeField, Range(0.05f, 0.9f)] private float _peakRate = 0.25f;

        public void Play(Vector3 position)
        {
            // 連続ヒットで同じ絵が重なっても動いて見えるよう、角度をばらす
            transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 90f));
            Begin(position);
        }

        protected override void Animate(float rate)
        {
            float scale = rate < _peakRate
                ? rate / _peakRate
                : 1f - (rate - _peakRate) / (1f - _peakRate);
            transform.localScale = Vector3.one * (_peakScale * scale);
            SetAlpha(_renderer, rate < _peakRate ? 1f : scale);
        }
    }
}
