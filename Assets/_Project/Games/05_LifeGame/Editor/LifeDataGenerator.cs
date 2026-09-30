using System.IO;
using UnityEditor;
using UnityEngine;

namespace MiniGame.LifeGame.Editor
{
    /// <summary>
    /// キャラのデータアセットとカタログを初期値で生成する（仕様書 §9）。
    /// 既にあるアセットは上書きしない。Inspector で調整した説明文やセリフを消さないため。
    /// </summary>
    public static class LifeDataGenerator
    {
        private const string CharacterDirectory = "Assets/_Project/Games/05_LifeGame/Data/Characters";
        private const string CatalogPath = CharacterDirectory + "/LifeCharacterCatalog.asset";

        /// <summary>先頭が人間の初期選択になる。ここは初期値なので、生成後の調整はアセットを直接変える</summary>
        private static readonly (string Id, string Name, LifeAbility Ability, string AbilityText, string VictoryLine)[] Characters =
        {
            ("Balance", "バランス", LifeAbility.StartMoney, "最初の所持金が100多い", "堅実な人生だった！"),
            ("Worker", "がんばり屋", LifeAbility.Salary, "給料が1割多い", "働いた分だけ報われる！"),
            ("Lucky", "らっきー", LifeAbility.Reroll, "ルーレットを1回だけ振り直せる", "運も実力のうち！"),
            ("Saver", "しっかり者", LifeAbility.Thrift, "出費・病気・事故・火事の支払いが1割少ない", "節約は最強！"),
        };

        /// <summary>カタログと各キャラが無ければ作り、カタログを返す（LifeGameSceneBuilder から呼ばれる）</summary>
        public static LifeCharacterCatalog EnsureCharacters()
        {
            var existing = AssetDatabase.LoadAssetAtPath<LifeCharacterCatalog>(CatalogPath);
            if (existing != null) return existing;

            Directory.CreateDirectory(CharacterDirectory);

            var catalog = ScriptableObject.CreateInstance<LifeCharacterCatalog>();
            var so = new SerializedObject(catalog);
            SerializedProperty list = so.FindProperty("_characters");
            list.arraySize = Characters.Length;
            for (int i = 0; i < Characters.Length; i++)
            {
                list.GetArrayElementAtIndex(i).objectReferenceValue = EnsureCharacter(Characters[i]);
            }

            so.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.CreateAsset(catalog, CatalogPath);
            AssetDatabase.SaveAssets();
            return catalog;
        }

        private static LifeCharacterData EnsureCharacter(
            (string Id, string Name, LifeAbility Ability, string AbilityText, string VictoryLine) values)
        {
            string path = $"{CharacterDirectory}/LifeChar_{values.Id}.asset";
            var character = AssetDatabase.LoadAssetAtPath<LifeCharacterData>(path);
            if (character != null) return character;

            character = ScriptableObject.CreateInstance<LifeCharacterData>();
            var so = new SerializedObject(character);
            so.FindProperty("_displayName").stringValue = values.Name;
            so.FindProperty("_ability").enumValueIndex = (int)values.Ability;
            so.FindProperty("_abilityText").stringValue = values.AbilityText;
            so.FindProperty("_victoryLine").stringValue = values.VictoryLine;
            so.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.CreateAsset(character, path);
            return character;
        }
    }
}
