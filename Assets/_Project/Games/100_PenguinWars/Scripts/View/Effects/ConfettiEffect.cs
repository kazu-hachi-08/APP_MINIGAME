using UnityEngine;

namespace MiniGame.PenguinWars
{
    /// <summary>
    /// 敵の城を落としたときの紙吹雪1枚（ステージ計画 Phase 7）。上へ弾けてから、くるくる回りながらひらひら落ちる。
    /// 絵は白い小さな四角1つで、色は出すときに付ける（色ごとに絵を作らないため）
    /// </summary>
    public class ConfettiEffect : PooledEffect
    {
        [SerializeField] private SpriteRenderer _renderer;
        [Tooltip("落ちる速さ。紙なので重力より弱くして、ゆっくり舞わせる")]
        [SerializeField] private float _gravity = 5f;
        [Tooltip("落ちる速さの上限。これ以上は速くならず、ひらひら漂う")]
        [SerializeField] private float _maxFallSpeed = 2.5f;
        [SerializeField] private float _swayWidth = 0.3f;
        [SerializeField] private float _swayCycles = 3f;
        [SerializeField] private float _spinSpeed = 540f;
        [Tooltip("この割合を過ぎたら薄くしていく")]
        [SerializeField, Range(0f, 1f)] private float _fadeStart = 0.7f;

        private Vector3 _origin;
        private Vector2 _velocity;
        private float _spinDirection;
        private float _swayPhase;

        public void Play(Vector3 position, Vector2 velocity, Color color)
        {
            _origin = position;
            _velocity = velocity;
            _spinDirection = Random.value < 0.5f ? -1f : 1f;
            _swayPhase = Random.value * 2f * Mathf.PI;
            _renderer.color = color;
            Begin(position);
        }

        protected override void Animate(float rate)
        {
            float t = rate * Duration;
            transform.position = _origin + new Vector3(_velocity.x * t + Sway(rate), Height(t), 0f);
            transform.rotation = Quaternion.Euler(0f, 0f, _spinDirection * _spinSpeed * t);
            SetAlpha(_renderer, 1f - Mathf.Clamp01((rate - _fadeStart) / (1f - _fadeStart)));
        }

        /// <summary>落ちる速さが上限に届くまでは放物線、届いたら一定の速さで落ちる</summary>
        private float Height(float t)
        {
            float limitTime = (_velocity.y + _maxFallSpeed) / _gravity;
            if (t <= limitTime) return _velocity.y * t - 0.5f * _gravity * t * t;

            float limitHeight = _velocity.y * limitTime - 0.5f * _gravity * limitTime * limitTime;
            return limitHeight - _maxFallSpeed * (t - limitTime);
        }

        /// <summary>出た瞬間は 0（揺れの途中から始めると、出た位置から跳んで見えるため）</summary>
        private float Sway(float rate) => (Mathf.Sin(rate * _swayCycles * 2f * Mathf.PI + _swayPhase) - Mathf.Sin(_swayPhase)) * _swayWidth;
    }
}
