#nullable enable
using UnityEditor;
using UnityEngine;

namespace CardGame.Unity.Editor
{
    /// <summary>
    /// 50_CardGame/Resources/Audio/ の WAV を自動設定する。
    /// - se/  : 短いので読み込み時に展開(遅延なく鳴る)
    /// - bgm/ : ストリーミング(WebGL は展開)。どちらも Vorbis で圧縮してビルドサイズを抑える
    /// </summary>
    public sealed class AudioImporterSettings : AssetPostprocessor
    {
        public override uint GetVersion() => 5;

        private void OnPreprocessAudio()
        {
            var path = assetPath.Replace('\\', '/');
            if (!path.Contains("/50_CardGame/Resources/Audio/")) return;
            bool bgm = path.Contains("/Audio/bgm/");

            var importer = (AudioImporter)assetImporter;
            importer.forceToMono = true;
            importer.loadInBackground = bgm;

            var settings = importer.defaultSampleSettings;
            settings.loadType = bgm ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = bgm ? 0.5f : 0.7f;
            settings.preloadAudioData = !bgm;
            importer.defaultSampleSettings = settings;

            // WebGL は Streaming に対応していないので展開に落とす
            var web = importer.GetOverrideSampleSettings("WebGL");
            web.compressionFormat = AudioCompressionFormat.Vorbis;
            web.quality = bgm ? 0.45f : 0.65f;
            web.loadType = AudioClipLoadType.DecompressOnLoad;
            web.preloadAudioData = true;   // 読み込まないまま鳴らすと WebGL では無音になる(BGM も 0.4MB 程度なので先に読む)
            importer.SetOverrideSampleSettings("WebGL", web);
        }
    }
}
