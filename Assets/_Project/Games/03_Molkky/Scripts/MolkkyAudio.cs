using System;
using MiniGame.Common.Audio;
using UnityEngine;

namespace MiniGame.Molkky
{
    /// <summary>
    /// モルック固有のSE（投擲・棒が当たる・ピンが倒れる・立て直し・結果）をプログラムで生成して鳴らす。
    /// 共通の SeId は特定のミニゲーム専用の音を持たないため、卓球と同じく生成したクリップを直接再生している。
    /// 正式な素材が用意できたら Inspector のクリップ欄に差し込めばそのまま置き換わる。
    /// </summary>
    public class MolkkyAudio : MonoBehaviour
    {
        private const int SampleRate = 44100;

        [SerializeField] private MolkkyPhysicsSettings _settings;
        [SerializeField] private PinRack _pinRack;
        [SerializeField] private StickThrower _stick;

        [Header("素材が用意できたら差し込む（未設定なら生成音を使う）")]
        [SerializeField] private AudioClip _throwClip;
        [SerializeField] private AudioClip _stickHitClip;
        [SerializeField] private AudioClip _pinFallClip;
        [SerializeField] private AudioClip _pinResetClip;
        [SerializeField] private AudioClip _scoreClip;
        [SerializeField] private AudioClip _missClip;
        [SerializeField] private AudioClip _winClip;
        [SerializeField] private AudioClip _overClip;
        [SerializeField] private AudioClip _disqualifiedClip;

        [Header("Volume")]
        [Range(0f, 1f)] [SerializeField] private float _throwVolume = 0.35f;
        [Range(0f, 1f)] [SerializeField] private float _hitVolume = 0.9f;
        [Range(0f, 1f)] [SerializeField] private float _resetVolume = 0.4f;
        [Range(0f, 1f)] [SerializeField] private float _resultVolume = 0.6f;

        [Header("Collision")]
        [Tooltip("この相対速度以下の接触は音を鳴らさない（転がって触れ合うだけの音を出さないため）")]
        [SerializeField] private float _minHitSpeed = 0.6f;
        [Tooltip("この相対速度で最大音量になる")]
        [SerializeField] private float _maxHitSpeed = 8f;
        [Tooltip("同じ音を続けて鳴らす最短間隔（秒）。一斉に倒れたとき音が重なって割れないようにする")]
        [SerializeField] private float _minInterval = 0.04f;

        private float _lastStickHitTime = float.NegativeInfinity;
        private float _lastPinFallTime = float.NegativeInfinity;

        private void Awake()
        {
            // 生成は1回だけ。以降は同じクリップをピッチだけ変えて使い回す
            if (_throwClip == null) _throwClip = CreateWhoosh();
            if (_stickHitClip == null) _stickHitClip = CreateWood("Se_MkStickHit", 320f, 30f, 0.5f);
            if (_pinFallClip == null) _pinFallClip = CreateWood("Se_MkPinFall", 760f, 45f, 0.3f);
            if (_pinResetClip == null) _pinResetClip = CreateWood("Se_MkPinReset", 520f, 60f, 0.1f);
            if (_scoreClip == null) _scoreClip = CreateNotes("Se_MkScore", new[] { 784f, 1047f }, 0.1f);
            if (_missClip == null) _missClip = CreateNotes("Se_MkMiss", new[] { 220f }, 0.25f);
            if (_winClip == null) _winClip = CreateNotes("Se_MkWin", new[] { 523f, 659f, 784f, 1047f, 784f, 1047f }, 0.12f);
            if (_overClip == null) _overClip = CreateSlide("Se_MkOver", 700f, 180f, 0.6f);
            if (_disqualifiedClip == null) _disqualifiedClip = CreateBuzz();
        }

        private void OnEnable()
        {
            _pinRack.PinFell += HandlePinFell;
            _stick.Hit += HandleStickHit;
        }

        private void OnDisable()
        {
            _pinRack.PinFell -= HandlePinFell;
            _stick.Hit -= HandleStickHit;
        }

        /// <summary>投げた瞬間の風切り音。強く投げるほど高くして、強さを耳でも分かるようにする</summary>
        public void PlayThrow(ThrowRequest request)
        {
            float strength = Mathf.InverseLerp(_settings.MinThrowSpeed, _settings.MaxThrowSpeed, request.Speed);
            Play(_throwClip, _throwVolume, Mathf.Lerp(0.85f, 1.25f, strength));
        }

        public void PlayPinReset()
        {
            Play(_pinResetClip, _resetVolume, UnityEngine.Random.Range(0.95f, 1.1f));
        }

