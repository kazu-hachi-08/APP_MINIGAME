using System;
using MiniGame.Common.Audio;
using UnityEngine;

namespace MiniGame.LifeGame
{
    /// <summary>
    /// 人生ゲームのSE（ルーレットの回転・停止、コマの移動、お金の増減、給料日、結婚・出産、ゴール、勝利、精算演出）をプログラムで生成して鳴らす（仕様書 §3.4）。
    /// モルックの MolkkyAudio と同じく、正式な素材が用意できたら Inspector のクリップ欄に差し込めばそのまま置き換わる。
    /// </summary>
    public class LifeAudio : MonoBehaviour
    {
        private const int SampleRate = 44100;

        // 同じ音の繰り返しが機械的に聞こえないよう、ピッチを少しだけ揺らす幅
        private const float StepPitchMin = 0.92f;
        private const float StepPitchMax = 1.08f;

        [SerializeField] private RouletteView _roulette;

        [Header("素材が用意できたら差し込む（未設定なら生成音を使う）")]
        [SerializeField] private AudioClip _tickClip;
        [SerializeField] private AudioClip _stopClip;
        [SerializeField] private AudioClip _stepClip;
        [SerializeField] private AudioClip _gainClip;
        [SerializeField] private AudioClip _lossClip;
        [SerializeField] private AudioClip _paydayClip;
        [SerializeField] private AudioClip _familyClip;
        [SerializeField] private AudioClip _goalClip;
        [SerializeField] private AudioClip _victoryClip;
        [SerializeField] private AudioClip _janClip;
        [SerializeField] private AudioClip _drumrollClip;
        [SerializeField] private AudioClip _countClip;
        [SerializeField] private AudioClip _popClip;

        [Header("Volume")]
        [Range(0f, 1f)] [SerializeField] private float _tickVolume = 0.35f;
        [Range(0f, 1f)] [SerializeField] private float _stepVolume = 0.4f;
        [Range(0f, 1f)] [SerializeField] private float _moneyVolume = 0.5f;
        [Range(0f, 1f)] [SerializeField] private float _jingleVolume = 0.6f;

        [Tooltip("ルーレットのカチカチを鳴らす最短間隔（秒）。回り始めの速いところで音が重なって割れないようにする")]
        [SerializeField] private float _minTickInterval = 0.04f;

        [Tooltip("精算のカウントアップの音を鳴らす最短間隔（秒）。毎フレーム鳴らすと音が潰れるため")]
        [SerializeField] private float _minCountInterval = 0.06f;

        private float _lastTickTime = float.NegativeInfinity;
        private float _lastCountTime = float.NegativeInfinity;

        private void Awake()
        {
            // 生成は1回だけ。以降は同じクリップを使い回す
            if (_tickClip == null) _tickClip = CreateClick("Se_LifeTick", 1800f, 0.03f);
            if (_stopClip == null) _stopClip = CreateNotes("Se_LifeStop", new[] { 880f, 1320f }, 0.08f);
            if (_stepClip == null) _stepClip = CreateClick("Se_LifeStep", 520f, 0.08f);
            if (_gainClip == null) _gainClip = CreateNotes("Se_LifeGain", new[] { 988f, 1319f }, 0.07f);
            if (_lossClip == null) _lossClip = CreateNotes("Se_LifeLoss", new[] { 440f, 330f }, 0.12f);
            if (_paydayClip == null) _paydayClip = CreateNotes("Se_LifePayday", new[] { 523f, 659f, 784f, 1047f }, 0.08f);
            if (_familyClip == null) _familyClip = CreateBell("Se_LifeFamily", new[] { 1319f, 1568f, 2093f }, 0.14f);
            if (_goalClip == null) _goalClip = CreateNotes("Se_LifeGoal", new[] { 523f, 523f, 784f, 1047f }, 0.12f);
            if (_victoryClip == null) _victoryClip = CreateNotes("Se_LifeVictory", new[] { 523f, 659f, 784f, 1047f, 784f, 1047f }, 0.13f);
            if (_janClip == null) _janClip = CreateChord("Se_LifeJan", new[] { 523f, 659f, 784f, 1047f }, 0.8f);
            if (_drumrollClip == null) _drumrollClip = CreateDrumroll("Se_LifeDrumroll", 1.4f);
            if (_countClip == null) _countClip = CreateClick("Se_LifeCount", 1400f, 0.03f);
            if (_popClip == null) _popClip = CreateNotes("Se_LifePop", new[] { 784f, 1175f }, 0.05f);
        }

        private void OnEnable()
        {
            _roulette.Ticked += PlayTick;
            _roulette.Stopped += PlayStop;
        }

