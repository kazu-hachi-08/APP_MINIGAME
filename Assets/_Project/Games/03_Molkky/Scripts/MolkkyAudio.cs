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

        // 強く投げるほど高く鳴らす風切り音のピッチ幅
        private const float ThrowPitchWeak = 0.85f;
        private const float ThrowPitchStrong = 1.25f;

        // 同じ音の繰り返しが機械的に聞こえないよう、ピッチを少しだけ揺らす幅
        private const float ResetPitchMin = 0.95f;
        private const float ResetPitchMax = 1.1f;
        private const float StickHitPitchMin = 0.9f;
        private const float StickHitPitchMax = 1.1f;

        // ピン同士の連鎖は1本ずつの違いを聞かせたいので、他より揺らす幅を広くする
        private const float PinFallPitchMin = 0.85f;
        private const float PinFallPitchMax = 1.2f;

        /// <summary>弱い接触でも無音にはせず、強さの差だけ伝わるようにする最小音量の割合</summary>
        private const float WeakHitVolumeRatio = 0.4f;

        // 木の音。基音に対して非整数倍の倍音を足すと、金属ではなく木らしく聞こえる
        private const float WoodDuration = 0.18f;
        private const float WoodOvertoneRatio = 2.7f;
        private const float WoodOvertoneGain = 0.35f;
        private const float WoodOvertoneDecayRatio = 1.8f;

        /// <summary>打撃の最初だけノイズを乗せて「当たった瞬間」を強調するための減衰速度</summary>
        private const float AttackNoiseDecay = 150f;

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

        [Tooltip("勝利演出で鳴らす勝利音のピッチ。50点のときより高くして別の音に聞こえるようにする")]
        [SerializeField] private float _victoryPitch = 1.25f;

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
            Play(_throwClip, _throwVolume, Mathf.Lerp(ThrowPitchWeak, ThrowPitchStrong, strength));
        }

        public void PlayPinReset()
        {
            Play(_pinResetClip, _resetVolume, UnityEngine.Random.Range(ResetPitchMin, ResetPitchMax));
        }

        public void PlayResult(ThrowOutcome outcome)
        {
            Play(ResultClip(outcome), _resultVolume, 1f);
        }

        /// <summary>勝利演出でキャラが登場したときの音。50点のときの音と同じ曲を高めに鳴らし、盛り上がりを続ける</summary>
        public void PlayVictory()
        {
            Play(_winClip, _resultVolume, _victoryPitch);
        }

        private AudioClip ResultClip(ThrowOutcome outcome)
        {
            switch (outcome)
            {
                case ThrowOutcome.Win: return _winClip;
                case ThrowOutcome.OverTo25: return _overClip;
                case ThrowOutcome.Disqualified: return _disqualifiedClip;
                case ThrowOutcome.Miss: return _missClip;
                default: return _scoreClip;
            }
        }

        private void HandleStickHit(float relativeSpeed)
        {
            if (relativeSpeed < _minHitSpeed || Time.time - _lastStickHitTime < _minInterval) return;

            _lastStickHitTime = Time.time;
            float strength = Mathf.InverseLerp(_minHitSpeed, _maxHitSpeed, relativeSpeed);
            float volume = _hitVolume * Mathf.Lerp(WeakHitVolumeRatio, 1f, strength);
            Play(_stickHitClip, volume, UnityEngine.Random.Range(StickHitPitchMin, StickHitPitchMax));
        }

        private void HandlePinFell(Pin pin)
        {
            if (Time.time - _lastPinFallTime < _minInterval) return;

            _lastPinFallTime = Time.time;
            // 1本ずつ少し高さを変えて、連鎖で倒れたとき「カラカラ」と聞こえるようにする
            Play(_pinFallClip, _hitVolume, UnityEngine.Random.Range(PinFallPitchMin, PinFallPitchMax));
        }

        private static void Play(AudioClip clip, float volume, float pitch)
        {
            if (clip == null || !AudioManager.HasInstance) return;

            AudioManager.Instance.PlaySeClip(clip, volume, pitch);
        }

        /// <summary>木同士が当たる「コン」。基音＋非整数倍の倍音にノイズのアタックを重ねる</summary>
        private static AudioClip CreateWood(string name, float frequency, float decay, float noise)
        {
            return CreateClip(name, WoodDuration, t =>
                Mathf.Sin(2f * Mathf.PI * frequency * t) * Mathf.Exp(-t * decay)
                + Mathf.Sin(2f * Mathf.PI * frequency * WoodOvertoneRatio * t) * WoodOvertoneGain
                    * Mathf.Exp(-t * decay * WoodOvertoneDecayRatio)
                + WhiteNoise() * noise * Mathf.Exp(-t * AttackNoiseDecay));
        }

        /// <summary>風切り音。ノイズの音量を山なりにして「シュッ」と聞かせる</summary>
        private static AudioClip CreateWhoosh()
        {
            const float duration = 0.3f;
            // 一次ローパスで高音の刺さりを抑える。小さいほどこもった音になる
            const float lowPassRate = 0.25f;
            float filtered = 0f;
            return CreateClip("Se_MkThrow", duration, t =>
            {
                filtered = Mathf.Lerp(filtered, WhiteNoise(), lowPassRate);
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
                // 矩形波を各音の頭だけ強めて、ゲームらしい歯切れのよい音色にする
                float wave = Mathf.Sin(2f * Mathf.PI * frequencies[note] * t);
                return Mathf.Sign(wave) * 0.25f * Mathf.Exp(-local * 10f) + wave * 0.3f;
            });
        }

        /// <summary>音程が下がっていく「ヒューン」。25点に戻るがっかり感を出す</summary>
        private static AudioClip CreateSlide(string name, float from, float to, float duration)
        {
            // 周波数が時間で変わるので、sin(2πft) ではなく位相を積算しないと音程が不自然に跳ねる
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
            const float frequency = 150f;
            return CreateClip("Se_MkDisqualified", beep * 2f + gap, t =>
            {
                bool silent = t > beep && t < beep + gap;
                if (silent) return 0f;

                return Mathf.Sign(Mathf.Sin(2f * Mathf.PI * frequency * t)) * 0.3f;
            });
        }

        private static float WhiteNoise()
        {
            return UnityEngine.Random.value * 2f - 1f;
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
