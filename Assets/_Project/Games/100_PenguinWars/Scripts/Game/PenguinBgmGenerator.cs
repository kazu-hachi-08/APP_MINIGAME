using UnityEngine;

namespace MiniGame.PenguinWars
{
    /// <summary>
    /// BGM素材が無いときに鳴らす仮のBGM（仕様書 §9）。矩形波のメロディ・三角波のベース・ノイズのハイハットで作るループ曲。
    /// 楽譜は8分音符1つを1マスとして並べ、最後のマスまで鳴り終わる作りにしてループのつなぎ目で音が切れないようにする
    /// </summary>
    public static class PenguinBgmGenerator
    {
        // BGMは長いので、SEの半分のサンプルレートにしてメモリと生成時間を抑える（ピコピコ音なので劣化は気にならない）
        private const int SampleRate = 22050;
        private const int Rest = -1;
        private const int Hold = -2;
        private const int StepsPerHalfBar = 4;

        private const float AttackSeconds = 0.005f;
        private const float ReleaseSeconds = 0.03f;
        private const float MelodySustain = 0.55f;
        private const float MelodyDecayRate = 8f;
        private const float HatSeconds = 0.04f;
        private const float KickSeconds = 0.12f;

        // ボスがいる間の曲は戦闘曲を速く・低くしたもの。別の曲を作らずに「山場」を伝える
        private const float BossTempoRate = 1.2f;
        private const int BossTransposeSemitones = -3;

        /// <summary>のんびりした明るい行進曲（ハ長調）</summary>
        private static readonly Score Title = new Score
        {
            Bpm = 112f,
            Melody = new[]
            {
                76, 76, 79, Hold, 76, Hold, 72, Hold,
                74, 74, 79, Hold, 74, Hold, 71, Hold,
                72, 74, 76, Hold, 81, Hold, 79, Hold,
                77, Hold, 76, Hold, 74, Hold, Rest, Rest,
                76, 76, 79, Hold, 84, Hold, 79, Hold,
                77, 76, 74, Hold, 79, Hold, 74, Hold,
                77, Hold, 81, Hold, 79, Hold, 74, Hold,
                72, Hold, Hold, Hold, Rest, Rest, Rest, Rest,
            },
            // 半小節ごとのベースの根音（C C G G Am Am F F C C G G F G C C）
            BassRoots = new[] { 48, 48, 43, 43, 45, 45, 41, 41, 48, 48, 43, 43, 41, 43, 48, 48 },
            DrivingBass = false,
            MelodyVolume = 0.16f,
            BassVolume = 0.3f,
            HatVolume = 0.05f,
            KickVolume = 0f,
        };

        /// <summary>テンポの速い戦闘曲（イ短調）。ベースを8分で刻み、キックで前に進む感じを出す</summary>
        private static readonly Score Battle = new Score
        {
            Bpm = 150f,
            Melody = new[]
            {
                81, Hold, 76, Hold, 81, 83, 84, Hold,
                83, Hold, 81, Hold, 77, Hold, Hold, Hold,
                79, Hold, 74, Hold, 79, 81, 83, Hold,
                80, Hold, 76, Hold, 71, Hold, Hold, Hold,
                81, Hold, 84, Hold, 88, Hold, 84, Hold,
                86, Hold, 84, Hold, 81, Hold, 77, Hold,
                79, 81, 83, Hold, 86, Hold, 83, Hold,
                80, Hold, 83, Hold, 88, Hold, Rest, Rest,
            },
            // 半小節ごとのベースの根音（Am F G E を2周）
            BassRoots = new[] { 45, 45, 41, 41, 43, 43, 40, 40, 45, 45, 41, 41, 43, 43, 40, 40 },
            DrivingBass = true,
            MelodyVolume = 0.14f,
            BassVolume = 0.28f,
            HatVolume = 0.06f,
            KickVolume = 0.4f,
        };

        private class Score
        {
            public float Bpm;
            /// <summary>MIDIノート番号（69 = ラ 440Hz）。Rest は休み、Hold は前の音を伸ばす</summary>
            public int[] Melody;
            public int[] BassRoots;
            /// <summary>true なら根音とオクターブ上を8分で刻む、false なら根音と5度を4分で鳴らす</summary>
            public bool DrivingBass;
            public float MelodyVolume;
            public float BassVolume;
            public float HatVolume;
            public float KickVolume;
        }

        public static AudioClip CreateTitle() => Create("Bgm_PenguinTitle", Title);

        public static AudioClip CreateBattle() => Create("Bgm_PenguinBattle", Battle);

        public static AudioClip CreateBoss() => Create("Bgm_PenguinBoss", Variant(Battle, BossTempoRate, BossTransposeSemitones));

        private static Score Variant(Score source, float tempoRate, int semitones)
        {
            return new Score
            {
                Bpm = source.Bpm * tempoRate,
                Melody = Transpose(source.Melody, semitones),
                BassRoots = Transpose(source.BassRoots, semitones),
                DrivingBass = source.DrivingBass,
                MelodyVolume = source.MelodyVolume,
                BassVolume = source.BassVolume,
                HatVolume = source.HatVolume,
                KickVolume = source.KickVolume,
            };
        }

