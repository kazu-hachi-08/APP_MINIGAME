using MiniGame.PenguinWars.Battle;
using UnityEngine;

namespace MiniGame.PenguinWars
{
    /// <summary>
    /// 遊びながら調整する数値をまとめる（仕様書 §3）。コードを触らずにインスペクタで変えられるようにするため。
    /// 項目はフェーズごとに足していく。
    /// </summary>
    [CreateAssetMenu(fileName = "PenguinWarsBalance", menuName = "MiniGame/PenguinWars/Balance")]
    public class PenguinWarsBalance : ScriptableObject
    {
        [Tooltip("戦場の長さ（ワールド単位）。城の位置は SceneBuilder がこの値で置くので、変えたら Rebuild する")]
        [SerializeField] private float _fieldLength = 30f;
        [SerializeField] private int _castleHpEndless = 3000;
        [SerializeField] private int _castleHpVersus = 5000;
        [Tooltip("城の中心から出撃位置までの距離。城の絵の中から出てこないようにする")]
        [SerializeField] private float _spawnOffset = 1.5f;
        [Tooltip("場に出せる自軍ユニットの上限（仕様書 §4.3。スマホの処理負荷対策）")]
        [SerializeField] private int _maxUnitsPerSide = 30;

        [Header("働きペンギン（仕様書 §4.2）。要素の並びがレベル1, 2, 3…")]
        [Tooltip("さかなの上限")]
        [SerializeField] private int[] _walletCaps = { 500, 800, 1100, 1400, 1700, 2000, 2300, 2600 };
        [Tooltip("さかなが増える速さ（/秒）")]
        [SerializeField] private float[] _walletRates = { 30f, 40f, 50f, 60f, 70f, 80f, 90f, 100f };
        [Tooltip("次のレベルに必要なさかな。最大レベルの分は無いので、上限の表より1つ少なくする")]
        [SerializeField] private int[] _walletLevelUpCosts = { 80, 160, 240, 320, 400, 480, 560 };
        [Tooltip("撃破報酬 = 倒した敵のコスト × この値（仕様書 §4.1）")]
        [SerializeField] private float _killRewardRate = 0.5f;

        [Header("ペンギン砲（仕様書 §4.4）")]
        [SerializeField] private float _cannonChargeTime = 40f;
        [Tooltip("自城から戦場の長さのこの割合までの敵に当たる")]
        [SerializeField] private float _cannonRangeRatio = 0.6f;
        [SerializeField] private int _cannonDamage = 100;

        [Header("ノックバック・特殊能力（仕様書 §5.3・§5.4）")]
        [Tooltip("ノックバックで後ろに飛ばされる距離")]
        [SerializeField] private float _knockbackDistance = 1.5f;
        [Tooltip("ノックバック中に動けない秒数")]
        [SerializeField] private float _knockbackDuration = 0.5f;
        [Tooltip("「遅くする」中の移動速度の倍率")]
        [SerializeField] private float _slowSpeedMultiplier = 0.5f;
        [Tooltip("城キラーの城へのダメージ倍率")]
        [SerializeField] private float _castleKillerMultiplier = 3f;

        [Header("エンドレス（仕様書 §2.1・§8.1）")]
        [Tooltip("ランダム編成の人数")]
        [SerializeField] private int _deckSize = 10;
        [Tooltip("ランダム編成に必ず入れる壁の数（壁がいないと序盤で詰むため）")]
        [SerializeField] private int _deckMinWalls = 2;
        [Tooltip("敵レベルが上がる間隔（秒）")]
        [SerializeField] private float _enemyLevelUpInterval = 30f;
        [Tooltip("レベル1の敵の出現間隔（秒）")]
        [SerializeField] private float _enemyBaseSpawnInterval = 4f;
        [Tooltip("レベルが1上がるごとに出現間隔にかける値")]
        [SerializeField] private float _enemySpawnIntervalMultiplier = 0.9f;
        [SerializeField] private float _enemyMinSpawnInterval = 0.8f;
        [Tooltip("出現キャラのコスト上限 = 基本 + レベル × 増分")]
        [SerializeField] private int _enemyCostLimitBase = 300;
        [SerializeField] private int _enemyCostLimitPerLevel = 250;
        [Tooltip("敵の体力・攻撃の倍率 = 基本 + レベル × 増分")]
        [SerializeField] private float _enemyStatMultiplierBase = 1f;
        [SerializeField] private float _enemyStatMultiplierPerLevel = 0.15f;
        [Tooltip("このレベルの倍数になったら大型を1体確定で出す")]
        [SerializeField] private int _enemyBossLevelInterval = 5;

        public float FieldLength => _fieldLength;
        public int CastleHpEndless => _castleHpEndless;
        public int CastleHpVersus => _castleHpVersus;
        public float SpawnOffset => _spawnOffset;
        public int MaxUnitsPerSide => _maxUnitsPerSide;
        public float KillRewardRate => _killRewardRate;
        public float CannonChargeTime => _cannonChargeTime;
        public float CannonRangeRatio => _cannonRangeRatio;
        public int CannonDamage => _cannonDamage;
        public int DeckSize => _deckSize;
        public int DeckMinWalls => _deckMinWalls;
        public float KnockbackDistance => _knockbackDistance;
        public float KnockbackDuration => _knockbackDuration;
        public float SlowSpeedMultiplier => _slowSpeedMultiplier;
        public float CastleKillerMultiplier => _castleKillerMultiplier;

        public WalletTable CreateWalletTable()
        {
            return new WalletTable(_walletCaps, _walletRates, _walletLevelUpCosts);
        }

        public EnemyWaveSettings CreateEnemyWaveSettings()
        {
            return new EnemyWaveSettings
            {
                LevelUpInterval = _enemyLevelUpInterval,
                BaseSpawnInterval = _enemyBaseSpawnInterval,
                SpawnIntervalMultiplier = _enemySpawnIntervalMultiplier,
                MinSpawnInterval = _enemyMinSpawnInterval,
                CostLimitBase = _enemyCostLimitBase,
                CostLimitPerLevel = _enemyCostLimitPerLevel,
                StatMultiplierBase = _enemyStatMultiplierBase,
                StatMultiplierPerLevel = _enemyStatMultiplierPerLevel,
                BossLevelInterval = _enemyBossLevelInterval,
            };
        }
    }
}
