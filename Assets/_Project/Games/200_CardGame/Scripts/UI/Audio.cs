#nullable enable
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CardGame.Unity.UI
{
    /// <summary>
    /// 効果音と BGM の再生。音源は Resources/Audio/{se,bgm}(Tools/AudioGen が合成した WAV)。
    /// 単純な AudioSource のプールで鳴らすだけ。音量は PlayerPrefs に保存する。
    /// </summary>
    public static class Audio
    {
        // ---- 効果音の名前(Resources/Audio/se/<名前>.wav) ----
        public const string CardPlay = "card_play";
        public const string CardDraw = "card_draw";
        public const string Attack = "attack";
        public const string AttackLeader = "attack_leader";
        public const string Summon = "summon";
        public const string Destroy = "destroy";
        public const string Spell = "spell";
        public const string Heal = "heal";
        public const string Buff = "buff";
        public const string TurnStart = "turn_start";
        public const string Awakening = "awakening";
        public const string Button = "button";
        public const string Win = "win";
        public const string Lose = "lose";

        private const string SeVolumeKey = "audio.se", BgmVolumeKey = "audio.bgm";

        private static AudioHost? _host;
        private static readonly Dictionary<string, AudioClip?> Clips = new();
        private static float _seVolume = -1, _bgmVolume = -1;

        public static float SeVolume
        {
            get { if (_seVolume < 0) _seVolume = PlayerPrefs.GetFloat(SeVolumeKey, 0.8f); return _seVolume; }
            set { _seVolume = Mathf.Clamp01(value); PlayerPrefs.SetFloat(SeVolumeKey, _seVolume); }
        }

        public static float BgmVolume
        {
            get { if (_bgmVolume < 0) _bgmVolume = PlayerPrefs.GetFloat(BgmVolumeKey, 0.45f); return _bgmVolume; }
            set
            {
                _bgmVolume = Mathf.Clamp01(value);
                PlayerPrefs.SetFloat(BgmVolumeKey, _bgmVolume);
                if (_host != null) _host.SetBgmVolume(_bgmVolume);
            }
        }

        private static AudioHost Host
        {
            get
            {
                if (_host == null)
                {
                    var go = new GameObject("AudioHost");
                    Object.DontDestroyOnLoad(go);
                    // 音を聞く「耳」。シーンのカメラに付いていなかったため、音が一切出ていなかった(2026-09-23)
                    if (Object.FindAnyObjectByType<AudioListener>() == null) go.AddComponent<AudioListener>();
                    _host = go.AddComponent<AudioHost>();
                }
                return _host;
            }
        }

        private static AudioClip? Clip(string folder, string name)
        {
            var path = $"Audio/{folder}/{name}";
            if (Clips.TryGetValue(path, out var c)) return c;
            c = Resources.Load<AudioClip>(path);
            if (c == null) Debug.LogWarning($"[Audio] 音源が無い: {path}");
            Clips[path] = c;
            return c;
        }

        /// <summary>
        /// 全ての音源を先に読み込む(起動時に 1 回)。WebGL は音の展開が非同期で、読み込み前に鳴らすと無音になるため
        /// (2026-09-23: BGM が一度も鳴らず、効果音は初回だけ無音だった)。
        /// </summary>
        public static void PreloadAll()
        {
            foreach (var folder in new[] { "se", "bgm" })
                foreach (var clip in Resources.LoadAll<AudioClip>("Audio/" + folder))
                {
                    Clips[$"Audio/{folder}/{clip.name}"] = clip;
                    if (clip.loadState == AudioDataLoadState.Unloaded) clip.LoadAudioData();
                }
        }

        /// <summary>効果音を鳴らす。pitch を少し散らすと連続再生でも耳に付きにくい。</summary>
        public static void Play(string name, float volume = 1f, float pitchJitter = 0.06f)
        {
            var clip = Clip("se", name);
            if (clip == null) return;
            Host.PlayOneShot(clip, volume * SeVolume, 1f + Random.Range(-pitchJitter, pitchJitter));
        }

        /// <summary>BGM を切り替える(クロスフェード)。同じ曲なら何もしない。</summary>
        public static void PlayBgm(string name, float fade = 1.0f)
        {
            var clip = Clip("bgm", name);
            if (clip == null) return;
            Host.CrossFadeTo(clip, BgmVolume, fade);
        }

        public static void StopBgm(float fade = 0.6f) => Host.CrossFadeTo(null, BgmVolume, fade);

        /// <summary>AudioSource を束ねる常駐オブジェクト。</summary>
        private sealed class AudioHost : MonoBehaviour
        {
            private readonly List<AudioSource> _pool = new();
            private AudioSource _bgmA = null!, _bgmB = null!;
            private bool _useA = true;
            private Coroutine? _fade;

            private void Awake()
            {
                _bgmA = gameObject.AddComponent<AudioSource>();
                _bgmB = gameObject.AddComponent<AudioSource>();
                foreach (var s in new[] { _bgmA, _bgmB })
                {
                    s.loop = true;
                    s.playOnAwake = false;
                    s.volume = 0;
                }
            }

            public void PlayOneShot(AudioClip clip, float volume, float pitch)
            {
                AudioSource? src = null;
                foreach (var s in _pool)
                    if (!s.isPlaying) { src = s; break; }
                if (src == null)
                {
                    if (_pool.Count >= 12) return;      // 同時に鳴りすぎるのを防ぐ
                    src = gameObject.AddComponent<AudioSource>();
                    src.playOnAwake = false;
                    _pool.Add(src);
                }
                src.clip = clip;
                src.volume = Mathf.Clamp01(volume);
                src.pitch = pitch;
                if (clip.loadState == AudioDataLoadState.Loaded) src.Play();
                else StartCoroutine(PlayWhenLoaded(src, clip, 1.5f));   // 読み込み中なら終わってから(遅すぎたら諦める)
            }

            public void SetBgmVolume(float v)
            {
                var cur = _useA ? _bgmA : _bgmB;
                if (cur.isPlaying) cur.volume = v;
            }

            public void CrossFadeTo(AudioClip? clip, float volume, float fade)
            {
                var cur = _useA ? _bgmA : _bgmB;
                if (clip != null && cur.clip == clip && cur.isPlaying) { cur.volume = volume; return; }
                var next = _useA ? _bgmB : _bgmA;
                _useA = !_useA;
                if (clip != null)
                {
                    next.clip = clip;
                    next.volume = 0;
                    if (clip.loadState == AudioDataLoadState.Loaded) next.Play();
                    else StartCoroutine(PlayWhenLoaded(next, clip, 30f));   // BGM は遅れても鳴らす
                }
                if (_fade != null) StopCoroutine(_fade);
                _fade = StartCoroutine(FadeRoutine(cur, next, clip == null ? 0 : volume, fade));
            }

            /// <summary>音源の読み込みを待ってから鳴らす。待っている間に別の音源に差し替えられたら何もしない。</summary>
            private IEnumerator PlayWhenLoaded(AudioSource src, AudioClip clip, float timeout)
            {
                if (clip.loadState == AudioDataLoadState.Unloaded) clip.LoadAudioData();
                float t = 0;
                while (clip.loadState == AudioDataLoadState.Loading && t < timeout) { t += Time.unscaledDeltaTime; yield return null; }
                if (clip.loadState != AudioDataLoadState.Loaded || src.clip != clip) yield break;
                if (timeout < 5f && t > 0.4f) yield break;   // 効果音は遅れて鳴ると不自然なので 0.4 秒を過ぎたら鳴らさない
                src.Play();
            }

            private IEnumerator FadeRoutine(AudioSource from, AudioSource to, float volume, float dur)
            {
                float startFrom = from.volume;
                float t = 0;
                while (t < dur)
                {
                    t += Time.unscaledDeltaTime;
                    float k = Mathf.Clamp01(t / dur);
                    from.volume = Mathf.Lerp(startFrom, 0, k);
                    to.volume = Mathf.Lerp(0, volume, k);
                    yield return null;
                }
                from.Stop();
                to.volume = volume;
                _fade = null;
            }
        }
    }
}
