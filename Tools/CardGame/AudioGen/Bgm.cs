namespace CardGame.AudioGen;

/// <summary>
/// BGM。ケルト民謡のイメージ(D ドリアン旋法、笛 + ハープ + バウロン + ドローン)。
/// ループ前提なので、末尾を先頭にクロスフェードして継ぎ目を消す。
/// - menu:   落ち着いた「エア」(ゆったりした旋律)。酒場で待っている雰囲気
/// - battle: 6/8 のジグ。速すぎず、対戦中ずっと鳴っても疲れない音量バランス
/// </summary>
public static class Bgm
{
    public static void WriteAll(string dir)
    {
        Write(dir, "menu", Menu());
        Write(dir, "battle", Battle());
    }

    private static void Write(string dir, string name, float[] buf)
    {
        buf = Synth.LoopCrossfade(buf, 1.2);
        Synth.FadeEdges(buf, 0.02);
        Synth.Normalize(buf, 0.62);
        Synth.WriteWav(Path.Combine(dir, name + ".wav"), buf);
        Console.WriteLine($"bgm/{name}.wav  {buf.Length / (double)Synth.SampleRate:0.0}s");
    }

    // D ドリアン: D E F G A B C
    private static readonly string[] Scale4 = { "D4", "E4", "F4", "G4", "A4", "B4", "C5", "D5" };
    private static readonly string[] Scale3 = { "D3", "E3", "F3", "G3", "A3", "B3", "C4", "D4" };

    /// <summary>メニュー: ゆったりしたエア。ハープの分散和音の上を笛が歌う。</summary>
    private static float[] Menu()
    {
        const double bpm = 72, beat = 60.0 / bpm;
        double len = beat * 4 * 8 + 2.0;          // 8 小節 + 余韻
        var b = Synth.Empty(len);
        var rng = new Random(101);

        // ドローン(D と A)
        Synth.Drone(b, 0, Synth.Note("D2"), len - 0.5, 0.075);
        Synth.Drone(b, 0, Synth.Note("A2"), len - 0.5, 0.05);

        // ハープ: 各小節の和音を分散で
        string[][] chords =
        {
            new[] { "D3", "A3", "D4", "F4" },   // Dm
            new[] { "C3", "G3", "C4", "E4" },   // C
            new[] { "F3", "C4", "F4", "A4" },   // F
            new[] { "G3", "D4", "G4", "B4" },   // G
            new[] { "D3", "A3", "D4", "F4" },
            new[] { "C3", "G3", "C4", "E4" },
            new[] { "A3", "E4", "A4", "C5" },   // Am
            new[] { "D3", "A3", "D4", "F4" },
        };
        for (int bar = 0; bar < 8; bar++)
        {
            var ch = chords[bar];
            for (int n = 0; n < 8; n++)
            {
                double t = bar * beat * 4 + n * beat * 0.5;
                var note = ch[n % ch.Length];
                Synth.Pluck(b, (int)(t * Synth.SampleRate), Synth.Note(note), 1.6, n % 4 == 0 ? 0.30 : 0.20, 0.9975, 0.45, rng);
            }
        }

        // 笛の旋律(2 小節 × 4 フレーズ)
        (int deg, double t, double d)[] melody =
        {
            (0, 0.0, 1.0), (2, 1.0, 0.5), (4, 1.5, 0.5), (3, 2.0, 1.0), (2, 3.0, 1.0),
            (4, 4.0, 1.0), (6, 5.0, 0.5), (7, 5.5, 0.5), (6, 6.0, 1.5),
            (5, 8.0, 1.0), (4, 9.0, 0.5), (3, 9.5, 0.5), (2, 10.0, 1.0), (0, 11.0, 1.0),
            (2, 12.0, 1.0), (4, 13.0, 1.0), (3, 14.0, 0.5), (2, 14.5, 0.5), (0, 15.0, 1.5),
        };
        foreach (var (deg, t, d) in melody)
            Synth.Flute(b, (int)(t * beat * 2 * Synth.SampleRate), Synth.Note(Scale4[deg]), d * beat * 2 * 0.95, 0.30, rng);

        Synth.Reverb(b, 0.33, 0.55);
        return b;
    }

