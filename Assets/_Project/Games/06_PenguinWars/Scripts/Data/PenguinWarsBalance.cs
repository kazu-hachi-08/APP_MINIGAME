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

        public float FieldLength => _fieldLength;
        public int CastleHpEndless => _castleHpEndless;
        public int CastleHpVersus => _castleHpVersus;
    }
}
