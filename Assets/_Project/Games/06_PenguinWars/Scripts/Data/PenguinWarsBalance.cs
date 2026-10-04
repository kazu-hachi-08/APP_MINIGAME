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

        public float FieldLength => _fieldLength;
        public int CastleHpEndless => _castleHpEndless;
        public int CastleHpVersus => _castleHpVersus;
        public float SpawnOffset => _spawnOffset;
        public int MaxUnitsPerSide => _maxUnitsPerSide;

        public WalletTable CreateWalletTable()
        {
            return new WalletTable(_walletCaps, _walletRates, _walletLevelUpCosts);
        }
    }
}
