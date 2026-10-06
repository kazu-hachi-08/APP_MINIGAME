using UnityEngine;

namespace MiniGame.PenguinWars
{
    /// <summary>
    /// ペンギン砲の青いビーム（仕様書 §9）。城から届く先まで一気に伸び、その後細くなって消える。
    /// 絵は横1単位・原点が左端中央なので、横のスケールがそのまま長さになる
    /// </summary>
    public class CannonBeamEffect : PooledEffect
    {
        [SerializeField] private SpriteRenderer _renderer;
        [Tooltip("ビームの太さ（絵の高さに対する倍率）")]
        [SerializeField] private float _thickness = 2.4f;
        [Tooltip("ここまでで先端まで伸びきる。残りで細くなって消える")]
        [SerializeField, Range(0.05f, 0.9f)] private float _extendRate = 0.3f;

        private float _length;

        /// <param name="direction">右へ撃つなら 1、左へ撃つなら -1</param>
        public void Play(Vector3 origin, float length, int direction)
        {
            _length = length * direction;
            Begin(origin);
        }

        protected override void Animate(float rate)
        {
            float extend = Mathf.Clamp01(rate / _extendRate);
            float shrink = Mathf.Clamp01((rate - _extendRate) / (1f - _extendRate));
            transform.localScale = new Vector3(_length * extend, _thickness * (1f - shrink), 1f);
            SetAlpha(_renderer, 1f - shrink * shrink);
        }
    }
}
