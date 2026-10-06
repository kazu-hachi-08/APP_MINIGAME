using UnityEngine;

namespace MiniGame.PenguinWars
{
    /// <summary>撃破時に魂がゆらゆら昇って消える。自分が倒したときは獲得さかなの数字も一緒に出す（仕様書 §9）</summary>
    public class SoulRiseEffect : PooledEffect
    {
        [SerializeField] private SpriteRenderer _soul;
        [SerializeField] private TextMesh _rewardLabel;
        [Tooltip("数字の影。雪の白い地面・明るい空のどちらの上でも読めるようにする")]
        [SerializeField] private TextMesh _rewardShadow;
        [SerializeField] private float _riseDistance = 1.6f;
        [SerializeField] private float _swayWidth = 0.15f;
        [SerializeField] private float _swayCycles = 2f;
        [Tooltip("この割合を過ぎたら薄くしていく")]
        [SerializeField, Range(0f, 1f)] private float _fadeStart = 0.5f;

        private Vector3 _origin;

        /// <param name="reward">0 以下なら数字を出さない（相手が倒したとき）</param>
        public void Play(Vector3 position, int reward)
        {
            _origin = position;
            bool showReward = reward > 0;
            _rewardLabel.gameObject.SetActive(showReward);
            _rewardShadow.gameObject.SetActive(showReward);
            if (showReward)
            {
                string text = $"+{reward}";
                _rewardLabel.text = text;
                _rewardShadow.text = text;
            }
            Begin(position);
        }

        protected override void Animate(float rate)
        {
            float easeOut = 1f - (1f - rate) * (1f - rate);
            float sway = Mathf.Sin(rate * _swayCycles * 2f * Mathf.PI) * _swayWidth;
            transform.position = _origin + new Vector3(sway, _riseDistance * easeOut, 0f);

            float alpha = 1f - Mathf.Clamp01((rate - _fadeStart) / (1f - _fadeStart));
            SetAlpha(_soul, alpha);
            SetAlpha(_rewardLabel, alpha);
            SetAlpha(_rewardShadow, alpha);
        }

        private static void SetAlpha(TextMesh text, float alpha)
        {
            Color color = text.color;
            color.a = alpha;
            text.color = color;
        }
    }
}
