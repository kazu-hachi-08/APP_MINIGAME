using UnityEngine;

namespace MiniGame.Common.Audio
{
    /// <summary>
    /// SE素材が未用意の間、最低限の効果音をプログラムで生成して AudioManager へ登録する仮実装
    /// 正式なAudioClipを AudioManager に設定すればそちらが優先されるため、
    /// 素材が揃った時点でこのコンポーネントを外せばよい
    /// </summary>
    [RequireComponent(typeof(AudioManager))]
    public class ProceduralSe : MonoBehaviour
    {
        private const int SampleRate = 44100;

        private void Start()
        {
            var audioManager = GetComponent<AudioManager>();

            RegisterIfMissing(audioManager, SeId.ButtonClick, CreateTone("Se_Click", 0.06f, 880f, 40f), 0.4f);
            RegisterIfMissing(audioManager, SeId.Kick, CreateKick("Se_Kick"), 0.7f);
            RegisterIfMissing(audioManager, SeId.Whistle, CreateWhistle("Se_Whistle"), 0.4f);
            RegisterIfMissing(audioManager, SeId.GoalCheer, CreateCheer("Se_Cheer"), 0.6f);
            RegisterIfMissing(audioManager, SeId.GameClear, CreateJingle("Se_Clear", new[] { 523f, 659f, 784f }), 0.5f);
            RegisterIfMissing(audioManager, SeId.GameOver, CreateJingle("Se_GameOver", new[] { 392f, 311f, 247f }), 0.5f);
        }

        private static void RegisterIfMissing(AudioManager audioManager, SeId id, AudioClip clip, float volume)
        {
            if (audioManager.HasSe(id)) return;
            audioManager.RegisterSe(id, clip, volume);
        }

        /// <summary>減衰するサイン波（クリック音・ジングル用）</summary>
        private static AudioClip CreateTone(string name, float duration, float frequency, float decay)
        {
            return CreateClip(name, duration, t => Mathf.Sin(2f * Mathf.PI * frequency * t) * Mathf.Exp(-t * decay));
        }

        /// <summary>低音の打撃音にノイズを重ねたキック音</summary>
        private static AudioClip CreateKick(string name)
        {
            return CreateClip(name, 0.18f, t =>
                Mathf.Sin(2f * Mathf.PI * 150f * t) * Mathf.Exp(-t * 28f)
                + (Random.value * 2f - 1f) * 0.35f * Mathf.Exp(-t * 70f));
        }

        /// <summary>ビブラートをかけた高音のホイッスル</summary>
        private static AudioClip CreateWhistle(string name)
        {
            const float duration = 0.4f;
            return CreateClip(name, duration, t =>
            {
                float frequency = 2600f + 90f * Mathf.Sin(2f * Mathf.PI * 14f * t);
                float envelope = Mathf.Min(1f, t / 0.03f) * Mathf.Min(1f, (duration - t) / 0.06f);
                return Mathf.Sin(2f * Mathf.PI * frequency * t) * envelope * 0.5f;
            });
        }

        /// <summary>ローパスをかけたノイズを膨らませた歓声</summary>
        private static AudioClip CreateCheer(string name)
        {
            const float duration = 1.2f;
            int sampleCount = Mathf.CeilToInt(SampleRate * duration);
            var data = new float[sampleCount];
            float filtered = 0f;

            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)SampleRate;
                // 一次ローパスでホワイトノイズの刺さりを抑え、ざわめきに近づける
                filtered = Mathf.Lerp(filtered, Random.value * 2f - 1f, 0.15f);
                float envelope = Mathf.Sin(Mathf.PI * (t / duration));
                data[i] = filtered * envelope;
            }

            return CreateClipFromData(name, data);
        }

        /// <summary>指定した音程を順に鳴らす短いジングル</summary>
        private static AudioClip CreateJingle(string name, float[] frequencies)
        {
            const float noteDuration = 0.16f;
            int noteSamples = Mathf.CeilToInt(SampleRate * noteDuration);
            var data = new float[noteSamples * frequencies.Length];

            for (int note = 0; note < frequencies.Length; note++)
            {
                for (int i = 0; i < noteSamples; i++)
                {
                    float t = i / (float)SampleRate;
                    data[note * noteSamples + i] =
                        Mathf.Sin(2f * Mathf.PI * frequencies[note] * t) * Mathf.Exp(-t * 8f) * 0.6f;
                }
            }

            return CreateClipFromData(name, data);
        }

        private static AudioClip CreateClip(string name, float duration, System.Func<float, float> sampler)
        {
            int sampleCount = Mathf.CeilToInt(SampleRate * duration);
            var data = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                data[i] = sampler(i / (float)SampleRate);
            }

            return CreateClipFromData(name, data);
        }

        private static AudioClip CreateClipFromData(string name, float[] data)
        {
            var clip = AudioClip.Create(name, data.Length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
