using System.Net.Http.Json;
using System.Text;
using Newtonsoft.Json.Linq;

/// <summary>
/// ローカルの ComfyUI(http://127.0.0.1:8188)で SDXL(Juggernaut XL)を回すプロバイダ。
/// ワークフローは txt2img の最小構成(Checkpoint → CLIP ×2 → EmptyLatent → KSampler → VAEDecode → SaveImage)を API 形式で送る。
/// 起動: D:\tools\ComfyUI_windows_portable\run_nvidia_gpu.bat(scripts/comfy-start.ps1 参照)
/// </summary>
static class ComfyLocal
{
    public const string BaseUrl = "http://127.0.0.1:8188";
    /// <summary>写実寄り(従来のカードイラスト・UI 素材)。</summary>
    public const string CheckpointReal = "Juggernaut-XL_v9_RunDiffusionPhoto_v2.safetensors";
    /// <summary>イラスト寄り(ハースストーン風)。Lykon/dreamshaper-xl-v2-turbo、openrail++。少ない手順(8)・低い CFG(2)で使う。</summary>
    public const string CheckpointTurbo = "DreamShaperXL_Turbo_v2_1.safetensors";

    /// <summary>使うモデル。UseTurbo() で切り替える。</summary>
    public static string Checkpoint = CheckpointReal;
    static int _steps = 30;
    static float _cfg = 4.5f;
    static string _sampler = "dpmpp_2m";

    /// <summary>Turbo 型(DreamShaper XL Turbo)の推奨設定に切り替える。</summary>
    public static void UseTurbo() { Checkpoint = CheckpointTurbo; _steps = 8; _cfg = 2.0f; _sampler = "dpmpp_sde"; }
    /// <summary>写実寄りのモデル(既定)に戻す。</summary>
    public static void UseReal() { Checkpoint = CheckpointReal; _steps = 30; _cfg = 4.5f; _sampler = "dpmpp_2m"; }

    public static async Task<bool> IsUpAsync(HttpClient http)
    {
        try { using var r = await http.GetAsync(BaseUrl + "/system_stats"); return r.IsSuccessStatusCode; }
        catch { return false; }
    }

    /// <summary>画像を 1 枚生成して PNG のバイト列を返す。</summary>
    public static async Task<byte[]> GenerateAsync(HttpClient http, string prompt, string negative, long seed, int width, int height)
    {
        int steps = _steps; float cfg = _cfg;
        var wf = new JObject
        {
            ["4"] = Node("CheckpointLoaderSimple", new JObject { ["ckpt_name"] = Checkpoint }),
            ["6"] = Node("CLIPTextEncode", new JObject { ["text"] = prompt, ["clip"] = Link("4", 1) }),
            ["7"] = Node("CLIPTextEncode", new JObject { ["text"] = negative, ["clip"] = Link("4", 1) }),
            ["5"] = Node("EmptyLatentImage", new JObject { ["width"] = width, ["height"] = height, ["batch_size"] = 1 }),
            ["3"] = Node("KSampler", new JObject
            {
                ["seed"] = seed, ["steps"] = steps, ["cfg"] = cfg, ["sampler_name"] = _sampler, ["scheduler"] = "karras", ["denoise"] = 1.0,
                ["model"] = Link("4", 0), ["positive"] = Link("6", 0), ["negative"] = Link("7", 0), ["latent_image"] = Link("5", 0),
            }),
            ["8"] = Node("VAEDecode", new JObject { ["samples"] = Link("3", 0), ["vae"] = Link("4", 2) }),
            ["9"] = Node("SaveImage", new JObject { ["filename_prefix"] = "artgen", ["images"] = Link("8", 0) }),
        };
        var body = new JObject { ["prompt"] = wf, ["client_id"] = "artgen" };
        using var res = await http.PostAsync(BaseUrl + "/prompt", new StringContent(body.ToString(Newtonsoft.Json.Formatting.None), Encoding.UTF8, "application/json"));
        var text = await res.Content.ReadAsStringAsync();
        if (!res.IsSuccessStatusCode) throw new Exception($"ComfyUI /prompt {(int)res.StatusCode}: {text[..Math.Min(300, text.Length)]}");
        var promptId = JObject.Parse(text)["prompt_id"]!.ToString();

        // 完了までポーリング(1 枚 15〜60 秒。初回はモデル読み込みで数分)
        var deadline = DateTime.UtcNow.AddMinutes(10);
        while (DateTime.UtcNow < deadline)
        {
            await Task.Delay(1500);
            var hist = JObject.Parse(await http.GetStringAsync($"{BaseUrl}/history/{promptId}"));
            var entry = hist[promptId];
            if (entry == null) continue;
            var status = entry["status"];
            if (status?["status_str"]?.ToString() == "error")
                throw new Exception("ComfyUI が失敗: " + status.ToString(Newtonsoft.Json.Formatting.None)[..300]);
            var img = entry["outputs"]?["9"]?["images"]?[0];
            if (img == null) continue;
            var url = $"{BaseUrl}/view?filename={Uri.EscapeDataString(img["filename"]!.ToString())}&subfolder={Uri.EscapeDataString(img["subfolder"]?.ToString() ?? "")}&type={img["type"]}";
            return await http.GetByteArrayAsync(url);
        }
        throw new TimeoutException("ComfyUI の生成がタイムアウト");
    }

