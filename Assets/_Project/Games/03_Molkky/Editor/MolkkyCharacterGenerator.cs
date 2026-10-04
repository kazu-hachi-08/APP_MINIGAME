using System.IO;
using UnityEditor;
using UnityEngine;

namespace MiniGame.Molkky.Editor
{
    /// <summary>
    /// キャラのデータアセットとカタログを初期値で生成するエディタユーティリティ。
    /// 既にあるアセットは上書きしない。Inspector で調整した倍率やセリフを消さないため。
    /// </summary>
    public static class MolkkyCharacterGenerator
    {
        private const string DataDirectory = "Assets/_Project/Games/03_Molkky/Data/Characters";
        private const string CatalogPath = DataDirectory + "/MolkkyCharacterCatalog.asset";

        /// <summary>
        /// MolkkyArtGenerator.CharacterIds と同じ並び。先頭のバランス型が初期選択になる。
        /// ここは初期値なので、生成後の調整はアセットの数値を直接変える
        /// </summary>
        private static readonly (string Name, float Power, float Control, float StickLength,
            float PowerShotSpeed, float PowerShotSpread, string VictoryLine)[] Defaults =
        {
            ("バランス型", 1f, 1f, 1f, 1.2f, 8f, "ぴったり50点！"),
            ("パワー型", 1.2f, 0.8f, 1f, 1.35f, 6f, "力こそパワー！"),
            ("精密型", 0.95f, 1.15f, 0.9f, 1.1f, 10f, "計算どおり！"),
            ("ロング棒", 0.9f, 1f, 1.3f, 1.2f, 8f, "まとめていただき！"),
            ("ショート棒", 1f, 1.25f, 0.8f, 1.1f, 10f, "一本釣り！"),
            ("豪腕ロング", 1.15f, 0.8f, 1.15f, 1.3f, 7f, "全部まとめてドーン！"),
        };

        /// <summary>
        /// カタログと各キャラが無ければ作り、カタログを返す（MolkkySceneBuilder から呼ばれる）。
        /// カタログが既にあっても、後から Defaults に足したキャラは末尾に追加する。
        /// 既存の番号を並べ替えると、オンラインで送る CharacterIndex の意味が端末ごとにずれるため末尾にだけ足す
        /// </summary>
        public static MolkkyCharacterCatalog EnsureGenerated()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<MolkkyCharacterCatalog>(CatalogPath);
            if (catalog != null && catalog.Count >= Defaults.Length) return catalog;

            if (!Directory.Exists(DataDirectory))
            {
                Directory.CreateDirectory(DataDirectory);
            }

            MolkkyArtGenerator.EnsureGenerated();

            bool isNew = catalog == null;
            if (isNew)
            {
                catalog = ScriptableObject.CreateInstance<MolkkyCharacterCatalog>();
            }

            AppendMissingCharacters(catalog);

            if (isNew)
            {
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }
            else
            {
                EditorUtility.SetDirty(catalog);
            }
            AssetDatabase.SaveAssets();
            return catalog;
        }

        /// <summary>カタログに入っている分はそのまま残し、足りない分だけ Defaults の並びで末尾に足す</summary>
        private static void AppendMissingCharacters(MolkkyCharacterCatalog catalog)
        {
            var so = new SerializedObject(catalog);
            SerializedProperty list = so.FindProperty("_characters");
            int existingCount = list.arraySize;
            list.arraySize = Defaults.Length;
            for (int i = existingCount; i < Defaults.Length; i++)
            {
                list.GetArrayElementAtIndex(i).objectReferenceValue =
                    EnsureCharacter(MolkkyArtGenerator.CharacterIds[i], Defaults[i]);
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static MolkkyCharacterData EnsureCharacter(string id,
            (string Name, float Power, float Control, float StickLength,
            float PowerShotSpeed, float PowerShotSpread, string VictoryLine) values)
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
            so.FindProperty("_powerShotSpeedMultiplier").floatValue = values.PowerShotSpeed;
            so.FindProperty("_powerShotAngleSpread").floatValue = values.PowerShotSpread;
            so.FindProperty("_victoryLine").stringValue = values.VictoryLine;
            so.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.CreateAsset(character, path);
            return character;
        }
    }
}
