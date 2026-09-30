using UnityEngine;

namespace MiniGame.LifeGame
{
    /// <summary>
    /// キャラ1体分の名前・能力・セリフ（仕様書 §9）。
    /// 1体1アセットに分け、2人で同時に調整してもコンフリクトしないようにしている。見た目はフェーズ6で足す。
    /// </summary>
    [CreateAssetMenu(fileName = "LifeChar_", menuName = "MiniGame/LifeGame/Character Data")]
    public class LifeCharacterData : ScriptableObject
    {
        [SerializeField] private string _displayName = "バランス";
        [SerializeField] private LifeAbility _ability = LifeAbility.StartMoney;

        [Tooltip("キャラ選択で見せる能力の説明。数値ではなく1行の文で見せる（仕様書 §9.3）")]
        [SerializeField] private string _abilityText = "初期所持金が多い";

        [SerializeField] private string _victoryLine = "堅実な人生だった！";

        public string DisplayName => _displayName;
        public LifeAbility Ability => _ability;
        public string AbilityText => _abilityText;
        public string VictoryLine => _victoryLine;
    }
}
