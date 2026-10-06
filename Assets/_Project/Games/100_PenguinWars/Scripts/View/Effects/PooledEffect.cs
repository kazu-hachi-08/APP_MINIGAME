using UnityEngine;

namespace MiniGame.PenguinWars
{
    /// <summary>
    /// 一定時間だけ動いて自分で非表示になる演出の土台。非表示になったものを EffectPool が使い回す。
    /// 1回ごとに Instantiate / Destroy しないのは、乱戦では1秒に何十回もヒットして GC でカクつくため
    /// </summary>
    public abstract class PooledEffect : MonoBehaviour
    {
        [SerializeField] private float _duration = 0.4f;

        private float _elapsed;

        protected float Duration => _duration;

        protected void Begin(Vector3 position)
        {
            transform.position = position;
            _elapsed = 0f;
            gameObject.SetActive(true);
            Animate(0f);
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            float rate = Mathf.Clamp01(_elapsed / _duration);
            Animate(rate);
            if (rate >= 1f) gameObject.SetActive(false);
        }

        /// <param name="rate">0（出た瞬間）〜 1（消える瞬間）</param>
        protected abstract void Animate(float rate);

        protected static void SetAlpha(SpriteRenderer renderer, float alpha)
        {
            Color color = renderer.color;
            color.a = alpha;
            renderer.color = color;
        }
    }
}