        private void OnDisable()
        {
            _roulette.Ticked -= PlayTick;
            _roulette.Stopped -= PlayStop;
        }

        public void PlayStep() => Play(_stepClip, _stepVolume, UnityEngine.Random.Range(StepPitchMin, StepPitchMax));

        public void PlayGain() => Play(_gainClip, _moneyVolume, 1f);

        public void PlayLoss() => Play(_lossClip, _moneyVolume, 1f);

        public void PlayPayday() => Play(_paydayClip, _jingleVolume, 1f);

        public void PlayFamily() => Play(_familyClip, _jingleVolume, 1f);

        public void PlayGoal() => Play(_goalClip, _jingleVolume, 1f);

        public void PlayVictory() => Play(_victoryClip, _jingleVolume, 1f);

        /// <summary>「けっさん！」のバナーや1位の発表など、場面の区切りを強調する和音</summary>
        public void PlayJan() => Play(_janClip, _jingleVolume, 1f);

        public void PlayDrumroll() => Play(_drumrollClip, _jingleVolume, 1f);

        /// <summary>称号カードの顔や順位の札が出たとき</summary>
        public void PlayPop() => Play(_popClip, _moneyVolume, 1f);

        /// <summary>数字が増えていく間に連続で呼ぶ。増えるときは高く、減るときは低くして向きを音でも伝える</summary>
        public void PlayCount(bool increasing)
        {
            if (Time.time - _lastCountTime < _minCountInterval) return;

            _lastCountTime = Time.time;
            Play(_countClip, _tickVolume, increasing ? 1.2f : 0.8f);
        }

        private void PlayTick()
        {
            if (Time.time - _lastTickTime < _minTickInterval) return;

            _lastTickTime = Time.time;
            Play(_tickClip, _tickVolume, 1f);
        }

        private void PlayStop() => Play(_stopClip, _jingleVolume, 1f);

        private static void Play(AudioClip clip, float volume, float pitch)
        {
            if (clip == null || !AudioManager.HasInstance) return;

            AudioManager.Instance.PlaySeClip(clip, volume, pitch);
        }

        /// <summary>短く減衰する「カチッ」「コトッ」。ルーレットの針とコマの移動に使う</summary>
        private static AudioClip CreateClick(string name, float frequency, float duration)
        {
            float decay = 5f / duration;
            return CreateClip(name, duration, t => Mathf.Sin(2f * Mathf.PI * frequency * t) * Mathf.Exp(-t * decay) * 0.6f);
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

        /// <summary>鐘の「キンコン」。倍音を足して余韻を長くし、結婚・出産のお祝いらしくする</summary>
        private static AudioClip CreateBell(string name, float[] frequencies, float noteDuration)
        {
            const float tail = 0.4f;
            const float overtoneRatio = 2.76f;
            const float overtoneGain = 0.25f;
            return CreateClip(name, noteDuration * frequencies.Length + tail, t =>
            {
                int note = Mathf.Min((int)(t / noteDuration), frequencies.Length - 1);
                float local = t - note * noteDuration;
                float f = frequencies[note];
                float envelope = Mathf.Exp(-local * 6f);
                return (Mathf.Sin(2f * Mathf.PI * f * t) + Mathf.Sin(2f * Mathf.PI * f * overtoneRatio * t) * overtoneGain)
                       * envelope * 0.4f;
            });
        }

        /// <summary>全部の音を同時に鳴らす「ジャン！」。頭にノイズを混ぜてシンバルのような打撃感を出す</summary>
        private static AudioClip CreateChord(string name, float[] frequencies, float duration)
        {
            const float noiseDecay = 25f;
            var random = new System.Random(1);
            return CreateClip(name, duration, t =>
            {
                float sum = 0f;
                foreach (float f in frequencies) sum += Mathf.Sin(2f * Mathf.PI * f * t);
                float tone = sum / frequencies.Length * Mathf.Exp(-t * 4f) * 0.6f;
                float noise = ((float)random.NextDouble() * 2f - 1f) * Mathf.Exp(-t * noiseDecay) * 0.4f;
                return tone + noise;
            });
        }

        /// <summary>
        /// 小刻みなノイズの連打をだんだん大きくするドラムロール。止められないので、1位の発表までの間に収まる長さにする
        /// </summary>
        private static AudioClip CreateDrumroll(string name, float duration)
        {
            const float hitsPerSecond = 22f;
            const float hitDecay = 40f;
            var random = new System.Random(2);
            return CreateClip(name, duration, t =>
            {
                float local = t % (1f / hitsPerSecond);
                float crescendo = Mathf.Lerp(0.25f, 0.7f, t / duration);
                return ((float)random.NextDouble() * 2f - 1f) * Mathf.Exp(-local * hitDecay) * crescendo;
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
