using UnityEngine;

namespace MiniGame.PenguinWars
{
    /// <summary>出撃時に城の前に出る煙（仕様書 §9）。ふくらみながら少し昇って消える</summary>
    public class SpawnSmokeEffect : PooledEffect
    {
        [SerializeField] private SpriteRenderer _renderer;
        [SerializeField] private float _startScale = 0.6f;
        [SerializeField] private float _endScale = 1.5f;
        [SerializeField] private float _riseDistance = 0.4f;

        private Vector3 _origin;

        public void Play(Vector3 position)
        {
            _origin = position;
            // 同じ場所に続けて出ても同じ形に見えないよう、向きをばらす
            _renderer.flipX = Random.value < 0.5f;
            Begin(position);
        }

        protected override void Animate(float rate)
        {
            float easeOut = 1f - (1f - rate) * (1f - rate);
            transform.localScale = Vector3.one * Mathf.Lerp(_startScale, _endScale, easeOut);
            transform.position = _origin + Vector3.up * (_riseDistance * easeOut);
            SetAlpha(_renderer, 1f - rate * rate);
        }
    }
}
