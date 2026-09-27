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
                Play(_cheerClip, _smallCheerVolume, 1.2f);
            }
        }

        public void PlayGiveUp()
        {
            Play(_giveUpClip, _penaltyVolume, 1f);
        }

        /// <summary>パターは「コツン」、それ以外は「カキーン」。毎回少し高さを変えて単調にしない</summary>
        private void HandleLaunched()
        {
            bool isPutter = _clubs.Current.Config.IsPutter;
            Play(isPutter ? _puttClip : _shotClip, _shotVolume, UnityEngine.Random.Range(0.95f, 1.05f));
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

        /// <summary>クラブがボールを打つ音。基音＋2.4倍の倍音（金属らしい非整数倍）にノイズのアタックを重ねる</summary>
        private static AudioClip CreateHit(string name, float frequency, float decay, float noise)
        {
            return CreateClip(name, 0.25f, t =>
                Mathf.Sin(2f * Mathf.PI * frequency * t) * 0.5f * Mathf.Exp(-t * decay)
                + Mathf.Sin(2f * Mathf.PI * frequency * 2.4f * t) * 0.25f * Mathf.Exp(-t * decay * 1.5f)
                + (UnityEngine.Random.value * 2f - 1f) * noise * Mathf.Exp(-t * 200f));
        }

        /// <summary>カップの中で2〜3回跳ねる「カラン、コロン」。間隔を詰めながら音を小さくする</summary>
        private static AudioClip CreateCupIn()
        {
            float[] starts = { 0f, 0.09f, 0.15f };
            float[] frequencies = { 1300f, 1100f, 1200f };
            return CreateClip("Se_GfCupIn", 0.45f, t =>
            {
                float sum = 0f;
                for (int i = 0; i < starts.Length; i++)
                {
                    float local = t - starts[i];
                    if (local < 0f) continue;

                    float gain = 0.5f / (i + 1);
                    sum += Mathf.Sin(2f * Mathf.PI * frequencies[i] * local) * gain * Mathf.Exp(-local * 25f);
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
            float filtered = 0f;
            float clap = 0f;
            return CreateClip("Se_GfCheer", duration, t =>
            {
                filtered = Mathf.Lerp(filtered, UnityEngine.Random.value * 2f - 1f, 0.12f);
                if (UnityEngine.Random.value < clapRate) clap = 1f;
                clap *= 0.992f;

                float envelope = Mathf.Sin(Mathf.PI * Mathf.Sqrt(t / duration));
                float clapNoise = (UnityEngine.Random.value * 2f - 1f) * clap * 0.5f;
                return (filtered * 0.8f + clapNoise) * envelope;
            });
        }

        /// <summary>水に落ちる「ボチャン」。低い音程が下がる音に、長めに引くノイズを重ねる</summary>
        private static AudioClip CreateSplash()
        {
            const float duration = 0.6f;
            float phase = 0f;
            float filtered = 0f;
            return CreateClip("Se_GfSplash", duration, t =>
            {
                phase += 2f * Mathf.PI * Mathf.Lerp(400f, 120f, t / 0.15f) / SampleRate;
                float plop = t < 0.15f ? Mathf.Sin(phase) * 0.5f * (1f - t / 0.15f) : 0f;
                filtered = Mathf.Lerp(filtered, UnityEngine.Random.value * 2f - 1f, 0.3f);
                return plop + filtered * 0.6f * Mathf.Exp(-t * 6f);
            });
        }

        /// <summary>OBの「ブブー」。低い矩形波を2回鳴らす</summary>
        private static AudioClip CreateBuzz()
        {
            const float beep = 0.18f;
            const float gap = 0.07f;
            return CreateClip("Se_GfOutOfBounds", beep * 2f + gap, t =>
            {
                bool silent = t > beep && t < beep + gap;
                if (silent) return 0f;

                return Mathf.Sign(Mathf.Sin(2f * Mathf.PI * 160f * t)) * 0.3f;
            });
        }

        /// <summary>音程が下がっていく「ヒューン」。ギブアップのがっかり感を出す</summary>
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
