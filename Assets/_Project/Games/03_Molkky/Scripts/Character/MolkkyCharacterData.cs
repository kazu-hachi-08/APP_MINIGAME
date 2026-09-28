using UnityEngine;

namespace MiniGame.Molkky
{
    /// <summary>
    /// キャラ1体分の見た目と能力倍率（キャラクター計画 §1）。
    /// 1体1アセットに分け、2人で同時に数値調整してもコンフリクトしないようにしている。
    /// 倍率は バランス型=1 を基準にし、MolkkyPhysicsSettings の基準値は変えずに掛けて使う。
    /// </summary>
    [CreateAssetMenu(fileName = "MolkkyChar_", menuName = "MiniGame/Molkky/Character Data")]
    public class MolkkyCharacterData : ScriptableObject
    {
        [SerializeField] private string _displayName = "バランス型";

        [Tooltip("投擲ラインで構えるときの背中")]
        [SerializeField] private Sprite _backSprite;

        [Tooltip("キャラ選択・勝利演出で使う正面の立ち絵")]
        [SerializeField] private Sprite _frontSprite;

        [Header("能力倍率（Phase C3 で投擲に反映する）")]
        [Tooltip("最大初速の倍率。高いほど遠くまで届く")]
        [SerializeField] private float _powerMultiplier = 1f;

        [Tooltip("フリック速度→強さの対応幅の倍率。高いほど強さのさじ加減がしやすい")]
        [SerializeField] private float _controlMultiplier = 1f;

        [Tooltip("棒の長さの倍率。長いほどまとめて倒しやすい")]
        [SerializeField] private float _stickLengthMultiplier = 1f;

        [Tooltip("勝利演出のセリフ（Phase C4 で使う）")]
        [SerializeField] private string _victoryLine = "ぴったり50点！";

        public string DisplayName => _displayName;
        public Sprite BackSprite => _backSprite;
        public Sprite FrontSprite => _frontSprite;
        public float PowerMultiplier => _powerMultiplier;
        public float ControlMultiplier => _controlMultiplier;
        public float StickLengthMultiplier => _stickLengthMultiplier;
        public string VictoryLine => _victoryLine;
    }
}
