using System.IO;
using UnityEditor;
using UnityEngine;

namespace MiniGame.LifeGame.Editor
{
    /// <summary>
    /// キャラ（仕様書 §9）とテーマ（仕様書 §6）のデータアセットを初期値で生成する。
    /// 既にあるアセットは上書きしない。Inspector で調整した説明文やセリフを消さないため。
    /// </summary>
    public static class LifeDataGenerator
    {
        private const string CharacterDirectory = "Assets/_Project/Games/05_LifeGame/Data/Characters";
        private const string CatalogPath = CharacterDirectory + "/LifeCharacterCatalog.asset";
        private const string ThemeDirectory = "Assets/_Project/Games/05_LifeGame/Data/Themes";

        // LifeThemeData の色の並び（LifeThemeDefaults.CellColors と同じ順）
        private static readonly string[] CellColorFields =
            { "_moneyGood", "_moneyBad", "_mishap", "_family", "_purchase", "_job", "_milestone", "_plain" };

        private static readonly string[] RouteFields =
            { "_jobRoute", "_universityRoute", "_freeterRoute", "_safeRoute", "_gambleRoute" };

        /// <summary>先頭が人間の初期選択になる。ここは初期値なので、生成後の調整はアセットを直接変える</summary>
        private static readonly (string Id, string Name, LifeAbility Ability, string AbilityText, string VictoryLine)[] Characters =
        {
            ("Balance", "バランス", LifeAbility.StartMoney, "最初の所持金が50多い", "堅実な人生だった！"),
            ("Worker", "がんばり屋", LifeAbility.Salary, "給料が1割多い", "働いた分だけ報われる！"),
            ("Lucky", "らっきー", LifeAbility.Reroll, "ルーレットを1回だけ振り直せる", "運も実力のうち！"),
            ("Saver", "しっかり者", LifeAbility.Thrift, "出費・病気・事故・火事の支払いが2割少ない", "節約は最強！"),
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

        /// <summary>3テーマのアセットが無ければ作り、現代・ファンタジー・宇宙の順で返す（LifeGameSceneBuilder から呼ばれる）</summary>
        public static LifeThemeData[] EnsureThemes()
        {
            Directory.CreateDirectory(ThemeDirectory);
            var themes = new LifeThemeData[LifeThemeDefaults.All.Length];
            for (int i = 0; i < themes.Length; i++) themes[i] = EnsureTheme(LifeThemeDefaults.All[i]);

            AssetDatabase.SaveAssets();
            return themes;
        }

        private static LifeThemeData EnsureTheme(LifeThemeDefaults values)
        {
            string path = $"{ThemeDirectory}/LifeTheme_{values.Id}.asset";
            var theme = AssetDatabase.LoadAssetAtPath<LifeThemeData>(path);
            if (theme != null) return theme;

            theme = ScriptableObject.CreateInstance<LifeThemeData>();
            var so = new SerializedObject(theme);
            so.FindProperty("_displayName").stringValue = values.Name;
            so.FindProperty("_currency").stringValue = values.Currency;
            SetStrings(so.FindProperty("_jobNames"), values.Jobs);
            SetStrings(so.FindProperty("_houseNames"), values.Houses);
            for (int i = 0; i < RouteFields.Length; i++) so.FindProperty(RouteFields[i]).stringValue = values.Routes[i];

            so.FindProperty("_background").colorValue = values.Background;
            so.FindProperty("_road").colorValue = values.Road;
            for (int i = 0; i < CellColorFields.Length; i++) so.FindProperty(CellColorFields[i]).colorValue = values.CellColors[i];

            SerializedProperty texts = so.FindProperty("_cellTexts");
            texts.arraySize = values.Texts.Length;
            for (int i = 0; i < values.Texts.Length; i++)
            {
                SerializedProperty entry = texts.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("Type").enumValueIndex = (int)values.Texts[i].Type;
                SetStrings(entry.FindPropertyRelative("Lines"), values.Texts[i].Lines);
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(theme, path);
            return theme;
        }

        private static void SetStrings(SerializedProperty list, string[] values)
        {
            list.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) list.GetArrayElementAtIndex(i).stringValue = values[i];
        }
    }
}