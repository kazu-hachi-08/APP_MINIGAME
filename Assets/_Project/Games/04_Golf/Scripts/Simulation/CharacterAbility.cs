namespace MiniGame.Golf
{
    /// <summary>
    /// キャラの能力倍率（バランス型＝1）。BallSimulator が UnityEngine に依存しないよう、
    /// ScriptableObject（GolfCharacterData）から数値だけを取り出して渡す。
    /// </summary>
    public readonly struct CharacterAbility
    {
        public static readonly CharacterAbility Default = new CharacterAbility(1f, 1f);

        public CharacterAbility(float distanceRate, float straightnessRate)
        {
            DistanceRate = distanceRate;
            StraightnessRate = straightnessRate;
        }

        /// <summary>クラブの最大初速に掛ける。高いほど遠くまで飛ぶ</summary>
        public float DistanceRate { get; }

        /// <summary>クラブの曲がりやすさをこれで割る。高いほどフック・スライスが小さい</summary>
        public float StraightnessRate { get; }
    }
}
