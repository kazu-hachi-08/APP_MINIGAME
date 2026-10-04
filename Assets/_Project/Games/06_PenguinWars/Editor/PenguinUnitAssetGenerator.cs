using System.Collections.Generic;
using System.IO;
using MiniGame.PenguinWars.Battle;
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
            public readonly int KnockbackCount;
            public readonly UnitAbility[] Abilities;

            public UnitDef(int no, string name, int cost, float cooldown, int hp, int attack,
                float range, float interval, float windup, float speed, bool area, int knockbackCount,
                params UnitAbility[] abilities)
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
                KnockbackCount = knockbackCount;
                Abilities = abilities;
            }
        }

        // 役割と能力が一通り揃う仮の10体（実装計画 Phase 5）。数値は手入れで、Phase 8 で計算式に置き換える
        // 並び: No, 名前, コスト, 再生産, HP, 攻撃, 射程, 攻撃間隔, 発生, 速度, 範囲, ノックバック回数, 能力…
        private static readonly UnitDef[] PlaceholderUnits =
        {
            new UnitDef(1, "ペンギン", 75, 2f, 100, 8, 1.4f, 1.2f, 0.3f, 1.0f, false, 3),
            new UnitDef(2, "かべペンギン", 150, 4f, 350, 5, 1.4f, 1.5f, 0.3f, 0.8f, false, 2),
            new UnitDef(4, "ヘルメットペンギン", 150, 5f, 250, 8, 1.4f, 1.2f, 0.3f, 0.8f, false, 1,
                new UnitAbility(UnitAbilityType.Steadfast)),
            new UnitDef(11, "おのペンギン", 300, 6f, 200, 40, 1.5f, 1.5f, 0.5f, 1.0f, true, 3),
            new UnitDef(13, "ボクサーペンギン", 350, 7f, 220, 35, 1.4f, 1.0f, 0.3f, 1.2f, false, 3,
                new UnitAbility(UnitAbilityType.Knockback, 0.3f)),
            new UnitDef(19, "バイクペンギン", 500, 10f, 250, 60, 1.4f, 1.5f, 0.3f, 2.0f, false, 3,
                new UnitAbility(UnitAbilityType.CastleKiller)),
            new UnitDef(23, "ゆみペンギン", 450, 8f, 120, 30, 3.5f, 2.5f, 0.6f, 0.8f, false, 2),
            new UnitDef(26, "のっぽペンギン", 900, 15f, 300, 50, 4.5f, 3.0f, 0.8f, 0.7f, true, 2),
            new UnitDef(34, "れいとうペンギン", 600, 12f, 250, 15, 2.5f, 2.0f, 0.4f, 0.9f, false, 3,
                new UnitAbility(UnitAbilityType.Freeze, 0.5f, 2f)),
            new UnitDef(43, "きょだいペンギン", 2500, 40f, 2000, 150, 2.0f, 3.0f, 1.0f, 0.6f, true, 1,
                new UnitAbility(UnitAbilityType.Steadfast)),
        };

        // 見た目（仕様書 §5.5「見た目」列。実装計画 Phase 6）。ここに無い No は基本ペンギンになる
        private static readonly Dictionary<int, PenguinLook> Looks = new Dictionary<int, PenguinLook>
        {
            [2] = new PenguinLook(body: "wide"),
            [4] = new PenguinLook(head: "helmet"),
            [11] = new PenguinLook(hand: "axe"),
            [13] = new PenguinLook(hand: "glove"),
            [19] = new PenguinLook(back: "bike"),
            [23] = new PenguinLook(hand: "bow"),
            [26] = new PenguinLook(body: "tall"),
            [34] = new PenguinLook(back: "freezer"),
            [43] = new PenguinLook(scale: 3),
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
            var existing = AssetDatabase.LoadAssetAtPath<PenguinUnitData>(path);
            if (existing == null)
            {
                CreateUnit(def, path);
                return;
            }

            // Phase 5 までに作ったアセットには見た目が無い。数値と違い手で調整していないはずなので、既定のままなら書き足す
            if (existing.Look.IsDefault && Looks.ContainsKey(def.No))
            {
                var so = new SerializedObject(existing);
                WriteLook(so.FindProperty("_look"), def.No);
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(existing);
            }
        }

        private static void CreateUnit(UnitDef def, string path)
        {
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
            so.FindProperty("_knockbackCount").intValue = def.KnockbackCount;
            WriteAbilities(so.FindProperty("_abilities"), def.Abilities);
            WriteLook(so.FindProperty("_look"), def.No);
            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(unit, path);
        }

        private static void WriteAbilities(SerializedProperty list, UnitAbility[] abilities)
        {
            list.arraySize = abilities.Length;
            for (int i = 0; i < abilities.Length; i++)
            {
                SerializedProperty entry = list.GetArrayElementAtIndex(i);
                // enum の SerializedProperty は宣言順の添字で指定する
                entry.FindPropertyRelative("_type").enumValueIndex = (int)abilities[i].Type;
                entry.FindPropertyRelative("_chance").floatValue = abilities[i].Chance;
                entry.FindPropertyRelative("_duration").floatValue = abilities[i].Duration;
            }
        }

        private static void WriteLook(SerializedProperty look, int no)
        {
            if (!Looks.TryGetValue(no, out PenguinLook def)) def = new PenguinLook();
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
