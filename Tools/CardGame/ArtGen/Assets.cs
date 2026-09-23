using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

/// <summary>
/// カード以外の画像素材(机の背景・カード枠)を Unity 用に整える。
///   dotnet run --project Tools/ArtGen -- table <入力画像>            → Resources/Field/table.jpg(2048×1024)
///   dotnet run --project Tools/ArtGen -- table --generate            → pollinations で仮の机を生成して同上
///   dotnet run --project Tools/ArtGen -- frame <入力画像> <class>    → Resources/Frames/frame_<class>.png(512×732、中央と外側の黒を透過)
/// </summary>
static class AssetTools
{
    public const int TableW = 2048, TableH = 1024; // POT(crunch 圧縮のため)。画面では 16:9 に切り抜かれる
    public const int FrameW = 512, FrameH = 732;   // 7:10、4 の倍数(DXT5)
    // 揃えた開口部の位置(カードに対する比)。Unity 側の CardView も同じ値を使う
    public const float OpenL = 0.085f, OpenR = 0.915f, OpenT = 0.070f, OpenB = 0.930f;

    public static async Task<int> RunAsync(List<string> args, string repoRoot, HttpClient http)
    {
        switch (args[0])
        {
            case "table":
            {
                var outPath = Path.Combine(repoRoot, "Unity", "Assets", "Resources", "Field", "table.jpg");
                Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);
                byte[] input;
                if (args.Contains("--generate"))
                {
                    const string prompt = "top-down view of a heavy dark oak tavern table in a fantasy world, rich wood grain, carved edges with brass fittings, " +
                                          "warm candlelight from the left and right edges, empty table surface, realistic digital painting, warm muted dark tones, no text, no logo";
                    int seed = int.TryParse(Opt(args, "--seed"), out var sd) ? sd : 7;
                    Console.WriteLine($"pollinations で机を生成(seed={seed})...");
                    input = await Pollinations.GenerateAsync(http, prompt, seed, 1280, 720);
                }
                else if (args.Count >= 2 && File.Exists(args[1])) input = await File.ReadAllBytesAsync(args[1]);
                else { Console.Error.WriteLine("使い方: table <入力画像> | table --generate [--seed N]"); return 2; }
                await File.WriteAllBytesAsync(outPath, ToTable(input));
                Console.WriteLine($"OK: {outPath}");
                return 0;
            }
            case "frame":
            {
                if (args.Count < 3 || !File.Exists(args[1])) { Console.Error.WriteLine("使い方: frame <入力画像> <neutral|knight|mage|necromancer|druid|dragon>"); return 2; }
                var cls = args[2].ToLowerInvariant();
                var outPath = Path.Combine(repoRoot, "Unity", "Assets", "Resources", "Frames", $"frame_{cls}.png");
                Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);
                int threshold = int.TryParse(Opt(args, "--threshold"), out var th) ? th : 40;
                await File.WriteAllBytesAsync(outPath, ToFrame(await File.ReadAllBytesAsync(args[1]), threshold));
                Console.WriteLine($"OK: {outPath}");
                return 0;
            }
            case "badge":
            {
                // バッジ(数値の土台)。--generate で候補を生成(--gem ruby|emerald|sapphire、--style N、--seed N)、
                // または <入力画像> を整形(円形に切り抜き、--recolor emerald|sapphire で赤い宝石の色を変える)。--out DIR、--name NAME
                var outDir = Opt(args, "--out") ?? Path.Combine(repoRoot, "Unity", "Assets", "Resources", "Badges");
                Directory.CreateDirectory(outDir);
                byte[] input;
                string name;
                if (args.Contains("--generate"))
                {
                    var gem = Opt(args, "--gem") ?? "ruby";
                    int style = int.TryParse(Opt(args, "--style"), out var st) ? st : 0;
                    int seed = int.TryParse(Opt(args, "--seed"), out var sd2) ? sd2 : 1;
                    string gemText = gem switch
                    {
                        "emerald" => "a polished round emerald gem",
                        "sapphire" => "a polished round light blue sapphire gem",
                        _ => "a polished round ruby gem",
                    };
                    string[] styles =
                    {
                        $"ornate baroque gold medallion, intricate filigree and engraved scrollwork, {gemText} set in the center, dark fantasy game UI badge icon, perfectly circular, symmetrical, centered, photorealistic 3d render, studio lighting, isolated on pure black background, no text",
                        $"antique bronze and gold round emblem with gothic engraving, small rivets, worn metal, {gemText} cabochon in the center, dark fantasy trading card game stat badge, centered, symmetrical, photorealistic, isolated on pure black background, no text",
                        $"heraldic round shield badge made of blackened steel with gold inlay and celtic knotwork, {gemText} in the center, dark fantasy card game icon, centered, symmetrical, photorealistic render, isolated on pure black background, no text",
                        $"circular gem setting with claw prongs, gold bezel with tiny engraved runes, {gemText}, viewed from above, game UI icon, centered, symmetrical, photorealistic, isolated on pure black background, no text",
                        $"round game UI stat badge: a large polished {gemText.Replace("a polished round ", "")} filling most of the circle, surrounded by a thin ornate engraved gold rim with tiny filigree, dark fantasy, photorealistic 3d render, centered, symmetrical, isolated on pure black background, no text",
                        $"large faceted {gemText.Replace("a polished round ", "")} cabochon set in a slim antique gold bezel with claw prongs and delicate engraving, top-down view, dark fantasy card game icon, photorealistic, centered, symmetrical, isolated on pure black background, no text",
                    };
                    Console.WriteLine($"badge {gem} style={style} seed={seed} ...");
                    input = await Pollinations.GenerateAsync(http, styles[Math.Clamp(style, 0, styles.Length - 1)], seed, 768, 768);
                    name = Opt(args, "--name") ?? $"badge_{gem}_s{style}_{seed}";
                    await File.WriteAllBytesAsync(Path.Combine(outDir, name + "_raw.png"), input); // 色替え用に元画像も残す
                }
                else if (args.Count >= 2 && File.Exists(args[1]))
                {
                    input = await File.ReadAllBytesAsync(args[1]);
                    name = Opt(args, "--name") ?? Path.GetFileNameWithoutExtension(args[1]);
                }
                else { Console.Error.WriteLine("使い方: badge --generate [--gem G] [--style N] [--seed N] | badge <入力画像> [--recolor emerald|sapphire] [--name NAME] [--out DIR]"); return 2; }
                var outPath = Path.Combine(outDir, name + ".png");
                float hole = float.TryParse(Opt(args, "--hole"), out var hv) ? hv : 0f;
                await File.WriteAllBytesAsync(outPath, ToBadge(input, Opt(args, "--recolor"), hole));
                Console.WriteLine($"OK: {outPath}");
                return 0;
            }
            case "assets":
            {
                // assets.json に並べた UI 素材をローカル ComfyUI でまとめて生成する。
                //   dotnet run --project Tools/ArtGen -- assets [--only Frames/frame_knight,...] [--force] [--out DIR] [--seed-offset N]
                if (!await ComfyLocal.IsUpAsync(http)) { Console.Error.WriteLine("ComfyUI が起動していない。scripts/comfy-start.ps1 で起動する"); return 2; }
                var specPath = Path.Combine(repoRoot, "Tools", "ArtGen", "assets.json");
                var spec = Newtonsoft.Json.Linq.JObject.Parse(await File.ReadAllTextAsync(specPath));
                var baseDir = Opt(args, "--out") ?? Path.Combine(repoRoot, "Unity", "Assets", "Resources");
                var only = Opt(args, "--only")?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToHashSet(StringComparer.OrdinalIgnoreCase);
                bool force = args.Contains("--force");
                int seedOffset = int.TryParse(Opt(args, "--seed-offset"), out var so2) ? so2 : 0;
                int ok = 0, fail = 0;
                foreach (var item in (Newtonsoft.Json.Linq.JArray)spec["items"]!)
                {
                    var name = item["name"]!.ToString();
                    if (only != null && !only.Contains(name)) continue;
                    var post = item["post"]!.ToString();
                    var ext = post is "table" or "card" or "full" ? ".jpg" : ".png";
                    var outPath = Path.Combine(baseDir, name.Replace('/', Path.DirectorySeparatorChar) + ext);
                    Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);
                    if (!force && File.Exists(outPath)) { Console.WriteLine($"{name} ... 既にある(スキップ)"); continue; }
                    Console.Write($"{name} ... ");
                    try
                    {
                        // "model": "turbo" の素材はイラスト寄りのモデル(ハースストーン風)で描く
                        bool turbo = (string?)item["model"] == "turbo";
                        if (turbo) ComfyLocal.UseTurbo(); else ComfyLocal.UseReal();
                        var png = await ComfyLocal.GenerateAsync(http, item["prompt"]!.ToString(), turbo ? ComfyLocal.NegativeHsUi : ComfyLocal.Negative,
                            (int)item["seed"]! + seedOffset, (int)item["width"]!, (int)item["height"]!);
                        byte[] outBytes = post switch
                        {
                            "table" => ToTable(png),
                            "frame" => ToFrame(png, 40),
                            "circle" => ToBadge(png, null, 0f, item["outSize"] != null ? (int)item["outSize"]! : 256),
                            "card" => ToPlate(png, (int?)item["outWidth"] ?? 512, (int?)item["outHeight"] ?? 732),
                            "full" => ToFull(png, (int)item["outWidth"]!, (int)item["outHeight"]!),
                            "cutout" => ToCutout(png, (int)item["outWidth"]!, (int)item["outHeight"]!, (bool?)item["hole"] == true, (string?)item["recolor"], (string?)item["holeShape"] == "rect"),
                            _ => ToPlate(png, (int)item["outWidth"]!, (int)item["outHeight"]!),
                        };
                        await File.WriteAllBytesAsync(outPath, outBytes);
                        Console.WriteLine($"OK ({outBytes.Length / 1024} KB)");
                        ok++;
                    }
                    catch (Exception ex) { Console.WriteLine("失敗: " + ex.Message); fail++; }
                }
                Console.WriteLine($"完了: 成功 {ok} / 失敗 {fail}");
                return fail == 0 ? 0 : 1;
            }
            default:
                return -1; // 素材コマンドではない
        }
    }

    static string? Opt(List<string> args, string name) { int i = args.IndexOf(name); return i >= 0 && i + 1 < args.Count ? args[i + 1] : null; }

    /// <summary>机の画像を 2048×1024 に「中央を覆う(cover)」形で切り抜いて JPEG 化する。</summary>
    public static byte[] ToTable(byte[] input)
    {
        using var src = Image.FromStream(new MemoryStream(input));
        using var bmp = new Bitmap(TableW, TableH);
        using (var g = Graphics.FromImage(bmp))
        {
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            // 16:9 の絵を 2:1 に収める: 上下を少し切る
            float scale = Math.Max((float)TableW / src.Width, (float)TableH / src.Height);
            float dw = src.Width * scale, dh = src.Height * scale;
            g.DrawImage(src, new RectangleF((TableW - dw) / 2, (TableH - dh) / 2, dw, dh));
        }
        return Jpeg(bmp, 90L);
    }

    /// <summary>
    /// 枠画像を透過 PNG にする。
    /// 1) 画像の外側の黒(背景)を flood fill で透過
    /// 2) 枠の内側の開口部を検出して透過(生成 AI は「中央を黒で」という指示に従わないことが多いため、
    ///    中央付近の「模様の少ない平坦な領域」を開口部とみなす)
    /// 出力は 512×732。
    /// </summary>
    public static byte[] ToFrame(byte[] input, int threshold)
    {
        using var src = new Bitmap(Image.FromStream(new MemoryStream(input)));
        int w = src.Width, h = src.Height;
        var px = new Color[w * h];
        for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) px[y * w + x] = src.GetPixel(x, y);
        bool Dark(int i) => px[i].R < threshold && px[i].G < threshold && px[i].B < threshold;

        // 1) 外側の黒を透過(四隅と四辺の中央から塗りつぶし)
        var clear = new bool[w * h];
        var stack = new Stack<int>();
        void Seed(int x, int y) { int i = y * w + x; if (Dark(i) && !clear[i]) { clear[i] = true; stack.Push(i); } }
        Seed(0, 0); Seed(w - 1, 0); Seed(0, h - 1); Seed(w - 1, h - 1); Seed(w / 2, 0); Seed(w / 2, h - 1); Seed(0, h / 2); Seed(w - 1, h / 2);
        while (stack.Count > 0)
        {
            int i = stack.Pop(); int x = i % w, y = i / w;
            if (x > 0) Seed(x - 1, y); if (x < w - 1) Seed(x + 1, y);
            if (y > 0) Seed(x, y - 1); if (y < h - 1) Seed(x, y + 1);
        }

        // 2) 開口部の検出: 勾配(隣接画素との差)が小さい区間が長く続くところを内側とみなす
        float Grad(int x, int y)
        {
            int i = y * w + x;
            var c = px[i];
            var cx2 = px[y * w + Math.Min(w - 1, x + 1)];
            var cy2 = px[Math.Min(h - 1, y + 1) * w + x];
            return (Math.Abs(c.R - cx2.R) + Math.Abs(c.G - cx2.G) + Math.Abs(c.B - cx2.B)
                  + Math.Abs(c.R - cy2.R) + Math.Abs(c.G - cy2.G) + Math.Abs(c.B - cy2.B)) / 6f;
        }
        // 横方向: 中央の帯(高さの 45〜55%)で列ごとの平均勾配
        var colG = new float[w];
        for (int x = 0; x < w; x++)
        {
            float sum = 0; int n = 0;
            for (int y = (int)(h * 0.45f); y < (int)(h * 0.55f); y++) { sum += Grad(x, y); n++; }
            colG[x] = sum / Math.Max(1, n);
        }
        var rowG = new float[h];
        for (int y = 0; y < h; y++)
        {
            float sum = 0; int n = 0;
            for (int x = (int)(w * 0.45f); x < (int)(w * 0.55f); x++) { sum += Grad(x, y); n++; }
            rowG[y] = sum / Math.Max(1, n);
        }
        const float Flat = 6f;   // これ未満なら「平坦」
        int left = FindEdge(colG, w / 2, -1, (int)(w * 0.06f)), right = FindEdge(colG, w / 2, +1, (int)(w * 0.06f));
        int top = FindEdge(rowG, h / 2, -1, (int)(h * 0.05f)), bottom = FindEdge(rowG, h / 2, +1, (int)(h * 0.05f));
        // 検出に失敗したら既定値(外側 12% を枠とみなす)
        if (left < w * 0.04f || left > w * 0.30f) left = (int)(w * 0.12f);
        if (right > w * 0.96f || right < w * 0.70f) right = (int)(w * 0.88f);
        if (top < h * 0.03f || top > h * 0.25f) top = (int)(h * 0.09f);
        if (bottom > h * 0.97f || bottom < h * 0.75f) bottom = (int)(h * 0.91f);

        int FindEdge(float[] g, int from, int dir, int minRun)
        {
            // 中央から外へ進み、「平坦が minRun 以上続いた後に平坦でなくなる」位置を境界とする
            int run = 0;
            for (int i = from; i > 0 && i < g.Length - 1; i += dir)
            {
                if (g[i] < Flat) run++;
                else if (run >= minRun) return i;
                else run = 0;
            }
            return dir < 0 ? 0 : g.Length - 1;
        }

        using var rgba = new Bitmap(w, h, PixelFormat.Format32bppArgb);
        const float Feather = 2f;
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            int i = y * w + x;
            var c = px[i];
            float a = 255f;
            if (clear[i]) a = 0;
            else
            {
                // 外周の黒の境界を 1px ぼかす
                int n = 0, cnt = 0;
                for (int dy = -1; dy <= 1; dy++) for (int dx = -1; dx <= 1; dx++)
                { int xx = x + dx, yy = y + dy; if (xx < 0 || yy < 0 || xx >= w || yy >= h) continue; cnt++; if (!clear[yy * w + xx]) n++; }
                a = 255f * n / cnt;
            }
            // 開口部の内側を透過(境界を Feather px でぼかす)
            float inside = Math.Min(Math.Min(x - left, right - x), Math.Min(y - top, bottom - y));
            if (inside > 0) a *= Math.Clamp(1f - inside / Feather, 0f, 1f);
            rgba.SetPixel(x, y, Color.FromArgb((int)a, c.R, c.G, c.B));
        }
        // 枠ごとに太さが違うので、開口部が常に同じ位置(OpenL..OpenB)に来るように拡大縮小して揃える。
        // こうすると Unity 側は固定の余白で中身を置ける(はみ出た外側の装飾は切れる)。
        using var bmp = new Bitmap(FrameW, FrameH, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bmp))
        {
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.CompositingMode = CompositingMode.SourceOver;
            float tl = FrameW * OpenL, tr = FrameW * OpenR, tt = FrameH * OpenT, tb = FrameH * OpenB;
            float sx = (tr - tl) / Math.Max(1, right - left), sy = (tb - tt) / Math.Max(1, bottom - top);
            g.DrawImage(rgba, new RectangleF(tl - left * sx, tt - top * sy, w * sx, h * sy));
        }
        using var ms = new MemoryStream();
        bmp.Save(ms, ImageFormat.Png);
        return ms.ToArray();
    }

    /// <summary>
    /// バッジ画像: 生成画像は背景が真っ黒でなく透かし(右下)も入るので、メダリオンの外周を検出して円形に切り抜く。
    /// recolor が指定されたら、赤い(色相 0 付近の彩度の高い)画素の色相を回して宝石の色を変える(形は同じまま 3 色を揃えるため)。
    /// 出力は 256×256 PNG。
    /// </summary>
    public static byte[] ToBadge(byte[] input, string? recolor, float hole = 0f, int outSize = 256)
    {
        using var src = new Bitmap(Image.FromStream(new MemoryStream(input)));
        int w = src.Width, h = src.Height;
        var px = new Color[w * h];
        for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) px[y * w + x] = src.GetPixel(x, y);
        // 背景色 = 四隅の平均。外周検出: 半径 r の円周上で背景と違う画素の割合が 55% を超える最大の r
        var corners = new[] { px[0], px[w - 1], px[(h - 1) * w], px[h * w - 1] };
        float bgR = (float)corners.Average(c => c.R), bgG = (float)corners.Average(c => c.G), bgB = (float)corners.Average(c => c.B);
        bool IsBg(Color c) => Math.Abs(c.R - bgR) + Math.Abs(c.G - bgG) + Math.Abs(c.B - bgB) < 60;
        float cx = w / 2f, cy = h / 2f, maxR = Math.Min(w, h) / 2f;
        float radius = maxR * 0.5f;
        for (float r = maxR - 1; r > maxR * 0.5f; r -= 1f)
        {
            int hit = 0, total = 0;
            for (int k = 0; k < 360; k += 2)
            {
                double a = k * Math.PI / 180;
                int x = (int)(cx + r * Math.Cos(a)), y = (int)(cy + r * Math.Sin(a));
                if (x < 0 || y < 0 || x >= w || y >= h) continue;
                total++; if (!IsBg(px[y * w + x])) hit++;
            }
            if (total > 0 && hit > total * 0.55f) { radius = r; break; }
        }
        using var rgba = new Bitmap(w, h, PixelFormat.Format32bppArgb);
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            var c = px[y * w + x];
            float d = (float)Math.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
            float a = Math.Clamp(radius + 0.5f - d, 0f, 1.5f) / 1.5f; // 1.5px のアンチエイリアス
            if (hole > 0) a *= Math.Clamp((d - hole * radius) / 2f, 0f, 1f); // 中央の穴(リングだけ残す)
            if (recolor != null) c = Recolor(c, recolor);
            c = Boost(c);
            rgba.SetPixel(x, y, Color.FromArgb((int)(255 * a), c.R, c.G, c.B));
        }
        using var bmp = new Bitmap(outSize, outSize, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bmp))
        {
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.CompositingMode = CompositingMode.SourceCopy;
            // 検出した円が画像いっぱいになるように切り抜いて縮小
            var srcRect = new RectangleF(cx - radius, cy - radius, radius * 2, radius * 2);
            g.DrawImage(rgba, new RectangleF(0, 0, outSize, outSize), srcRect, GraphicsUnit.Pixel);
        }
        using var ms = new MemoryStream();
        bmp.Save(ms, ImageFormat.Png);
        return ms.ToArray();
    }

    /// <summary>カード上では 1/5 程度に縮小され暗く沈むので、明るさ(ガンマ 0.72)と彩度(×1.35)を上げておく。</summary>
    static Color Boost(Color c)
    {
        float r = c.R / 255f, g = c.G / 255f, b = c.B / 255f;
        float lum = 0.299f * r + 0.587f * g + 0.114f * b;
        r = lum + (r - lum) * 1.35f; g = lum + (g - lum) * 1.35f; b = lum + (b - lum) * 1.35f;
        r = MathF.Pow(Math.Clamp(r, 0, 1), 0.72f); g = MathF.Pow(Math.Clamp(g, 0, 1), 0.72f); b = MathF.Pow(Math.Clamp(b, 0, 1), 0.72f);
        return Color.FromArgb(c.A, (int)(r * 255), (int)(g * 255), (int)(b * 255));
    }

    /// <summary>赤い画素(色相 ±40°、彩度 0.35 以上)の色相を emerald(+120°)/ sapphire(+200°)に回す。</summary>
    static Color Recolor(Color c, string target)
    {
        float hue = c.GetHue(), sat = c.GetSaturation(), bri = c.GetBrightness();
        float dist = Math.Min(hue, 360 - hue);
        if (dist > 40 || sat < 0.35f) return c;
        float shift = target == "emerald" ? 120f : 200f;
        float weight = Math.Clamp((40 - dist) / 10f, 0f, 1f) * Math.Clamp((sat - 0.35f) / 0.15f, 0f, 1f);
        var shifted = FromHsl((hue + shift) % 360, sat, bri);
        return Color.FromArgb(c.A, (int)(c.R + (shifted.R - c.R) * weight), (int)(c.G + (shifted.G - c.G) * weight), (int)(c.B + (shifted.B - c.B) * weight));
    }

    static Color FromHsl(float h, float s, float l)
    {
        float c = (1 - Math.Abs(2 * l - 1)) * s, x = c * (1 - Math.Abs(h / 60 % 2 - 1)), m = l - c / 2;
        (float r, float g, float b) = h switch { < 60 => (c, x, 0f), < 120 => (x, c, 0f), < 180 => (0f, c, x), < 240 => (0f, x, c), < 300 => (x, 0f, c), _ => (c, 0f, x) };
        return Color.FromArgb(255, (int)((r + m) * 255), (int)((g + m) * 255), (int)((b + m) * 255));
    }

    /// <summary>画像全体をそのまま縮小して JPEG に(リーダーの肖像など)。</summary>
    public static byte[] ToFull(byte[] input, int outW, int outH)
    {
        using var src = new Bitmap(Image.FromStream(new MemoryStream(input)));
        using var bmp = new Bitmap(outW, outH, PixelFormat.Format24bppRgb);
        using (var g = Graphics.FromImage(bmp))
        {
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.DrawImage(src, new Rectangle(0, 0, outW, outH));
        }
        return Jpeg(bmp, 90);
    }

    /// <summary>
    /// 切り抜き(ハースストーン風の部品): 外周から黒を塗りつぶして透過し、hole なら中央の黒(額の窓)も透過する。
    /// 不透明部分の外接矩形で切り出して outW × outH に。recolor(emerald / sapphire)で赤い宝石の色を回す。
    /// </summary>
    public static byte[] ToCutout(byte[] input, int outW, int outH, bool hole, string? recolor, bool rectHole = false)
    {
        using var src = new Bitmap(Image.FromStream(new MemoryStream(input)));
        int w = src.Width, h = src.Height;
        var px = new Color[w * h];
        for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) px[y * w + x] = src.GetPixel(x, y);
        // 背景 = 四隅の平均色。真っ黒でなく濃い灰色のことが多いので、背景色に近い画素を消す
        var corners = new[] { px[0], px[w - 1], px[(h - 1) * w], px[h * w - 1] };
        float bgR = (float)corners.Average(c => c.R), bgG = (float)corners.Average(c => c.G), bgB = (float)corners.Average(c => c.B);
        bool Dark(int i) { var c = px[i]; return Math.Abs(c.R - bgR) + Math.Abs(c.G - bgG) + Math.Abs(c.B - bgB) < 75 || c.R + c.G + c.B < 60; }
        var clear = new bool[w * h];
        var stack = new Stack<int>();
        void Seed(int x, int y) { int i = y * w + x; if (Dark(i) && !clear[i]) { clear[i] = true; stack.Push(i); } }
        for (int x = 0; x < w; x += 8) { Seed(x, 0); Seed(x, h - 1); }
        for (int y = 0; y < h; y += 8) { Seed(0, y); Seed(w - 1, y); }
        while (stack.Count > 0)
        {
            int i = stack.Pop(); int x = i % w, y = i / w;
            if (x > 0) Seed(x - 1, y); if (x < w - 1) Seed(x + 1, y);
            if (y > 0) Seed(x, y - 1); if (y < h - 1) Seed(x, y + 1);
        }
        // 額の窓: 中心から上下左右に進み、明るい(金の)画素に当たった所を内側の楕円とする
        float ex = 0, ey = 0;
        if (hole)
        {
            bool Bright(int x, int y) { var c = px[y * w + x]; return c.R + c.G + c.B > 330; }
            int Scan(int dx, int dy) { int x = w / 2, y = h / 2, n = 0; while (x > 0 && y > 0 && x < w - 1 && y < h - 1 && !Bright(x, y)) { x += dx; y += dy; n++; } return n; }
            ex = Math.Min(Scan(1, 0), Scan(-1, 0)) + 2; ey = Math.Min(Scan(0, 1), Scan(0, -1)) + 2;
        }
        int minX = w, minY = h, maxX = 0, maxY = 0;
        using var rgba = new Bitmap(w, h, PixelFormat.Format32bppArgb);
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            int i = y * w + x;
            float a = 0;
            if (!clear[i])
            {
                int n = 0, cnt = 0;
                for (int dy = -1; dy <= 1; dy++) for (int dx = -1; dx <= 1; dx++)
                { int xx = x + dx, yy = y + dy; if (xx < 0 || yy < 0 || xx >= w || yy >= h) continue; cnt++; if (!clear[yy * w + xx]) n++; }
                a = 255f * n / cnt;
                if (x < minX) minX = x; if (x > maxX) maxX = x; if (y < minY) minY = y; if (y > maxY) maxY = y;
            }
            if (hole && ex > 0 && ey > 0)
            {
                float dx = (x - w / 2f) / ex, dy = (y - h / 2f) / ey;
                // 楕円の窓(既定)/ 角の丸い四角の窓(rectHole、スペル用の額)
                float r = rectHole ? MathF.Pow(MathF.Pow(Math.Abs(dx), 8) + MathF.Pow(Math.Abs(dy), 8), 1f / 8f) : MathF.Sqrt(dx * dx + dy * dy);
                a *= Math.Clamp((r - 1f) * Math.Min(ex, ey) / 2f + 0.5f, 0f, 1f);   // 楕円の内側を透過(2px ぼかし)
            }
            var c = px[i];
            if (recolor != null) c = Recolor(c, recolor);
            rgba.SetPixel(x, y, Color.FromArgb((int)a, c.R, c.G, c.B));
        }
        if (maxX <= minX || maxY <= minY) { minX = 0; minY = 0; maxX = w - 1; maxY = h - 1; }
        using var bmp = new Bitmap(outW, outH, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bmp))
        {
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.CompositingMode = CompositingMode.SourceCopy;
            g.DrawImage(rgba, new Rectangle(0, 0, outW, outH), new Rectangle(minX, minY, maxX - minX + 1, maxY - minY + 1), GraphicsUnit.Pixel);
        }
        // 窓の大きさ(出力画像に対する比)を表示する。Unity 側の額の拡大率(CardView.OvalArt)に使う
        if (hole) Console.Write($"[窓 横 {2 * ex / (maxX - minX + 1):F3} × 縦 {2 * ey / (maxY - minY + 1):F3}] ");
        using var ms = new MemoryStream();
        bmp.Save(ms, ImageFormat.Png);
        return ms.ToArray();
    }

    /// <summary>銘板・羊皮紙: 黒背景を落として中央の素材だけを切り出し、指定サイズの PNG にする。</summary>
    public static byte[] ToPlate(byte[] input, int outW, int outH)
    {
        using var src = new Bitmap(Image.FromStream(new MemoryStream(input)));
        int w = src.Width, h = src.Height;
        // 明るい画素の外接矩形を求める(背景の黒を除く)
        int minX = w, minY = h, maxX = 0, maxY = 0;
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            var c = src.GetPixel(x, y);
            if (c.R + c.G + c.B < 90) continue;
            if (x < minX) minX = x; if (x > maxX) maxX = x;
            if (y < minY) minY = y; if (y > maxY) maxY = y;
        }
        if (maxX <= minX || maxY <= minY) { minX = 0; minY = 0; maxX = w - 1; maxY = h - 1; }
        using var bmp = new Bitmap(outW, outH, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bmp))
        {
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.CompositingMode = CompositingMode.SourceCopy;
            g.DrawImage(src, new Rectangle(0, 0, outW, outH), new Rectangle(minX, minY, maxX - minX + 1, maxY - minY + 1), GraphicsUnit.Pixel);
        }
        using var ms = new MemoryStream();
        bmp.Save(ms, ImageFormat.Png);
        return ms.ToArray();
    }

    static byte[] Jpeg(Bitmap bmp, long quality)
    {
        var enc = ImageCodecInfo.GetImageEncoders().First(e => e.MimeType == "image/jpeg");
        var prm = new EncoderParameters(1);
        prm.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality);
        using var ms = new MemoryStream();
        bmp.Save(ms, enc, prm);
        return ms.ToArray();
    }
}
