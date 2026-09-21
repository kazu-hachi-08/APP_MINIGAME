using System.Collections;
using System.Collections.Generic;
using MiniGame.Common.Core;
using UnityEngine;

namespace MiniGame.Common.Audio
{
    /// <summary>
    /// BGM・SEの再生、音量管理、永続化を行う統合オーディオマネージャー
    /// </summary>
    public class AudioManager : SingletonMonoBehaviour<AudioManager>
    {
        private const string PrefsKeyMasterVol = "Audio_MasterVolume";
        private const string PrefsKeyBgmVol = "Audio_BgmVolume";
        private const string PrefsKeySeVol = "Audio_SeVolume";
        private const string PrefsKeyMute = "Audio_IsMute";

        [Header("Audio Sources")]
        [SerializeField] private AudioSource _bgmSourceA;
        [SerializeField] private AudioSource _bgmSourceB;
        [SerializeField] private AudioSource _seSourceTemplate;
        [SerializeField] private int _seSourcePoolSize = 6;

        [Header("Sound Data Lists")]
        [SerializeField] private List<BgmData> _bgmList = new List<BgmData>();
        [SerializeField] private List<SeData> _seList = new List<SeData>();

        private readonly Dictionary<BgmId, BgmData> _bgmDict = new Dictionary<BgmId, BgmData>();
        private readonly Dictionary<SeId, SeData> _seDict = new Dictionary<SeId, SeData>();
        private readonly List<AudioSource> _seSourcePool = new List<AudioSource>();

        private AudioSource _activeBgmSource;
        private AudioSource _inactiveBgmSource;
        private Coroutine _bgmCrossfadeRoutine;

        public float MasterVolume { get; private set; } = 1.0f;
        public float BgmVolume { get; private set; } = 0.8f;
        public float SeVolume { get; private set; } = 1.0f;
        public bool IsMuted { get; private set; } = false;

        public BgmId CurrentBgmId { get; private set; } = BgmId.None;

        protected override void Awake()
        {
            base.Awake();
            InitializeAudioSources();
            LoadAudioSettings();
            BuildDictionaries();
        }

        private void InitializeAudioSources()
        {
            if (_bgmSourceA == null)
            {
                var bgmAObj = new GameObject("BgmSource_A");
                bgmAObj.transform.SetParent(transform);
                _bgmSourceA = bgmAObj.AddComponent<AudioSource>();
                _bgmSourceA.loop = true;
                _bgmSourceA.playOnAwake = false;
            }

            if (_bgmSourceB == null)
            {
                var bgmBObj = new GameObject("BgmSource_B");
                bgmBObj.transform.SetParent(transform);
                _bgmSourceB = bgmBObj.AddComponent<AudioSource>();
                _bgmSourceB.loop = true;
                _bgmSourceB.playOnAwake = false;
            }

            _activeBgmSource = _bgmSourceA;
            _inactiveBgmSource = _bgmSourceB;

            // SEプール作成
            for (int i = 0; i < _seSourcePoolSize; i++)
            {
                var seObj = new GameObject($"SeSource_{i}");
                seObj.transform.SetParent(transform);
                var source = seObj.AddComponent<AudioSource>();
                source.loop = false;
                source.playOnAwake = false;
                _seSourcePool.Add(source);
            }
        }

        private void BuildDictionaries()
        {
            _bgmDict.Clear();
            foreach (var item in _bgmList)
            {
                if (item.clip != null && !_bgmDict.ContainsKey(item.id))
                {
                    _bgmDict.Add(item.id, item);
                }
            }

            _seDict.Clear();
            foreach (var item in _seList)
            {
                if (item.clip != null && !_seDict.ContainsKey(item.id))
                {
                    _seDict.Add(item.id, item);
                }
            }
        }

        private void LoadAudioSettings()
        {
            MasterVolume = PlayerPrefs.GetFloat(PrefsKeyMasterVol, 1.0f);
            BgmVolume = PlayerPrefs.GetFloat(PrefsKeyBgmVol, 0.8f);
            SeVolume = PlayerPrefs.GetFloat(PrefsKeySeVol, 1.0f);
            IsMuted = PlayerPrefs.GetInt(PrefsKeyMute, 0) == 1;

            ApplyVolumes();
        }

        public void SaveAudioSettings()
        {
            PlayerPrefs.SetFloat(PrefsKeyMasterVol, MasterVolume);
            PlayerPrefs.SetFloat(PrefsKeyBgmVol, BgmVolume);
            PlayerPrefs.SetFloat(PrefsKeySeVol, SeVolume);
            PlayerPrefs.SetInt(PrefsKeyMute, IsMuted ? 1 : 0);
            PlayerPrefs.Save();
        }

        public void SetMasterVolume(float volume)
        {
            MasterVolume = Mathf.Clamp01(volume);
            ApplyVolumes();
        }

        public void SetBgmVolume(float volume)
        {
            BgmVolume = Mathf.Clamp01(volume);
            ApplyVolumes();
        }

        public void SetSeVolume(float volume)
        {
            SeVolume = Mathf.Clamp01(volume);
            ApplyVolumes();
        }

        public void SetMute(bool isMute)
        {
            IsMuted = isMute;
            ApplyVolumes();
        }

        private void ApplyVolumes()
        {
            float effectiveBgm = IsMuted ? 0f : MasterVolume * BgmVolume;
            if (_activeBgmSource != null) _activeBgmSource.volume = effectiveBgm;
            if (_inactiveBgmSource != null) _inactiveBgmSource.volume = 0f;
        }

