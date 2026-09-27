using System.IO;
using UnityEditor;
using UnityEngine;

namespace MiniGame.Golf.Editor
{
    /// <summary>
    /// クラブ4種類の GolfClubData を作る（§7.5、Phase 3）。
    /// 「無ければ作る」だけにして、調整済みの値を上書きしない。
    /// </summary>
    public static class GolfClubAssetBuilder
    {
        private const string ClubDirectory = "Assets/_Project/Games/04_Golf/Data/Clubs";

        private readonly struct ClubSpec
        {
            public ClubSpec(string fileName, string displayName, ClubConfig config)
            {
                FileName = fileName;
                DisplayName = displayName;
                Config = config;
            }

            public string FileName { get; }
            public string DisplayName { get; }
            public ClubConfig Config { get; }
        }

        // 最大飛距離（着地まで）が §7.5 の暫定値（22 / 15 / 8ユニット）になるよう、重力6で初速と打ち出し角を合わせている。
        // 飛ぶクラブほど曲がりやすく、パターはほとんど曲がらない
        private static readonly ClubSpec[] Specs =
        {
            new ClubSpec("Club_Driver", "ドライバー", new ClubConfig(16.2f, 15f, 1f)),
            new ClubSpec("Club_Iron", "アイアン", new ClubConfig(10.4f, 28f, 0.8f)),
            new ClubSpec("Club_Wedge", "ウェッジ", new ClubConfig(7f, 50f, 0.5f)),
            // グリーンの端から端（約8ユニット）まで届く強さ
            new ClubSpec("Club_Putter", "パター", new ClubConfig(6f, 0f, 0.1f, true)),
        };

        /// <summary>飛ぶ順（ドライバー → パター）に並べて返す。切り替えもこの順で回る</summary>
        public static GolfClubData[] EnsureClubs()
        {
            if (!Directory.Exists(ClubDirectory)) Directory.CreateDirectory(ClubDirectory);

            var clubs = new GolfClubData[Specs.Length];
            for (int i = 0; i < Specs.Length; i++)
            {
                clubs[i] = EnsureClub(Specs[i]);
            }

            AssetDatabase.SaveAssets();
            return clubs;
        }

        private static GolfClubData EnsureClub(ClubSpec spec)
        {
            string path = $"{ClubDirectory}/{spec.FileName}.asset";
            var club = AssetDatabase.LoadAssetAtPath<GolfClubData>(path);
            if (club != null) return club;

            club = ScriptableObject.CreateInstance<GolfClubData>();
            AssetDatabase.CreateAsset(club, path);

            var so = new SerializedObject(club);
            so.FindProperty("_displayName").stringValue = spec.DisplayName;
            SerializedProperty config = so.FindProperty("_config");
            config.FindPropertyRelative(nameof(ClubConfig.MaxLaunchSpeed)).floatValue = spec.Config.MaxLaunchSpeed;
            config.FindPropertyRelative(nameof(ClubConfig.LaunchAngleDegrees)).floatValue = spec.Config.LaunchAngleDegrees;
            config.FindPropertyRelative(nameof(ClubConfig.CurveFactor)).floatValue = spec.Config.CurveFactor;
            config.FindPropertyRelative(nameof(ClubConfig.IsPutter)).boolValue = spec.Config.IsPutter;
            so.ApplyModifiedPropertiesWithoutUndo();

            return club;
        }
    }
}
