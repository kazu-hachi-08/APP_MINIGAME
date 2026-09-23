using CardGame.AudioGen;

// THE CHAOS Ⅱ の効果音と BGM を合成して WAV に書き出す開発ツール。
//   dotnet run --project Tools/AudioGen -- [--out DIR] [--only se|bgm]
// 出力: Unity/Assets/Resources/Audio/se/*.wav, Audio/bgm/*.wav
// 音源は物理モデル寄りの合成(撥弦・笛・太鼓・鐘)。素材の権利関係が発生しないよう、外部アセットは使わない。

var args_ = Environment.GetCommandLineArgs().Skip(1).ToList();
string? Opt(string name) { int i = args_.IndexOf(name); return i >= 0 && i + 1 < args_.Count ? args_[i + 1] : null; }

var repoRoot = FindRepoRoot();
var outDir = Opt("--out") ?? Path.Combine(repoRoot, "Unity", "Assets", "Resources", "Audio");
var only = Opt("--only");

if (only != "bgm") Se.WriteAll(Path.Combine(outDir, "se"));
if (only != "se") Bgm.WriteAll(Path.Combine(outDir, "bgm"));
Console.WriteLine("完了");
return 0;

static string FindRepoRoot()
{
    var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
    while (dir != null && !File.Exists(Path.Combine(dir.FullName, "CLAUDE.md"))) dir = dir.Parent;
    return dir?.FullName ?? throw new Exception("リポジトリのルート(CLAUDE.md)が見つからない");
}
