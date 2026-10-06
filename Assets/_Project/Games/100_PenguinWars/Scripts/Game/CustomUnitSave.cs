using MiniGame.PenguinWars.Battle;
using UnityEngine;

namespace MiniGame.PenguinWars
{
    /// <summary>
    /// じぶんペンギンの3枠を PlayerPrefs に JSON 1本で読み書きする（中身の形は CustomUnitPresets が持つ）。
    /// ステージのセーブ（CampaignSave）とキーを分け、片方が壊れてももう片方に響かないようにする
    /// </summary>
    public static class CustomUnitSave
    {
        private const string Key = "PenguinWars.CustomUnits";

        public static CustomUnitPresets Load()
        {
            return CustomUnitPresets.FromJson(PlayerPrefs.GetString(Key, string.Empty));
        }

        /// <summary>アプリが落ちても失わないよう、書いたらすぐディスクに出す</summary>
        public static void Save(CustomUnitPresets presets)
        {
            PlayerPrefs.SetString(Key, presets.ToJson());
            PlayerPrefs.Save();
        }
    }
}
