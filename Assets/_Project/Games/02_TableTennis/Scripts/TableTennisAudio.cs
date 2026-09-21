using System;
using MiniGame.Common.Audio;
using UnityEngine;

namespace MiniGame.TableTennis
{
    /// <summary>
    /// 卓球固有のSE（打球・バウンド・ネット）をプログラムで生成して鳴らす。
    /// 共通の SeId は特定のミニゲーム専用の音を持たないため、生成したクリップを直接再生している。
    /// 正式な素材が用意できたら Inspector のクリップ欄に差し込めばそのまま置き換わる。
    /// </summary>
    public class TableTennisAudio : MonoBehaviour
    {
        private const int SampleRate = 44100;

        [Header("素材が用意できたら差し込む（未設定なら生成音を使う）")]
        [SerializeField] private AudioClip _hitClip;
        [SerializeField] private AudioClip _bounceClip;
        [SerializeField] private AudioClip _netClip;

        [Header("Volume")]
        [Range(0f, 1f)] [SerializeField] private float _hitVolume = 0.85f;
        [Range(0f, 1f)] [SerializeField] private float _bounceVolume = 0.5f;
        [Range(0f, 1f)] [SerializeField] private float _netVolume = 0.7f;

        private void Awake()
        {
            // 生成は1回だけ。以降は同じクリップをピッチだけ変えて使い回す
            if (_hitClip == null) _hitClip = CreateHit();
            if (_bounceClip == null) _bounceClip = CreateBounce();
            if (_netClip == null) _netClip = CreateNet();
        }

        /// <summary>打球音。強く打つほど高く鳴らし、フリックの強さを耳でも分かるようにする</summary>
        public void PlayHit(float strength)
        {
            Play(_hitClip, _hitVolume, Mathf.Lerp(0.92f, 1.3f, Mathf.Clamp01(strength)));
        }

        /// <summary>台へのバウンド音。連続して鳴るので打球音より軽く短くする</summary>
        public void PlayBounce()
        {
            Play(_bounceClip, _bounceVolume, UnityEngine.Random.Range(0.95f, 1.08f));
        }

        public void PlayNet()
        {
            Play(_netClip, _netVolume, 1f);
        }

        private static void Play(AudioClip clip, float volume, float pitch)
        {
            if (clip == null || !AudioManager.HasInstance) return;

            AudioManager.Instance.PlaySeClip(clip, volume, pitch);
        }

        /// <summary>ラバーで弾く「パコン」。高音の短い減衰音にノイズのアタックを重ねる</summary>
        private static AudioClip CreateHit()
        {
            return CreateClip("Se_TtHit", 0.12f, t =>
                Mathf.Sin(2f * Mathf.PI * 1400f * t) * Mathf.Exp(-t * 55f)
                + Mathf.Sin(2f * Mathf.PI * 720f * t) * 0.4f * Mathf.Exp(-t * 40f)
                + (UnityEngine.Random.value * 2f - 1f) * 0.25f * Mathf.Exp(-t * 180f));
        }

        /// <summary>台に当たる「コツン」。打球音より低く、減衰を速くする</summary>
        private static AudioClip CreateBounce()
        {
            return CreateClip("Se_TtBounce", 0.09f, t =>
                Mathf.Sin(2f * Mathf.PI * 900f * t) * Mathf.Exp(-t * 70f)
                + Mathf.Sin(2f * Mathf.PI * 420f * t) * 0.3f * Mathf.Exp(-t * 60f));
        }

        /// <summary>ネットに吸われる「ボスッ」。倍音を持たせず低い帯域だけで鳴らす</summary>
        private static AudioClip CreateNet()
        {
            return CreateClip("Se_TtNet", 0.16f, t =>
                Mathf.Sin(2f * Mathf.PI * 180f * t) * Mathf.Exp(-t * 26f)
                + (UnityEngine.Random.value * 2f - 1f) * 0.2f * Mathf.Exp(-t * 45f));
        }

        private static AudioClip CreateClip(string name, float duration, Func<float, float> wave)
        {
            int sampleCount = Mathf.CeilToInt(SampleRate * duration);
            var data = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                data[i] = Mathf.Clamp(wave(i / (float)SampleRate), -1f, 1f);
            }

            var clip = AudioClip.Create(name, sampleCount, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
