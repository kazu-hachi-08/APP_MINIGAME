using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MiniGame.PenguinWars.Editor
{
    /// <summary>
    /// キャラデータ（Data/Units/Unit_NNN.asset）とカタログを作る。
    /// 既にあるアセットの数値は上書きしない（遊びながら調整した値を消さないため）。カタログは毎回 Data/Units/ から集め直す
    /// </summary>
    public static class PenguinUnitAssetGenerator
    {
        private const string DataDirectory = "Assets/_Project/Games/06_PenguinWars/Data";
        private const string UnitsDirectory = DataDirectory + "/Units";
        private const string CatalogPath = DataDirectory + "/PenguinUnitCatalog.asset";
        private const string LogPrefix = "[PenguinUnitAssetGenerator]";

        private readonly struct UnitDef
        {
            public readonly int No;
            public readonly string Name;
            public readonly int Cost;
            public readonly float Cooldown;
            public readonly int Hp;
            public readonly int Attack;
            public readonly float Range;
            public readonly float Interval;
            public readonly float Windup;
            public readonly float Speed;
            public readonly bool Area;

            public UnitDef(int no, string name, int cost, float cooldown, int hp, int attack,
                float range, float interval, float windup, float speed, bool area)
            {
                No = no;
                Name = name;
                Cost = cost;
                Cooldown = cooldown;
                Hp = hp;
                Attack = attack;
                Range = range;
                Interval = interval;
                Windup = windup;
                Speed = speed;
                Area = area;
            }
        }

        // 壁・アタッカー・遠距離の仮の3体（実装計画 Phase 2）。コスト・再生産は Phase 3 で使う
        private static readonly UnitDef[] PlaceholderUnits =
        {
            new UnitDef(1, "ペンギン", 75, 2f, 100, 8, 1.4f, 1.2f, 0.3f, 1.0f, false),
            new UnitDef(11, "おのペンギン", 300, 6f, 200, 40, 1.5f, 1.5f, 0.5f, 1.0f, true),
            new UnitDef(23, "ゆみペンギン", 450, 8f, 120, 30, 3.5f, 2.5f, 0.6f, 0.8f, false),
        };

        [MenuItem("Tools/MiniGame/Generate PenguinWars Units", false, 8)]
        public static void Generate()
        {
            PenguinUnitCatalog catalog = EnsureAssets();
            Debug.Log($"{LogPrefix} キャラデータとカタログを更新しました（{catalog.Units.Count}体）: {CatalogPath}");
        }

        /// <summary>SceneBuilder からも呼ぶ。Rebuild だけで遊べる状態にするため</summary>
        public static PenguinUnitCatalog EnsureAssets()
        {
            EnsureDirectory(UnitsDirectory);
            foreach (UnitDef def in PlaceholderUnits)
            {
                EnsureUnit(def);
            }
            AssetDatabase.SaveAssets();

            PenguinUnitCatalog catalog = EnsureCatalog();
            FillCatalog(catalog);
            AssetDatabase.SaveAssets();
            return catalog;
        }

        private static void EnsureUnit(UnitDef def)
        {
            string path = $"{UnitsDirectory}/Unit_{def.No:000}.asset";
            if (AssetDatabase.LoadAssetAtPath<PenguinUnitData>(path) != null) return;

            var unit = ScriptableObject.CreateInstance<PenguinUnitData>();
            var so = new SerializedObject(unit);
            so.FindProperty("_no").intValue = def.No;
            so.FindProperty("_displayName").stringValue = def.Name;
            so.FindProperty("_cost").intValue = def.Cost;
            so.FindProperty("_cooldown").floatValue = def.Cooldown;
            so.FindProperty("_maxHp").intValue = def.Hp;
            so.FindProperty("_attack").intValue = def.Attack;
            so.FindProperty("_range").floatValue = def.Range;
            so.FindProperty("_attackInterval").floatValue = def.Interval;
            so.FindProperty("_windup").floatValue = def.Windup;
            so.FindProperty("_moveSpeed").floatValue = def.Speed;
            so.FindProperty("_isAreaAttack").boolValue = def.Area;
            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(unit, path);
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
