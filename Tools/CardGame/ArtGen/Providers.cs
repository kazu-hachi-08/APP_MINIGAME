using System.Security.Cryptography;
using System.Text;

/// <summary>プロンプト組み立て(英語)と、無料の pollinations.ai プロバイダ。</summary>
/// 文言と語順は既存の 122 枚を生成したものから変えない(同じシードで同じ絵が出る = 個別の作り直しが既存と揃う)。
/// ダーク寄せの試作(雰囲気語を先頭に置く)は被写体が消えたため不採用(2026-09-22)。
static class ArtPrompts
{
    public const string Style = "fantasy trading card game illustration, highly detailed digital painting, sharp focus, rich colors, dramatic lighting, " +
                                "portrait orientation, main subject large in the upper half of the frame, lower third only background, no text, no border, no watermark";

    public static string ClassStyle(string cls) => cls switch
    {
        "騎士" => "kingdom knights theme: silver armor, blue and gold heraldry, castles and banners, noble and proud mood",
        "魔導" => "arcane academy theme: purple robes, glowing magic circles, fire and ice magic, mystical mood",
        "死霊術師" => "necromancer theme: bones and gravestones, dark green and black palette, eerie green glow, desolate mood",
        "森の民" => "forest folk theme: deep green forest, sunbeams through leaves, moss and flowers, vibrant living mood",
        "竜族" => "dragon theme: red and orange scales, volcanoes and lava, drifting embers, overwhelming power",
        _ => "medieval fantasy villagers and travelers theme: muted earthy colors, humble warm mood",
    };

    public static string Build(string subject, string cls) => $"{Style}. Subject: {subject}. {ClassStyle(cls)}.";

    /// <summary>カード ID から決まるシード(再生成しても同じ絵になる。変えたいときは --seed-offset)。</summary>
    public static int SeedFor(string id, int offset)
    {
        var h = MD5.HashData(Encoding.UTF8.GetBytes(id));
        return (BitConverter.ToInt32(h, 0) & 0x7fffffff) % 1_000_000 + offset;
    }
}

static class Pollinations
{
    public static async Task<byte[]> GenerateAsync(HttpClient http, string prompt, int seed, int width, int height)
    {
        var url = $"https://image.pollinations.ai/prompt/{Uri.EscapeDataString(prompt)}?width={width}&height={height}&seed={seed}&nologo=true&model=flux";
        Exception? last = null;
        for (int attempt = 1; attempt <= 4; attempt++)
        {
            try
            {
                using var res = await http.GetAsync(url);
                var bytes = await res.Content.ReadAsByteArrayAsync();
                var type = res.Content.Headers.ContentType?.MediaType ?? "";
                if (res.IsSuccessStatusCode && type.StartsWith("image/") && bytes.Length > 5000) return bytes;
                last = new Exception($"HTTP {(int)res.StatusCode} {type} {Encoding.UTF8.GetString(bytes.Take(200).ToArray())}");
            }
            catch (Exception ex) { last = ex; }
            await Task.Delay(5000 * attempt);
        }
        throw last!;
    }
}

/// <summary>
/// Unity で crunch 圧縮が効くように、画像を 512×1024(2 の冪)に揃える。
/// 上 768px に 2:3 の絵を置き、下 256px は絵の最下段を引き伸ばして埋める(カード上では隠れる領域)。
/// CardView は常に上端揃えで切り抜く。
/// </summary>
static class ImageNormalize
{
    public static byte[] ToCardSize(byte[] input)
    {
        using var src = System.Drawing.Image.FromStream(new MemoryStream(input));
        using var bmp = new System.Drawing.Bitmap(512, 1024);
        using (var g = System.Drawing.Graphics.FromImage(bmp))
        {
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            g.DrawImage(src, new System.Drawing.Rectangle(0, 0, 512, 768));
            var strip = new System.Drawing.Rectangle(0, src.Height - 24, src.Width, 24);
            g.DrawImage(src, new System.Drawing.Rectangle(0, 760, 512, 264), strip, System.Drawing.GraphicsUnit.Pixel);
        }
        var enc = System.Drawing.Imaging.ImageCodecInfo.GetImageEncoders().First(e => e.MimeType == "image/jpeg");
        var prm = new System.Drawing.Imaging.EncoderParameters(1);
        prm.Param[0] = new System.Drawing.Imaging.EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 88L);
        using var ms = new MemoryStream();
        bmp.Save(ms, enc, prm);
        return ms.ToArray();
    }
}
