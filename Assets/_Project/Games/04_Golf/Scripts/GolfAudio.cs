using System;
using MiniGame.Common.Audio;
using UnityEngine;

namespace MiniGame.Golf
{
    /// <summary>
    /// ゴルフ固有のSE（ショット・カップイン・歓声・池・OB・ギブアップ）をプログラムで生成して鳴らす。
    /// 共通の SeId は特定のミニゲーム専用の音を持たないため、モルックと同じく生成したクリップを直接再生している。
    /// 正式な素材が用意できたら Inspector のクリップ欄に差し込めばそのまま置き換わる。
    /// </summary>
    public class GolfAudio : MonoBehaviour
    {
        private const int SampleRate = 44100;

        // 毎回同じ高さだと単調に聞こえるので、打球音のピッチを少しだけ揺らす幅
        private const float ShotPitchMin = 0.95f;
        private const float ShotPitchMax = 1.05f;

        /// <summary>パーの拍手は同じ歓声クリップを高めに鳴らし、バーディー以上の大歓声と聞き分けられるようにする</summary>
        private const float SmallCheerPitch = 1.2f;

        // 打球音。基音に対して非整数倍の倍音を足すと、木ではなく金属らしく聞こえる
        private const float HitDuration = 0.25f;
        private const float HitOvertoneRatio = 2.4f;
        private const float HitOvertoneGain = 0.25f;
        private const float HitOvertoneDecayRatio = 1.5f;

        /// <summary>打撃の最初だけノイズを乗せて「当たった瞬間」を強調するための減衰速度</summary>
        private const float AttackNoiseDecay = 200f;

        [SerializeField] private GolfBall _ball;
        [SerializeField] private ClubSelector _clubs;

        [Header("素材が用意できたら差し込む（未設定なら生成音を使う）")]
        [SerializeField] private AudioClip _shotClip;
        [SerializeField] private AudioClip _puttClip;
        [SerializeField] private AudioClip _cupInClip;
        [SerializeField] private AudioClip _cheerClip;
        [SerializeField] private AudioClip _splashClip;
        [SerializeField] private AudioClip _outOfBoundsClip;
        [SerializeField] private AudioClip _giveUpClip;

        [Header("Volume")]
        [Range(0f, 1f)] [SerializeField] private float _shotVolume = 0.8f;
        [Range(0f, 1f)] [SerializeField] private float _cupInVolume = 0.8f;
        [Tooltip("バーディー以上の歓声の大きさ")]
        [Range(0f, 1f)] [SerializeField] private float _bigCheerVolume = 0.7f;
        [Tooltip("パーの拍手の大きさ。バーディーとの差が分かるよう控えめにする")]
        [Range(0f, 1f)] [SerializeField] private float _smallCheerVolume = 0.25f;
        [Range(0f, 1f)] [SerializeField] private float _penaltyVolume = 0.6f;

        private void Awake()
        {
            // 生成は1回だけ。以降は同じクリップをピッチだけ変えて使い回す
            if (_shotClip == null) _shotClip = CreateHit("Se_GfShot", 1650f, 38f, 0.6f);
            if (_puttClip == null) _puttClip = CreateHit("Se_GfPutt", 820f, 70f, 0.15f);
            if (_cupInClip == null) _cupInClip = CreateCupIn();
            if (_cheerClip == null) _cheerClip = CreateCheer();
            if (_splashClip == null) _splashClip = CreateSplash();
            if (_outOfBoundsClip == null) _outOfBoundsClip = CreateBuzz();
            if (_giveUpClip == null) _giveUpClip = CreateSlide("Se_GfGiveUp", 600f, 200f, 0.6f);
        }

        private void OnEnable()
        {
            _ball.Launched += HandleLaunched;
            _ball.Penalized += HandlePenalized;
        }

        private void OnDisable()
        {
            _ball.Launched -= HandleLaunched;
            _ball.Penalized -= HandlePenalized;
        }

        /// <summary>カップインの「カラン」。パーなら拍手、バーディー以上なら大歓声を重ねる</summary>
        public void PlayHoleOut(int strokes, int par)
        {
            Play(_cupInClip, _cupInVolume, 1f);

            if (strokes < par || strokes == 1)
            {
                Play(_cheerClip, _bigCheerVolume, 1f);
            }
            else if (strokes == par)
            {
                Play(_cheerClip, _smallCheerVolume, SmallCheerPitch);
            }
        }

        public void PlayGiveUp()
        {
            Play(_giveUpClip, _penaltyVolume, 1f);
        }

        /// <summary>パターは「コツン」、それ以外は「カキーン」</summary>
        private void HandleLaunched()
        {
            bool isPutter = _clubs.Current.Config.IsPutter;
            Play(isPutter ? _puttClip : _shotClip, _shotVolume, UnityEngine.Random.Range(ShotPitchMin, ShotPitchMax));
        }

