using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// カードイラストを Gemini の画像生成 API で一括生成する開発ツール。
//   dotnet run --project Tools/CardGame/ArtGen --[--style real|hs] [--provider gemini|pollinations|local] [--only K001,K002] [--limit N] [--force] [--dry-run] [--model NAME] [--out DIR] [--seed-offset N]
//   素材の変換(机の背景・カード枠)は Assets.cs を参照
// - 入力: Docs/50_CardGame/art/card-art-prompts.csv(`dotnet run --project Core/CardGame.Cli -- art` で生成)
// - 出力: Assets/_Project/Games/50_CardGame/Resources/CardArt/<ID>.jpg(既にあるものはスキップ。--force で上書き)
// - API キーは環境変数 GEMINI_API_KEY から読む(リポジトリには置かない)

var args_ = Environment.GetCommandLineArgs().Skip(1).ToList();
string? Opt(string name) { int i = args_.IndexOf(name); return i >= 0 && i + 1 < args_.Count ? args_[i + 1] : null; }
bool Flag(string name) => args_.Contains(name);

var repoRoot = FindRepoRoot();
// 素材コマンド(table / frame)はカード生成とは別経路(Assets.cs)
if (args_.Count > 0 && (args_[0] == "table" || args_[0] == "frame" || args_[0] == "badge" || args_[0] == "assets"))
{
    using var assetHttp = new HttpClient { Timeout = TimeSpan.FromMinutes(3) };
    return await AssetTools.RunAsync(args_, repoRoot, assetHttp);
}
var csvPath = Path.Combine(repoRoot, "Docs", "50_CardGame", "art", "card-art-prompts.csv");
var outDir = Opt("--out") ?? Path.Combine(repoRoot, "Assets", "_Project", "Games", "50_CardGame", "Resources", "CardArt"); // --out で別フォルダに(見本出し用)
Directory.CreateDirectory(outDir);

var apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
bool dryRun = Flag("--dry-run");
var provider = Opt("--provider") ?? (string.IsNullOrWhiteSpace(apiKey) ? "pollinations" : "gemini"); // gemini | pollinations | local(ComfyUI)
if (provider == "gemini" && string.IsNullOrWhiteSpace(apiKey) && !dryRun)
{
    Console.Error.WriteLine("環境変数 GEMINI_API_KEY が設定されていない(setx GEMINI_API_KEY \"...\" の後、新しいターミナルで実行)");
    return 2;
}
int seedOffset = int.TryParse(Opt("--seed-offset"), out var so) ? so : 0;
// 検品で選び直したカードのシード(Tools/CardGame/ArtGen/seed-overrides.json: { "N001": 100 }。--seed-offset を指定したときはそちらが優先)
var seedOverridesPath = Path.Combine(repoRoot, "Tools", "CardGame", "ArtGen", "seed-overrides.json");
var seedOverrides = File.Exists(seedOverridesPath)
    ? Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, int>>(File.ReadAllText(seedOverridesPath))!
    : new Dictionary<string, int>();
int SeedOffsetFor(string id) => Opt("--seed-offset") == null && seedOverrides.TryGetValue(id, out var o) ? o : seedOffset;
ComfyLocal.Style = Opt("--style") ?? "hs";     // hs(ハースストーン風、既定) | real(旧い写実寄り。art-archive/ 用)
if (ComfyLocal.Style == "hs") ComfyLocal.UseTurbo();   // ハースストーン風はイラスト寄りのモデルで描く
var subjects = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(Path.Combine(repoRoot, "Tools", "CardGame", "ArtGen", "subjects.json")))!;

var model = Opt("--model") ?? "gemini-2.5-flash-image";
var only = Opt("--only")?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToHashSet(StringComparer.OrdinalIgnoreCase);
int limit = int.TryParse(Opt("--limit"), out var l) ? l : int.MaxValue;
bool force = Flag("--force");

var rows = ReadCsv(csvPath);
var targets = rows
    .Where(r => only == null || only.Contains(r.Id))
    .Where(r => force || !File.Exists(Path.Combine(outDir, r.Id + ".png")) && !File.Exists(Path.Combine(outDir, r.Id + ".jpg")))
    .Take(limit)
    .ToList();

Console.WriteLine($"対象 {targets.Count} 枚(全 {rows.Count} 枚、provider={provider}, model={model})");
if (dryRun)
{
    foreach (var r in targets) Console.WriteLine($"  {r.Id} {r.Name}");
    return 0;
}

