using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.PenguinWars
{
    /// <summary>
    /// UI の Image でキャラのコマを順番に切り替える（ずかん用。仕様書 §2.0）。
    /// 戦闘の UnitView は状態からコマを選ぶが、ずかんには状態がないので決まった並びを繰り返すだけにする
    /// </summary>
    public class UnitSpriteAnimator : MonoBehaviour
    {
        [SerializeField] private Image _image;
        [SerializeField] private float _frameSeconds = 0.2f;

        private UnitSpriteSet _sprites;
        private PenguinFrame[] _sequence;
        private float _phaseSteps;

        /// <param name="phaseSteps">開始をずらすコマ数。並んだキャラが揃って動くと機械的に見えるため</param>
        public void Play(UnitSpriteSet sprites, PenguinFrame[] sequence, float phaseSteps = 0f)
        {
            _sprites = sprites;
            _sequence = sequence;
            _phaseSteps = phaseSteps;
            ApplyFrame();
        }

        private void Update()
        {
            ApplyFrame();
        }

        private void ApplyFrame()
        {
            if (_sprites == null || _sequence == null || _sequence.Length == 0) return;

            // タイトルは timeScale に関係なく動かしたいので unscaledTime を使う
            int step = Mathf.FloorToInt(Time.unscaledTime / _frameSeconds + _phaseSteps);
            Sprite sprite = _sprites.Get(_sequence[step % _sequence.Length]);
            _image.sprite = sprite;
            _image.enabled = sprite != null;
        }
    }
}
