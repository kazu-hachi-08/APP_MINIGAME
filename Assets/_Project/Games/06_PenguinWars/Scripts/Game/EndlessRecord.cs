using UnityEngine;

namespace MiniGame.PenguinWars
{
    /// <summary>エンドレスのベスト生存時間（仕様書 §2.1）。秒の整数で PlayerPrefs に残す</summary>
    public static class EndlessRecord
    {
        private const string BestSecondsKey = "PenguinWars.Endless.BestSeconds";

        /// <summary>まだ遊んだことがなければ 0</summary>
        public static int LoadBestSeconds()
        {
            return PlayerPrefs.GetInt(BestSecondsKey, 0);
        }

        /// <summary>ベストを超えていたら保存して true（NEW RECORD!）</summary>
        public static bool TryUpdateBest(int seconds)
        {
            if (seconds <= LoadBestSeconds()) return false;

            PlayerPrefs.SetInt(BestSecondsKey, seconds);
            // アプリを落とされても記録が残るよう、その場で書き込む
            PlayerPrefs.Save();
            return true;
        }
    }
}