        public void PlayResult(ThrowOutcome outcome)
        {
            switch (outcome)
            {
                case ThrowOutcome.Win:
                    Play(_winClip, _resultVolume, 1f);
                    break;
                case ThrowOutcome.OverTo25:
                    Play(_overClip, _resultVolume, 1f);
                    break;
                case ThrowOutcome.Disqualified:
                    Play(_disqualifiedClip, _resultVolume, 1f);
                    break;
                case ThrowOutcome.Miss:
                    Play(_missClip, _resultVolume, 1f);
                    break;
                default:
                    Play(_scoreClip, _resultVolume, 1f);
                    break;
            }
        }

        private void HandleStickHit(float relativeSpeed)
        {
            if (relativeSpeed < _minHitSpeed || Time.time - _lastStickHitTime < _minInterval) return;

            _lastStickHitTime = Time.time;
            float strength = Mathf.InverseLerp(_minHitSpeed, _maxHitSpeed, relativeSpeed);
            Play(_stickHitClip, _hitVolume * Mathf.Lerp(0.4f, 1f, strength), UnityEngine.Random.Range(0.9f, 1.1f));
        }

        private void HandlePinFell(Pin pin)
        {
            if (Time.time - _lastPinFallTime < _minInterval) return;

            _lastPinFallTime = Time.time;
            // 1本ずつ少し高さを変えて、連鎖で倒れたとき「カラカラ」と聞こえるようにする
            Play(_pinFallClip, _hitVolume, UnityEngine.Random.Range(0.85f, 1.2f));
        }

        private static void Play(AudioClip clip, float volume, float pitch)
        {
            if (clip == null || !AudioManager.HasInstance) return;

            AudioManager.Instance.PlaySeClip(clip, volume, pitch);
        }

        /// <summary>木同士が当たる「コン」。基音＋2.7倍の倍音（木らしい非整数倍）にノイズのアタックを重ねる</summary>
        private static AudioClip CreateWood(string name, float frequency, float decay, float noise)
        {
            return CreateClip(name, 0.18f, t =>
                Mathf.Sin(2f * Mathf.PI * frequency * t) * Mathf.Exp(-t * decay)
                + Mathf.Sin(2f * Mathf.PI * frequency * 2.7f * t) * 0.35f * Mathf.Exp(-t * decay * 1.8f)
                + (UnityEngine.Random.value * 2f - 1f) * noise * Mathf.Exp(-t * 150f));
        }

        /// <summary>風切り音。ノイズの音量を山なりにして「シュッ」と聞かせる</summary>
        private static AudioClip CreateWhoosh()
        {
            const float duration = 0.3f;
            float filtered = 0f;
            return CreateClip("Se_MkThrow", duration, t =>
            {
                // 一次ローパスで高音の刺さりを抑える
                filtered = Mathf.Lerp(filtered, UnityEngine.Random.value * 2f - 1f, 0.25f);
                return filtered * Mathf.Sin(Mathf.PI * t / duration);
            });
        }

        /// <summary>指定した音程を順に鳴らす短いジングル（得点・ミス・勝利）</summary>
        private static AudioClip CreateNotes(string name, float[] frequencies, float noteDuration)
        {
            float duration = noteDuration * frequencies.Length;
            return CreateClip(name, duration, t =>
            {
                int note = Mathf.Min((int)(t / noteDuration), frequencies.Length - 1);
                float local = t - note * noteDuration;
                // 矩形波寄りにしてゲームらしい音色にする
                float wave = Mathf.Sin(2f * Mathf.PI * frequencies[note] * t);
                return Mathf.Sign(wave) * 0.25f * Mathf.Exp(-local * 10f) + wave * 0.3f;
            });
        }

        /// <summary>音程が下がっていく「ヒューン」。25点に戻るがっかり感を出す</summary>
        private static AudioClip CreateSlide(string name, float from, float to, float duration)
        {
            float phase = 0f;
            return CreateClip(name, duration, t =>
            {
                float frequency = Mathf.Lerp(from, to, t / duration);
                phase += 2f * Mathf.PI * frequency / SampleRate;
                return Mathf.Sin(phase) * 0.5f * (1f - t / duration * 0.7f);
            });
        }

        /// <summary>失格の「ブブー」。低い矩形波を2回鳴らす</summary>
        private static AudioClip CreateBuzz()
        {
            const float beep = 0.22f;
            const float gap = 0.08f;
            return CreateClip("Se_MkDisqualified", beep * 2f + gap, t =>
            {
                bool silent = t > beep && t < beep + gap;
                if (silent) return 0f;

                return Mathf.Sign(Mathf.Sin(2f * Mathf.PI * 150f * t)) * 0.3f;
            });
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
