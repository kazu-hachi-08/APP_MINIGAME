using UnityEngine;

namespace MiniGame.PenguinWars.Editor
{
    /// <summary>演出（煙・火花・魂・ビーム・紙吹雪）の見本と、BattleEvent を演出と音に振り分ける BattleEventPresenter・PenguinWarsAudio・ボス登場の演出</summary>
    public static partial class PenguinWarsSceneBuilder
    {
        private const string BuiltinFontName = "LegacyRuntime.ttf";

        // ユニット（5〜7）の手前、城のHPバー（10〜11）と同じくらい。数字はさらに手前
        private const int SmokeSortingOrder = 8;
        private const int BeamSortingOrder = 9;
        // 崩れる城・ユニット・撃破の数字より手前に降らせる
        private const int ConfettiSortingOrder = 15;
        private const int SparkSortingOrder = 12;
        private const int SoulSortingOrder = 12;
        private const int RewardShadowSortingOrder = 13;
        private const int RewardSortingOrder = 14;

        private const int RewardFontSize = 64;
        private const float RewardCharacterSize = 0.06f;
        private static readonly Vector3 RewardOffset = new Vector3(0f, 0.8f, 0f);
        private static readonly Vector3 RewardShadowOffset = new Vector3(0.04f, -0.04f, 0f);
        private static readonly Color RewardColor = new Color(1f, 0.85f, 0.2f);

        private static (BattleEventPresenter presenter, PenguinWarsAudio audio) CreateEffects(
            BattleRunner battleRunner, BattleCamera battleCamera, BattleHud hud, BossWarningBanner bossBanner, CastleView leftCastle, CastleView rightCastle)
        {
            var effectsObj = new GameObject("Effects");
            Transform root = effectsObj.transform;
            var audio = effectsObj.AddComponent<PenguinWarsAudio>();
            var presenter = effectsObj.AddComponent<BattleEventPresenter>();
            var bossEntrance = effectsObj.AddComponent<BossEntrancePresenter>();
            SetRefs(bossEntrance, ("_battleRunner", battleRunner), ("_battleCamera", battleCamera), ("_banner", bossBanner), ("_audio", audio));

            SetRefs(presenter, ("_battleRunner", battleRunner), ("_battleCamera", battleCamera), ("_hud", hud),
                ("_audio", audio),
                ("_leftCollapse", leftCastle.GetComponent<CastleCollapse>()),
                ("_rightCollapse", rightCastle.GetComponent<CastleCollapse>()),
                ("_bossEntrance", bossEntrance),
                ("_smokeTemplate", CreateSpriteEffect<SpawnSmokeEffect>(root, "SmokeTemplate", EffectArtGenerator.SmokePath, SmokeSortingOrder)),
                ("_sparkTemplate", CreateSpriteEffect<HitSparkEffect>(root, "SparkTemplate", EffectArtGenerator.SparkPath, SparkSortingOrder)),
                ("_soulTemplate", CreateSoulTemplate(root)),
                ("_beamTemplate", CreateSpriteEffect<CannonBeamEffect>(root, "BeamTemplate", EffectArtGenerator.BeamPath, BeamSortingOrder)),
                ("_confettiTemplate", CreateSpriteEffect<ConfettiEffect>(root, "ConfettiTemplate", EffectArtGenerator.ConfettiPath, ConfettiSortingOrder)));
            return (presenter, audio);
        }

        /// <summary>絵1枚だけの演出の見本。非アクティブにしておき、BattleEventPresenter が複製して使い回す</summary>
        private static T CreateSpriteEffect<T>(Transform parent, string name, string spritePath, int sortingOrder) where T : PooledEffect
        {
            SpriteRenderer renderer = CreateSprite(parent, name, PenguinSpriteWriter.Load(spritePath), sortingOrder);
            var effect = renderer.gameObject.AddComponent<T>();
            SetRefs(effect, ("_renderer", renderer));
            renderer.gameObject.SetActive(false);
            return effect;
        }

        private static SoulRiseEffect CreateSoulTemplate(Transform parent)
        {
            SpriteRenderer soul = CreateSprite(parent, "SoulTemplate", PenguinSpriteWriter.Load(EffectArtGenerator.SoulPath), SoulSortingOrder);
            GameObject soulObj = soul.gameObject;
            TextMesh shadow = CreateRewardText(soulObj.transform, "RewardShadow", Color.black,
                RewardOffset + RewardShadowOffset, RewardShadowSortingOrder);
            TextMesh label = CreateRewardText(soulObj.transform, "Reward", RewardColor, RewardOffset, RewardSortingOrder);

            var effect = soulObj.AddComponent<SoulRiseEffect>();
            SetRefs(effect, ("_soul", soul), ("_rewardLabel", label), ("_rewardShadow", shadow));
            soulObj.SetActive(false);
            return effect;
        }

        /// <summary>獲得さかなの数字。数字と「+」だけなので、日本語フォントが無くても表示できる標準フォントを使う</summary>
        private static TextMesh CreateRewardText(Transform parent, string name, Color color, Vector3 localPosition, int sortingOrder)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = localPosition;

            Font font = Resources.GetBuiltinResource<Font>(BuiltinFontName);
            var text = obj.AddComponent<TextMesh>();
            text.font = font;
            text.fontSize = RewardFontSize;
            text.characterSize = RewardCharacterSize;
            text.fontStyle = FontStyle.Bold;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.color = color;

            var renderer = obj.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = font.material;
            renderer.sortingOrder = sortingOrder;
            return text;
        }
    }
}
