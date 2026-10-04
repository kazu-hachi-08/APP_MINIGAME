using UnityEditor;
using UnityEngine;

namespace MiniGame.PenguinWars.Editor
{
    /// <summary>オンライン対戦のステージ（仕様書 §3.4）。1ステージ1アセットで Data/Stages に置く</summary>
    public static partial class PenguinWarsSceneBuilder
    {
        private const string StageDirectory = DataDirectory + "/Stages";

        private readonly struct StageSpec
        {
            public readonly string FileName;
            public readonly string DisplayName;
            public readonly float FieldLength;
            public readonly int CastleHp;
            public readonly float CannonRangeRatio;
            public readonly Color Tint;
            public readonly float AvalancheInterval;

            public StageSpec(string fileName, string displayName, float fieldLength, int castleHp, float cannonRangeRatio,
                Color tint, float avalancheInterval)
            {
                FileName = fileName;
                DisplayName = displayName;
                FieldLength = fieldLength;
                CastleHp = castleHp;
                CannonRangeRatio = cannonRangeRatio;
                Tint = tint;
                AvalancheInterval = avalancheInterval;
            }
        }

        // 最初に作るときの値。作った後の調整はアセットの Inspector で行う
        private static readonly StageSpec[] StageSpecs =
        {
            // 狭くて開始すぐ押し合いになる
            new StageSpec("Stage_01_IcePath", "こおりの小道", 18f, 4000, 0.6f, new Color(0.8f, 0.93f, 1f), 0f),
            // 砲が届きにくく、守りの切り札が弱い
            new StageSpec("Stage_02_SnowMountain", "ゆきやま", 24f, 5000, 0.4f, Color.white, 0f),
            // 真ん中の押し合いを定期的になだれが崩す
            new StageSpec("Stage_03_AvalancheValley", "なだれの谷", 28f, 4500, 0.6f, new Color(0.78f, 0.8f, 0.92f), 60f),
        };

        /// <summary>遊びながら調整した数値を消さないよう、既にあるステージは作り直さない</summary>
        private static PenguinStageData[] EnsureStages()
        {
            EnsureDirectory(StageDirectory);
            var stages = new PenguinStageData[StageSpecs.Length];
            for (int i = 0; i < stages.Length; i++) stages[i] = EnsureStage(StageSpecs[i]);
            AssetDatabase.SaveAssets();
            return stages;
        }

        private static PenguinStageData EnsureStage(StageSpec spec)
        {
            string path = $"{StageDirectory}/{spec.FileName}.asset";
            var stage = AssetDatabase.LoadAssetAtPath<PenguinStageData>(path);
            if (stage != null) return stage;

            stage = ScriptableObject.CreateInstance<PenguinStageData>();
            AssetDatabase.CreateAsset(stage, path);
            var so = new SerializedObject(stage);
            so.FindProperty("_displayName").stringValue = spec.DisplayName;
            so.FindProperty("_fieldLength").floatValue = spec.FieldLength;
            so.FindProperty("_castleHp").intValue = spec.CastleHp;
            so.FindProperty("_cannonRangeRatio").floatValue = spec.CannonRangeRatio;
            so.FindProperty("_tint").colorValue = spec.Tint;
            so.FindProperty("_avalancheInterval").floatValue = spec.AvalancheInterval;
            so.ApplyModifiedPropertiesWithoutUndo();
            return stage;
        }
    }
}