        /// <summary>Rest・Hold（負の値）は音ではないのでそのまま残す</summary>
        private static int[] Transpose(int[] notes, int semitones)
        {
            var shifted = new int[notes.Length];
            for (int i = 0; i < notes.Length; i++) shifted[i] = notes[i] < 0 ? notes[i] : notes[i] + semitones;
            return shifted;
        }

        private static AudioClip Create(string name, Score score)
        {
            // 8分音符1つぶんの秒数
            float stepSeconds = 60f / score.Bpm / 2f;
            int stepSamples = Mathf.RoundToInt(stepSeconds * SampleRate);
            var data = new float[stepSamples * score.Melody.Length];

            AddVoice(data, score.Melody, stepSamples, score.MelodyVolume, false);
            AddVoice(data, BuildBass(score), stepSamples, score.BassVolume, true);
            AddDrums(data, score, stepSamples);

            for (int i = 0; i < data.Length; i++) data[i] = Mathf.Clamp(data[i], -1f, 1f);
            var clip = AudioClip.Create(name, data.Length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static int[] BuildBass(Score score)
        {
            int[] pattern = score.DrivingBass
                ? new[] { 0, 12, 0, 12 }      // 根音・オクターブ上を8分で
                : new[] { 0, Hold, 7, Hold }; // 根音・5度を4分で
            var bass = new int[score.BassRoots.Length * StepsPerHalfBar];
            for (int i = 0; i < bass.Length; i++)
            {
                int offset = pattern[i % StepsPerHalfBar];
                bass[i] = offset == Hold ? Hold : score.BassRoots[i / StepsPerHalfBar] + offset;
            }
            return bass;
        }

        /// <summary>楽譜を音ごとに区切り、Hold の長さぶん伸ばして鳴らす</summary>
        private static void AddVoice(float[] data, int[] notes, int stepSamples, float volume, bool isBass)
        {
            for (int start = 0; start < notes.Length; start++)
            {
                if (notes[start] < 0) continue;

                int length = 1;
                while (start + length < notes.Length && notes[start + length] == Hold) length++;
                AddNote(data, start * stepSamples, length * stepSamples, MidiToFrequency(notes[start]), volume, isBass);
            }
        }

        private static void AddNote(float[] data, int offset, int sampleCount, float frequency, float volume, bool isBass)
        {
            float duration = sampleCount / (float)SampleRate;
            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)SampleRate;
                float phase = frequency * t % 1f;
                // ベースは丸い三角波、メロディは歯切れのよい矩形波
                float wave = isBass ? 4f * Mathf.Abs(phase - 0.5f) - 1f : (phase < 0.5f ? 1f : -1f);
                // 立ち上がりと終わりをなだらかにして、音の切れ目のプツッというノイズを消す
                float envelope = Mathf.Min(1f, t / AttackSeconds) * Mathf.Min(1f, (duration - t) / ReleaseSeconds);
                if (!isBass) envelope *= MelodySustain + (1f - MelodySustain) * Mathf.Exp(-t * MelodyDecayRate);
                data[offset + i] += wave * envelope * volume;
            }
        }

        /// <summary>ハイハットは裏拍（戦闘曲は毎8分）、キックは4分ごと</summary>
        private static void AddDrums(float[] data, Score score, int stepSamples)
        {
            var random = new System.Random(3);
            for (int step = 0; step < score.Melody.Length; step++)
            {
                int offset = step * stepSamples;
                bool isOffBeat = step % 2 == 1;
                if (score.DrivingBass || isOffBeat) AddHat(data, offset, random, score.HatVolume);
                if (score.KickVolume > 0f && !isOffBeat) AddKick(data, offset, score.KickVolume);
            }
        }

        private static void AddHat(float[] data, int offset, System.Random random, float volume)
        {
            int count = Mathf.RoundToInt(HatSeconds * SampleRate);
            count = Mathf.Min(count, data.Length - offset);
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;
                float noise = (float)random.NextDouble() * 2f - 1f;
                data[offset + i] += noise * Mathf.Exp(-t * 120f) * volume;
            }
        }

        /// <summary>音程が急に下がる低いサイン波で「ドッ」という太鼓にする</summary>
        private static void AddKick(float[] data, int offset, float volume)
        {
            int count = Mathf.RoundToInt(KickSeconds * SampleRate);
            count = Mathf.Min(count, data.Length - offset);
            float phase = 0f;
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;
                float frequency = 50f + 100f * Mathf.Exp(-t * 30f);
                phase += 2f * Mathf.PI * frequency / SampleRate;
                data[offset + i] += Mathf.Sin(phase) * Mathf.Exp(-t * 25f) * volume;
            }
        }

        private static float MidiToFrequency(int note) => 440f * Mathf.Pow(2f, (note - 69) / 12f);
    }
}
