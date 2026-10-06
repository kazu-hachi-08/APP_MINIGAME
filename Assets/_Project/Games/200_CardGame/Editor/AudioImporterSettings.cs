#nullable enable
using UnityEditor;
using UnityEngine;

namespace CardGame.Unity.Editor
{
    /// <summary>
    /// 200_CardGame/Resources/Audio/ の WAV を自動設定する。
    /// - se/  : 短いので読み込み時に展開(遅延なく鳴る)
    /// - bgm/ : ストリーミング(WebGL は展開)。どちらも Vorbis で圧縮してビルドサイズを抑える
    /// </summary>
    public sealed class AudioImporterSettings : AssetPostprocessor
    {
        public override uint GetVersion() => 6;

        private void OnPreprocessAudio()
        {
            var path = assetPath.Replace('\\', '/');
            if (!path.Contains("/200_CardGame/Resources/Audio/")) return;
            bool bgm = path.Contains("/Audio/bgm/");

            var importer = (AudioImporter)assetImporter;
            importer.forceToMono = true;
            // 裏読みの BGM は Web ビルド時に "Failed getting load state of FSB" で失敗する。Streaming なら無くても止まらない
            importer.loadInBackground = false;

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
