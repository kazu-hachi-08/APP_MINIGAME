using System.Collections.Generic;
using System.IO;
using MiniGame.PenguinWars.Battle;
using UnityEditor;
using UnityEngine;

namespace MiniGame.PenguinWars.Editor
{
    /// <summary>
    /// キャラデータ（Data/Units/Unit_NNN.asset）とカタログを作る。
    /// 数値は UnitDefinitions＋UnitStatFormula、見た目は UnitLooks が正。既にあるアセットも毎回上書きする
    /// （調整の置き場所を定義表1か所にするため。同じパスに書き直すので GUID は変わらず、編成や参照は切れない）。
    /// カタログは毎回 Data/Units/ から集め直す
    /// </summary>
    public static class PenguinUnitAssetGenerator
    {
        private const string DataDirectory = "Assets/_Project/Games/100_PenguinWars/Data";
        private const string UnitsDirectory = DataDirectory + "/Units";
        private const string CatalogPath = DataDirectory + "/PenguinUnitCatalog.asset";
        /// <summary>SceneBuilder から呼ぶ。生成メニューを Rebuild PenguinWars 1つにまとめるため</summary>
        public static PenguinUnitCatalog EnsureAssets()
        {
            EnsureDirectory(UnitsDirectory);
            foreach (UnitDefinition def in UnitDefinitions.All)
            {
                WriteUnit(def);
            }
            AssetDatabase.SaveAssets();

            PenguinUnitCatalog catalog = EnsureCatalog();
            FillCatalog(catalog);
            AssetDatabase.SaveAssets();
            return catalog;
        }

        private static void WriteUnit(UnitDefinition def)
        {
            string path = $"{UnitsDirectory}/Unit_{def.No:000}.asset";
            var unit = AssetDatabase.LoadAssetAtPath<PenguinUnitData>(path);
            bool isNew = unit == null;
            if (isNew) unit = ScriptableObject.CreateInstance<PenguinUnitData>();

            // 生成済みのスプライト欄は触らない（アート生成メニューが書き込む）
            var so = new SerializedObject(unit);
            WriteStats(so, def.Name, UnitStatFormula.Calculate(def));
            WriteLook(so.FindProperty("_look"), UnitLooks.Get(def.No));
            so.ApplyModifiedPropertiesWithoutUndo();

            if (isNew) AssetDatabase.CreateAsset(unit, path);
            else EditorUtility.SetDirty(unit);
        }

        private static void WriteStats(SerializedObject so, string name, UnitStats stats)
        {
            so.FindProperty("_no").intValue = stats.UnitNo;
            so.FindProperty("_displayName").stringValue = name;
            so.FindProperty("_role").enumValueIndex = (int)stats.Role;
            so.FindProperty("_cost").intValue = stats.Cost;
            so.FindProperty("_cooldown").floatValue = stats.Cooldown;
            so.FindProperty("_maxHp").intValue = stats.MaxHp;
            so.FindProperty("_attack").intValue = stats.Attack;
            so.FindProperty("_range").floatValue = stats.Range;
            so.FindProperty("_attackInterval").floatValue = stats.AttackInterval;
            so.FindProperty("_windup").floatValue = stats.Windup;
            so.FindProperty("_moveSpeed").floatValue = stats.MoveSpeed;
            so.FindProperty("_isAreaAttack").boolValue = stats.IsAreaAttack;
            so.FindProperty("_knockbackCount").intValue = stats.KnockbackCount;
            WriteAbilities(so.FindProperty("_abilities"), stats.Abilities);
        }

        private static void WriteAbilities(SerializedProperty list, IReadOnlyList<UnitAbility> abilities)
        {
            list.arraySize = abilities.Count;
            for (int i = 0; i < abilities.Count; i++)
            {
                SerializedProperty entry = list.GetArrayElementAtIndex(i);
                // enum の SerializedProperty は宣言順の添字で指定する
                entry.FindPropertyRelative("_type").enumValueIndex = (int)abilities[i].Type;
                entry.FindPropertyRelative("_chance").floatValue = abilities[i].Chance;
                entry.FindPropertyRelative("_duration").floatValue = abilities[i].Duration;
            }
        }

        private static void WriteLook(SerializedProperty look, PenguinLook def)
        {
            look.FindPropertyRelative("_body").stringValue = def.Body;
            look.FindPropertyRelative("_bodyColor").stringValue = def.BodyColor;
            look.FindPropertyRelative("_head").stringValue = def.Head;
            look.FindPropertyRelative("_hand").stringValue = def.Hand;
            look.FindPropertyRelative("_back").stringValue = def.Back;
            look.FindPropertyRelative("_scale").intValue = def.Scale;
        }

        private static PenguinUnitCatalog EnsureCatalog()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<PenguinUnitCatalog>(CatalogPath);
            if (catalog != null) return catalog;

            catalog = ScriptableObject.CreateInstance<PenguinUnitCatalog>();
            AssetDatabase.CreateAsset(catalog, CatalogPath);
            return catalog;
        }

        /// <summary>Data/Units/ のアセットを No 順で並べる。2人で別々にキャラを足しても、ここを実行すれば一覧に載る</summary>
        private static void FillCatalog(PenguinUnitCatalog catalog)
        {
            var units = new List<PenguinUnitData>();
            foreach (string guid in AssetDatabase.FindAssets("t:PenguinUnitData", new[] { UnitsDirectory }))
            {
                var unit = AssetDatabase.LoadAssetAtPath<PenguinUnitData>(AssetDatabase.GUIDToAssetPath(guid));
                if (unit != null) units.Add(unit);
            }
            units.Sort((a, b) => a.No.CompareTo(b.No));

            var so = new SerializedObject(catalog);
            SerializedProperty list = so.FindProperty("_units");
            list.arraySize = units.Count;
            for (int i = 0; i < units.Count; i++)
            {
                list.GetArrayElementAtIndex(i).objectReferenceValue = units[i];
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
        }

        /// <summary>ディスクに作っただけだと CreateAsset が親フォルダを見つけられないことがあるので取り込み直す</summary>
        private static void EnsureDirectory(string path)
        {
            if (Directory.Exists(path)) return;

            Directory.CreateDirectory(path);
            AssetDatabase.Refresh();
        }
    }
}
