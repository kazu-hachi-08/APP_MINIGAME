using MiniGame.PenguinWars.Battle;
using UnityEngine;

namespace MiniGame.PenguinWars
{
    /// <summary>ステージモードの進み具合を PlayerPrefs に JSON 1本で読み書きする（中身の形は CampaignProgress が持つ）</summary>
    public static class CampaignSave
    {
        private const string Key = "PenguinWars.Campaign";

        public static CampaignProgress Load()
        {
            return CampaignProgress.FromJson(PlayerPrefs.GetString(Key, string.Empty));
        }

        /// <summary>アプリが落ちても失わないよう、書いたらすぐディスクに出す</summary>
        public static void Save(CampaignProgress progress)
        {
            PlayerPrefs.SetString(Key, progress.ToJson());
            PlayerPrefs.Save();
        }
    }
}
