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
            if (existing != null)
            {
                // 必殺技より前に作られた選手データにも、後から技を割り当てる
                EnsureSpecials(existing);
                return existing;
            }

            if (!Directory.Exists(DataDirectory))
            {
                Directory.CreateDirectory(DataDirectory);
            }

            TableTennisArtGenerator.EnsureGenerated();

            // 先頭をスタンダードにする（選択画面の初期値になる）
            var catalog = ScriptableObject.CreateInstance<LoadoutCatalog>();
            catalog.Characters = new[]
            {
                CreateCharacter("MARIO", PlayStyle.Standard, TableTennisArtGenerator.MarioName, 1f, 1f, 1f),
                CreateCharacter("KOKINIWA", PlayStyle.Technique, TableTennisArtGenerator.KokiniwaName, 1.2f, 0.9f, 1.2f),
                CreateCharacter("YOKOZUNA", PlayStyle.Power, TableTennisArtGenerator.YokozunaName, 0.8f, 1.2f, 0.9f)
            };
            catalog.Rackets = new[]
            {
                CreateRacket(PlayStyle.Standard, TableTennisArtGenerator.StandardRacketName, 1f, 1f, 1f),
                CreateRacket(PlayStyle.Technique, TableTennisArtGenerator.TechniqueRacketName, 0.9f, 1.3f, 0.8f),
                CreateRacket(PlayStyle.Power, TableTennisArtGenerator.PowerRacketName, 1.15f, 0.8f, 1.2f)
            };

            AssetDatabase.CreateAsset(catalog, CatalogPath);
            EnsureSpecials(catalog);
            AssetDatabase.SaveAssets();
            return catalog;
        }

        /// <summary>
        /// 技が未設定の選手に、タイプに応じた弱必殺技を割り当てる。
        /// 設定済みの選手は触らない（Inspector で差し替えた技を消さないため）
        /// </summary>
        private static void EnsureSpecials(LoadoutCatalog catalog)
        {
            foreach (CharacterData character in catalog.Characters)
            {
                if (character == null) continue;

                if (character.WeakSpecial == null)
                {
                    character.WeakSpecial = EnsureWeakSpecial(character.Style);
                    EditorUtility.SetDirty(character);
                }

                if (character.StrongSpecial == null)
                {
                    character.StrongSpecial = EnsureStrongSpecial(character.Style);
                    EditorUtility.SetDirty(character);
                }
            }

            AssetDatabase.SaveAssets();
        }

        private static SpecialData EnsureStrongSpecial(PlayStyle style)
        {
            string path = $"{DataDirectory}/Special_Strong_{style}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<SpecialData>(path);
            if (existing != null) return existing;

            var data = ScriptableObject.CreateInstance<SpecialData>();
            switch (style)
            {
                // スタンダード: ラリーが終わるまで絶対にミスしない
                case PlayStyle.Standard:
                    data.DisplayName = "スターラリー";
                    data.BallColor = new Color(1f, 0.55f, 0.9f);
                    data.LastsForRally = true;
                    data.PerfectTiming = true;
                    data.ReachMultiplier = 2f;
                    break;

                // テクニック: 相手コートでバウンドするまで消える。NPCは見えない球の判断に表れないので返球率で表す
                case PlayStyle.Technique:
                    data.DisplayName = "消える魔球";
                    data.BallColor = new Color(0.55f, 1f, 0.6f);
                    data.Invisible = true;
                    data.OpponentReturnRateMultiplier = 0.5f;
                    break;

                // パワー: 台を揺らして低く滑るバウンドにする（エッジより低い）
                default:
                    data.DisplayName = "四股ドスコイ";
                    data.BallColor = new Color(1f, 0.6f, 0.2f);
                    data.BounceHeightMultiplier = 0.25f;
                    data.OpponentReturnRateMultiplier = 0.6f;
                    break;
            }

            AssetDatabase.CreateAsset(data, path);
            return data;
        }

        private static SpecialData EnsureWeakSpecial(PlayStyle style)
        {
            string path = $"{DataDirectory}/Special_Weak_{style}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<SpecialData>(path);
            if (existing != null) return existing;

            var data = ScriptableObject.CreateInstance<SpecialData>();
            switch (style)
            {
                // スタンダード: 素直に速い球
                case PlayStyle.Standard:
                    data.DisplayName = "ファイアショット";
                    data.BallColor = new Color(1f, 0.35f, 0.15f);
                    data.SpeedMultiplier = 1.25f;
                    break;

                // テクニック: バウンド後に大きく横へ逃げる球
                case PlayStyle.Technique:
                    data.DisplayName = "魔球カーブ";
                    data.BallColor = new Color(0.35f, 0.8f, 1f);
                    data.SideSpinMultiplier = 2.5f;
                    data.MinSideSpin = 1.5f;
                    break;

                // パワー: 相手をのけぞらせて出足を止める
                default:
                    data.DisplayName = "つっぱり";
                    data.BallColor = new Color(0.75f, 0.45f, 1f);
                    data.StunDuration = 1f;
                    data.StunMoveMultiplier = 0.2f;
                    break;
            }

            AssetDatabase.CreateAsset(data, path);
            return data;
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
