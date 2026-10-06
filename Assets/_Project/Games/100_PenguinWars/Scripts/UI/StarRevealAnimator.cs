using System;
using MiniGame.PenguinWars.Battle;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.PenguinWars
{
    /// <summary>
    /// リザルトの★を左から1つずつポンと出す（ステージ計画 Phase 7）。今回新しく取った★は光りながら跳ね続ける。
    /// リザルトは時間を止めた後に出ることがあるので、すべて unscaled の時間で動かす
    /// </summary>
    public class StarRevealAnimator : MonoBehaviour
    {
        [Tooltip("左から StageLabels.StarOrder の順（クリア・城HP・タイム）")]
        [SerializeField] private Text[] _stars;
        [SerializeField] private PenguinWarsAudio _audio;
        [SerializeField] private float _firstDelay = 0.3f;
        [SerializeField] private float _interval = 0.3f;
        [SerializeField] private float _popDuration = 0.25f;
        [Tooltip("ポンと出るときに一瞬だけ大きくなる倍率")]
        [SerializeField] private float _popOvershoot = 1.4f;
        [SerializeField] private Color _earnedColor = new Color(1f, 0.92f, 0.3f);
        [SerializeField] private Color _missingColor = new Color(0.5f, 0.5f, 0.55f);
        [Tooltip("新しく取った★が光るときの色")]
        [SerializeField] private Color _glowColor = Color.white;
        [SerializeField] private float _bounceHeight = 20f;
        [SerializeField] private float _bounceSeconds = 0.6f;

        private Vector2[] _basePositions;
        private bool[] _isEarned;
        private bool[] _isNew;
        private float _elapsed;
        private bool _finished = true;
        private Action _onFinished;

        /// <param name="onFinished">最後の★が出終わったら呼ぶ（仲間のカードはこの後に出す）</param>
        public void Play(StarFlags stars, StarFlags newStars, Action onFinished)
        {
            CaptureBasePositions();
            _isEarned = new bool[_stars.Length];
            _isNew = new bool[_stars.Length];
            for (int i = 0; i < _stars.Length; i++)
            {
                StarFlags star = StageLabels.StarOrder[i];
                bool earned = StarRule.Has(stars, star);
                _isEarned[i] = earned;
                _isNew[i] = StarRule.Has(newStars, star);
                _stars[i].text = StageLabels.Star(earned);
                _stars[i].color = earned ? _earnedColor : _missingColor;
                _stars[i].transform.localScale = Vector3.zero;
            }

            _elapsed = 0f;
            _finished = false;
            _onFinished = onFinished;
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            _finished = true;
            gameObject.SetActive(false);
        }

        private void CaptureBasePositions()
        {
            if (_basePositions != null) return;

            _basePositions = new Vector2[_stars.Length];
            for (int i = 0; i < _stars.Length; i++) _basePositions[i] = _stars[i].rectTransform.anchoredPosition;
        }

        private void Update()
        {
            if (_isNew == null) return;

            float previous = _elapsed;
            _elapsed += Time.unscaledDeltaTime;
            for (int i = 0; i < _stars.Length; i++) Animate(i, previous);
            CheckFinished();
        }

        private float RevealTime(int index) => _firstDelay + index * _interval;

        private void Animate(int index, float previous)
        {
            float local = _elapsed - RevealTime(index);
            if (local < 0f) return;

            // 出た瞬間（このフレームで出番を過ぎた）だけ音を鳴らす
            if (previous < RevealTime(index) && _isEarned[index]) _audio.PlayStar(index);

            _stars[index].transform.localScale = Vector3.one * PopScale(local);
            if (_isNew[index]) Glow(index, local);
        }

        /// <summary>0 → 一瞬大きく → 1 に戻る</summary>
        private float PopScale(float local)
        {
            float rate = Mathf.Clamp01(local / _popDuration);
            return rate < 0.5f
                ? Mathf.Lerp(0f, _popOvershoot, rate * 2f)
                : Mathf.Lerp(_popOvershoot, 1f, (rate - 0.5f) * 2f);
        }

        private void Glow(int index, float local)
        {
            float wave = Mathf.Abs(Mathf.Sin(local / _bounceSeconds * Mathf.PI));
            _stars[index].rectTransform.anchoredPosition = _basePositions[index] + Vector2.up * (_bounceHeight * wave);
            _stars[index].color = Color.Lerp(_earnedColor, _glowColor, wave);
        }

        private void CheckFinished()
        {
            if (_finished || _elapsed < RevealTime(_stars.Length - 1) + _popDuration) return;

            _finished = true;
            Action onFinished = _onFinished;
            _onFinished = null;
            onFinished?.Invoke();
        }
    }
}