        /// <summary>
        /// BGMの再生（クロスフェード対応）
        /// </summary>
        public void PlayBgm(BgmId id, float fadeDuration = 0.5f)
        {
            if (id == BgmId.None)
            {
                StopBgm(fadeDuration);
                return;
            }

            if (CurrentBgmId == id && _activeBgmSource != null && _activeBgmSource.isPlaying)
            {
                return;
            }

            if (!_bgmDict.TryGetValue(id, out var bgmData) || bgmData.clip == null)
            {
                Debug.LogWarning($"[AudioManager] 未登録のBGM ID: {id}");
                return;
            }

            CurrentBgmId = id;

            if (_bgmCrossfadeRoutine != null)
            {
                StopCoroutine(_bgmCrossfadeRoutine);
            }

            _bgmCrossfadeRoutine = StartCoroutine(CrossfadeBgmRoutine(bgmData, fadeDuration));
        }

        /// <summary>
        /// AudioClipを直接指定してBGM再生
        /// </summary>
        public void PlayBgmClip(AudioClip clip, float fadeDuration = 0.5f, float volumeRate = 1.0f)
        {
            if (clip == null)
            {
                StopBgm(fadeDuration);
                return;
            }

            CurrentBgmId = BgmId.None;
            var data = new BgmData { clip = clip, volume = volumeRate, id = BgmId.None };

            if (_bgmCrossfadeRoutine != null)
            {
                StopCoroutine(_bgmCrossfadeRoutine);
            }

            _bgmCrossfadeRoutine = StartCoroutine(CrossfadeBgmRoutine(data, fadeDuration));
        }

        /// <summary>
        /// BGMの停止
        /// </summary>
        public void StopBgm(float fadeDuration = 0.5f)
        {
            CurrentBgmId = BgmId.None;
            if (_bgmCrossfadeRoutine != null)
            {
                StopCoroutine(_bgmCrossfadeRoutine);
            }

            _bgmCrossfadeRoutine = StartCoroutine(FadeOutBgmRoutine(fadeDuration));
        }

        private IEnumerator CrossfadeBgmRoutine(BgmData nextBgm, float duration)
        {
            var oldSource = _activeBgmSource;
            var newSource = _inactiveBgmSource;

            newSource.clip = nextBgm.clip;
            newSource.volume = 0f;
            newSource.Play();

            float targetMaxVolume = (IsMuted ? 0f : MasterVolume * BgmVolume) * (nextBgm.volume > 0 ? nextBgm.volume : 1f);
            float startOldVolume = oldSource.isPlaying ? oldSource.volume : 0f;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                if (oldSource.isPlaying)
                {
                    oldSource.volume = Mathf.Lerp(startOldVolume, 0f, t);
                }
                newSource.volume = Mathf.Lerp(0f, targetMaxVolume, t);
                yield return null;
            }

            oldSource.Stop();
            oldSource.volume = 0f;
            newSource.volume = targetMaxVolume;

            // スワップ
            _activeBgmSource = newSource;
            _inactiveBgmSource = oldSource;
            _bgmCrossfadeRoutine = null;
        }

        private IEnumerator FadeOutBgmRoutine(float duration)
        {
            if (_activeBgmSource != null && _activeBgmSource.isPlaying)
            {
                float startVol = _activeBgmSource.volume;
                float elapsed = 0f;

                while (elapsed < duration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    _activeBgmSource.volume = Mathf.Lerp(startVol, 0f, elapsed / duration);
                    yield return null;
                }

                _activeBgmSource.Stop();
                _activeBgmSource.volume = 0f;
            }

            _bgmCrossfadeRoutine = null;
        }

        /// <summary>
        /// SEのワンショット再生
        /// </summary>
        public void PlaySe(SeId id, float pitch = 1.0f)
        {
            if (id == SeId.None) return;

            if (!_seDict.TryGetValue(id, out var seData) || seData.clip == null)
            {
                return;
            }

            PlaySeClip(seData.clip, (seData.volume > 0 ? seData.volume : 1f), pitch);
        }

        /// <summary>
        /// AudioClip直接指定でSE再生
        /// </summary>
        public void PlaySeClip(AudioClip clip, float volumeScale = 1.0f, float pitch = 1.0f)
        {
            if (clip == null || IsMuted) return;

            AudioSource source = GetAvailableSeSource();
            if (source != null)
            {
                source.pitch = pitch;
                float effectiveVolume = MasterVolume * SeVolume * volumeScale;
                source.PlayOneShot(clip, effectiveVolume);
            }
        }

        private AudioSource GetAvailableSeSource()
        {
            foreach (var source in _seSourcePool)
            {
                if (!source.isPlaying)
                {
                    return source;
                }
            }

            // プールに空きがない場合は最初のを再利用
            return _seSourcePool.Count > 0 ? _seSourcePool[0] : null;
        }

        /// <summary>
        /// 指定SEが登録済みか（仮素材の自動生成が正式素材を上書きしないよう判定に使う）
        /// </summary>
        public bool HasSe(SeId id)
        {
            return _seDict.ContainsKey(id);
        }

        /// <summary>
        /// ランタイムでのSEデータ登録用ヘルパー
        /// </summary>
        public void RegisterSe(SeId id, AudioClip clip, float volume = 1f)
        {
            _seDict[id] = new SeData { id = id, clip = clip, volume = volume };
        }

        /// <summary>
        /// ランタイムでのBGMデータ登録用ヘルパー
        /// </summary>
        public void RegisterBgm(BgmId id, AudioClip clip, float volume = 1f)
        {
            _bgmDict[id] = new BgmData { id = id, clip = clip, volume = volume };
        }
    }
}