    static JObject Node(string type, JObject inputs) => new JObject { ["class_type"] = type, ["inputs"] = inputs };
    static JArray Link(string node, int slot) => new JArray(node, slot);

    /// <summary>
    /// SDXL 向けのプロンプト。被写体を先頭に(雰囲気語が先だと被写体が消える)。
    /// カードごとに構図・カメラ・光・時間帯を ID から決めて変える(同じ服装・同じ構図が並ばないように)。
    /// </summary>
    /// <summary>画風。"real" = 写実寄りのダークファンタジー(現行)、"hs" = ハースストーン風(手描き調・鮮やか・誇張)。</summary>
    public static string Style = "real";

    public static string BuildPrompt(string id, string subject, string cls)
    {
        if (Style == "hs") return BuildPromptHs(id, subject, cls);
        var h = System.Security.Cryptography.MD5.HashData(System.Text.Encoding.UTF8.GetBytes(id));
        string Pick(string[] xs, int i) => xs[h[i] % xs.Length];
        return $"{subject}. {Pick(Shots, 0)}, {Pick(Angles, 1)}. {Pick(Lights, 2)}, {Pick(Times, 3)}. " +
               $"{ArtPrompts.ClassStyle(cls)}. dark fantasy trading card game illustration, painterly realistic digital painting, " +
               "deep shadows, muted rich colors, highly detailed, sharp focus, " +
               "portrait orientation, main subject large in the upper half of the frame, lower third only background";
    }

    /// <summary>ハースストーン風: 手描きの厚塗り、鮮やかな色、誇張したシルエット、暖かい縁の光。</summary>
    static string BuildPromptHs(string id, string subject, string cls)
    {
        var h = System.Security.Cryptography.MD5.HashData(System.Text.Encoding.UTF8.GetBytes(id));
        string Pick(string[] xs, int i) => xs[h[i] % xs.Length];
        // 人物のいない題材(スペルの情景・物など)。「英雄的な人物」の指示を外さないと人物が描かれてしまう(2026-09-23 の検品で判明)
        if (IsSceneOnly(subject))
            return $"{subject}. {Pick(Angles, 1)}. {ScenePalette(cls)}. " +
                   "Hearthstone spell card art, Blizzard Entertainment art style, stylized hand-painted digital illustration, " +
                   "dramatic magical effect or object as the only focus, no people, vibrant saturated colors, warm glow, soft painterly brushwork, " +
                   "highly polished game illustration, portrait orientation, main subject large in the upper half of the frame";
        return $"{subject}. {Pick(Shots, 0)}, {Pick(Angles, 1)}. {ArtPrompts.ClassStyle(cls)}. " +
               "Hearthstone card art, Blizzard Entertainment art style, World of Warcraft concept art, stylized hand-painted digital illustration, " +
               "bold chunky shapes, exaggerated heroic proportions, vibrant saturated colors, warm golden rim light, soft painterly brushwork, " +
               "clean readable silhouette, whimsical and epic, highly polished game illustration, " +
               "portrait orientation, main subject large in the upper half of the frame, lower third only background";
    }