        private void HandlePenalized(GroundType ground)
        {
            Play(ground == GroundType.Water ? _splashClip : _outOfBoundsClip, _penaltyVolume, 1f);
        }

        private static void Play(AudioClip clip, float volume, float pitch)
        {
            if (clip == null || !AudioManager.HasInstance) return;

            AudioManager.Instance.PlaySeClip(clip, volume, pitch);
        }

        /// <summary>クラブがボールを打つ音。基音＋非整数倍の倍音にノイズのアタックを重ねる</summary>
        private static AudioClip CreateHit(string name, float frequency, float decay, float noise)
        {
            return CreateClip(name, HitDuration, t =>
                Mathf.Sin(2f * Mathf.PI * frequency * t) * 0.5f * Mathf.Exp(-t * decay)
                + Mathf.Sin(2f * Mathf.PI * frequency * HitOvertoneRatio * t) * HitOvertoneGain
                    * Mathf.Exp(-t * decay * HitOvertoneDecayRatio)
                + WhiteNoise() * noise * Mathf.Exp(-t * AttackNoiseDecay));
        }

        /// <summary>カップの中で2〜3回跳ねる「カラン、コロン」。間隔を詰めながら音を小さくする</summary>
        private static AudioClip CreateCupIn()
        {
            const float duration = 0.45f;
            const float bounceDecay = 25f;
            float[] starts = { 0f, 0.09f, 0.15f };
            float[] frequencies = { 1300f, 1100f, 1200f };
            return CreateClip("Se_GfCupIn", duration, t =>
            {
                float sum = 0f;
                for (int i = 0; i < starts.Length; i++)
                {
                    float local = t - starts[i];
                    if (local < 0f) continue;

                    float gain = 0.5f / (i + 1);
                    sum += Mathf.Sin(2f * Mathf.PI * frequencies[i] * local) * gain * Mathf.Exp(-local * bounceDecay);
                }

                return sum;
            });
        }

        /// <summary>
        /// 歓声と拍手。ローパスをかけたノイズ（ざわめき）に、ランダムな位置のパチパチ音（拍手）を重ねる。
        /// 山なりの音量にして「ワーッ」と盛り上がって引いていくように聞かせる
        /// </summary>
        private static AudioClip CreateCheer()
        {
            const float duration = 2f;
            const float clapRate = 0.004f;
            // 一次ローパスで高音を削り、ざわめきらしいこもった音にする。小さいほどこもる
            const float lowPassRate = 0.12f;
            // 1サンプルごとに掛ける減衰。拍手1回が短く「パチッ」と切れる長さにする
            const float clapDecay = 0.992f;
            float filtered = 0f;
            float clap = 0f;
            return CreateClip("Se_GfCheer", duration, t =>
            {
                filtered = Mathf.Lerp(filtered, WhiteNoise(), lowPassRate);
                if (UnityEngine.Random.value < clapRate) clap = 1f;
                clap *= clapDecay;

                float envelope = Mathf.Sin(Mathf.PI * Mathf.Sqrt(t / duration));
                float clapNoise = WhiteNoise() * clap * 0.5f;
                return (filtered * 0.8f + clapNoise) * envelope;
            });
        }

        /// <summary>水に落ちる「ボチャン」。低い音程が下がる音に、長めに引くノイズを重ねる</summary>
        private static AudioClip CreateSplash()
        {
            const float duration = 0.6f;
            const float plopDuration = 0.15f;
            const float plopFrequencyFrom = 400f;
            const float plopFrequencyTo = 120f;
            const float lowPassRate = 0.3f;
            const float noiseDecay = 6f;
            float phase = 0f;
            float filtered = 0f;
            return CreateClip("Se_GfSplash", duration, t =>
            {
                // 周波数が時間で変わるので、sin(2πft) ではなく位相を積算しないと音程が不自然に跳ねる
                phase += 2f * Mathf.PI * Mathf.Lerp(plopFrequencyFrom, plopFrequencyTo, t / plopDuration) / SampleRate;
                float plop = t < plopDuration ? Mathf.Sin(phase) * 0.5f * (1f - t / plopDuration) : 0f;
                filtered = Mathf.Lerp(filtered, WhiteNoise(), lowPassRate);
                return plop + filtered * 0.6f * Mathf.Exp(-t * noiseDecay);
            });
        }

        /// <summary>OBの「ブブー」。低い矩形波を2回鳴らす</summary>
        private static AudioClip CreateBuzz()
        {
            const float beep = 0.18f;
            const float gap = 0.07f;
            const float frequency = 160f;
            return CreateClip("Se_GfOutOfBounds", beep * 2f + gap, t =>
            {
                bool silent = t > beep && t < beep + gap;
                if (silent) return 0f;

                return Mathf.Sign(Mathf.Sin(2f * Mathf.PI * frequency * t)) * 0.3f;
            });
        }

        /// <summary>音程が下がっていく「ヒューン」。ギブアップのがっかり感を出す</summary>
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
