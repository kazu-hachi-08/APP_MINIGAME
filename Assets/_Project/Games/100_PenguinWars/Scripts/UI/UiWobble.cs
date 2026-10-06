using UnityEngine;

namespace MiniGame.PenguinWars
{
    /// <summary>文字を左右に傾けながら少し伸び縮みさせる（リザルトの NEW RECORD! など、目に留めてほしい表示用）</summary>
    public class UiWobble : MonoBehaviour
    {
        [SerializeField] private float _angle = 8f;
        [SerializeField] private float _scaleWidth = 0.08f;
        [SerializeField] private float _cycleSeconds = 0.5f;

        private void Update()
        {
            // リザルトは時間を止めた後に出ることがあるので unscaled
            float wave = Mathf.Sin(Time.unscaledTime / _cycleSeconds * 2f * Mathf.PI);
            transform.localRotation = Quaternion.Euler(0f, 0f, wave * _angle);
            transform.localScale = Vector3.one * (1f + Mathf.Abs(wave) * _scaleWidth);
        }
    }
}
