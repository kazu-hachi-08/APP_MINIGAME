namespace CardGame.AudioGen;

/// <summary>
/// 効果音。世界観(中世ファンタジー / 酒場の机の上で遊ぶ)に合わせ、
/// 「金属」「木」「紙」「土」の質感を中心にし、電子音は使わない。
/// </summary>
public static class Se
{
    public static void WriteAll(string dir)
    {
        Write(dir, "card_play", CardPlay());
        Write(dir, "card_draw", CardDraw());
        Write(dir, "attack", Attack());
        Write(dir, "attack_leader", AttackLeader());
        Write(dir, "summon", Summon());
        Write(dir, "destroy", Destroy());
        Write(dir, "spell", Spell());
        Write(dir, "heal", Heal());
        Write(dir, "buff", Buff());
        Write(dir, "turn_start", TurnStart());
        Write(dir, "awakening", Awakening());
        Write(dir, "button", Button());
        Write(dir, "win", Win());
        Write(dir, "lose", Lose());
    }

    private static void Write(string dir, string name, float[] buf)
    {
        Synth.FadeEdges(buf);
        Synth.Normalize(buf, 0.8);
        Synth.WriteWav(Path.Combine(dir, name + ".wav"), buf);
        Console.WriteLine($"se/{name}.wav  {buf.Length / (double)Synth.SampleRate:0.00}s");
    }

    /// <summary>カードを場に置く: 木の机に置く「コトッ」。</summary>
    private static float[] CardPlay()
    {
        var b = Synth.Empty(0.45);
        var rng = new Random(11);
        Synth.Drum(b, 0, 0.18, 0.5, 190, rng);          // 木の胴鳴り
        Synth.Paper(b, 0, 0.12, 0.35, rng);             // 紙が触れる音
        Synth.Reverb(b, 0.12, 0.25);
        return b;
    }

    /// <summary>ドロー: 紙がこすれる短い音。</summary>
    private static float[] CardDraw()
    {
        var b = Synth.Empty(0.35);
        Synth.Paper(b, 0, 0.22, 0.75, new Random(12));
        return b;
    }

    /// <summary>攻撃: 剣のぶつかり(金属)+ 風切り。</summary>
    private static float[] Attack()
    {
        var b = Synth.Empty(0.7);
        var rng = new Random(13);
        Synth.Whoosh(b, 0, 0.14, 0.35, 900, 2600, rng);
        Synth.Metal(b, (int)(0.10 * Synth.SampleRate), 0.5, 0.55, 520, rng);
        Synth.Drum(b, (int)(0.10 * Synth.SampleRate), 0.14, 0.35, 130, rng);
        Synth.Reverb(b, 0.2, 0.35);
        return b;
    }

    /// <summary>リーダーへの一撃: より低く重い。</summary>
    private static float[] AttackLeader()
    {
        var b = Synth.Empty(1.1);
        var rng = new Random(14);
        Synth.Whoosh(b, 0, 0.18, 0.4, 700, 1800, rng);
        Synth.Metal(b, (int)(0.12 * Synth.SampleRate), 0.7, 0.6, 330, rng);
        Synth.Drum(b, (int)(0.12 * Synth.SampleRate), 0.45, 0.8, 70, rng);
        Synth.Bell(b, (int)(0.13 * Synth.SampleRate), Synth.Note("A2"), 0.9, 0.18);
        Synth.Reverb(b, 0.3, 0.5);
        return b;
    }

    /// <summary>召喚: 低いドローンの立ち上がり + ハープの上昇 + 鐘。</summary>
    private static float[] Summon()
    {
        var b = Synth.Empty(1.3);
        var rng = new Random(15);
        Synth.Whoosh(b, 0, 0.5, 0.22, 200, 1400, rng);
        string[] notes = { "D3", "A3", "D4", "F4", "A4" };
        for (int i = 0; i < notes.Length; i++)
            Synth.Pluck(b, (int)(i * 0.055 * Synth.SampleRate), Synth.Note(notes[i]), 1.0, 0.5, 0.9965, 0.6, rng);
        Synth.Bell(b, (int)(0.26 * Synth.SampleRate), Synth.Note("D5"), 1.0, 0.16);
        Synth.Reverb(b, 0.32, 0.5);
        return b;
    }

    /// <summary>破壊: 鈍い衝撃 + 破片(木と石)。</summary>
    private static float[] Destroy()
    {
        var b = Synth.Empty(0.9);
        var rng = new Random(16);
        Synth.Drum(b, 0, 0.5, 0.85, 60, rng);
        Synth.Metal(b, (int)(0.02 * Synth.SampleRate), 0.35, 0.25, 220, rng);
        // 破片が散る
        for (int i = 0; i < 9; i++)
            Synth.Paper(b, (int)((0.05 + rng.NextDouble() * 0.35) * Synth.SampleRate), 0.06, 0.25, rng);
        Synth.Reverb(b, 0.25, 0.45);
        return b;
    }

