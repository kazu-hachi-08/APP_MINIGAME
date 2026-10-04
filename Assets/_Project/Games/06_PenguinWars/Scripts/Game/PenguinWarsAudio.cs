using System;
using System.Collections.Generic;
using MiniGame.Common.Audio;
using UnityEngine;

namespace MiniGame.PenguinWars
{
    /// <summary>
    /// ペンギン大戦争のSE（出撃ポンッ・ヒットペチッ・撃破・ペンギン砲・レベルUP・城崩れ）をプログラムで生成して鳴らす（仕様書 §9）。
    /// 他のゲームの *Audio と同じく、正式な素材が用意できたらクリップ欄に差し込めばそのまま置き換わる
    /// </summary>
    public class PenguinWarsAudio : MonoBehaviour
    {
        private const int SampleRate = 44100;

        // 同じ音の繰り返しが機械的に聞こえないよう、ピッチを少しだけ揺らす幅
        private const float PitchJitter = 0.08f;

        [Header("素材が用意できたら差し込む（未設定なら生成音を使う）")]
        [SerializeField] private AudioClip _spawnClip;
        [SerializeField] private AudioClip _hitClip;
        [SerializeField] private AudioClip _deathClip;
        [SerializeField] private AudioClip _cannonClip;
        [SerializeField] private AudioClip _levelUpClip;
        [SerializeField] private AudioClip _collapseClip;
        [Header("BGM（Audio/ に決まった名前で置くと Rebuild PenguinWars で自動で差し込まれる。仕様書 §9）")]
        [Tooltip("タイトル〜編成発表のBGM。未設定なら鳴らさない")]
        [SerializeField] private AudioClip _titleBgmClip;
        [Tooltip("プレイ中のBGM。未設定ならタイトルのBGMを流し続ける")]
        [SerializeField] private AudioClip _bgmClip;

        [Header("Volume")]
        [Range(0f, 1f)] [SerializeField] private float _spawnVolume = 0.5f;
        [Tooltip("一番よく鳴る音なので小さめにする")]
        [Range(0f, 1f)] [SerializeField] private float _hitVolume = 0.25f;
        [Range(0f, 1f)] [SerializeField] private float _deathVolume = 0.35f;
        [Range(0f, 1f)] [SerializeField] private float _cannonVolume = 0.7f;
        [Range(0f, 1f)] [SerializeField] private float _jingleVolume = 0.5f;
        [Range(0f, 1f)] [SerializeField] private float _bgmVolume = 0.6f;
        [SerializeField] private float _bgmFadeSeconds = 0.5f;

        [Header("同じ音を鳴らす最短間隔（秒）。大量の敵でも音が割れず、うるさくならないように")]
        [SerializeField] private float _hitMinInterval = 0.08f;
        [SerializeField] private float _defaultMinInterval = 0.04f;

        private readonly Dictionary<AudioClip, float> _lastPlayTimes = new Dictionary<AudioClip, float>();

        private void Awake()
        {
            // 生成は1回だけ。以降は同じクリップを使い回す
            if (_spawnClip == null) _spawnClip = CreateSweep("Se_PenguinSpawn", 320f, 760f, 0.12f);
            if (_hitClip == null) _hitClip = CreateSlap("Se_PenguinHit");
            if (_deathClip == null) _deathClip = CreateSweep("Se_PenguinDeath", 500f, 1300f, 0.4f);
            if (_cannonClip == null) _cannonClip = CreateRumble("Se_PenguinCannon", 0.8f, 70f, 4f);
            if (_levelUpClip == null) _levelUpClip = CreateNotes("Se_PenguinLevelUp", new[] { 523f, 659f, 784f, 1047f }, 0.09f);
            if (_collapseClip == null) _collapseClip = CreateRumble("Se_PenguinCollapse", 1.5f, 45f, 1.8f);
        }

        public void PlaySpawn() => Play(_spawnClip, _spawnVolume, RandomPitch(), _defaultMinInterval);

        public void PlayHit() => Play(_hitClip, _hitVolume, RandomPitch(), _hitMinInterval);

        public void PlayDeath() => Play(_deathClip, _deathVolume, RandomPitch(), _defaultMinInterval);

        public void PlayCannon() => Play(_cannonClip, _cannonVolume, 1f, _defaultMinInterval);

        public void PlayLevelUp() => Play(_levelUpClip, _jingleVolume, 1f, _defaultMinInterval);

        public void PlayCollapse() => Play(_collapseClip, _cannonVolume, 1f, _defaultMinInterval);

        public void PlayTitleBgm() => PlayBgm(_titleBgmClip);