    /// <summary>対戦: 6/8 のジグ。太鼓が刻み、ハープと笛が掛け合う(A-A-B-B の 32 小節)。</summary>
    private static float[] Battle()
    {
        const double bpm = 96, beat = 60.0 / bpm;        // 付点 4 分 = 1 拍(6/8)
        const int bars = 32;
        double len = beat * bars + 2.5;
        var b = Synth.Empty(len);
        var rng = new Random(202);

        Synth.Drone(b, 0, Synth.Note("D2"), len - 0.5, 0.07);
        Synth.Drone(b, 0, Synth.Note("A2"), len - 0.5, 0.04);

        // バウロン: 6/8 の「ダン・タ・タ ダン・タ・タ」。8 小節ごとに少し強くする
        for (int bar = 0; bar < bars; bar++)
        {
            double t0 = bar * beat;
            double accent = (bar % 8 == 0) ? 0.75 : 0.55;
            Synth.Drum(b, (int)(t0 * Synth.SampleRate), 0.35, accent, 82, rng);
            Synth.Drum(b, (int)((t0 + beat / 3) * Synth.SampleRate), 0.15, 0.16, 150, rng);
            Synth.Drum(b, (int)((t0 + beat * 2 / 3) * Synth.SampleRate), 0.15, 0.20, 140, rng);
        }

        // ハープの伴奏(和音を 3 連で刻む)
        string[][] chords =
        {
            new[] { "D3", "A3", "F4" }, new[] { "D3", "A3", "F4" },
            new[] { "C3", "G3", "E4" }, new[] { "C3", "G3", "E4" },
            new[] { "A3", "E4", "C5" }, new[] { "A3", "E4", "C5" },
            new[] { "G3", "D4", "B4" }, new[] { "D3", "A3", "F4" },
        };
        for (int bar = 0; bar < bars; bar++)
        {
            var ch = chords[bar % chords.Length];
            for (int n = 0; n < 3; n++)
            {
                double t = bar * beat + n * beat / 3;
                Synth.Pluck(b, (int)(t * Synth.SampleRate), Synth.Note(ch[n % ch.Length]), 0.9, n == 0 ? 0.26 : 0.15, 0.996, 0.55, rng);
            }
        }

        // 笛: 4 小節 = 12 音のフレーズ。A A' B B' の順に 8 回並べて 32 小節
        int[] a1 = { 0, 2, 4, 3, 2, 0, 2, 4, 5, 4, 2, 0 };
        int[] a2 = { 0, 2, 4, 3, 2, 0, 4, 2, 0, 2, 4, 5 };
        int[] b1 = { 4, 5, 6, 5, 4, 2, 4, 6, 7, 6, 4, 2 };
        int[] b2 = { 7, 6, 5, 4, 5, 6, 4, 2, 4, 3, 2, 0 };
        int[][] order = { a1, a2, b1, b2, a1, a2, b1, b2 };
        for (int seg = 0; seg < order.Length; seg++)
        {
            var phrase = order[seg];
            bool low = seg == 1 || seg == 5;             // 2 回目は低い音域で応答して単調さを避ける
            for (int i = 0; i < phrase.Length; i++)
            {
                double t = (seg * 4) * beat + i * beat / 3;
                var name = (low ? Scale3 : Scale4)[phrase[i]];
                Synth.Flute(b, (int)(t * Synth.SampleRate), Synth.Note(name), beat / 3 * 0.92, low ? 0.22 : 0.26, rng, 0.004);
            }
        }

        Synth.Reverb(b, 0.28, 0.45);
        return b;
    }
}
