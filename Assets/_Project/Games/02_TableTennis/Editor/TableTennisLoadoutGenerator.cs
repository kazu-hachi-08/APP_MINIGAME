using System.IO;
using UnityEditor;
using UnityEngine;

namespace MiniGame.TableTennis.Editor
{
    /// <summary>
    /// 選手・ラケットのデータアセット（§25）を初期値で生成するエディタユーティリティ。
    /// 既にあるアセットは上書きしない。Inspector で調整した値を消さないため。
    /// </summary>
    public static class TableTennisLoadoutGenerator
    {
        private const string DataDirectory = "Assets/_Project/Games/02_TableTennis/Data/Loadout";
        private const string CatalogPath = DataDirectory + "/LoadoutCatalog.asset";

        [MenuItem("Tools/MiniGame/Generate Table Tennis Loadout Data", false, 5)]
        public static void Generate()
        {
            EnsureGenerated();
            Debug.Log($"[TableTennisLoadoutGenerator] 選手・ラケットのデータを確認しました: {DataDirectory}");
        }

        /// <summary>カタログと各データが無ければ作り、カタログを返す（TableTennisSceneBuilder から呼ばれる）</summary>
        public static LoadoutCatalog EnsureGenerated()
        {
            var existing = AssetDatabase.LoadAssetAtPath<LoadoutCatalog>(CatalogPath);
            if (existing != null) return existing;

            if (!Directory.Exists(DataDirectory))
            {
                Directory.CreateDirectory(DataDirectory);
            }

            TableTennisArtGenerator.EnsureGenerated();

            // 先頭をスタンダードにする（選択画面の初期値になる）
            var catalog = ScriptableObject.CreateInstance<LoadoutCatalog>();
            catalog.Characters = new[]
            {
                CreateCharacter("KAZUKI", PlayStyle.Standard, TableTennisArtGenerator.KazukiName, 1f, 1f, 1f),
                CreateCharacter("KOKINIWA", PlayStyle.Technique, TableTennisArtGenerator.KokiniwaName, 1.2f, 0.9f, 1.2f),
                CreateCharacter("YOKOZUNA", PlayStyle.Power, TableTennisArtGenerator.YokozunaName, 0.8f, 1.2f, 0.9f)
            };
            catalog.Rackets = new[]
            {
                CreateRacket(PlayStyle.Standard, TableTennisArtGenerator.StandardRacketName, 1f, 1f, 1f),
                CreateRacket(PlayStyle.Power, TableTennisArtGenerator.PowerRacketName, 1.15f, 0.8f, 1.2f),
                CreateRacket(PlayStyle.Technique, TableTennisArtGenerator.TechniqueRacketName, 0.9f, 1.3f, 0.8f)
            };

            AssetDatabase.CreateAsset(catalog, CatalogPath);
            AssetDatabase.SaveAssets();
            return catalog;
        }

        private static CharacterData CreateCharacter(string displayName, PlayStyle style, string spriteName,
            float moveSpeed, float reach, float swingDuration)
        {
            var data = ScriptableObject.CreateInstance<CharacterData>();
            data.DisplayName = displayName;
            data.Style = style;
            data.BackSprite = TableTennisArtGenerator.Load(spriteName + TableTennisArtGenerator.BackSuffix);
            data.FrontSprite = TableTennisArtGenerator.Load(spriteName + TableTennisArtGenerator.FrontSuffix);
            data.MoveSpeedMultiplier = moveSpeed;
            data.ReachMultiplier = reach;
            data.SwingDurationMultiplier = swingDuration;

            AssetDatabase.CreateAsset(data, $"{DataDirectory}/Character_{displayName}.asset");
            return data;
        }

        private static RacketData CreateRacket(PlayStyle style, string spriteName,
            float speed, float spin, float error)
        {
            var data = ScriptableObject.CreateInstance<RacketData>();
            data.Style = style;
            data.Sprite = TableTennisArtGenerator.Load(spriteName);
            data.SpeedMultiplier = speed;
            data.SpinMultiplier = spin;
            data.ErrorMultiplier = error;

            AssetDatabase.CreateAsset(data, $"{DataDirectory}/Racket_{style}.asset");
            return data;
        }
    }
}