    /// <summary>スペル: 魔力の走り + 着弾の鐘。</summary>
    private static float[] Spell()
    {
        var b = Synth.Empty(1.0);
        var rng = new Random(17);
        Synth.Whoosh(b, 0, 0.30, 0.3, 400, 3000, rng);
        Synth.Bell(b, (int)(0.24 * Synth.SampleRate), Synth.Note("A4"), 0.7, 0.2);
        Synth.Bell(b, (int)(0.26 * Synth.SampleRate), Synth.Note("E5"), 0.6, 0.12);
        Synth.Reverb(b, 0.3, 0.45);
        return b;
    }

    /// <summary>回復: 柔らかい鐘とハープ(長三度で明るく)。</summary>
    private static float[] Heal()
    {
        var b = Synth.Empty(1.2);
        var rng = new Random(18);
        string[] notes = { "F4", "A4", "C5" };
        for (int i = 0; i < notes.Length; i++)
            Synth.Pluck(b, (int)(i * 0.07 * Synth.SampleRate), Synth.Note(notes[i]), 1.0, 0.45, 0.997, 0.4, rng);
        Synth.Bell(b, (int)(0.1 * Synth.SampleRate), Synth.Note("F5"), 1.0, 0.12);
        Synth.Reverb(b, 0.35, 0.5);
        return b;
    }

    /// <summary>強化: 上昇する 3 音 + 金属の輝き。</summary>
    private static float[] Buff()
    {
        var b = Synth.Empty(0.9);
        var rng = new Random(19);
        string[] notes = { "D4", "F4", "A4", "D5" };
        for (int i = 0; i < notes.Length; i++)
            Synth.Pluck(b, (int)(i * 0.05 * Synth.SampleRate), Synth.Note(notes[i]), 0.7, 0.4, 0.995, 0.7, rng);
        Synth.Metal(b, (int)(0.15 * Synth.SampleRate), 0.4, 0.12, 1600, rng);
        Synth.Reverb(b, 0.25, 0.4);
        return b;
    }

    /// <summary>ターン開始: 太鼓 2 つ。</summary>
    private static float[] TurnStart()
    {
        var b = Synth.Empty(1.0);
        var rng = new Random(20);
        Synth.Drum(b, 0, 0.4, 0.8, 95, rng);
        Synth.Drum(b, (int)(0.16 * Synth.SampleRate), 0.5, 0.55, 78, rng);
        Synth.Reverb(b, 0.22, 0.4);
        return b;
    }

    /// <summary>覚醒の刻: 深い鐘 + 上昇する光(ハープ)+ ドローン。</summary>
    private static float[] Awakening()
    {
        var b = Synth.Empty(2.2);
        var rng = new Random(21);
        Synth.Bell(b, 0, Synth.Note("D3"), 2.0, 0.42);
        Synth.Bell(b, (int)(0.02 * Synth.SampleRate), Synth.Note("A3"), 1.8, 0.22);
        Synth.Drone(b, 0, Synth.Note("D2"), 2.0, 0.10);
        string[] notes = { "D4", "E4", "F4", "A4", "C5", "D5" };
        for (int i = 0; i < notes.Length; i++)
            Synth.Pluck(b, (int)((0.18 + i * 0.065) * Synth.SampleRate), Synth.Note(notes[i]), 1.4, 0.35, 0.997, 0.55, rng);
        Synth.Reverb(b, 0.4, 0.6);
        return b;
    }

    /// <summary>ボタン: 木を叩く短い音。</summary>
    private static float[] Button()
    {
        var b = Synth.Empty(0.2);
        Synth.Drum(b, 0, 0.09, 0.45, 320, new Random(22));
        return b;
    }

    /// <summary>勝利: 笛の短い節 + 太鼓。</summary>
    private static float[] Win()
    {
        var b = Synth.Empty(2.6);
        var rng = new Random(23);
        (string n, double t, double d)[] mel =
        {
            ("D4", 0.00, 0.22), ("F4", 0.22, 0.22), ("A4", 0.44, 0.30), ("D5", 0.74, 0.55), ("C5", 1.30, 0.25), ("D5", 1.55, 0.75),
        };
        foreach (var (n, t, d) in mel) Synth.Flute(b, (int)(t * Synth.SampleRate), Synth.Note(n), d, 0.4, rng);
        Synth.Drum(b, 0, 0.4, 0.5, 95, rng);
        Synth.Drum(b, (int)(0.74 * Synth.SampleRate), 0.5, 0.6, 85, rng);
        Synth.Drone(b, 0, Synth.Note("D3"), 2.3, 0.08);
        Synth.Reverb(b, 0.3, 0.5);
        return b;
    }

    /// <summary>敗北: 下降する笛 + 低いドローン。</summary>
    private static float[] Lose()
    {
        var b = Synth.Empty(2.8);
        var rng = new Random(24);
        (string n, double t, double d)[] mel = { ("A4", 0.0, 0.45), ("F4", 0.45, 0.45), ("D4", 0.9, 0.6), ("A3", 1.5, 1.1) };
        foreach (var (n, t, d) in mel) Synth.Flute(b, (int)(t * Synth.SampleRate), Synth.Note(n), d, 0.34, rng, 0.01);
        Synth.Drone(b, 0, Synth.Note("D2"), 2.6, 0.12);
        Synth.Drum(b, (int)(1.5 * Synth.SampleRate), 0.6, 0.35, 60, rng);
        Synth.Reverb(b, 0.35, 0.55);
        return b;
    }
}
