using UnityEngine;

namespace MiniGame.Golf
{
    /// <summary>
    /// キャラ1体分の見た目と能力倍率。モルックの MolkkyCharacterData と同じ形。
    /// 1体1アセットに分け、2人で同時に数値調整してもコンフリクトしないようにしている。
    /// 倍率は バランス型=1 を基準にし、GolfPhysicsSettings・GolfClubData の基準値は変えずに掛けて使う。
    /// </summary>
    [CreateAssetMenu(fileName = "GolfChar_", menuName = "MiniGame/Golf/Character Data")]
    public class GolfCharacterData : ScriptableObject
    {
        [SerializeField] private string _displayName = "バランス型";

        [Tooltip("キャラ選択で使う正面の立ち絵")]
        [SerializeField] private Sprite _frontSprite;

        [Tooltip("背後視点のゴルファーの帽子の色")]
        [SerializeField] private Color _capColor = Color.white;

        [Tooltip("背後視点のゴルファーの髪の色")]
        [SerializeField] private Color _hairColor = Color.black;

        [Header("能力倍率（バランス型＝1）")]
        [Tooltip("クラブの最大初速の倍率。高いほど遠くまで飛ぶ（パターには効かない）")]
        [SerializeField] private float _distanceMultiplier = 1f;

        [Tooltip("曲がりにくさ。クラブの曲がりやすさをこの値で割る。高いほどフック・スライスが小さい")]
        [SerializeField] private float _straightnessMultiplier = 1f;

        public string DisplayName => _displayName;
        public Sprite FrontSprite => _frontSprite;
        public Color CapColor => _capColor;
        public Color HairColor => _hairColor;
        public float DistanceMultiplier => _distanceMultiplier;
        public float StraightnessMultiplier => _straightnessMultiplier;

        public CharacterAbility Ability => new CharacterAbility(_distanceMultiplier, _straightnessMultiplier);
    }
}