    /// <summary>人物なしの題材用の色だけの指定(ClassStyle は「騎士団」「村人」など人物を呼び込むので使わない)。</summary>
    static string ScenePalette(string cls) => cls switch
    {
        "騎士" => "blue and gold color palette, bright daylight",
        "魔導" => "purple and arcane blue glowing color palette",
        "死霊術師" => "eerie green glow and dark shadows",
        "森の民" => "lush green forest light with sunbeams",
        "竜族" => "fiery orange and red color palette with embers",
        _ => "warm earthy colors, sunny sky",
    };

    /// <summary>題材が「人物なし」(subjects.json で no character と書いたもの)か。</summary>
    public static bool IsSceneOnly(string subject) => subject.Contains("no character", StringComparison.OrdinalIgnoreCase);

    public const string NegativeHs = "text, watermark, signature, logo, border, frame, card, lowres, blurry, deformed, extra fingers, bad anatomy, cropped, out of frame, photograph, photorealistic, photo, realistic skin pores, gritty, grainy, desaturated, muted colors, dark and murky";
    /// <summary>ハースストーン風の UI 部品用(額・リボンなどを描かせるので border / frame は外す)。</summary>
    public const string NegativeHsUi = "text, letters, watermark, signature, logo, lowres, blurry, deformed, photograph, photorealistic, photo, grainy";

    /// <summary>剣や手の崩れを抑える語(鞘が剣になる・2 本持ちなど。2026-09-23 の検品で判明)。</summary>
    const string NegativeAnatomy = ", two swords, extra sword, duplicate weapon, double hilt, malformed weapon, extra hands, extra arms, malformed hands, fused fingers, writing, letters, runes text";

    public static string NegativeFor(string subject) => Style != "hs" ? Negative
        : IsSceneOnly(subject) ? NegativeHs + NegativeAnatomy + ", person, people, man, woman, knight, warrior, humanoid, character, face"
        : NegativeHs + NegativeAnatomy;

    // 構図のばらつき(ID から決定論的に選ぶ)
    static readonly string[] Shots =
    {
        "extreme close-up of the face and shoulders", "head and shoulders portrait", "waist-up shot", "three-quarter body shot",
        "full body shot", "wide shot showing the surroundings", "dynamic action pose mid-movement", "the subject seen from behind looking away",
    };
    static readonly string[] Angles =
    {
        "eye level", "low angle looking up at the subject", "high angle looking down", "dutch angle",
        "profile view from the side", "three-quarter view", "slightly off-center composition", "centered symmetrical composition",
    };
    static readonly string[] Lights =
    {
        "rim lighting from behind", "warm torchlight from below", "cold moonlight from the side", "shafts of light through fog",
        "single candle as the only light source", "overcast diffuse light", "firelight flickering on the subject", "silhouette against a bright background",
    };
    static readonly string[] Times =
    {
        "at night", "at dusk", "at dawn", "in deep fog", "during rain", "under a stormy sky", "in a dim interior", "in falling snow",
    };

    public const string Negative = "text, watermark, signature, logo, border, frame, card, lowres, blurry, deformed, extra fingers, bad anatomy, cropped, out of frame, cartoon, anime, 3d render, plastic";
}