using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(3) };
if (provider == "local" && !await ComfyLocal.IsUpAsync(http)) { Console.Error.WriteLine("ComfyUI が起動していない(" + ComfyLocal.BaseUrl + ")。scripts/comfy-start.ps1 で起動する"); return 2; }
if (provider == "gemini") http.DefaultRequestHeaders.Add("x-goog-api-key", apiKey);
int ok = 0, fail = 0;
foreach (var r in targets)
{
    var outPath = Path.Combine(outDir, r.Id + ".jpg"); // どちらの provider でも 512×1024 の JPEG に正規化する
    Console.Write($"{r.Id} {r.Name} ... ");
    try
    {
        byte[] png;
        if (provider == "gemini") png = ImageNormalize.ToCardSize(await GenerateAsync(http, model, r.Prompt));
        else if (provider == "local")
        {
            var subject = subjects.TryGetValue(r.Id, out var sj) ? sj : r.Name;
            png = ImageNormalize.ToCardSize(await ComfyLocal.GenerateAsync(http, ComfyLocal.BuildPrompt(r.Id, subject, r.Class), ComfyLocal.NegativeFor(subject), ArtPrompts.SeedFor(r.Id, SeedOffsetFor(r.Id)), 832, 1216));
        }
        else
        {
            var subject = subjects.TryGetValue(r.Id, out var sj) ? sj : r.Name;
            png = ImageNormalize.ToCardSize(await Pollinations.GenerateAsync(http, ArtPrompts.Build(subject, r.Class), ArtPrompts.SeedFor(r.Id, seedOffset), 832, 1248));
        }
        await File.WriteAllBytesAsync(outPath, png);
        Console.WriteLine($"OK ({png.Length / 1024} KB)");
        ok++;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"失敗: {ex.Message}");
        fail++;
        if (ex.Message.Contains("429")) { Console.WriteLine("  レート制限。60 秒待機"); await Task.Delay(60_000); }
    }
    if (provider != "local") await Task.Delay(1500); // 連続呼び出しを避ける(ローカルは不要)
}
Console.WriteLine($"完了: 成功 {ok} / 失敗 {fail}");
return fail == 0 ? 0 : 1;

// ------------------------------------------------------------------

static async Task<byte[]> GenerateAsync(HttpClient http, string model, string prompt)
{
    var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent";
    var body = new JObject
    {
        ["contents"] = new JArray(new JObject { ["parts"] = new JArray(new JObject { ["text"] = prompt }) }),
        ["generationConfig"] = new JObject
        {
            ["responseModalities"] = new JArray("IMAGE"),
            ["imageConfig"] = new JObject { ["aspectRatio"] = "2:3" },
        },
    };

    for (int attempt = 1; attempt <= 3; attempt++)
    {
        using var content = new StringContent(body.ToString(Formatting.None), Encoding.UTF8, "application/json");
        using var res = await http.PostAsync(url, content);
        var text = await res.Content.ReadAsStringAsync();
        if (!res.IsSuccessStatusCode)
        {
            // 古い API で imageConfig が未対応なら外して再試行
            if ((int)res.StatusCode == 400 && text.Contains("imageConfig") && body["generationConfig"]?["imageConfig"] != null)
            {
                ((JObject)body["generationConfig"]!).Remove("imageConfig");
                body["contents"]![0]!["parts"]![0]!["text"] = prompt + " アスペクト比 2:3 の縦長画像。";
                continue;
            }
            if ((int)res.StatusCode >= 500 && attempt < 3) { await Task.Delay(3000 * attempt); continue; }
            throw new Exception($"HTTP {(int)res.StatusCode}: {Truncate(text, 300)}");
        }
        var json = JObject.Parse(text);
        var parts = json["candidates"]?[0]?["content"]?["parts"] as JArray;
        var inline = parts?.Select(p => p["inlineData"] ?? p["inline_data"]).FirstOrDefault(d => d != null);
        if (inline == null)
        {
            var reason = json["candidates"]?[0]?["finishReason"]?.ToString() ?? json["promptFeedback"]?.ToString() ?? "画像が返らなかった";
            throw new Exception(reason + " " + Truncate(text, 200));
        }
        return Convert.FromBase64String(inline["data"]!.ToString());
    }
    throw new Exception("再試行上限");
}

static string Truncate(string s, int n) => s.Length <= n ? s : s[..n] + "…";

static string FindRepoRoot()
{
    var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
    while (dir != null && !File.Exists(Path.Combine(dir.FullName, "CLAUDE.md"))) dir = dir.Parent;
    return dir?.FullName ?? throw new Exception("リポジトリのルート(CLAUDE.md)が見つからない");
}

static List<(string Id, string Name, string Class, string Prompt)> ReadCsv(string path)
{
    // 簡易 CSV パーサ(ダブルクォート対応)。列: ファイル名,ID,名前,クラス,種類,コスト,攻撃,体力,効果,フレーバー,プロンプト
    var list = new List<(string, string, string, string)>();
    var lines = File.ReadAllText(path, Encoding.UTF8).Replace("\r\n", "\n").Split('\n');
    foreach (var line in lines.Skip(1))
    {
        if (string.IsNullOrWhiteSpace(line)) continue;
        var cells = ParseLine(line);
        if (cells.Count < 11) continue;
        list.Add((cells[1], cells[2], cells[3], cells[10]));
    }
    return list;
}

static List<string> ParseLine(string line)
{
    var cells = new List<string>();
    var sb = new StringBuilder();
    bool inQ = false;
    for (int i = 0; i < line.Length; i++)
    {
        char c = line[i];
        if (inQ)
        {
            if (c == '"' && i + 1 < line.Length && line[i + 1] == '"') { sb.Append('"'); i++; }
            else if (c == '"') inQ = false;
            else sb.Append(c);
        }
        else if (c == '"') inQ = true;
        else if (c == ',') { cells.Add(sb.ToString()); sb.Clear(); }
        else sb.Append(c);
    }
    cells.Add(sb.ToString());
    return cells;
}
