using UnityEditor;
using UnityEngine;

namespace MiniGame.PenguinWars.Editor
{
    /// <summary>オンライン対戦のステージ（仕様書 §3.4）。1ステージ1アセットで Data/Stages に置く</summary>
    public static partial class PenguinWarsSceneBuilder
    {
        private const string VersusStageDirectory = DataDirectory + "/Stages";
        // 両方の城が1画面（BattleCamera の横幅16 − 左右の余白2×2）に収まる長さ。城が遠いと中央で押し合ったまま膠着するため
        private const float VersusFieldLength = 12f;
        // 戦場が短いと 60% では中央を越えて守りが強すぎるので、自陣側だけに届くようにする
        private const float VersusCannonRangeRatio = 0.4f;

        private readonly struct VersusStageSpec
        {
            public readonly string FileName;
            public readonly string DisplayName;
            public readonly float FieldLength;
            public readonly int CastleHp;
            public readonly float CannonRangeRatio;
            public readonly Color Tint;
            public readonly float AvalancheInterval;

            public VersusStageSpec(string fileName, string displayName, float fieldLength, int castleHp, float cannonRangeRatio,
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
        private static readonly VersusStageSpec[] VersusStageSpecs =
        {
            // 城HPが低く、早く決着がつく
            new VersusStageSpec("Stage_01_IcePath", "こおりの小道", VersusFieldLength, 4000, VersusCannonRangeRatio, new Color(0.8f, 0.93f, 1f), 0f),
            // 城HPが高く、じっくり攻め合う
            new VersusStageSpec("Stage_02_SnowMountain", "ゆきやま", VersusFieldLength, 5000, VersusCannonRangeRatio, Color.white, 0f),
            // 真ん中の押し合いを定期的になだれが崩す
            new VersusStageSpec("Stage_03_AvalancheValley", "なだれの谷", VersusFieldLength, 4500, VersusCannonRangeRatio, new Color(0.78f, 0.8f, 0.92f), 60f),
        };

        /// <summary>遊びながら調整した数値を消さないよう、既にあるステージは作り直さない</summary>
        private static PenguinStageData[] EnsureStages()
        {
            EnsureDirectory(VersusStageDirectory);
            var stages = new PenguinStageData[VersusStageSpecs.Length];
            for (int i = 0; i < stages.Length; i++) stages[i] = EnsureStage(VersusStageSpecs[i]);
            AssetDatabase.SaveAssets();
            return stages;
        }

        private static PenguinStageData EnsureStage(VersusStageSpec spec)
        {
            string path = $"{VersusStageDirectory}/{spec.FileName}.asset";
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
