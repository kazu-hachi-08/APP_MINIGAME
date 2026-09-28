using System.IO;
using UnityEditor;
using UnityEngine;

namespace MiniGame.Molkky.Editor
{
    /// <summary>
    /// キャラのデータアセットとカタログを初期値で生成するエディタユーティリティ（仕様書 §20.2）。
    /// 既にあるアセットは上書きしない。Inspector で調整した倍率やセリフを消さないため。
    /// </summary>
    public static class MolkkyCharacterGenerator
    {
        private const string DataDirectory = "Assets/_Project/Games/03_Molkky/Data/Characters";
        private const string CatalogPath = DataDirectory + "/MolkkyCharacterCatalog.asset";

        /// <summary>
        /// MolkkyArtGenerator.CharacterIds と同じ並び。先頭のバランス型が初期選択になる。
        /// 倍率は仕様書 §20.2 の初期値。生成後はアセットの数値を直接調整する
        /// </summary>
        private static readonly (string Name, float Power, float Control, float StickLength, string VictoryLine)[] Defaults =
        {
            ("バランス型", 1f, 1f, 1f, "ぴったり50点！"),
            ("パワー型", 1.2f, 0.8f, 1f, "力こそパワー！"),
            ("精密型", 0.85f, 1.3f, 0.9f, "計算どおり！"),
            ("ロング棒", 0.9f, 1f, 1.3f, "まとめていただき！"),
        };

        [MenuItem("Tools/MiniGame/Generate Molkky Character Data", false, 6)]
        public static void Generate()
        {
            EnsureGenerated();
            Debug.Log($"[MolkkyCharacterGenerator] キャラのデータを確認しました: {DataDirectory}");
        }

        /// <summary>カタログと各キャラが無ければ作り、カタログを返す（MolkkySceneBuilder から呼ばれる）</summary>
        public static MolkkyCharacterCatalog EnsureGenerated()
        {
            var existing = AssetDatabase.LoadAssetAtPath<MolkkyCharacterCatalog>(CatalogPath);
            if (existing != null) return existing;

            if (!Directory.Exists(DataDirectory))
            {
                Directory.CreateDirectory(DataDirectory);
            }

            MolkkyArtGenerator.EnsureGenerated();

            var characters = new MolkkyCharacterData[Defaults.Length];
            for (int i = 0; i < Defaults.Length; i++)
            {
                characters[i] = EnsureCharacter(MolkkyArtGenerator.CharacterIds[i], Defaults[i]);
            }

            var catalog = ScriptableObject.CreateInstance<MolkkyCharacterCatalog>();
            var so = new SerializedObject(catalog);
            SerializedProperty list = so.FindProperty("_characters");
            list.arraySize = characters.Length;
            for (int i = 0; i < characters.Length; i++)
            {
                list.GetArrayElementAtIndex(i).objectReferenceValue = characters[i];
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.CreateAsset(catalog, CatalogPath);
            AssetDatabase.SaveAssets();
            return catalog;
        }

        private static MolkkyCharacterData EnsureCharacter(string id,
            (string Name, float Power, float Control, float StickLength, string VictoryLine) values)
        {
            string path = $"{DataDirectory}/MolkkyChar_{id}.asset";
            var character = AssetDatabase.LoadAssetAtPath<MolkkyCharacterData>(path);
            if (character != null) return character;

            character = ScriptableObject.CreateInstance<MolkkyCharacterData>();
            var so = new SerializedObject(character);
            so.FindProperty("_displayName").stringValue = values.Name;
            so.FindProperty("_backSprite").objectReferenceValue =
                MolkkyArtGenerator.Load(MolkkyArtGenerator.CharacterBackName(id));
            so.FindProperty("_frontSprite").objectReferenceValue =
                MolkkyArtGenerator.Load(MolkkyArtGenerator.CharacterFrontName(id));
            so.FindProperty("_powerMultiplier").floatValue = values.Power;
            so.FindProperty("_controlMultiplier").floatValue = values.Control;
            so.FindProperty("_stickLengthMultiplier").floatValue = values.StickLength;
            so.FindProperty("_victoryLine").stringValue = values.VictoryLine;
            so.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.CreateAsset(character, path);
            return character;
        }
    }
}