        public void PlayBattleBgm() => PlayBgm(_bgmClip);

        public void StopBgm()
        {
            if (!AudioManager.HasInstance) return;

            AudioManager.Instance.StopBgm(_bgmFadeSeconds);
        }

        /// <summary>素材が無いときは何もしない（止めると、前に流れていた曲まで消えて無音になるため）</summary>
        private void PlayBgm(AudioClip clip)
        {
            if (clip == null || !AudioManager.HasInstance) return;

            AudioManager.Instance.PlayBgmClip(clip, _bgmFadeSeconds, _bgmVolume);
        }

        private static float RandomPitch() => UnityEngine.Random.Range(1f - PitchJitter, 1f + PitchJitter);

        /// <summary>同じフレーム・短い間隔で同じ音が重なったら1回にまとめる</summary>
        private void Play(AudioClip clip, float volume, float pitch, float minInterval)
        {
            if (clip == null || !AudioManager.HasInstance) return;
            if (_lastPlayTimes.TryGetValue(clip, out float last) && Time.unscaledTime - last < minInterval) return;

            _lastPlayTimes[clip] = Time.unscaledTime;
            AudioManager.Instance.PlaySeClip(clip, volume, pitch);
        }

        /// <summary>音程が滑らかに上がる「ポンッ」「ひゅ〜ん」。出撃と、魂が昇る撃破に使う</summary>
        private static AudioClip CreateSweep(string name, float fromFrequency, float toFrequency, float duration)
        {
            // 立ち上がりを一瞬だけなだらかにして、鳴り始めのプツッというノイズを消す
            const float attackTime = 0.005f;
            float slope = (toFrequency - fromFrequency) / duration;
            return CreateClip(name, duration, t =>
            {
                // 周波数を直線で変えるときの位相は、周波数を時間で積分したもの
                float phase = 2f * Mathf.PI * (fromFrequency * t + 0.5f * slope * t * t);
                float envelope = Mathf.Min(1f, t / attackTime) * (1f - t / duration);
                return Mathf.Sin(phase) * envelope * 0.6f;
            });
        }

        /// <summary>ごく短いノイズと高めの打撃音を重ねた「ペチッ」</summary>
        private static AudioClip CreateSlap(string name)
        {
            const float duration = 0.07f;
            var random = new System.Random(1);
            return CreateClip(name, duration, t =>
            {
                float noise = ((float)random.NextDouble() * 2f - 1f) * Mathf.Exp(-t * 90f);
                float tone = Mathf.Sin(2f * Mathf.PI * 1100f * t) * Mathf.Exp(-t * 60f);
                return (noise * 0.6f + tone * 0.4f) * 0.8f;
            });
        }

        /// <summary>低いうなりにこもったノイズを重ねた「ドーン」「ゴゴゴ」。ペンギン砲と城崩れに使う</summary>
        private static AudioClip CreateRumble(string name, float duration, float baseFrequency, float decay)
        {
            var random = new System.Random(2);
            float filtered = 0f;
            return CreateClip(name, duration, t =>
            {
                // 一次ローパスでノイズの刺さりを抑え、地響きに近づける（サンプル順に呼ばれる前提）
                filtered = Mathf.Lerp(filtered, (float)random.NextDouble() * 2f - 1f, 0.08f);
                float tone = Mathf.Sin(2f * Mathf.PI * baseFrequency * t);
                float envelope = Mathf.Min(1f, t / 0.01f) * Mathf.Exp(-t * decay) * (1f - t / duration);
                return (tone * 0.5f + filtered * 1.5f) * envelope;
            });
        }

        /// <summary>指定した音程を順に鳴らす短いジングル。矩形波を各音の頭だけ強めて、ゲームらしい歯切れのよい音色にする</summary>
        private static AudioClip CreateNotes(string name, float[] frequencies, float noteDuration)
        {
            return CreateClip(name, noteDuration * frequencies.Length, t =>
            {
                int note = Mathf.Min((int)(t / noteDuration), frequencies.Length - 1);
                float local = t - note * noteDuration;
                float wave = Mathf.Sin(2f * Mathf.PI * frequencies[note] * t);
                return Mathf.Sign(wave) * 0.2f * Mathf.Exp(-local * 10f) + wave * 0.3f;
            });
        }

        private static AudioClip CreateClip(string name, float duration, Func<float, float> wave)
        {
            int sampleCount = Mathf.CeilToInt(SampleRate * duration);
            var data = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++) data[i] = Mathf.Clamp(wave(i / (float)SampleRate), -1f, 1f);

            var clip = AudioClip.Create(name, sampleCount, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
